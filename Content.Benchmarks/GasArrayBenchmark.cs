using System;
using BenchmarkDotNet.Attributes;
using Robust.Shared.Analyzers;
using Robust.Shared.Maths;

namespace Content.Benchmarks;

/// <summary>
/// Benchmark for profiling NumericsHelpers.HorizontalAdd,
/// frequently used for TotalMoles in Atmospherics,
/// depending on the length of gas arrays.
/// </summary>
[Virtual]
[GcServer(true)]
public class GasArrayBenchmark
{
    [Params(8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32)]
    public int ArrayLength;

    [Params(true, false)]
    public bool PadArray;

    private float[] _input = default!;

    [GlobalSetup]
    public void Setup()
    {
        _input = new float[ArrayLength];
        if (PadArray)
            Array.Resize(ref _input, MathHelper.NextMultipleOf(_input.Length, 4));
    }

    [Benchmark]
    public float BenchmarkHorizontalAdd()
    {
        return NumericsHelpers.HorizontalAdd(_input);
    }
}
