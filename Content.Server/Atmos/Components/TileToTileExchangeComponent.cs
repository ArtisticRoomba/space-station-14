using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Teleportation.Components;

namespace Content.Server.Atmos.Components;

/// <summary>
/// Component that shares gas between two tiles, regardless of distance or location.
/// Used if you want to link two <see cref="GasMixture"/>s together for stuff like portals.
/// System will inspect <see cref="LinkedEntityComponent"/> to determine which entities to share gas with.
/// Device must have an <see cref="AtmosDeviceComponent"/>.
/// </summary>
[RegisterComponent]
public sealed partial class TileToTileExchangeComponent : Component;
