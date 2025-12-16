using Robust.Shared.Serialization;

namespace Content.Shared.Power.EntitySystems;

/// <summary>
/// Shared APC power debug overlay networking primitives, modeled after SharedAtmosDebugOverlaySystem.
/// </summary>
public abstract class SharedApcPowerDebugOverlaySystem : EntitySystem
{
    public const float LocalViewRange = 16f;

    protected float AccumulatedFrameTime;

    [Serializable, NetSerializable]
    public readonly record struct ApcPowerDebugOverlayData(NetEntity Receiver, float Load);

    [Serializable, NetSerializable]
    public sealed class ApcPowerDebugOverlayMessage(List<ApcPowerDebugOverlayData> overlayData) : EntityEventArgs
    {
        public List<ApcPowerDebugOverlayData> OverlayData { get; } = overlayData;
    }

    [Serializable, NetSerializable]
    public sealed class ApcPowerDebugOverlayDisableMessage : EntityEventArgs;
}

