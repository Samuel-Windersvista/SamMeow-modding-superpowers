using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;

namespace _12Bundle
{
    public record ModMetadata : AbstractModMetadata
    {
        public override string Name { get; init; } = "BundleExample";
        public override string Author { get; init; } = "SPTarkov";
        public override List<string>? Contributors { get; init; }
        public override SemanticVersioning.Version Version { get; init; } = new("1.0.0");
        public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.0");
        
        
        public override List<string>? Incompatibilities { get; init; }
        public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
        public override string? Url { get; init; }
        public override bool? IsBundleMod { get; init; } = true;
        public override string License { get; init; } = "MIT";
        public override string ModGuid { get; init; } = "com.sp-tarkov.examples.bundleexample";
    }

    [Injectable(TypePriority = OnLoadOrder.PostSptModLoader)]
    public class BundleExample(ISptLogger<BundleExample> logger) : IOnLoad
    {
        public Task OnLoad()
        {
            logger.Success("Bundle example loaded!");
            return Task.CompletedTask;
        }
    }
}
