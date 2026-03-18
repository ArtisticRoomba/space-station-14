using Content.Shared.Atmos.Numerics;
using Robust.Shared.Analyzers;

namespace Content.Shared.Atmos.Tests.Numerics;

[TestFixture, TestOf(typeof(BitHelpers))]
[Parallelizable(ParallelScope.All)]
[Virtual]
public class BitHelpersTests
{
    [TestCase(0, 0, 0ul)]
    [TestCase(1, 0, 1ul)]
    [TestCase(0, 1, 2ul)]
    [TestCase(1, 1, 3ul)]
    [TestCase(2, 0, 4ul)]
    [TestCase(0, 2, 8ul)]
    [TestCase(-1, -1, ulong.MaxValue)]
    public void BitInterleave_ReturnExpectedMorton(int x, int y, ulong expected)
    {
        var morton = BitHelpers.BitInterleave((x, y));

        Assert.That(morton, Is.EqualTo(expected));
    }

    [TestCase(0ul, 0, 0)]
    [TestCase(1ul, 1, 0)]
    [TestCase(2ul, 0, 1)]
    [TestCase(3ul, 1, 1)]
    [TestCase(4ul, 2, 0)]
    [TestCase(8ul, 0, 2)]
    [TestCase(ulong.MaxValue, -1, -1)]
    public void BitDeinterleave_ReturnExpectedVector(ulong morton, int expectedX, int expectedY)
    {
        var vec = BitHelpers.BitDeinterleave(morton);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(vec.X, Is.EqualTo(expectedX));
            Assert.That(vec.Y, Is.EqualTo(expectedY));
        }
    }

    [TestCase(0, 0)]
    [TestCase(1, 1)]
    [TestCase(-1, -1)]
    [TestCase(int.MaxValue, int.MaxValue)]
    [TestCase(int.MinValue, int.MinValue)]
    [TestCase(int.MinValue, int.MaxValue)]
    [TestCase(int.MaxValue, int.MinValue)]
    public void BitInterleaveDeinterleave_RoundTrip_ReturnsOriginal(int x, int y)
    {
        var morton = BitHelpers.BitInterleave((x, y));
        var vec = BitHelpers.BitDeinterleave(morton);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(vec.X, Is.EqualTo(x));
            Assert.That(vec.Y, Is.EqualTo(y));
        }
    }
}
