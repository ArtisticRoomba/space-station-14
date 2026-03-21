using System.Runtime.CompilerServices;
using Content.Shared.Atmos.Maths;
using JetBrains.Annotations;

namespace Content.Shared.Atmos.Subsystems.Airtight;

/// <summary>
/// Helper class for airtight related operations.
/// </summary>
public static class AirtightHelpers
{
    /// <summary>
    /// Gets the inverse of the given set directions.
    /// This is commonly used to translate blocked directions to airflow directions.
    /// </summary>
    /// <param name="direction">The <see cref="AtmosDirection"/> to invert.</param>
    /// <returns>The inverse of the given <see cref="AtmosDirection"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [PublicAPI]
    public static AtmosDirection InvertSetDirections(this AtmosDirection direction)
    {
        // Yep, that's right!
        return ~direction;
    }

    /// <summary>
    /// Takes in a number of <see cref="AtmosDirection"/>s
    /// and combines them into a single <see cref="AtmosDirection"/>.
    /// </summary>
    /// <param name="directions">The <see cref="AtmosDirection"/>s to combine.</param>
    /// <returns>The combined <see cref="AtmosDirection"/>.</returns>
    [PublicAPI]
    public static AtmosDirection CombineDirections(params AtmosDirection[] directions)
    {
        var combined = AtmosDirection.Invalid;
        foreach (var direction in directions)
        {
            combined |= direction;
        }
        return combined;
    }

    /// <summary>
    /// Builds a <see cref="TileAirtightData"/> from a number of <see cref="EntityAirtightData"/>s.
    /// </summary>
    /// <param name="data">The <see cref="EntityAirtightData"/>s to combine.</param>
    /// <returns>The combined <see cref="TileAirtightData"/>.</returns>
    [PublicAPI]
    public static TileAirtightData BuildTileAirtightData(params EntityAirtightData[] data)
    {
        var combined = new TileAirtightData();
        foreach (var entityData in data)
        {
            combined.AdjacentAirflowDirections |= entityData.CurrentAirBlockedDirections.InvertSetDirections();
            combined.NoAirWhenFullyAirBlocked |= entityData.NoAirWhenFullyAirBlocked;
            combined.FixVacuum |= entityData.FixVacuum;
        }
        return combined;
    }
}
