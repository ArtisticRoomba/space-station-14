using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Content.Shared.Atmos.Collections.Spatial;
using Content.Shared.Atmos.Maths;
using Robust.Shared.Analyzers;
using Robust.Shared.Maths;

namespace Content.Atmos.Benchmarks;

/// <summary>
/// Benchmarks for <see cref="MortonArray{T}"/>, <see cref="ChunkMap{T}"/>, and <see cref="TileMap{T}"/>,
/// comapred to the standard <see cref="Dictionary{TKey,TValue}"/>.
/// </summary>
[MemoryDiagnoser]
[Virtual]
public class SpatialDataStructuresBenchmark
{
    /// <summary>
    /// Number of operations to perform in each benchmark.
    /// </summary>
    [Params(5000000)]
    public int OperationCount;

    /// <summary>
    /// Chunk size for ChunkMap and TileMap tests (must be power of 2).
    /// </summary>
    [Params(16, 64)]
    public int ChunkSize;

    private int _seed;
    private Vector2i[] _positions = null!;
    private Vector2i[] _mortonPositions = null!;
    private Vector2i[] _mortonNeighborOrigins = null!;

    private Dictionary<Vector2i, TestValue> _dictionary = default!;
    private MortonArray<TestValue> _mortonArray = default!;
    private ChunkMap<TestValue> _chunkMap = default!;
    private TileMap<TestValue> _tileMap = default!;

    private readonly TestValue _value = new() { X = 1, Y = 2.0f, Z = 3L };

    private static readonly AtmosDirection[] CardinalDirections =
    [
        AtmosDirection.North,
        AtmosDirection.East,
        AtmosDirection.South,
        AtmosDirection.West,
    ];

    private struct TestValue
    {
        public int X;
        public float Y;
        public long Z;
    }

    [GlobalSetup]
    public void Setup()
    {
        _seed = 69420;
        _positions = GenerateRandomPos(OperationCount);
        _mortonPositions = ClampPositions(_positions, 0, 511);
        _mortonNeighborOrigins = ClampPositions(_positions, 1, 510);
    }

    private static Vector2i[] ClampPositions(Vector2i[] source, int min, int max)
    {
        var result = new Vector2i[source.Length];
        for (var i = 0; i < source.Length; i++)
        {
            var p = source[i];
            result[i] = new Vector2i(Math.Clamp(p.X, min, max), Math.Clamp(p.Y, min, max));
        }

        return result;
    }

    private void FillDictionary(Dictionary<Vector2i, TestValue> dict)
    {
        for (var i = 0; i < _positions.Length; i++)
        {
            dict[_positions[i]] = _value;
        }
    }

    private void FillMorton(MortonArray<TestValue> array)
    {
        for (var i = 0; i < _mortonPositions.Length; i++)
        {
            array.Insert(_mortonPositions[i], _value);
        }
    }

    private void FillChunkMap(ChunkMap<TestValue> chunkMap)
    {
        for (var i = 0; i < _positions.Length; i++)
        {
            chunkMap.Set(_positions[i], _value);
        }
    }

    private void FillTileMap(TileMap<TestValue> tileMap)
    {
        for (var i = 0; i < _positions.Length; i++)
        {
            tileMap.Insert(_positions[i], _value);
        }
    }

    private Vector2i[] GenerateRandomPos(int count)
    {
        var positions = new Vector2i[count];
        var random = new Random(_seed);

        for (var i = 0; i < count; i++)
        {
            positions[i] = new Vector2i(
                random.Next(-500, 500),
                random.Next(-500, 500)
            );
        }

        return positions;
    }

    #region Dict

    [IterationSetup(Target = nameof(DictionaryAdd))]
    public void SetupDictionaryAdd()
    {
        _dictionary = new Dictionary<Vector2i, TestValue>();
    }

    [IterationSetup(Target = nameof(DictionaryRemove))]
    public void SetupDictionaryRemove()
    {
        _dictionary = new Dictionary<Vector2i, TestValue>();
        FillDictionary(_dictionary);
    }

    [IterationSetup(Target = nameof(DictionaryIndexing))]
    public void SetupDictionaryIndexing()
    {
        _dictionary = new Dictionary<Vector2i, TestValue>();
        FillDictionary(_dictionary);
    }

    [IterationSetup(Target = nameof(DictionaryCardinalNeighborAccess))]
    public void SetupDictionaryCardinalNeighborAccess()
    {
        _dictionary = new Dictionary<Vector2i, TestValue>();
        FillDictionary(_dictionary);
    }

    [Benchmark(Description = "Dictionary Add")]
    public void DictionaryAdd()
    {
        var dict = _dictionary!;

        for (var i = 0; i < _positions.Length; i++)
        {
            dict[_positions[i]] = _value;
        }
    }

    [Benchmark(Description = "Dictionary Remove")]
    public void DictionaryRemove()
    {
        var dict = _dictionary!;
        for (var i = 0; i < _positions.Length; i++)
        {
            dict.Remove(_positions[i]);
        }
    }

    [Benchmark(Description = "Dictionary Indexing")]
    public void DictionaryIndexing()
    {
        var dict = _dictionary!;
        TestValue result = default;
        for (var i = 0; i < _positions.Length; i++)
        {
            if (dict.TryGetValue(_positions[i], out var retrieved))
            {
                result = retrieved;
            }
        }
    }

    [Benchmark(Description = "Dictionary Cardinal Neighbor Access")]
    public void DictionaryCardinalNeighborAccess()
    {
        var dict = _dictionary!;

        TestValue result = default;
        for (var i = 0; i < _positions.Length; i++)
        {
            var origin = _positions[i];
            if (dict.TryGetValue(origin, out var center))
                result = center;

            foreach (var dir in CardinalDirections)
            {
                if (dict.TryGetValue(origin.Offset(dir), out var neighbor))
                    result = neighbor;
            }
        }
    }

    #endregion

    #region MortonArray

    [IterationSetup(Target = nameof(MortonArrayAdd))]
    public void SetupMortonArrayAdd()
    {
        _mortonArray = new MortonArray<TestValue>(512);
    }

    [IterationSetup(Target = nameof(MortonArrayRemove))]
    public void SetupMortonArrayRemove()
    {
        _mortonArray = new MortonArray<TestValue>(512);
        FillMorton(_mortonArray);
    }

    [IterationSetup(Target = nameof(MortonArrayIndexing))]
    public void SetupMortonArrayIndexing()
    {
        _mortonArray = new MortonArray<TestValue>(512);
        FillMorton(_mortonArray);
    }

    [IterationSetup(Target = nameof(MortonArrayCardinalNeighborAccess))]
    public void SetupMortonArrayCardinalNeighborAccess()
    {
        _mortonArray = new MortonArray<TestValue>(512);
        FillMorton(_mortonArray);
    }

    [Benchmark(Description = "MortonArray Add")]
    public void MortonArrayAdd()
    {
        var array = _mortonArray!;

        for (var i = 0; i < _mortonPositions.Length; i++)
        {
            array.Insert(_mortonPositions[i], _value);
        }
    }

    [Benchmark(Description = "MortonArray Remove")]
    public void MortonArrayRemove()
    {
        var array = _mortonArray!;
        for (var i = 0; i < _mortonPositions.Length; i++)
        {
            array.Remove(_mortonPositions[i]);
        }
    }

    [Benchmark(Description = "MortonArray Indexing")]
    public void MortonArrayIndexing()
    {
        var array = _mortonArray!;
        TestValue result = default;
        for (var i = 0; i < _mortonPositions.Length; i++)
        {
            result = array.GetValue(_mortonPositions[i]);
        }
    }

    [Benchmark(Description = "MortonArray Cardinal Neighbor Access")]
    public void MortonArrayCardinalNeighborAccess()
    {
        var array = _mortonArray!;

        TestValue result = default;
        for (var i = 0; i < _mortonNeighborOrigins.Length; i++)
        {
            var origin = _mortonNeighborOrigins[i];

            result = array.GetValue(origin);
            foreach (var dir in CardinalDirections)
            {
                result = array.GetValue(origin.Offset(dir));
            }
        }
    }

    #endregion

    #region ChunkMap

    [IterationSetup(Target = nameof(ChunkMapAdd))]
    public void SetupChunkMapAdd()
    {
        _chunkMap = new ChunkMap<TestValue>(ChunkSize);
    }

    [IterationSetup(Target = nameof(ChunkMapRemove))]
    public void SetupChunkMapRemove()
    {
        _chunkMap = new ChunkMap<TestValue>(ChunkSize);
        FillChunkMap(_chunkMap);
    }

    [IterationSetup(Target = nameof(ChunkMapIndexing))]
    public void SetupChunkMapIndexing()
    {
        _chunkMap = new ChunkMap<TestValue>(ChunkSize);
        FillChunkMap(_chunkMap);
    }

    [Benchmark(Description = "ChunkMap Add")]
    public void ChunkMapAdd()
    {
        var chunkMap = _chunkMap!;

        for (var i = 0; i < _positions.Length; i++)
        {
            chunkMap.Set(_positions[i], _value);
        }
    }

    [Benchmark(Description = "ChunkMap Remove")]
    public void ChunkMapRemove()
    {
        var chunkMap = _chunkMap!;
        for (var i = 0; i < _positions.Length; i++)
        {
            chunkMap.Remove(_positions[i]);
        }
    }

    [Benchmark(Description = "ChunkMap Indexing")]
    public void ChunkMapIndexing()
    {
        var chunkMap = _chunkMap!;

        TestValue result = default;
        for (var i = 0; i < _positions.Length; i++)
        {
            if (chunkMap.TryGetValue(_positions[i], out var retrieved))
            {
                result = retrieved;
            }
        }
    }

    #endregion

    #region TileMap

    [IterationSetup(Target = nameof(TileMapAdd))]
    public void SetupTileMapAdd()
    {
        _tileMap = new TileMap<TestValue>(ChunkSize);
    }

    [IterationSetup(Target = nameof(TileMapRemove))]
    public void SetupTileMapRemove()
    {
        _tileMap = new TileMap<TestValue>(ChunkSize);
        FillTileMap(_tileMap);
    }

    [IterationSetup(Target = nameof(TileMapIndexing))]
    public void SetupTileMapIndexing()
    {
        _tileMap = new TileMap<TestValue>(ChunkSize);
        FillTileMap(_tileMap);
    }

    [IterationSetup(Target = nameof(TileMapCardinalNeighborAccess))]
    public void SetupTileMapCardinalNeighborAccess()
    {
        _tileMap = new TileMap<TestValue>(ChunkSize);
        FillTileMap(_tileMap);
    }

    [Benchmark(Description = "TileMap Add")]
    public void TileMapAdd()
    {
        var tileMap = _tileMap!;

        for (var i = 0; i < _positions.Length; i++)
        {
            tileMap.Insert(_positions[i], _value);
        }
    }

    [Benchmark(Description = "TileMap Remove")]
    public void TileMapRemove()
    {
        var tileMap = _tileMap!;
        for (var i = 0; i < _positions.Length; i++)
        {
            tileMap.Remove(_positions[i]);
        }
    }

    [Benchmark(Description = "TileMap Indexing")]
    public void TileMapIndexing()
    {
        var tileMap = _tileMap!;

        TestValue result = default;
        for (var i = 0; i < _positions.Length; i++)
        {
            if (tileMap.TryGetValue(_positions[i], out var retrieved))
            {
                result = retrieved;
            }
        }
    }

    [Benchmark(Description = "TileMap Cardinal Neighbor Access")]
    public void TileMapCardinalNeighborAccess()
    {
        var tileMap = _tileMap!;

        TestValue result = default;
        for (var i = 0; i < _positions.Length; i++)
        {
            var origin = _positions[i];
            if (tileMap.TryGetValue(origin, out var center))
                result = center;

            foreach (var dir in CardinalDirections)
            {
                if (tileMap.TryGetValue(origin.Offset(dir), out var neighbor))
                    result = neighbor;
            }
        }
    }

    #endregion
}
