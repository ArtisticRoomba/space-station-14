using System;
using Content.Shared.Atmos.Collections.Spatial;
using Robust.Shared.Analyzers;

namespace Content.Shared.Atmos.Tests.Collections.Spatial;

[TestFixture, TestOf(typeof(TileMap<>))]
[Parallelizable(ParallelScope.All)]
[Virtual]
public sealed class TileMapTests
{
    [TestCase(0)]
    [TestCase(3)]
    [TestCase(-2)]
    public void Constructor_NonPowerOfTwoChunkSize_ThrowsArgumentOutOfRangeException(int chunkSize)
    {
        Assert.That(() => _ = new TileMap<int>(chunkSize), Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [TestCase(0, 0, 0, 0)]
    [TestCase(15, 15, 15, 15)]
    [TestCase(16, 16, 0, 0)]
    [TestCase(17, 18, 1, 2)]
    [TestCase(-1, -1, 15, 15)]
    [TestCase(-16, -16, 0, 0)]
    [TestCase(-17, -18, 15, 14)]
    public void LocalTilePosition_WorldCoordinates_ReturnsChunkLocalCoordinates(int tileX,
        int tileY,
        int localX,
        int localY)
    {
        var map = new TileMap<string>(16);

        var local = map.LocalTilePosition((tileX, tileY));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(local.X, Is.EqualTo(localX));
            Assert.That(local.Y, Is.EqualTo(localY));
        }
    }

    [Test]
    public void TryGetValue_MissingTileWithoutChunk_ReturnsFalseAndNull()
    {
        var map = new TileMap<string>(8);

        var found = map.TryGetValue((4, 4), out var value);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(found, Is.False);
            Assert.That(value, Is.Null);
        }
    }

    [Test]
    public void Insert_ThenTryGetValueAtSameTile_ReturnsStoredValue()
    {
        var map = new TileMap<string>(4);
        map.Insert((1, 0), "plasma");

        var found = map.TryGetValue((1, 0), out var value);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(found, Is.True);
            Assert.That(value, Is.EqualTo("plasma"));
        }
    }

    [Test]
    public void Insert_SameTileTwice_OverwritesStoredValue()
    {
        var map = new TileMap<string>(8);
        map.Insert((1, 1), "oxygen");
        map.Insert((1, 1), "nitrogen");

        var found = map.TryGetValue((1, 1), out var value);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(found, Is.True);
            Assert.That(value, Is.EqualTo("nitrogen"));
        }
    }

    [Test]
    public void Insert_TilesInDifferentChunks_StoresValuesIndependently()
    {
        var map = new TileMap<string>(4);
        map.Insert((0, 0), "a");
        map.Insert((4, 0), "b");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(map.TryGetValue((0, 0), out var first), Is.True);
            Assert.That(first, Is.EqualTo("a"));
            Assert.That(map.TryGetValue((4, 0), out var second), Is.True);
            Assert.That(second, Is.EqualTo("b"));
        }
    }

    [Test]
    public void TryGetValue_UnsetTileInExistingChunk_ReturnsFalseAndNull()
    {
        var map = new TileMap<string>(4);
        map.Insert((0, 0), "co2");

        var found = map.TryGetValue((1, 0), out var value);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(found, Is.False);
            Assert.That(value, Is.Null);
        }
    }

    [Test]
    public void Remove_ExistingTile_ReturnsTrueAndSubsequentLookupReturnsFalse()
    {
        var map = new TileMap<string>(4);
        map.Insert((1, 0), "n2o");

        var removed = map.Remove((1, 0));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(removed, Is.True);
            Assert.That(map.TryGetValue((1, 0), out var value), Is.False);
            Assert.That(value, Is.Null);
        }
    }

    [Test]
    public void Remove_TileWithNoChunk_ReturnsFalse()
    {
        var map = new TileMap<string>(8);

        Assert.That(map.Remove((-4, -4)), Is.False);
    }
}
