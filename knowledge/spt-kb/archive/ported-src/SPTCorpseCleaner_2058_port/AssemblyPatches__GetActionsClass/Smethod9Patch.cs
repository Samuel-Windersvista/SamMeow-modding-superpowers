using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.Communications;
using SPT.Reflection.Patching;
using System;
using System.Reflection;

namespace SPTCorpseCleaner.AssemblyPatches__GetActionsClass {
    public class Smethod9Patch : ModulePatch {
        protected override MethodBase GetTargetMethod () {
            return typeof(EFT.InteractionContextHelper).GetMethod(nameof(EFT.InteractionContextHelper.GetAvailableInteractionState), new Type[] { typeof(GamePlayerOwner), typeof(Item), typeof(ItemController), typeof(String), typeof(String), typeof(IPlayer) });
        }

        [PatchPostfix]
        public static void Postfix (GamePlayerOwner owner, Item rootItem, ItemController lootItemOwner, String lootItemId, String lootItemName, IPlayer lootItemLastOwner, ref AvailableInteractionState __result) {
            if (lootItemName.ToLowerInvariant() != "corpse") { return; }
            if (rootItem.TemplateId != "55d7217a4bdc2d86028b456d") { return; }// this id means "默认物品栏"
            if (__result.Actions.FindIndex(x => x.Name.ToLowerInvariant() == "search") < 0) { return; }
            __result.Actions.Add(new InteractionAction() {
                Name = "arena/contextInteractions/card/delete".Localized(null),
                TargetName = lootItemName,
                Action = Smethod9Patch.DeleteCorpse
            });
        }

        private static void DeleteCorpse () {
            GameWorld? gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld == null) { return; }
            InteractableObject? interactableObject = GamePlayerOwner.MyPlayer?.InteractableObject;
            if (interactableObject == null || !interactableObject.isActiveAndEnabled) {
                NotificationManager.DisplayMessageNotification("no target or target invalid", ENotificationDurationType.Default, ENotificationIconType.Default, null);
                return;
            }
            Corpse? corpse = interactableObject.GetComponent<Corpse>();
            if (corpse == null) {
                NotificationManager.DisplayMessageNotification("target is not a corpse", ENotificationDurationType.Default, ENotificationIconType.Default, null);
                return;
            }
            corpse.Kill();
            corpse.gameObject.SetActive(false);
            corpse.gameObject.DestroyAllChildren(false);
            gameWorld.DestroyLoot(corpse);// Corpse is essentially the IKillableLootItem, pop it for Radar and DynamicMaps
            NotificationManager.DisplayMessageNotification(String.Concat("corpse <", interactableObject.name, "> has been deleted"), ENotificationDurationType.Default, ENotificationIconType.Default, null);
        }
    }
}
