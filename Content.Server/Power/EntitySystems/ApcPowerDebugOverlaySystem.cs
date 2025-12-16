using System.Numerics;
using Content.Server.Power.Components;
using Content.Shared.CCVar;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Power;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server.Power.EntitySystems;

[UsedImplicitly]
public sealed class ApcPowerDebugOverlaySystem : SharedApcPowerDebugOverlaySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    /// <summary>
    /// Players allowed to see the APC power debug overlay.
    /// </summary>
    private readonly HashSet<ICommonSession> _playerObservers = new();

    private float _updateCooldown;

    public override void Initialize()
    {
        base.Initialize();
        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
        SubscribeNetworkEvent<RequestApcPowerDebugOverlayEvent>(OnRequestOverlay);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
        _playerObservers.Clear();
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus != SessionStatus.InGame)
            RemoveObserver(e.Session);
    }

    private void OnRequestOverlay(RequestApcPowerDebugOverlayEvent ev, EntitySessionEventArgs args)
    {
        ToggleObserver(args.SenderSession);
    }

    public bool AddObserver(ICommonSession observer)
    {
        return _playerObservers.Add(observer);
    }

    public bool HasObserver(ICommonSession observer)
    {
        return _playerObservers.Contains(observer);
    }

    public bool RemoveObserver(ICommonSession observer)
    {
        if (!_playerObservers.Remove(observer))
            return false;

        var disable = new ApcPowerDebugOverlayDisableMessage();
        RaiseNetworkEvent(disable, observer.Channel);
        return true;
    }


    public bool ToggleObserver(ICommonSession observer)
    {
        if (HasObserver(observer))
        {
            RemoveObserver(observer);
            return false;
        }

        AddObserver(observer);
        return true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_playerObservers.Count == 0)
            return;

        AccumulatedFrameTime += frameTime;
        _updateCooldown = 1f / _configManager.GetCVar(CCVars.NetAtmosDebugOverlayTickRate);

        if (AccumulatedFrameTime < _updateCooldown)
            return;

        AccumulatedFrameTime -= _updateCooldown;

        foreach (var session in _playerObservers)
        {
            if (session.AttachedEntity is not { Valid: true } entity)
                continue;

            var xform = Transform(entity);
            if (xform.MapID == MapId.Nullspace)
                continue;

            var worldPos = _transform.GetWorldPosition(xform);
            var worldBounds = Box2.CenteredAround(worldPos, new Vector2(LocalViewRange, LocalViewRange));

            var overlayData = new List<ApcPowerDebugOverlayData>();
            var receivers = new HashSet<Entity<ApcPowerReceiverComponent>>();
            _lookup.GetEntitiesIntersecting(xform.MapID, worldBounds, receivers);

            foreach (var receiver in receivers)
            {
                var load = receiver.Comp.Load;
                if (load <= 0f)
                    continue;

                overlayData.Add(new ApcPowerDebugOverlayData(GetNetEntity(receiver.Owner), load));
            }

            if (overlayData.Count == 0)
                continue;

            var msg = new ApcPowerDebugOverlayMessage(overlayData);
            RaiseNetworkEvent(msg, session.Channel);
        }
    }
}
