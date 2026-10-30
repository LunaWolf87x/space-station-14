using System.Linq;
using Content.Shared._Starlight.Modsuits.Components;
using Content.Shared._Starlight.Modsuits.Events;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.DoAfter;
using Content.Shared.Inventory.Events;
using Robust.Shared.Containers;

namespace Content.Shared._Starlight.Modsuits;

public abstract partial class SharedModsuitSystem : EntitySystem
{
    [Dependency] private /*readonly*/ SharedActionsSystem _actions = null!;
    [Dependency] private /*readonly*/ SharedContainerSystem _container = null!;
    [Dependency] private /*readonly*/ SharedUserInterfaceSystem _ui = null!;
    [Dependency] private /*readonly*/ SharedDoAfterSystem _doAfter = null!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ModsuitComponent, MapInitEvent>(OnMapInIt);

        SubscribeLocalEvent<ModsuitComponent, GotEquippedEvent>(OnEquipped);
        //UI handling Events
        SubscribeLocalEvent<ModsuitComponent, DeployModsuit>(OpenRadialUI);
        SubscribeLocalEvent<ModsuitComponent, ModsuitSystemMessage>(OnSystemMessage);
        SubscribeLocalEvent<ModsuitComponent, PowerModsuit>(OnActivate);
        //MODsuit state changing events
        SubscribeLocalEvent<ModsuitComponent, DeployPartEvent>(DeployPart);
        SubscribeLocalEvent<ModsuitComponent, RetractPartEvent>(RetractPart);
        SubscribeLocalEvent<ModsuitComponent, CheckAirtightnessEvent>(CheckAirtightness);
    }
    // Spawns MODsuit parts into respective containers whenever it's initialized on the Map
    private void OnMapInIt(EntityUid uid, ModsuitComponent component, MapInitEvent args)
    {
        foreach (var (part, prototype)in component.Parts)
        {
            var container = _container.EnsureContainer<ContainerSlot>(
                uid,
                ModsuitContainers.GetPartContainer(part));

            if (container.ContainedEntity != null)
            {
                component.SpawnedParts[part] = container.ContainedEntity.Value;
                component.DeployedParts[part] = false;
                continue;
            }

            var entity = Spawn(prototype.ToString(), Transform(uid).Coordinates);

            if (!_container.Insert(entity, container))
            {
                Del(entity);
                continue;
            }

            component.SpawnedParts.TryAdd(part, entity);
            component.DeployedParts.TryAdd(part, false);
            Dirty(uid, component);
        }
    }

    private void OnEquipped(EntityUid uid, ModsuitComponent component, GotEquippedEvent args)
    {
        foreach (var action in component.ActionEndpoints)
        {
            EntityUid? entity = null;
            if (_actions.AddAction(args.EquipTarget, ref entity, out _, action, uid))
                component.ActionEntities.Add(entity.Value);
            Dirty(uid, component);
        }
    }

    [SubscribeLocalEvent]
    public void OnGotUnequipped(Entity<ModsuitComponent> ent, ref GotUnequippedEvent args)
    {
        var targetActions = _actions.GetActions(args.EquipTarget);
        foreach (var action in targetActions.Where(action => ent.Comp.ActionEntities.Contains(action)))
            _actions.RemoveAction(args.EquipTarget, action.AsNullable());
        ent.Comp.ActionEntities.Clear();
        Dirty(ent);
    }
    protected virtual void OnActivate(EntityUid uid, ModsuitComponent component, PowerModsuit args)
    {
    }

    private void OpenRadialUI(EntityUid uid, ModsuitComponent component, DeployModsuit args)
    {
        args.Handled = true;
        _ui.OpenUi(uid, ModsuitUiKey.Radial, args.Performer);
    }

    private void OnSystemMessage(EntityUid uid, ModsuitComponent component, ModsuitSystemMessage args)
    {
        DoAfterEvent doAfter;
        if (component.DeployedParts.TryGetValue(args.Part, out var deployed) && deployed)
        {
            doAfter = new RetractPartEvent(GetNetEntity(args.Actor), GetNetEntity(uid), args.Part);
        }
        else
            doAfter = new DeployPartEvent(GetNetEntity(args.Actor), GetNetEntity(uid), args.Part);

        var doAfterArgs = new DoAfterArgs(EntityManager, args.Actor,
            TimeSpan.FromSeconds(component.PowerOn ? component.ActiveDelay : 0), doAfter, uid);
        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    protected virtual void DeployPart(EntityUid uid, ModsuitComponent component, DeployPartEvent args)
    {
    }

    protected virtual void RetractPart(EntityUid uid, ModsuitComponent component, RetractPartEvent args)
    {
    }

    protected virtual void CheckAirtightness(Entity<ModsuitComponent> ent, ref CheckAirtightnessEvent args)
    {
    }

}
