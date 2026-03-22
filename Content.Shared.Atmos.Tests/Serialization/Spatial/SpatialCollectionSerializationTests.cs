using System.Reflection;
using Content.Shared.Atmos.Collections.Spatial;
using Robust.Shared.Analyzers;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.UnitTesting.Shared.Serialization;

namespace Content.Shared.Atmos.Tests.Serialization.Spatial;

[TestFixture]
[Virtual]
public sealed class SpatialCollectionSerializationTests : SerializationTest
{
    protected override Assembly[] Assemblies =>
    [
        typeof(SpatialCollectionSerializationTests).Assembly,
        typeof(TileMap<>).Assembly,
    ];

    [Test]
    public void ChunkMapSerializer_RoundTripsChunkDataAndChunkSize()
    {
        var source = new ChunkMap<int>(4);
        source.SetChunk((-1, 2), 11);
        source.SetChunk((3, -4), 22);

        var node = (MappingDataNode)Serialization.WriteValue(source, notNullableOverride: true);
        var roundTrip = Serialization.Read<ChunkMap<int>>(node, notNullableOverride: true);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(roundTrip.ChunkSize, Is.EqualTo(4));
            Assert.That(roundTrip.ChunkCount, Is.EqualTo(2));
            Assert.That(roundTrip.TryGetValue((-1, 8), out var first), Is.True);
            Assert.That(first, Is.EqualTo(11));
            Assert.That(roundTrip.TryGetValue((12, -16), out var second), Is.True);
            Assert.That(second, Is.EqualTo(22));
        }
    }

    [Test]
    public void MortonArraySerializer_RoundTripsBackingDataAndCount()
    {
        var source = new MortonArray<int>(3);
        source.Insert((0, 0), 5);
        source.Insert((0, 0), 9);
        source.Insert((1, 1), 7);

        var node = (MappingDataNode)Serialization.WriteValue(source, notNullableOverride: true);
        var roundTrip = Serialization.Read<MortonArray<int>>(node, notNullableOverride: true);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(roundTrip.SideLength, Is.EqualTo(3));
            Assert.That(roundTrip.Count, Is.EqualTo(source.Count));
            Assert.That(roundTrip.GetValue((0, 0)), Is.EqualTo(9));
            Assert.That(roundTrip.GetValue((1, 1)), Is.EqualTo(7));
            Assert.That(roundTrip.GetValue((1, 0)), Is.EqualTo(0));
        }
    }

    [Test]
    public void TileMapSerializer_RoundTripsTilesAcrossPositiveAndNegativeChunks()
    {
        var source = new TileMap<string>(4);
        source.Insert((0, 0), "a");
        source.Insert((4, 0), "b");
        source.Insert((-4, -4), "c");

        var node = (MappingDataNode)Serialization.WriteValue(source, notNullableOverride: true);
        var roundTrip = Serialization.Read<TileMap<string>>(node, notNullableOverride: true);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(roundTrip.ChunkSize, Is.EqualTo(4));
            Assert.That(roundTrip.ChunkCount, Is.EqualTo(source.ChunkCount));
            Assert.That(roundTrip.TryGetValue((0, 0), out var first), Is.True);
            Assert.That(first, Is.EqualTo("a"));
            Assert.That(roundTrip.TryGetValue((4, 0), out var second), Is.True);
            Assert.That(second, Is.EqualTo("b"));
            Assert.That(roundTrip.TryGetValue((-4, -4), out var third), Is.True);
            Assert.That(third, Is.EqualTo("c"));
            Assert.That(roundTrip.TryGetValue((1, 1), out var missing), Is.False);
            Assert.That(missing, Is.Null);
        }
    }
}
