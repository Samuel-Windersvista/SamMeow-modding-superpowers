namespace InescapableTarkovsSoftcore;

/// <summary>
/// Single source of truth for the mod identity strings (GUID, display name,
/// version). Consumed by <see cref="ModMetadata"/>, <see cref="ModLoadHook"/>,
/// and the build script.
/// </summary>
public static class ModIdentity
{
    public const string Guid = "com.sammeow.inescapable-softcore";

    public const string Name = "Inescapable Tarkov's Softcore";

    public const string Version = "0.1.0";
}
