using Robust.Shared.Utility;

namespace Content.Shared.Atmos.Collections.Spatial;

/// <summary>
/// Array intended for storing 2D <see cref="Vector2i"/> values in a 1D array using Morton encoding (Z-order curve).
/// </summary>
/// <para>In order to preserve cache locality of elements,
/// we use a Z-order curve to get frequently indexed elements close together in memory.</para>
/// <para>Technically not the best for cache locality,
/// however a Morton encoding is fairly easy to calculate, and it's easy to write.</para>
/// <remarks>Representative grid must be a square.</remarks>
public sealed partial class MortonArray<T>
{
    /// <summary>
    /// Underlying array storing the elements in Morton order.
    /// </summary>
    private T[]? _array;

    /// <summary>
    /// Current side length of the square grid.
    /// </summary>
    public int SideLength { get; private set; }

    /// <summary>
    /// Current side length of the square grid that the underlying array can fit.
    /// Morton encoding requires the side length to be a power of 2,
    /// so this is the smallest power of 2 that can fit a square grid of the given side length.
    /// The rest is just padded to preserve the Morton encoding.
    /// </summary>
    private int ActualSideLength;

    /// <summary>
    /// Current capacity of the array.
    /// </summary>
    public int Capacity => _array?.Length ?? 0;

    /// <summary>
    /// Creates a new <see cref="MortonArray{T}"/>
    /// with the given desired side length.
    /// </summary>
    /// <param name="sideLength">The desired side length of the square grid.</param>
    public MortonArray(int sideLength)
    {
        ActualSideLength = BufferHelpers.FittingPowerOfTwo(sideLength);
        _array = new T[ActualSideLength^2];
        SideLength = sideLength;
    }
}
