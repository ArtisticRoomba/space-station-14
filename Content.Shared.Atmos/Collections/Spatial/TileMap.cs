using System.Diagnostics.CodeAnalysis;
using Content.Shared.Atmos.Maths;
using JetBrains.Annotations;

namespace Content.Shared.Atmos.Collections.Spatial;

/// <summary>
/// Datastructure intended for mapping a single <see cref="Vector2i"/>
/// position to a value of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The datatype to store.</typeparam>
/// <para>This is effectively leveraging both
/// <see cref="ChunkMap{T}"/> and <see cref="MortonArray{T}"/>
/// to create a chunked tilemap that preserves data locality while still having
/// a reasonably fast index.</para>
/// <para>This is an abstracted datastructure purely for easily testing
/// tile simulation in a vacuum. For datastructures that are intended to be shared and synced
/// between client and server, the client would instead need to keep track of Morton arrays on
/// an entity that is physically on the grid and loaded/unloaded by PVS,
/// with the <see cref="TileMap{T}"/>'s <see cref="ChunkMap{T}"/> pointing to entities maybe.
/// This entity would then be stored on a <see cref="ChunkMap{T}"/> or something similar.</para>
/// <para>The server, however, can get away with just using this thing willy nilly.</para>
public sealed class TileMap<T>
{
    /// <summary>
    /// Underlying storage mapping chunk coordinates to their chunk value.
    /// </summary>
    private readonly ChunkMap<MortonArray<T>>? _chunks;

    /// <summary>
    /// Bitmask for converting global tile coordinates to chunk-local coordinates.
    /// For power-of-two chunk sizes, <c>tile &amp; (ChunkSize - 1)</c> extracts the
    /// low bits, which are exactly the in-chunk offset.
    /// </summary>
    /// <example>For a chunk size 16: x=-1 -> 15, x=16 -> 0, x=17 -> 1.</example>
    private readonly int _chunkMask;

    /// <summary>
    /// Edge length of each chunk in tiles.
    /// </summary>
    public int ChunkSize => _chunks?.ChunkSize ?? 0;

    /// <summary>
    /// Number of chunks currently allocated.
    /// </summary>
    public int ChunkCount => _chunks?.ChunkCount ?? 0;

    /// <summary>
    /// Creates a new <see cref="TileMap{T}"/> with the desired chunk size.
    /// Chunk size must be a positive power of two so bitmasking can map world
    /// coordinates to valid local coordinates for <see cref="MortonArray{T}"/>.
    /// Validation is enforced by <see cref="ChunkMap{T}"/>.
    /// </summary>
    /// <param name="chunkSize">Chunk edge length in tiles.</param>
    public TileMap(int chunkSize)
    {
        _chunks = new ChunkMap<MortonArray<T>>(chunkSize);
        _chunkMask = chunkSize - 1;
    }

    /// <summary>
    /// Converts global tile coordinates into local coordinates within a chunk.
    /// This is equivalent to modulo by <see cref="ChunkSize"/> for power-of-two sizes,
    /// but always yields a non-negative result in [0, ChunkSize - 1], including for
    /// negative world coordinates.
    /// </summary>
    /// <param name="tilePos">Global tile coordinates.</param>
    /// <returns>Chunk-local coordinates in the range [0, <see cref="ChunkSize"/> - 1].</returns>
    [PublicAPI]
    public Vector2i LocalTilePosition(Vector2i tilePos)
    {
        // fuck its genius
        return new Vector2i(tilePos.X & _chunkMask, tilePos.Y & _chunkMask);
    }

    /// <summary>
    /// Tries to get the value associated with the given tile position.
    /// </summary>
    /// <param name="tilePos">Tile position to get the value of.</param>
    /// <param name="value">The value associated with the given tile position, if it exists.</param>
    /// <returns>True if a value exists for the given tile position, false otherwise.</returns>
    [PublicAPI]
    public bool TryGetValue(Vector2i tilePos, [NotNullWhen(true)] out T? value)
    {
        if (!_chunks!.TryGetValue(tilePos, out var array))
        {
            value = default;
            return false;
        }

        var localPos = LocalTilePosition(tilePos);
        if (array.GetValue(localPos) is { } tileValue)
        {
            value = tileValue;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Inserts a value at the given tile position.
    /// </summary>
    /// <param name="tilePos">Global tile coordinates to insert at.</param>
    /// <param name="value">Value to insert.</param>
    [PublicAPI]
    public void Insert(Vector2i tilePos, T value)
    {
        if (!_chunks!.TryGetValue(tilePos, out var array))
        {
            array = new MortonArray<T>(ChunkSize);
            _chunks.Set(tilePos, array);
        }

        array.Insert(LocalTilePosition(tilePos), value);
    }

    /// <summary>
    /// Removes the value at the given tile position.
    /// </summary>
    /// <param name="tilePos">Global tile coordinates to remove from.</param>
    /// <returns>True if the containing chunk exists and a removal was attempted.</returns>
    [PublicAPI]
    public bool Remove(Vector2i tilePos)
    {
        if (!_chunks!.TryGetValue(tilePos, out var array))
            return false;

        if (array.Count <= 0)
            return false;

        array.Remove(LocalTilePosition(tilePos));
        return true;
    }

    /// <summary>
    /// Sets an entire chunk at explicit chunk coordinates.
    /// </summary>
    [PublicAPI]
    public void SetChunk(Vector2i chunk, MortonArray<T> value)
    {
        _chunks!.SetChunk(chunk, value);
    }

    /// <summary>
    /// Enumerates all currently allocated chunks and their backing arrays.
    /// </summary>
    [PublicAPI]
    public IEnumerable<(Vector2i Chunk, MortonArray<T> Value)> EnumerateChunks()
    {
        return _chunks!.EnumerateChunks();
    }

    /// <summary>
    /// Clears all allocated chunks.
    /// </summary>
    [PublicAPI]
    public void Clear()
    {
        _chunks!.Clear();
    }
}
