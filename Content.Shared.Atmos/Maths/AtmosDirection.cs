using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Maths;

/// <summary>
/// Enum that represents various cardinal directions in Atmospherics.
/// We use this over the engine's <see cref="Direction"/> as we're going
/// to be doing some heavy bitflag usage
/// </summary>
/// <remarks>Atmospherics expects directions to come in pairs,
/// so directions cannot be singular in nature. Ex. there must be a "down" direction
/// associated with an "up" direction.</remarks>
[Flags, Serializable]
[FlagsFor(typeof(AtmosDirectionFlags))]
public enum AtmosDirection
{
    Invalid = 0,                        // 0
    North   = 1 << 0,                   // 1
    South   = 1 << 1,                   // 2
    East    = 1 << 2,                   // 4
    West    = 1 << 3,                   // 8

    NorthEast = North | East,           // 5
    SouthEast = South | East,           // 6
    NorthWest = North | West,           // 9
    SouthWest = South | West,           // 10

    All = North | South | East | West,  // 15
}
