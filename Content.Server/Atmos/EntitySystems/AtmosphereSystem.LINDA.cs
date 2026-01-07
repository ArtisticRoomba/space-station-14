using System.Diagnostics;
using Content.Server.Atmos.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace Content.Server.Atmos.EntitySystems;

public sealed partial class AtmosphereSystem
{
    #region Singlethreaded Impl

    private void ProcessCell(
        Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> ent,
        TileAtmosphere tile,
        int fireCount)
    {
        var gridAtmosphere = ent.Comp1;
        // Can't process a tile without air
        if (tile.Air == null)
        {
            // Critical section: Disposal of active group that is shared among multiple tiles.
            RemoveActiveTile(gridAtmosphere, tile);
            return;
        }

        if (tile.ArchivedCycle < fireCount)
            Archive(tile, fireCount);

        tile.CurrentCycle = fireCount;
        var adjacentTileLength = 0;

        for (var i = 0; i < Atmospherics.Directions; i++)
        {
            var direction = (AtmosDirection)(1 << i);
            if (tile.AdjacentBits.IsFlagSet(direction))
                adjacentTileLength++;
        }

        for (var i = 0; i < Atmospherics.Directions; i++)
        {
            var direction = (AtmosDirection)(1 << i);
            if (!tile.AdjacentBits.IsFlagSet(direction))
                continue;
            var enemyTile = tile.AdjacentTiles[i];

            // If the tile is null or has no air, we don't do anything for it.
            if (enemyTile?.Air == null)
                continue;
            if (fireCount <= enemyTile.CurrentCycle)
                continue;
            Archive(enemyTile, fireCount);

            var shouldShareAir = false;

            if (ExcitedGroups && tile.ExcitedGroup != null && enemyTile.ExcitedGroup != null)
            {
                if (tile.ExcitedGroup != enemyTile.ExcitedGroup)
                {
                    // Critical section: Merging two active groups.
                    ExcitedGroupMerge(gridAtmosphere, tile.ExcitedGroup, enemyTile.ExcitedGroup);
                }

                shouldShareAir = true;
            }
            else if (CompareExchange(tile, enemyTile) != GasCompareResult.NoExchange)
            {
                // Critical section: Adding tiles to the ActiveTiles hashset.
                AddActiveTile(gridAtmosphere, enemyTile);
                if (ExcitedGroups)
                {
                    var excitedGroup = tile.ExcitedGroup;
                    excitedGroup ??= enemyTile.ExcitedGroup;

                    if (excitedGroup == null)
                    {
                        excitedGroup = new ExcitedGroup();
                        gridAtmosphere.ExcitedGroups.Add(excitedGroup);
                    }

                    if (tile.ExcitedGroup == null)
                        ExcitedGroupAddTile(excitedGroup, tile);

                    if (enemyTile.ExcitedGroup == null)
                        ExcitedGroupAddTile(excitedGroup, enemyTile);
                }

                shouldShareAir = true;
            }

            if (shouldShareAir)
            {
                var difference = Share(tile, enemyTile, adjacentTileLength);

                // Monstermos already handles this, so let's not handle it ourselves.
                if (!MonstermosEqualization)
                {
                    // Critical section: HighPressureDelta tile invalidation.
                    if (difference >= 0)
                    {
                        ConsiderPressureDifference(gridAtmosphere, tile, direction, difference);
                    }
                    else
                    {
                        ConsiderPressureDifference(gridAtmosphere, enemyTile, i.ToOppositeDir(), -difference);
                    }
                }

                // Critical section: Potential reset of an active group's cooldown.
                LastShareCheck(tile);
            }
        }

        if (tile.Air != null)
            React(tile.Air, tile);

        // Critical section: Tile invalidation adds tiles to hashset.
        InvalidateVisuals(ent, tile);

        var remove = true;

        if (tile.Air!.Temperature > Atmospherics.MinimumTemperatureStartSuperConduction)
        {
            // Critical section: Superconductivity tiles are added to a hashset.
            if (ConsiderSuperconductivity(gridAtmosphere, tile, true))
                remove = false;
        }

        // Critical section: Active tiles removed.
        if (ExcitedGroups && tile.ExcitedGroup == null && remove)
            RemoveActiveTile(gridAtmosphere, tile);
    }

    private void Archive(TileAtmosphere tile, int fireCount)
    {
        if (tile.Air != null)
            tile.AirArchived = new GasMixture(tile.Air);

        tile.ArchivedCycle = fireCount;
    }

    private void LastShareCheck(TileAtmosphere tile)
    {
        if (tile.Air == null || tile.ExcitedGroup == null)
            return;

        switch (tile.LastShare)
        {
            // Refresh this tile's suspension cooldown if it had significant sharing.
            case > Atmospherics.MinimumAirToSuspend:
                ExcitedGroupResetCooldowns(tile.ExcitedGroup);
                break;

            // If this tile moved a very small amount of air, but not enough to matter,
            // we set the dismantle cooldown to 0.
            // This dissolves the group without performing an equalization as we expect
            // the group to be mostly equalized already if we're moving around miniscule
            // amounts of air.
            case > Atmospherics.MinimumMolesDeltaToMove:
                tile.ExcitedGroup.DismantleCooldown = 0;
                break;
        }
    }

    /// <summary>
    ///     Makes a tile become active and start processing. Does NOT check if the tile belongs to the grid atmos.
    /// </summary>
    /// <param name="gridAtmosphere">Grid Atmosphere where to get the tile.</param>
    /// <param name="tile">Tile Atmosphere to be activated.</param>
    ///<remarks>This method is public for the purposes of benchmarking.
    /// DO NOT USE THIS METHOD TO MARK A TILE AS ACTIVE IN REGULAR CODE.</remarks>
    public void AddActiveTile(GridAtmosphereComponent gridAtmosphere, TileAtmosphere tile)
    {
        if (tile.Air == null || tile.Excited)
            return;

        tile.Excited = true;
        gridAtmosphere.ActiveTiles.Add(tile);
    }

    /// <summary>
    ///     Makes a tile become inactive and stop processing.
    /// </summary>
    /// <param name="gridAtmosphere">Grid Atmosphere where to get the tile.</param>
    /// <param name="tile">Tile Atmosphere to be deactivated.</param>
    /// <param name="disposeExcitedGroup">Whether to dispose of the tile's <see cref="ExcitedGroup"/></param>
    private void RemoveActiveTile(GridAtmosphereComponent gridAtmosphere,
        TileAtmosphere tile,
        bool disposeExcitedGroup = true)
    {
        DebugTools.Assert(tile.Excited == gridAtmosphere.ActiveTiles.Contains(tile));
        DebugTools.Assert(tile.Excited || tile.ExcitedGroup == null);

        if (!tile.Excited)
            return;

        tile.Excited = false;
        gridAtmosphere.ActiveTiles.Remove(tile);

        if (tile.ExcitedGroup == null)
            return;

        if (disposeExcitedGroup)
            ExcitedGroupDispose(gridAtmosphere, tile.ExcitedGroup);
        else
            ExcitedGroupRemoveTile(tile.ExcitedGroup, tile);
    }

    /// <summary>
    ///     Calculates the heat capacity for a gas mixture, using the archived values.
    /// </summary>
    public float GetHeatCapacityArchived(TileAtmosphere tile)
    {
        if (tile.AirArchived == null)
            return tile.HeatCapacity;

        return GetHeatCapacity(tile.AirArchived);
    }

    /// <summary>
    ///     Shares gas between two tiles. Part of LINDA.
    /// </summary>
    public float Share(TileAtmosphere tileReceiver, TileAtmosphere tileSharer, int atmosAdjacentTurfs)
    {
        if (tileReceiver.Air is not { } receiver || tileSharer.Air is not { } sharer ||
            tileReceiver.AirArchived == null || tileSharer.AirArchived == null)
            return 0f;

        var temperatureDelta = tileReceiver.AirArchived.Temperature - tileSharer.AirArchived.Temperature;
        var absTemperatureDelta = Math.Abs(temperatureDelta);
        var oldHeatCapacity = 0f;
        var oldSharerHeatCapacity = 0f;

        if (absTemperatureDelta > Atmospherics.MinimumTemperatureDeltaToConsider)
        {
            oldHeatCapacity = GetHeatCapacity(receiver);
            oldSharerHeatCapacity = GetHeatCapacity(sharer);
        }

        var heatCapacityToSharer = 0f;
        var heatCapacitySharerToThis = 0f;
        var movedMoles = 0f;
        var absMovedMoles = 0f;

        for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
        {
            var thisValue = receiver.Moles[i];
            var sharerValue = sharer.Moles[i];
            var delta = (thisValue - sharerValue) / (atmosAdjacentTurfs + 1);
            if (!(MathF.Abs(delta) >= Atmospherics.GasMinMoles))
                continue;
            if (absTemperatureDelta > Atmospherics.MinimumTemperatureDeltaToConsider)
            {
                var gasHeatCapacity = delta * GasSpecificHeats[i];
                if (delta > 0)
                {
                    heatCapacityToSharer += gasHeatCapacity;
                }
                else
                {
                    heatCapacitySharerToThis -= gasHeatCapacity;
                }
            }

            if (!receiver.Immutable)
                receiver.Moles[i] -= delta;
            if (!sharer.Immutable)
                sharer.Moles[i] += delta;
            movedMoles += delta;
            absMovedMoles += MathF.Abs(delta);
        }

        tileReceiver.LastShare = absMovedMoles;

        if (absTemperatureDelta > Atmospherics.MinimumTemperatureDeltaToConsider)
        {
            var newHeatCapacity = oldHeatCapacity + heatCapacitySharerToThis - heatCapacityToSharer;
            var newSharerHeatCapacity = oldSharerHeatCapacity + heatCapacityToSharer - heatCapacitySharerToThis;

            // Transfer of thermal energy (via changed heat capacity) between self and sharer.
            if (!receiver.Immutable && newHeatCapacity > Atmospherics.MinimumHeatCapacity)
            {
                receiver.Temperature =
                    ((oldHeatCapacity * receiver.Temperature) -
                     (heatCapacityToSharer * tileReceiver.AirArchived.Temperature) +
                     (heatCapacitySharerToThis * tileSharer.AirArchived.Temperature)) / newHeatCapacity;
            }

            if (!sharer.Immutable && newSharerHeatCapacity > Atmospherics.MinimumHeatCapacity)
            {
                sharer.Temperature =
                    ((oldSharerHeatCapacity * sharer.Temperature) -
                     (heatCapacitySharerToThis * tileSharer.AirArchived.Temperature) +
                     (heatCapacityToSharer * tileReceiver.AirArchived.Temperature)) / newSharerHeatCapacity;
            }

            // Thermal energy of the system (self and sharer) is unchanged.

            if (MathF.Abs(oldSharerHeatCapacity) > Atmospherics.MinimumHeatCapacity)
            {
                if (MathF.Abs(newSharerHeatCapacity / oldSharerHeatCapacity - 1) < 0.1)
                {
                    TemperatureShare(tileReceiver, tileSharer, Atmospherics.OpenHeatTransferCoefficient);
                }
            }
        }

        if (!(temperatureDelta > Atmospherics.MinimumTemperatureToMove) &&
            !(MathF.Abs(movedMoles) > Atmospherics.MinimumMolesDeltaToMove))
            return 0f;
        var moles = receiver.TotalMoles;
        var theirMoles = sharer.TotalMoles;

        return (tileReceiver.AirArchived.Temperature * (moles + movedMoles)) -
               (tileSharer.AirArchived.Temperature * (theirMoles - movedMoles)) * Atmospherics.R / receiver.Volume;
    }

    /// <summary>
    ///     Shares temperature between two mixtures, taking a conduction coefficient into account.
    /// </summary>
    public float TemperatureShare(TileAtmosphere tileReceiver, TileAtmosphere tileSharer, float conductionCoefficient)
    {
        if (tileReceiver.Air is not { } receiver || tileSharer.Air is not { } sharer ||
            tileReceiver.AirArchived == null || tileSharer.AirArchived == null)
            return 0f;

        var temperatureDelta = tileReceiver.AirArchived.Temperature - tileSharer.AirArchived.Temperature;
        if (MathF.Abs(temperatureDelta) > Atmospherics.MinimumTemperatureDeltaToConsider)
        {
            var heatCapacity = GetHeatCapacityArchived(tileReceiver);
            var sharerHeatCapacity = GetHeatCapacityArchived(tileSharer);

            if (sharerHeatCapacity > Atmospherics.MinimumHeatCapacity &&
                heatCapacity > Atmospherics.MinimumHeatCapacity)
            {
                var heat = conductionCoefficient * temperatureDelta *
                           (heatCapacity * sharerHeatCapacity / (heatCapacity + sharerHeatCapacity));

                if (!receiver.Immutable)
                {
                    receiver.Temperature =
                        MathF.Abs(MathF.Max(receiver.Temperature - heat / heatCapacity, Atmospherics.TCMB));
                }

                if (!sharer.Immutable)
                {
                    sharer.Temperature =
                        MathF.Abs(MathF.Max(sharer.Temperature + heat / sharerHeatCapacity, Atmospherics.TCMB));
                }
            }
        }

        return sharer.Temperature;
    }

    /// <summary>
    ///     Shares temperature between a gas mixture and an abstract sharer, taking a conduction coefficient into account.
    /// </summary>
    public float TemperatureShare(TileAtmosphere tileReceiver,
        float conductionCoefficient,
        float sharerTemperature,
        float sharerHeatCapacity)
    {
        if (tileReceiver.Air is not { } receiver || tileReceiver.AirArchived == null)
            return 0;

        var temperatureDelta = tileReceiver.AirArchived.Temperature - sharerTemperature;
        if (MathF.Abs(temperatureDelta) > Atmospherics.MinimumTemperatureDeltaToConsider)
        {
            var heatCapacity = GetHeatCapacityArchived(tileReceiver);

            if (sharerHeatCapacity > Atmospherics.MinimumHeatCapacity &&
                heatCapacity > Atmospherics.MinimumHeatCapacity)
            {
                var heat = conductionCoefficient * temperatureDelta *
                           (heatCapacity * sharerHeatCapacity / (heatCapacity + sharerHeatCapacity));

                if (!receiver.Immutable)
                {
                    receiver.Temperature =
                        MathF.Abs(MathF.Max(receiver.Temperature - heat / heatCapacity, Atmospherics.TCMB));
                }

                sharerTemperature =
                    MathF.Abs(MathF.Max(sharerTemperature + heat / sharerHeatCapacity, Atmospherics.TCMB));
            }
        }

        return sharerTemperature;
    }

    #endregion

    #region Multithreaded Impl

    /*
     Multithreading LINDA is a pain in the ass but let's see if we can figure it out.

     LINDA can be multithreaded to begin with because LINDA archives the air *before* doing
     any calculations, making its work order-invariant.

     The downside is that there's a lot of early return shit in LINDA that makes vectorizing it *on top of*
     multithreading it a big pain. But I've done this same thing before in DeltaPressure so let's see how it goes.
     */

    #region Preprocessing & Helpers

    /// <summary>
    /// Archives the <see cref="GasMixture"/> on a <see cref="TileAtmosphere"/>
    /// and its neighbors.
    /// </summary>
    /// <param name="tile">The <see cref="TileAtmosphere"/> to archive.</param>
    /// <param name="cycleCount">The cycle count at which this tile was archived.
    /// Prevents double-archival.</param>
    private void ArchiveDeep(TileAtmosphere tile, int cycleCount)
    {
        Archive(tile, cycleCount);

        // Now move onto neighbors.
        for (var i = 0; i < Atmospherics.Directions; i++)
        {
            var enemyTile = tile.AdjacentTiles[i];
            if (enemyTile?.Air == null)
                continue;
            if (cycleCount <= enemyTile.CurrentCycle)
                continue;
            Archive(enemyTile, cycleCount);
        }
    }

    /// <summary>
    /// Prunes tiles that are ineligible for LINDA processing from the active tiles list.
    /// </summary>
    /// <param name="gridAtmosphere">The <see cref="GridAtmosphereComponent"/> to prune tiles from.</param>
    private void PruneLindaNonEligibleTiles(GridAtmosphereComponent gridAtmosphere)
    {
        foreach (var tile in gridAtmosphere.ActiveTiles)
        {
            if (tile.Air is null)
                RemoveActiveTile(gridAtmosphere, tile);
        }
    }

    /// <summary>
    /// Counts the number of adjacent tiles that air can flow to. Used for determining how much gas to share during LINDA.
    /// </summary>
    /// <param name="tile">The <see cref="TileAtmosphere"/> to count adjacent tiles for.</param>
    /// <returns>The number of adjacent tiles that have air.</returns>
    private static int AdjacentTileCount(TileAtmosphere tile)
    {
        var count = 0;
        for (var i = 0; i < Atmospherics.Directions; i++)
        {
            if (tile.AdjacentBits.IsFlagSet((AtmosDirection)(1 << i)))
                count++;
        }

        return count;
    }

    /// <summary>
    /// Merges two <see cref="ExcitedGroup"/>s if two tiles are eligible for merging
    /// and if they are not the same group already.
    /// Creates new groups and adds tiles to groups as necessary if two eligible tiles are not in any group.
    /// </summary>
    /// <param name="gridAtmosphere">The <see cref="GridAtmosphereComponent"/> to merge groups in.</param>
    private void MergeAndCreateEligibleExcitedGroups(GridAtmosphereComponent gridAtmosphere)
    {
        foreach (var tile in gridAtmosphere.ActiveTiles)
        {
            var shareAir = false;
            for (var i = 0; i < Atmospherics.Directions; i++)
            {
                var enemyTile = tile.AdjacentTiles[i];
                if (enemyTile?.Air == null)
                    continue;

                if (ExcitedGroups && tile.ExcitedGroup != null && enemyTile.ExcitedGroup != null)
                {
                    if (tile.ExcitedGroup != enemyTile.ExcitedGroup)
                    {
                        // Critical section: Merging two active groups.
                        ExcitedGroupMerge(gridAtmosphere, tile.ExcitedGroup, enemyTile.ExcitedGroup);
                    }

                    shareAir = true;
                }
                else
                {
                    // microopt, pass in GasMixtures as they're already confirmed non-null
                    // null check: tiles being processed here were confirmed to have non-null AirArchived.
                    // this is a LINDA-specific opt: pass in regular tiles if refactoring this method for use
                    // generically.
                    Debug.Assert(tile.AirArchived != null,
                        "Encountered null AirArchived on source tile during MergeAndCreateEligibleExcitedGroups!");
                    Debug.Assert(enemyTile.AirArchived != null,
                        "Encountered null AirArchived on adjacent tile during MergeAndCreateEligibleExcitedGroups!");
                    if (CompareExchange(tile.AirArchived, enemyTile.AirArchived) != GasCompareResult.NoExchange)
                    {
                        // Critical section: Adding tiles to the ActiveTiles hashset.
                        AddActiveTile(gridAtmosphere, enemyTile);
                        if (ExcitedGroups)
                        {
                            var excitedGroup = tile.ExcitedGroup;
                            excitedGroup ??= enemyTile.ExcitedGroup;

                            if (excitedGroup == null)
                            {
                                excitedGroup = new ExcitedGroup();
                                gridAtmosphere.ExcitedGroups.Add(excitedGroup);
                            }

                            if (tile.ExcitedGroup == null)
                                ExcitedGroupAddTile(excitedGroup, tile);

                            if (enemyTile.ExcitedGroup == null)
                                ExcitedGroupAddTile(excitedGroup, enemyTile);
                        }

                        shareAir = true;
                    }
                }
            }

            if (shareAir)
            {
                gridAtmosphere.LindaShareAirTiles.Add(tile);
            }
        }
    }

    /// <summary>
    /// Finalizes the results of LINDA processing for a grid.
    /// </summary>
    /// <param name="ent"></param>
    private void FinalizeLindaTileProcessing(
        Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> ent)
    {
        foreach (var tile in ent.Comp1.LindaShareAirTiles)
        {
            for (var i = 0; i < Atmospherics.Directions; i++)
            {
                var direction = (AtmosDirection)(1 << i);

                if (!tile.AdjacentBits.IsFlagSet(direction))
                    continue;
                var enemyTile = tile.AdjacentTiles[i];
                if (enemyTile?.Air == null)
                    continue;

                var difference = tile.LindaInfo.ComputedDifference[i];

                // Monstermos already handles this, so let's not handle it ourselves.
                if (!MonstermosEqualization)
                {
                    // Critical section: HighPressureDelta tile invalidation.
                    if (difference >= 0)
                    {
                        ConsiderPressureDifference(ent.Comp1, tile, direction, difference);
                    }
                    else
                    {
                        ConsiderPressureDifference(ent.Comp1, enemyTile, i.ToOppositeDir(), -difference);
                    }
                }

                // Critical section: Potential reset of an active group's cooldown.
                LastShareCheck(tile);

                if (tile.Air != null)
                    React(tile.Air, tile);

                // Critical section: Tile invalidation adds tiles to hashset.
                InvalidateVisuals(ent, tile);

                var remove = true;

                if (tile.Air!.Temperature > Atmospherics.MinimumTemperatureStartSuperConduction)
                {
                    // Critical section: Superconductivity tiles are added to a hashset.
                    if (ConsiderSuperconductivity(ent.Comp1, tile, true))
                        remove = false;
                }

                // Critical section: Active tiles removed.
                if (ExcitedGroups && tile.ExcitedGroup == null && remove)
                    RemoveActiveTile(ent.Comp1, tile);
            }
        }

        ent.Comp1.LindaShareAirTiles.Clear();
    }

    #endregion

    #region Main Processing

    /// <summary>
    /// Processes a single cell for the parallelized version of LINDA.
    /// </summary>
    /// <param name="ent">The grid entity.</param>
    /// <param name="tile">The tile to process.</param>
    private void ProcessCellParallelSolve(
        Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> ent,
        TileAtmosphere tile)
    {
        var numAdjacentTiles = AdjacentTileCount(tile);
        for (var i = 0; i < Atmospherics.Directions; i++)
        {
            var direction = (AtmosDirection)(1 << i);

            var enemyTile = tile.AdjacentTiles[i];
            if (enemyTile?.Air == null)
                continue;

            var difference = Share(tile, enemyTile, numAdjacentTiles);
        }
    }

    /// <summary>
    /// Calculates the results of LINDA sharing for a span of tiles on a grid.
    /// </summary>
    /// <param name="ent">The grid entity.</param>
    /// <param name="start">The index of the first tile to process in the grid's active tiles list.</param>
    /// <param name="end">The index of the last tile to process in the grid's active tiles list.</param>
    private void VectorizedShare(
        Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> ent,
        int start,
        int end)
    {
        // Alright pain time.
    }
    #endregion

    #endregion
}
