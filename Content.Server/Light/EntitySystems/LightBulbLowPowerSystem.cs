using Content.Server.Light.Components;
using Content.Server.Power.Components;
using Content.Shared.Light.Components;
using Robust.Server.GameObjects;

namespace Content.Server.Light.EntitySystems;

/// <summary>
/// Handles low power behavior for <see cref="LightBulbComponent"/> entities
/// that have a <see cref="PoweredLightLowPowerComponent"/>.
/// Lightbulbs in a low power deficit will have reduced brightness and may flicker,
/// depending on the bulb's type.
/// </summary>
public sealed class LightBulbLowPowerSystem : EntitySystem
{
    [Dependency] private readonly SharedPointLightSystem _pointLightSystem = default!;

    EntityQuery<LightBulbComponent> _lightBulbQuery;

    public override void Initialize()
    {
        base.Initialize();

        _lightBulbQuery = GetEntityQuery<LightBulbComponent>();
    }

    // That's right baby. Another update loop for the masses.
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<PoweredLightLowPowerComponent, PoweredLightComponent, ApcPowerReceiverComponent, PointLightComponent>();
        while (query.MoveNext(out var uid, out var lowPowerComp, out var poweredLightComp, out var apcComp, out var pointLightComp))
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (apcComp.PowerDisabled)
                continue;

            if (!_lightBulbQuery.TryComp(poweredLightComp.LightBulbContainer.ContainedEntity, out var bulbComp))
                continue;

            // Light returned to a normal power state, so reapply normal brightness.
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (apcComp.NetworkLoad.DeficitFactor == 1)
            {
                _pointLightSystem.SetEnergy(uid, bulbComp.LightEnergy, pointLightComp);
                continue;
            }

            if (apcComp.NetworkLoad.ReceivingPower == 0)
            {
                _pointLightSystem.SetEnergy(uid, 0, pointLightComp);
                continue;
            }

            // Light is in a low power state.
            // Different types of bulbs respond accordingly to low power, so handle them here.

            // fuck you lets see if this works
            var energyFactor = bulbComp.LightEnergy * apcComp.NetworkLoad.DeficitFactor *
                               lowPowerComp.LowPowerBrightnessMultiplier;
            _pointLightSystem.SetEnergy(uid, energyFactor, pointLightComp);
        }
    }
}
