using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using Newtonsoft.Json;

namespace UpdateHierarchy
{
    [BepInPlugin("com.choccy.updatehierarchy", "Choccy.AnimatorUpdate", "1.0.3")]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo("Animator Updater is loaded");
            LoadList();
            //new UpdateAnimatorPatch1().Enable(); This one does not have any effect
            new UpdateAnimatorPatch2().Enable(); //this one has it but desync the hand
            //new ModSetupPatch1().Enable(); //this one has no effect
            new ModSetupPatch2().Enable(); //this one have the effect
            //new InventoryReloadPatch().Enable();
            
        }

        private void LoadList()
        {
            string dllPath = Assembly.GetExecutingAssembly().Location; //get assembly path
            string folderPath = Path.GetDirectoryName(dllPath);
            string jsonPath = Path.Combine(folderPath, "ExclusionList.json");// combines the path to get json directory

            if (!File.Exists(jsonPath))
            {
                //Logger.LogWarning("config.json not found next to plugin DLL.");
                return;
            }

            string json = File.ReadAllText(jsonPath);
            var data = JsonConvert.DeserializeObject<ExclusionListModel>(json);

            if (data != null && data.WeaponIDs != null)
            {
                JsonLoader.ExcludedWeapons = new HashSet<string>(data.WeaponIDs);
                //Logger.LogInfo($"Loaded {data.WeaponIDs.Count} excluded weapons.");
            }
        }
    }
}
//i don't know how to explain this but i sort of think i understand
//SORT OF
class ExclusionListModel
{
    public List<string> WeaponIDs;
}
public static class JsonLoader
{
    public static HashSet<string> ExcludedWeapons { get; set; } = new HashSet<string>();

    public static bool IsExcluded(string weaponName)
    {
        return ExcludedWeapons.Contains(weaponName);
    }
}
