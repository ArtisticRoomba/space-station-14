using Content.Shared.Light.Components;

namespace Content.Server.Light.Components;

/// <summary>
/// Marks a <see cref="LightBulbComponent"/> as being affected by low power conditions.
/// Entities with this component may have their brightness reduced and may flicker
/// when the power network is in a deficit.
/// </summary>
[RegisterComponent]
public sealed partial class PoweredLightLowPowerComponent : Component
{
    /// <summary>
    /// Random chance to flicker when in low power state, per update tick,
    /// if this light bulb supports flickering.
    /// </summary>
    [DataField]
    public float FlickerChance = 0.1f;

    /// <summary>
    /// Multiplier for exacerbating low brightness conditions.
    /// The relationship between brightness and "voltage" drop is linear,
    /// however for theatrical effect we can drop off the brightness faster.
    /// </summary>
    [DataField]
    public float LowPowerBrightnessMultiplier = 0.5f;
}
