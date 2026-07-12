using Content.Client.Fov.Overlays;
using Content.Shared.Fov.Components;
using Content.Shared.Ghost;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.Fov.Systems;

/// <summary>
/// Manages the simple FOV overlay. Always active for alive players, hidden for ghosts.
/// </summary>
public sealed class FovLimiterSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;

    private SimpleFovOverlay? _overlay;
    private bool _overlayActive;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FovLimiterComponent, ComponentStartup>(OnFovLimiterStartup);
        SubscribeLocalEvent<FovLimiterComponent, ComponentShutdown>(OnFovLimiterShutdown);
        SubscribeLocalEvent<FovLimiterComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<FovLimiterComponent, PlayerDetachedEvent>(OnPlayerDetached);
    }

    private void OnFovLimiterStartup(EntityUid uid, FovLimiterComponent component, ComponentStartup args) { }
    private void OnFovLimiterShutdown(EntityUid uid, FovLimiterComponent component, ComponentShutdown args) { }
    private void OnPlayerAttached(EntityUid uid, FovLimiterComponent component, PlayerAttachedEvent args) { }
    private void OnPlayerDetached(EntityUid uid, FovLimiterComponent component, PlayerDetachedEvent args) { }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var playerUid = _playerManager.LocalEntity;
        if (playerUid == null)
        {
            RemoveOverlay();
            return;
        }

        if (_entMan.TryGetComponent<GhostComponent>(playerUid.Value, out _))
        {
            RemoveOverlay();
            return;
        }

        var hasLimiter = false;
        var query = EntityQueryEnumerator<FovLimiterComponent>();
        while (query.MoveNext(out var uid, out var limiter))
        {
            if (!limiter.Enabled)
                continue;

            if (!limiter.ApplyToAllPlayers && _playerManager.LocalPlayer?.ControlledEntity != uid)
                continue;

            hasLimiter = true;
            break;
        }

        if (hasLimiter)
            EnsureOverlay();
        else
            RemoveOverlay();
    }

    private void EnsureOverlay()
    {
        if (_overlayActive)
            return;

        _overlay ??= new SimpleFovOverlay(_prototypeManager);
        _overlayManager.AddOverlay(_overlay);
        _overlayActive = true;
    }

    private void RemoveOverlay()
    {
        if (!_overlayActive)
            return;

        if (_overlay != null)
            _overlayManager.RemoveOverlay(_overlay);
        _overlayActive = false;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        RemoveOverlay();
    }
}
