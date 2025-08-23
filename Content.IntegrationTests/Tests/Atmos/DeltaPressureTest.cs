#nullable enable
using System.Numerics;
using Content.Server.Atmos;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.IntegrationTests.Tests.Atmos;

/// <summary>
/// Tests for AtmosphereSystem.DeltaPressure and surrounding systems
/// handling the DeltaPressureComponent.
/// </summary>
[TestFixture]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
[TestOf(typeof(DeltaPressureSystem))]
public sealed class DeltaPressureTest
{
    #region Test Prototypes

    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  parent: BaseStructure
  id: DeltaPressureSolidTest
  placement:
    mode: SnapgridCenter
    snap:
    - Wall
  components:
  - type: Physics
    bodyType: Static
  - type: Fixtures
    fixtures:
      fix1:
        shape:
          !type:PhysShapeAabb
          bounds: ""-0.5,-0.5,0.5,0.5""
        mask:
        - FullTileMask
        layer:
        - WallLayer
        density: 1000
  - type: Airtight
  - type: DeltaPressure
    minPressure: 15000
    minPressureDelta: 10000
    scalingType: Threshold
    baseDamage:
      types:
        Structural: 1000
  - type: Damageable
  - type: Destructible
    thresholds:
    - trigger:
        !type:DamageTrigger
        damage: 300
      behaviors:
      - !type:SpawnEntitiesBehavior
        spawn:
          Girder:
            min: 1
            max: 1
      - !type:DoActsBehavior
        acts: [ ""Destruction"" ]

- type: entity
  parent: DeltaPressureSolidTest
  id: DeltaPressureSolidTestNoAutoJoin
  components:
  - type: DeltaPressure
    autoJoinProcessingList: false

- type: entity
  parent: DeltaPressureSolidTest
  id: DeltaPressureSolidTestAbsolute
  components:
  - type: DeltaPressure
    minPressure: 10000
    minPressureDelta: 15000
    scalingType: Threshold
    baseDamage:
      types:
        Structural: 1000
";

    #endregion

    #region Test Setup

    private TestPair _pair = default!;
    private TestMapData MapData => _pair.TestMap!;
    private IEntityManager EntMan => _pair.Server.EntMan;
    private AtmosphereSystem AtmosphereSystem => EntMan.System<AtmosphereSystem>();
    private SharedTransformSystem TransformSystem => EntMan.System<SharedTransformSystem>();

    [SetUp]
    public async Task Setup()
    {
        _pair = await PoolManager.GetServerClient();
        await _pair.CreateTestMap();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _pair.CleanReturnAsync();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a test grid with a 5x5 area for delta pressure testing.
    /// </summary>
    private async Task<Entity<MapGridComponent>> CreateTestGrid()
    {
        var grid = Entity<MapGridComponent>.Invalid;
        await _pair.Server.WaitPost(() =>
        {
            grid = MapData.Grid;
            
            // Ensure we have a proper atmospheric grid setup
            var gridAtmosComp = EntMan.GetComponent<GridAtmosphereComponent>(grid);
            
            // Set up some basic tiles for atmosphere testing
            var mapGrid = EntMan.GetComponent<MapGridComponent>(grid);
            var tileDefinitionManager = _pair.Server.ResolveDependency<ITileDefinitionManager>();
            var platingTile = new Tile(tileDefinitionManager["Plating"].TileId);
            
            // Create a 5x5 grid of plating for testing
            for (var x = -2; x <= 2; x++)
            {
                for (var y = -2; y <= 2; y++)
                {
                    mapGrid.SetTile(new Vector2i(x, y), platingTile);
                }
            }
        });
        
        return grid;
    }

    /// <summary>
    /// Spawns a DeltaPressure test entity at the specified coordinates.
    /// </summary>
    private async Task<Entity<DeltaPressureComponent>> SpawnTestEntity(
        string prototypeId, 
        EntityCoordinates coordinates)
    {
        var entity = Entity<DeltaPressureComponent>.Invalid;
        await _pair.Server.WaitPost(() =>
        {
            var uid = EntMan.SpawnEntity(prototypeId, coordinates);
            entity = new Entity<DeltaPressureComponent>(uid, EntMan.GetComponent<DeltaPressureComponent>(uid));
        });
        return entity;
    }

    /// <summary>
    /// Sets the pressure of a specific tile by adjusting gas moles.
    /// </summary>
    private async Task SetTilePressure(
        Entity<MapGridComponent> grid, 
        Vector2i tileIndices, 
        float targetPressure)
    {
        await _pair.Server.WaitPost(() =>
        {
            var gridAtmosComp = EntMan.GetComponent<GridAtmosphereComponent>(grid);
            
            if (!gridAtmosComp.Tiles.TryGetValue(tileIndices, out var tile) || tile.Air == null)
                return;

            tile.Air.Clear();
            var moles = (targetPressure * tile.Air.Volume) / (Atmospherics.R * Atmospherics.T20C);
            tile.Air.AdjustMoles(Gas.Nitrogen, moles);
        });
    }

    /// <summary>
    /// Gets the tile atmosphere at the specified indices.
    /// </summary>
    private TileAtmosphere? GetTileAtmosphere(Entity<MapGridComponent> grid, Vector2i tileIndices)
    {
        var gridAtmosComp = EntMan.GetComponent<GridAtmosphereComponent>(grid);
        return gridAtmosComp.Tiles.TryGetValue(tileIndices, out var tile) ? tile : null;
    }

    /// <summary>
    /// Runs a specified number of simulation ticks and waits for completion.
    /// </summary>
    private async Task RunTicks(int ticks)
    {
        await _pair.RunTicksSync(ticks);
    }

    /// <summary>
    /// Verifies that an entity is correctly added or removed from the processing list.
    /// </summary>
    private void AssertProcessingListState(
        Entity<MapGridComponent> grid, 
        Entity<DeltaPressureComponent> entity, 
        bool shouldBeInList, 
        string context)
    {
        var isInList = AtmosphereSystem.IsDeltaPressureEntityInList(grid.Owner, entity);
        Assert.That(isInList, Is.EqualTo(shouldBeInList), 
            $"Entity {context}: expected processing list state {shouldBeInList}, got {isInList}");
    }

    #endregion

    #region Tests

    /// <summary>
    /// Asserts that an entity with a DeltaPressureComponent with autoJoinProcessingList
    /// set to true is automatically added to the DeltaPressure processing list
    /// on the grid's GridAtmosphereComponent.
    ///
    /// Also asserts that an entity with a DeltaPressureComponent with autoJoinProcessingList
    /// set to false is not automatically added to the DeltaPressure processing list.
    /// </summary>
    [Test]
    public async Task ProcessingListAutoJoinTest()
    {
        var grid = await CreateTestGrid();
        var testCoords = new EntityCoordinates(grid.Owner, Vector2.Zero);

        await _pair.Server.WaitAssertion(() =>
        {
            // Test entity with autoJoinProcessingList = true (default)
            var autoJoinEntity = EntMan.SpawnEntity("DeltaPressureSolidTest", testCoords);
            var autoJoinDeltaPressure = new Entity<DeltaPressureComponent>(
                autoJoinEntity, 
                EntMan.GetComponent<DeltaPressureComponent>(autoJoinEntity));

            AssertProcessingListState(grid, autoJoinDeltaPressure, true, 
                "with autoJoinProcessingList=true should be automatically added");

            // Clean up
            EntMan.DeleteEntity(autoJoinEntity);
            AssertProcessingListState(grid, autoJoinDeltaPressure, false, 
                "should be removed after deletion");

            // Test entity with autoJoinProcessingList = false
            var noAutoJoinEntity = EntMan.SpawnEntity("DeltaPressureSolidTestNoAutoJoin", testCoords);
            var noAutoJoinDeltaPressure = new Entity<DeltaPressureComponent>(
                noAutoJoinEntity, 
                EntMan.GetComponent<DeltaPressureComponent>(noAutoJoinEntity));

            AssertProcessingListState(grid, noAutoJoinDeltaPressure, false, 
                "with autoJoinProcessingList=false should not be automatically added");

            // Clean up
            EntMan.DeleteEntity(noAutoJoinEntity);
        });
    }

    /// <summary>
    /// Tests manual addition and removal of entities from the DeltaPressure processing list.
    /// </summary>
    [Test]
    public async Task ProcessingListJoinLeaveTest()
    {
        var grid = await CreateTestGrid();
        var testCoords = new EntityCoordinates(grid.Owner, Vector2.Zero);

        await _pair.Server.WaitAssertion(() =>
        {
            // Spawn entity that doesn't auto-join
            var entityUid = EntMan.SpawnEntity("DeltaPressureSolidTestNoAutoJoin", testCoords);
            var deltaPressureEntity = new Entity<DeltaPressureComponent>(
                entityUid, 
                EntMan.GetComponent<DeltaPressureComponent>(entityUid));

            // Verify it's not automatically added
            AssertProcessingListState(grid, deltaPressureEntity, false, 
                "with autoJoinProcessingList=false should not be in processing list initially");

            // Manually add to processing list
            var addResult = AtmosphereSystem.TryAddDeltaPressureEntity(grid.Owner, deltaPressureEntity);
            Assert.That(addResult, Is.True, "Should be able to manually add entity to processing list");
            AssertProcessingListState(grid, deltaPressureEntity, true, 
                "should be in processing list after manual addition");

            // Manually remove from processing list
            var removeResult = AtmosphereSystem.TryRemoveDeltaPressureEntity(grid.Owner, deltaPressureEntity);
            Assert.That(removeResult, Is.True, "Should be able to manually remove entity from processing list");
            AssertProcessingListState(grid, deltaPressureEntity, false, 
                "should not be in processing list after manual removal");

            // Clean up
            EntMan.DeleteEntity(entityUid);
        });
    }

    /// <summary>
    /// Asserts that an entity that doesn't need to be damaged by DeltaPressure
    /// is not damaged by DeltaPressure when pressure is below threshold.
    /// </summary>
    [Test]
    public async Task ProcessingDeltaStandbyTest()
    {
        var grid = await CreateTestGrid();
        var testCoords = new EntityCoordinates(grid.Owner, Vector2.Zero);
        var deltaPressureEntity = await SpawnTestEntity("DeltaPressureSolidTest", testCoords);

        // Verify entity is in processing list
        await _pair.Server.WaitAssertion(() =>
        {
            AssertProcessingListState(grid, deltaPressureEntity, true, 
                "should be automatically added to processing list");
        });

        // Test all directions around the entity
        for (var i = 0; i < Atmospherics.Directions; i++)
        {
            var direction = (AtmosDirection)(1 << i);
            
            await _pair.Server.WaitPost(() =>
            {
                var entityIndices = TransformSystem.GetGridOrMapTilePosition(deltaPressureEntity);
                var targetIndices = entityIndices.Offset(direction);
                var tile = GetTileAtmosphere(grid, targetIndices);

                Assert.That(tile?.Air, Is.Not.Null, 
                    $"Tile at {targetIndices} should have air for direction {direction}!");

                // Set pressure below threshold (minPressureDelta - 10)
                var subThresholdPressure = deltaPressureEntity.Comp.MinPressureDelta - 10;
                var moles = (subThresholdPressure * tile.Air!.Volume) / (Atmospherics.R * Atmospherics.T20C);
                tile.Air.AdjustMoles(Gas.Nitrogen, moles);
            });

            await RunTicks(30);

            // Entity should still exist since pressure is below damage threshold
            await _pair.Server.WaitAssertion(() =>
            {
                Assert.That(!EntMan.Deleted(deltaPressureEntity), 
                    $"Entity should still exist after experiencing sub-threshold pressure from {direction} direction!");
                
                // Clean up the pressure for next iteration
                var entityIndices = TransformSystem.GetGridOrMapTilePosition(deltaPressureEntity);
                var targetIndices = entityIndices.Offset(direction);
                var tile = GetTileAtmosphere(grid, targetIndices);
                tile?.Air?.Clear();
            });

            await RunTicks(30);
        }
    }

    /// <summary>
    /// Asserts that an entity that needs to be damaged by DeltaPressure
    /// is damaged by DeltaPressure when the pressure is above the threshold.
    /// </summary>
    [Test]
    public async Task ProcessingDeltaDamageTest()
    {
        var grid = await CreateTestGrid();
        var testCoords = new EntityCoordinates(grid.Owner, Vector2.Zero);

        // Test all directions around the entity
        for (var i = 0; i < Atmospherics.Directions; i++)
        {
            var direction = (AtmosDirection)(1 << i);
            
            // Need to spawn a new entity for each direction test to ensure clean state
            var deltaPressureEntity = await SpawnTestEntity("DeltaPressureSolidTest", testCoords);
            
            await _pair.Server.WaitAssertion(() =>
            {
                AssertProcessingListState(grid, deltaPressureEntity, true, 
                    "should be automatically added to processing list");
            });

            await _pair.Server.WaitPost(() =>
            {
                var entityIndices = TransformSystem.GetGridOrMapTilePosition(deltaPressureEntity);
                var targetIndices = entityIndices.Offset(direction);
                var tile = GetTileAtmosphere(grid, targetIndices);

                Assert.That(tile?.Air, Is.Not.Null, 
                    $"Tile at {targetIndices} should have air for direction {direction}!");

                // Set pressure above threshold (minPressureDelta + 10)
                var aboveThresholdPressure = deltaPressureEntity.Comp.MinPressureDelta + 10;
                var moles = (aboveThresholdPressure * tile.Air!.Volume) / (Atmospherics.R * Atmospherics.T20C);
                tile.Air.AdjustMoles(Gas.Nitrogen, moles);
            });

            await RunTicks(30);

            // Entity should be destroyed due to exceeding damage threshold
            await _pair.Server.WaitAssertion(() =>
            {
                Assert.That(EntMan.Deleted(deltaPressureEntity), 
                    $"Entity should be destroyed after experiencing above-threshold pressure from {direction} direction!");
                
                // Clean up the pressure for next iteration
                var entityIndices = new Vector2i(0, 0); // Test coordinates are at (0,0)
                var targetIndices = entityIndices.Offset(direction);
                var tile = GetTileAtmosphere(grid, targetIndices);
                tile?.Air?.Clear();
            });

            await RunTicks(30);
        }
    }

    /// <summary>
    /// Asserts that an entity that doesn't need to be damaged by DeltaPressure
    /// is not damaged by DeltaPressure when using absolute pressure thresholds below the minimum.
    /// </summary>
    [Test]
    public async Task ProcessingAbsoluteStandbyTest()
    {
        var grid = await CreateTestGrid();
        var testCoords = new EntityCoordinates(grid.Owner, Vector2.Zero);
        var deltaPressureEntity = await SpawnTestEntity("DeltaPressureSolidTestAbsolute", testCoords);

        // Verify entity is in processing list
        await _pair.Server.WaitAssertion(() =>
        {
            AssertProcessingListState(grid, deltaPressureEntity, true, 
                "should be automatically added to processing list");
        });

        // Test all directions around the entity
        for (var i = 0; i < Atmospherics.Directions; i++)
        {
            var direction = (AtmosDirection)(1 << i);
            
            await _pair.Server.WaitPost(() =>
            {
                var entityIndices = TransformSystem.GetGridOrMapTilePosition(deltaPressureEntity);
                var targetIndices = entityIndices.Offset(direction);
                var tile = GetTileAtmosphere(grid, targetIndices);

                Assert.That(tile?.Air, Is.Not.Null, 
                    $"Tile at {targetIndices} should have air for direction {direction}!");

                // Set pressure below absolute threshold (minPressure - 10)
                var subThresholdPressure = deltaPressureEntity.Comp.MinPressure - 10;
                var moles = (subThresholdPressure * tile.Air!.Volume) / (Atmospherics.R * Atmospherics.T20C);
                tile.Air.AdjustMoles(Gas.Nitrogen, moles);
            });

            await RunTicks(30);

            // Entity should still exist since absolute pressure is below damage threshold
            await _pair.Server.WaitAssertion(() =>
            {
                Assert.That(!EntMan.Deleted(deltaPressureEntity), 
                    $"Entity should still exist after experiencing sub-threshold absolute pressure from {direction} direction!");
                
                // Clean up the pressure for next iteration
                var entityIndices = TransformSystem.GetGridOrMapTilePosition(deltaPressureEntity);
                var targetIndices = entityIndices.Offset(direction);
                var tile = GetTileAtmosphere(grid, targetIndices);
                tile?.Air?.Clear();
            });

            await RunTicks(30);
        }
    }

    /// <summary>
    /// Asserts that an entity that needs to be damaged by DeltaPressure
    /// is damaged by DeltaPressure when the absolute pressure is above the threshold.
    /// </summary>
    [Test]
    public async Task ProcessingAbsoluteDamageTest()
    {
        var grid = await CreateTestGrid();
        var testCoords = new EntityCoordinates(grid.Owner, Vector2.Zero);

        // Test all directions around the entity
        for (var i = 0; i < Atmospherics.Directions; i++)
        {
            var direction = (AtmosDirection)(1 << i);
            
            // Need to spawn a new entity for each direction test to ensure clean state
            var deltaPressureEntity = await SpawnTestEntity("DeltaPressureSolidTestAbsolute", testCoords);
            
            await _pair.Server.WaitAssertion(() =>
            {
                AssertProcessingListState(grid, deltaPressureEntity, true, 
                    "should be automatically added to processing list");
            });

            await _pair.Server.WaitPost(() =>
            {
                var entityIndices = TransformSystem.GetGridOrMapTilePosition(deltaPressureEntity);
                var targetIndices = entityIndices.Offset(direction);
                var tile = GetTileAtmosphere(grid, targetIndices);

                Assert.That(tile?.Air, Is.Not.Null, 
                    $"Tile at {targetIndices} should have air for direction {direction}!");

                // Set pressure above absolute threshold but below delta threshold
                // This ensures absolute pressure alone causes damage
                var aboveAbsoluteThresholdPressure = deltaPressureEntity.Comp.MinPressure + 10;
                var moles = (aboveAbsoluteThresholdPressure * tile.Air!.Volume) / (Atmospherics.R * Atmospherics.T20C);
                tile.Air.AdjustMoles(Gas.Nitrogen, moles);
            });

            await RunTicks(30);

            // Entity should be destroyed due to exceeding absolute pressure threshold
            await _pair.Server.WaitAssertion(() =>
            {
                Assert.That(EntMan.Deleted(deltaPressureEntity), 
                    $"Entity should be destroyed after experiencing above-threshold absolute pressure from {direction} direction!");
                
                // Clean up the pressure for next iteration
                var entityIndices = new Vector2i(0, 0); // Test coordinates are at (0,0)
                var targetIndices = entityIndices.Offset(direction);
                var tile = GetTileAtmosphere(grid, targetIndices);
                tile?.Air?.Clear();
            });

            await RunTicks(30);
        }
    }

    #endregion
}
