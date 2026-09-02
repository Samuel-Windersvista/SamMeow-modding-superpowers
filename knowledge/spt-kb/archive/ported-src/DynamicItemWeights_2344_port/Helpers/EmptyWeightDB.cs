using BepInEx.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace Tosox.DynamicItemWeights.Helpers
{
    public class EmptyWeightDB
    {
        private static readonly string ConfigPath = Path.Combine(Plugin.ConfigFolder, "emptyweights.json");

        private readonly ManualLogSource _logger;
        private Dictionary<string, float> _byTpl = new Dictionary<string, float>();

        public EmptyWeightDB(ManualLogSource logger)
        {
            _logger = logger;
        }

        public bool Load()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    _logger.LogError("Failed to load empty weights: config file is missing");
                    return false;
                }

                string json = File.ReadAllText(ConfigPath);
                _byTpl = JsonConvert.DeserializeObject<Dictionary<string, float>>(json);

                if (_byTpl == null)
                {
                    _logger.LogError("Failed to load empty weights: deserialized data is null");
                    return false;
                }

                _logger.LogInfo($"Loaded {_byTpl.Count} empty weights");
                return true;
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Failed to load empty weights: {ex}");
                return false;
            }
        }

        public bool TryGet(string tpl, out float emptyWeight)
        {
            return _byTpl.TryGetValue(tpl, out emptyWeight);
        }
    }
}
