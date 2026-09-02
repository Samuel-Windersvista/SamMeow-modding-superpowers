using System.Reflection;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using TMPro;
using UnityEngine;

namespace UIFixes;

public static class StashSearchPatches
{
    private static string Query = null;

    private static UnityEngine.UI.Toggle SearchButton = null;

    public static void Enable()
    {
        new FocusStashSearchPatch().Enable();
        new OpenSearchPatch().Enable();

        new AddSearchStashPatch().Enable();
        new PositionSearchStashPatch().Enable();
    }

    public class FocusStashSearchPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(StashSearchWindow), nameof(StashSearchWindow.Show));
        }

        [PatchPostfix]
        public static void Postfix(TMP_InputField ____searchField)
        {
            ____searchField.GetOrAddComponent<SearchKeyListener>();

            if (!string.IsNullOrEmpty(Query))
            {
                ____searchField.text = Query;
                Query = null;
            }

            ____searchField.ActivateInputField();
            ____searchField.Select();
        }
    }

    public class OpenSearchPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(SimpleStashPanel), nameof(SimpleStashPanel.Show));
        }

        [PatchPostfix]
        public static void Postfix(UnityEngine.UI.Toggle ____searchTab)
        {
            if (____searchTab == null || Plugin.InRaid())
            {
                return;
            }

            SearchButton = ____searchTab;

            var listener = ____searchTab.GetOrAddComponent<SearchKeyListener>();
            listener.Init(() =>
            {
                if (Settings.StashSearchContextMenu.Value)
                {
                    SetQuery();
                }

                OpenSearch();
            });
        }

        private static void SetQuery()
        {
            if (!Settings.ItemContextBlocksTextInputs.Value && Plugin.TextboxActive())
            {
                return;
            }

            // Item under cursor
            ItemContextAbstractClass itemContext = ItemUiContext.Instance.R().ItemContext;

            // Item being dragged
            DragItemContext dragItemContext = ItemUiContext.Instance.R().DragItemContext;

            // Only do anything if the mouse is over an item and nothing is being dragged
            if (itemContext == null || dragItemContext != null)
            {
                return;
            }

            Query = itemContext.Item.Name.Localized(EFT.EStringCase.None);
        }
    }

    private static void OpenSearch()
    {
        if (SearchButton != null && !SearchButton.isOn)
        {
            SearchButton.SetIsOnWithoutNotify(true);
        }
    }

    public class AddSearchStashPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.GetItemContextInteractions));
        }

        [PatchPostfix]
        private static void Prefix(ItemContextAbstractClass itemContext, ContextInteractions<EItemInfoButton> __result)
        {
            if (!Settings.StashSearchContextMenu.Value || Plugin.InRaid())
            {
                return;
            }

            // Not that
            if (__result.GetType().FullName == "UIFixes.EmptySlotMenu")
            {
                return;
            }

            if (itemContext.ViewType != EItemViewType.Inventory && itemContext.ViewType != EItemViewType.TradingPlayer)
            {
                return;
            }

            var item = itemContext.Item;
            if (item == null)
            {
                return;
            }

            var text = "UI/SearchWindow/Tooltip/SearchInStash".Localized(EFT.EStringCase.Upper);
            __result.method_2(
                "StashSearch",
                text,
                () =>
                {
                    Query = item.Name.Localized(EFT.EStringCase.None);
                    OpenSearch();
                },
                Resources.Load<Sprite>("Characteristics/Icons/FilterSearch"));
        }
    }

    public class PositionSearchStashPatch : ModulePatch
    {
        private static Transform TargetSibling = null;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(InteractionButtonsContainer), "method_1");
        }

        [PatchPostfix]
        public static void Postfix(string key, SimpleContextMenuButton __result)
        {
            if (Plugin.InRaid())
            {
                return;
            }

            if (key == EItemInfoButton.NeededSearch.ToString())
            {
                TargetSibling = __result.Transform;
            }

            // Dynamic actions use the localized string for the key, because BSG
            var text = "UI/SearchWindow/Tooltip/SearchInStash".Localized(EFT.EStringCase.Upper);
            if (key != text)
            {
                return;
            }

            if (TargetSibling != null)
            {
                __result.Transform.SetSiblingIndex(TargetSibling.GetSiblingIndex() + 1);
                TargetSibling = null;
            }
        }
    }
}