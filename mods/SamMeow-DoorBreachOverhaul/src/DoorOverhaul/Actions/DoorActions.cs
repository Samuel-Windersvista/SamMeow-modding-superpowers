using System.Collections.Generic;
using DoorBreach;
using EFT;
using EFT.Interactive;
using EFT.UI;
using DoorOverhaul.Doors;
using DoorOverhaul.Helpers;

namespace DoorOverhaul.Actions
{
    internal static class DoorActions
    {
        public const string PeekName = "Peek";
        public const string OpenFurtherName = "Open Further";
        public const string OpenQuietlyName = "Open Quietly";

        private const float FullyOpenMargin = 0.03f;

        public static List<ModInteractionAction> Create(GamePlayerOwner owner, Door door)
        {
            var actions = new List<ModInteractionAction>();
            if (!DoorMath.CanSwing(door) || IsWrecked(door))
            {
                return actions;
            }

            if (door.DoorState == EDoorState.Shut)
            {
                actions.Add(new ModInteractionAction
                {
                    Kind = DoorActionKind.Peek,
                    Name = PeekName,
                    Disabled = !door.Operatable,
                    Action = () => PeekDriver.BeginHold(owner, door, InteractKey.Held, Settings.QuickPeekPercent.Value / 100f)
                });
                actions.Add(new ModInteractionAction
                {
                    Kind = DoorActionKind.OpenQuietly,
                    Name = OpenQuietlyName,
                    Disabled = !door.Operatable,
                    Action = () => OpenQuietly(owner, door)
                });
            }
            else if (door.DoorState == EDoorState.Open && DoorMath.OpenFraction(door) < DoorMath.FullyOpenFraction(door) - FullyOpenMargin)
            {
                actions.Add(new ModInteractionAction
                {
                    Kind = DoorActionKind.OpenFurther,
                    Name = OpenFurtherName,
                    Disabled = !door.Operatable,
                    Action = () => PeekDriver.BeginHold(owner, door, InteractKey.Held, 1f)
                });
            }

            return actions;
        }

        private const float WreckedShare = 0.5f;

        public static bool IsWrecked(Door door)
        {
            if (!Destruction.DoorDestructionRegistry.TryGet(door, out Destruction.DoorEntry entry) || entry.State == null)
            {
                return false;
            }

            Destruction.Core.FractureState state = entry.State;
            if (entry.Takeover != null || state.Leaf != Destruction.Core.LeafStatus.Attached)
            {
                return true;
            }

            foreach (Destruction.Core.Anchor anchor in state.Anchors)
            {
                if (anchor.Failed && anchor.Spec.Kind == Destruction.Core.AnchorKind.Hinge)
                {
                    return true;
                }
            }

            return state.IntactCount < state.Layout.Cells.Length * (1f - WreckedShare);
        }

        public static void AppendMissing(List<InteractionAction> actions, GamePlayerOwner owner, Door door)
        {
            foreach (ModInteractionAction action in Create(owner, door))
            {
                if (!actions.Exists(a => a is ModInteractionAction mod && mod.Kind == action.Kind))
                {
                    actions.Add(action);
                }
            }
        }

        public static bool StartNativeOpen(GamePlayerOwner owner, Door door)
        {
            Player player = owner?.Player;
            if (player == null || door == null)
            {
                return false;
            }

            player.MovementContext.ResetCanUsePropState();
            var result = Door.Interact(player, EInteractionType.Open);
            if (result.Failed)
            {
                return false;
            }

            player.ExecuteInteraction(door, result.Value);
            return true;
        }

        public static bool OpenWithOverride(GamePlayerOwner owner, Door door, float fraction, float slowdown, bool quiet)
        {
            DoorOpenOverrides.Arm(door, fraction, slowdown, quiet);
            bool started = StartNativeOpen(owner, door);
            if (!started || door.DoorState != EDoorState.Shut)
            {
                DoorOpenOverrides.Disarm(door);
            }

            return started;
        }

        public static void OpenQuietly(GamePlayerOwner owner, Door door)
        {
            if (door.DoorState == EDoorState.Shut)
            {
                OpenWithOverride(owner, door, Settings.QuietOpenPercent.Value / 100f, Settings.QuietSwingSlowdown.Value, quiet: true);
            }
        }
    }
}
