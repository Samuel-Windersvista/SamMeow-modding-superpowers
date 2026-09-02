using Comfort.Common;
using EFT.UI;
using EFT.UI.DragAndDrop;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeltSlot.Helpers
{
    internal class UI_Mappings
    {
        #region Variables
        // Main Menu UI Elements
        public GameObject containerGameObject = null;
        public GameObject lootContainerGameObject = null;
        public GameObject slotTemplate = null;
        public GameObject beltSlot = null;
        public GameObject armBandSlot = null;
        public GameObject windowsPlaceHolder = null;
        public GameObject windowCloseButton = null;
        //public GameObject widowedGridWindow = null;
        public ToggleButton toggleButton = null;
        public InventoryScreen inventoryScreen = null;
        public PreloaderUI preloaderUI = null;
        public bool noActiveWindow = false;

        public GameObject[] windowsPlaceHolderArray = null;
        #endregion

        #region Game Object Mappings
        // Mappings of container view in the inventory screen
        public void setContainer_Mappings()
        {
            if(inventoryScreen == null)
            {
                inventoryScreen = Singleton<CommonUI>.Instance.InventoryScreen;
            }
            containerGameObject = inventoryScreen.transform.Find("Items Panel/LeftSide/Containers Panel/Scrollview Parent/Containers Scrollview/Content").gameObject;
            slotTemplate = containerGameObject.transform.Find("Slot Template").gameObject;
        }

        // Mappings of equipment view in the inventory screen, currently just the armband slot
        public void setEquipment_Mappings()
        {
            if (inventoryScreen == null)
            {
                inventoryScreen = Singleton<CommonUI>.Instance.InventoryScreen;
            }
            armBandSlot = inventoryScreen.transform.Find("Items Panel/LeftSide/Left Panel/Gear Panel/ArmBand Slot").gameObject;
        }

        // Mappings of preloader UI elements
        public void setPrelaoderUI_Mappings()
        {
            if(preloaderUI == null)
            {
                preloaderUI = Singleton<PreloaderUI>.Instance;
            }
            windowsPlaceHolder = preloaderUI.transform.Find("Preloader UI/UIContext/WindowsPlaceholder").gameObject;
        }

        // Mappings of complex loot container view in the inventory screen, currently not used
        public void setComplexLootUI_Mappings()
        {
            if (inventoryScreen == null)
            {
                inventoryScreen = Singleton<CommonUI>.Instance.InventoryScreen;
            }
            lootContainerGameObject = inventoryScreen.transform.Find("Items Panel/Items Panel/Stash Panel/Complex Loot Panel/Containers Scrollview/Content").gameObject;
        }

        // Mappings of the belt slot, which is instantiated from the slot template
        public void setBeltSlot_Mappings()
        {

            beltSlot = GameObject.Instantiate(slotTemplate, containerGameObject?.transform);

        }
        #endregion

        // Sets the priority window
        public void setWindowPriorityButton(GameObject? target)
        {
            setWindowsPlaceHolderArray();
            if ((windowsPlaceHolderArray.Length) <= 15)
            {
                noActiveWindow = true;
                return;
            }
            if(target != null)
            {
                GameObject targetCaption = target.transform.Find("Caption Panel").gameObject;
                GameObject targetButton = targetCaption.transform.Find("Priority").gameObject;
                toggleButton = targetButton.GetComponent<ToggleButton>();
                noActiveWindow = false;
            }
            GameObject windowClone = getGridWindowClone();
            if (windowClone == null)
            {
                noActiveWindow = true;
                return;
            }
            if (windowClone.transform.childCount <= 6)
            {
                noActiveWindow = true;
                return;
            }
            if(!windowClone.activeSelf)
            {
                noActiveWindow = true;
                return;
            }
            GameObject captionPanel = windowClone.transform.Find("Caption Panel").gameObject;
            GameObject priorityButton = captionPanel.transform.Find("Priority").gameObject;
            toggleButton = priorityButton.GetComponent<ToggleButton>();
            noActiveWindow = false;
        }

        // Set the windows placeholder array by getting all the child GameObjects of the windows placeholder GameObject
        public void setWindowsPlaceHolderArray()
        {
            // Create a new array to hold the GameObjects
            windowsPlaceHolderArray = new GameObject[windowsPlaceHolder.transform.childCount];

            // Loop through the transforms and add the corresponding GameObject to the array
            for (int i = 0; i < windowsPlaceHolder.transform.childCount; i++)
            {
                windowsPlaceHolderArray[i] = windowsPlaceHolder.transform.GetChild(i).gameObject;
            }
        }

        // Set the settings of the belt slot, such as its position, name, and visibility
        public void setBeltSlot_Settings(int location)
        {
            if(beltSlot == null)
            {
                setBeltSlot_Mappings();
            }
            if(beltSlot != null)
            {
                beltSlot.transform.SetSiblingIndex(location); // Set the belt slot to be the 4th item in the container list
                GameObject _headerPanel = beltSlot.transform.GetChild(0).gameObject; // Header panel of the belt slot
                GameObject _slotPanel = beltSlot.transform.GetChild(1).gameObject; // Slot panel of the belt slot
                GameObject _slotViewHeader = _headerPanel.transform.GetChild(1).gameObject; // Slot view header of the belt slot
                GameObject _slotName = _slotViewHeader.transform.GetChild(2).gameObject; // Slot name of the belt slot

                _slotPanel.SetActive(false); // Hide the slot panel
                _slotViewHeader.GetComponent<SlotViewHeader>().Interactable = false; // Make the slot view header not interactable
                _slotViewHeader.transform.GetChild(3).gameObject.SetActive(false); // Hide the arrow icon
                _slotName.GetComponent<TextMeshProUGUI>().text = "BELT"; // Set the slot name to "BELT"

                beltSlot.SetActive(true);
            }
        }

        // Finds the grid window clone, moves the grids to the belt slot, and hides the window clone
        public void setBeltSlotGrid()
        {
            setWindowsPlaceHolderArray();
            if((windowsPlaceHolderArray.Length) <= 15)
            {
                return;
            }
            GameObject windowClone = getGridWindowClone();
            if(windowClone == null)
            {
                return;
            }
            if (windowClone.transform.childCount <= 6)
            {
                return;
            }
            GameObject beltGrid = windowClone.transform.GetChild(6).gameObject; // Get the sixth child of the window clone, which is the grid window

            beltGrid.transform.parent = beltSlot?.transform; // Set the parent of the grid to the belt slot
            GridWindow gridWindow = windowClone.GetComponent<GridWindow>(); // Get the GridWindow component of the grid
            //gridWindow.method_2(false); // Disable the grid window

            windowClone.SetActive(false); // Hide the window clone
            disableGridWindow(); // Disable the grid window clone
        }

        public void disableGridWindow()
        {
            setWindowsPlaceHolderArray();
            if ((windowsPlaceHolderArray.Length) <= 15)
            {
                return;
            }
            GameObject gridWindowClone = getGridWindowClone();
            if (gridWindowClone == null)
            {
                return;
            }
            if(gridWindowClone.transform.childCount > 6)
            {
                return;
            }
            //if(Plugin.Instance != null)
            //{
            //    Plugin.Instance.widowedGridWindow = gridWindowClone; // Store the grid window clone in the plugin instance for later use
            //}
            GridWindow gridWindow = gridWindowClone.GetComponent<GridWindow>();
            gridWindow.method_2(false); // Disable the grid window
        }

        // Get the grid window clone from the windows placeholder array
        public GameObject getGridWindowClone()
        {
            setWindowsPlaceHolderArray();
            GameObject gridWindowClone; // Get the first child of the windows placeholder, which is the grid window template
            gridWindowClone = windowsPlaceHolderArray[windowsPlaceHolderArray.Length - 1];

            return gridWindowClone;
        }

        // Finds the original window of the belt grids
        public GameObject getDisabledWindowClone()
        {
            setWindowsPlaceHolderArray();
            GameObject gridWindowClone; // Get the first child of the windows placeholder, which is the grid window template
            gridWindowClone = windowsPlaceHolderArray[windowsPlaceHolderArray.Length - 1];
            if (!windowsPlaceHolderArray[windowsPlaceHolderArray.Length - 1].activeSelf)
            {
                gridWindowClone = windowsPlaceHolderArray[windowsPlaceHolderArray.Length - 1];
                return gridWindowClone;
            }
            else if(!windowsPlaceHolderArray[windowsPlaceHolderArray.Length - 2].activeSelf)
            {
                gridWindowClone= windowsPlaceHolderArray[windowsPlaceHolderArray.Length - 2];
                return gridWindowClone;
            }
            else if(!windowsPlaceHolderArray[windowsPlaceHolderArray.Length - 3].activeSelf)
            {
                gridWindowClone= windowsPlaceHolderArray[windowsPlaceHolderArray.Length - 3];
                return gridWindowClone;
            }
            else if(!windowsPlaceHolderArray[windowsPlaceHolderArray.Length - 4].activeSelf)
            {
                gridWindowClone= windowsPlaceHolderArray[windowsPlaceHolderArray.Length - 4];
                return gridWindowClone;
            }

            return gridWindowClone;
        }

        // Get the close button of the grid window
        public Button getCloseButton(GameObject button)
        {
            // Get caption panel
            GameObject captionPanel = button.transform.Find("Caption Panel").gameObject;

            // Get close button, no matter its position in the game object
            GameObject closeButton = captionPanel.transform.Find("Close Button").gameObject;
            return closeButton.GetComponent<Button>();
        }
    }
}
