using System;
using System.Threading;
using System.Threading.Tasks;
using EFT;
using KmyTarkovReflection;

// ReSharper disable UnusedMember.Global

namespace KmyTarkovApi.Helpers
{
    public class PoolManagerClassHelper
    {
        private static readonly Lazy<PoolManagerClassHelper> Lazy =
            new Lazy<PoolManagerClassHelper>(() => new PoolManagerClassHelper());

        public static PoolManagerClassHelper Instance => Lazy.Value;

        public static JobPriorityData JobPriorityHelper => JobPriorityData.Instance;

        public ObjectsFactory PoolManagerClass { get; private set; }

        public readonly RefHelper.HookRef Constructor;

        private readonly
            Func<ObjectsFactory, ObjectsFactory.PoolsCategory, ObjectsFactory.AssemblyType, ResourceKey[], object,
                IProgress<InitLevelProgress>,
                CancellationToken, Task> _refLoadBundlesAndCreatePools;

        private PoolManagerClassHelper()
        {
            var poolManagerClassType = typeof(ObjectsFactory);

            _refLoadBundlesAndCreatePools = RefHelper
                .ObjectMethodDelegate<Func<ObjectsFactory, ObjectsFactory.PoolsCategory,
                    ObjectsFactory.AssemblyType, ResourceKey
                    [], object, IProgress<InitLevelProgress>,
                    CancellationToken, Task>>(RefTool.GetEftMethod(poolManagerClassType, RefTool.Public,
                    x => x.Name == "LoadBundlesAndCreatePools" &&
                         x.GetParameters().Length == 6 &&
                         x.GetParameters()[0].ParameterType == typeof(ObjectsFactory.PoolsCategory)));

            Constructor = RefHelper.HookRef.Create(poolManagerClassType.GetConstructors()[0]);
        }

        [EFTHelperHook]
        private void Hook()
        {
            Constructor.Add(this, nameof(OnConstructor));
        }

        private static void OnConstructor(ObjectsFactory __instance)
        {
            Instance.PoolManagerClass = __instance;
        }

        public Task LoadBundlesAndCreatePools(ObjectsFactory instance, ObjectsFactory.PoolsCategory poolsCategory,
            ObjectsFactory.AssemblyType assemblyType, ResourceKey[] resources, object yield,
            IProgress<InitLevelProgress> progress = null,
            CancellationToken ct = default)
        {
            return _refLoadBundlesAndCreatePools(instance, poolsCategory, assemblyType, resources, yield, progress, ct);
        }

        public class JobPriorityData
        {
            private static readonly Lazy<JobPriorityData> Lazy =
                new Lazy<JobPriorityData>(() => new JobPriorityData());

            public static JobPriorityData Instance => Lazy.Value;

            public object General => Diz.Jobs.EJobPriority.General;

            public object Low => Diz.Jobs.EJobPriority.Low;

            public object Immediate => Diz.Jobs.EJobPriority.Immediate;

            private JobPriorityData()
            {
            }
        }
    }
}