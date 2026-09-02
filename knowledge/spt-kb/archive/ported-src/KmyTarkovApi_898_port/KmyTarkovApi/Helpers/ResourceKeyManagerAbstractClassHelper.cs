using System;
using System.Collections.Generic;

namespace KmyTarkovApi.Helpers
{
    public class ResourceKeyManagerAbstractClassHelper
    {
        private static readonly Lazy<ResourceKeyManagerAbstractClassHelper> Lazy =
            new Lazy<ResourceKeyManagerAbstractClassHelper>(() => new ResourceKeyManagerAbstractClassHelper());

        public static ResourceKeyManagerAbstractClassHelper Instance => Lazy.Value;

        // PORT-NOTE: ResourceKeyManagerAbstractClass 在 SPT 4.1 中已被移除（3.11 的 voice resource-key 静态字典
        // Dictionary_0 在 4.1 中无对应物，voice 数据改由 EFT.CustomizationSolver._voices 实例字典管理）。
        // 降级方案：VoiceDictionary 直接返回 null，调用方需自行判空。
        public Dictionary<string, string> VoiceDictionary => null;

        private ResourceKeyManagerAbstractClassHelper()
        {
        }
    }
}
