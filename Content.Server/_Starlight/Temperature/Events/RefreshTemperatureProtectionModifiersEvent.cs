namespace Content.Server._Starlight.Temperature.Events
{
    public sealed class RefreshTemperatureProtectionModifiersEvent : EntityEventArgs
    {
        public float CoefficientModifier { get; private set; } = 0.0f;

        public void ModifyProtection(float modifier)
        {
            CoefficientModifier += modifier;
        }
    }
}
