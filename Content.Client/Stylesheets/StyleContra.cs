using System.Linq;
using Content.Client.Resources;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Robust.Client.UserInterface.StylesheetHelpers;

namespace Content.Client.Stylesheets
{
    public sealed class StyleContra : StyleBase
    {
        // Точные цвета из CS 1.6
        public static readonly Color MainBg = Color.FromHex("#4C5844");
        public static readonly Color DarkBorder = Color.FromHex("#2D3528");
        public static readonly Color TextColor = Color.FromHex("#D4D0C8");
        public static readonly Color ProgressFill = Color.FromHex("#D8C000");
        public static readonly Color CancelText = Color.FromHex("#000000");

        public static readonly Color ButtonColorDefault = MainBg;
        public static readonly Color ButtonColorHovered = Color.FromHex("#5A6B4F");
        public static readonly Color ButtonColorPressed = Color.FromHex("#3A4535");

        public override Stylesheet Stylesheet { get; }

        public StyleContra(IResourceCache resCache) : base(resCache)
        {
            var csFont12 = resCache.GetFont(
                new[] {
                    "/Fonts/NotoSans/NotoSans-Regular.ttf",
                    "/Fonts/NotoSans/NotoSansSymbols-Regular.ttf",
                    "/Fonts/NotoSans/NotoSansSymbols2-Regular.ttf"
                },
                12
            );
            var csFontBold16 = resCache.GetFont(
                new[] {
                    "/Fonts/NotoSans/NotoSans-Bold.ttf",
                    "/Fonts/NotoSans/NotoSansSymbols-Regular.ttf",
                    "/Fonts/NotoSans/NotoSansSymbols2-Regular.ttf"
                },
                16
            );
            var csFontMono12 = resCache.GetFont(
                new[] {
                    "/Fonts/RobotoMono/RobotoMono-Regular.ttf"
                },
                12
            );

            // Стиль для кнопок (прямоугольные, без скруглений)
            var buttonStyle = new StyleBoxFlat
            {
                BackgroundColor = MainBg,
                BorderColor = DarkBorder,
                BorderThickness = new Thickness(2),
                Padding = new Thickness(10, 4),
                ContentMarginLeftOverride = 8,
                ContentMarginRightOverride = 8,
            };
            var buttonStyleHover = new StyleBoxFlat
            {
                BackgroundColor = ButtonColorHovered,
                BorderColor = DarkBorder,
                BorderThickness = new Thickness(2),
                Padding = new Thickness(10, 4),
            };
            var buttonStylePressed = new StyleBoxFlat
            {
                BackgroundColor = ButtonColorPressed,
                BorderColor = DarkBorder,
                BorderThickness = new Thickness(2),
                Padding = new Thickness(10, 4),
            };

            var bgStyle = new StyleBoxFlat
            {
                BackgroundColor = MainBg
            };

            var panelStyle = new StyleBoxFlat
            {
                BackgroundColor = MainBg,
                BorderColor = DarkBorder,
                BorderThickness = new Thickness(1),
            };

            var progressBg = new StyleBoxFlat { BackgroundColor = DarkBorder };
            var progressFg = new StyleBoxFlat { BackgroundColor = ProgressFill };

            // Убираем CornerRadius, используем обычные прямоугольные рамки
            // Для AngleRect используем тот же panelStyle
            // Для OpenRight/OpenLeft – просто прямоугольные кнопки (можно использовать buttonStyle)

            Stylesheet = new Stylesheet(BaseRules.Concat(new StyleRule[]
            {
                Element<PanelContainer>()
                    .Prop(PanelContainer.StylePropertyPanel, bgStyle),

                Element<Label>().Class("CsHeading")
                    .Prop(Label.StylePropertyFont, csFontBold16)
                    .Prop(Label.StylePropertyFontColor, TextColor),

                Element<Label>().Class("CsText")
                    .Prop(Label.StylePropertyFont, csFont12)
                    .Prop(Label.StylePropertyFontColor, TextColor),

                Element<Label>().Class("CsMono")
                    .Prop(Label.StylePropertyFont, csFontMono12)
                    .Prop(Label.StylePropertyFontColor, TextColor),

                Element<Button>().Class("CsButton")
                    .Prop(Button.StylePropertyStyleBox, buttonStyle)
                    .Prop(Label.StylePropertyFont, csFont12)
                    .Prop(Label.StylePropertyFontColor, TextColor),
                Element<Button>().Class("CsButton")
                    .Pseudo(Button.StylePseudoClassHover)
                    .Prop(Button.StylePropertyStyleBox, buttonStyleHover),
                Element<Button>().Class("CsButton")
                    .Pseudo(Button.StylePseudoClassPressed)
                    .Prop(Button.StylePropertyStyleBox, buttonStylePressed),

                Element<Button>().Class("CancelButton")
                    .Prop(Label.StylePropertyFontColor, CancelText),

                Element<ProgressBar>()
                    .Prop(ProgressBar.StylePropertyBackground, progressBg)
                    .Prop(ProgressBar.StylePropertyForeground, progressFg),

                Element<PanelContainer>().Class(ClassHighDivider)
                    .Prop(PanelContainer.StylePropertyPanel, new StyleBoxFlat
                    {
                        BackgroundColor = TextColor,
                        ContentMarginBottomOverride = 2,
                        ContentMarginLeftOverride = 2
                    }),

                // Для AngleRect используем panelStyle (с рамкой и фоном)
                Element<PanelContainer>().Class("AngleRect")
                    .Prop(PanelContainer.StylePropertyPanel, panelStyle),

                // Для LabelHeading и LabelSubText
                Element<Label>().Class("LabelHeading")
                    .Prop(Label.StylePropertyFont, csFontBold16)
                    .Prop(Label.StylePropertyFontColor, TextColor),

                Element<Label>().Class("LabelSubText")
                    .Prop(Label.StylePropertyFont, csFont12)
                    .Prop(Label.StylePropertyFontColor, Color.FromHex("#AAAAAA")),

                // Для кнопок OpenRight и OpenLeft – используем обычный buttonStyle (прямоугольные)
                Element<Button>().Class("OpenRight")
                    .Prop(Button.StylePropertyStyleBox, buttonStyle)
                    .Prop(Label.StylePropertyFont, csFont12)
                    .Prop(Label.StylePropertyFontColor, TextColor),
                Element<Button>().Class("OpenRight")
                    .Pseudo(Button.StylePseudoClassHover)
                    .Prop(Button.StylePropertyStyleBox, buttonStyleHover),
                Element<Button>().Class("OpenRight")
                    .Pseudo(Button.StylePseudoClassPressed)
                    .Prop(Button.StylePropertyStyleBox, buttonStylePressed),

                Element<Button>().Class("OpenLeft")
                    .Prop(Button.StylePropertyStyleBox, buttonStyle)
                    .Prop(Label.StylePropertyFont, csFont12)
                    .Prop(Label.StylePropertyFontColor, TextColor),
                Element<Button>().Class("OpenLeft")
                    .Pseudo(Button.StylePseudoClassHover)
                    .Prop(Button.StylePropertyStyleBox, buttonStyleHover),
                Element<Button>().Class("OpenLeft")
                    .Pseudo(Button.StylePseudoClassPressed)
                    .Prop(Button.StylePropertyStyleBox, buttonStylePressed),
            }).ToList());
        }
    }
}
