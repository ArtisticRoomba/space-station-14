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
    /// Total number of grids with <see cref="GridAtmosphereComponent"/>.
    /// </summary>
    private static readonly Gauge TotalGridAtmosCompCount = Metrics.CreateGauge(
        "atmosphere_system_grid_atmosphere_component_count",
        "Total number of Atmospherics GridAtmosphereComponents in existence."
    );

    // do this one later, this is kinda important
    // private static readonly Gauge AtmosMaxProcessingTime = Metrics.CreateGauge(
    //     "atmosphere_system_max_processing_time",
    //     "Current CVAR atmos_max_processing_time value. " +
    //     "Time taken in processing stages should never blow past this value in normal operation!"
    // );


    #region ProcessingStates
    /*
     Okay so processing states are a bit annoying to track.
     Each processing state run is for each grid. Attempting to track them on a grid level
     is something that I don't want to deal with right now - this is best
     saved for when processing states are interfaced.

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

    #endregion

    #endregion
}
