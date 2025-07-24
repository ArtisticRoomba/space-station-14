using Content.Shared.Damage;
using Content.Shared.FixedPoint;

namespace Content.Server.Atmos.Components;

/// <summary>
/// Entities that have this component will have damage done to them depending on the local pressure
/// environment that they reside in.
/// </summary>
/// <remarks>If the entity does not have an <see cref="AirtightComponent"/>,
/// simple damage depending on the current pressure will be done instead.</remarks>
[RegisterComponent]
public sealed partial class DeltaPressureComponent : Component
{
    /// <summary>
    /// The base damage applied to the entity per atmos tick when it is above the damage threshold.
    /// This damage will be scaled as defined by the <see cref="DeltaPressureDamageScalingType"/> enum
    /// depending on the current effective pressure this entity is experiencing.
    /// </summary>
    [DataField]
    public DamageSpecifier BaseDamage = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            { "Structural", 20 },
        },
    };

    /// <summary>
    /// The minimum pressure at which the entity will start taking damage.
    /// This doesn't depend on the difference in pressure.
    /// The entity will start to take damage if it is exposed to this pressure.
    /// </summary>
    [DataField]
    public float MinPressure = 5000;

    /// <summary>
    /// The minimum difference in pressure between any side required for the entity to start taking damage.
    /// </summary>
    [DataField]
    public float MinPressureDelta = 2000;

    /// <summary>
    /// The maximum pressure at which damage will no longer scale.
    /// If the effective pressure goes beyond this, the damage will be considered at this pressure.
    /// </summary>
    [DataField]
    public float MaxPressure = 10000;

    /// <summary>
    /// Simple constant to affect the scaling behavior.
    /// See comments in the <see cref="DeltaPressureDamageScalingType"/> types to see how this affects scaling.
    /// </summary>
    [DataField]
    public float ScalingPower = 1;
}

[Serializable]
public enum DeltaPressureDamageScalingType : byte
{
    /// <summary>
    /// Damage dealt will be constant as long as the minimum values are met.
    /// </summary>
    Threshold,

    /// <summary>
    /// Damage dealt will be a linear function.
    /// Scaling power determines the slope of the function.
    /// </summary>
    Linear,

    /// <summary>
    /// Damage dealt will be a logarithmic function.
    /// Scaling power determines the base of the log.
    /// </summary>
    Log,

    /// <summary>
    /// Damage dealt will be an exponential function.
    /// Scaling power determines the power of the exponent.
    /// </summary>
    Exponential,
}
