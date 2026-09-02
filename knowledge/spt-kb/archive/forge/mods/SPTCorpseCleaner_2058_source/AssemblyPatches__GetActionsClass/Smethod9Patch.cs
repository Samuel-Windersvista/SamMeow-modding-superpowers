using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using SPT.Reflection.Patching;
using System;
using System.Reflection;

namespace SPTCorpseCleaner.AssemblyPatches__GetActionsClass {
    public class Smethod9Patch : ModulePatch {
        protected override MethodBase GetTargetMethod () {
            return typeof(GetActionsClass).GetMethod(nameof(GetActionsClass.smethod_9));
        }

        [PatchPostfix]
        public static void Postfix (GamePlayerOwner owner, Item rootItem, TraderControllerClass lootItemOwner, String lootItemId, String lootItemName, IPlayer lootItemLastOwner, ref ActionsReturnClass __result) {
            if (lootItemName.ToLowerInvariant() != "corpse") {return;}
            if (rootItem.TemplateId != "55d7217a4bdc2d86028b456d") {return;}// this id means "默认物品栏"
            if (__result.Actions.FindIndex(x => x.Name.ToLowerInvariant() == "search") < 0) {return;}
            __result.Actions.Add(new ActionsTypesClass() {
                Name = "arena/contextInteractions/card/delete".Localized(),
                TargetName = lootItemName,
                Action = Smethod9Patch.DeleteCorpse
            });
        }

        private static void DeleteCorpse () {
            GameWorld? gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld == null) { return; }
            InteractableObject? interactableObject = gameWorld.MainPlayer?.InteractableObject;
            if (interactableObject == null || !interactableObject.isActiveAndEnabled) {
                NotificationManagerClass.DisplayMessageNotification("no target or target invalid");
                return;
            }
            Corpse? corpse = interactableObject.GetComponent<Corpse>();
            if (corpse == null) {
                NotificationManagerClass.DisplayMessageNotification("target is not a corpse");
                return;
            }
            corpse.Kill();
            corpse.gameObject.SetActive(false);
            corpse.gameObject.DestroyAllChildren();
            gameWorld.DestroyLoot(corpse);// Corpse is essentially the IKillableLootItem, pop it for Radar and DynamicMaps
            NotificationManagerClass.DisplayMessageNotification(String.Concat("corpse <", interactableObject.name, "> has been deleted"));
        }
    }
}
