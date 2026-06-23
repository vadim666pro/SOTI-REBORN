using Content.Shared.Damage.Components;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.Damage.Systems;

/// <summary>
/// System that manages the damage sprite overlay.
/// </summary>
public sealed class DamageSpriteOverlaySystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IOverlayManager _overlayManager = default!;

    private DamageSpriteOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageSpriteComponent, ComponentInit>(OnSpriteCompInit);
        SubscribeLocalEvent<DamageSpriteComponent, ComponentShutdown>(OnSpriteCompShutdown);
        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnPlayerDetached);

        _overlay = new();
    }

    private void OnSpriteCompInit(EntityUid uid, DamageSpriteComponent component, ComponentInit args)
    {
        if (_playerManager.LocalEntity == uid)
            _overlayManager.AddOverlay(_overlay);
    }

    private void OnSpriteCompShutdown(EntityUid uid, DamageSpriteComponent component, ComponentShutdown args)
    {
        if (_playerManager.LocalEntity == uid)
            _overlayManager.RemoveOverlay(_overlay);
    }

    private void OnPlayerAttached(LocalPlayerAttachedEvent args)
    {
        if (TryComp<DamageSpriteComponent>(args.Entity, out var spriteComp))
            _overlayManager.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(LocalPlayerDetachedEvent args)
    {
        _overlayManager.RemoveOverlay(_overlay);
    }
}
