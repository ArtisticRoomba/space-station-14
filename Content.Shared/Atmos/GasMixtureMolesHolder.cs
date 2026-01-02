using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Content.Shared.Atmos;

/// <summary>
/// A special struct to store gas moles.
/// </summary>
///
/// <para>For performance reasons we store the gases in an inline array backed by SIMD vectors.
/// Storing the gases in vectors allows us to do SIMD operations on these values in bulk
/// without loading/unloading them from memory multiple times.</para>
///
/// <para></para>
[InlineArray(Atmospherics.AdjustedNumberOfGases)] // TODO Dynamically adjust this value based on how many gases are defined.
public struct GasMixtureMolesHolder // TODO implement ICloneable, IList, IStructuralEquatable, IStructureComparable.
{
    /// <summary>
    /// The first element in the inline array.
    /// </summary>
    /// <remarks>TODO in the future this should be a Vector256 with
    /// helper methods choosing to load Vector256 floats into Vector128 and then just use that
    /// when AVX isn't available. However for simplicity and proof of concept this is staying so for now.</remarks>
    private Vector128<float> _element0;

    public int Length => Atmospherics.AdjustedNumberOfGases;
}
