using Content.Server.Atmos.Components;
using Content.Shared.Atmos;
using Content.Shared.Teleportation.Components;
using Robust.Shared.Collections;
using Robust.Shared.Timing;

namespace Content.Server.Atmos.EntitySystems;

/// <summary>
/// System for handling <see cref="TileToTileExchangeComponent"/>.
/// System will exchange gases between tiles every atmospherics update.
/// TODO ATMOS: When LINDA is abstracted to be an actually decent publicly usable system,
/// please tileshare using that instead of doing this gas summation and distribution.
/// </summary>
public sealed partial class TileToTileExchangeSystem : EntitySystem
{
    [Dependency] private AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private IGameTiming _timing = default!;

    [Dependency] private EntityQuery<TileToTileExchangeComponent> _tileExchangeQuery;

    /// <summary>
    /// Cached list of <see cref="GasMixture"/>s to avoid allocs when summing volumes/mixes.
    /// </summary>
    private ValueList<GasMixture> _gasMixturesSinksSources = [];

    /// <summary>
    /// Cached list of <see cref="GasMixture"/>s to avoid allocs when summing volumes/mixes.
    /// This list contains only sources from one-way links.
    /// </summary>
    private ValueList<GasMixture> _gasMixtureSources = [];

    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(AtmosphereSystem));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // TODO update limiting

        var curTick = _timing.CurTick;
        var query = EntityQueryEnumerator<TileToTileExchangeComponent, LinkedEntityComponent>();
        while (query.MoveNext(out var uid, out var tileExchangeComp, out var linkedEntComp))
        {
            /*
             Gas needs to be summed up across all tiles before being redistributed to all tiles.
             Some ents have a one-way connection, so we also need to consider instances where ents only sink from a GasMixture
             and don't source to another.
             */

            // Before first, ensure that we haven't processed this group already.
            if (tileExchangeComp.LastUpdated == curTick)
                continue;

            // Mark this one as processed first.
            MarkProcessed((uid, tileExchangeComp), curTick);

            // First, go through every linked entity (two-way and one-way) to get our candidate mixtures.
            // Sum up the volume to distribute properly later.
            var totalVol = 0f;
            var oneWayVol = 0f;
            _gasMixturesSinksSources.Clear();
            foreach (var ent in linkedEntComp.LinkedEntities)
            {
                var mixture = _atmosphereSystem.GetTileMixture(ent, true);
                if (mixture is null)
                    continue;

                totalVol += mixture.Volume;
                _gasMixturesSinksSources.Add(mixture);

                // And mark as processed.
                MarkProcessed(ent, curTick);
            }

            // Now consider one-way links that only source to this tile.
            _gasMixtureSources.Clear();
            foreach (var ent in linkedEntComp.OneWayLinkedEntities)
            {
                var mixture = _atmosphereSystem.GetTileMixture(ent, true);
                if (mixture is null)
                    continue;

                // Need to add one-way sources to the total volume as well,
                // since we will be summing them up and redistributing them.
                // To prevent vacuuming up the one-way sources constantly, we will just have to manually check
                // if the summed mixture is at lower pressure and then determine how much it would take to equalize pressure,
                // then carry it over.
                totalVol += mixture.Volume;
                oneWayVol += mixture.Volume;
                _gasMixtureSources.Add(mixture);
            }

            // Summer mixture.
            // God, give me struct GasMixtures, and my life is yours.
            var merged = new GasMixture(totalVol);
            foreach (var mixture in _gasMixturesSinksSources)
            {
                var pulledAir = mixture.RemoveRatio(1f);
                _atmosphereSystem.Merge(merged, pulledAir);
            }

            var preMerged = new GasMixture(oneWayVol);
            foreach (var mixture in _gasMixtureSources)
            {
                var pulledAir = mixture.RemoveRatio(1f);
                _atmosphereSystem.Merge(preMerged, pulledAir);
            }

            // Distribute the two-way sources.
            // To increase perf we could assume that all mixtures have the same volume, but you never know. Smile.
            foreach (var mixture in _gasMixturesSinksSources)
            {
                var toAdd = merged.RemoveVolume(mixture.Volume);
                _atmosphereSystem.Merge(mixture, toAdd);
            }

            // Next, we need to determine how much of the one-way source mixtures we can actually pull in without causing a vacuum.
            // This sucks to do with N mixtures.
            // Perform an exchange query to determine if we are at lower pressure, and if so, how much of the one-way sources we can actually pull in.
            if (merged.Pressure < preMerged.Pressure)
            {
                var equalizationFrac = _atmosphereSystem.FractionToEqualizePressure(merged, preMerged);
                foreach (var mixture in _gasMixtureSources)
                {
                    // TODO check if this is right
                    var toAdd = preMerged.RemoveVolume(mixture.Volume * equalizationFrac);
                    _atmosphereSystem.Merge(mixture, toAdd);
                }
            }
        }
    }

    /// <summary>
    /// Marks an entity as processed.
    /// </summary>
    /// <param name="ent">The entity to mark as processed.</param>
    /// <param name="curTick">The current game tick.</param>
    private void MarkProcessed(Entity<TileToTileExchangeComponent?> ent, GameTick curTick)
    {
        if (_tileExchangeQuery.Resolve(ent, ref ent.Comp))
        {
            ent.Comp.LastUpdated = curTick;
        }
    }
}
