using System;
using Content.Shared.Atmos.Collections.Spatial;
using Content.Shared.Atmos.Maths;
using Robust.Shared.Analyzers;

namespace Content.Shared.Atmos.Tests.Collections.Spatial;

[TestFixture, TestOf(typeof(MortonArray<>))]
[Parallelizable(ParallelScope.All)]
[Virtual]
public sealed class MortonArrayTests
{
    [Test]
    public void Constructor_WithRequestedSideLength_StoresSideLengthAndStartsEmpty()
    {
        var array = new MortonArray<int>(3);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(array.SideLength, Is.EqualTo(3));
            Assert.That(array.Capacity, Is.GreaterThan(0));
            Assert.That(array.Count, Is.Zero);
        }
    }

    [Test]
    public void InsertAndGetValue_PositionWithinBounds_ReturnsInsertedValue()
    {
        var array = new MortonArray<int>(4);
        array.Insert((1, 1), 42);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(array.GetValue((1, 1)), Is.EqualTo(42));
            Assert.That(array[(1, 1)], Is.EqualTo(42));
            Assert.That(array, Has.Count.EqualTo(1));
        }
    }

    [Test]
    public void InsertAndGetValue_MaxInBoundsPosition_DoesNotThrowAndReturnsValue()
    {
        var array = new MortonArray<int>(16);

        Assert.That(() => array.Insert((15, 15), 123), Throws.Nothing);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(array.GetValue((15, 15)), Is.EqualTo(123));
            Assert.That(array.Count, Is.EqualTo(1));
        }
    }

    [Test]
    public void Insert_SamePositionTwice_OverwritesValueAndIncrementsCountPerInsert()
    {
        var array = new MortonArray<int>(4);
        array.Insert((1, 1), 5);
        array.Insert((1, 1), 7);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(array.GetValue((1, 1)), Is.EqualTo(7));
            Assert.That(array, Has.Count.EqualTo(2));
        }
    }

    [Test]
    public void GetValue_PositionOutOfBounds_ThrowsIndexOutOfRangeException()
    {
        var array = new MortonArray<int>(4);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(() => array.GetValue((-1, 0)), Throws.InstanceOf<IndexOutOfRangeException>());
            Assert.That(() => array.GetValue((4, 0)), Throws.InstanceOf<IndexOutOfRangeException>());
            Assert.That(() => array.GetValue((0, 4)), Throws.InstanceOf<IndexOutOfRangeException>());
        }
    }

    [Test]
    public void Remove_ExistingPosition_ClearsValueAndDecrementsCount()
    {
        var array = new MortonArray<int>(4);
        array.Insert((1, 1), 99);

        array.Remove((1, 1));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(array.GetValue((1, 1)), Is.EqualTo(0));
            Assert.That(array.Count, Is.Zero);
        }
    }

    [Test]
    public void Clear_ReferenceTypeArray_ResetsCountAndClearsStoredValues()
    {
        var array = new MortonArray<string>(4);
        array.Insert((0, 0), "god");
        array.Insert((1, 0), "help me");

        array.Clear();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(array.Count, Is.Zero);
            Assert.That(array.GetValue((0, 0)), Is.Null);
            Assert.That(array.GetValue((1, 0)), Is.Null);
        }
    }

    [Test]
    public void Wipe_AfterMultipleInserts_ClearsAllValuesAndResetsCount()
    {
        var array = new MortonArray<int>(4);
        array.Insert((0, 0), 1);
        array.Insert((1, 0), 2);
        array.Insert((0, 1), 3);

        array.Wipe();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(array.Count, Is.Zero);
            Assert.That(array.GetValue((0, 0)), Is.EqualTo(0));
            Assert.That(array.GetValue((1, 0)), Is.EqualTo(0));
            Assert.That(array.GetValue((0, 1)), Is.EqualTo(0));
        }
    }

    [Test]
    public void Enumerator_AfterReset_ReplaysSequenceFromBeginning()
    {
        var array = new MortonArray<int>(4);
        array.Insert((0, 0), 10);
        array.Insert((1, 0), 20);

        var enumerator = array.GetEnumerator();

        Assert.That(enumerator.MoveNext(), Is.True);
        var firstPassFirstValue = enumerator.Current;

        while (enumerator.MoveNext()) { }

        enumerator.Reset();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(enumerator.MoveNext(), Is.True);
            Assert.That(enumerator.Current, Is.EqualTo(firstPassFirstValue));
        }
        enumerator.Dispose();
    }
}
