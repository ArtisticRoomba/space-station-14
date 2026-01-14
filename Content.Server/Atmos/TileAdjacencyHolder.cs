using System.Collections;
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
/// <remarks>Do not confuse with indexing <see cref="TileAdjacencyHolder"/> by-index between
/// indexing <see cref="AdjacentTiles"/> by-index.</remarks>
public struct TileAdjacencyHolder() : IEnumerable<TileAtmosphere?>
{
    /// <summary>
    /// Tiles that are adjacent to this tile. The index of each tile corresponds to the direction in Atmospherics.Directions.
    /// </summary>
    /// <remarks>This inline array contains all adjacent tiles, even if they are air-blocked.
    /// If you would like to query for tiles with null being returned when air cannot flow to the tile, access this struct by-index.</remarks>
    public TileAtmosphereInlineArray AdjacentTiles = new();

    /// <summary>
    /// Current directions that air can flow to from this tile.
    /// This is a combination of the airtight blocks from this tile and neighboring tiles.
    /// </summary>
    /// <remarks>Since this is a struct, it would be best to use the helper methods
    /// provided to access this field (see <see cref="OrFlag"/> and <see cref="AndFlag"/>).</remarks>
    public AtmosDirection AdjacentBits { get; private set; } = AtmosDirection.Invalid;

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
            if (AdjacentBits.IsFlagSet(direction))
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
            var direction = (AtmosDirection)(1 << i);
            if (AdjacentBits.IsFlagSet(direction))
            {
                tempArray[i] = null;
            }
        }

        return tempArray;
    }

    /// <summary>
    /// Gets an enumerator that iterates through the adjacent tiles, with null representing tiles that air cannot flow to.
    /// </summary>
    /// <returns>An enumerator that iterates through the adjacent tiles.</returns>
    public IEnumerator<TileAtmosphere?> GetEnumerator()
    {
        for (var i = 0; i < Atmospherics.Directions; i++)
        {
            yield return this[i];
        }
    }

    /// <summary>
    /// Gets an enumerator that iterates through the adjacent tiles, with null representing tiles that air cannot flow to.
    /// </summary>
    /// <returns>An enumerator that iterates through the adjacent tiles.</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        // evil IEnumerator object alloc
        return GetEnumerator();
    }

    /// <summary>
    /// Presents the adjacent tiles as an array, with null representing tiles that air cannot flow to.
    /// </summary>
    /// <returns>An array of adjacent tiles, with null representing tiles that air cannot flow to.</returns>
    /// <remarks>Please design your code to use an InlineArray instead of doing this.</remarks>
    [Obsolete("Atmospherics should be designed to use the TileAtmosphereInlineArray directly for tile adjacency operations.")]
    public TileAtmosphere?[] ToArray()
    {
        var array = new TileAtmosphere?[Atmospherics.Directions];
        for (var i = 0; i < Atmospherics.Directions; i++)
        {
            array[i] = this[i];
        }
        return array;
    }

    #region Flag Setters

    // ooo you love structs dont you

    /// <summary>
    /// ORs the given direction flag into the current adjacent bits.
    /// </summary>
    /// <param name="direction">The direction to OR the flag for.</param>
    public void OrFlag(AtmosDirection direction)
    {
        AdjacentBits |= direction;
    }

    /// <summary>
    /// ANDs the given direction flag into the current adjacent bits.
    /// </summary>
    /// <param name="direction">The direction to AND the flag for.</param>
    public void AndFlag(AtmosDirection direction)
    {
        AdjacentBits &= direction;
    }

    /// <summary>
    /// Sets the adjacent bits to invalid.
    /// </summary>
    public void SetInvalid()
    {
        AdjacentBits = AtmosDirection.Invalid;
    }

    #endregion
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
