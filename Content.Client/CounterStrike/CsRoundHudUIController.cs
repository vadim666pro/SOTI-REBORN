using System.Numerics;
using Content.Client.Gameplay;
using Content.Client.GameTicking;
using Content.Shared.CounterStrike.Events;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Timing;

namespace Content.Client.CounterStrike;

/// <summary>
/// Client-side HUD controller that displays the CS round timer, score, and round number
/// at the top-center of the screen.
/// </summary>
public sealed class CsRoundHudUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>
{
    [Dependency] private readonly IGameTiming _timing = default!;

    private CsRoundHudControl? _control;

    private float _timerRemaining;
    private string _phase = string.Empty;
    private int _ctWins;
    private int _tWins;
    private int _roundNumber;
    private int _maxRounds;
    private bool _active;
    private bool _bombPlanted;
    private float _bombTimerRemaining;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<CsRoundHudEvent>(OnHudEvent);
        SubscribeNetworkEvent<CsRoundHudClearEvent>(OnHudClear);
    }

    public void OnStateEntered(GameplayState state)
    {
        _control = new CsRoundHudControl();
        UIManager.RootControl.AddChild(_control);
        UpdateControl();
    }

    public void OnStateExited(GameplayState state)
    {
        if (_control == null)
            return;
        UIManager.RootControl.RemoveChild(_control);
        _control.Dispose();
        _control = null;
    }

    private void OnHudEvent(CsRoundHudEvent ev, EntitySessionEventArgs args)
    {
        _timerRemaining = ev.TimerRemaining;
        _phase = ev.Phase;
        _ctWins = ev.CtWins;
        _tWins = ev.TWins;
        _roundNumber = ev.RoundNumber;
        _maxRounds = ev.MaxRounds;
        _bombPlanted = ev.BombPlanted;
        _bombTimerRemaining = ev.BombTimerRemaining;
        _active = true;
        UpdateControl();
    }

    private void OnHudClear(CsRoundHudClearEvent ev, EntitySessionEventArgs args)
    {
        _active = false;
        UpdateControl();
    }

    private void UpdateControl()
    {
        if (_control == null)
            return;
        _control.SetData(_timerRemaining, _phase, _ctWins, _tWins, _roundNumber, _maxRounds, _active, _timing, _bombPlanted, _bombTimerRemaining);
    }

    private sealed class CsRoundHudControl : Control
    {
        private Font _fontLarge = default!;
        private Font _fontMedium = default!;
        private Font _fontSmall = default!;

        private float _timerRemaining;
        private string _phase = string.Empty;
        private int _ctWins;
        private int _tWins;
        private int _roundNumber;
        private int _maxRounds;
        private bool _active;
        private IGameTiming? _timing;
        private bool _bombPlanted;
        private float _bombTimerRemaining;

        // Colors
        private static readonly Color CtColor = new Color(80, 160, 255);   // blue-ish
        private static readonly Color TColor = new Color(255, 180, 60);    // orange-ish
        private static readonly Color TimerColor = Color.White;
        private static readonly Color BombTimerColor = new Color(255, 50, 50);  // red for bomb
        private static readonly Color PhaseColor = new Color(200, 200, 200);
        private static readonly Color RoundColor = new Color(160, 160, 160);
        private static readonly Color SeparatorColor = new Color(100, 100, 100);
        private static readonly Color BackgroundColor = new Color(0, 0, 0, 160);

        public CsRoundHudControl()
        {
            MouseFilter = MouseFilterMode.Ignore;
            var cache = IoCManager.Resolve<IResourceCache>();
            _fontLarge = new VectorFont(cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Bold.ttf"), 22);
            _fontMedium = new VectorFont(cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 16);
            _fontSmall = new VectorFont(cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 13);
        }

        public void SetData(float timerRemaining, string phase, int ctWins, int tWins,
            int roundNumber, int maxRounds, bool active, IGameTiming timing,
            bool bombPlanted, float bombTimerRemaining)
        {
            _timerRemaining = timerRemaining;
            _phase = phase;
            _ctWins = ctWins;
            _tWins = tWins;
            _roundNumber = roundNumber;
            _maxRounds = maxRounds;
            _active = active;
            _timing = timing;
            _bombPlanted = bombPlanted;
            _bombTimerRemaining = bombTimerRemaining;
            Visible = active;
        }

        protected override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);

            if (!_active || _timing == null)
                return;

            var overlayManager = IoCManager.Resolve<IOverlayManager>();
            if (overlayManager.HasOverlay<RoleRevealOverlay>())
                return;

            // --- Prepare texts ---
            string timerText;
            Color timerColor;

            if (_bombPlanted)
            {
                // Bomb countdown — blink red every second
                var bombSeconds = MathF.Max(0f, _bombTimerRemaining);
                var displaySeconds = (int)MathF.Ceiling(bombSeconds);
                timerText = $"C4 {displaySeconds}";

                // Blink: show red during first half of each second, white during second half
                var fractionInSecond = bombSeconds - MathF.Floor(bombSeconds);
                timerColor = fractionInSecond > 0.5f ? BombTimerColor : Color.White;
            }
            else
            {
                var minutes = (int)(_timerRemaining / 60f);
                var seconds = (int)MathF.Round(_timerRemaining % 60f, MidpointRounding.AwayFromZero);
                if (seconds == 60) { minutes += 1; seconds = 0; }
                timerText = $"{minutes:00}:{seconds:00}";
                timerColor = TimerColor;
            }

            var phaseText = _phase.ToUpperInvariant();
            var roundText = $"ROUND {_roundNumber}";

            // --- Measure ---
            var phaseDims = handle.GetDimensions(_fontSmall, phaseText, UIScale);
            var roundDims = handle.GetDimensions(_fontSmall, roundText, UIScale);
            var timerDims = handle.GetDimensions(_fontLarge, timerText, UIScale);
            var ctDims = handle.GetDimensions(_fontMedium, "CT", UIScale);
            var tDims = handle.GetDimensions(_fontMedium, "T", UIScale);
            var ctWinsText = _ctWins.ToString();
            var tWinsText = _tWins.ToString();
            var ctWinsDims = handle.GetDimensions(_fontLarge, ctWinsText, UIScale);
            var tWinsDims = handle.GetDimensions(_fontLarge, tWinsText, UIScale);

            // --- Layout: 3 rows ---
            var gap = 10f;
            var smallGap = 6f;
            var padding = 14f;
            var rowGap = 3f;

            var middleRowHeight = MathF.Max(timerDims.Y, MathF.Max(ctDims.Y, tDims.Y));
            middleRowHeight = MathF.Max(middleRowHeight, MathF.Max(ctWinsDims.Y, tWinsDims.Y));

            var midWidth = ctDims.X + smallGap + ctWinsDims.X + gap + timerDims.X + gap + tWinsDims.X + smallGap + tDims.X;
            var bgWidth = MathF.Max(midWidth, MathF.Max(phaseDims.X, roundDims.X)) + padding * 2;
            var bgHeight = phaseDims.Y + rowGap + middleRowHeight + rowGap + roundDims.Y + padding * 2;

            var bgX = (PixelSize.X - bgWidth) / 2f;
            var bgY = 8f;

            handle.DrawRect(new UIBox2(bgX, bgY, bgX + bgWidth, bgY + bgHeight), BackgroundColor);

            var cx = bgX + padding;
            var cy = bgY + padding;

            // --- Row 1: Phase ---
            handle.DrawString(_fontSmall, new Vector2(cx + (bgWidth - padding * 2 - phaseDims.X) / 2f, cy), phaseText, UIScale, PhaseColor);
            cy += phaseDims.Y + rowGap;

            // --- Row 2: CT [wins] | timer | [wins] T ---
            var rowY = cy + (middleRowHeight - timerDims.Y) / 2f;
            var rowStartX = cx + (bgWidth - padding * 2 - midWidth) / 2f;
            var x = rowStartX;

            // CT label
            handle.DrawString(_fontMedium, new Vector2(x, cy + (middleRowHeight - ctDims.Y) / 2f), "CT", UIScale, CtColor);
            x += ctDims.X + smallGap;

            // CT wins
            handle.DrawString(_fontLarge, new Vector2(x, cy + (middleRowHeight - ctWinsDims.Y) / 2f), ctWinsText, UIScale, CtColor);
            x += ctWinsDims.X + gap;

            // Timer (bomb or regular)
            handle.DrawString(_fontLarge, new Vector2(x, rowY), timerText, UIScale, timerColor);
            x += timerDims.X + gap;

            // T wins
            handle.DrawString(_fontLarge, new Vector2(x, cy + (middleRowHeight - tWinsDims.Y) / 2f), tWinsText, UIScale, TColor);
            x += tWinsDims.X + smallGap;

            // T label
            handle.DrawString(_fontMedium, new Vector2(x, cy + (middleRowHeight - tDims.Y) / 2f), "T", UIScale, TColor);

            cy += middleRowHeight + rowGap;

            // --- Row 3: Round ---
            handle.DrawString(_fontSmall, new Vector2(cx + (bgWidth - padding * 2 - roundDims.X) / 2f, cy), roundText, UIScale, RoundColor);
        }
    }
}
