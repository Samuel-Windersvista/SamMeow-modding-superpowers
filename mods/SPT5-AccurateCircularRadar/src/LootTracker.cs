using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Radar
{
    /// <summary>
    /// Decides which containers and loose items are worth a blip, keeps them in a quadtree, and works
    /// out which of them are close enough to draw on each sweep.
    /// </summary>
    /// <remarks>
    /// 4.1 -> 5.0 适配（IL2CPP）：
    /// <list type="bullet">
    /// <item>Il2Cpp 集合只实现 <c>Il2CppSystem.Collections.Generic</c> 的接口，托管 LINQ
    /// （<c>Reverse()</c> / <c>First()</c> / <c>ToArray()</c>）不再适用，改为显式循环。</item>
    /// <item><b>容器增删事件降级为轮询</b>：5.0 interop 的 <c>IItemOwner</c> 接口没有暴露
    /// <c>AddItemEvent</c> / <c>RemoveItemEvent</c>（只在内部 <c>Il2CppProxy</c> 上有 private 访问器），
    /// 无法按上游方式订阅。改为在 <see cref="Tick"/>（扫描间隔）里重估"玩家外圈范围内"的已跟踪容器：
    /// 容器在雷达上可见的前提就是落在 OuterRange 内，因此对可见行为等价；代价是每个扫描间隔对
    /// 已跟踪容器各读一次 transform 位置（不做全量价格计算，只有范围内的才评估）。</item>
    /// <item><c>GetWishlist()</c> 返回 Il2Cpp 的 <c>IReadOnlyDictionary</c>（含 TryGetValue）。</item>
    /// </list>
    /// </remarks>
    internal sealed class LootTracker
    {
        /// <summary>Item owners whose root item starts with this id are player inventories.</summary>
        private const string PlayerInventoryPrefix = "55d7217a4bdc2d86028b456d";

        /// <summary>Drawers legitimately sit at identical positions, so they skip de-duplication.</summary>
        private const string DrawerPrefix = "578f87b7245977356274f2cd";

        /// <summary>Padding added around the sampled loot bounds when sizing the quadtree, in metres.</summary>
        private const float BoundsPaddingX = 5f;
        private const float BoundsPaddingZ = 2f;

        private readonly GameWorld _gameWorld;
        private readonly Player _player;
        private readonly Il2CppSystem.Collections.Generic.IReadOnlyDictionary<MongoID, EWishlistGroup> _wishlist;

        private readonly Dictionary<string, BlipOther> _blips = new Dictionary<string, BlipOther>();

        /// <summary>
        /// Container owners we keep re-evaluating, by owner id (the polling replacement for the
        /// upstream AddItemEvent / RemoveItemEvent subscriptions).
        /// </summary>
        private readonly Dictionary<string, IItemOwner> _containerOwners = new Dictionary<string, IItemOwner>();

        /// <summary>Owner id -> transform, for the container poll.</summary>
        private readonly Dictionary<string, Transform> _containerTransforms = new Dictionary<string, Transform>();

        private Quadtree _tree;

        // Double buffered so a sweep does not allocate once the lists have grown.
        private List<BlipOther> _visible = new List<BlipOther>();
        private List<BlipOther> _previouslyVisible = new List<BlipOther>();
        private readonly HashSet<BlipOther> _visibleSet = new HashSet<BlipOther>();
        private readonly List<BlipOther> _toHide = new List<BlipOther>();

        private float _lastScanTime;

        public LootTracker(GameWorld gameWorld, Player player)
        {
            _gameWorld = gameWorld;
            _player = player;
            _wishlist = player.Profile?.WishlistManager?.GetWishlist();
        }

        /// <summary>Rescans every item owner in the world and rebuilds the spatial index.</summary>
        public void Rebuild()
        {
            Clear();

            float xMin = float.MaxValue, xMax = float.MinValue;
            float zMin = float.MaxValue, zMax = float.MinValue;

            // Il2Cpp 的 Dictionary 不参与托管 LINQ：先按枚举顺序落成托管列表，再倒序处理，
            // 以保持"同一位置上最后注册的 owner 生效"这一与游戏绘制顺序一致的行为。
            var owners = new List<OwnerEntry>();
            foreach (var entry in _gameWorld.ItemOwners)
                owners.Add(new OwnerEntry(entry.Key, entry.Value.Transform));

            var seenPositions = new HashSet<Vector3>();
            int skippedNull = 0, skippedPlayer = 0, skippedDup = 0, skippedNoRoot = 0;
            var samples = new List<string>(3);

            for (int i = owners.Count - 1; i >= 0; i--)
            {
                IItemOwner owner = owners[i].Owner;
                Transform transform = owners[i].Transform;

                if (owner == null || transform == null)
                {
                    skippedNull++;
                    continue;
                }

                if (owner.RootItem.Name.StartsWith(PlayerInventoryPrefix))
                {
                    skippedPlayer++;
                    continue;
                }

                if (!owner.ContainerName.StartsWith(DrawerPrefix) && !seenPositions.Add(transform.position))
                {
                    skippedDup++;
                    continue;
                }

                Item rootItem = FirstItem(owner.Items);
                if (rootItem == null)
                {
                    skippedNoRoot++;
                    continue;
                }

                // 诊断：前 3 个通过过滤的 owner 的样本（名称 / 是否容器 / 自身价格 / GetAllItems 产出数）。
                if (samples.Count < 3)
                    samples.Add($"name={rootItem.Name}, cont={rootItem.IsContainer}, price={PriceOf(rootItem)}, all={CountAllItems(rootItem)}");

                Add(owner.ID, rootItem, transform);

                if (rootItem.IsContainer)
                {
                    _containerOwners[owner.ID] = owner;
                    _containerTransforms[owner.ID] = transform;
                }

                Vector3 position = transform.position;
                xMin = Mathf.Min(xMin, position.x);
                xMax = Mathf.Max(xMax, position.x);
                zMin = Mathf.Min(zMin, position.z);
                zMax = Mathf.Max(zMax, position.z);
            }

            _tree = new Quadtree(Rect.MinMaxRect(
                xMin - BoundsPaddingX, zMin - BoundsPaddingZ,
                xMax + BoundsPaddingX, zMax + BoundsPaddingZ));

            foreach (BlipOther blip in _blips.Values)
                _tree.Insert(blip);

            RadarPlugin.Log.LogInfo(
                $"Loot scan: owners={owners.Count}, tracked={_blips.Count}, maxPrice={ItemPricing.LastMaxPrice}, " +
                $"threshold={RadarConfig.LootThreshold.Value}, skipped[null={skippedNull}, player={skippedPlayer}, " +
                $"dup={skippedDup}, noRoot={skippedNoRoot}]");

            foreach (string sample in samples)
                RadarPlugin.Log.LogInfo($"Loot sample: {sample}");

            // Show the new set on the next frame rather than waiting out a full scan interval.
            _lastScanTime = float.NegativeInfinity;
        }

        /// <summary>Registers an item if it clears the value or wishlist filter.</summary>
        /// <param name="lazyUpdate">
        /// Defer sampling the transform until the blip is first drawn. Items the game has just spawned
        /// have not settled yet, so reading their position immediately gives the wrong answer.
        /// </param>
        public void Add(string id, Item item, Transform transform, bool lazyUpdate = false)
        {
            if (item.Name.StartsWith(PlayerInventoryPrefix))
                return;

            bool wishlisted = IsWishlistedForRadar(item);
            bool valuable = IsValuableForRadar(item);
            if (!wishlisted && !valuable)
                return;

            // Re-registering the same id would otherwise strand the previous blip's GameObject.
            Remove(id);

            var blip = new BlipOther(id, transform, lazyUpdate, wishlisted ? BlipKind.WishlistLoot : BlipKind.Loot);
            _blips[id] = blip;
            _tree?.Insert(blip);
        }

        public void Remove(string id)
        {
            if (!_blips.TryGetValue(id, out BlipOther blip))
                return;

            _blips.Remove(id);
            _tree?.Remove(new Vector2(blip.TargetPosition.x, blip.TargetPosition.z), id);
            blip.DestroyBlip();
        }

        public void RemoveByKey(int key)
        {
            // [CRITICAL] 5.0 的 GetByKey 对不存在的 key 直接抛 KeyNotFoundException（interop 实现为
            // Dictionary.get_Item）。游戏会合法地对已移除 / 未跟踪的 key 调用 Remove(key)；
            // 异常若穿透补丁进入游戏代码会导致闪退（2026-09-15 实战事故）。先 ContainsKey 再取值。
            if (!_gameWorld.LootItems.ContainsKey(key))
                return;

            LootItem item = _gameWorld.LootItems.GetByKey(key);
            if (item == null)
                return;

            Remove(item.ItemId);
        }

        public void Clear()
        {
            foreach (BlipOther blip in _blips.Values)
                blip.DestroyBlip();

            _blips.Clear();
            _containerOwners.Clear();
            _containerTransforms.Clear();
            _tree?.Clear();
            _tree = null;

            _visible.Clear();
            _previouslyVisible.Clear();
            _visibleSet.Clear();
            _toHide.Clear();
        }

        /// <summary>
        /// Re-queries which loot is in range. Runs on its own timer so it stays on the scan interval
        /// even when Fire Mode drives the player sweep at a much higher rate.
        /// </summary>
        public void Tick()
        {
            if (Time.time - _lastScanTime < RadarConfig.ScanInterval.Value)
                return;

            _lastScanTime = Time.time;

            (_previouslyVisible, _visible) = (_visible, _previouslyVisible);
            _visible.Clear();

            Vector3 position = _player.Transform.position;
            _tree?.QueryRange(new Vector2(position.x, position.z), RadarConfig.OuterRange.Value, _visible);

            _visibleSet.Clear();
            foreach (BlipOther blip in _visible)
                _visibleSet.Add(blip);

            _toHide.Clear();
            foreach (BlipOther blip in _previouslyVisible)
            {
                if (!_visibleSet.Contains(blip))
                    _toHide.Add(blip);
            }

            PollContainers(position);
        }

        public void Render()
        {
            // Blips that just left range still need a frame to clear themselves off the face.
            foreach (BlipOther blip in _toHide)
                blip.Update();

            foreach (BlipOther blip in _visible)
                blip.Update();
        }

        /// <summary>
        /// Polling replacement for the upstream container add/remove event handlers: re-evaluates the
        /// tracked containers that sit inside the radar's outer range and keeps their blips in sync.
        /// </summary>
        private void PollContainers(Vector3 playerPosition)
        {
            if (!RadarConfig.LootEnabled.Value && !RadarConfig.WishlistLootEnabled.Value)
                return;

            if (_containerOwners.Count == 0)
                return;

            float outerRange = RadarConfig.OuterRange.Value;
            float sqrOuterRange = outerRange * outerRange;

            foreach (var pair in _containerOwners)
            {
                string id = pair.Key;
                IItemOwner owner = pair.Value;

                if (owner == null || !_containerTransforms.TryGetValue(id, out Transform transform) || transform == null)
                    continue;

                Vector3 position = transform.position;
                float dx = position.x - playerPosition.x;
                float dz = position.z - playerPosition.z;
                if (dx * dx + dz * dz > sqrOuterRange)
                    continue;

                Item root = FirstItem(owner.Items);
                if (root == null)
                    continue;

                bool wishlisted = IsWishlistedForRadar(root);
                bool valuable = IsValuableForRadar(root);
                bool hasBlip = _blips.ContainsKey(id);

                if (!wishlisted && !valuable)
                {
                    if (hasBlip)
                        Remove(id);
                }
                else if (!hasBlip)
                {
                    Add(id, root, transform);
                }
                else
                {
                    SetKind(id, wishlisted ? BlipKind.WishlistLoot : BlipKind.Loot);
                }
            }
        }

        private void SetKind(string id, BlipKind kind)
        {
            if (_blips.TryGetValue(id, out BlipOther blip))
                blip.SetKind(kind);
        }

        /// <summary>Wishlisted according to the current setting.</summary>
        private bool IsWishlistedForRadar(Item item) =>
            RadarConfig.WishlistLootEnabled.Value && IsWishlisted(item);

        /// <summary>Above the configured price threshold according to the current setting.</summary>
        private static bool IsValuableForRadar(Item item) =>
            RadarConfig.LootEnabled.Value && IsValuable(item);

        /// <summary>True when the item - or anything inside it - beats the configured price threshold.</summary>
        private static bool IsValuable(Item item)
        {
            if (item == null)
                return false;

            if (!item.IsContainer)
                return PriceOf(item) > RadarConfig.LootThreshold.Value;

            int best = 0;
            foreach (Item subItem in item.GetAllItems())
                best = Mathf.Max(best, PriceOf(subItem));

            return best > RadarConfig.LootThreshold.Value;
        }

        private static int PriceOf(Item item)
        {
            int price = ItemPricing.GetBestPrice(item);
            if (!RadarConfig.LootValuePerSlot.Value)
                return price;

            IntVec2 cellSize = item.CalculateCellSize();
            int slots = cellSize.X * cellSize.Y;
            return slots > 0 ? price / slots : price;
        }

        private bool IsWishlisted(Item item)
        {
            if (_wishlist == null || item == null)
                return false;

            if (!item.IsContainer)
                return IsTemplateWishlisted(item.TemplateId);

            foreach (Item subItem in item.GetAllItems())
            {
                if (IsTemplateWishlisted(subItem.TemplateId))
                    return true;
            }

            return false;
        }

        private bool IsTemplateWishlisted(MongoID templateId) =>
            _wishlist.TryGetValue(templateId, out EWishlistGroup group) && group == EWishlistGroup.Other;

        /// <summary>Il2Cpp 的 <c>IEnumerable&lt;Item&gt;</c> 上无法用 LINQ，手取第一个元素。</summary>
        private static Item FirstItem(Il2CppSystem.Collections.Generic.IEnumerable<Item> items)
        {
            if (items == null)
                return null;

            foreach (Item item in items)
                return item;

            return null;
        }

        /// <summary>
        /// 诊断：统计 <c>GetAllItems()</c>（ItemExtensions 的迭代器）实际产出的元素数。
        /// 1.1.5 下迭代器状态机经 interop 枚举若静默为空，容器估值会恒为 0。
        /// 返回 -1 表示枚举抛异常。
        /// </summary>
        private static int CountAllItems(Item item)
        {
            int count = 0;
            try
            {
                foreach (Item _ in item.GetAllItems())
                    count++;
            }
            catch (Exception)
            {
                return -1;
            }

            return count;
        }

        private readonly struct OwnerEntry
        {
            public readonly IItemOwner Owner;
            public readonly Transform Transform;

            public OwnerEntry(IItemOwner owner, Transform transform)
            {
                Owner = owner;
                Transform = transform;
            }
        }
    }
}
