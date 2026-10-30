using Content.Server._Starlight.Atmos.Events;
using Content.Server.Atmos.Components;

namespace Content.Server.Atmos.EntitySystems;

public sealed partial class BarotraumaSystem
{
    public void RefreshPressureProtectionModifiers(EntityUid performer, Entity<PressureProtectionComponent> ent)
    {
        var comp = ent.Comp;
        if (comp == null)
            return;

        if (!Resolve(ent, ref comp, false))
            return;

        ent.Comp = comp;

        var ev = new RefreshPressureProtectionModifiersEvent();
        RaiseLocalEvent(ent, ev);

        if (MathHelper.CloseTo(ev.LowPressureModifier, comp.LowPressureModifier) &&
            MathHelper.CloseTo(ev.LowPressureMultiplier, comp.LowPressureMultiplier) &&
            MathHelper.CloseTo(ev.HighPressureModifier, comp.HighPressureModifier) &&
            MathHelper.CloseTo(ev.HighPressureMultiplier, comp.HighPressureMultiplier)
           )
            return;

        comp.LowPressureModifier += ev.LowPressureModifier;
        comp.LowPressureMultiplier *= ev.LowPressureMultiplier;
        comp.HighPressureModifier += ev.HighPressureModifier;
        comp.HighPressureMultiplier *= ev.HighPressureMultiplier;
        if (TryComp<BarotraumaComponent>(performer, out var barotrauma))
        {
            UpdateCachedResistances(performer, barotrauma);
        }
    }
}
