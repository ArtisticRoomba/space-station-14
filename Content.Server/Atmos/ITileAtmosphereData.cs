using Content.Server.Atmos.Components;

namespace Content.Server.Atmos;

/// <summary>
/// Defines an interface for a <see cref="Vector2i"/> key,
/// <see cref="TileAtmosphere"/> value dictionary used in a <see cref="GridAtmosphereComponent"/>
/// to store data on tiles in a grid.
/// </summary>
public interface ITileAtmosphereData
{
    /// <summary>
    /// Current number of elements stored.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Current number of elements the data structure can hold without resizing.
    /// </summary>
    public int Capacity { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="TileAtmosphere"/> at the specified <see cref="Vector2i"/> key.
    /// </summary>
    /// <param name="key"></param>
    public TileAtmosphere this[Vector2i key] { get; set; }

    /// <summary>
    /// Adds the specified <see cref="TileAtmosphere"/> value with the specified <see cref="Vector2i"/> key.
    /// </summary>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="value">The value of the element to add.</param>
    public void Add(Vector2i key, TileAtmosphere value);
}

