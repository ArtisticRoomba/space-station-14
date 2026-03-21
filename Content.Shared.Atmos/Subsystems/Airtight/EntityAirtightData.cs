using Content.Shared.Atmos.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Subsystems.Airtight;

/// <summary>
/// Struct for storing airtight information on an entity.
/// This data is combined to form airtightness on a tile,
/// the product of this being <see cref="TileAirtightData"/>.
/// </summary>
public struct EntityAirtightData() : IRobustCloneable<EntityAirtightData>
{
    /// <summary>
    /// Initial directions that this entity blocks air from flowing through.
    /// This may need to be rotated to accurately reflect
    /// the actual blocked directions of this entity on the tile if the entity is allowed to rotate.
    /// </summary>
    public AtmosDirection InitialAirBlockedDirections = AtmosDirection.Invalid;

    /// <summary>
    /// Current directions that this entity blocks air from flowing through.
    /// </summary>
    public AtmosDirection CurrentAirBlockedDirections = AtmosDirection.Invalid;

    /// <summary>
    /// Whether this entity should cause the tile to have no air when all directions are blocked.
    /// </summary>
    public bool NoAirWhenFullyAirBlocked = true;

    /// <summary>
    /// Whether Atmospherics should seed air from other tiles to replace a vacuum when a tile
    /// becomes vacuumed by this entity's airtightness instead of allowing the vacuum to persist.
    /// </summary>
    public bool FixVacuum = false;

    /// <summary>
    /// Clones this <see cref="EntityAirtightData"/>.
    /// </summary>
    /// <param name="data">The <see cref="EntityAirtightData"/> to clone.</param>
    public EntityAirtightData(EntityAirtightData data) : this()
    {
        InitialAirBlockedDirections = data.InitialAirBlockedDirections;
        CurrentAirBlockedDirections = data.CurrentAirBlockedDirections;
        NoAirWhenFullyAirBlocked = data.NoAirWhenFullyAirBlocked;
        FixVacuum = data.FixVacuum;
    }

    /// <summary>
    /// Clones this <see cref="EntityAirtightData"/>.
    /// </summary>
    /// <returns>A clone of this <see cref="EntityAirtightData"/>.</returns>
    public EntityAirtightData Clone()
    {
        return new EntityAirtightData(this);
    }
}
