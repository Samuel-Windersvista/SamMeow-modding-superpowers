using EFT.InventoryLogic;
using System;
using Diz.LanguageExtensions;

using StashGridCollectionClass = EFT.InventoryLogic.GridItemCollection;
using FreeSpaceInventoryErrorClass = EFT.InventoryLogic.Grid.NoFreeSpaceError;
using FilterInventoryErrorClass = EFT.InventoryLogic.Grid.ItemFiltersWontAllowError;
using RemoveInventoryErrorClass = EFT.InventoryLogic.Grid.ItemNotInGridError;
using ContainerRemoveEventClass = EFT.InventoryLogic.ContainerRemoveResult;
using ContainerAddEventClass = EFT.InventoryLogic.GridAddResult;
using ContainerRemoveEventResultStruct = Diz.LanguageExtensions.OperationResult<EFT.InventoryLogic.ContainerRemoveResult>;
using ContainerAddEventResultStruct = Diz.LanguageExtensions.OperationResult<EFT.InventoryLogic.GridAddResult>;
using EFT.UI.DragAndDrop;
using EFT;
using Comfort.Common;


namespace DrakiaXYZ.LootRadius.Helpers
{
    /**
     * Custom StashGrid implementation that doesn't do parent ownership validation, and only allows removing items
     */
    class LootRadiusStashGrid : Grid
    {
        public static string GRIDID = "67e0b18aeef9ae200b0495f0";
        public GridView[] GridViews { get; set; } = null;

        public override GridItemCollection ItemCollection { get; } = new LootRadiusStashGridCollection();

        public LootRadiusStashGrid(string id, CompoundItem parentItem) : 
            base(id, 10, 10, true, false, Array.Empty<ItemFilter>(), parentItem, -1) { }

        /**
         * Don't allow moving items around in the custom grid, but allow adding new items
         */
        public override bool CheckCompatibility(Item item)
        {
            return !this.Contains(item);
        }

        /**
         * Simplified item adding, as we know the incoming data is sane. This removes any chance of accidentally changing the item address
         */
        public override ContainerAddEventResultStruct AddInternal(Item item, LocationInGrid location, bool simulate, bool ignoreRestrictions)
        {
            if (location == null)
            {
                return new FreeSpaceInventoryErrorClass(item, this);
            }

            if (!ignoreRestrictions && !this.CheckCompatibility(item))
            {
                return new FilterInventoryErrorClass(item, this);
            }

            IContainerResizeResult resizeResult = null;
            var newAddress = this.CreateItemAddress(location);
            if (simulate)
            {
                return new ContainerAddEventClass(this, item, newAddress, item.StackObjectsCount, resizeResult, true);
            }

            IntVec2 originalGridSize = new IntVec2(this.GridWidth, this.GridHeight);
            this.PlaceItem(item, location);
            IntVec2 newGridSize = new IntVec2(this.GridWidth, this.GridHeight);
            
            if (originalGridSize != newGridSize)
            {
                resizeResult = new GridResizeResult(this, originalGridSize, newGridSize);
            }

            return new ContainerAddEventClass(this, item, newAddress, item.StackObjectsCount, resizeResult, false);
        }

        /**
         * More simple item removal handling
         */
        public override ContainerRemoveEventResultStruct RemoveInternal(Item item, bool simulate, bool ignoreRestrictions)
        {
            if (!base.Contains(item))
            {
                return new RemoveInventoryErrorClass(item, this);
            }

            LocationInGrid locationInGrid = this.ItemCollection[item];
            if (!simulate)
            {
                base.RemoveItem(item, locationInGrid, true);
            }
            return new ContainerRemoveEventClass(item, base.CreateItemAddress(locationInGrid), simulate);
        }

        public void OwnerRemoveItemEvent(RemoveItemEventArgs args)
        {
            if (args.Status != CommandStatus.Succeed)
            {
                return;
            }

            // Child items of items in the grid, we don't want to actually remove them, let the grid handle it
            if (args.From.Container.ParentItem != args.Item)
            {
                return;
            }

            var owner = Singleton<GameWorld>.Instance.FindOwnerById(args.OwnerId);
            owner.RemoveItemEvent -= this.OwnerRemoveItemEvent;

            // If we have GridViews we can update, try to remove this item from them
            if (GridViews != null && this.ItemCollection.ContainsKey(args.Item))
            {
                var locationInGrid = this.ItemCollection[args.Item];
                var item = args.Item;
                var location = CreateItemAddress(locationInGrid);

                foreach (var gridView in GridViews)
                {
                    ((IRemoveHandler)gridView).OnItemRemoved(new RemoveItemEventArgs(item, location, CommandStatus.Begin, owner));
                    ((IRemoveHandler)gridView).OnItemRemoved(new RemoveItemEventArgs(item, location, CommandStatus.Succeed, owner));
                }
            }

            this.RemoveInternal(args.Item, false, false);
        }

        /**
         * Custom grid collection that doesn't do address validation
         */
        internal class LootRadiusStashGridCollection : GridItemCollection
        {
            public override void Add(Item item, Grid grid, LocationInGrid location)
            {
                this.Items[item] = location;
                this.ItemsList.Add(item);

                if (item.CurrentAddress == null)
                {
                    item.CurrentAddress = grid.CreateItemAddress(location);
                }
            }

            public override void Remove(Item item, Grid grid)
            {
                this.Items.Remove(item);
                this.ItemsList.Remove(item);

                if (item.CurrentAddress?.Container?.ID == grid.ID)
                {
                    item.CurrentAddress = null;
                }
            }
        }
    }
}
