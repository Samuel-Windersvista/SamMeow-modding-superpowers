using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using EFT;
using EFT.UI;
using EFT.InventoryLogic;
using UnityEngine.Networking;
using System.Threading.Tasks;

using Logger = SPT.Common.Utils.ServerLog;
using Comfort.Common;
using System.Reflection;
using MunitionsExpert;
using static MunitionsExpert.Attributes;
using MunitionsExpert.Patches;
using System.IO;

namespace MunitionsExpert
{
    public class MunitionsExpert
    {
        private static ModInformation _modInfo;
        public static ModInformation ModInfo
        {
            private set
            {
                _modInfo = value;
            }
            get
            {
                if (_modInfo == null)
                    _modInfo = ModInformation.Load();
                return _modInfo;
            }
        }

        public static Dictionary<Enum, Sprite> iconCache = new Dictionary<Enum, Sprite>();
        public static List<ItemAttribute> penAttributes = new List<ItemAttribute>();    // For refreshing armor class rating
        public static string modName = ModInfo.name;

        public static void Main()
        {
            new PatchManager().RunPatches();
            CacheIcons();
        }

        public static void CacheIcons()
        {
            iconCache.Add(ENewItemAttributeId.Damage, Resources.Load<Sprite>("characteristics/icons/icon_info_damage"));
            iconCache.Add(ENewItemAttributeId.FragmentationChance, Resources.Load<Sprite>("characteristics/icons/icon_info_shrapnelcount"));
            iconCache.Add(EItemAttributeId.LightBleedingDelta, Resources.Load<Sprite>("characteristics/icons/icon_info_bloodloss"));
            iconCache.Add(EItemAttributeId.HeavyBleedingDelta, Resources.Load<Sprite>("characteristics/icon_info_hydration"));
            iconCache.Add(ENewItemAttributeId.Penetration, Resources.Load<Sprite>("characteristics/icon_info_penetration"));
            _ = LoadTexture(ENewItemAttributeId.ArmorDamage, Path.Combine(ModInfo.path, "res/armorDamage.png"));
            _ = LoadTexture(ENewItemAttributeId.RicochetChance, Path.Combine(ModInfo.path, "res/ricochet.png"));
        }

        public static async Task LoadTexture(Enum id, string path)
        {
            using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(path))
            {
                uwr.SendWebRequest();

                while (!uwr.isDone)
                    await Task.Delay(5);

                if (uwr.responseCode != 200)
                {
                    Logger.Error(modName, $"[{modName}] Request error {uwr.responseCode}: {uwr.error}");
                }
                else
                {
                    // Get downloaded asset bundle
                    Logger.Info(modName, $"[{modName}] Retrieved texture! {id.ToString()} from {path}");
                    Texture2D cachedTexture = DownloadHandlerTexture.GetContent(uwr);
                    iconCache.Add(id, Sprite.Create(cachedTexture, new Rect(0, 0, cachedTexture.width, cachedTexture.height), new Vector2(0, 0)));
                }
            }
        }

        public static string RemoveSpaceBetweenValueAndPercent(string str)
        {
            return Regex.Replace(str, "(?<=[0-9]+)\\ (?=%)", string.Empty);
        }

        public static void FormatExistingAttributes(ref List<ItemAttribute> attributes, AmmoTemplate template)
        {
            if (attributes == null || template == null) return;

            for (int i = 0; i < attributes.Count; i++)
            {
                ItemAttribute attr = attributes[i];
                if (attr == null) continue;

                if ((EItemAttributeId)attr.Id == EItemAttributeId.CenterOfImpact)
                {
                    float num = template.ammoAccr;
                    (attr as ItemAttribute).LessIsGood = false; // More accuracy = better
                    attr.LabelVariations = EItemAttributeLabelVariations.Colored; // Red if bad, green if good
                    attr.StringValue = () => $"{num}%"; // Pointless but at least it'll be consistent lol
                }
                else if ((EItemAttributeId)attr.Id == EItemAttributeId.Recoil)
                {
                    float num = template.ammoRec;
                    (attr as ItemAttribute).LessIsGood = true; //Less recoil = better
                    attr.LabelVariations = EItemAttributeLabelVariations.Colored; //Red if bad, green if good
                    attr.StringValue = () => $"{num}%"; // Missing percent sign
                }
                else if ((EItemAttributeId)attr.Id == EItemAttributeId.DurabilityBurn)
                {
                    float num = (template.DurabilityBurnModificator - 1f) * 100f;
                    if(attr as ItemAttribute == null)
                    {
                        ItemAttribute attrChar = new ItemAttribute((EItemAttributeId)attr.Id);
                        attrChar.CopyFrom(attr);
                        attr = attrChar;
                    }

                    (attr as ItemAttribute).LessIsGood = true; //Less burn = better

                    attr.Base = () => (template.DurabilityBurnModificator - 1f);
                    attr.LabelVariations = EItemAttributeLabelVariations.Colored; //Red if bad, green if good
                    attr.StringValue = () => $"{num}%"; // Missing percent sign
                }

                string str = RemoveSpaceBetweenValueAndPercent(attr.StringValue());
                attr.StringValue = () => str;

                attributes[i] = attr;
            }
        }

        static public void AddNewAttributes(ref List<ItemAttribute> attributes, AmmoTemplate template)
        {
            int projCount = template.ProjectileCount;
            int totalDamage = template.Damage * template.ProjectileCount;

            string damageStr = totalDamage.ToString(); // Total damage
            if (template.ProjectileCount > 1)
            {
                damageStr += $" ({template.Damage} x {template.ProjectileCount})";  // Add the "damage calculation" after total damage (damage per pellet * pellet count)
            }

            ItemAttribute at_damage = new ItemAttribute(ENewItemAttributeId.Damage)
            {
                Name = ENewItemAttributeId.Damage.GetName(),
                Base = () => totalDamage,
                StringValue = () => damageStr,
                DisplayType = () => EItemAttributeDisplayType.Compact
            };
            attributes.Add(at_damage);

            if (template.ArmorDamage > 0)
            {
                ItemAttribute at_armordmg = new ItemAttribute(ENewItemAttributeId.ArmorDamage)
                {
                    Name = ENewItemAttributeId.ArmorDamage.GetName(),
                    Base = () => template.ArmorDamage,
                    StringValue = () => $"{(template.ArmorDamage).ToString()}%",
                    DisplayType = () => EItemAttributeDisplayType.Compact
                };
                attributes.Add(at_armordmg);
            }

            if (template.PenetrationPower > 0)
            {
                string getStringValue()
                {
                    int ratedClass = 0;

                    // SPT 4.1: ServerSettings armor-class threshold table no longer exists on the client.
                    // Use the standard EFT armor-class vs penetration-power mapping instead.
                    int pen = template.PenetrationPower;
                    if (pen >= 60) ratedClass = 6;
                    else if (pen >= 50) ratedClass = 5;
                    else if (pen >= 40) ratedClass = 4;
                    else if (pen >= 30) ratedClass = 3;
                    else if (pen >= 20) ratedClass = 2;
                    else ratedClass = 1;

                    return $"{(ratedClass > 0 ? $"{"ME_class".Localized(EFT.EStringCase.None)} {ratedClass}" : "ME_noarmor".Localized(EFT.EStringCase.None))} ({template.PenetrationPower.ToString()})";
                }

                ItemAttribute at_pen = new ItemAttribute(ENewItemAttributeId.Penetration)
                {
                    Name = ENewItemAttributeId.Penetration.GetName(),
                    Base = () => template.PenetrationPower,
                    StringValue = getStringValue,
                    DisplayType = () => EItemAttributeDisplayType.Compact
                };
                attributes.Add(at_pen);
            }

            if (template.FragmentationChance > 0)
            {
                ItemAttribute at_frag = new ItemAttribute(ENewItemAttributeId.FragmentationChance)
                {
                    Name = ENewItemAttributeId.FragmentationChance.GetName(),
                    Base = () => template.FragmentationChance,
                    StringValue = () => $"{(template.FragmentationChance * 100).ToString()}%",
                    DisplayType = () => EItemAttributeDisplayType.Compact
                };
                attributes.Add(at_frag);
            }

            if (template.RicochetChance > 0)
            {
                ItemAttribute at_ricochet = new ItemAttribute(ENewItemAttributeId.RicochetChance)
                {
                    Name = ENewItemAttributeId.RicochetChance.GetName(),
                    Base = () => template.RicochetChance,
                    StringValue = () => $"{(template.RicochetChance * 100).ToString()}%",
                    DisplayType = () => EItemAttributeDisplayType.Compact
                };
                attributes.Add(at_ricochet);
            }
        }
    }
}
