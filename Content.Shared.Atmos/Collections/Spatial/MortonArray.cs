// This file includes code based on the List<T> class from https://github.com/dotnet/runtime/
// The original code is Copyright © .NET Foundation and Contributors. All rights reserved. Licensed under the MIT License (MIT).
// Also see Robust.Shared.Collections.ValueList<T>.

using System.Collections;
using System.Runtime.CompilerServices;
using Content.Shared.Atmos.Numerics;
using JetBrains.Annotations;
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
public sealed class MortonArray<T> : IEnumerable<T>
{
    /// <summary>
    /// Underlying array storing the elements in Morton order.
    /// </summary>
    private T[]? _array;

    /// <summary>
    /// Current side length of the square grid.
    /// </summary>
    public readonly int SideLength;

    /// <summary>
    /// Current capacity of the array.
    /// </summary>
    public int Capacity => _array?.Length ?? 0;

    /// <summary>
    /// Current count of elements in the array.
    /// </summary>
    public int Count { get; private set; }

    /// <summary>
    /// Current bounds of the array, from (0, 0) to (SideLength - 1, SideLength - 1).
    /// </summary>
    public Box2i Bounds => new((0, 0), (SideLength - 1, SideLength - 1));

    /// <summary>
    /// Current side length of the square grid that the underlying array can fit.
    /// Morton encoding requires the side length to be a power of 2,
    /// so this is the smallest power of 2 that can fit a square grid of the given side length.
    /// The rest is just padded to preserve the Morton encoding.
    /// </summary>
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private int _actualSideLength;

    /// <summary>
    /// Creates a new <see cref="MortonArray{T}"/>
    /// with the given desired side length.
    /// </summary>
    /// <param name="sideLength">The desired side length of the square grid.</param>
    public MortonArray(int sideLength)
    {
        _actualSideLength = BufferHelpers.FittingPowerOfTwo(sideLength);
        _array = new T[_actualSideLength^2];
        SideLength = sideLength;
    }

    /// <summary>
    /// Provides indexed access to the elements in the array using a <see cref="Vector2i"/> position.
    /// </summary>
    /// <param name="pos">The <see cref="Vector2i"/> position to access.</param>
    /// <returns>A reference to the element at the given position.</returns>
    /// <remarks>Note that a Morton array's origin starts at 0,
    /// so an array with <see cref="SideLength"/> 4 covers bounds from 0 to 3.</remarks>
    [PublicAPI]
    public T this[Vector2i pos]
    {
        get => GetValue(pos);
        set => Insert(pos, value);
    }

    /// <summary>
    /// Gets the value at the given <see cref="Vector2i"/> position.
    /// </summary>
    /// <param name="pos">The <see cref="Vector2i"/> position to get the value from.</param>
    /// <returns>The value at the given position.</returns>
    [PublicAPI]
    public T GetValue(Vector2i pos)
    {
        CheckBounds(pos);
        var mortonIndex = BitHelpers.BitInterleave(pos);
        return _array![mortonIndex];
    }

    /// <summary>
    /// Inserts a value into the array at the given position.
    /// </summary>
    /// <param name="pos">The <see cref="Vector2i"/> position to insert the value at.</param>
    /// <param name="value">The value to insert.</param>
    [PublicAPI]
    public void Insert(Vector2i pos, T value)
    {
        CheckBounds(pos);
        var mortonIndex = BitHelpers.BitInterleave(pos);
        _array![mortonIndex] = value;
        Count++;
    }

    /// <summary>
    /// Removes the element at the given <see cref="Vector2i"/> position.
    /// </summary>
    /// <param name="pos">The <see cref="Vector2i"/> position to remove the element from.</param>
    [PublicAPI]
    public void Remove(Vector2i pos)
    {
        CheckBounds(pos);
        var mortonIndex = BitHelpers.BitInterleave(pos);
        _array![mortonIndex] = default!;
        Count--;
    }

    /// <summary>
    /// Wipes the array, setting all memory to default.
    /// </summary>
    [PublicAPI]
    public void Wipe()
    {
        if (_array == null)
            return;

        Array.Clear(_array, 0, _array.Length);
        Count = 0;
    }

    /// <summary>
    /// Clears the array.
    /// </summary>
    /// <remarks>Use <see cref="Wipe"/> if you want to zero out all memory.
    /// Array will be zeroed out for ref-types, but value types will just have their count reset to 0,
    /// and the old values will be left in memory until overwritten.</remarks>
    [PublicAPI]
    public void Clear()
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            var size = Count;
            Count = 0;
            if (size > 0)
            {
                Array.Clear(_array!, 0, size);
            }
        }
        else
        {
            Count = 0;
        }
    }

    /// <summary>
    /// Checks if the given position is within the bounds of the current side length.
    /// </summary>
    /// <param name="pos">The position to check.</param>
    /// <exception cref="IndexOutOfRangeException">Thrown if the position is out of bounds.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CheckBounds(Vector2i pos)
    {
        // Casting to uint will wrap negatives so technically one less check.
        // Inclusive so that the max valid index is SideLength - 1.
        // TODO see if replacing this with Box2i.Contains is faster smile
        if ((uint)pos.X >= SideLength || (uint)pos.Y >= SideLength)
            throw new IndexOutOfRangeException($"Position {pos} is out of bounds for side length {SideLength}.");
    }

    public IEnumerator<T> GetEnumerator()
    {
        return new Enumerator(this);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// Enumerator for iterating over the elements in the <see cref="MortonArray{T}"/>.
    /// </summary>
    /// <param name="mortonArray">The <see cref="MortonArray{T}"/> to enumerate over.</param>
    public struct Enumerator(MortonArray<T> mortonArray) : IEnumerator<T>
    {
        private int _index = -1;

        /// <summary>
        /// Gets the current element in the array.
        /// </summary>
        public T Current => mortonArray._array![_index];

        /// <summary>
        /// Gets the current element in the array as an object.
        /// </summary>
        object? IEnumerator.Current => Current;

        /// <summary>
        /// Advances the enumerator to the next element in the array.
        /// </summary>
        /// <returns>True if the enumerator was successfully advanced to the next element;
        /// false if the enumerator has passed the end of the array.</returns>
        public bool MoveNext()
        {
            return ++_index < mortonArray.Capacity;
        }

        /// <summary>
        /// Resets the enumerator to its initial position, which is before the first element in the array.
        /// </summary>
        public void Reset()
        {
            _index = -1;
        }

        public void Dispose()
        {
            // oh boy! I hope I don't have a hard shift at the boilerplate factory today!
        }
    }
}
