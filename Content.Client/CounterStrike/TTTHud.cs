using System.Numerics;
using Content.Client.Resources;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Client.CounterStrike;

public sealed class TTTHud : Control
{
    private static readonly Color BackgroundColor = new(0, 0, 0, 175);
    private static readonly Color LabelColor = new(220, 220, 220);
    private static readonly Color TimerBlueColor = new(70, 145, 255);
    private static readonly Color TimerRedColor = new(255, 65, 65);
    private static readonly Color ArrivedColor = new(255, 95, 75);

    private readonly Texture _icon;
    private readonly Font _labelFont;
    private readonly Font _valueFont;

    private float _timeUntilPolice;
    private bool _policeArrived;
    private bool _active;
    private bool _timerBlue;

    public TTTHud()
    {
        MouseFilter = MouseFilterMode.Ignore;

        var cache = IoCManager.Resolve<IResourceCache>();
        _icon = cache.GetTexture(new ResPath("/Textures/Objects/counterstrike/Other/interface.rsi/ttt.png"));
        var font = cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf");
        _labelFont = new VectorFont(font, 15);
        _valueFont = new VectorFont(font, 20);
    }

    public void SetData(float timeUntilPolice, bool policeArrived)
    {
        _timeUntilPolice = MathF.Max(0f, timeUntilPolice);
        _policeArrived = policeArrived;
        _timerBlue = !_timerBlue;
        _active = true;
        Visible = true;
    }

    public void Clear()
    {
        _active = false;
        Visible = false;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        if (!_active)
            return;

        var label = _policeArrived ? "Статус раунда" : "Таймер до прибытия полиции:";
        var value = _policeArrived ? "Полиция прибыла" : FormatTime(_timeUntilPolice);
        var valueColor = _policeArrived ? ArrivedColor : _timerBlue ? TimerBlueColor : TimerRedColor;

        var labelSize = handle.GetDimensions(_labelFont, label, UIScale);
        var valueSize = handle.GetDimensions(_valueFont, value, UIScale);

        const float iconSize = 32f;
        const float gap = 12f;
        const float padding = 12f;
        const float rowGap = 2f;

        var textWidth = MathF.Max(labelSize.X, valueSize.X);
        var textHeight = labelSize.Y + rowGap + valueSize.Y;
        var contentHeight = MathF.Max(iconSize, textHeight);
        var width = padding * 2 + iconSize + gap + textWidth;
        var height = padding * 2 + contentHeight;
        var x = (PixelSize.X - width) / 2f;
        const float y = 8f;

        handle.DrawRect(new UIBox2(x, y, x + width, y + height), BackgroundColor);

        var iconY = y + (height - iconSize) / 2f;
        handle.DrawTexture(_icon, new Vector2(x + padding, iconY));

        var textX = x + padding + iconSize + gap;
        var textY = y + (height - textHeight) / 2f;
        handle.DrawString(_labelFont, new Vector2(textX, textY), label, UIScale, LabelColor);
        handle.DrawString(_valueFont, new Vector2(textX, textY + labelSize.Y + rowGap), value, UIScale, valueColor);
    }

    private static string FormatTime(float secondsRemaining)
    {
        var totalSeconds = (int)MathF.Ceiling(secondsRemaining);
        return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
    }
}
