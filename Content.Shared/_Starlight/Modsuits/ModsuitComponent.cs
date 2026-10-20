using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.Modsuits.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ModsuitComponent : Component
{
    [DataField]
    public bool ProvidesPressureProtection = true;
    [DataField]
    public bool ProvidesTemperatureProtection = true;

    [DataField]
    public SoundSpecifier DeploySound = new SoundPathSpecifier("/Audio/Mecha/mechmove03.ogg");
    [DataField]
    public SoundSpecifier ErrorSound = new SoundPathSpecifier("/Audio/Machines/airlock_deny.ogg");
    [DataField]
    public SoundSpecifier PowerOnSound = new SoundPathSpecifier("/Audio/Misc/cryo_warning.ogg");

    [DataField]
    public List<EntProtoId> ActionEndpoints = new();
    [NonSerialized, AutoNetworkedField]
    public List<EntityUid> ActionEntities = new();

    [DataField]
    public Dictionary<ModsuitPartType, EntProtoId> Parts = new();
    [ViewVariables, AutoNetworkedField]
    public Dictionary<ModsuitPartType, bool> DeployedParts = new();
    [ViewVariables, AutoNetworkedField]
    public Dictionary<ModsuitPartType, EntityUid> SpawnedParts = new();
    [ViewVariables, AutoNetworkedField]
    public double ActiveDelay = 1.0;
    [ViewVariables, AutoNetworkedField]
    public bool PowerOn = false;
}
