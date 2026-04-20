using Content.Server.Atmos.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Teleportation.Components;
using Robust.Shared.Timing;

namespace Content.Server.Atmos.EntitySystems;

/// <summary>
/// System for handling <see cref="TileToTileExchangeComponent"/>.
/// System will exchange gases between tiles every atmospherics update.
/// TODO ATMOS: When LINDA is abstracted to be an actually decent publicly usable system,
/// please tileshare using that instead of doing this gas summation and distribution.
/// </summary>
public sealed class TileToTileExchangeSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    [Dependency] private readonly EntityQuery<TileToTileExchangeComponent> _tileExchangeQuery;

    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(AtmosphereSystem));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var curTick = _timing.CurTick;
        var query = EntityQueryEnumerator<TileToTileExchangeComponent, LinkedEntityComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var tileExchangeComp, out var linkedEntComp, out var xform))
        {
            /*
             Gas needs to be summed up across all tiles before being redistributed to all tiles.
             Some ents have a one-way connection, so we also need to consider instances where ents only sink from a GasMixture
             and don't source to another.
             */
        }
    }
}
