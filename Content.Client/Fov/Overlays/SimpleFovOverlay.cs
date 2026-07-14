using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client.Fov.Overlays;

/// <summary>
/// World-space overlay that draws a black mask outside the player's 120° FOV cone.
/// Stencil: writes the VISIBLE cone + player circle to stencil, then fills outside with black.
/// </summary>
public sealed class SimpleFovOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> StencilClearId = "StencilClear";
    private static readonly ProtoId<ShaderPrototype> StencilMaskId = "StencilMask";
    private static readonly ProtoId<ShaderPrototype> StencilDrawId = "StencilDraw";

    private readonly IPlayerManager _player;
    private readonly IEntityManager _entMan;
    private readonly SharedTransformSystem _transform;
    private readonly ShaderInstance _stencilClear;
    private readonly ShaderInstance _stencilMask;
    private readonly ShaderInstance _stencilDraw;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    public SimpleFovOverlay(IPrototypeManager prototypeManager)
    {
        _player = IoCManager.Resolve<IPlayerManager>();
        _entMan = IoCManager.Resolve<IEntityManager>();
        _transform = _entMan.System<SharedTransformSystem>();
        _stencilClear = prototypeManager.Index(StencilClearId).InstanceUnique();
        _stencilMask = prototypeManager.Index(StencilMaskId).InstanceUnique();
        _stencilDraw = prototypeManager.Index(StencilDrawId).InstanceUnique();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;

        var playerUid = _player.LocalEntity;
        if (playerUid == null)
            return;

        if (!_entMan.TryGetComponent<TransformComponent>(playerUid.Value, out var xform))
            return;

        if (xform.MapID != args.MapId)
            return;

        var center = _transform.GetWorldPosition(xform);
        var playerAngle = _transform.GetWorldRotation(xform);

        // Subtract 90° to compensate for Robust rendering axis offset
        var baseAngle = (float)playerAngle.Theta - MathF.PI * 0.5f;

        // 120° FOV: ±60°
        var halfFov = MathHelper.DegreesToRadians(60f);
        var leftFovAngle = baseAngle + halfFov;
        var rightFovAngle = baseAngle - halfFov;

        // Build 120° cone as triangle fan (VISIBLE area)
        const int Segments = 48;
        var coneVerts = new Vector2[Segments + 2];
        coneVerts[0] = center;
        for (var i = 0; i <= Segments; i++)
        {
            var t = i / (float)Segments;
            var ang = (float)(rightFovAngle + 2.0 * halfFov * t);
            coneVerts[i + 1] = center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 30f;
        }

        // 1) Clear stencil to 0
        handle.UseShader(_stencilClear);
        handle.DrawRect(args.WorldBounds, Color.White);

        // 2) Write VISIBLE cone to stencil = 1 (transparent area)
        handle.UseShader(_stencilMask);
        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, coneVerts, Color.White);

        // 2b) Also write player circle to stencil = 1
        handle.DrawCircle(center, 0.6f, Color.White, true);

        // 3) Draw black OUTSIDE stencil (where stencil != 1 = blind zone)
        handle.UseShader(_stencilDraw);
        handle.DrawRect(args.WorldBounds, Color.Black.WithAlpha(0.85f));

        handle.UseShader(null);
    }
}
