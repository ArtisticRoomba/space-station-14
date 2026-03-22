using Content.Shared.Atmos.Subsystems.Airtight.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Atmos.Subsystems.Airtight.Components;

/// <summary>
/// Component defining an entity as being airtight, meaning it can block air from flowing through it in certain directions,
/// and can contribute to the airtightness of a tile.
/// </summary>
/// <remarks>Use the APIs available in <see cref="AirtightSystem"/>.</remarks>
[RegisterComponent]
[Access(typeof(AirtightSystem), typeof(SharedAirtightMapSystem), Other = AccessPermissions.None)]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AirtightComponent : Component
{
    /// <summary>
    /// The last known position of the ent.
    /// This is used during component shutdown for updating the map as we're
    /// working around the fact that we don't know where the entity was during deletion,
    /// as the entity is deparented. Fun.
    /// </summary>
    [ViewVariables]
    public (EntityUid Grid, Vector2i Tile) LastPosition;

    /// <summary>
    /// Airtight data for this entity.
    /// </summary>
    /// TODO ATMOS: might need atomization and DeltaFielding, consider IEntityAirtightData and ITileAirtightData.
    [DataField, AutoNetworkedField]
    public EntityAirtightData AirtightData;
}
