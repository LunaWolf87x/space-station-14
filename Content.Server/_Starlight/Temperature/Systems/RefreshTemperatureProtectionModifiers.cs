using Content.Server._Starlight.Temperature.Events;
using Content.Shared._Starlight.Temperature.Components;

namespace Content.Server.Temperature.Systems;

public sealed partial class TemperatureSystem
{

    public void RefreshTemperatureProtectionModifiers(Entity<TemperatureProtectionComponent> ent)
    {
        var comp = ent.Comp;
        if (comp == null)
            return;

        if (!Resolve(ent, ref comp, false))
            return;

        ent.Comp = comp;

        var ev = new RefreshTemperatureProtectionModifiersEvent();
        RaiseLocalEvent(ent, ev);

        if (MathHelper.CloseTo(ev.CoefficientModifier, comp.CoolingCoefficient)
           )
            return;

        comp.CoolingCoefficient += ev.CoefficientModifier;
        if (comp.CoolingCoefficient > 1)
            comp.CoolingCoefficient = 1;
        if (comp.CoolingCoefficient < 0)
            comp.CoolingCoefficient = 0;
    }
}
