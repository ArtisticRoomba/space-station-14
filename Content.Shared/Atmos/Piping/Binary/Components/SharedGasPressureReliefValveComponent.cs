﻿using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Piping.Binary.Components;

/// <summary>
/// Represents the unique key for the UI.
/// </summary>
[Serializable, NetSerializable]
public enum GasPressureReliefValveUiKey : byte
{
    Key,
}

/// <summary>
/// Message sent to change the pressure threshold of the Gas Pressure Relief Valve.
/// </summary>
/// <param name="pressure">The new pressure threshold value.</param>
[Serializable, NetSerializable]
public sealed class GasPressureReliefValveChangeThresholdMessage(float pressure) : BoundUserInterfaceMessage
{
    /// <summary>
    /// Gets the new threshold pressure value.
    /// </summary>
    public float ThresholdPressure { get; } = pressure;
}

/// <summary>
/// Packages and sends down data from the server to the client about the valve.
/// </summary>
[Serializable] [NetSerializable]
public sealed class PressureReliefValveUserMessage(float inletPressure, float outletPressure, float flowRate)
    : BoundUserInterfaceMessage
{
    public float FlowRate = flowRate;
    public float InletPressure = inletPressure;
    public float OutletPressure = outletPressure;
}
