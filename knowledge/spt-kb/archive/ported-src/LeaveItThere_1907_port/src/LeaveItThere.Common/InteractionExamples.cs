using EFT;
using EFT.Communications;
using EFT.Interactive;
using LeaveItThere.Addon;
using LeaveItThere.Components;
using UnityEngine;

namespace LeaveItThere.Common;

public class InteractionExamples
{
	public class SuperSimpleExample : CustomInteraction
	{
		public override string Name => "My Interaction Name";

		public SuperSimpleExample(FakeItem fakeItem)
			: base(fakeItem)
		{
		}

		public override void OnInteract()
		{
			NotificationManager.DisplayMessageNotification("My FakeItem's name is: " + ((LootItem)base.FakeItem.LootItem).Name.Localized((string)null), (ENotificationDurationType)0, (ENotificationIconType)0, (Color?)null);
		}
	}

	public class ExampleThatRunsCodeOnceWhenCreated : CustomInteraction
	{
		public class SomeClass
		{
			public string Things = "Stuff";
		}

		private SomeClass _someClass;

		public override string Name => "Tell Me Length Of My Name";

		public ExampleThatRunsCodeOnceWhenCreated(FakeItem fakeItem)
			: base(fakeItem)
		{
			_someClass = new SomeClass();
		}

		public override void OnInteract()
		{
			NotificationManager.DisplayMessageNotification(_someClass.Things, (ENotificationDurationType)0, (ENotificationIconType)0, (Color?)null);
		}
	}

	public class AllTheOtherThingsExample : CustomInteraction
	{
		public override string Name => "My Name";

		public override bool Enabled => false;

		public override bool AutoPromptRefresh => true;

		public override string TargetName => GetString();

		public override void OnInteract()
		{
			NotificationManager.DisplayMessageNotification("Interaction Selected!", (ENotificationDurationType)0, (ENotificationIconType)0, (Color?)null);
		}

		public string GetString()
		{
			return "A cool string";
		}
	}

	public class HowToUse
	{
		private void PluginAwakeFunctionOrSimilar()
		{
			LITStaticEvents.OnFakeItemInitialized += OnFakeItemInitialized;
		}

		public void OnFakeItemInitialized(FakeItem fakeItem)
		{
			if (!(fakeItem.TemplateId != "the item id I am targeting"))
			{
				fakeItem.Interactions.Add(new SuperSimpleExample(fakeItem));
				fakeItem.Interactions.Add(new ExampleThatRunsCodeOnceWhenCreated(fakeItem));
				fakeItem.Interactions.Add(new AllTheOtherThingsExample());
			}
		}
	}
}
