using Content.Shared.Damage.Components;
using Robust.Client.Graphics;
using Robust.Client.Player;
using System.Numerics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Graphics.RSI;

namespace Content.Client.Damage;

/// <summary>
/// Overlay that renders damage sprites on screen.
/// </summary>
public sealed class DamageSpriteOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    [Dependency] private readonly IResourceCache _resCache = default!;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    private const string DamageSpriteRsi = "/Textures/Objects/counterstrike/blood.rsi";
    private static readonly string[] SpriteStates = { "blood1", "blood2", "blood3" };

    private RSI? _bloodRsi;

    public DamageSpriteOverlay()
    {
        IoCManager.InjectDependencies(this);
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        var playerEntity = _playerManager.LocalEntity;

        if (playerEntity == null)
            return false;

        if (!_entityManager.TryGetComponent<DamageSpriteComponent>(playerEntity, out var spriteComp))
            return false;

        if (spriteComp.Sprites.Count == 0)
            return false;

        return true;
    }

        protected override void Draw(in OverlayDrawArgs args)
    {
        var playerEntity = _playerManager.LocalEntity;

        if (playerEntity == null)
            return;

        if (!_entityManager.TryGetComponent<DamageSpriteComponent>(playerEntity, out var spriteComp))
            return;

            var screenHandle = args.ScreenHandle;
            var viewportSize = args.Viewport.Size;

            // Try load RSI once
            if (_bloodRsi == null)
            {
                try
                {
                    _bloodRsi = _resCache.GetResource<RSIResource>(DamageSpriteRsi).RSI;
                }
                catch
                {
                    _bloodRsi = null;
                }
            }

            foreach (var sprite in spriteComp.Sprites)
            {
                var position = new Vector2(
                    sprite.Position.X * viewportSize.X,
                    sprite.Position.Y * viewportSize.Y
                );

                var color = Color.Red.WithAlpha(sprite.Opacity * 0.6f);

                // If RSI and a state are available, draw the RSI frame; otherwise fallback to circle
                if (_bloodRsi != null)
                {
                    // Use the per-sprite state index chosen on the server/shared side.
                    var idx = sprite.StateIndex;
                    if (idx < 0 || idx >= SpriteStates.Length)
                        idx = 0;

                    var stateName = SpriteStates[idx];

                    if (_bloodRsi.TryGetState(stateName, out var state))
                    {
                        var texture = state.Frame0;
                        // draw the texture centered at the position
                        var originedPos = position - (Vector2)texture.Size / 2f;
                        screenHandle.DrawTexture(texture, originedPos, color);
                        continue;
                    }
                }

                screenHandle.DrawCircle(position, 20 * sprite.Scale, color);
            }
    }
}
