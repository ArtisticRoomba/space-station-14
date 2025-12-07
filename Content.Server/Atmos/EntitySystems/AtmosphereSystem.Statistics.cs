using Content.Server.Atmos.Components;
using Prometheus;
using Robust.Shared.Profiling;

namespace Content.Server.Atmos.EntitySystems;

public sealed partial class AtmosphereSystem
{
    /*
     Partial class for gathering a bunch of time and allocation statistics about Atmospherics.

     Generally speaking I'd like to confirm that whatever I do is actually working in a live environment.
     I also want a massive Grafana dashboard that I can stare at :)

     Because ProcessingStates are not interfaced there's a massive amount of boilerplate here.
     I hate it.
     */

    /// <summary>
    /// Resets all statistics for the current atmos subtick.
    /// This is different from an entire update - this measures
    /// the amount of time we have spent in one call in each processing state
    /// while executing <see cref="AtmosphereSystem.Update"/>.
    /// </summary>
    private void ResetSubtickStatistics()
    {
        ProcessRevalidateCurrentCount.Set(0);
        TileEqualizeCurrentCount.Set(0);
        ProcessActiveTilesCurrentCount.Set(0);
        ProcessExcitedGroupsCurrentCount.Set(0);
        ProcessHighPressureCurrentCount.Set(0);
        ProcessDeltaPressureCurrentCount.Set(0);
        ProcessDeltaPressureDamageCurrentCount.Set(0);
        ProcessHotspotsCurrentCount.Set(0);
        ProcessSuperconductivityCurrentCount.Set(0);
        ProcessPipeNetCurrentCount.Set(0);
        ProcessAtmosDevicesCurrentCount.Set(0);
    }

    /// <summary>
    /// Logs all time and allocation statistics for the current atmos subtick.
    /// This is different from an entire update - this measures
    /// the amount of time we have spent in one call in each processing state
    /// while executing <see cref="AtmosphereSystem.Update"/>.
    /// </summary>
    private void LogSubtickStatistics()
    {

    }

    /// <summary>
    /// Total number of grids with <see cref="GridAtmosphereComponent"/>.
    /// </summary>
    private static readonly Gauge TotalGridAtmosCompCount = Metrics.CreateGauge(
        "atmosphere_system_grid_atmosphere_component_count_processing",
        "Total number of Atmospherics GridAtmosphereComponents scheduled for processing."
    );

    private static readonly Gauge AtmosMaxProcessingTime = Metrics.CreateGauge(
        "atmosphere_system_max_processing_time",
        "Current CVAR atmos_max_processing_time value. " +
        "Time taken in processing stages should never blow past this value in normal operation!"
    );


    #region ProcessingStates
    /*
     Okay so processing states are a bit annoying to track.
     Each processing state run is for each grid. Attempting to track them on a grid level
     is something that I don't want to deal with right now - this is best
     saved for when processing states are cleaned up.

     Until then, we should track two sets of statistics for each processing state:
        1. Total time and allocations for the entire AtmosTick update.
        2. Time and allocations for the current EntitySystem.Update call.

     Each processing state should also have its unique set of metrics relevant to what it does.
    */

    #region ProcessRevalidate

    /// <summary>
    /// Stopwatch to time the process revalidate stage and track allocations.
    /// </summary>
    private ProfSampler _processRevalidateSw;

    private static readonly Gauge ProcessRevalidateCurrentCount = Metrics.CreateGauge(
        "atmosphere_system_process_revalidate_current_count",
        "Number of tiles processed for revalidation this update run."
    );

    private static readonly Gauge ProcessRevalidateTime = Metrics.CreateGauge(
        "atmosphere_system_process_revalidate_time_ticks",
        "Time taken to process revalidation of invalid tiles this update run."
    );

    private static readonly Gauge ProcessRevalidateAlloc = Metrics.CreateGauge(
        "atmosphere_system_process_revalidate_alloc_bytes",
        "Allocations made during processing revalidation of invalid tiles this update run."
    );

    private static readonly Gauge TotalProcessRevalidateCount = Metrics.CreateGauge(
        "atmosphere_system_process_revalidate_count",
        "Number of tiles scheduled for revalidation for the entire update."
    );

    private static readonly Gauge TotalProcessRevalidateTime = Metrics.CreateGauge(
        "atmosphere_system_total_process_revalidate_time_ticks",
        "Total time taken to process revalidation of invalid tiles for the entire update."
    );

    private static readonly Gauge TotalProcessRevalidateAlloc = Metrics.CreateGauge(
        "atmosphere_system_total_process_revalidate_alloc_bytes",
        "Total allocations made during processing revalidation of invalid tiles for the entire update."
    );

    private void LogProcessRevalidateStatistics()
    {
        var data = ProfData.TimeAlloc(_processRevalidateSw).TimeAllocSample;
        ProcessRevalidateTime.Set(data.Time);
        ProcessRevalidateAlloc.Set(data.Alloc);
        TotalProcessRevalidateTime.Inc(data.Time);
        TotalProcessRevalidateAlloc.Inc(data.Alloc);
    }

    private void ResetProcessRevalidateStatistics()
    {
        _processRevalidateSw.Reset();
        ProcessRevalidateTime.Set(0);
        ProcessRevalidateAlloc.Set(0);
        TotalProcessRevalidateTime.Set(0);
        TotalProcessRevalidateAlloc.Set(0);
    }

    #endregion

    #region TileEqualize

    private ProfSampler _tileEqualizeSw;

    private static readonly Gauge TileEqualizeCurrentCount = Metrics.CreateGauge(
        "atmosphere_system_tile_equalize_current_count",
        "Number of tiles processed for equalization this update run."
    );

    private static readonly Gauge TileEqualizeTime = Metrics.CreateGauge(
        "atmosphere_system_tile_equalize_time_ticks",
        "Time taken to process equalization of tiles this update run."
    );

    private static readonly Gauge TileEqualizeAlloc = Metrics.CreateGauge(
        "atmosphere_system_tile_equalize_alloc_bytes",
        "Allocations made during processing equalization of tiles this update run."
    );

    private static readonly Gauge TotalTileEqualizeCount = Metrics.CreateGauge(
        "atmosphere_system_total_tile_equalize_count",
        "Number of tiles processed for equalization for the entire update."
    );

    private static readonly Gauge TotalTileEqualizeTime = Metrics.CreateGauge(
        "atmosphere_system_total_tile_equalize_time_ticks",
        "Total time taken to process equalization of tiles for the entire update."
    );

    private static readonly Gauge TotalTileEqualizeAlloc = Metrics.CreateGauge(
        "atmosphere_system_total_tile_equalize_alloc_bytes",
        "Total allocations made during processing equalization of tiles for the entire update."
    );

    private void LogTileEqualizeStatistics()
    {
        var data = ProfData.TimeAlloc(_tileEqualizeSw).TimeAllocSample;
        TileEqualizeTime.Set(data.Time);
        TileEqualizeAlloc.Set(data.Alloc);
        TotalTileEqualizeTime.Inc(data.Time);
        TotalTileEqualizeAlloc.Inc(data.Alloc);
    }

    private void ResetTileEqualizeStatistics()
    {
        _tileEqualizeSw.Reset();
        TileEqualizeTime.Set(0);
        TileEqualizeAlloc.Set(0);
        TotalTileEqualizeTime.Set(0);
        TotalTileEqualizeAlloc.Set(0);
    }

    #endregion

    #region ProcessActiveTiles

    private ProfSampler _processActiveTilesSw;

    private static readonly Gauge ProcessActiveTilesCurrentCount = Metrics.CreateGauge(
        "atmosphere_system_process_active_tiles_current_count",
        "Number of active tiles processed this update run."
    );

    private static readonly Gauge ProcessActiveTilesTime = Metrics.CreateGauge(
        "atmosphere_system_process_active_tiles_time_ticks",
        "Time taken to process active tiles this update run."
    );

    private static readonly Gauge ProcessActiveTilesAlloc = Metrics.CreateGauge(
        "atmosphere_system_process_active_tiles_alloc_bytes",
        "Allocations made during processing active tiles this update run."
    );

    private static readonly Gauge TotalProcessActiveTilesCount = Metrics.CreateGauge(
        "atmosphere_system_total_process_active_tiles_count",
        "Number of active tiles processed for the entire update."
    );

    private static readonly Gauge TotalProcessActiveTilesTime = Metrics.CreateGauge(
        "atmosphere_system_total_process_active_tiles_time_ticks",
        "Total time taken to process active tiles for the entire update."
    );

    private static readonly Gauge TotalProcessActiveTilesAlloc = Metrics.CreateGauge(
        "atmosphere_system_total_process_active_tiles_alloc_bytes",
        "Total allocations made during processing active tiles for the entire update."
    );

    private void LogProcessActiveTilesStatistics()
    {
        var data = ProfData.TimeAlloc(_processActiveTilesSw).TimeAllocSample;
        ProcessActiveTilesTime.Set(data.Time);
        ProcessActiveTilesAlloc.Set(data.Alloc);
        TotalProcessActiveTilesTime.Inc(data.Time);
        TotalProcessActiveTilesAlloc.Inc(data.Alloc);
    }

    private void ResetProcessActiveTilesStatistics()
    {
        _processActiveTilesSw.Reset();
        ProcessActiveTilesTime.Set(0);
        ProcessActiveTilesAlloc.Set(0);
        TotalProcessActiveTilesTime.Set(0);
        TotalProcessActiveTilesAlloc.Set(0);
    }

    #endregion

    #region ProcessExcitedGroups

    private ProfSampler _processExcitedGroupsSw;

    private static readonly Gauge ExcitedGroupsDissolvedCount = Metrics.CreateGauge(
        "atmosphere_system_excited_groups_dissolved_count",
        "Number of excited groups that have been dissolved this atmostick. " +
        "These are the groups that have been equalized and deactivated."
    );

    private static readonly Gauge ExcitedGroupsDisposedCount = Metrics.CreateGauge(
        "atmosphere_system_excited_groups_disposed_count",
        "Number of excited groups that have been disposed this atmostick " +
        "without equalization (because they were too close in pressure)."
    );

    private static readonly Gauge ProcessExcitedGroupsCurrentCount = Metrics.CreateGauge(
        "atmosphere_system_process_excited_groups_current_count",
        "Number of excited groups processed this update run."
    );

    private static readonly Gauge ProcessExcitedGroupsTime = Metrics.CreateGauge(
        "atmosphere_system_process_excited_groups_time_ticks",
        "Time taken to process excited groups this update run."
    );

    private static readonly Gauge ProcessExcitedGroupsAlloc = Metrics.CreateGauge(
        "atmosphere_system_process_excited_groups_alloc_bytes",
        "Allocations made during processing excited groups this update run."
    );

    private static readonly Gauge TotalProcessExcitedGroupsCount = Metrics.CreateGauge(
        "atmosphere_system_total_process_excited_groups_count",
        "Number of excited groups processed for the entire update."
    );

    private static readonly Gauge TotalProcessExcitedGroupsTime = Metrics.CreateGauge(
        "atmosphere_system_total_process_excited_groups_time_ticks",
        "Total time taken to process excited groups for the entire update."
    );

    private static readonly Gauge TotalProcessExcitedGroupsAlloc = Metrics.CreateGauge(
        "atmosphere_system_total_process_excited_groups_alloc_bytes",
        "Total allocations made during processing excited groups for the entire update."
    );

    private void LogProcessExcitedGroupsStatistics()
    {
        var data = ProfData.TimeAlloc(_processExcitedGroupsSw).TimeAllocSample;
        ProcessExcitedGroupsTime.Set(data.Time);
        ProcessExcitedGroupsAlloc.Set(data.Alloc);
        TotalProcessExcitedGroupsTime.Inc(data.Time);
        TotalProcessExcitedGroupsAlloc.Inc(data.Alloc);
    }

    private void ResetProcessExcitedGroupsStatistics()
    {
        _processExcitedGroupsSw.Reset();
        ProcessExcitedGroupsTime.Set(0);
        ProcessExcitedGroupsAlloc.Set(0);
        TotalProcessExcitedGroupsTime.Set(0);
        TotalProcessExcitedGroupsAlloc.Set(0);
        ExcitedGroupsDissolvedCount.Set(0);
        ExcitedGroupsDisposedCount.Set(0);
    }

    #endregion

    #region ProcessHighPressureDelta

    private ProfSampler _processHighPressureSw;

    private static readonly Gauge ProcessHighPressureCurrentCount = Metrics.CreateGauge(
        "atmosphere_system_process_high_pressure_current_count",
        "Number of tiles processed for high pressure delta this update run."
    );

    private static readonly Gauge ProcessHighPressureTime = Metrics.CreateGauge(
        "atmosphere_system_process_high_pressure_time_ticks",
        "Time taken to process high pressure delta this update run."
    );

    private static readonly Gauge ProcessHighPressureAlloc = Metrics.CreateGauge(
        "atmosphere_system_process_high_pressure_alloc_bytes",
        "Allocations made during processing high pressure delta this update run."
    );

    private static readonly Gauge TotalProcessHighPressureCount = Metrics.CreateGauge(
        "atmosphere_system_total_process_high_pressure_count",
        "Number of tiles processed for high pressure delta for the entire update."
    );

    private static readonly Gauge TotalProcessHighPressureTime = Metrics.CreateGauge(
        "atmosphere_system_total_process_high_pressure_time_ticks",
        "Total time taken to process high pressure delta for the entire update."
    );

    private static readonly Gauge TotalProcessHighPressureAlloc = Metrics.CreateGauge(
        "atmosphere_system_total_process_high_pressure_alloc_bytes",
        "Total allocations made during processing high pressure delta for the entire update."
    );

    private void LogProcessHighPressureStatistics()
    {
        var data = ProfData.TimeAlloc(_processHighPressureSw).TimeAllocSample;
        ProcessHighPressureTime.Set(data.Time);
        ProcessHighPressureAlloc.Set(data.Alloc);
        TotalProcessHighPressureTime.Inc(data.Time);
        TotalProcessHighPressureAlloc.Inc(data.Alloc);
    }

    private void ResetProcessHighPressureStatistics()
    {
        _processHighPressureSw.Reset();
        ProcessHighPressureTime.Set(0);
        ProcessHighPressureAlloc.Set(0);
        TotalProcessHighPressureTime.Set(0);
        TotalProcessHighPressureAlloc.Set(0);
    }

    #endregion

    #region ProcessDeltaPressure

    private ProfSampler _processDeltaPressureSw;

    private static readonly Gauge ProcessDeltaPressureDamageDealtCount = Metrics.CreateGauge(
        "atmosphere_system_process_delta_pressure_damage_dealt_count",
        "Number of entities that took damage from delta pressure this atmostick."
    );

    private static readonly Gauge ProcessDeltaPressureCurrentCount = Metrics.CreateGauge(
        "atmosphere_system_process_delta_pressure_current_count",
        "Number of delta pressure entities processed this update run."
    );

    private static readonly Gauge ProcessDeltaPressureDamageCurrentCount = Metrics.CreateGauge(
        "atmosphere_system_process_delta_pressure_damage_current_count",
        "Number of delta pressure entities processed for damage this update run."
    );

    private static readonly Gauge ProcessDeltaPressureTime = Metrics.CreateGauge(
        "atmosphere_system_process_delta_pressure_time_ticks",
        "Time taken to process delta pressure damage this update run."
    );

    private static readonly Gauge ProcessDeltaPressureAlloc = Metrics.CreateGauge(
        "atmosphere_system_process_delta_pressure_alloc_bytes",
        "Allocations made during processing delta pressure damage this update run."
    );

    private static readonly Gauge TotalProcessDeltaPressureCount = Metrics.CreateGauge(
        "atmosphere_system_total_process_delta_pressure_count",
        "Number of delta pressure entities processed for the entire update."
    );

    private static readonly Gauge TotalProcessDeltaPressureDamageCount = Metrics.CreateGauge(
        "atmosphere_system_total_process_delta_pressure_damage_count",
        "Number of delta pressure entities processed for damage for the entire update."
    );

    private static readonly Gauge TotalProcessDeltaPressureTime = Metrics.CreateGauge(
        "atmosphere_system_total_process_delta_pressure_time_ticks",
        "Total time taken to process delta pressure damage for the entire update."
    );

    private static readonly Gauge TotalProcessDeltaPressureAlloc = Metrics.CreateGauge(
        "atmosphere_system_total_process_delta_pressure_alloc_bytes",
        "Total allocations made during processing delta pressure damage for the entire update."
    );

    private void LogProcessDeltaPressureStatistics()
    {
        var data = ProfData.TimeAlloc(_processDeltaPressureSw).TimeAllocSample;
        ProcessDeltaPressureTime.Set(data.Time);
        ProcessDeltaPressureAlloc.Set(data.Alloc);
        TotalProcessDeltaPressureTime.Inc(data.Time);
        TotalProcessDeltaPressureAlloc.Inc(data.Alloc);
    }

    private void ResetProcessDeltaPressureStatistics()
    {
        _processDeltaPressureSw.Reset();
        ProcessDeltaPressureTime.Set(0);
        ProcessDeltaPressureAlloc.Set(0);
        TotalProcessDeltaPressureTime.Set(0);
        TotalProcessDeltaPressureAlloc.Set(0);
        ProcessDeltaPressureDamageDealtCount.Set(0);
    }

    #endregion

    #region ProcessHotspots

    private ProfSampler _processHotspotsSw;

    private static readonly Gauge ProcessHotspotsCurrentCount = Metrics.CreateGauge(
        "atmosphere_system_process_hotspots_current_count",
        "Number of hotspot tiles processed this update run."
    );

    private static readonly Gauge ProcessHotspotsTime = Metrics.CreateGauge(
        "atmosphere_system_process_hotspots_time_ticks",
        "Time taken to process hotspot tiles this update run."
    );

    private static readonly Gauge ProcessHotspotsAlloc = Metrics.CreateGauge(
        "atmosphere_system_process_hotspots_alloc_bytes",
        "Allocations made during processing hotspot tiles this update run."
    );

    private static readonly Gauge TotalProcessHotspotsCount = Metrics.CreateGauge(
        "atmosphere_system_total_process_hotspots_count",
        "Number of hotspot tiles processed for the entire update."
    );

    private static readonly Gauge TotalProcessHotspotsTime = Metrics.CreateGauge(
        "atmosphere_system_total_process_hotspots_time_ticks",
        "Total time taken to process hotspot tiles for the entire update."
    );

    private static readonly Gauge TotalProcessHotspotsAlloc = Metrics.CreateGauge(
        "atmosphere_system_total_process_hotspots_alloc_bytes",
        "Total allocations made during processing hotspot tiles for the entire update."
    );

    private void LogProcessHotspotsStatistics()
    {
        var data = ProfData.TimeAlloc(_processHotspotsSw).TimeAllocSample;
        ProcessHotspotsTime.Set(data.Time);
        ProcessHotspotsAlloc.Set(data.Alloc);
        TotalProcessHotspotsTime.Inc(data.Time);
        TotalProcessHotspotsAlloc.Inc(data.Alloc);
    }

    private void ResetProcessHotspotsStatistics()
    {
        _processHotspotsSw.Reset();
        ProcessHotspotsTime.Set(0);
        ProcessHotspotsAlloc.Set(0);
        TotalProcessHotspotsTime.Set(0);
        TotalProcessHotspotsAlloc.Set(0);
    }

    #endregion

    #region ProcessSuperconductivity

    private ProfSampler _processSuperconductivitySw;

    private static readonly Gauge ProcessSuperconductivityCurrentCount = Metrics.CreateGauge(
        "atmosphere_system_process_superconductivity_current_count",
        "Number of superconductivity tiles processed this update run."
    );

    private static readonly Gauge ProcessSuperconductivityTime = Metrics.CreateGauge(
        "atmosphere_system_process_superconductivity_time_ticks",
        "Time taken to process superconductivity tiles this update run."
    );

    private static readonly Gauge ProcessSuperconductivityAlloc = Metrics.CreateGauge(
        "atmosphere_system_process_superconductivity_alloc_bytes",
        "Allocations made during processing superconductivity tiles this update run."
    );

    private static readonly Gauge TotalProcessSuperconductivityCount = Metrics.CreateGauge(
        "atmosphere_system_total_process_superconductivity_count",
        "Number of superconductivity tiles processed for the entire update."
    );

    private static readonly Gauge TotalProcessSuperconductivityTime = Metrics.CreateGauge(
        "atmosphere_system_total_process_superconductivity_time_ticks",
        "Total time taken to process superconductivity tiles for the entire update."
    );

    private static readonly Gauge TotalProcessSuperconductivityAlloc = Metrics.CreateGauge(
        "atmosphere_system_total_process_superconductivity_alloc_bytes",
        "Total allocations made during processing superconductivity tiles for the entire update."
    );

    private void LogProcessSuperconductivityStatistics()
    {
        var data = ProfData.TimeAlloc(_processSuperconductivitySw).TimeAllocSample;
        ProcessSuperconductivityTime.Set(data.Time);
        ProcessSuperconductivityAlloc.Set(data.Alloc);
        TotalProcessSuperconductivityTime.Inc(data.Time);
        TotalProcessSuperconductivityAlloc.Inc(data.Alloc);
    }

    private void ResetProcessSuperconductivityStatistics()
    {
        _processSuperconductivitySw.Reset();
        ProcessSuperconductivityTime.Set(0);
        ProcessSuperconductivityAlloc.Set(0);
        TotalProcessSuperconductivityTime.Set(0);
        TotalProcessSuperconductivityAlloc.Set(0);
    }

    #endregion

    #region ProcessPipeNet

    private ProfSampler _processPipeNetSw;

    private static readonly Gauge ProcessPipeNetCurrentCount = Metrics.CreateGauge(
        "atmosphere_system_process_pipenet_current_count",
        "Number of pipe nets processed this update run."
    );

    private static readonly Gauge ProcessPipeNetTime = Metrics.CreateGauge(
        "atmosphere_system_process_pipenet_time_ticks",
        "Time taken to process pipe nets this update run."
    );

    private static readonly Gauge ProcessPipeNetAlloc = Metrics.CreateGauge(
        "atmosphere_system_process_pipenet_alloc_bytes",
        "Allocations made during processing pipe nets this update run."
    );

    private static readonly Gauge TotalProcessPipeNetCount = Metrics.CreateGauge(
        "atmosphere_system_total_process_pipenet_count",
        "Number of pipe nets processed for the entire update."
    );

    private static readonly Gauge TotalProcessPipeNetTime = Metrics.CreateGauge(
        "atmosphere_system_total_process_pipenet_time_ticks",
        "Total time taken to process pipe nets for the entire update."
    );

    private static readonly Gauge TotalProcessPipeNetAlloc = Metrics.CreateGauge(
        "atmosphere_system_total_process_pipenet_alloc_bytes",
        "Total allocations made during processing pipe nets for the entire update."
    );

    private void LogProcessPipeNetStatistics()
    {
        var data = ProfData.TimeAlloc(_processPipeNetSw).TimeAllocSample;
        ProcessPipeNetTime.Set(data.Time);
        ProcessPipeNetAlloc.Set(data.Alloc);
        TotalProcessPipeNetTime.Inc(data.Time);
        TotalProcessPipeNetAlloc.Inc(data.Alloc);
    }

    private void ResetProcessPipeNetStatistics()
    {
        _processPipeNetSw.Reset();
        ProcessPipeNetTime.Set(0);
        ProcessPipeNetAlloc.Set(0);
        TotalProcessPipeNetTime.Set(0);
        TotalProcessPipeNetAlloc.Set(0);
    }

    #endregion

    #region ProcessAtmosDevices

    private ProfSampler _processAtmosDevicesSw;

    private static readonly Gauge ProcessAtmosDevicesCurrentCount = Metrics.CreateGauge(
        "atmosphere_system_process_atmos_devices_current_count",
        "Number of atmos devices processed this update run."
    );

    private static readonly Gauge ProcessAtmosDevicesTime = Metrics.CreateGauge(
        "atmosphere_system_process_atmos_devices_time_ticks",
        "Time taken to process atmos devices this update run."
    );

    private static readonly Gauge ProcessAtmosDevicesAlloc = Metrics.CreateGauge(
        "atmosphere_system_process_atmos_devices_alloc_bytes",
        "Allocations made during processing atmos devices this update run."
    );

    private static readonly Gauge TotalProcessAtmosDevicesCount = Metrics.CreateGauge(
        "atmosphere_system_total_process_atmos_devices_count",
        "Number of atmos devices processed for the entire update."
    );

    private static readonly Gauge TotalProcessAtmosDevicesTime = Metrics.CreateGauge(
        "atmosphere_system_total_process_atmos_devices_time_ticks",
        "Total time taken to process atmos devices for the entire update."
    );

    private static readonly Gauge TotalProcessAtmosDevicesAlloc = Metrics.CreateGauge(
        "atmosphere_system_total_process_atmos_devices_alloc_bytes",
        "Total allocations made during processing atmos devices for the entire update."
    );

    private void LogProcessAtmosDevicesStatistics()
    {
        var data = ProfData.TimeAlloc(_processAtmosDevicesSw).TimeAllocSample;
        ProcessAtmosDevicesTime.Set(data.Time);
        ProcessAtmosDevicesAlloc.Set(data.Alloc);
        TotalProcessAtmosDevicesTime.Inc(data.Time);
        TotalProcessAtmosDevicesAlloc.Inc(data.Alloc);
    }

    private void ResetProcessAtmosDevicesStatistics()
    {
        _processAtmosDevicesSw.Reset();
        ProcessAtmosDevicesTime.Set(0);
        ProcessAtmosDevicesAlloc.Set(0);
        TotalProcessAtmosDevicesTime.Set(0);
        TotalProcessAtmosDevicesAlloc.Set(0);
    }

    #endregion

    #endregion
}
