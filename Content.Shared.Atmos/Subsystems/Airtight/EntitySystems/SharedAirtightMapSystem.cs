namespace Content.Shared.Atmos.Subsystems.Airtight.EntitySystems;

/// <summary>
/// Abstract base for the airtight map. Making a generic chunkable map
/// is genuinely painful, so for now the server is the only one that has a map.
/// The client still knows about the concept of airtightness though and can query it.
/// </summary>
public abstract partial class SharedAirtightMapSystem : EntitySystem;
