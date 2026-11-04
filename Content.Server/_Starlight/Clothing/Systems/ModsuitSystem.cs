using System.Linq;
using System.Timers;
using Content.Server._Starlight.Atmos.Events;
using Content.Server._Starlight.Temperature.Events;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Temperature.Systems;
using Content.Shared._Starlight.Modsuits;
using Content.Shared._Starlight.Modsuits.Components;
using Content.Shared._Starlight.Modsuits.Events;
using Content.Shared._Starlight.Temperature.Components;
using Content.Shared.Atmos.Components;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.Toggleable;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using DependencyAttribute = Robust.Shared.IoC.DependencyAttribute;
using Timer = Robust.Shared.Timing.Timer; //Timer requested a specifier, will try this one

namespace Content.Server._Starlight.Clothing.Systems;

public sealed partial class ModsuitSystem : SharedModsuitSystem
{
    [Dependency] private SharedContainerSystem _container = null!;
    [Dependency] private InventorySystem _inventory = null!;
    [Dependency] private SharedAudioSystem _audio = null!;
    [Dependency] private SharedAppearanceSystem _appearance = null!;
    [Dependency] private BarotraumaSystem _barotraumaSystem = null!;
    [Dependency] private TemperatureSystem _temperatureSystem = null!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PressureProtectionComponent, RefreshPressureProtectionModifiersEvent>(OnPressureProtectionEvent);
        SubscribeLocalEvent<TemperatureProtectionComponent, RefreshTemperatureProtectionModifiersEvent>(OnTemperatureProtectionEvent);
    }

    protected override void DeployPart(EntityUid uid, ModsuitComponent component, DeployPartEvent args)
    {
        if (!component.SpawnedParts.TryGetValue(args.Part, out var entity))
            return;
        var performer = GetEntity(args.Performer);
        var slot = ModsuitContainers.GetInventorySlot(args.Part);
        if (ModsuitContainers.TryGetStorageContainer(args.Part, out var storageName))
        {
            if (_inventory.TryGetSlotEntity(performer, slot, out var oldItem) /* && oldItem != null*/)
            {
                if (_inventory.TryUnequip(performer, performer, slot, force: true))
                {
                    if (_container.TryGetContainer(uid, storageName, out var storage)) //BaseContainer?
                        _container.Insert(oldItem.Value, storage);
                }
            }
        }

        if (!_inventory.TryEquip(performer, entity, slot, force: true))
        {
            if (_inventory.TryGetSlotEntity(performer, slot, out var oldItem) /*&& oldItem != null*/)
            {
                _audio.PlayPredicted(component.ErrorSound, uid, performer, AudioParams.Default.WithVolume(-2f));
            }
            return;
        }
        _audio.PlayPvs(component.DeploySound, uid, AudioParams.Default.WithVolume(-2f));
        component.DeployedParts[args.Part] = true;
        EnsureComp<UnremoveableComponent>(entity);

        if (!HasComp<UnremoveableComponent>(uid))
        {
            EnsureComp<UnremoveableComponent>(uid);
        }
        if (component.PowerOn)
            RaiseLocalEvent(uid, new CheckAirtightnessEvent(performer));
    }

    protected override void RetractPart(EntityUid uid, ModsuitComponent component, RetractPartEvent args)
    {
        if (!component.SpawnedParts.TryGetValue(args.Part, out var entity))
            return;
        var performer = GetEntity(args.Performer);
        RemComp<UnremoveableComponent>(entity);

        var slot = ModsuitContainers.GetInventorySlot(args.Part);

        if (!_inventory.TryUnequip(uid, performer, slot, force: true))
            return;

        var container = _container.EnsureContainer<ContainerSlot>(uid, ModsuitContainers.GetPartContainer(args.Part));

        if (!_container.Insert(entity, container))
            return;
        if (ModsuitContainers.TryGetStorageContainer(args.Part, out var storageName))
        {
            if (!_container.TryGetContainer(uid, storageName, out var baseContainer))
                return;
            if (baseContainer is not ContainerSlot storage)
                return;
            if (storage.ContainedEntity != null)
            {
                var oldItem = storage.ContainedEntity.Value;
                _container.Remove(oldItem, storage);
                _inventory.TryEquip(performer, oldItem, slot, force: true);
            }
        }
        _audio.PlayPredicted(component.DeploySound, uid, performer, AudioParams.Default.WithVolume(-2f));
        component.DeployedParts[args.Part] = false;

        if (!component.DeployedParts.Values.Any(x => x))
            RemComp<UnremoveableComponent>(uid);
        if (component.PowerOn)
            RaiseLocalEvent(uid, new CheckAirtightnessEvent(performer));
    }

    protected override void OnActivate(EntityUid uid, ModsuitComponent component, PowerModsuit args)
    {
        args.Handled = true;
        var performer = args.Performer;
        var delay = 0.0;
        foreach (var partKey in component.SpawnedParts.Keys)
        {
            var currentDelay = delay;
            Timer.Spawn(TimeSpan.FromSeconds(currentDelay), () =>
            {
                var part = component.SpawnedParts[partKey];
                _appearance.SetData(part, ToggleableVisuals.Enabled, component.PowerOn);
                _audio.PlayPvs(component.PowerOnSound, uid, AudioParams.Default.WithVolume(-2f));
            });
            delay += component.ActiveDelay;
        }

        Timer.Spawn(TimeSpan.FromSeconds(delay), () =>
        {
            _appearance.SetData(uid, ToggleableVisuals.Enabled, component.PowerOn);
            _audio.PlayPvs(component.PowerOnSound, uid, AudioParams.Default.WithVolume(-2f));
        });
        component.PowerOn = !component.PowerOn;
        Dirty(uid,component);
        RaiseLocalEvent(uid, new CheckAirtightnessEvent(performer));
    }

    protected override void CheckAirtightness(Entity<ModsuitComponent> ent, ref CheckAirtightnessEvent args)
    {
        var component = ent.Comp;
        var helmet = component.SpawnedParts[ModsuitPartType.Helmet];
        if (component.PowerOn && component.DeployedParts[ModsuitPartType.Helmet] &&
            component.DeployedParts[ModsuitPartType.Chestplate])
        {
            EnsureComp<BreathToolComponent>(helmet);
            EnsureComp<IdentityBlockerComponent>(helmet);
            if (!EnsureComp<HideLayerClothingComponent>(helmet, out var hideComp))
            {
                hideComp.Layers = new()
                {
                    { HumanoidVisualLayers.Hair, SlotFlags.HEAD },
                    { HumanoidVisualLayers.Snout, SlotFlags.HEAD },
                    { HumanoidVisualLayers.HeadTop, SlotFlags.HEAD },
                    { HumanoidVisualLayers.HeadSide, SlotFlags.HEAD },
                    { HumanoidVisualLayers.FacialHair, SlotFlags.HEAD },
                };
            }

            if (TryComp<ClothingComponent>(helmet, out var clothComp) &&
                _container.TryGetContainingContainer(helmet, out var container))
            {
                var clothingGotEquippedEvent = new ClothingGotEquippedEvent(container.Owner, clothComp);
                RaiseLocalEvent(helmet, ref clothingGotEquippedEvent);
            }

            foreach (var part in ModsuitContainers.ProtectionSlots)
            {
                var performer = args.Player;
                var partEntity = component.SpawnedParts[part.Key];
                if (component.ProvidesPressureProtection)
                {
                    if (TryComp<PressureProtectionComponent>(partEntity, out var pressureProtection))
                        _barotraumaSystem.RefreshPressureProtectionModifiers(performer, (partEntity, pressureProtection));//Need to pass through the owning entity ie: Strayed
                }

                if (component.ProvidesTemperatureProtection)
                {
                    if (TryComp<TemperatureProtectionComponent>(partEntity, out var temperatureProtection))
                        _temperatureSystem.RefreshTemperatureProtectionModifiers((partEntity, temperatureProtection)); //Once again, variable is attempting to access a function that does not exist
                }

                if (TryComp<ModsuitClothingComponent>(partEntity, out var modComp))
                    modComp.ValuesChanged = true;
            }
        }
        else
        {
            if (!component.DeployedParts[ModsuitPartType.Helmet] || !component.PowerOn)
            {
                if (TryComp<ClothingComponent>(helmet, out var clothComp) &&
                    _container.TryGetContainingContainer(helmet, out var container))
                {
                    var clothingGotUnequippedEvent = new ClothingGotUnequippedEvent(container.Owner, clothComp);
                    RaiseLocalEvent(helmet, ref clothingGotUnequippedEvent);
                }

                RemComp<BreathToolComponent>(helmet);
                RemComp<IdentityBlockerComponent>(helmet);
                RemComp<HideLayerClothingComponent>(helmet);
            }

            foreach (var part in ModsuitContainers.ProtectionSlots)
            {
                var performer = args.Player;
                var partEntity = component.SpawnedParts[part.Key];
                if (TryComp<PressureProtectionComponent>(partEntity, out var pressureProtection))
                    _barotraumaSystem.RefreshPressureProtectionModifiers(performer, (partEntity, pressureProtection)); //lacks any reference to the entity its being equipped to

                if (TryComp<TemperatureProtectionComponent>(partEntity, out var temperatureProtection))
                    _temperatureSystem.RefreshTemperatureProtectionModifiers((partEntity, temperatureProtection));

                if (TryComp<ModsuitClothingComponent>(partEntity, out var modComp))
                    modComp.ValuesChanged = false;
            }
        }
    }

    private void OnPressureProtectionEvent(Entity<PressureProtectionComponent> ent,
        ref RefreshPressureProtectionModifiersEvent args)
    {
        if (!TryComp<ModsuitClothingComponent>(ent, out var comp))
            return;
        args.ModifyProtection(
            (comp.ValuesChanged ? -1 : 1) * comp.LowPressureModifier,
            comp.ValuesChanged ? 1f / comp.LowPressureMultiplier : comp.LowPressureMultiplier,
            (comp.ValuesChanged ? -1 : 1) * comp.HighPressureModifier,
            comp.ValuesChanged ? 1f / comp.HighPressureMultiplier : comp.HighPressureMultiplier
        );
    }

    private void OnTemperatureProtectionEvent(Entity<TemperatureProtectionComponent> ent,
        ref RefreshTemperatureProtectionModifiersEvent args)
    {
        if (!TryComp<ModsuitClothingComponent>(ent, out var comp))
            return;
        args.ModifyProtection(
            (comp.ValuesChanged ? -1 : 1) * comp.CoefficientModifier
        );
    }

}
