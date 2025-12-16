using Content.Client.Power.Components;
using Content.Client.Power.EntitySystems;
using Content.Client.Resources;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;

namespace Content.Client.Power;

public sealed class ApcPowerRecieverLoadDebugOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    private readonly SharedTransformSystem _transform;
    private readonly EntityLookupSystem _lookup;
    private readonly ApcPowerDebugOverlaySystem _apcDebug;
    private readonly HashSet<Entity<ApcPowerReceiverComponent>> _intersecting = [];

    private readonly Font _font;
    private const string TextFontPath = "/Fonts/NotoSans/NotoSans-Regular.ttf";
    private const int TextFontSize = 12;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    public ApcPowerRecieverLoadDebugOverlay(ApcPowerDebugOverlaySystem apcDebug)
    {
        _apcDebug = apcDebug;
        IoCManager.InjectDependencies(this);

        _transform = _entManager.System<SharedTransformSystem>();
        _lookup = _entManager.System<EntityLookupSystem>();
        _font = _resourceCache.GetFont(TextFontPath, TextFontSize);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        _intersecting.Clear();
        _lookup.GetEntitiesIntersecting(args.MapId, args.WorldBounds, _intersecting);

        if (args.ViewportControl == null)
            return;

        foreach (var ent in _intersecting)
        {
            if (!_apcDebug.TryGetData(ent.Owner, out var data))
                continue;

            var (worldPos, _) = _transform.GetWorldPositionRotation(ent.Owner);

            var load = data.Load;
            var text = $"Load: {load:N0}";
            var screenPos = args.ViewportControl.WorldToScreen(worldPos);
            args.ScreenHandle.DrawString(_font, screenPos, text, Color.Gold);
        }
    }
}
