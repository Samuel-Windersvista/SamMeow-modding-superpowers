using EFT;
using EFT.Interactive;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Radar
{
    /// <summary>
    /// Draws individual mines as blips and each minefield's footprint as a shaded region.
    /// </summary>
    /// <remarks>
    /// 4.1 -> 5.0 适配（IL2CPP）：
    /// <list type="bullet">
    /// <item><c>BorderZone._triggerZoneSettings</c> / <c>_extents</c> 在 interop 里是 public 字段，
    /// 不再需要反射（反射取值对 Il2Cpp 代理对象不可靠），改为直接字段访问。</item>
    /// <item>类型判定由 <c>zone.GetType().Name == "Minefield"</c> 改为 <c>TryCast&lt;Minefield&gt;()</c>：
    /// 泛型 <c>GetAllObjects&lt;BorderZone&gt;()</c> 返回的代理按声明类型包装，<c>GetType()</c> 恒为
    /// BorderZone；<c>TryCast</c> 走原生类型判定，才是正确的判断。</item>
    /// <item><c>GetAllObjects&lt;T&gt;()</c> 返回 Il2Cpp 序列，去掉托管 LINQ 的 <c>ToArray()</c>。</item>
    /// </list>
    /// </remarks>
    internal sealed class MinefieldTracker
    {
        private readonly GameWorld _gameWorld;
        private readonly List<BlipOther> _blips = new List<BlipOther>();
        private readonly List<RadarRegion> _regions = new List<RadarRegion>();

        public MinefieldTracker(GameWorld gameWorld)
        {
            _gameWorld = gameWorld;
        }

        public void Rebuild()
        {
            Clear();

            if (!RadarConfig.MinefieldEnabled.Value)
                return;

            foreach (var mine in _gameWorld.MineManager.Mines)
            {
                if (mine == null)
                    continue;

                _blips.Add(new BlipOther(mine.GetInstanceID().ToString(), mine.transform, false, BlipKind.Mine));
            }

            try
            {
                foreach (BorderZone zone in LocationScene.GetAllObjects<BorderZone>())
                {
                    if (zone == null || zone.TryCast<Minefield>() == null)
                        continue;

                    _regions.Add(new RadarRegion(BuildCorners(zone)));
                }
            }
            catch (Exception e)
            {
                // 单个区域失败不影响地雷 blip；仅记录并放弃轮廓。
                RadarPlugin.Log.LogWarning($"Minefield outlines disabled: {e.Message}");
            }
        }

        public void Clear()
        {
            foreach (RadarRegion region in _regions)
                region.Destroy();

            foreach (BlipOther blip in _blips)
                blip.DestroyBlip();

            _regions.Clear();
            _blips.Clear();
        }

        public void Render()
        {
            foreach (BlipOther blip in _blips)
                blip.Update();

            foreach (RadarRegion region in _regions)
                region.UpdateVisual();
        }

        /// <summary>
        /// Projects a zone's local box onto the ground plane as four world-space corners.
        /// </summary>
        /// <remarks>
        /// _extents is the half-size of the zone box; _triggerZoneSettings holds a per-edge inset in
        /// world units - x/y trim the +X/-X edges, z/w trim the +Z/-Z edges - which is what makes the
        /// lethal area smaller than the collider.
        /// </remarks>
        private static Vector3[] BuildCorners(BorderZone zone)
        {
            Vector4 triggerZoneSettings = zone._triggerZoneSettings;
            Vector3 extents = zone._extents;

            Transform transform = zone.transform;
            Vector3 scale = transform.lossyScale;

            float minX = -extents.x + triggerZoneSettings.y / scale.x;
            float maxX = extents.x - triggerZoneSettings.x / scale.x;
            float minZ = -extents.z + triggerZoneSettings.w / scale.z;
            float maxZ = extents.z - triggerZoneSettings.z / scale.z;

            return new[]
            {
                transform.TransformPoint(new Vector3(minX, 0, minZ)),
                transform.TransformPoint(new Vector3(minX, 0, maxZ)),
                transform.TransformPoint(new Vector3(maxX, 0, maxZ)),
                transform.TransformPoint(new Vector3(maxX, 0, minZ)),
            };
        }
    }
}
