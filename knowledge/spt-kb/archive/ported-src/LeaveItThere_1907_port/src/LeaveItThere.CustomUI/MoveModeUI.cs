using System;
using System.Collections.Generic;
using System.Linq;
using LeaveItThere.Helpers;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace LeaveItThere.CustomUI;

internal class MoveModeUI : MonoBehaviour
{
	public enum ERecolorTarget
	{
		Base,
		Highlight,
		Pressed
	}

	public enum ETabType
	{
		None = -1,
		Position,
		Rotation,
		Physics
	}

	public enum ESpaceReference
	{
		Player,
		Item,
		World
	}

	public delegate void TabSwitchedHandler(ETabType tabType);

	public class PhysicsTab : MenuTab
	{
		public Toggle ItemFloats;

		public PhysicsTab(MoveModeUI ui)
		{
			TabButton = ((Component)((Component)ui).gameObject.transform.Find("TabGroup/TabButtons/PhysicsTab")).gameObject.GetComponent<Button>();
			Content = ((Component)((Component)ui).gameObject.transform.Find("TabGroup/PhysicsContent")).gameObject;
			GameObject gameObject = ((Component)Content.transform.Find("VContainer")).gameObject;
			ItemFloats = ((Component)gameObject.transform.Find("ItemFloats")).gameObject.GetComponent<Toggle>();
			SelectablesOnTab = new List<Selectable>(1) { (Selectable)(object)ItemFloats };
			Init();
		}
	}

	public class RotationTab : MenuTab
	{
		public TMP_Dropdown RotateRelativeTo;

		public Toggle LockX;

		public Toggle LockY;

		public Toggle LockZ;

		public Button ResetRotationButton;

		public Button UndoRotationButton;

		public RotationTab(MoveModeUI ui)
		{
			TabButton = ((Component)((Component)ui).gameObject.transform.Find("TabGroup/TabButtons/RotationTab")).gameObject.GetComponent<Button>();
			Content = ((Component)((Component)ui).gameObject.transform.Find("TabGroup/RotationContent")).gameObject;
			GameObject gameObject = ((Component)Content.transform.Find("VContainer")).gameObject;
			RotateRelativeTo = ((Component)gameObject.transform.Find("RotateRelativeTo")).gameObject.GetComponent<TMP_Dropdown>();
			LockX = ((Component)gameObject.transform.Find("LockX")).gameObject.GetComponent<Toggle>();
			LockY = ((Component)gameObject.transform.Find("LockY")).gameObject.GetComponent<Toggle>();
			LockZ = ((Component)gameObject.transform.Find("LockZ")).gameObject.GetComponent<Toggle>();
			ResetRotationButton = ((Component)gameObject.transform.Find("ResetRotationButton")).gameObject.GetComponent<Button>();
			UndoRotationButton = ((Component)gameObject.transform.Find("UndoRotationButton")).gameObject.GetComponent<Button>();
			SelectablesOnTab = new List<Selectable>(6)
			{
				(Selectable)(object)RotateRelativeTo,
				(Selectable)(object)LockX,
				(Selectable)(object)LockY,
				(Selectable)(object)LockZ,
				(Selectable)(object)ResetRotationButton,
				(Selectable)(object)UndoRotationButton
			};
			Init();
		}
	}

	public class PositionTab : MenuTab
	{
		public TMP_Dropdown MoveRelativeTo;

		public Toggle LockX;

		public Toggle LockY;

		public Toggle LockZ;

		public Button MoveToPlayerButton;

		public Button UndoMoveButton;

		public PositionTab(MoveModeUI ui)
		{
			TabButton = ((Component)((Component)ui).gameObject.transform.Find("TabGroup/TabButtons/PositionTab")).gameObject.GetComponent<Button>();
			Content = ((Component)((Component)ui).gameObject.transform.Find("TabGroup/PositionContent")).gameObject;
			GameObject gameObject = ((Component)Content.transform.Find("VContainer")).gameObject;
			MoveRelativeTo = ((Component)gameObject.transform.Find("MoveRelativeTo")).gameObject.GetComponent<TMP_Dropdown>();
			LockX = ((Component)gameObject.transform.Find("LockX")).gameObject.GetComponent<Toggle>();
			LockY = ((Component)gameObject.transform.Find("LockY")).gameObject.GetComponent<Toggle>();
			LockZ = ((Component)gameObject.transform.Find("LockZ")).gameObject.GetComponent<Toggle>();
			MoveToPlayerButton = ((Component)gameObject.transform.Find("MoveToPlayerButton")).gameObject.GetComponent<Button>();
			UndoMoveButton = ((Component)gameObject.transform.Find("UndoMoveButton")).gameObject.GetComponent<Button>();
			SelectablesOnTab = new List<Selectable>(6)
			{
				(Selectable)(object)MoveRelativeTo,
				(Selectable)(object)LockX,
				(Selectable)(object)LockY,
				(Selectable)(object)LockZ,
				(Selectable)(object)MoveToPlayerButton,
				(Selectable)(object)UndoMoveButton
			};
			Init();
		}
	}

	public abstract class MenuTab
	{
		public delegate void TabButtonClickedHandler(MenuTab tab);

		public GameObject Content;

		public Button TabButton;

		public List<Selectable> SelectablesOnTab = new List<Selectable>();

		public event TabButtonClickedHandler TabButtonClicked;

		public void Init()
		{
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Expected O, but got Unknown
			((UnityEvent)TabButton.onClick).AddListener(new UnityAction(ButtonClicked));
		}

		private void ButtonClicked()
		{
			this.TabButtonClicked?.Invoke(this);
		}

		public void Activate()
		{
			Content.SetActive(true);
			Instance.SelectedTab = this;
		}

		public void Deactivate()
		{
			Content.SetActive(false);
		}
	}

	private static MoveModeUI _instance;

	public MenuTab SelectedTab;

	public Canvas Canvas;

	public Image Background;

	public PositionTab PosTab;

	public RotationTab RotTab;

	public PhysicsTab PhysTab;

	public List<MenuTab> AllMenuTabs;

	public RectTransform MenuRect;

	public Button DragWindowButton;

	public Button SaveButton;

	public Button CancelButton;

	public static MoveModeUI Instance
	{
		get
		{
			if ((UnityEngine.Object)(object)_instance == (UnityEngine.Object)null)
			{
				_instance = UnityEngine.Object.Instantiate<GameObject>(BundleThings.MoveModeUIPrefab).AddComponent<MoveModeUI>();
			}
			return _instance;
		}
	}

	public ESpaceReference RepositionReference
	{
		get
		{
			if (PosTab.MoveRelativeTo.value == 0)
			{
				return ESpaceReference.Player;
			}
			if (PosTab.MoveRelativeTo.value == 1)
			{
				return ESpaceReference.Item;
			}
			return ESpaceReference.World;
		}
	}

	public ESpaceReference RotationReference
	{
		get
		{
			if (RotTab.RotateRelativeTo.value == 0)
			{
				return ESpaceReference.Player;
			}
			if (RotTab.RotateRelativeTo.value == 1)
			{
				return ESpaceReference.Item;
			}
			return ESpaceReference.World;
		}
	}

	public ETabType SelectedTabType
	{
		get
		{
			if (SelectedTab is PositionTab)
			{
				return ETabType.Position;
			}
			if (SelectedTab is RotationTab)
			{
				return ETabType.Rotation;
			}
			if (SelectedTab is PhysicsTab)
			{
				return ETabType.Physics;
			}
			return ETabType.None;
		}
	}

	public bool IsActive => ((Component)this).gameObject.activeSelf;

	public event TabSwitchedHandler TabSwitched;

	private MoveModeUI()
	{
		PosTab = new PositionTab(this);
		RotTab = new RotationTab(this);
		PhysTab = new PhysicsTab(this);
		AllMenuTabs = new List<MenuTab>(3) { PosTab, RotTab, PhysTab };
		DragWindowButton = ((Component)((Component)this).gameObject.transform.Find("TabGroup/StaticButtons/Drag")).gameObject.GetComponent<Button>();
		MenuRect = RectTransformExtensions.RectTransform(((Component)((Component)this).gameObject.transform.Find("TabGroup")).gameObject);
		((Component)DragWindowButton).gameObject.AddComponent<ButtonDrag>().Init(MenuRect);
		Canvas = ((Component)this).gameObject.GetComponent<Canvas>();
		Background = ((Component)((Component)this).gameObject.transform.Find("TabGroup/bg")).gameObject.GetComponent<Image>();
		SaveButton = ((Component)((Component)this).gameObject.transform.Find("TabGroup/ExitButtons/SaveButton")).gameObject.GetComponent<Button>();
		CancelButton = ((Component)((Component)this).gameObject.transform.Find("TabGroup/ExitButtons/CancelButton")).gameObject.GetComponent<Button>();
		((Component)this).gameObject.SetActive(false);
	}

	private void Awake()
	{
		PosTab.Activate();
		PosTab.TabButtonClicked += OnTabButtonClicked;
		RotTab.TabButtonClicked += OnTabButtonClicked;
		PhysTab.TabButtonClicked += OnTabButtonClicked;
	}

	private void OnEnable()
	{
		UIColorHelper.RefreshColors();
	}

	public void OnTabButtonClicked(MenuTab tab)
	{
		ChangeTabs(tab.GetType());
	}

	public void ChangeTabs<TNewTab>() where TNewTab : MenuTab
	{
		ChangeTabs(typeof(TNewTab));
	}

	public void ChangeTabs(Type tabType)
	{
		if (!(SelectedTab.GetType() == tabType))
		{
			SelectedTab.Deactivate();
			AllMenuTabs.First((MenuTab t) => t.GetType() == tabType).Activate();
			this.TabSwitched?.Invoke(SelectedTabType);
			UIColorHelper.RefreshColors();
		}
	}

	public void ToggleActive()
	{
		SetActive(!IsActive);
	}

	public void SetActive(bool isActive)
	{
		((Component)this).gameObject.SetActive(isActive);
	}
}
