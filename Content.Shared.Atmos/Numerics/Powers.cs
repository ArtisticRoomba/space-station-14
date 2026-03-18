using JetBrains.Annotations;

namespace Content.Shared.Atmos.Numerics;

/// <summary>
/// Static class containing helpers for common operations related to powers.
/// </summary>
public static class Powers
{
    /*
     hey man this is my fun dont shit on it
     */

    /// <summary>
    /// Checks if the given integer is a power of two.
    /// </summary>
    /// <param name="value">The integer to check.</param>
    /// <returns>True if the integer is a power of two, false otherwise.</returns>
    [PublicAPI]
    public static bool IsPowerOfTwo(int value)
    {
        return (value & (value - 1)) == 0 && value > 0;
    }
}
