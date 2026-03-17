using JetBrains.Annotations;

namespace Content.Shared.Atmos.Numerics;

/// <summary>
/// Static class containing helpers for common bit manipulation operations.
/// </summary>
public static class BitHelpers
{
    /// <summary>
    /// Interleaves all bits from the components of a
    /// <see cref="Vector2i"/> into a 64-bit Morton code.
    /// </summary>
    [PublicAPI]
    public static ulong BitInterleave(Vector2i vec)
    {
        return Part1By1((uint) vec.X) | (Part1By1((uint) vec.Y) << 1);
    }

    /// <summary>
    /// Reconstructs the original integer pair from a 64-bit Morton code.
    /// </summary>
    [PublicAPI]
    public static Vector2i BitDeinterleave(ulong morton)
    {
        var x = (int) Compact1By1(morton);
        var y = (int) Compact1By1(morton >> 1);
        return (x, y);
    }

    /// <summary>
    /// Spreads the bits of a 32-bit integer so that there are zeros between each bit.
    /// </summary>
    /// <param name="value">The 32-bit integer to spread.</param>
    /// <returns>The 64-bit integer with the bits of the input spread out.</returns>
    private static ulong Part1By1(uint value)
    {
        ulong x = value;
        x = (x | (x << 16)) & 0x0000FFFF0000FFFFUL;
        x = (x | (x << 8))  & 0x00FF00FF00FF00FFUL;
        x = (x | (x << 4))  & 0x0F0F0F0F0F0F0F0FUL;
        x = (x | (x << 2))  & 0x3333333333333333UL;
        x = (x | (x << 1))  & 0x5555555555555555UL;
        return x;
    }

    // Compacts every other bit back down into the low 32 bits.
    private static uint Compact1By1(ulong value)
    {
        var x = value & 0x5555555555555555UL;
        x = (x | (x >> 1))  & 0x3333333333333333UL;
        x = (x | (x >> 2))  & 0x0F0F0F0F0F0F0F0FUL;
        x = (x | (x >> 4))  & 0x00FF00FF00FF00FFUL;
        x = (x | (x >> 8))  & 0x0000FFFF0000FFFFUL;
        x = (x | (x >> 16)) & 0x00000000FFFFFFFFUL;
        return (uint) x;
    }
}
