using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Client.Overlays;

/// <summary>
/// Two-pass bloom (glow) post-processing overlay.
/// Extracts bright pixels, blurs them with a separable gaussian, and composites back.
/// </summary>
public sealed class BloomOverlay : Overlay
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;

    // Tuning
    public float Threshold { get; set; } = 0.35f;
    public float Intensity { get; set; } = 1.2f;
    public int BlurPasses { get; set; } = 3; // each pass = horizontal + vertical

    private readonly ShaderInstance _extractShader;
    private readonly ShaderInstance _blurShader;
    private readonly ShaderInstance _combineShader;

    // Intermediate render targets, lazily created / resized
    private IRenderTexture? _rtBright;
    private IRenderTexture? _rtBlurA;
    private IRenderTexture? _rtBlurB;

    public BloomOverlay()
    {
        IoCManager.InjectDependencies(this);

        _extractShader = _proto.Index(new ProtoId<ShaderPrototype>("BloomExtract")).InstanceUnique();
        _blurShader = _proto.Index(new ProtoId<ShaderPrototype>("BloomBlur")).InstanceUnique();
        _combineShader = _proto.Index(new ProtoId<ShaderPrototype>("BloomCombine")).InstanceUnique();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var handle = args.WorldHandle;
        var vpSize = args.ViewportBounds.Size;
        // Capture WorldBounds before lambdas (in parameter cannot be captured)
        var worldBounds = args.WorldBounds;

        // Ensure intermediate RTs exist at the right size
        EnsureRenderTargets(vpSize);

        var texelSize = new Vector2(1f / vpSize.X, 1f / vpSize.Y);

        // ── Pass 1: extract bright pixels into _rtBright ──────────────────────
        _extractShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _extractShader.SetParameter("bloomThreshold", Threshold);

        handle.RenderInRenderTarget(_rtBright!, () =>
        {
            handle.UseShader(_extractShader);
            handle.DrawRect(worldBounds, Color.White);
            handle.UseShader(null);
        }, Color.Black);

        // ── Pass 2: multi-pass gaussian blur ──────────────────────────────────
        // Each "pass" does horizontal then vertical, ping-ponging between _rtBlurA and _rtBlurB.
        var source = _rtBright!;
        for (var i = 0; i < BlurPasses; i++)
        {
            // Horizontal blur: source → _rtBlurA
            _blurShader.SetParameter("SCREEN_TEXTURE", source.Texture);
            _blurShader.SetParameter("blurDirection", new Vector2(1f, 0f));
            _blurShader.SetParameter("texelSize", texelSize);

            handle.RenderInRenderTarget(_rtBlurA!, () =>
            {
                handle.UseShader(_blurShader);
                handle.DrawRect(worldBounds, Color.White);
                handle.UseShader(null);
            }, Color.Black);

            // Vertical blur: _rtBlurA → _rtBlurB
            _blurShader.SetParameter("SCREEN_TEXTURE", _rtBlurA!.Texture);
            _blurShader.SetParameter("blurDirection", new Vector2(0f, 1f));
            _blurShader.SetParameter("texelSize", texelSize);

            handle.RenderInRenderTarget(_rtBlurB!, () =>
            {
                handle.UseShader(_blurShader);
                handle.DrawRect(worldBounds, Color.White);
                handle.UseShader(null);
            }, Color.Black);

            source = _rtBlurB!;
        }

        // ── Pass 3: combine original scene + blurred bloom ────────────────────
        _combineShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _combineShader.SetParameter("bloomTexture", _rtBlurB!.Texture);
        _combineShader.SetParameter("bloomIntensity", Intensity);

        handle.UseShader(_combineShader);
        handle.DrawRect(worldBounds, Color.White);
        handle.UseShader(null);
    }

    private void EnsureRenderTargets(Vector2i size)
    {
        var format = new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb);

        if (_rtBright == null || _rtBright.Size != size)
        {
            _rtBright?.Dispose();
            _rtBright = _clyde.CreateRenderTarget(size, format, name: "bloom-bright");
        }

        if (_rtBlurA == null || _rtBlurA.Size != size)
        {
            _rtBlurA?.Dispose();
            _rtBlurA = _clyde.CreateRenderTarget(size, format, name: "bloom-blur-a");
        }

        if (_rtBlurB == null || _rtBlurB.Size != size)
        {
            _rtBlurB?.Dispose();
            _rtBlurB = _clyde.CreateRenderTarget(size, format, name: "bloom-blur-b");
        }
    }

    protected override void DisposeBehavior()
    {
        base.DisposeBehavior();
        _rtBright?.Dispose();
        _rtBlurA?.Dispose();
        _rtBlurB?.Dispose();
        _rtBright = null;
        _rtBlurA = null;
        _rtBlurB = null;
    }
}
