using Content.Shared.Atmos.Collections.Spatial;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared.Atmos.Serialization;

[TypeSerializer]
public sealed class MortonArraySerializer<T> : ITypeSerializer<MortonArray<T>, MappingDataNode>,
    ITypeCopier<MortonArray<T>>
{
    public ValidationNode Validate(ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        var mapping = new Dictionary<ValidationNode, ValidationNode>
        {
            [new ValidatedValueNode(new ValueDataNode("sideLength"))] =
                node.TryGetValue("sideLength", out var sideNode)
                    ? serializationManager.ValidateNode<int>(sideNode, context)
                    : new ErrorNode(node, "Missing required field: sideLength"),
            [new ValidatedValueNode(new ValueDataNode("count"))] =
                node.TryGetValue("count", out var countNode)
                    ? serializationManager.ValidateNode<int>(countNode, context)
                    : new ErrorNode(node, "Missing required field: count")
        };

        if (node.TryGetValue("data", out var dataNode) && dataNode is SequenceDataNode sequence)
        {
            var list = new List<ValidationNode>(sequence.Count);
            foreach (var elem in sequence)
            {
                list.Add(serializationManager.ValidateNode<T>(elem, context));
            }

            mapping[new ValidatedValueNode(new ValueDataNode("data"))] = new ValidatedSequenceNode(list);
        }
        else
        {
            mapping[new ValidatedValueNode(new ValueDataNode("data"))] =
                new ErrorNode(node, "Missing required field: data");
        }

        return new ValidatedMappingNode(mapping);
    }

    public MortonArray<T> Read(ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<MortonArray<T>>? instanceProvider = null)
    {
        var sideLength = serializationManager.Read<int>(node["sideLength"], hookCtx, context);
        var count = serializationManager.Read<int>(node["count"], hookCtx, context);
        var dataNode = node.Cast<SequenceDataNode>("data");

        var array = instanceProvider != null ? instanceProvider() : new MortonArray<T>(sideLength);
        if (array.SideLength != sideLength)
            array = new MortonArray<T>(sideLength);

        array.Wipe();

        var i = 0;
        foreach (var elem in dataNode)
        {
            if (i >= array.RawLength)
                break;

            array.SetRawValue(i, serializationManager.Read<T>(elem, hookCtx, context));
            i++;
        }

        array.SetCountUnsafe(count);
        return array;
    }

    public DataNode Write(ISerializationManager serializationManager,
        MortonArray<T> value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        var data = new SequenceDataNode();
        foreach (var elem in value)
        {
            data.Add(serializationManager.WriteValue(elem, alwaysWrite, context));
        }

        return new MappingDataNode
        {
            { "sideLength", serializationManager.WriteValue(value.SideLength, alwaysWrite, context) },
            { "count", serializationManager.WriteValue(value.Count, alwaysWrite, context) },
            { "data", data }
        };
    }

    public void CopyTo(ISerializationManager serializationManager,
        MortonArray<T> source,
        ref MortonArray<T> target,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null)
    {
        if (target.SideLength != source.SideLength)
            target = new MortonArray<T>(source.SideLength);

        target.Wipe();

        var i = 0;
        foreach (var elem in source)
        {
            if (i >= target.RawLength)
                break;

            target.SetRawValue(i, serializationManager.CreateCopy(elem, hookCtx, context));
            i++;
        }

        target.SetCountUnsafe(source.Count);
    }
}
