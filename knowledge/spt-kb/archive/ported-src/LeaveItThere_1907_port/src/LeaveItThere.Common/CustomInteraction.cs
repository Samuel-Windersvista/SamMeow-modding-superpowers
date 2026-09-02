using System;
using System.Collections.Generic;
using System.Linq;
using EFT.UI;
using LeaveItThere.Components;
using LeaveItThere.Helpers;

namespace LeaveItThere.Common;

public abstract class CustomInteraction
{
	public class DisabledInteraction(string name) : CustomInteraction
	{
		public override string Name => name;

		public override bool Enabled => false;

		public override void OnInteract()
		{
		}
	}

	private static Action _refreshPrompt = InteractionHelper.RefreshPrompt;

	public FakeItem FakeItem { get; private set; }

	public virtual string TargetName => null;

	public virtual bool Enabled => true;

	public virtual bool AutoPromptRefresh => false;

	public abstract string Name { get; }

	public CustomInteraction()
	{
	}

	public CustomInteraction(FakeItem fakeItem)
	{
		FakeItem = fakeItem;
	}

	public abstract void OnInteract();

	public InteractionAction GetActionsTypesClass()
	{
		return new InteractionAction
		{
			Action = (AutoPromptRefresh ? ((Action)Delegate.Combine(new Action(OnInteract), _refreshPrompt)) : new Action(OnInteract)),
			Name = Name,
			Disabled = !Enabled,
			TargetName = TargetName
		};
	}

	public static List<InteractionAction> GetActionsTypesClassList(List<CustomInteraction> interactions)
	{
		List<InteractionAction> list = new List<InteractionAction>();
		string text = null;
		foreach (CustomInteraction interaction in interactions)
		{
			if (text == null && interaction.TargetName != null)
			{
				text = interaction.TargetName;
			}
			list.Add(interaction.GetActionsTypesClass());
		}
		if (text != null && list.Any())
		{
			list[0].TargetName = text;
		}
		return list;
	}
}
