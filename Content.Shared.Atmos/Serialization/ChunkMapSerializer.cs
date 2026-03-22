using System.Linq;
using Content.Shared.Atmos.Collections.Spatial;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared.Atmos.Serialization;

[TypeSerializer]
public sealed class ChunkMapSerializer<T> : ITypeSerializer<ChunkMap<T>, MappingDataNode>, ITypeCopier<ChunkMap<T>>
{
    public ValidationNode Validate(ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        var mapping = new Dictionary<ValidationNode, ValidationNode>
        {
            [new ValidatedValueNode(new ValueDataNode("chunkSize"))] =
                node.TryGetValue("chunkSize", out var chunkSizeNode)
                    ? serializationManager.ValidateNode<int>(chunkSizeNode, context)
                    : new ErrorNode(node, "Missing required field: chunkSize")
        };

        if (node.TryGetValue("chunks", out var chunksNode) && chunksNode is MappingDataNode chunksMapping)
        {
            var validatedChunks = new Dictionary<ValidationNode, ValidationNode>();
            foreach (var (key, value) in chunksMapping)
            {
                validatedChunks.Add(
                    serializationManager.ValidateNode<Vector2i>(chunksMapping.GetKeyNode(key), context),
                    serializationManager.ValidateNode<T>(value, context));
            }

            mapping[new ValidatedValueNode(new ValueDataNode("chunks"))] = new ValidatedMappingNode(validatedChunks);
        }

        return new ValidatedMappingNode(mapping);
    }

    public ChunkMap<T> Read(ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<ChunkMap<T>>? instanceProvider = null)
    {
        var chunkSize = serializationManager.Read<int>(node["chunkSize"], hookCtx, context);
        var map = instanceProvider != null ? instanceProvider() : new ChunkMap<T>(chunkSize);

        if (map.ChunkSize != chunkSize)
            map = new ChunkMap<T>(chunkSize);
        else
            map.Clear();

        if (!node.TryGet<MappingDataNode>("chunks", out var chunksNode))
            return map;

        foreach (var (chunkKey, valueNode) in chunksNode)
        {
            var chunk = serializationManager.Read<Vector2i>(chunksNode.GetKeyNode(chunkKey), hookCtx, context);
            var value = serializationManager.Read<T>(valueNode, hookCtx, context);
            map.SetChunk(chunk, value);
        }

        return map;
    }

    public DataNode Write(ISerializationManager serializationManager,
        ChunkMap<T> value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        var chunksNode = new MappingDataNode();
        var chunks = value.EnumerateChunks().ToList();
        chunks.Sort(static (a, b) =>
        {
            var x = a.Chunk.X.CompareTo(b.Chunk.X);
            return x != 0 ? x : a.Chunk.Y.CompareTo(b.Chunk.Y);
        });

        foreach (var (chunk, chunkValue) in chunks)
        {
            var keyNode = serializationManager.WriteValue(chunk, alwaysWrite, context);
            if (keyNode is not ValueDataNode valueNode)
                throw new NotSupportedException("ChunkMap chunk key did not serialize to ValueDataNode.");

            chunksNode.Add(valueNode.Value, serializationManager.WriteValue(chunkValue, alwaysWrite, context));
        }

        return new MappingDataNode
        {
            { "chunkSize", serializationManager.WriteValue(value.ChunkSize, alwaysWrite, context) },
            { "chunks", chunksNode }
        };
    }

    public void CopyTo(ISerializationManager serializationManager,
        ChunkMap<T> source,
        ref ChunkMap<T> target,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null)
    {
        if (target.ChunkSize != source.ChunkSize)
            target = new ChunkMap<T>(source.ChunkSize);
        else
            target.Clear();

        foreach (var (chunk, value) in source.EnumerateChunks())
        {
            target.SetChunk(chunk, serializationManager.CreateCopy(value, hookCtx, context));
        }
    }
}
