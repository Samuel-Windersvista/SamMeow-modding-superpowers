using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using SPTushonka.Common.Http;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Radar
{
    /// <summary>Lazily resolved handle to the client's backend session.</summary>
    /// <remarks>
    /// 4.1 -> 5.0 适配：<c>ClientAppUtils.GetMainApp().GetClientBackEndSession()</c> 在 5.0 没有对应工具类，
    /// 直接走其实现本体：<c>Singleton&lt;ClientApplication&lt;IEftSession&gt;&gt;.Instance.GetClientBackEndSession()</c>
    /// （4.1 的 ClientAppUtils 源码即此实现）。会话不可用时返回 null，调用方降级。
    /// </remarks>
    internal static class EftSession
    {
        private static IEftSession _session;

        public static IEftSession Current
        {
            get
            {
                if (_session != null)
                    return _session;

                try
                {
                    if (!Singleton<ClientApplication<IEftSession>>.Instantiated)
                        return null;

                    ClientApplication<IEftSession> app = Singleton<ClientApplication<IEftSession>>.Instance;
                    _session = app?.GetClientBackEndSession();
                }
                catch (Exception e)
                {
                    RadarPlugin.Log.LogWarning($"Client backend session unavailable: {e.Message}");
                    _session = null;
                }

                return _session;
            }
        }
    }

    internal static class TraderExtensions
    {
        /// <summary>
        /// 5.0 的 <c>GetSupplyData</c> 返回 Il2Cpp 的 <c>Task&lt;Result&lt;SupplyData&gt;&gt;</c>；
        /// Il2CppInterop 生成的 <c>TaskAwaiter&lt;T&gt;</c> 实现了托管 <c>INotifyCompletion</c>，因此可直接 await。
        /// </summary>
        public static async Task UpdateSupplyData(this EFT.Trading.Trader trader)
        {
            try
            {
                IEftSession session = EftSession.Current;
                if (session == null || trader == null)
                    return;

                Result<SupplyData> result = await session.GetSupplyData(trader.Id);
                if (result.Succeed)
                    trader._supplyData = result.Value;
                else
                    RadarPlugin.Log.LogWarning($"Failed to download supply data for trader {trader.Id}");
            }
            catch (Exception e)
            {
                RadarPlugin.Log.LogWarning($"Error updating supply data for trader {trader.Id}: {e.Message}");
            }
        }
    }

    /// <summary>
    /// Live flea prices served by the optional LootValue server mod.
    /// </summary>
    /// <remarks>
    /// Nothing here ever blocks the main thread: the availability probe and every price lookup run in
    /// the background, and callers get whatever is already cached. Missing prices fall back to the
    /// ragfair price table in <see cref="ItemPricing"/>.
    /// <para>
    /// 4.1 -> 5.0 适配：<c>RequestHandler.HttpClient.PostAsync(path, byte[])</c> 在 SPTushonka 里不存在，
    /// 改用等价的 <c>RequestHandler.PostJsonAsync(path, json)</c>；请求体手写 JSON，避免把托管 POCO
    /// 交给 IL2CPP 侧的 Newtonsoft 反射（不可行）。
    /// </para>
    /// </remarks>
    internal static class FleaPriceCache
    {
        private const string PricePath = "/LootValue/GetItemLowestFleaPrice";

        /// <summary>Cheap, always-present template used to decide whether the endpoint answers at all.</summary>
        private const string ProbeTemplateId = "5c06782b86f77426df5407d2";

        private const double CacheLifetimeSeconds = 300;

        private const int ProbePending = 0;
        private const int ProbeRunning = 1;
        private const int ProbeSucceeded = 2;
        private const int ProbeFailed = 3;

        private static readonly ConcurrentDictionary<string, CachedPrice> Cache =
            new ConcurrentDictionary<string, CachedPrice>();

        private static int _probeState = ProbePending;

        /// <summary>
        /// Whether the LootValue endpoint is answering. Kicks off the probe on first use and reports
        /// false until it lands, so the first loot scan is never stalled on the network.
        /// </summary>
        public static bool EndpointAvailable
        {
            get
            {
                if (!LootValueInstalled)
                    return false;

                if (Interlocked.CompareExchange(ref _probeState, ProbeRunning, ProbePending) == ProbePending)
                    _ = RunProbeAsync();

                return Volatile.Read(ref _probeState) == ProbeSucceeded;
            }
        }

        /// <summary>
        /// 本机是否安装 LootValue 服务端 mod（扫描 <c>SPT_Runtime/user/mods</c> 的目录名与 package.json）。
        /// 未安装时跳过探测：SPTushonka HTTP 客户端自带重试与错误日志，探测不存在的路由会在每次
        /// 会话产生 4 条 404 错误（2026-09-15 实战反馈）。检测失败时保持上游行为（照常探测）。
        /// </summary>
        private static bool LootValueInstalled
        {
            get
            {
                if (_lootValueInstalled.HasValue)
                    return _lootValueInstalled.Value;

                bool found = false;
                try
                {
                    string gameRoot = Path.GetDirectoryName(Application.dataPath);
                    string modsDir = Path.Combine(gameRoot ?? string.Empty, "SPT_Runtime", "user", "mods");
                    if (Directory.Exists(modsDir))
                    {
                        foreach (string dir in Directory.GetDirectories(modsDir))
                        {
                            string dirName = Path.GetFileName(dir);
                            if (dirName.IndexOf("lootvalue", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                found = true;
                                break;
                            }

                            string packageJson = Path.Combine(dir, "package.json");
                            if (File.Exists(packageJson) &&
                                File.ReadAllText(packageJson).IndexOf("lootvalue", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                found = true;
                                break;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    RadarPlugin.Log.LogDebug($"LootValue detection failed: {e.Message}");
                    found = true; // 检测失败 → 照常探测（保持上游行为）
                }

                _lootValueInstalled = found;
                if (!found)
                    RadarPlugin.Log.LogDebug("LootValue server mod not detected; live flea prices disabled.");

                return found;
            }
        }

        private static bool? _lootValueInstalled;

        /// <summary>
        /// The cached price for a template, refreshing it in the background once stale.
        /// Returns null while a price has yet to arrive.
        /// </summary>
        public static double? FetchPrice(string templateId)
        {
            try
            {
                IEftSession session = EftSession.Current;
                if (session == null || session.RagFair == null || !session.RagFair.Available)
                    return null;
            }
            catch (Exception e)
            {
                RadarPlugin.Log.LogDebug($"RagFair unavailable, flea price skipped: {e.Message}");
                return null;
            }

            if (!Cache.TryGetValue(templateId, out CachedPrice cached))
            {
                _ = QueryAndCacheAsync(templateId);
                return null;
            }

            if ((DateTime.UtcNow - cached.LastUpdate).TotalSeconds > CacheLifetimeSeconds)
                _ = QueryAndCacheAsync(templateId);

            return cached.Price;
        }

        private static async Task RunProbeAsync()
        {
            double? price = await QueryAndCacheAsync(ProbeTemplateId);
            Volatile.Write(ref _probeState, price != null ? ProbeSucceeded : ProbeFailed);
        }

        private static async Task<double?> QueryAndCacheAsync(string templateId)
        {
            string response;
            try
            {
                string json = "{\"templateId\":\"" + templateId + "\"}";
                response = await RequestHandler.PostJsonAsync(PricePath, json);
            }
            catch (Exception)
            {
                // LootValue is optional; a missing route is the normal case, not an error worth logging.
                return null;
            }

            if (string.IsNullOrEmpty(response) || response == "null")
                return null;

            if (!double.TryParse(response, NumberStyles.Float, CultureInfo.InvariantCulture, out double price))
                return null;

            // LootValue reports a negative price for items it refuses to value.
            if (price < 0)
            {
                Cache.TryRemove(templateId, out _);
                return null;
            }

            Cache[templateId] = new CachedPrice(price);
            return price;
        }

        private readonly struct CachedPrice
        {
            public double Price { get; }
            public DateTime LastUpdate { get; }

            public CachedPrice(double price)
            {
                Price = price;
                LastUpdate = DateTime.UtcNow;
            }
        }
    }

    /// <summary>
    /// Works out what an item is worth, taking the better of the flea price and the best trader offer.
    /// </summary>
    /// <remarks>
    /// 4.1 -> 5.0 适配与降级：
    /// <list type="bullet">
    /// <item><c>Init(MonoBehaviour)</c> 的协程改为托管 async Task（<c>_ = InitAsync()</c>），不再需要 MonoBehaviour。</item>
    /// <item>ragfair 价格表：<c>RagfairGetPrices</c> 的回调参数 <c>Result&lt;Dictionary&lt;string,float&gt;&gt;</c>
    /// 是 Il2Cpp 值类型，经 <c>Callback&lt;T&gt;</c> 的隐式转换（来自 <c>System.Action&lt;Result&lt;T&gt;&gt;</c>）订阅；
    /// 若运行期封送失败则 <c>_fleaPrices</c> 保持 null，仅剩 LootValue HTTP 价格。</item>
    /// <item>trader 报价：<c>Traders</c> 枚举 + <c>GetUserItemPrice</c>；<c>ItemPrice.CurrencyId</c> 在 5.0 是
    /// <c>Nullable&lt;MongoID&gt;</c>（4.1 为 string），需显式转换。</item>
    /// <item>整条会话/网络路径失败一律降级为 0/-1，不崩溃、不阻塞主线程。</item>
    /// </list>
    /// </remarks>
    internal static class ItemPricing
    {
        /// <summary>Ragfair price table, downloaded once per raid.</summary>
        private static Dictionary<string, float> _fleaPrices;

        /// <summary>Best trader offer per item name; trader prices do not move during a raid.</summary>
        private static readonly Dictionary<string, int> TraderPrices = new Dictionary<string, int>();

        private static bool _initStarted;

        internal static bool Ready { get; private set; }

        /// <summary>Downloads trader supply data and the ragfair price table in the background.</summary>
        public static void Init()
        {
            if (_initStarted) return;

            _initStarted = true;
            _ = InitAsync();
        }

        public static int GetBestPrice(Item item)
        {
            // [CRITICAL] 各价格源相互隔离：任一来源抛异常不得毒化其它来源的结果。
            // 实战事故：商人供货数据异步就绪后 GetUserItemPrice 抛异常，被外层 catch 吞掉后
            // 整个 GetBestPrice 退化为 -1，已命中的本地价格表结果也被丢弃（tracked 58 -> 0）。
            int best = -1;

            try
            {
                best = Mathf.Max(best, GetFleaPrice(item));
            }
            catch (Exception e)
            {
                RadarPlugin.Log.LogDebug($"Flea price failed: {e.Message}");
            }

            try
            {
                best = Mathf.Max(best, GetBestTraderPrice(item));
            }
            catch (Exception e)
            {
                RadarPlugin.Log.LogDebug($"Trader price failed: {e.Message}");
            }

            try
            {
                best = Mathf.Max(best, GetHandbookPrice(item));
            }
            catch (Exception e)
            {
                RadarPlugin.Log.LogDebug($"Handbook price failed: {e.Message}");
            }

            // 诊断：记录扫描期间出现过的最高价（Rebuild 后写入日志）。
            if (best > LastMaxPrice)
                LastMaxPrice = best;

            return best;
        }

        /// <summary>诊断用：最近估价中出现的最高价格。</summary>
        internal static int LastMaxPrice { get; private set; }

        /// <summary>
        /// 1.1.5 的 <c>Item</c> 同时提供 <c>StringTemplateId</c>（string）与 <c>TemplateId</c>（MongoID）；
        /// 桥的实测代码优先用前者，这里保持同样的取值顺序，避免隐式转换在运行期的形态差异。
        /// </summary>
        private static string TemplateIdOf(Item item)
        {
            string stringId = item.StringTemplateId;
            return !string.IsNullOrEmpty(stringId) ? stringId : item.TemplateId.ToString();
        }

        /// <summary>
        /// 本地兜底价格源：物品手册价（<c>ItemTemplate.CreditsPrice</c>）。
        /// 5.0 下网络价格源可能全部不可用（ragfair 回调受 IL2CPP 封送限制、LootValue 为可选服务端 mod），
        /// 手册价保证「高价值物品」过滤仍能工作；阈值需按手册价量级调整。
        /// </summary>
        private static int GetHandbookPrice(Item item)
        {
            try
            {
                return item?.Template != null ? item.Template.CreditsPrice : 0;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private static async Task InitAsync()
        {
            try
            {
                await LoadTraderSupplyDataAsync();
            }
            catch (Exception e)
            {
                RadarPlugin.Log.LogWarning($"Trader supply data unavailable: {e.Message}");
            }

            try
            {
                LoadRagfairPrices();
            }
            catch (Exception e)
            {
                RadarPlugin.Log.LogWarning($"Ragfair price table unavailable: {e.Message}");
            }

            Ready = true;
        }

        private static async Task LoadTraderSupplyDataAsync()
        {
            IEftSession session = EftSession.Current;
            if (session == null)
                return;

            var traders = new List<EFT.Trading.Trader>();
            foreach (EFT.Trading.Trader trader in session.Traders)
            {
                if (trader != null)
                    traders.Add(trader);
            }

            foreach (EFT.Trading.Trader trader in traders)
                await trader.UpdateSupplyData();
        }

        private static void LoadRagfairPrices()
        {
            IEftSession session = EftSession.Current;
            if (session == null)
                return;

            // Il2Cpp 的 Result<T> 是值类型；这里用显式类型的托管委托，再经 Callback<T> 的隐式转换订阅。
            System.Action<Result<Il2CppSystem.Collections.Generic.Dictionary<string, float>>> onPrices = result =>
            {
                if (!result.Succeed || result.Value == null)
                {
                    RadarPlugin.Log.LogWarning("Failed to get ragfair price table");
                    return;
                }

                var table = new Dictionary<string, float>();
                foreach (var pair in result.Value)
                    table[pair.Key] = pair.Value;

                _fleaPrices = table;
            };

            session.RagfairGetPrices(onPrices);
        }

        private static int GetFleaPrice(Item item)
        {
            string templateId = TemplateIdOf(item);

            if (FleaPriceCache.EndpointAvailable)
            {
                double? price = FleaPriceCache.FetchPrice(templateId);
                if (price != null)
                    return (int)price.Value;
            }

            Dictionary<string, float> table = _fleaPrices;
            if (table != null && table.TryGetValue(templateId, out float tablePrice))
                return (int)tablePrice;

            // 本地跳蚤价格表（服务端 prices.json）— 5.0 的主要价格源（ragfair 回调不可用）。
            table = LocalPrices;
            if (table != null && table.TryGetValue(templateId, out float localPrice))
                return (int)localPrice;

            return -1;
        }

        /// <summary>
        /// 本地跳蚤价格表：直接读取 SPT 服务端数据文件
        /// <c>SPT_Runtime/SPT_Data/database/templates/prices.json</c>（templateId -&gt; 价格，约 4.7k 条）。
        /// 与 ragfair 回调下发的数据同源；单机环境下始终存在，是 5.0 下最可靠的本地价格源。
        /// 文件缺失 / 解析失败时返回 null，估价自动退化到后续来源。
        /// </summary>
        private static Dictionary<string, float> LocalPrices
        {
            get
            {
                if (_localPricesLoaded)
                    return _localPrices;

                _localPricesLoaded = true;
                try
                {
                    string gameRoot = Path.GetDirectoryName(Application.dataPath);
                    string path = Path.Combine(
                        gameRoot ?? string.Empty,
                        "SPT_Runtime", "SPT_Data", "database", "templates", "prices.json");

                    if (!File.Exists(path))
                    {
                        RadarPlugin.Log.LogWarning($"Local price table not found: {path}");
                        return null;
                    }

                    string text = File.ReadAllText(path);
                    var table = new Dictionary<string, float>();
                    foreach (Match match in Regex.Matches(text, "\"([0-9a-fA-F]{24})\"\\s*:\\s*(\\d+)"))
                    {
                        if (float.TryParse(match.Groups[2].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out float price))
                            table[match.Groups[1].Value] = price;
                    }

                    _localPrices = table;
                    RadarPlugin.Log.LogInfo($"Local flea price table loaded: {table.Count} entries.");
                }
                catch (Exception e)
                {
                    RadarPlugin.Log.LogWarning($"Local price table unavailable: {e.Message}");
                }

                return _localPrices;
            }
        }

        private static Dictionary<string, float> _localPrices;
        private static bool _localPricesLoaded;

        private static int GetBestTraderPrice(Item item)
        {
            if (_traderPricingDisabled)
                return 0;

            try
            {
                // 按模板缓存（含失败值）：避免每件物品每次估价都遍历全部商人（实战反馈的卡顿来源之一）。
                string key = TemplateIdOf(item);
                if (TraderPrices.TryGetValue(key, out int cached))
                    return cached;

                TraderOffer offer = GetBestTraderOffer(item);
                int price = offer?.Price ?? 0;

                // 初始化完成前不缓存失败值：商人供货数据是异步下载的，过早固化 0 会永久丢价。
                if (price > 0 || Ready)
                    TraderPrices[key] = price;

                return price;
            }
            catch (Exception e)
            {
                // 一次失败即熔断：本会话不再走商人价（本地价格表已覆盖主用途，避免每件物品反复抛异常）。
                _traderPricingDisabled = true;
                RadarPlugin.Log.LogWarning($"Trader pricing disabled after failure: {e.Message}");
                return 0;
            }
        }

        private static bool _traderPricingDisabled;

        private static TraderOffer GetBestTraderOffer(Item item)
        {
            IEftSession session = EftSession.Current;
            if (session == null)
                return null;

            switch (item.Owner?.OwnerType)
            {
                case EOwnerType.RagFair:
                case EOwnerType.Trader:
                    // Trader and flea stock is priced per stack; value a single unit instead.
                    if (item.StackObjectsCount > 1 || item.UnlimitedCount)
                    {
                        item = item.CloneItem();
                        item.StackObjectsCount = 1;
                        item.UnlimitedCount = false;
                    }
                    break;
            }

            // Il2Cpp 序列上无法用托管 LINQ 的 OrderByDescending：手工取最大值。
            TraderOffer best = null;
            double bestValue = double.NegativeInfinity;

            foreach (EFT.Trading.Trader trader in session.Traders)
            {
                TraderOffer offer = GetTraderOffer(item, trader);
                if (offer == null)
                    continue;

                double value = offer.Price * offer.Course;
                if (value > bestValue)
                {
                    bestValue = value;
                    best = offer;
                }
            }

            return best;
        }

        private static TraderOffer GetTraderOffer(Item item, EFT.Trading.Trader trader)
        {
            if (trader == null || trader._supplyData == null)
                return null;

            Il2CppSystem.Nullable<EFT.Trading.Trader.ItemPrice> result = trader.GetUserItemPrice(item);
            if (!result.HasValue)
                return null;

            EFT.Trading.Trader.ItemPrice price = result.Value;

            // 5.0：CurrencyId 是 Nullable<MongoID>（MongoID 有到 string 的隐式转换）。
            string currencyId = price.CurrencyId.HasValue ? price.CurrencyId.Value : null;

            double course = 1.0;
            Il2CppSystem.Collections.Generic.Dictionary<string, double> courses = trader.CurrencyCourses;
            if (courses != null && currencyId != null && courses.ContainsKey(currencyId))
                course = courses[currencyId];

            return new TraderOffer(trader.LocalizedName, price.Amount, course, item.StackObjectsCount);
        }

        public sealed class TraderOffer
        {
            public string Name { get; }
            public int Price { get; }
            public double Course { get; }
            public int Count { get; }

            public TraderOffer(string name, int price, double course, int count)
            {
                Name = name;
                Price = price;
                Course = course;
                Count = count;
            }
        }
    }
}
