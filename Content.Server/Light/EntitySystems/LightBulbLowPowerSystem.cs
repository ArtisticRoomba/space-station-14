using Content.Server.Light.Components;
using Content.Server.Power.Components;
using Content.Shared.Light.Components;

namespace Content.Server.Light.EntitySystems;

/// <summary>
/// Handles low power behavior for <see cref="LightBulbComponent"/> entities
/// that have a <see cref="LightBulbLowPowerComponent"/>.
/// Lightbulbs in a low power deficit will have reduced brightness and may flicker,
/// depending on the bulb's type.
/// </summary>
public sealed class LightBulbLowPowerSystem : EntitySystem
{
    // That's right baby. Another update loop for the masses.
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<LightBulbLowPowerComponent, LightBulbComponent, ApcPowerReceiverComponent>();
    }
}
