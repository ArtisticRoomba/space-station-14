using System.Runtime.CompilerServices;
using Content.Shared.Atmos;

namespace Content.Server.Atmos;

/// <summary>
/// A struct that holds a list of adjacent tiles to a tile, as well as presenting that list to Atmospherics by-index
/// that represents blocked directions as a null tile.
/// </summary>
/// <para>
/// This is effectively a fancy data wrapper that allows us to take advantage of the memory we're allocating for the refs
/// while still presenting a nullable list of adjacent tiles. So fast adjacent lookups even for tiles that are fully air blocked.
/// </para>
public struct TileAdjacencyHolder
{
    /// <summary>
    /// Tiles that are adjacent to this tile. The index of each tile corresponds to the direction in Atmospherics.Directions.
    /// </summary>
    /// <remarks>This inline array contains all adjacent tiles, even if they are air-blocked.
    /// If you would like to query for tiles with null being returned when air cannot flow to the tile, access this struct by-index.</remarks>
    public TileAtmosphereInlineArray AdjacentTiles;

    /// <summary>
    /// Current directions that air can flow to from this tile.
    /// This is a combination of the airtight blocks from this tile and neighboring tiles.
    /// </summary>
    public AtmosDirection AdjacentBits;

    /// <summary>
    /// Gets the adjacent tile in the given direction, or null if air cannot flow to that tile.
    /// </summary>
    /// <param name="index">Direction index. Must be between 0 and Atmospherics.Directions - 1.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when index is out of range.</exception>
    public TileAtmosphere? this[int index]
    {
        get
        {
            if (index is < 0 or >= Atmospherics.Directions)
                throw new ArgumentOutOfRangeException(nameof(index), $"Index must be between 0 and {Atmospherics.Directions - 1}");

            var tile = AdjacentTiles[index];
            var direction = (AtmosDirection)(1 << index);
            if (tile != null && tile.AdjacentBits.IsFlagSet(direction))
                return tile;

            return null;
        }
    }

    /// <summary>
    /// Gets the adjacent tile in the given direction, or null if air cannot flow to that tile.
    /// </summary>
    /// <param name="direction">Direction. Must be a cardinal direction (North, South, East, West).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an invalid direction is given (a non-cardinal direction)</exception>
    public TileAtmosphere? this[AtmosDirection direction]
    {
        get
        {
            if ((int)direction is < 0 or >= Atmospherics.Directions)
                throw new ArgumentOutOfRangeException(nameof(direction), "Direction must be a cardinal direction (North, South, East, West)");

            var tile = AdjacentTiles[(int)direction >> 1];
            if (tile != null && tile.AdjacentBits.IsFlagSet(direction))
                return tile;

            return null;
        }
    }



    /// <summary>
    /// Presents the adjacent tiles as a <see cref="TileAtmosphereInlineArray"/>, with null representing tiles that air cannot flow to.
    /// </summary>
    public TileAtmosphereInlineArray AsAirBlocked()
    {
        // make a copy of the adjacent tiles and then set blocked directions to null.
        // this will return a copy with nulled out refs for directions that are blocked, without modifying the original data.
        var tempArray = AdjacentTiles;
        for (var i = 0; i < Atmospherics.Directions; i++)
        {
            var tile = tempArray[i];
            var direction = (AtmosDirection)(1 << i);
            if (tile != null && !tile.AdjacentBits.IsFlagSet(direction))
            {
                tempArray[i] = null;
            }
        }
        return tempArray;
    }
}

/// <summary>
/// An inline array of refs to adjacent tiles.
/// Used for better cache locality and preventing array allocations.
/// </summary>
[InlineArray(Atmospherics.Directions)]
public struct TileAtmosphereInlineArray
{
    private TileAtmosphere? _tile0;
}
