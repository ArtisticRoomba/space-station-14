using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared.Atmos.Numerics;
using JetBrains.Annotations;

namespace Content.Shared.Atmos.Collections.Spatial;

/// <summary>
/// Map intended for storing values associated with <see cref="Vector2i"/>
/// spatial positions in a grid of fixed-size chunks.
/// A tileable area is divided into square chunks of a given size,
/// with each chunk storing a value of type <typeparamref name="T"/>.
/// </summary>
/// <para>In essence this DS is mapping a pair of (x, y) coordinates across
/// a certain area to a single element.</para>
public sealed class ChunkMap<T>
{
    /// <summary>
    /// Number of chunks currently stored in the map.
    /// </summary>
    public int Chunks => _data.Count;

    /// <summary>
    /// Size of each chunk edge in tiles.
    /// </summary>
    public readonly int ChunkSize;

    /// <summary>
    /// Number of bits to shift a tile coordinate to obtain chunk coordinates.
    /// Equivalent to <c>log2(chunkSize)</c>.
    /// </summary>
    private readonly int _shift;

    /// <summary>
    /// Underlying storage mapping encoded chunk IDs to their chunk value.
    /// </summary>
    private readonly Dictionary<ulong, T> _data = new();

    /// <summary>
    /// Creates a new <see cref="ChunkMap{T}"/> with the desired chunk size.
    /// Chunk size must be a positive power of two. Gotta go fast.
    /// </summary>
    /// <param name="chunkSize">Chunk edge length in tiles.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="chunkSize"/> is not a positive power of two.
    /// </exception>
    [PublicAPI]
    public ChunkMap(int chunkSize)
    {
        if (!Powers.IsPowerOfTwo(chunkSize))
            throw new ArgumentOutOfRangeException(nameof(chunkSize), "chunkSize must be a power of two.");

        ChunkSize = chunkSize;
        _shift = BitOperations.TrailingZeroCount((uint)chunkSize);
    }

    /// <summary>
    /// Maps a global tile coordinate to chunk coordinates.
    /// </summary>
    /// <param name="p">Global tile coordinates.</param>
    /// <returns>The chunk-space coordinates containing <paramref name="p"/>.</returns>
    /// <remarks>
    /// Uses arithmetic right shift, which is equivalent to floor division by
    /// <see cref="ChunkSize"/> for signed integers when chunk size is a power of two.
    /// </remarks>
    private Vector2i ChunkOf(Vector2i p)
    {
        return (p.X >> _shift, p.Y >> _shift);
    }

    /// <summary>
    /// Maps a global tile coordinate directly to an encoded chunk ID.
    /// </summary>
    /// <param name="p">Global tile coordinates.</param>
    /// <returns>The encoded ID of the chunk containing <paramref name="p"/>.</returns>
    [PublicAPI]
    public ulong CodeOf(Vector2i p)
    {
        return EncodeChunk(ChunkOf(p));
    }

    /// <summary>
    /// Encodes chunk coordinates into a single Morton code.
    /// </summary>
    /// <param name="chunk">Chunk-space coordinates to encode.</param>
    /// <returns>A reversible 64-bit Morton code for the chunk coordinates.</returns>
    [PublicAPI]
    public static ulong EncodeChunk(Vector2i chunk)
    {
        return BitHelpers.BitInterleave(chunk);
    }

    /// <summary>
    /// Decodes a chunk Morton code back into chunk coordinates.
    /// </summary>
    /// <param name="code">Encoded chunk Morton code.</param>
    /// <returns>The original chunk-space coordinates.</returns>
    [PublicAPI]
    public static Vector2i DecodeChunk(ulong code)
    {
        return BitHelpers.BitDeinterleave(code);
    }

    /// <summary>
    /// Converts an encoded chunk ID into the chunk's tile bounds.
    /// </summary>
    /// <param name="code">Encoded chunk Morton code.</param>
    /// <returns>
    /// Inclusive tile bounds covered by the decoded chunk.
    /// </returns>
    [PublicAPI]
    public Box2i DecodeChunkBounds(ulong code)
    {
        var c = DecodeChunk(code);
        var min = new Vector2i(c.X << _shift, c.Y << _shift);
        var max = new Vector2i(min.X + ChunkSize - 1, min.Y + ChunkSize - 1);
        return new Box2i(min, max);
    }

    /// <summary>
    /// Tries to get the value associated with the chunk containing the given tile.
    /// </summary>
    /// <param name="tile">Global tile coordinates.</param>
    /// <param name="value">
    /// When this method returns, contains the stored chunk value if found;
    /// otherwise the default value for <typeparamref name="T"/>.
    /// </param>
    /// <returns>
    /// True if a value exists for the containing chunk,
    /// otherwise false.
    /// </returns>
    [PublicAPI]
    public bool TryGetValue(Vector2i tile, [MaybeNullWhen(false)] out T value)
    {
        return _data.TryGetValue(CodeOf(tile), out value!);
    }

    /// <summary>
    /// Stores a value for the chunk containing the given tile.
    /// </summary>
    /// <param name="tile">Global tile coordinates used to locate the chunk.</param>
    /// <param name="value">Value to store for that chunk.</param>
    /// <remarks>
    /// Any tile in the same chunk maps to the same entry.
    /// Existing values for that chunk are overwritten.
    /// </remarks>
    [PublicAPI]
    public void Set(Vector2i tile, T value)
    {
        _data[CodeOf(tile)] = value;
    }

    /// <summary>
    /// Removes the value associated with the chunk containing the given tile.
    /// </summary>
    /// <param name="tile">Global tile coordinates used to locate the chunk.</param>
    /// <returns>
    /// True if an entry for the containing chunk was removed,
    /// otherwise false.
    /// </returns>
    [PublicAPI]
    public bool Remove(Vector2i tile)
    {
        return _data.Remove(CodeOf(tile));
    }
}
