using System.Diagnostics.CodeAnalysis;
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
/// an entity that is physically on the grid and loaded/unloaded by PVS.
/// This entity would then be stored on a <see cref="ChunkMap{T}"/> or something similar.</para>
/// <para>The server, however, can get away with just using this thing willy nilly.</para>
public sealed class TileMap<T>
{
    /// <summary>
    /// Underlying storage mapping chunk coordinates to their chunk value.
    /// </summary>
    private readonly ChunkMap<MortonArray<T>>? _chunks;

    /// <summary>
    /// Edge length of each chunk in tiles.
    /// </summary>
    public int ChunkSize => _chunks?.ChunkSize ?? 0;

    /// <summary>
    /// Creates a new <see cref="TileMap{T}"/> with the desired chunk size.
    /// Chunk size must be a positive power of two.
    /// </summary>
    /// <param name="chunkSize">Chunk edge length in tiles.</param>
    public TileMap(int chunkSize)
    {
        _chunks = new ChunkMap<MortonArray<T>>(chunkSize);
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
        if (_chunks is null || !_chunks.TryGetValue(tilePos, out var array))
        {
            value = default;
            return false;
        }

        if (array.GetValue(tilePos) is { } tileValue)
        {
            value = tileValue;
            return true;
        }

        value = default;
        return false;
    }
}
