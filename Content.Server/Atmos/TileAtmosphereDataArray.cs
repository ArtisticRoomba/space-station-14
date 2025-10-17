using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Content.Server.Atmos;

/// <summary>
/// Array-based implementation of <see cref="ITileAtmosphereData"/>.
/// </summary>
///
/// <para>The standard way to store a dense (x, y)
/// key value pair with negative values is to use a dictionary.
/// Most operations in Atmospherics are getting the data of the neighboring tiles.
/// This data is cached on the <see cref="TileAtmosphere"/>, however this consumes
/// memory for each tile and may not be as performant as directly accessing the array.
/// </para>
///
/// <para>
/// This implementation is an experiment to see if storing <see cref="TileAtmosphere"/>
/// data in a 1D z-order curve array is more performant than a dictionary.
/// Z-order curves provide cache locality and potentially enable 2D convolution,
/// which could be useful for certain atmospheric calculations.
/// </para>
public sealed class TileAtmosphereDataArray : ITileAtmosphereData
{
    /*
     Since a Z-order curve cannot have negative values,
     and we want to ensure values have good locality,
     we need to define 4 arrays that each store a quadrant of the grid.
     */

    private readonly TileAtmosphere[] _arrayPosPos; // I
    private readonly TileAtmosphere[] _arrayNegPos; // II
    private readonly TileAtmosphere[] _arrayNegNeg; // III
    private readonly TileAtmosphere[] _arrayPosNeg; // IV

    /*
     Unfortunately we also need to provide a lot of default dictionary
     behavior and I don't want to think about implementing all of that right now.
     Here little maintainer, take this pipe bomb.
     */
    public Dictionary<Vector2i, TileAtmosphere> BackingDictionary;

    /// <summary>
    /// Initializes a new instance of a <see cref="TileAtmosphereDataArray"/>
    /// with the specified minimum and maximum coordinates.
    /// </summary>
    /// <param name="min">The coordinates of the bottom-left of the bound.</param>
    /// <param name="max">The coordinates of the top-right of the bound.</param>
    public TileAtmosphereDataArray(Vector2i min, Vector2i max)
    {
        // use that one helper method when
        var sizePosX = Math.Max(0, max.X + 1);
        var sizeNegX = Math.Max(0, -min.X);
        var sizePosY = Math.Max(0, max.Y + 1);
        var sizeNegY = Math.Max(0, -min.Y);

        _arrayPosPos = new TileAtmosphere[sizePosX * sizePosY];
        _arrayNegPos = new TileAtmosphere[sizeNegX * sizePosY];
        _arrayNegNeg = new TileAtmosphere[sizeNegX * sizeNegY];
        _arrayPosNeg = new TileAtmosphere[sizePosX * sizeNegY];
        BackingDictionary = new Dictionary<Vector2i, TileAtmosphere>();
    }

    /// <summary>
    /// Gets the number of elements contained in the <see cref="TileAtmosphereDataArray"/>.
    /// </summary>
    /// <remarks>I'm straight piratesoftwaremaxxing</remarks>
    public int Count => _arrayPosPos.Length +
                         _arrayNegPos.Length +
                         _arrayNegNeg.Length +
                         _arrayPosNeg.Length;

    public bool IsSynchronized => false;
    public object SyncRoot => this;

    private TileAtmosphere[] GetArrayForCoordinates(Vector2i coord)
    {
        return GetArrayForCoordinates(coord.X, coord.Y);
    }

    /// <summary>
    /// Retrieves the appropriate array for the given coordinates.
    /// </summary>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    /// <returns>The array corresponding to the quadrant of the coordinates.</returns>
    /// <exception cref="InvalidOperationException">Thrown if an invalid quadrant index is computed.</exception>
    private TileAtmosphere[] GetArrayForCoordinates(int x, int y)
    {
        /*
         In order to not obliterate CPU pipelining when constantly retrieving arrays,
         we perform bitwise operations to determine the quadrant index
         (so we don't have to branch on every access):

         1. First shift the sin bits of x and y to the least significant bit position.
         2. Then, combine these bits to form a 2-bit index:
            - Bit 0 -> Sign of x (0 for positive, 1 for negative)
            - Bit 1 -> Sign of y (0 for positive, 1 for negative)
         */
        var index = ((x >> 31) & 1) | (((y >> 31) & 1) << 1);

        return index switch
        {
            0 => _arrayPosPos, // Quadrant I
            1 => _arrayNegPos, // Quadrant II
            2 => _arrayNegNeg, // Quadrant III
            3 => _arrayPosNeg, // Quadrant IV
            _ => throw new InvalidOperationException("Invalid quadrant index"),
        };
    }

    /// <summary>
    /// Calculates the Z-value for the given coordinates.
    /// </summary>
    /// <param name="coord">The coordinates.</param>
    /// <returns>The Z-value for the coordinates.</returns>
    /// <remarks>See https://en.wikipedia.org/wiki/Z-order_curve</remarks>
    private static int EncodeZValue(Vector2i coord)
    {
        return EncodeZValue(coord.X, coord.Y);
    }

    /// <summary>
    /// Calculates the Z-value for the given coordinates.
    /// </summary>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    /// <returns>The Z-value for the coordinates.</returns>
    /// <remarks>See https://en.wikipedia.org/wiki/Z-order_curve</remarks>
    private static int EncodeZValue(int x, int y)
    {
        var z = 0;
        for (var i = 0; i < sizeof(int) * 4; i++)
        {
            z |= ((x >> i) & 1) << (2 * i);
            z |= ((y >> i) & 1) << (2 * i + 1);
        }
        return z;
    }

    /// <summary>
    /// Decodes a given Z-value into its corresponding coordinates.
    /// </summary>
    /// <param name="z">The Z-value to decode.</param>
    /// <param name="x">The decoded x coordinate.</param>
    /// <param name="y">The decoded y coordinate.</param>
    /// <remarks>See https://en.wikipedia.org/wiki/Z-order_curve</remarks>
    private static void DecodeZValue(int z, out int x, out int y)
    {
        x = 0;
        y = 0;
        for (var i = 0; i < sizeof(int) * 4; i++)
        {
            x |= ((z >> (2 * i)) & 1) << i;
            y |= ((z >> (2 * i + 1)) & 1) << i;
        }
    }

    private static void DecodeZValue(int z, out Vector2i coord)
    {
        DecodeZValue(z, out var x, out var y);
        coord = new Vector2i(x, y);
    }

    /// <summary>
    /// Clears all elements from the <see cref="TileAtmosphereDataArray"/>.
    /// </summary>
    public void Clear()
    {
        Array.Clear(_arrayPosPos);
        Array.Clear(_arrayNegPos);
        Array.Clear(_arrayNegNeg);
        Array.Clear(_arrayPosNeg);
        BackingDictionary.Clear();
    }

    public IDictionaryEnumerator GetEnumerator()
    {
        return BackingDictionary.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// Copies the elements of the <see cref="TileAtmosphereDataArray"/> to an <see cref="Array"/>,
    /// starting at the specified index of the target array.
    /// Copies from all four quadrant arrays in order.
    /// </summary>
    /// <param name="array">The destination array.</param>
    /// <param name="index">The starting index in the destination array.</param>
    public void CopyTo(Array array, int index)
    {
        Array.Copy(_arrayPosPos, (TileAtmosphere[])array, index);
        Array.Copy(_arrayNegPos, (TileAtmosphere[])array, index + _arrayPosPos.Length);
        Array.Copy(_arrayNegNeg, (TileAtmosphere[])array, index + _arrayPosPos.Length + _arrayNegPos.Length);
        Array.Copy(_arrayPosNeg,
            (TileAtmosphere[])array,
            index + _arrayPosPos.Length + _arrayNegPos.Length + _arrayNegNeg.Length);
    }

    /// <summary>
    /// Gets or sets the <see cref="TileAtmosphere"/>
    /// at the specified <see cref="Vector2i"/> key.
    /// </summary>
    /// <param name="key">The key of the element to get or set.</param>
    public TileAtmosphere this[Vector2i key]
    {
        get
        {
            var array = GetArrayForCoordinates(key);
            return array[EncodeZValue(key)];
        }
        set
        {
            var array = GetArrayForCoordinates(key);
            array[EncodeZValue(key)] = value;
        }
    }

    public void Add(Vector2i key, TileAtmosphere value)
    {
        var array = GetArrayForCoordinates(key);
        array[EncodeZValue(key)] = value;
    }

    public bool TryGetValue(Vector2i key, [MaybeNullWhen(false)] out TileAtmosphere value)
    {
        var newValue = this[key];
        if (!Unsafe.IsNullRef(ref newValue))
        {
            value = newValue;
            return true;
        }

        value = null;
        return false;
    }

    public bool Remove(Vector2i key)
    {
        var value = this[key];
        if (Unsafe.IsNullRef(ref value))
            return false;

        var array = GetArrayForCoordinates(key);
        array[EncodeZValue(key)] = null!;
        return true;
    }
}
