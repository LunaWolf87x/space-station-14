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


        //If the change is negligible, leave the Coefficients as is instead of modifying them
        if (MathHelper.CloseTo(ev.CoefficientModifier, comp.CoolingCoefficient)
           )
            return;

        if (MathHelper.CloseTo(ev.CoefficientModifier, comp.HeatingCoefficient)
        )
            return;

        // Caps both heating and cooling coefficients so they don't exceed 100% or 0% mitigation
        comp.CoolingCoefficient += ev.CoefficientModifier;
        if (comp.CoolingCoefficient > 1)
            comp.CoolingCoefficient = 1;
        if (comp.CoolingCoefficient < 0)
            comp.CoolingCoefficient = 0;

        comp.HeatingCoefficient += ev.CoefficientModifier;
        if (comp.HeatingCoefficient > 1)
            comp.HeatingCoefficient = 1;
        if (comp.HeatingCoefficient < 0)
            comp.HeatingCoefficient = 0;
    }
}
