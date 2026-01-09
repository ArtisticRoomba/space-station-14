using System.Runtime.CompilerServices;
using Content.Shared.Atmos;
using Content.Shared.Atmos.EntitySystems;

namespace Content.Server.Atmos.Serialization;

/// <summary>
/// Data struct used to wrap necessary information for LINDA.
/// LINDA sometimes needs to store information that must be accounted for in certain processing steps
/// (ex. some operations can't be multithreaded so they are delegated to main loop).
/// </summary>
/// <remarks>This is an internal Atmospherics struct. If you need shit from this (???) write an API.</remarks>
[Access(typeof(SharedAtmosphereSystem))]
public struct LindaInfo
{
    /// <summary>
    /// In which directions LINDA should hare air to.
    /// </summary>
    [ViewVariables]
    public AtmosDirection ShouldShareAir = AtmosDirection.Invalid;

    /// <summary>
    /// The mole differences from each direction
    /// that LINDA computed from sharing air.
    /// This is used to apply HighPressureDelta effects.
    /// This is formatted in <see cref="Atmospherics.Directions"/>.
    /// </summary>
    public ComputedDistanceHolder ComputedDifference;

    public ArchivedGasMixture ArchivedGasMixture;

    public LindaInfo()
    {
        ComputedDifference = default;
        ArchivedGasMixture = default;
    }
}

/// <summary>
/// Special <see cref="GasMixture"/>-esque culled copy that contains only necessary information LINDA needs for processing.
/// </summary>
/// <para>Since LINDA doesn't need all the information available in a GasMixture,
/// we can copy select parts of it. Bonus points, since LINDA will be serverside for the forseeable future,
/// and inline arrays are easy to replace, we can make the moles array an inline array (for when heat capacity is needed).</para>
public struct ArchivedGasMixture
{
    public GasMixtureMolesHolder Moles;
}

/// <summary>
/// Struct that holds the results of LINDA's Share operation
/// for each direction.
/// </summary>
/// <remarks>I love cache locality!!!!!!!!!!!!!</remarks>
[InlineArray(Atmospherics.Directions)]
public struct ComputedDistanceHolder
{
    private float _element0;
}

/// <summary>
/// Struct that holds the moles of each gas in a GasMixture for LINDA's processing.
/// </summary>
/// <remarks>Could you guess it? I love cache locality!!!!!</remarks>
[InlineArray(Atmospherics.AdjustedNumberOfGases)]
public struct GasMixtureMolesHolder
{
    private float _element0;
}
