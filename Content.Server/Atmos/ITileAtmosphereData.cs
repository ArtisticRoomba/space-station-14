using Content.Server.Atmos.Components;

namespace Content.Server.Atmos;

/// <summary>
/// <para>Defines an interface for a <see cref="Vector2i"/> key,
/// <see cref="TileAtmosphere"/> value data structure used in a
/// <see cref="GridAtmosphereComponent"/>
/// to store data on tiles in a grid.</para>
///
/// <para>This interface should contain all signatures that are
/// currently being used by Atmospherics so data structures can
/// be easily swapped out.</para>
/// </summary>
public interface ITileAtmosphereData : ICollection<TileAtmosphere>;

