using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Maths;
using Robust.Shared.Enums;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.Timing;

namespace Content.Client.GameTicking
{
    public sealed class RoleRevealOverlay : Overlay
    {
        private readonly IResourceCache _resCache;
        private readonly IGameTiming _timing;

        private Texture? _texture;
        private Font? _fontLarge;

        private string _roleText = string.Empty;
        private string? _antagText;

        private TimeSpan _startTime;
        private float _displayTime = 4f;
        private float _fadeTime = 3f;
        private bool _active = false;

        public override OverlaySpace Space => OverlaySpace.ScreenSpace;

        public RoleRevealOverlay()
        {
            IoCManager.InjectDependencies(this);
            _resCache = IoCManager.Resolve<IResourceCache>();
            _timing = IoCManager.Resolve<IGameTiming>();

            // Ensure this overlay is drawn on top of everything else.
            ZIndex = int.MaxValue;

            // lazy font load
            try
            {
                _fontLarge = new VectorFont(_resCache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-BoldItalic.ttf"), 48);
            }
            catch
            {
                _fontLarge = null;
            }
        }

        public override bool OverwriteTargetFrameBuffer => true;

        public void Start(string imagePath, string roleText, string? antagText, float displayTime, float fadeTime)
        {
            _roleText = $"ВАША РОЛЬ: {roleText}";
            _antagText = antagText;
            _displayTime = displayTime;
            _fadeTime = fadeTime;
            _startTime = _timing.CurTime;
            _active = true;

            try
            {
                var res = _resCache.GetResource<TextureResource>(imagePath);
                _texture = res.Texture;
            }
            catch
            {
                _texture = null;
            }
        }

        protected override bool BeforeDraw(in OverlayDrawArgs args)
        {
            return _active;
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            var screenHandle = args.ScreenHandle;
            // Use ViewportBounds to cover the full screen-space area available to this viewport.
            var vb = args.ViewportBounds;
            var viewportTopLeft = new Vector2(vb.Left, vb.Top);
            var sizei = vb.Size;
            var viewportSize = new Vector2(sizei.X, sizei.Y);

            var elapsed = (float)(_timing.CurTime - _startTime).TotalSeconds;
            var alpha = 1f;

            if (elapsed > _displayTime)
            {
                var t = (elapsed - _displayTime) / MathF.Max(0.001f, _fadeTime);
                alpha = 1f - MathF.Min(1f, MathF.Max(0f, t));
            }

            if (alpha <= 0f)
            {
                _active = false;
                return;
            }

            // Draw full-screen image stretched
            if (_texture != null)
            {
                var rect = UIBox2.FromDimensions(viewportTopLeft, viewportSize);
                screenHandle.DrawTextureRect(_texture, rect, Color.White.WithAlpha(alpha));
            }

            // Draw text centered
            var font = _fontLarge ?? new VectorFont(_resCache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-BoldItalic.ttf"), 48);
            var scale = 1f;
            var text = _roleText + (_antagText != null ? "\n" + _antagText : string.Empty);
            var dims = screenHandle.GetDimensions(font, text, scale);
            var pos = viewportTopLeft + viewportSize / 2f - dims / 2f;
            screenHandle.DrawString(font, pos, text, scale, Color.White.WithAlpha(alpha));
        }
    }
}
