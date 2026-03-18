using System;
using Content.Shared.Atmos.Collections.Spatial;
using Robust.Shared.Analyzers;

namespace Content.Shared.Atmos.Tests.Collections.Spatial;

[TestFixture, TestOf(typeof(ChunkMap<>))]
[Parallelizable(ParallelScope.All)]
[Virtual]
public sealed class ChunkMapTests
{
    [TestCase(0)]
    [TestCase(3)]
    [TestCase(-2)]
    public void Constructor_NonPowerOfTwoChunkSize_ThrowsArgumentOutOfRangeException(int chunkSize)
    {
        Assert.That(() => _ = new ChunkMap<int>(chunkSize), Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void Constructor_PowerOfTwoChunkSize_StartsWithoutChunks()
    {
        var map = new ChunkMap<int>(8);

        Assert.That(map.Chunks, Is.Zero);
    }

    [Test]
    public void Set_TilesInSameChunk_StoresSingleChunkAndReturnsStoredValueFromAnyTile()
    {
        var map = new ChunkMap<int>(4);
        map.Set((0, 0), 42);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(map.TryGetValue((3, 3), out var value), Is.True);
            Assert.That(value, Is.EqualTo(42));
            Assert.That(map.Chunks, Is.EqualTo(1));
        }
    }

    [Test]
    public void Set_SameChunkWrittenTwice_OverwritesValueWithoutAddingChunk()
    {
        var map = new ChunkMap<int>(4);
        map.Set((1, 1), 10);
        map.Set((2, 2), 99);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(map.TryGetValue((0, 0), out var value), Is.True);
            Assert.That(value, Is.EqualTo(99));
            Assert.That(map.Chunks, Is.EqualTo(1));
        }
    }

    [Test]
    public void Set_TilesInDifferentChunks_StoresIndependentValues()
    {
        var map = new ChunkMap<int>(4);
        map.Set((0, 0), 11);
        map.Set((4, 0), 22);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(map.TryGetValue((3, 3), out var first), Is.True);
            Assert.That(first, Is.EqualTo(11));
            Assert.That(map.TryGetValue((7, 3), out var second), Is.True);
            Assert.That(second, Is.EqualTo(22));
            Assert.That(map.Chunks, Is.EqualTo(2));
        }
    }

    [Test]
    public void TryGetValue_MissingChunk_ReturnsFalseAndDefaultValue()
    {
        var map = new ChunkMap<int>(4);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(map.TryGetValue((100, 100), out var value), Is.False);
            Assert.That(value, Is.Zero);
        }
    }

    [Test]
    public void Remove_ExistingChunk_ReturnsTrueAndRemovesValueForWholeChunk()
    {
        var map = new ChunkMap<int>(4);
        map.Set((1, 1), 5);

        var removed = map.Remove((3, 3));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(removed, Is.True);
            Assert.That(map.TryGetValue((0, 0), out _), Is.False);
            Assert.That(map.Chunks, Is.Zero);
        }
    }

    [Test]
    public void Remove_MissingChunk_ReturnsFalse()
    {
        var map = new ChunkMap<int>(4);

        Assert.That(map.Remove((0, 0)), Is.False);
    }

    [TestCase(0, 0)]
    [TestCase(1, -1)]
    [TestCase(-1, 1)]
    [TestCase(int.MaxValue, int.MinValue)]
    public void EncodeChunkDecodeChunk_RoundTripReturnsOriginal(int x, int y)
    {
        var code = ChunkMap<int>.EncodeChunk((x, y));
        var decoded = ChunkMap<int>.DecodeChunk(code);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(decoded.X, Is.EqualTo(x));
            Assert.That(decoded.Y, Is.EqualTo(y));
        }
    }

    [TestCase(3, 3, 0, 0)]
    [TestCase(4, 4, 1, 1)]
    [TestCase(-1, -1, -1, -1)]
    [TestCase(-4, -4, -1, -1)]
    public void CodeOf_DecodedChunkForTile_ReturnsExpectedChunkCoordinates(int tileX, int tileY, int chunkX, int chunkY)
    {
        var map = new ChunkMap<int>(4);
        var code = map.CodeOf((tileX, tileY));
        var chunk = ChunkMap<int>.DecodeChunk(code);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(chunk.X, Is.EqualTo(chunkX));
            Assert.That(chunk.Y, Is.EqualTo(chunkY));
        }
    }

    [Test]
    public void DecodeChunkBounds_NegativeChunk_ReturnsInclusiveTileBounds()
    {
        var map = new ChunkMap<int>(4);
        var code = ChunkMap<int>.EncodeChunk((-1, -1));

        var bounds = map.DecodeChunkBounds(code);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(bounds.Left, Is.EqualTo(-4));
            Assert.That(bounds.Bottom, Is.EqualTo(-4));
            Assert.That(bounds.Right, Is.EqualTo(-1));
            Assert.That(bounds.Top, Is.EqualTo(-1));
        }
    }
}
