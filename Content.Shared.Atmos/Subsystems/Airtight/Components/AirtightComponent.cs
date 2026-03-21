using Content.Shared.Atmos.Subsystems.Airtight.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Atmos.Subsystems.Airtight.Components;

[RegisterComponent, Access(typeof(AirtightSystem))]
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
