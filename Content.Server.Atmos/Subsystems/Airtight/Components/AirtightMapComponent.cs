using Content.Server.Atmos.Subsystems.Airtight.EntitySystems;
using Content.Shared.Atmos.Subsystems.Airtight.Components;
using Robust.Shared.Analyzers;

namespace Content.Server.Atmos.Subsystems.Airtight.Components;

/// <summary>
/// Component for grids that tracks airtight/non-airtight tiles in a map, and updates them
/// when <see cref="AirtightComponent"/> entities are added, removed, or move around on the grid.
/// </summary>
[RegisterComponent]
[Access(typeof(AirtightMapSystem), Other = AccessPermissions.None)]
public sealed partial class AirtightMapComponent : Component
{

}
