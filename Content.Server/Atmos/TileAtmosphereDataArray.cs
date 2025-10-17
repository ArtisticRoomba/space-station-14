using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Content.Server.Atmos;

public sealed class TileAtmosphereDataArray : ITileAtmosphereData
{
    #region Interface Implementation

    public IEnumerator<TileAtmosphere> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Add(TileAtmosphere item)
    {
        throw new NotImplementedException();
    }

    public void Clear()
    {
        throw new NotImplementedException();
    }

    public bool Contains(TileAtmosphere item)
    {
        throw new NotImplementedException();
    }

    public void CopyTo(TileAtmosphere[] array, int arrayIndex)
    {
        throw new NotImplementedException();
    }

    public bool Remove(TileAtmosphere item)
    {
        throw new NotImplementedException();
    }

    public int Count { get; }
    public bool IsReadOnly { get; }

    #endregion
}
