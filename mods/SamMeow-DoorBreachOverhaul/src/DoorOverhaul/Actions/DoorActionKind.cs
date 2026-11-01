using EFT.UI;

namespace DoorOverhaul.Actions
{
    internal enum DoorActionKind
    {
        Peek,
        OpenFurther,
        OpenQuietly
    }

    internal sealed class ModInteractionAction : InteractionAction
    {
        public DoorActionKind Kind;
    }
}
