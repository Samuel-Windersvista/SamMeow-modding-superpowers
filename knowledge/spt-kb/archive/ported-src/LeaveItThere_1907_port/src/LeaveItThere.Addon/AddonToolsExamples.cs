using EFT.Communications;
using LeaveItThere.Common;
using LeaveItThere.Components;
using UnityEngine;

namespace LeaveItThere.Addon;

internal class AddonToolsExamples
{
	public class MySimpleCustomInteraction : CustomInteraction
	{
		public override string Name => "My Simple Interaction";

		public override void OnInteract()
		{
			NotificationManager.DisplayMessageNotification("My Simple Interaction selected!", (ENotificationDurationType)0, (ENotificationIconType)0, (Color?)null);
		}
	}

	public void OnFakeItemInitialized(FakeItem fakeItem)
	{
		if (!(fakeItem.TemplateId != "590c657e86f77412b013051d"))
		{
			fakeItem.Interactions.Add(new MySimpleCustomInteraction());
		}
	}
}
