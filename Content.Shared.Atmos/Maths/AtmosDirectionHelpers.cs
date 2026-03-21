using System.Numerics;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace Content.Shared.Atmos.Maths;

public static class AtmosDirectionHelpers
{
    /// <summary>
    /// Gets the opposite of a given <see cref="AtmosDirection"/>.
    /// </summary>
    /// <param name="direction">The <see cref="AtmosDirection"/> to get the opposite of.</param>
    /// <returns>The opposite of the given <see cref="AtmosDirection"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the given <see cref="AtmosDirection"/> is not a valid direction,
    /// or when an opposite for the given direction doesn't exist.</exception>+
    [PublicAPI]
    public static AtmosDirection GetOpposite(this AtmosDirection direction)
    {
        return direction switch
        {
            AtmosDirection.North => AtmosDirection.South,
            AtmosDirection.South => AtmosDirection.North,
            AtmosDirection.East => AtmosDirection.West,
            AtmosDirection.West => AtmosDirection.East,
            AtmosDirection.NorthEast => AtmosDirection.SouthWest,
            AtmosDirection.NorthWest => AtmosDirection.SouthEast,
            AtmosDirection.SouthEast => AtmosDirection.NorthWest,
            AtmosDirection.SouthWest => AtmosDirection.NorthEast,
            _ => throw new ArgumentOutOfRangeException(nameof(direction)),
        };
    }

    /// <summary>
    /// Returns the index that corresponds to the opposite direction of some other direction index.
    /// I.e., <c>1&lt;&lt;OppositeIndex(i) == (1&lt;&lt;i).GetOpposite()</c>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [PublicAPI]
    public static int ToOppositeIndex(this int index)
    {
        return index ^ 1;
    }

    /// <summary>
    /// Gets the opposite of a given direction index as an <see cref="AtmosDirection"/>.
    /// </summary>
    /// <param name="index">The index of the direction to get the opposite of.</param>
    /// <returns>The opposite of the given direction index as an <see cref="AtmosDirection"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [PublicAPI]
    public static AtmosDirection ToOppositeDir(this int index)
    {
        return (index^1).ToDirection();
    }

    /// <summary>
    /// Converts a direction index to an <see cref="AtmosDirection"/>.
    /// </summary>
    /// <param name="index">The index of the direction to convert.</param>
    /// <returns>The <see cref="AtmosDirection"/> corresponding to the given index.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [PublicAPI]
    public static AtmosDirection ToDirection(this int index)
    {
        return (AtmosDirection)(1 << index);
    }

    /// <summary>
    /// Converts an <see cref="AtmosDirection"/> to a <see cref="Direction"/>.
    /// </summary>
    /// <param name="direction">The <see cref="AtmosDirection"/> to convert.</param>
    /// <returns>The <see cref="Direction"/> corresponding to the given <see cref="AtmosDirection"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the given
    /// <see cref="AtmosDirection"/> cannot be converted.</exception>
    [PublicAPI]
    public static Direction ToDirection(this AtmosDirection direction)
    {
        return direction switch
        {
            AtmosDirection.North => Direction.North,
            AtmosDirection.South => Direction.South,
            AtmosDirection.East => Direction.East,
            AtmosDirection.West => Direction.West,
            AtmosDirection.NorthEast => Direction.NorthEast,
            AtmosDirection.NorthWest => Direction.NorthWest,
            AtmosDirection.SouthEast => Direction.SouthEast,
            AtmosDirection.SouthWest => Direction.SouthWest,
            AtmosDirection.Invalid => Direction.Invalid,
            _ => throw new ArgumentOutOfRangeException(nameof(direction))
        };
    }

    /// <summary>
    /// Converts a <see cref="Direction"/> to an <see cref="AtmosDirection"/>.
    /// </summary>
    /// <param name="direction">The <see cref="Direction"/> to convert.</param>
    /// <returns>The <see cref="AtmosDirection"/> corresponding to the given <see cref="Direction"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the given <see cref="Direction"/> cannot be converted.</exception>
    [PublicAPI]
    public static AtmosDirection ToAtmosDirection(this Direction direction)
    {
        return direction switch
        {
            Direction.North => AtmosDirection.North,
            Direction.South => AtmosDirection.South,
            Direction.East => AtmosDirection.East,
            Direction.West => AtmosDirection.West,
            Direction.NorthEast => AtmosDirection.NorthEast,
            Direction.NorthWest => AtmosDirection.NorthWest,
            Direction.SouthEast => AtmosDirection.SouthEast,
            Direction.SouthWest => AtmosDirection.SouthWest,
            Direction.Invalid => AtmosDirection.Invalid,
            _ => throw new ArgumentOutOfRangeException(nameof(direction))
        };
    }

    /// <summary>
    /// Converts an <see cref="AtmosDirection"/> to an angle, where angle is -PI to +PI.
    /// </summary>
    /// <param name="direction">The <see cref="AtmosDirection"/> to convert.</param>
    /// <returns>The angle corresponding to the given <see cref="AtmosDirection"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the given <see cref="AtmosDirection"/> cannot be converted.</exception>
    [PublicAPI]
    public static Angle ToAngle(this AtmosDirection direction)
    {
        return direction switch
        {
            AtmosDirection.South => Angle.Zero,
            AtmosDirection.East => new Angle(MathHelper.PiOver2),
            AtmosDirection.North => new Angle(Math.PI),
            AtmosDirection.West => new Angle(-MathHelper.PiOver2),
            AtmosDirection.NorthEast => new Angle(Math.PI * 3 / 4),
            AtmosDirection.NorthWest => new Angle(-Math.PI * 3 / 4),
            AtmosDirection.SouthWest => new Angle(-MathHelper.PiOver4),
            AtmosDirection.SouthEast => new Angle(MathHelper.PiOver4),
            _ => throw new ArgumentOutOfRangeException(nameof(direction), $"It was {direction}."),
        };
    }

    /// <summary>
    /// Converts an angle to a cardinal <see cref="AtmosDirection"/>.
    /// </summary>
    /// <param name="angle">The <see cref="Angle"/> to convert.</param>
    /// <returns>The cardinal <see cref="AtmosDirection"/> corresponding to the given <see cref="Angle"/>.</returns>
    [PublicAPI]
    public static AtmosDirection ToAtmosDirectionCardinal(this Angle angle)
    {
        return angle.GetCardinalDir().ToAtmosDirection();
    }

    /// <summary>
    /// Converts an angle to an <see cref="AtmosDirection"/>.
    /// </summary>
    /// <param name="angle">The <see cref="Angle"/> to convert.</param>
    /// <returns>The <see cref="AtmosDirection"/> corresponding to the given <see cref="Angle"/>.</returns>
    [PublicAPI]
    public static AtmosDirection ToAtmosDirection(this Angle angle)
    {
        return angle.GetDir().ToAtmosDirection();
    }

    /// <summary>
    /// Converts an <see cref="AtmosDirection"/> to an index.
    /// </summary>
    /// <param name="direction">The <see cref="AtmosDirection"/> to convert.</param>
    /// <returns>The index corresponding to the given <see cref="AtmosDirection"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [PublicAPI]
    public static int ToIndex(this AtmosDirection direction)
    {
        // This will throw if you pass an invalid direction. Not this method's fault, but yours!
        return BitOperations.Log2((uint)direction);
    }

    /// <summary>
    /// Returns a new <see cref="AtmosDirection"/> with the bits of the given other direction set.
    /// </summary>
    /// <param name="direction">The original <see cref="AtmosDirection"/>.</param>
    /// <param name="other">The <see cref="AtmosDirection"/> whose bits to set in the result.</param>
    /// <returns>A new <see cref="AtmosDirection"/> with the bits of <paramref name="other"/> set in the result.</returns>
    [PublicAPI]
    public static AtmosDirection WithFlag(this AtmosDirection direction, AtmosDirection other)
    {
        return direction | other;
    }

    /// <summary>
    /// Returns a new <see cref="AtmosDirection"/> with the bits of the given other direction unset.
    /// </summary>
    /// <param name="direction">The original <see cref="AtmosDirection"/>.</param>
    /// <param name="other">The <see cref="AtmosDirection"/> whose bits to unset in the result.</param>
    /// <returns>A new <see cref="AtmosDirection"/> with the bits of <paramref name="other"/> unset in the result.</returns>
    [PublicAPI]
    public static AtmosDirection WithoutFlag(this AtmosDirection direction, AtmosDirection other)
    {
        return direction & ~other;
    }

    /// <summary>
    /// Checks if the bits of the given other direction are set in this direction.
    /// </summary>
    /// <param name="direction">The <see cref="AtmosDirection"/> to check.</param>
    /// <param name="other">The <see cref="AtmosDirection"/> whose bits to check for.</param>
    /// <returns>True if the bits of <paramref name="other"/> are set in <paramref name="direction"/>, false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [PublicAPI]
    public static bool IsFlagSet(this AtmosDirection direction, AtmosDirection other)
    {
        return (direction & other) == other;
    }

    [PublicAPI]
    public static Vector2i CardinalToIntVec(this AtmosDirection dir)
    {
        return dir switch
        {
            AtmosDirection.North => Vector2i.Up,
            AtmosDirection.East => Vector2i.Right,
            AtmosDirection.South => Vector2i.Down,
            AtmosDirection.West => Vector2i.Left,
            _ => throw new ArgumentException($"Direction dir {dir} is not a cardinal direction", nameof(dir))
        };
    }

    /// <summary>
    /// Offsets a position by one tile in the given cardinal direction.
    /// </summary>
    /// <param name="pos">The position to offset.</param>
    /// <param name="dir">The cardinal direction to offset in.</param>
    /// <returns>The offset position.</returns>
    [PublicAPI]
    public static Vector2i Offset(this Vector2i pos, AtmosDirection dir)
    {
        return pos + dir.CardinalToIntVec();
    }
}
