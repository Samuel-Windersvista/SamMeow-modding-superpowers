using EFT.UI;
using LockableDoors.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LockableDoors.Models
{
    public abstract class CustomInteraction
    {

        public CustomInteraction() { }

        /// <summary>
        /// Text that shows under the interaction prompt. Only works if it is on the first interaction in the list. GetActionsTypesClassList finds the first TargetName and applies it to the first interaction, but if you are using GetActionsTypesClass you'll have to manually ensure that the first interaction has the desired TargetName set.
        /// </summary>
        public virtual string TargetName  => null;

        /// <summary>
        /// If returns false, interaction prompt will be greyed out and not selectable.
        /// </summary>
        public virtual bool Enabled => true;

        /// <summary>
        /// If returns true, the interaction will auto refresh after Action() is called. This will update any names or states of the interaction, but will also reset the selected action to the first one.
        /// </summary>
        public virtual bool AutoPromptRefresh => false;

        /// <summary>
        /// Name of interaction item in prompt.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Interaction callback. Will be called when an interaction is interacted with.
        /// </summary>
        public abstract void OnInteract();

        private static Action _refreshPrompt = new(RefreshPrompt);

        private static void RefreshPrompt()
        {
            LDSession.Instance.GamePlayerOwner.ClearInteractionState();

            try
            {
                LDSession.Instance.GamePlayerOwner.InteractionsChangedHandler();
            }
            catch (Exception) { } // sometimes this causes errors, don't really care in those cases so just avoid the exception
        }

        public InteractionAction ActionsTypesClass
        {
            get
            {
                InteractionAction typesClass = new()
                {
                    Action = AutoPromptRefresh ? OnInteract + _refreshPrompt : OnInteract,
                    Name = Name,
                    Disabled = !Enabled
                };

                typesClass.TargetName = TargetName;

                return typesClass;
            }
        }

        public static List<InteractionAction> GetActionsTypesClassList(List<CustomInteraction> interactions)
        {
            List<InteractionAction> actionsTypesClassList = [];
            string targetName = null;

            foreach (CustomInteraction interaction in interactions)
            {
                if (targetName == null && interaction.TargetName != null)
                {
                    targetName = interaction.TargetName;
                }

                actionsTypesClassList.Add(interaction.ActionsTypesClass);
            }

            if (targetName != null && actionsTypesClassList.Any())
            {
                actionsTypesClassList[0].TargetName = targetName;
            }

            return actionsTypesClassList;
        }

        public class DisabledInteraction(string name) : CustomInteraction
        {
            public override string Name => name;
            public override bool Enabled => false;
            public override void OnInteract() { }
        }
    }
}
