using System;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Enums;
using Robust.Shared.Timing;
using Robust.Shared.GameObjects;
using Content.Client.GameTicking.Managers;
using Content.Client.Stylesheets;

namespace Content.Client.Overlays;

/// <summary>
/// Screen-space HUD overlay for the bodycam: REC indicator, date/time, round timer, corner brackets.
/// </summary>
public sealed class BodycamHudOverlay : Overlay
{
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IUserInterfaceManager _uiMan = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;

    private Font _font = default!;
    private Font _fontBold = default!;
    private Font _fontMono = default!;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    // HUD toggles
    public bool ShowRec { get; set; } = true;
    public bool ShowTimestamp { get; set; } = true;
    public bool ShowDate { get; set; } = true;
    public bool ShowRoundTimer { get; set; } = true;
    public bool ShowFrame { get; set; } = true;
    public bool ShowCameraLabel { get; set; } = true;

    public BodycamHudOverlay()
    {
        IoCManager.InjectDependencies(this);
        _font = _resourceCache.NotoStack();
        _fontBold = _resourceCache.NotoStack(variation: "Bold");
        ZIndex = 150;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.ScreenHandle;
        var uiScale = _uiMan.RootControl.UIScale;
        var vp = args.ViewportBounds;
        var time = (float)_timing.CurTime.TotalSeconds;

        float rightMargin = 14f * uiScale;
        float topMargin = 14f * uiScale;
        float lineH = 24f * uiScale;

        // ── Top-right corner stack ───────────────────────────────────────────
        var cursorY = topMargin;
        var rightEdge = vp.Right - rightMargin;

        // "CAM 01" label
        if (ShowCameraLabel)
        {
            var label = "CAM 01";
            var scale = uiScale * 1.6f;
            var sz = handle.GetDimensions(_fontBold, label, scale);
            var pos = new Vector2(rightEdge - sz.X, cursorY);
            handle.DrawString(_fontBold, pos, label, scale, new Color(1f, 1f, 1f, 0.6f));
            cursorY += sz.Y + 4f * uiScale;
        }

        // REC indicator (blinking red dot + text)
        if (ShowRec)
        {
            var blink = ((int)Math.Floor(time * 2.0)) % 2 == 0;
            var color = blink ? Color.Red : new Color(0.5f, 0f, 0f, 1f);
            var scale = uiScale * 2f;

            var recText = "REC";
            var recSize = handle.GetDimensions(_fontBold, recText, scale);
            var recPos = new Vector2(rightEdge - recSize.X, cursorY);

            // Red dot
            var dotRadius = 10f * uiScale;
            var dotCenter = new Vector2(recPos.X - 16f * uiScale, recPos.Y + recSize.Y * 0.5f);
            handle.DrawCircle(dotCenter, dotRadius, color);

            handle.DrawString(_fontBold, recPos, recText, scale, Color.White);
            cursorY += recSize.Y + 6f * uiScale;
        }

        // Date (DD.MM.YYYY)
        if (ShowDate)
        {
            var now = DateTime.Now;
            var dateText = $"{now.Day:00}.{now.Month:00}.{now.Year:0000}";
            var scale = uiScale * 1.6f;
            var sz = handle.GetDimensions(_font, dateText, scale);
            var pos = new Vector2(rightEdge - sz.X, cursorY);
            handle.DrawString(_font, pos, dateText, scale, new Color(1f, 1f, 1f, 0.8f));
            cursorY += sz.Y + 2f * uiScale;
        }

        // Timestamp (HH:MM:SS)
        if (ShowTimestamp)
        {
            var now = DateTime.Now;
            var timeText = $"{now.Hour:00}:{now.Minute:00}:{now.Second:00}";
            var scale = uiScale * 1.6f;
            var sz = handle.GetDimensions(_font, timeText, scale);
            var pos = new Vector2(rightEdge - sz.X, cursorY);
            handle.DrawString(_font, pos, timeText, scale, new Color(1f, 1f, 1f, 0.8f));
            cursorY += sz.Y + 4f * uiScale;
        }

        // Round timer
        if (ShowRoundTimer)
        {
            var ts = _entMan.System<ClientGameTicker>().RoundDuration();
            ts = TimeSpan.FromSeconds(Math.Floor(ts.TotalSeconds));
            var roundText = $"R {(int)ts.TotalHours:00}:{ts.Minutes:00}:{ts.Seconds:00}";
            var scale = uiScale * 1.4f;
            var sz = handle.GetDimensions(_font, roundText, scale);
            var pos = new Vector2(rightEdge - sz.X, cursorY);
            handle.DrawString(_font, pos, roundText, scale, new Color(1f, 1f, 1f, 0.5f));
        }

        // ── Bottom-left: coordinates / FPS hint ──────────────────────────────
        if (ShowFrame)
        {
            var margin = 8f * uiScale;
            var len = 30f * uiScale;
            var thick = 2f * uiScale;

            // Corner brackets
            DrawCorner(handle, new Vector2(vp.Left + margin, vp.Top + margin), len, thick, true, true);
            DrawCorner(handle, new Vector2(vp.Right - margin, vp.Top + margin), len, thick, false, true);
            DrawCorner(handle, new Vector2(vp.Left + margin, vp.Bottom - margin), len, thick, true, false);
            DrawCorner(handle, new Vector2(vp.Right - margin, vp.Bottom - margin), len, thick, false, false);
        }
    }

    private void DrawCorner(DrawingHandleScreen handle, Vector2 corner, float len, float thick, bool left, bool top)
    {
        var color = new Color(1f, 1f, 1f, 0.7f);
        var hEnd = corner + new Vector2(left ? len : -len, 0);
        var vEnd = corner + new Vector2(0, top ? len : -len);
        handle.DrawLine(corner, hEnd, color);
        handle.DrawLine(corner, vEnd, color);
    }
}
