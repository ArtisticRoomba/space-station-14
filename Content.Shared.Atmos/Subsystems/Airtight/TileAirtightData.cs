using Content.Shared.Atmos.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Subsystems.Airtight;

/// <summary>
/// Struct for storing airtight information on a physical
/// grid tile. This datatype is a product of the airtightness of the entities on this tile,
/// and is used to determine how air can flow through this tile to adjacent tiles,
/// among other specific behaviors.
/// </summary>
public struct TileAirtightData() : IRobustCloneable<TileAirtightData>
{
    /// <summary>
    /// Directions that air can flow to, from this tile to an adjacent tile.
    /// </summary>
    /// <example>A set bit/direction denotes allowed flow in that direction.</example>
    public AtmosDirection AdjacentAirflowDirections = AtmosDirection.Invalid;

    /// <summary>
    /// Whether this tile should have no air when all directions are blocked.
    /// Some entities do not fully take up an entire tile
    /// and allow air to be stored despite all directions being blocked (an encirclement
    /// of thindows).
    /// </summary>
    public bool NoAirWhenFullyAirBlocked = true;

    /// <summary>
    /// Whether a vacuum should be fixed by seeding air from other tiles
    /// instead of allowing the vacuum to persist.
    /// This behavior allows airlocks to open/close constantly without voiding air from an enclosed room.
    /// </summary>
    /// TODO: This needs to be partially replaced by air pushing to other tiles when a tile is blocked,
    /// though this requires a BFS API.
    public bool FixVacuum = false;

    /// <summary>
    /// Clones this <see cref="TileAirtightData"/>.
    /// </summary>
    /// <param name="data">The <see cref="TileAirtightData"/> to clone.</param>
    public TileAirtightData(TileAirtightData data) : this()
    {
        AdjacentAirflowDirections = data.AdjacentAirflowDirections;
        NoAirWhenFullyAirBlocked = data.NoAirWhenFullyAirBlocked;
        FixVacuum = data.FixVacuum;
    }

    /// <summary>
    /// Clones this <see cref="TileAirtightData"/>.
    /// </summary>
    /// <returns>A clone of this <see cref="TileAirtightData"/>.</returns>
    public TileAirtightData Clone()
    {
        return new TileAirtightData(this);
    }
}
