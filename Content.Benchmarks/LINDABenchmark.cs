using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Content.IntegrationTests;
using Content.IntegrationTests.Pair;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos.Components;
using Content.Shared.CCVar;
using Robust.Shared;
using Robust.Shared.Analyzers;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Random;

namespace Content.Benchmarks;

[Virtual]
[GcServer(true)]
[MemoryDiagnoser]
public class LindaBenchmark
{
    [Params(1, 10, 100, 1000, 5000, 10000, 50000, 100000)]
    public int TileCount;

    private TestPair _pair = default!;
    private IEntityManager _entMan = default!;
    private SharedMapSystem _map = default!;
    private IRobustRandom _random = default!;
    private IConfigurationManager _cvar = default!;
    private ITileDefinitionManager _tileDefMan = default!;
    private AtmosphereSystem _atmospereSystem = default!;

    private Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent>
        _testEnt;

    [GlobalSetup]
    public async Task SetupAsync()
    {
        ProgramShared.PathOffset = "../../../../";
        PoolManager.Startup();
        _pair = await PoolManager.GetServerClient();
        var server = _pair.Server;

        var mapdata = await _pair.CreateTestMap();

        _entMan = server.ResolveDependency<IEntityManager>();
        _map = _entMan.System<SharedMapSystem>();
        _random = server.ResolveDependency<IRobustRandom>();
        _cvar = server.ResolveDependency<IConfigurationManager>();
        _tileDefMan = server.ResolveDependency<ITileDefinitionManager>();
        _atmospereSystem = _entMan.System<AtmosphereSystem>();

        _random.SetSeed(69420);

        // sorry buddy LINDA didnt consent
        _cvar.SetCVar(CCVars.MonstermosEqualization, false);
        _cvar.SetCVar(CCVars.MonstermosDepressurization, false);

        var plating = _tileDefMan["Plating"].TileId;

        var length = TileCount + 2; // ensures we can spawn exactly N windows between side walls
        const int height = 1;

        await server.WaitPost(() =>
        {
            for (var x = 0; x < length; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    _map.SetTile(mapdata.Grid, mapdata.Grid, new Vector2i(x, y), new Tile(plating));
                }
            }
        });

        await server.WaitRunTicks(5);

        var uid = mapdata.Grid.Owner;
        _testEnt = new Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent>(
            uid,
            _entMan.GetComponent<GridAtmosphereComponent>(uid),
            _entMan.GetComponent<GasTileOverlayComponent>(uid),
            _entMan.GetComponent<MapGridComponent>(uid),
            _entMan.GetComponent<TransformComponent>(uid));
    }

    /// <summary>
    /// Mark all tiles as active so they get queued for processing.
    /// </summary>
    [IterationSetup]
    public void IterationSetup()
    {
        _pair.Server.WaitPost(delegate
        {
            foreach (var (_, tile) in _testEnt.Comp1.Tiles)
            {
                _atmospereSystem.AddActiveTile(_testEnt.Comp1, tile);
            }
        })
            .GetAwaiter()
            .GetResult();
    }

    [Benchmark]
    public async Task PerformSingleRun_Linda()
    {
        await _pair.Server.WaitPost(delegate
        {
            _atmospereSystem.RunProcessingStage(_testEnt, AtmosphereProcessingState.ActiveTiles);
        });
    }

    [Benchmark]
    public async Task PerformFullRun_Linda()
    {
        await _pair.Server.WaitPost(delegate
        {
            while (!_atmospereSystem.RunProcessingStage(_testEnt, AtmosphereProcessingState.ActiveTiles)) { }
        });
    }


}
