using System.Reflection;
using EFT.InventoryLogic;
using HarmonyLib;

namespace UIFixes;

public class MultiSelectItemContext : DragItemContext
{
    public MultiSelectItemContext(ItemContextAbstractClass itemContext, ItemRotation rotation) : base(itemContext, rotation)
    {
        // Adjust event handlers
        ItemContext sourceContext = SourceContext();
        if (sourceContext != null)
        {
            // Listen for underlying context being disposed, it might mean the item is gone (merged, destroyed, etc)
            sourceContext.OnDisposed += OnParentDispose;
            // This serves no purpose and causes stack overflows
            sourceContext.OnCloseDependentWindow -= CloseDependentWindows;
        }
    }

    public MultiSelectItemContext Refresh()
    {
        ItemContext sourceContext = SourceContext();
        return sourceContext != null && Item == sourceContext.Item ? new MultiSelectItemContext(sourceContext, ItemRotation) : null;
    }

    public void UpdateDragContext(DragItemContext itemContext)
    {
        SetPosition(itemContext.CursorPosition, itemContext.ItemPosition);
        ItemRotation = itemContext.ItemRotation;
    }

    public override void Dispose()
    {
        base.Dispose();
        ItemContext sourceContext = SourceContext();
        if (sourceContext != null)
        {
            sourceContext.OnDisposed -= OnParentDispose;
        }
    }

    private void OnParentDispose()
    {
        if (Item.CurrentAddress == null ||
            (Item.CurrentAddress.Container.ParentItem is MagazineItemClass &&
            Item.CurrentAddress.Container.ParentItem is not CylinderMagazineItemClass))
        {
            // This item was entirely merged away, or went into a magazine
            MultiSelect.Deselect(this);
        }
    }

    // used by ItemUiContext.QuickFindAppropriatePlace, the one that picks a container, i.e. ctrl-click
    // DragItemContext (drag) defaults to None, but we want what the underlying item allows
    public override bool CanQuickMoveTo(ETargetContainer targetContainer)
    {
        ItemContext sourceContext = SourceContext();
        return sourceContext != null
            ? sourceContext.CanQuickMoveTo(targetContainer)
            : base.CanQuickMoveTo(targetContainer);
    }


    private ItemContext SourceContext() => (ItemContext)AccessTools.Field(typeof(ItemContext), "_source").GetValue(this);}