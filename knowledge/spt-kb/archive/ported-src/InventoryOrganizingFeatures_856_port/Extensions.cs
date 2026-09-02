using EFT.InventoryLogic;

namespace Seion.Iof
{
    internal static class Extensions
    {
        public static bool CanAccept(this Grid grid, Item item)
        {
            // SPT 4.1.2: ItemFilterExtension.CanAccept(IContainer, Item) (deobfuscated GClass2861/ContainerFilter)
            return ItemFilterExtension.CanAccept(grid, item);
        }
    }
}
