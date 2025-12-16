using Content.Shared.Power.EntitySystems;
using JetBrains.Annotations;
using Robust.Client.Graphics;

namespace Content.Client.Power.EntitySystems;

[UsedImplicitly]
public sealed class ApcPowerDebugOverlaySystem : SharedApcPowerDebugOverlaySystem
{
    private readonly Dictionary<EntityUid, ApcPowerDebugOverlayData> _receiverData = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ApcPowerDebugOverlayMessage>(HandleOverlayMessage);
        SubscribeNetworkEvent<ApcPowerDebugOverlayDisableMessage>(HandleDisableMessage);

        var overlayManager = IoCManager.Resolve<IOverlayManager>();
        if (!overlayManager.HasOverlay<ApcPowerRecieverLoadDebugOverlay>())
            overlayManager.AddOverlay(new ApcPowerRecieverLoadDebugOverlay(this));
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _receiverData.Clear();

        var overlayManager = IoCManager.Resolve<IOverlayManager>();
        if (overlayManager.HasOverlay<ApcPowerRecieverLoadDebugOverlay>())
            overlayManager.RemoveOverlay<ApcPowerRecieverLoadDebugOverlay>();
    }

    private void HandleOverlayMessage(ApcPowerDebugOverlayMessage msg)
    {
        _receiverData.Clear();

        foreach (var data in msg.OverlayData)
        {
            var uid = GetEntity(data.Receiver);
            _receiverData[uid] = data;
        }
    }

    private void HandleDisableMessage(ApcPowerDebugOverlayDisableMessage msg)
    {
        _receiverData.Clear();
    }

    public bool TryGetData(EntityUid uid, out ApcPowerDebugOverlayData data)
    {
        return _receiverData.TryGetValue(uid, out data);
    }
}
