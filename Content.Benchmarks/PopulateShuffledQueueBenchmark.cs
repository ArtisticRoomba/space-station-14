using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Temperature.HeatContainer;
using Robust.Shared.Analyzers;

namespace Content.Benchmarks;

[Virtual]
[GcServer(true)]
[MemoryDiagnoser]
public class PopulateShuffledQueueBenchmark
{
    /// <summary>
    /// Number of elements to populate the shuffled queue with.
    /// </summary>
    [Params(10, 100, 1000, 5000, 10000)]
    public int Elements;

    public HashSet<HeatContainer> HashSet = [];
    public List<HeatContainer> WorkingList = [];
    public Queue<HeatContainer> ShuffledQueue = [];

    [GlobalSetup]
    public void Setup()
    {
        HashSet.EnsureCapacity(Elements);
        WorkingList.EnsureCapacity(Elements);
        ShuffledQueue.EnsureCapacity(Elements);

        for (var i = 0; i < Elements; i++)
        {
            // we're primiarily replicating Entity<T>s for atmospherics so just fill this shit with a
            // struct
            var data = new HeatContainer(1, 1);
            HashSet.Add(data);
        }
    }

    [Benchmark]
    public void PopulateShuffledQueue()
    {
        AtmosphereSystem.PopulateShuffledQueue(in HashSet, WorkingList, ShuffledQueue);
    }
}
