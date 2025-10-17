using System.Collections;
using System.Diagnostics.CodeAnalysis;
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
public interface ITileAtmosphereData : ICollection
{
    /// <summary>
    /// Gets or sets the <see cref="TileAtmosphere"/>
    /// at the specified <see cref="Vector2i"/> key.
    /// </summary>
    /// <param name="key"></param>
    TileAtmosphere this[Vector2i key] { get; set; }

    /// <summary>
    /// Adds the specified <see cref="TileAtmosphere"/> value
    /// with the specified <see cref="Vector2i"/> key.
    /// </summary>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="value">The value of the element to add.</param>
    void Add(Vector2i key, TileAtmosphere value);

    /// <summary>
    /// Tries to get the <see cref="TileAtmosphere"/> associated
    /// with the specified <see cref="Vector2i"/> key.
    /// </summary>
    /// <param name="key">The key of the element to get.</param>
    /// <param name="value">The element that is returned.</param>
    /// <returns>True if the value could be retrieved; otherwise, false.</returns>
    bool TryGetValue(Vector2i key, [MaybeNullWhen(false)] out TileAtmosphere value);

    /// <summary>
    /// Removes the <see cref="TileAtmosphere"/> associated
    /// with the specified <see cref="Vector2i"/> key.
    /// </summary>
    /// <param name="key">The key of the element to remove.</param>
    /// <returns>True if the element is successfully found and removed; otherwise, false.</returns>
    bool Remove(Vector2i key);
}

