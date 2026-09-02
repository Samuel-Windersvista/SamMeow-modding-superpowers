using BeltSlot.Helpers;
using BeltSlot.Patches;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.Hideout;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using EFT.UI.Screens;
using HarmonyLib;
using SPT.Common.Utils;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static EFT.UI.TraderScreensGroup;

namespace BeltSlot
{
    [BepInPlugin("BeltSlot", "BeltSlot", "1.0.0")]
    [BepInDependency("com.SPT.core", "3.11.0")]
    [BepInDependency("com.aaaWTT-PacknStrap.Core", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        #region Variables
        private static FieldInfo _background = null;
        private Vector3 mousePosition = Vector3.zero;
        EEftScreenType previousScreenType = EEftScreenType.None;
        EEftScreenType eScreenType;
        CurrentScreenSingletonClass currentScreenSingletonClass = null;
        public Item? itemToTest;
        public string? itemId = "0000000000";
        public InventoryEquipment inventoryEquipment;
        public InventoryScreen inventoryScreen;
        private static UI_Mappings uiMappings;
        public bool beltToggle = true;
        bool windowToggle = true;
        public bool inventoryScreenLoaded = false;
        public static Plugin? Instance { get; private set; }
        internal static UI_Mappings UiMappings { get => uiMappings; set => uiMappings = value; }
        public static ManualLogSource? LogSource;
        private int beltSlotLocation = 3;
        private bool enableLogging = false;
        public bool packNStrapInstalled;
        public GameObject widowedGridWindow;
        #endregion

        #region Test Methods
        // Check if the input field is focused, if so, return true
        private bool isInputFieldFocused()
        {
            if (EventSystem.current.currentSelectedGameObject != null
            && EventSystem.current.currentSelectedGameObject.GetComponent<TMPro.TMP_InputField>() != null)
            {
                if (enableLogging)
                {
                    Logger.LogInfo("Text field in focus.");
                }
                return true;
            }
            return false;
        }

        // Check if the inventory screen is focused, if so, return true
        private bool isInventoryScreenFocus()
        {
            var inventoryScreen = Singleton<CommonUI>.Instance.InventoryScreen;
            if (inventoryScreen.isActiveAndEnabled)
            {
                if (enableLogging)
                {
                    Logger.LogInfo("Inventory screen is focused.");
                }
                return true;
            }
            return false;
        }

        // Get the current game world, if it is hideout, return true, otherwise return false
        private bool getCurrentGameWorld()
        {
            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld is HideoutGameWorld)
            {
                if (enableLogging)
                {
                    Logger.LogInfo("Game world is hideout");
                }
                return true;
            }
            if (gameWorld is ClientGameWorld)
            {
                if (enableLogging)
                {
                    Logger.LogInfo("Game world is main player");
                }
                return false;
            }
            return true;
        }

        // Check if the menutaskbar is active, if so check the current screen type
        private bool getButtonInteractable()
        {
            MenuTaskBar menuTaskBar = MonoBehaviourSingleton<PreloaderUI>.Instance.MenuTaskBar;

            if (menuTaskBar.isActiveAndEnabled)
            {
                eScreenType = getCurrentScreen();

                if (eScreenType == EEftScreenType.MainMenu || eScreenType == EEftScreenType.Inventory || eScreenType == EEftScreenType.Trader || eScreenType == EEftScreenType.Hideout || eScreenType == EEftScreenType.FleaMarket)
                {
                    return true;
                }
                return false;
            }
            return false;
        }

        // Check if the current scene is EftMainScene, if so, return false, otherwise return true
        private bool testScene()
        {
            string currentScene = getCurrentScene();
            if (currentScene == "Unkown")
            {
                return false;
            }

            // Removed (currentScene == "EftMainScene") check, see what this effects
            if (currentScene == "LoginUIScene")
            {
                return false;
            }
            return true;
        }

        // Check if the current scene is a raid scene, if so, return true, otherwise return false
        private bool testInRaidScene()
        {
            string _currentScene = getCurrentScene();
            switch(_currentScene)
            {
                case "Factory_Rework_Day_Scripts":
                case "Factory_Rework_Night_Scripts":
                case "Sandbox_Scripts":
                case "City_Scripts":
                case "Shopping_Mall_Scripts":
                case "custom_Scripts":
                case "woods_Scripts":
                case "Reserve_Base_Scripts":
                case "Lighthouse_Scripts":
                case "shoreline_scripts":
                case "Laboratory_Scripts":
                    return true;
                default: return false;
            }
                
            /*if(_currentScene == "Factory_Rework_Day_Scripts" || _currentScene == "Factory_Rework_Night_Scripts")
            {
                // Current scene is Factory
                return true;
            }
            else if(_currentScene == "Sandbox_Scripts")
            {
                // Current scene is Ground Zero
                return true;
            }
            else if(_currentScene == "City_Scripts")
            {
                // Current scene is Streets of Tarkov
                return true;
            }
            else if(_currentScene == "Shopping_Mall_Scripts")
            {
                // Current scene is Shopping Mall
                return true;
            }
            else if(_currentScene == "custom_Scripts")
            {
                // Current scene is Customs
                return true;
            }
            else if(_currentScene == "woods_Scripts")
            {
                // Current scene is Woods
                return true;
            }
            else if(_currentScene == "Reserve_Base_Scripts")
            {
                // Current scene is Reserve
                return true;
            }
            else if(_currentScene == "Lighthouse_Scripts")
            {
                // Current scene is Lighthouse
                return true;
            }
            else if(_currentScene == "shoreline_scripts")
            {
                // Current scene is Shoreline
                return true;
            }
            else if(_currentScene == "Laboratory_Scripts")
            {
                // Current scene is Laboratory
                return true;
            }
            else
            {
                return false;
            }*/
        }

        // Check if screen has changed
        private bool testScreenChange()
        {
            if (previousScreenType == getCurrentScreen())
            {
                return false;
            }
            return true;
        }

        // Check if the hideout loading screen is active, if so, return false, otherwise return true
        private bool getHideoutLoading()
        {
            var _hideoutLoadingScreen = MonoBehaviourSingleton<PreloaderUI>.Instance.HideoutLoadingScreen;

            if (_hideoutLoadingScreen == null)
            {
                return true;
            }

            if (!_hideoutLoadingScreen.isActiveAndEnabled)
            {
                if (enableLogging)
                {
                    Logger.LogInfo($"loading screen is not active");
                }
                return true;
            }

            Type type = typeof(HideoutLoadingScreen);
            if (_background == null)
            {
                _background = AccessTools.Field(type, "_background");
            }

            Image background = (Image)_background.GetValue(_hideoutLoadingScreen);

            if (background.enabled)
            {
                return false;
            }
            return true;
        }

        // Check current screen type
        private EEftScreenType getCurrentScreen()
        {
            if (currentScreenSingletonClass == null)
            {
                currentScreenSingletonClass = CurrentScreenSingletonClass.Instance;
            }

            MenuTaskBar menuTaskBar = MonoBehaviourSingleton<PreloaderUI>.Instance.MenuTaskBar;

            if (menuTaskBar.isActiveAndEnabled)
            {
                EEftScreenType _eScreenType = currentScreenSingletonClass.CurrentScreenController.ScreenType;

                if (enableLogging)
                {
                    Logger.LogInfo($"Current screen type: {_eScreenType}");
                }
                return _eScreenType;
            }
            else
            {
                if (enableLogging)
                {
                    Logger.LogInfo("MenuTaskBar is not active, returning None");
                }
                return EEftScreenType.None;
            }
        }

        // Check current scene
        private string getCurrentScene()
        {
            string _currentScene = SceneManager.GetActiveScene().name;
            if (_currentScene == null || _currentScene == string.Empty)
            {
                if (enableLogging)
                {
                    Logger.LogInfo("Current scene is null or empty");
                }
                return "Unknown";
            }

            if (enableLogging)
            {
                Logger.LogInfo($"Current scene: {_currentScene}");
            }

            return _currentScene;
        }

        // Test the ArmBand slot, if it has a compound item, return true, otherwise return false
        private bool TestArmBand()
        {
            if (!testScene() && !testInRaidScene())
            {
                return false;
            }
            if (getCurrentScene() != "CommonUIScene" && getCurrentScene() != "MenuUIScene" && !testInRaidScene() )
            {
                return false;
            }
            if (!getHideoutLoading())
            {
                return false;
            }
            // do not trigger if inventory screen is not focused or input field is focused
            if (isInventoryScreenFocus() && !isInputFieldFocused())
            {
                if (inventoryEquipment != null)
                {
                    if(uiMappings?.armBandSlot == null)
                    {
                        uiMappings?.setEquipment_Mappings();
                    }
                    Slot slot = inventoryEquipment.GetSlot(EquipmentSlot.ArmBand);
                    if(slot.Items.IsNullOrEmpty())
                    {
                        itemId = "0000000000"; // Reset the item ID
                        if(enableLogging)
                        {
                            LogSource?.LogInfo("No item in ArmBand slot");
                        }
                        return false;
                    }
                    Item item = slot.ContainedItem;
                    if(!item.IsContainer)
                    {
                        itemId = "0000000000"; // Reset the item ID
                        if (enableLogging)
                        {
                            LogSource?.LogInfo("Item in ArmBand slot is not a compound item: " + item.Id);
                        }
                        return false;
                    }
                    else
                    {
                        if (enableLogging)
                        {
                            LogSource?.LogInfo("Item in ArmBand slot is a compound item: " + item.Id);
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        // Test the item in the ArmBand slot, if it has changed, return true, otherwise return false
        private bool TestItem(String? item)
        {
            if (!testScene() && !testInRaidScene())
            {
                return false;
            }
            if (getCurrentScene() != "CommonUIScene" && getCurrentScene() != "MenuUIScene" && !testInRaidScene() )
            {
                return false;
            }
            if (!getHideoutLoading())
            {
                return false;
            }
            // do not trigger if inventory screen is not focused or input field is focused
            if (isInventoryScreenFocus() && !isInputFieldFocused())
            {
                if(TestArmBand())
                {
                    string? itemTest = item;
                    Slot? slot = inventoryEquipment?.GetSlot(EquipmentSlot.ArmBand);
                    /*if(itemTest == null)
                    {
                        if(enableLogging)
                        {
                            LogSource?.LogInfo("No item in ArmBand slot, either first start or ");
                        }
                        itemToTest = slot?.ContainedItem;
                        return true;
                    }*/
                    if(itemTest != slot?.ContainedItem.Id)
                    {
                        if(enableLogging)
                        {
                            LogSource?.LogInfo("Item in ArmBand slot has changed, updating itemToTest");
                        }
                        itemToTest = slot?.ContainedItem;
                        itemId = slot?.ContainedItem.Id;
                        return true;
                    }
                }
                return false;
            }
            return false;
        }

        // Test if the belt slot has a grid, if so, return true, otherwise return false
        private bool TestBeltHasGrid()
        {
            if (!testScene() && !testInRaidScene())
            {
                return false;
            }
            if (getCurrentScene() != "CommonUIScene" && getCurrentScene() != "MenuUIScene" && !testInRaidScene() )
            {
                return false;
            }
            if (!getHideoutLoading())
            {
                return false;
            }
            // do not trigger if inventory screen is not focused or input field is focused
            if (isInventoryScreenFocus() && !isInputFieldFocused())
            {
                if(UiMappings?.beltSlot?.transform.childCount >= 6)
                {
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region Get Methods
        // Get Tarkov application instance, if it exists, otherwise return null
        // not currrenlty used, but might be useful in the future
        private TarkovApplication? getTarkovApplication()
        {
            // OH MY GOD THIS WORKS!!!!
            // It took me two days to figure this out
            TarkovApplication _tarkovApplication;
            if (TarkovApplication.Exist(out _tarkovApplication))

                if (_tarkovApplication == null)
                {
                    if (enableLogging)
                    {
                        Logger.LogInfo("Tarkov application is null");
                    }
                    Logger.LogInfo("Tarkov application is null");
                    return null;
                }
            return _tarkovApplication;
        }

        // Get the custom belt slot location
        private int setBeltSlotLocation()
        {
            if(Settings.BeltSlotLocation.Value == BeltSlotLocationOption.AbovePockets)
            {
                beltSlotLocation = 4;
            }
            else if(Settings.BeltSlotLocation.Value == BeltSlotLocationOption.BelowPockets)
            {
                beltSlotLocation = 5;
            }
            return beltSlotLocation;
        }

        // Check for modifier key
        private static bool setModifierKey()
        {
            return Settings.ModifierKey.Value switch
            {
                ModifierKeyOptions.Alt => Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt),
                _ => false,
            };
        }
        #endregion

        // BaseUnityPlugin inherits MonoBehaviour, so you can use base unity functions like Awake() and Update()
        private void Awake()
        {
            packNStrapInstalled = Chainloader.PluginInfos.Keys.Contains("com.aaaWTT-PacknStrap.Core");
            Settings.Init(Config);
            Plugin.Instance = this;
            UiMappings = new UI_Mappings();
            // save the Logger to variable so we can use it elsewhere in the project
            LogSource = Logger;
            LogSource.LogInfo("plugin loaded!");

            // Currently just used to get an instance of the inventory equipment screen
            new InventoryEquipmentPatch().Enable();
            new EquipmentTabPatch().Enable();
            //new ItemUiContextPatch().Enable();

            // Enables the correct patch based on if PackNStrap is installed or not
            if (packNStrapInstalled)
            {
                new GetPrioritizedContainersPatch().Disable();
                new GetPrioritizedContainersPackNStrapPatch().Enable();
            }
            else
            {
                new GetPrioritizedContainersPackNStrapPatch().Disable();
                new GetPrioritizedContainersPatch().Enable();
            }
            //new manualprioritywindowpatch().enable();
            //new quickmovetocontainersoverridepatch().enable();
        }

        // Using lateupdate because hoping it would fix issues with the belt grid not opening when it should
        // not sure if it worked
        private void LateUpdate()
        {
            if (!Singleton<CommonUI>.Instantiated)
            {
                return;
            }
            if(!Singleton<PreloaderUI>.Instantiated)
            {
                return;
            }

            if(!inventoryScreenLoaded)
            {
                return;
            }

            // Checks for input for toggle buttons
            HandleInput();

            // Checks if inventory open and sets the inventoryEquipment variable
            OnEnterInventory();

            // Handles updating the armband slot variables and sets the belt slot
            UpdateArmbandSlot();

            // Handles clearing the belt grid if the toggle is off
            ClearBeltGrid();

            // Sets custom window priority
            setWindowPriority(windowToggle, null);

            clearWidowedGridWindow();

            // Not currenlty working, figuring it out
            //if(Input.GetKeyDown(KeyCode.X))
            //{
            //    GameObject windowClone = uiMappings.getDisabledWindowClone();
            //    setWindowPriority(true, windowClone);
            //}

            // Test for current screen and send log message, for debuging purposes
            /*if (Input.GetKeyDown(KeyCode.O))
            {
                string _currentScene = getCurrentScene();
                //if(enableLogging)
                //{
                    LogSource?.LogInfo($"Current scene: {_currentScene}");
                //}
                EEftScreenType _currentScreen = getCurrentScreen();
                //if (enableLogging)
                //{
                    LogSource?.LogInfo($"Current screen type: {_currentScreen}");
                //}

            }*/
        }

        #region Methods
        // Handles toggle keys
        private void HandleInput()
        {
            if(setModifierKey() && Settings.ModifierKeyToggle.Value)
            {
                // Auto window priority toggle
                if (Input.GetKeyDown(Settings.PriorityToggleKey.Value.MainKey))
                {
                    if (!testScene() && !testInRaidScene())
                    {
                        return;
                    }
                    if (getCurrentScene() != "CommonUIScene" && getCurrentScene() != "MenuUIScene" && !testInRaidScene())
                    {
                        return;
                    }
                    if (!getHideoutLoading())
                    {
                        return;
                    }
                    if (!Settings.AutoWindowPriority.Value)
                    {
                        return;
                    }
                    if (isInventoryScreenFocus() && !isInputFieldFocused())
                    {
                        windowToggle = !windowToggle;
                    }
                }
                // Belt toggle
                if (Input.GetKeyDown(Settings.BeltToggleKey.Value.MainKey))
                {
                    if (!testScene() && !testInRaidScene())
                    {
                        return;
                    }
                    if (getCurrentScene() != "CommonUIScene" && getCurrentScene() != "MenuUIScene" && !testInRaidScene())
                    {
                        return;
                    }
                    if (!getHideoutLoading())
                    {
                        return;
                    }
                    if (!Settings.EnableBeltToggle.Value)
                    {
                        return;
                    }
                    if (isInventoryScreenFocus() && !isInputFieldFocused())
                    {
                        beltToggle = !beltToggle;
                    }
                }
            }
            else if(!Settings.ModifierKeyToggle.Value)
            {
                // Auto window priority toggle
                if (Input.GetKeyDown(Settings.PriorityToggleKey.Value.MainKey))
                {
                    if (!testScene() && !testInRaidScene())
                    {
                        return;
                    }
                    if (getCurrentScene() != "CommonUIScene" && getCurrentScene() != "MenuUIScene" && !testInRaidScene())
                    {
                        return;
                    }
                    if (!getHideoutLoading())
                    {
                        return;
                    }
                    if(!Settings.AutoWindowPriority.Value)
                    {
                        return;
                    }
                    if (isInventoryScreenFocus() && !isInputFieldFocused())
                    {
                        windowToggle = !windowToggle;
                    }
                }
                // Belt toggle
                if (Input.GetKeyDown(Settings.BeltToggleKey.Value.MainKey))
                {
                    if (!testScene() && !testInRaidScene())
                    {
                        return;
                    }
                    if (getCurrentScene() != "CommonUIScene" && getCurrentScene() != "MenuUIScene" && !testInRaidScene())
                    {
                        return;
                    }
                    if (!getHideoutLoading())
                    {
                        return;
                    }
                    if(!Settings.EnableBeltToggle.Value)
                    {
                        return;
                    }
                    if (isInventoryScreenFocus() && !isInputFieldFocused())
                    {
                        beltToggle = !beltToggle;
                    }
                }
            }

        }

        // Handles custom windows priority
        private void setWindowPriority(bool isOn, GameObject? target)
        {
            if (!testScene() && !testInRaidScene())
            {
                return;
            }
            if (getCurrentScene() != "CommonUIScene" && getCurrentScene() != "MenuUIScene" && !testInRaidScene())
            {
                return;
            }
            if (!getHideoutLoading())
            {
                return;
            }
            if(!Settings.AutoWindowPriority.Value)
            {
                return;
            }
            // do not trigger if inventory screen is not focused or input field is focused
            if (isInventoryScreenFocus() && !isInputFieldFocused())
            {
                uiMappings.setWindowPriorityButton(target);
                if(!UiMappings.noActiveWindow)
                {
                    uiMappings.toggleButton.IsOn = isOn;
                }
            }
        }

        private void clearWidowedGridWindow()
        {
            if (!testScene() && !testInRaidScene())
            {
                return;
            }
            if (getCurrentScene() != "CommonUIScene" && getCurrentScene() != "MenuUIScene" && !testInRaidScene())
            {
                return;
            }
            if (!getHideoutLoading())
            {
                return;
            }
            // do not trigger if inventory screen is not focused or input field is focused
            if (isInventoryScreenFocus() && !isInputFieldFocused())
            {
                if (TestBeltHasGrid())
                {
                    uiMappings.disableGridWindow();
                }
            }
        }

        // Open the belt slot if the inventory is open and the armband slot has a compound item
        // also sets the uiMappings if they are null
        private void OnEnterInventory()
        {
            if (!testScene() && !testInRaidScene())
            {
                return;
            }
            if (getCurrentScene() != "CommonUIScene" && getCurrentScene() != "MenuUIScene" && !testInRaidScene())
            {
                return;
            }
            if (!getHideoutLoading())
            {
                return;
            }
            // do not trigger if inventory screen is not focused or input field is focused
            if (isInventoryScreenFocus() && !isInputFieldFocused())
            {
                if (UiMappings != null)
                {
                    if (UiMappings.beltSlot == null)
                    {
                        UiMappings.setContainer_Mappings();
                        UiMappings.setBeltSlot_Settings(setBeltSlotLocation());
                        UiMappings.setPrelaoderUI_Mappings();
                        //await pauseWait(2000); // Wait for 100 milliseconds to ensure the UI is ready
                        if(beltToggle)
                        {
                            if (!TestBeltHasGrid())
                            {
                                if (TestArmBand())
                                {
                                    OpenBelt(1000);
                                    //await pauseWait(100); // Wait for 100 milliseconds to ensure the UI is ready
                                    Slot? slot = inventoryEquipment?.GetSlot(EquipmentSlot.ArmBand);
                                    itemToTest = slot?.ContainedItem;
                                    itemId = slot?.ContainedItem.Id;
                                    return;
                                }
                            }
                        }
                    }
                    else if(UiMappings.beltSlot.transform.GetSiblingIndex() != setBeltSlotLocation())
                    {
                        UiMappings.setBeltSlot_Settings(setBeltSlotLocation());
                        if (enableLogging)
                        {
                            Logger.LogInfo("belt slot in the wrong place, fixing");
                        }

                        //await pauseWait(100); // Wait for 100 milliseconds to ensure the UI is ready
                        if(beltToggle)
                        {
                            if(!TestBeltHasGrid())
                            {
                                if (TestArmBand())
                                {
                                    OpenBelt(10);
                                    //await pauseWait(100); // Wait for 100 milliseconds to ensure the UI is ready
                                    Slot? slot = inventoryEquipment?.GetSlot(EquipmentSlot.ArmBand);
                                    itemToTest = slot?.ContainedItem;
                                    itemId = slot?.ContainedItem.Id;
                                    return;
                                }
                            }
                        }
                    }
                    if (enableLogging)
                    {
                        Logger?.LogInfo("belt slot in the right place");
                    }
                    return;
                }
            }
        }

        // Updates the armband slot and opens the belt slot if the item in the armband slot has changed
        private void UpdateArmbandSlot()
        {
            if (!testScene() && !testInRaidScene())
            {
                return;
            }
            if (getCurrentScene() != "CommonUIScene" && getCurrentScene() != "MenuUIScene" && !testInRaidScene())
            {
                return;
            }
            if (!getHideoutLoading())
            {
                return;
            }
            // do not trigger if inventory screen is not focused or input field is focused
            if (isInventoryScreenFocus() && !isInputFieldFocused())
            {
                if (UiMappings?.beltSlot == null)
                {
                    UiMappings?.setContainer_Mappings();
                    UiMappings?.setBeltSlot_Settings(setBeltSlotLocation());
                    UiMappings?.setPrelaoderUI_Mappings();
                }
                //await pauseWait(100); // Wait for 100 milliseconds to ensure the UI is ready
                if (beltToggle)
                {
                    //setWindowPriority(true);
                    if(TestItem(itemId))
                    {
                        if (!TestBeltHasGrid())
                        {
                            OpenBelt(10);
                        }
                    }
                }
                RefreshBeltSlot();
            }
        }

        // Refreshes the belt slot if the item in the armband slot has not changed but the belt slot is not open
        private void RefreshBeltSlot()
        {
            if (!testScene() && !testInRaidScene())
            {
                return;
            }
            if (getCurrentScene() != "CommonUIScene" && getCurrentScene() != "MenuUIScene" && !testInRaidScene() )
            {
                return;
            }
            if (!getHideoutLoading())
            {
                return;
            }
            // do not trigger if inventory screen is not focused or input field is focused
            if (isInventoryScreenFocus() && !isInputFieldFocused())
            {
                //setWindowPriority(true);
                if(beltToggle)
                {
                    if (TestArmBand())
                    {
                        if (!TestBeltHasGrid())
                        {
                            OpenBelt(10);
                        }
                    }
                }
            }
            return;
        }

        // Opens the belt slot by simulating a click on the armband slot
        private void OpenBelt(int wait)
        {

            Vector2 mousePosition = Input.mousePosition;

            // This bit of code finally does what I was wanting to do, it opens whatever item is in the ArmBand slot
            if(uiMappings.armBandSlot.transform.childCount <= 8)
            {
                return;
            }
            GameObject armBandClone = uiMappings.armBandSlot.transform.GetChild(8).gameObject;
            SlotItemView slotItemView = armBandClone.GetComponent<SlotItemView>();
            slotItemView.OnClick(PointerEventData.InputButton.Left, mousePosition, true);
            slotItemView.OnPointerExit(new PointerEventData(EventSystem.current));

            setWindowPriority(false, null);

            UiMappings.setBeltSlotGrid();
            return;
            // Code above is equivalent to clicking the ArmBand slot with the mouse, which opens the item in that slot
        }

        // Clears the belt grid if the toggle is off
        private void ClearBeltGrid()
        {
            if(!beltToggle)
            {
                if(TestBeltHasGrid())
                {
                    CloseBelt();
                }
            }
        }

        // Closes the belt slot by simulating a click on the close button
        private void CloseBelt()
        {
            itemToTest = null; // Clear the item to test
            itemId = "0000000000"; // Reset the item ID
            GameObject beltGrids = UiMappings.beltSlot.transform.GetChild(5).gameObject;
            GameObject windowClone = uiMappings.getDisabledWindowClone();
            beltGrids.transform.parent = windowClone.transform;
            windowClone.SetActive(true);

            uiMappings.getCloseButton(windowClone).OnPointerClick(new PointerEventData(EventSystem.current));
            
        }
        #endregion
    }
}
