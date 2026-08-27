using Robust.Client.Graphics;
using Robust.Shared.IoC;

namespace Content.Client.Overlays;

/// <summary>
/// Manages the BloomOverlay lifecycle: adds it when the game starts, removes on shutdown.
/// </summary>
public sealed class BloomOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;

    private BloomOverlay? _overlay;

    public override void Initialize()
    {
        base.Initialize();
        _overlay = new BloomOverlay();
        _overlayMan.AddOverlay(_overlay);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        if (_overlay != null)
        {
            _overlayMan.RemoveOverlay(_overlay);
            _overlay.Dispose();
            _overlay = null;
        }
    }
}
