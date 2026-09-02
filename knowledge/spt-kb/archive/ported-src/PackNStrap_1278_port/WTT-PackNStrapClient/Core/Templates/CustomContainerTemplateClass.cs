using EFT.InventoryLogic;

namespace PackNStrap.Core.Templates;

public class CustomContainerTemplateClass : SearchableItemTemplate, IGridLayoutComponentTemplate
{
    string IGridLayoutComponentTemplate.LayoutName => LayoutName;

    public string LayoutName;
}