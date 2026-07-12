using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Client.Overlays;

/// <summary>
/// Full-screen post-process overlay for the bodycam effect.
/// Passes camera offset, fisheye, damage distortion, and glitch parameters to the shader.
/// </summary>
public sealed class BodycamOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> ShaderId = "Bodycam";

    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;

    private readonly ShaderInstance _shader;

    // ── Shader parameters (driven by BodycamOverlaySystem) ──────────────────
    public Vector2 CameraOffset { get; set; }
    public float DamageLevel { get; set; }
    public float DamageFlash { get; set; }
    public float GlitchIntensity { get; set; }

    // ── Static tuning (set once) ────────────────────────────────────────────
    public float FisheyeStrength { get; set; } = 0.05f;
    public float GrainStrength { get; set; } = 0.3f;
    public float EdgeBlurStrength { get; set; } = 0.08f;
    public float CornerRadius { get; set; } = 0.08f;
    public float CornerFeather { get; set; } = 0.04f;

    private float _prevTime;

    public BodycamOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototypeManager.Index(ShaderId).InstanceUnique();
        ZIndex = 9;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var handle = args.WorldHandle;
        var time = (float)_timing.CurTime.TotalSeconds;
        var dt = time - _prevTime;
        _prevTime = time;

        // Core
        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("time", time);
        _shader.SetParameter("deltaTime", dt);

        // Fisheye
        _shader.SetParameter("fisheyeStrength", FisheyeStrength);

        // Grain
        _shader.SetParameter("grainStrength", GrainStrength);

        // Edge blur
        _shader.SetParameter("edgeBlurStrength", EdgeBlurStrength);

        // Rounded corners
        _shader.SetParameter("cornerRadius", CornerRadius);
        _shader.SetParameter("cornerFeather", CornerFeather);

        // Damage / health
        _shader.SetParameter("damageLevel", DamageLevel);
        _shader.SetParameter("damageFlash", DamageFlash);

        // Glitch
        _shader.SetParameter("glitchIntensity", GlitchIntensity);

        handle.UseShader(_shader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}
