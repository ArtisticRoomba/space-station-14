using System.Numerics;
using System.Runtime.CompilerServices;

namespace Content.Shared.Atmos.Maths;

public static class AtmosDirectionHelpers
{
    /// <summary>
    /// Gets the opposite of a given <see cref="AtmosDirection"/>.
    /// </summary>
    /// <param name="direction">The <see cref="AtmosDirection"/> to get the opposite of.</param>
    /// <returns>The opposite of the given <see cref="AtmosDirection"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the given <see cref="AtmosDirection"/> is not a valid direction,
    /// or when an opposite for the given direction doesn't exist.</exception>
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
    public static AtmosDirection ToOppositeDir(this int index)
    {
        return (AtmosDirection)(1 << (index ^ 1));
    }

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
    /// Converts a direction to an angle, where angle is -PI to +PI.
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
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
    /// Converts an angle to a cardinal AtmosDirection
    /// </summary>
    /// <param name="angle"></param>
    /// <returns></returns>
    public static AtmosDirection ToAtmosDirectionCardinal(this Angle angle)
    {
        return angle.GetCardinalDir().ToAtmosDirection();
    }

    /// <summary>
    /// Converts an angle to an AtmosDirection
    /// </summary>
    /// <param name="angle"></param>
    /// <returns></returns>
    public static AtmosDirection ToAtmosDirection(this Angle angle)
    {
        return angle.GetDir().ToAtmosDirection();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ToIndex(this AtmosDirection direction)
    {
        // This will throw if you pass an invalid direction. Not this method's fault, but yours!
        return BitOperations.Log2((uint)direction);
    }

    public static AtmosDirection WithFlag(this AtmosDirection direction, AtmosDirection other)
    {
        return direction | other;
    }

    public static AtmosDirection WithoutFlag(this AtmosDirection direction, AtmosDirection other)
    {
        return direction & ~other;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsFlagSet(this AtmosDirection direction, AtmosDirection other)
    {
        return (direction & other) == other;
    }

    public static Vector2i CardinalToIntVec(this AtmosDirection dir)
    {
        switch (dir)
        {
            case AtmosDirection.North:
                return new Vector2i(0, 1);
            case AtmosDirection.East:
                return new Vector2i(1, 0);
            case AtmosDirection.South:
                return new Vector2i(0, -1);
            case AtmosDirection.West:
                return new Vector2i(-1, 0);
            default:
                throw new ArgumentException($"Direction dir {dir} is not a cardinal direction", nameof(dir));
        }
    }

    public static Vector2i Offset(this Vector2i pos, AtmosDirection dir)
    {
        return pos + dir.CardinalToIntVec();
    }
}
