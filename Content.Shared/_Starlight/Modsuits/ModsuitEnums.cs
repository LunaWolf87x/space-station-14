using Robust.Shared.Serialization;

namespace Content.Shared._Starlight.Modsuits;

public enum ModsuitPartType
{
    Helmet,
    Chestplate,
    Gauntlets,
    Boots
}
[Serializable, NetSerializable]
public enum ModsuitUiKey
{
    Radial
}

[Serializable, NetSerializable]
public enum ModsuitVisuals : byte
{
    IconLayer
}
