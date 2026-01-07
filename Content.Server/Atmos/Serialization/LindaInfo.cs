using System.Runtime.CompilerServices;
using Content.Shared.Atmos;
using Content.Shared.Atmos.EntitySystems;

namespace Content.Server.Atmos.Serialization;

/// <summary>
/// Data struct used to wrap necessary information for LINDA.
/// LINDA sometimes needs to store information that must be accounted for in certain processing steps
/// (ex. some operations can't be multithreaded so they are delegated to main loop).
/// </summary>
/// <remarks>This is an internal Atmospherics struct. If you need shit from this (???) write an API.</remarks>
[Access(typeof(SharedAtmosphereSystem))]
public struct LindaInfo
{
    /// <summary>
    /// Whether LINDA will perform air sharing with other tiles this processing run.
    /// </summary>
    [ViewVariables]
    public bool ShareAir;

    /// <summary>
    /// The mole differences from each direction
    /// that LINDA computed from sharing air.
    /// This is used to apply HighPressureDelta effects.
    /// This is formatted in <see cref="Atmospherics.Directions"/>.
    /// </summary>
    public ComputedDistanceHolder ComputedDifference;
}

/// <summary>
/// Struct that holds the results of LINDA's Share operation
/// for each direction.
/// </summary>
/// <remarks>I love cache locality!!!!!!!!!!!!!</remarks>
[InlineArray(Atmospherics.Directions)]
public struct ComputedDistanceHolder
{
    private float _element0;
}
