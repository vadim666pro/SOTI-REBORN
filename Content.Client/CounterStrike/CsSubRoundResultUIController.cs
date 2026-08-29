using System.Numerics;
using Content.Client.Gameplay;
using Content.Shared.CounterStrike.Events;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Utility;

namespace Content.Client.CounterStrike;

public sealed class CsSubRoundResultUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>
{
    private CsSubRoundResultControl? _control;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<CsSubRoundResultEvent>(OnResultEvent);
        SubscribeNetworkEvent<CsSubRoundResultClearEvent>(OnResultClear);
    }

    public void OnStateEntered(GameplayState state)
    {
        _control = new CsSubRoundResultControl();
        UIManager.RootControl.AddChild(_control);
    }

    public void OnStateExited(GameplayState state)
    {
        if (_control == null)
            return;
        UIManager.RootControl.RemoveChild(_control);
        _control.Dispose();
        _control = null;
    }

    private void OnResultEvent(CsSubRoundResultEvent ev, EntitySessionEventArgs args)
    {
        _control?.ShowResult(ev.WinnerTeam, ev.SurvivorsCt, ev.SurvivorsT, ev.FunnyPlayerName, ev.FunnyPhrase, ev.ImagePath);
    }

    private void OnResultClear(CsSubRoundResultClearEvent ev, EntitySessionEventArgs args)
    {
        _control?.Hide();
    }

    private sealed class CsSubRoundResultControl : Control
    {
        private Font _fontTitle = default!;
        private Font _fontMedium = default!;
        private Font _fontSmall = default!;

        private bool _visible;
        private string _winnerTeam = string.Empty;
        private int _survivorsCt;
        private int _survivorsT;
        private string _funnyPlayerName = string.Empty;
        private string _funnyPhrase = string.Empty;
        private string _imagePath = string.Empty;
        private IResourceCache? _cache;

        private static readonly Color BgColor = new Color(0, 0, 0, 230);
        private static readonly Color CtColor = new Color(80, 160, 255);
        private static readonly Color TColor = new Color(255, 180, 60);
        private static readonly Color TextColor = Color.White;
        private static readonly Color SubTextColor = new Color(200, 200, 200);
        private static readonly Color FunnyColor = new Color(255, 220, 100);

        public CsSubRoundResultControl()
        {
            MouseFilter = MouseFilterMode.Ignore;
            _cache = IoCManager.Resolve<IResourceCache>();
            _fontTitle = new VectorFont(_cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Bold.ttf"), 32);
            _fontMedium = new VectorFont(_cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 18);
            _fontSmall = new VectorFont(_cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 14);
        }

        public void ShowResult(string winnerTeam, int survivorsCt, int survivorsT, string funnyPlayerName, string funnyPhrase, string imagePath)
        {
            _winnerTeam = winnerTeam;
            _survivorsCt = survivorsCt;
            _survivorsT = survivorsT;
            _funnyPlayerName = funnyPlayerName;
            _funnyPhrase = funnyPhrase;
            _imagePath = imagePath;
            _visible = true;
            Visible = true;
        }

        public new void Hide()
        {
            _visible = false;
            Visible = false;
        }

        protected override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);

            if (!_visible)
                return;

            var panelWidth = 550f;
            var panelHeight = 460f;
            var x = (PixelSize.X - panelWidth) / 2f;
            var y = (PixelSize.Y - panelHeight) / 2f;
            var padding = 20f;

            handle.DrawRect(new UIBox2(x, y, x + panelWidth, y + panelHeight), BgColor);

            var cx = x + padding;
            var cy = y + padding;

            // Winner team
            var isCt = _winnerTeam == "КТ" || _winnerTeam == "CT";
            var winnerColor = isCt ? CtColor : TColor;
            var winnerText = $"Победа команды {_winnerTeam}!";
            var winnerDims = handle.GetDimensions(_fontTitle, winnerText, UIScale);
            handle.DrawString(_fontTitle, new Vector2(cx + (panelWidth - padding * 2 - winnerDims.X) / 2f, cy), winnerText, UIScale, winnerColor);
            cy += winnerDims.Y + 12f;

            // Survivors
            var survivorsText = $"Выжило: КТ {_survivorsCt} | Т {_survivorsT}";
            var survDims = handle.GetDimensions(_fontMedium, survivorsText, UIScale);
            handle.DrawString(_fontMedium, new Vector2(cx + (panelWidth - padding * 2 - survDims.X) / 2f, cy), survivorsText, UIScale, SubTextColor);
            cy += survDims.Y + 16f;

            // Funny phrase
            var phraseDims = handle.GetDimensions(_fontSmall, _funnyPhrase, UIScale);
            handle.DrawString(_fontSmall, new Vector2(cx + (panelWidth - padding * 2 - phraseDims.X) / 2f, cy), _funnyPhrase, UIScale, FunnyColor);
            cy += phraseDims.Y + 16f;

            // Image placeholder
            try
            {
                if (_cache != null && !string.IsNullOrEmpty(_imagePath))
                {
                    var texture = _cache.GetResource<TextureResource>(new ResPath(_imagePath));
                    var imgSize = 256f;
                    var imgX = cx + (panelWidth - padding * 2 - imgSize) / 2f;
                    handle.DrawTextureRect(texture, new UIBox2(imgX, cy, imgX + imgSize, cy + imgSize));
                }
            }
            catch
            {
                // Image not found — skip silently
            }
        }
    }
}
