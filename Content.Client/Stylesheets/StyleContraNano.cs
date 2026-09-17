using System.Linq;
using System.Numerics;
using Content.Client.ContextMenu.UI;
using Content.Client.Examine;
using Content.Client.PDA;
using Content.Client.Resources;
using Content.Client.Silicons.Laws.SiliconLawEditUi;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Controls.FancyTree;
using Content.Client.Verbs.UI;
using Content.Shared.Verbs;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Graphics;
using static Robust.Client.UserInterface.StylesheetHelpers;

namespace Content.Client.Stylesheets
{
    public sealed class StyleContraNano : StyleBase
    {
        // ====== Палитра Contra ======
        public static readonly Color MainBg       = Color.FromHex("#4C5844");
        public static readonly Color DarkBorder   = Color.FromHex("#2D3528");
        public static readonly Color LightBorder  = Color.FromHex("#6E7B63");
        public static readonly Color TextColor    = Color.FromHex("#D4D0C8");
        public static readonly Color SubTextColor = Color.FromHex("#AAAAAA");
        public static readonly Color ProgressFill = Color.FromHex("#D8C000");
        public static readonly Color Accent       = ProgressFill;

        public static readonly Color BtnDefault   = MainBg;
        public static readonly Color BtnHovered   = Color.FromHex("#5A6B4F");
        public static readonly Color BtnPressed   = Color.FromHex("#3A4535");
        public static readonly Color BtnDisabled  = Color.FromHex("#30382C");

        public static readonly Color DangerRed    = Color.FromHex("#BB3232");
        public static readonly Color DangerRedHov = Color.FromHex("#DF6B6B");
        public static readonly Color GoodGreen    = Color.FromHex("#31843E");
        public static readonly Color GoodGreenHov = Color.FromHex("#3E9C4F");
        public static readonly Color WarnOrange   = Color.FromHex("#A5762F");

        public override Stylesheet Stylesheet { get; }

        public StyleContraNano(IResourceCache resCache) : base(resCache)
        {
            // ====== Шрифты ======
            var f8    = resCache.NotoStack(size: 8);
            var f10   = resCache.NotoStack(size: 10);
            var f10i  = resCache.NotoStack(variation: "Italic", size: 10);
            var f12   = resCache.NotoStack(size: 12);
            var f12i  = resCache.NotoStack(variation: "Italic", size: 12);
            var f12b  = resCache.NotoStack(variation: "Bold", size: 12);
            var f12bi = resCache.NotoStack(variation: "BoldItalic", size: 12);
            var f14bi = resCache.NotoStack(variation: "BoldItalic", size: 14);
            var f14bd = resCache.NotoStack(variation: "Bold", display: true, size: 14);
            var f15   = resCache.NotoStack(variation: "Regular", size: 15);
            var f16   = resCache.NotoStack(size: 16);
            var f16b  = resCache.NotoStack(variation: "Bold", size: 16);
            var f16bd = resCache.NotoStack(variation: "Bold", display: true, size: 16);
            var f18b  = resCache.NotoStack(variation: "Bold", size: 18);
            var f20b  = resCache.NotoStack(variation: "Bold", size: 20);
            var mono12 = resCache.GetFont("/EngineFonts/NotoSans/NotoSansMono-Regular.ttf", size: 12);

            // ====== Общие StyleBoxFlat ======
            var panelBg = new StyleBoxFlat
            {
                BackgroundColor = MainBg,
                BorderColor = DarkBorder,
                BorderThickness = new Thickness(2),
            };

            var windowBg = new StyleBoxFlat
            {
                BackgroundColor = MainBg,
                BorderColor = DarkBorder,
                BorderThickness = new Thickness(2),
            };

            var windowHeader = new StyleBoxFlat
            {
                BackgroundColor = DarkBorder,
                BorderColor = DarkBorder,
                BorderThickness = new Thickness(1),
                ContentMarginBottomOverride = 0,
            };

            var windowHeaderAlert = new StyleBoxFlat
            {
                BackgroundColor = DangerRed,
                BorderColor = DarkBorder,
                BorderThickness = new Thickness(1),
                ContentMarginBottomOverride = 0,
            };

            var borderedWindowBackground = new StyleBoxFlat
            {
                BackgroundColor = MainBg,
                BorderColor = DarkBorder,
                BorderThickness = new Thickness(2),
            };

            var borderedTransparentWindowBackground = new StyleBoxFlat
            {
                BackgroundColor = Color.FromHex("#4C5844CC"),
                BorderColor = DarkBorder,
                BorderThickness = new Thickness(2),
            };

            var invSlotBg = new StyleBoxFlat
            {
                BackgroundColor = Color.FromHex("#3A4535"),
                BorderColor = DarkBorder,
                BorderThickness = new Thickness(1),
            };

            var handSlotHighlight = new StyleBoxFlat
            {
                BackgroundColor = Color.FromHex("#5A6B4F"),
                BorderColor = ProgressFill,
                BorderThickness = new Thickness(1),
            };

            var hotbarBackground = new StyleBoxFlat
            {
                BackgroundColor = MainBg,
                BorderColor = DarkBorder,
                BorderThickness = new Thickness(2),
            };

            // ====== Кнопки ======
            var buttonBase = new StyleBoxFlat
            {
                BackgroundColor = BtnDefault,
                BorderColor = DarkBorder,
                BorderThickness = new Thickness(2),
                ContentMarginLeftOverride = 8,
                ContentMarginRightOverride = 8,
                Padding = new Thickness(10, 4),
            };
            var buttonHover    = new StyleBoxFlat(buttonBase) { BackgroundColor = BtnHovered };
            var buttonPressed  = new StyleBoxFlat(buttonBase) { BackgroundColor = BtnPressed };
            var buttonDisabled = new StyleBoxFlat(buttonBase) { BackgroundColor = BtnDisabled };

            var buttonRect = new StyleBoxFlat
            {
                BackgroundColor = MainBg,
                BorderColor = DarkBorder,
                BorderThickness = new Thickness(2),
                Padding = new Thickness(2),
            };
            var buttonRectHover    = new StyleBoxFlat(buttonRect) { BackgroundColor = BtnHovered };
            var buttonRectPressed  = new StyleBoxFlat(buttonRect) { BackgroundColor = BtnPressed };
            var buttonRectDisabled = new StyleBoxFlat(buttonRect) { BackgroundColor = BtnDisabled };

            var buttonStorage = new StyleBoxFlat(buttonBase) { Padding = new Thickness(0) };

            var buttonContext = new StyleBoxFlat
            {
                BackgroundColor = Color.FromHex("#3A4535CC"),
                BorderColor = DarkBorder,
                BorderThickness = new Thickness(1),
            };

            var lineEdit = new StyleBoxFlat
            {
                BackgroundColor = Color.FromHex("#3A4535"),
                BorderColor = DarkBorder,
                BorderThickness = new Thickness(1),
                ContentMarginLeftOverride = 5,
                ContentMarginRightOverride = 5,
            };

            var actionSearchBox = new StyleBoxFlat(lineEdit);

            var chatBg    = new StyleBoxFlat { BackgroundColor = Color.FromHex("#3A4535DD") };
            var chatSubBg = new StyleBoxFlat(chatBg) { ContentMarginLeftOverride = 2, ContentMarginTopOverride = 2 };

            var tabContainerPanel = new StyleBoxFlat
            {
                BackgroundColor = MainBg,
                BorderColor = DarkBorder,
                BorderThickness = new Thickness(2),
            };
            var tabActive = new StyleBoxFlat
            {
                BackgroundColor = Color.FromHex("#5A6B4F"),
                BorderColor = DarkBorder,
                BorderThickness = new Thickness(1),
                ContentMarginLeftOverride = 5,
            };
            var tabInactive = new StyleBoxFlat
            {
                BackgroundColor = DarkBorder,
                BorderColor = DarkBorder,
                BorderThickness = new Thickness(1),
                ContentMarginLeftOverride = 5,
            };

            var progressBarBackground = new StyleBoxFlat
            {
                BackgroundColor = DarkBorder,
                ContentMarginTopOverride = 14.5f,
                ContentMarginBottomOverride = 14.5f,
            };
            var progressBarForeground = new StyleBoxFlat
            {
                BackgroundColor = ProgressFill,
                ContentMarginTopOverride = 14.5f,
                ContentMarginBottomOverride = 14.5f,
            };

            var tooltipBox = new StyleBoxFlat
            {
                BackgroundColor = Color.FromHex("#2D3528"),
                BorderColor = LightBorder,
                BorderThickness = new Thickness(1),
                ContentMarginLeftOverride = 7,
                ContentMarginRightOverride = 7,
            };

            var paperBackground = new StyleBoxFlat
            {
                BackgroundColor = Color.FromHex("#D4D0C8"),
                BorderColor = DarkBorder,
                BorderThickness = new Thickness(2),
            };

            var itemListBackgroundSelected      = new StyleBoxFlat { BackgroundColor = Color.FromHex("#5A6B4F") };
            var itemListItemBackground          = new StyleBoxFlat { BackgroundColor = Color.FromHex("#3A4535") };
            var itemListItemBackgroundDisabled  = new StyleBoxFlat { BackgroundColor = Color.FromHex("#2D3528") };

            // ====== Stylesheet ======
            Stylesheet = new Stylesheet(BaseRules.Concat(new StyleRule[]
            {
                // ============ Окна ============
                new StyleRule(
                    new SelectorElement(typeof(Label), new[] { DefaultWindow.StyleClassWindowTitle }, null, null),
                    new[]
                    {
                        new StyleProperty(Label.StylePropertyFontColor, Accent),
                        new StyleProperty(Label.StylePropertyFont, f14bd),
                    }),
                new StyleRule(
                    new SelectorElement(typeof(Label), new[] { "windowTitleAlert" }, null, null),
                    new[]
                    {
                        new StyleProperty(Label.StylePropertyFontColor, Color.White),
                        new StyleProperty(Label.StylePropertyFont, f14bd),
                    }),
                new StyleRule(
                    new SelectorElement(null, new[] { DefaultWindow.StyleClassWindowPanel }, null, null),
                    new[] { new StyleProperty(PanelContainer.StylePropertyPanel, windowBg) }),
                new StyleRule(
                    new SelectorElement(null, new[] { "BorderedWindowPanel" }, null, null),
                    new[] { new StyleProperty(PanelContainer.StylePropertyPanel, borderedWindowBackground) }),
                new StyleRule(
                    new SelectorElement(null, new[] { "TransparentBorderedWindowPanel" }, null, null),
                    new[] { new StyleProperty(PanelContainer.StylePropertyPanel, borderedTransparentWindowBackground) }),
                new StyleRule(
                    new SelectorElement(null, new[] { "InventorySlotBackground" }, null, null),
                    new[] { new StyleProperty(PanelContainer.StylePropertyPanel, invSlotBg) }),
                new StyleRule(
                    new SelectorElement(null, new[] { "HandSlotHighlight" }, null, null),
                    new[] { new StyleProperty(PanelContainer.StylePropertyPanel, handSlotHighlight) }),
                new StyleRule(
                    new SelectorElement(typeof(PanelContainer), new[] { "HotbarPanel" }, null, null),
                    new[] { new StyleProperty(PanelContainer.StylePropertyPanel, hotbarBackground) }),
                new StyleRule(
                    new SelectorElement(typeof(PanelContainer), new[] { DefaultWindow.StyleClassWindowHeader }, null, null),
                    new[] { new StyleProperty(PanelContainer.StylePropertyPanel, windowHeader) }),
                new StyleRule(
                    new SelectorElement(typeof(PanelContainer), new[] { "windowHeaderAlert" }, null, null),
                    new[] { new StyleProperty(PanelContainer.StylePropertyPanel, windowHeaderAlert) }),

                // ============ Общие панели ============
                new StyleRule(new SelectorElement(typeof(PanelContainer), null, null, null),
                    new[] { new StyleProperty(PanelContainer.StylePropertyPanel, panelBg) }),

                // ============ Кнопки ============
                Element<ContainerButton>().Class(ContainerButton.StyleClassButton)
                    .Prop(ContainerButton.StylePropertyStyleBox, buttonBase),
                Element<ContainerButton>().Class(ContainerButton.StyleClassButton)
                    .Pseudo(ContainerButton.StylePseudoClassNormal)
                    .Prop(Control.StylePropertyModulateSelf, Color.White),
                Element<ContainerButton>().Class(ContainerButton.StyleClassButton)
                    .Pseudo(ContainerButton.StylePseudoClassHover)
                    .Prop(ContainerButton.StylePropertyStyleBox, buttonHover),
                Element<ContainerButton>().Class(ContainerButton.StyleClassButton)
                    .Pseudo(ContainerButton.StylePseudoClassPressed)
                    .Prop(ContainerButton.StylePropertyStyleBox, buttonPressed),
                Element<ContainerButton>().Class(ContainerButton.StyleClassButton)
                    .Pseudo(ContainerButton.StylePseudoClassDisabled)
                    .Prop(ContainerButton.StylePropertyStyleBox, buttonDisabled),

                // Форма кнопок OpenRight/OpenLeft/OpenBoth/Square — плоские
                Element<ContainerButton>().Class(ContainerButton.StyleClassButton)
                    .Class(ButtonOpenRight)
                    .Prop(ContainerButton.StylePropertyStyleBox, buttonBase),
                Element<ContainerButton>().Class(ContainerButton.StyleClassButton)
                    .Class(ButtonOpenLeft)
                    .Prop(ContainerButton.StylePropertyStyleBox, buttonBase),
                Element<ContainerButton>().Class(ContainerButton.StyleClassButton)
                    .Class(ButtonOpenBoth)
                    .Prop(ContainerButton.StylePropertyStyleBox, buttonBase),
                Element<ContainerButton>().Class(ContainerButton.StyleClassButton)
                    .Class(ButtonSquare)
                    .Prop(ContainerButton.StylePropertyStyleBox, buttonBase),

                // Метка внутри кнопок — по центру
                new StyleRule(new SelectorElement(typeof(Label), new[] { Button.StyleClassButton }, null, null),
                    new[] { new StyleProperty(Label.StylePropertyAlignMode, Label.AlignMode.Center) }),

                // ============ Красные/зелёные кнопки ============
                Element<Button>().Class("ButtonColorRed")
                    .Prop(Control.StylePropertyModulateSelf, DangerRed),
                Element<Button>().Class("ButtonColorRed").Pseudo(ContainerButton.StylePseudoClassNormal)
                    .Prop(Control.StylePropertyModulateSelf, DangerRed),
                Element<Button>().Class("ButtonColorRed").Pseudo(ContainerButton.StylePseudoClassHover)
                    .Prop(Control.StylePropertyModulateSelf, DangerRedHov),

                Element<Button>().Class("ButtonColorGreen")
                    .Prop(Control.StylePropertyModulateSelf, GoodGreen),
                Element<Button>().Class("ButtonColorGreen").Pseudo(ContainerButton.StylePseudoClassNormal)
                    .Prop(Control.StylePropertyModulateSelf, GoodGreen),
                Element<Button>().Class("ButtonColorGreen").Pseudo(ContainerButton.StylePseudoClassHover)
                    .Prop(Control.StylePropertyModulateSelf, GoodGreenHov),

                Element<Button>().Class("ButtonAccept")
                    .Prop(Control.StylePropertyModulateSelf, GoodGreen),
                Element<Button>().Class("ButtonAccept").Pseudo(ContainerButton.StylePseudoClassNormal)
                    .Prop(Control.StylePropertyModulateSelf, GoodGreen),
                Element<Button>().Class("ButtonAccept").Pseudo(ContainerButton.StylePseudoClassHover)
                    .Prop(Control.StylePropertyModulateSelf, GoodGreenHov),
                Element<Button>().Class("ButtonAccept").Pseudo(ContainerButton.StylePseudoClassDisabled)
                    .Prop(Control.StylePropertyModulateSelf, BtnDisabled),

                // ============ ConfirmButton ============
                Element<ConfirmButton>()
                    .Pseudo(ConfirmButton.ConfirmPrefix + ContainerButton.StylePseudoClassNormal)
                    .Prop(Control.StylePropertyModulateSelf, DangerRed),
                Element<ConfirmButton>()
                    .Pseudo(ConfirmButton.ConfirmPrefix + ContainerButton.StylePseudoClassHover)
                    .Prop(Control.StylePropertyModulateSelf, DangerRedHov),
                Element<ConfirmButton>()
                    .Pseudo(ConfirmButton.ConfirmPrefix + ContainerButton.StylePseudoClassPressed)
                    .Prop(Control.StylePropertyModulateSelf, BtnPressed),
                Element<ConfirmButton>()
                    .Pseudo(ConfirmButton.ConfirmPrefix + ContainerButton.StylePseudoClassDisabled)
                    .Prop(Control.StylePropertyModulateSelf, BtnDisabled),

                // ============ LineEdit ============
                new StyleRule(new SelectorElement(typeof(LineEdit), null, null, null),
                    new[] { new StyleProperty(LineEdit.StylePropertyStyleBox, lineEdit) }),
                new StyleRule(new SelectorElement(typeof(LineEdit), new[] { LineEdit.StyleClassLineEditNotEditable }, null, null),
                    new[] { new StyleProperty("font-color", SubTextColor) }),
                new StyleRule(new SelectorElement(typeof(LineEdit), null, null, new[] { LineEdit.StylePseudoClassPlaceholder }),
                    new[] { new StyleProperty("font-color", Color.Gray) }),
                Element<TextEdit>().Pseudo(TextEdit.StylePseudoClassPlaceholder)
                    .Prop("font-color", Color.Gray),

                // ============ Чат ============
                new StyleRule(new SelectorElement(typeof(PanelContainer), new[] { "ChatPanel" }, null, null),
                    new[] { new StyleProperty(PanelContainer.StylePropertyPanel, chatBg) }),
                new StyleRule(new SelectorElement(typeof(PanelContainer), new[] { "ChatSubPanel" }, null, null),
                    new[] { new StyleProperty(PanelContainer.StylePropertyPanel, chatSubBg) }),
                new StyleRule(new SelectorElement(typeof(LineEdit), new[] { "chatLineEdit" }, null, null),
                    new[] { new StyleProperty(LineEdit.StylePropertyStyleBox, new StyleBoxEmpty()) }),
                new StyleRule(new SelectorElement(typeof(LineEdit), new[] { "actionSearchBox" }, null, null),
                    new[] { new StyleProperty(LineEdit.StylePropertyStyleBox, actionSearchBox) }),

                // Селектор канала и фильтры
                new StyleRule(new SelectorElement(typeof(Button), new[] { "chatSelectorOptionButton" }, null, null),
                    new[] { new StyleProperty(Button.StylePropertyStyleBox, buttonBase) }),
                new StyleRule(new SelectorElement(typeof(Button), new[] { "chatSelectorOptionButton" }, null, new[] {ContainerButton.StylePseudoClassHover}),
                    new[] { new StyleProperty(Button.StylePropertyStyleBox, buttonHover) }),
                new StyleRule(new SelectorElement(typeof(Button), new[] { "chatSelectorOptionButton" }, null, new[] {ContainerButton.StylePseudoClassPressed}),
                    new[] { new StyleProperty(Button.StylePropertyStyleBox, buttonPressed) }),

                new StyleRule(new SelectorElement(typeof(ContainerButton), new[] { "chatFilterOptionButton" }, null, null),
                    new[] { new StyleProperty(ContainerButton.StylePropertyStyleBox, buttonBase) }),
                new StyleRule(new SelectorElement(typeof(ContainerButton), new[] { "chatFilterOptionButton" }, null, new[] {ContainerButton.StylePseudoClassNormal}),
                    new[] { new StyleProperty(Control.StylePropertyModulateSelf, Color.White) }),
                new StyleRule(new SelectorElement(typeof(ContainerButton), new[] { "chatFilterOptionButton" }, null, new[] {ContainerButton.StylePseudoClassHover}),
                    new[] { new StyleProperty(ContainerButton.StylePropertyStyleBox, buttonHover) }),
                new StyleRule(new SelectorElement(typeof(ContainerButton), new[] { "chatFilterOptionButton" }, null, new[] {ContainerButton.StylePseudoClassPressed}),
                    new[] { new StyleProperty(ContainerButton.StylePropertyStyleBox, buttonPressed) }),

                Element<Button>().Class("OutputPanelScrollDownButton")
                    .Prop(Button.StylePropertyStyleBox, buttonBase),

                // ============ TabContainer ============
                new StyleRule(new SelectorElement(typeof(TabContainer), null, null, null),
                    new[]
                    {
                        new StyleProperty(TabContainer.StylePropertyPanelStyleBox, tabContainerPanel),
                        new StyleProperty(TabContainer.StylePropertyTabStyleBox, tabActive),
                        new StyleProperty(TabContainer.StylePropertyTabStyleBoxInactive, tabInactive),
                    }),

                // ============ ProgressBar ============
                new StyleRule(new SelectorElement(typeof(ProgressBar), null, null, null),
                    new[]
                    {
                        new StyleProperty(ProgressBar.StylePropertyBackground, progressBarBackground),
                        new StyleProperty(ProgressBar.StylePropertyForeground, progressBarForeground),
                    }),

                // ============ Tooltip ============
                new StyleRule(new SelectorElement(typeof(Tooltip), null, null, null),
                    new[] { new StyleProperty(PanelContainer.StylePropertyPanel, tooltipBox) }),
                new StyleRule(new SelectorElement(typeof(PanelContainer), new[] { "tooltipBox" }, null, null),
                    new[] { new StyleProperty(PanelContainer.StylePropertyPanel, tooltipBox) }),
                new StyleRule(new SelectorElement(typeof(PanelContainer), new[] { "speechBox", "sayBox" }, null, null),
                    new[] { new StyleProperty(PanelContainer.StylePropertyPanel, tooltipBox) }),
                new StyleRule(new SelectorElement(typeof(PanelContainer), new[] { "speechBox", "whisperBox" }, null, null),
                    new[] { new StyleProperty(PanelContainer.StylePropertyPanel, tooltipBox) }),
                new StyleRule(new SelectorChild(
                    new SelectorElement(typeof(PanelContainer), new[] {"speechBox", "whisperBox"}, null, null),
                    new SelectorElement(typeof(RichTextLabel), new[] {"bubbleContent"}, null, null)),
                    new[] { new StyleProperty("font", f12i) }),
                new StyleRule(new SelectorChild(
                    new SelectorElement(typeof(PanelContainer), new[] {"speechBox", "emoteBox"}, null, null),
                    new SelectorElement(typeof(RichTextLabel), null, null, null)),
                    new[] { new StyleProperty("font", f12i) }),

                // Tooltip-метки
                new StyleRule(new SelectorElement(typeof(RichTextLabel), new[] {"tooltipAlertTitle"}, null, null),
                    new[] { new StyleProperty("font", f18b) }),
                new StyleRule(new SelectorElement(typeof(RichTextLabel), new[] {"tooltipAlertDesc"}, null, null),
                    new[] { new StyleProperty("font", f16) }),
                new StyleRule(new SelectorElement(typeof(RichTextLabel), new[] {"tooltipAlertCooldown"}, null, null),
                    new[] { new StyleProperty("font", f16) }),
                new StyleRule(new SelectorElement(typeof(RichTextLabel), new[] {"tooltipActionTitle"}, null, null),
                    new[] { new StyleProperty("font", f16b) }),
                new StyleRule(new SelectorElement(typeof(RichTextLabel), new[] {"tooltipActionDesc"}, null, null),
                    new[] { new StyleProperty("font", f15) }),
                new StyleRule(new SelectorElement(typeof(RichTextLabel), new[] {"tooltipActionCooldown"}, null, null),
                    new[] { new StyleProperty("font", f15) }),

                // ============ ItemList ============
                new StyleRule(new SelectorElement(typeof(ItemList), null, null, null),
                    new[]
                    {
                        new StyleProperty(ItemList.StylePropertyBackground,
                            new StyleBoxFlat { BackgroundColor = MainBg }),
                        new StyleProperty(ItemList.StylePropertyItemBackground, itemListItemBackground),
                        new StyleProperty(ItemList.StylePropertyDisabledItemBackground, itemListItemBackgroundDisabled),
                        new StyleProperty(ItemList.StylePropertySelectedItemBackground, itemListBackgroundSelected),
                    }),

                // ============ Лейблы ============
                new StyleRule(new SelectorElement(typeof(Label), new[] { "LabelHeading" }, null, null),
                    new[]
                    {
                        new StyleProperty(Label.StylePropertyFont, f16b),
                        new StyleProperty(Label.StylePropertyFontColor, Accent),
                    }),
                new StyleRule(new SelectorElement(typeof(Label), new[] { "LabelHeadingBigger" }, null, null),
                    new[]
                    {
                        new StyleProperty(Label.StylePropertyFont, f20b),
                        new StyleProperty(Label.StylePropertyFontColor, Accent),
                    }),
                new StyleRule(new SelectorElement(typeof(Label), new[] { "LabelSubText" }, null, null),
                    new[]
                    {
                        new StyleProperty(Label.StylePropertyFont, f10),
                        new StyleProperty(Label.StylePropertyFontColor, SubTextColor),
                    }),
                new StyleRule(new SelectorElement(typeof(Label), new[] { "LabelKeyText" }, null, null),
                    new[]
                    {
                        new StyleProperty(Label.StylePropertyFont, f12b),
                        new StyleProperty(Label.StylePropertyFontColor, Accent),
                    }),
                new StyleRule(new SelectorElement(typeof(Label), new[] { "LabelSecondaryColor" }, null, null),
                    new[]
                    {
                        new StyleProperty(Label.StylePropertyFont, f12),
                        new StyleProperty(Label.StylePropertyFontColor, SubTextColor),
                    }),
                Element<Label>().Class("LabelBig").Prop(Label.StylePropertyFont, f16),
                Element<Label>().Class("LabelSmall").Prop(Label.StylePropertyFont, f10),

                // ============ Разделители ============
                new StyleRule(new SelectorElement(typeof(PanelContainer), new[] { ClassHighDivider }, null, null),
                    new[] { new StyleProperty(PanelContainer.StylePropertyPanel,
                        new StyleBoxFlat { BackgroundColor = Accent, ContentMarginBottomOverride = 2, ContentMarginLeftOverride = 2 }) }),
                new StyleRule(new SelectorElement(typeof(PanelContainer), new[] { ClassLowDivider }, null, null),
                    new[] { new StyleProperty(PanelContainer.StylePropertyPanel,
                        new StyleBoxFlat { BackgroundColor = LightBorder, ContentMarginBottomOverride = 1, ContentMarginLeftOverride = 1 }) }),

                // ============ FancyWindow заголовки и панели ============
                new StyleRule(new SelectorElement(typeof(Label), new[] { "FancyWindowTitle" }, null, null),
                    new[]
                    {
                        new StyleProperty(Label.StylePropertyFont, f12b),
                        new StyleProperty(Label.StylePropertyFontColor, Accent),
                    }),
                new StyleRule(new SelectorElement(typeof(PanelContainer), new[] { "WindowHeadingBackground" }, null, null),
                    new[] { new StyleProperty(PanelContainer.StylePropertyPanel,
                        new StyleBoxFlat { BackgroundColor = DarkBorder }) }),
                new StyleRule(new SelectorElement(typeof(PanelContainer), new[] { "PanelBackgroundBaseDark" }, null, null),
                    new[] { new StyleProperty(PanelContainer.StylePropertyPanel,
                        new StyleBoxFlat { BackgroundColor = DarkBorder }) }),
                new StyleRule(new SelectorElement(typeof(PanelContainer), new[] { "PanelBackgroundLight" }, null, null),
                    new[] { new StyleProperty(PanelContainer.StylePropertyPanel,
                        new StyleBoxFlat { BackgroundColor = BtnHovered }) }),

                // Help-кнопка окна
                Element<TextureButton>().Class(FancyWindow.StyleClassWindowHelpButton)
                    .Prop(TextureButton.StylePropertyTexture, resCache.GetTexture("/Textures/Interface/Nano/help.png"))
                    .Prop(Control.StylePropertyModulateSelf, TextColor),
                Element<TextureButton>().Class(FancyWindow.StyleClassWindowHelpButton).Pseudo(ContainerButton.StylePseudoClassHover)
                    .Prop(Control.StylePropertyModulateSelf, DangerRedHov),
                Element<TextureButton>().Class(FancyWindow.StyleClassWindowHelpButton).Pseudo(ContainerButton.StylePseudoClassPressed)
                    .Prop(Control.StylePropertyModulateSelf, DangerRed),

                // Refresh
                Element<TextureButton>().Class("Refresh")
                    .Prop(TextureButton.StylePropertyTexture, resCache.GetTexture("/Textures/Interface/Nano/circular_arrow.svg.96dpi.png"))
                    .Prop(Control.StylePropertyModulateSelf, TextColor),

                // NTLogo
                Element<TextureRect>().Class("NTLogoDark")
                    .Prop(TextureRect.StylePropertyTexture, resCache.GetTexture("/Textures/Interface/Nano/ntlogo.svg.png"))
                    .Prop(Control.StylePropertyModulateSelf, SubTextColor),

                Element<Label>().Class("WindowFooterText")
                    .Prop(Label.StylePropertyFont, f8)
                    .Prop(Label.StylePropertyFontColor, SubTextColor),

                // ============ Кнопка закрытия окна (перекрываем BaseRules) ============
                new StyleRule(
                    new SelectorElement(typeof(TextureButton), new[] { DefaultWindow.StyleClassWindowCloseButton }, null, null),
                    new[]
                    {
                        new StyleProperty(TextureButton.StylePropertyTexture, resCache.GetTexture("/Textures/Interface/Nano/cross.svg.png")),
                        new StyleProperty(Control.StylePropertyModulateSelf, TextColor),
                    }),
                new StyleRule(
                    new SelectorElement(typeof(TextureButton), new[] { DefaultWindow.StyleClassWindowCloseButton }, null,
                        new[] { TextureButton.StylePseudoClassHover }),
                    new[] { new StyleProperty(Control.StylePropertyModulateSelf, DangerRedHov) }),
                new StyleRule(
                    new SelectorElement(typeof(TextureButton), new[] { DefaultWindow.StyleClassWindowCloseButton }, null,
                        new[] { TextureButton.StylePseudoClassPressed }),
                    new[] { new StyleProperty(Control.StylePropertyModulateSelf, DangerRed) }),

                // CrossButtonRed
                Element<TextureButton>().Class("CrossButtonRed")
                    .Prop(TextureButton.StylePropertyTexture, resCache.GetTexture("/Textures/Interface/Nano/cross.svg.png"))
                    .Prop(Control.StylePropertyModulateSelf, DangerRed),
                Element<TextureButton>().Class("CrossButtonRed").Pseudo(TextureButton.StylePseudoClassHover)
                    .Prop(Control.StylePropertyModulateSelf, DangerRedHov),

                // ============ MenuButton (верхняя панель F1–F10) ============
                new StyleRule(
                    new SelectorElement(typeof(MenuButton), new[] { ButtonSquare }, null, null),
                    new[] { new StyleProperty(Button.StylePropertyStyleBox, buttonBase) }),
                new StyleRule(
                    new SelectorElement(typeof(MenuButton), new[] { ButtonOpenLeft }, null, null),
                    new[] { new StyleProperty(Button.StylePropertyStyleBox, buttonBase) }),
                new StyleRule(
                    new SelectorElement(typeof(MenuButton), new[] { ButtonOpenRight }, null, null),
                    new[] { new StyleProperty(Button.StylePropertyStyleBox, buttonBase) }),
                new StyleRule(
                    new SelectorElement(typeof(MenuButton), null, null, new[] { Button.StylePseudoClassNormal }),
                    new[] { new StyleProperty(Button.StylePropertyModulateSelf, BtnDefault) }),
                new StyleRule(
                    new SelectorElement(typeof(MenuButton), null, null, new[] { Button.StylePseudoClassHover }),
                    new[] { new StyleProperty(Button.StylePropertyModulateSelf, BtnHovered) }),
                new StyleRule(
                    new SelectorElement(typeof(MenuButton), null, null, new[] { Button.StylePseudoClassPressed }),
                    new[] { new StyleProperty(Button.StylePropertyModulateSelf, BtnPressed) }),
                new StyleRule(
                    new SelectorElement(typeof(MenuButton), new[] { MenuButton.StyleClassRedTopButton }, null, new[] { Button.StylePseudoClassNormal }),
                    new[] { new StyleProperty(Button.StylePropertyModulateSelf, DangerRed) }),
                new StyleRule(
                    new SelectorElement(typeof(MenuButton), new[] { MenuButton.StyleClassRedTopButton }, null, new[] { Button.StylePseudoClassHover }),
                    new[] { new StyleProperty(Button.StylePropertyModulateSelf, DangerRedHov) }),
                new StyleRule(
                    new SelectorElement(typeof(Label), new[] { MenuButton.StyleClassLabelTopButton }, null, null),
                    new[] { new StyleProperty(Label.StylePropertyFont, f14bd) }),

                // ============ ContextMenu ============
                Element<PanelContainer>().Class(ContextMenuPopup.StyleClassContextMenuPopup)
                    .Prop(PanelContainer.StylePropertyPanel, panelBg),
                Element<PanelContainer>().Class(ContextMenuPopup.StyleClassContextMenuPopup)
                    .Prop("font", f12),
                Element<ContextMenuElement>().Class(ContextMenuElement.StyleClassContextMenuButton)
                    .Prop(ContainerButton.StylePropertyStyleBox, buttonContext),
                Element<ContextMenuElement>().Class(ContextMenuElement.StyleClassContextMenuButton)
                    .Prop("font", f12),
                Element<ContextMenuElement>().Class(ContextMenuElement.StyleClassContextMenuButton)
                    .Pseudo(ContainerButton.StylePseudoClassNormal)
                    .Prop(Control.StylePropertyModulateSelf, BtnDefault),
                Element<ContextMenuElement>().Class(ContextMenuElement.StyleClassContextMenuButton)
                    .Pseudo(ContainerButton.StylePseudoClassHover)
                    .Prop(Control.StylePropertyModulateSelf, BtnHovered),
                Element<ContextMenuElement>().Class(ContextMenuElement.StyleClassContextMenuButton)
                    .Pseudo(ContainerButton.StylePseudoClassPressed)
                    .Prop(Control.StylePropertyModulateSelf, BtnPressed),
                Element<ContextMenuElement>().Class(ContextMenuElement.StyleClassContextMenuButton)
                    .Pseudo(ContainerButton.StylePseudoClassDisabled)
                    .Prop(Control.StylePropertyModulateSelf, BtnDisabled),

                Element<ContextMenuElement>().Class(ConfirmationMenuElement.StyleClassConfirmationContextMenuButton)
                    .Prop(ContainerButton.StylePropertyStyleBox, buttonContext),
                Element<ContextMenuElement>().Class(ConfirmationMenuElement.StyleClassConfirmationContextMenuButton)
                    .Pseudo(ContainerButton.StylePseudoClassNormal)
                    .Prop(Control.StylePropertyModulateSelf, DangerRed),
                Element<ContextMenuElement>().Class(ConfirmationMenuElement.StyleClassConfirmationContextMenuButton)
                    .Pseudo(ContainerButton.StylePseudoClassHover)
                    .Prop(Control.StylePropertyModulateSelf, DangerRedHov),

                // ============ ExamineButton ============
                Element<ExamineButton>().Class(ExamineButton.StyleClassExamineButton)
                    .Prop(ContainerButton.StylePropertyStyleBox, buttonContext),
                Element<ExamineButton>().Class(ExamineButton.StyleClassExamineButton)
                    .Pseudo(ContainerButton.StylePseudoClassNormal)
                    .Prop(Control.StylePropertyModulateSelf, Color.Transparent),
                Element<ExamineButton>().Class(ExamineButton.StyleClassExamineButton)
                    .Pseudo(ContainerButton.StylePseudoClassHover)
                    .Prop(Control.StylePropertyModulateSelf, BtnHovered),
                Element<ExamineButton>().Class(ExamineButton.StyleClassExamineButton)
                    .Pseudo(ContainerButton.StylePseudoClassPressed)
                    .Prop(Control.StylePropertyModulateSelf, BtnPressed),

                // ============ ItemStatus ============
                Element()
                    .Class("ItemStatusNotHeld")
                    .Prop("font", f10i)
                    .Prop("font-color", SubTextColor)
                    .Prop(nameof(Control.Margin), new Thickness(4, 0, 0, 2)),
                Element()
                    .Class("ItemStatus")
                    .Prop(nameof(RichTextLabel.LineHeightScale), 0.7f)
                    .Prop(nameof(Control.Margin), new Thickness(4, 0, 0, 2)),

                // ============ Storage-кнопки ============
                Element<ContainerButton>().Class("storageButton")
                    .Prop(ContainerButton.StylePropertyStyleBox, buttonStorage),
                Element<ContainerButton>().Class("storageButton")
                    .Pseudo(ContainerButton.StylePseudoClassNormal)
                    .Prop(Control.StylePropertyModulateSelf, BtnDefault),
                Element<ContainerButton>().Class("storageButton")
                    .Pseudo(ContainerButton.StylePseudoClassHover)
                    .Prop(Control.StylePropertyModulateSelf, BtnHovered),
                Element<ContainerButton>().Class("storageButton")
                    .Pseudo(ContainerButton.StylePseudoClassPressed)
                    .Prop(Control.StylePropertyModulateSelf, BtnPressed),
                Element<ContainerButton>().Class("storageButton")
                    .Pseudo(ContainerButton.StylePseudoClassDisabled)
                    .Prop(Control.StylePropertyModulateSelf, BtnDisabled),

                // ============ ListContainer ============
                Element<ContainerButton>().Class(ListContainer.StyleClassListContainerButton)
                    .Prop(ContainerButton.StylePropertyStyleBox, buttonBase),
                Element<ContainerButton>().Class(ListContainer.StyleClassListContainerButton)
                    .Pseudo(ContainerButton.StylePseudoClassNormal)
                    .Prop(Control.StylePropertyModulateSelf, BtnDefault),
                Element<ContainerButton>().Class(ListContainer.StyleClassListContainerButton)
                    .Pseudo(ContainerButton.StylePseudoClassHover)
                    .Prop(Control.StylePropertyModulateSelf, BtnHovered),
                Element<ContainerButton>().Class(ListContainer.StyleClassListContainerButton)
                    .Pseudo(ContainerButton.StylePseudoClassPressed)
                    .Prop(Control.StylePropertyModulateSelf, BtnPressed),
                Element<ContainerButton>().Class(ListContainer.StyleClassListContainerButton)
                    .Pseudo(ContainerButton.StylePseudoClassDisabled)
                    .Prop(Control.StylePropertyModulateSelf, BtnDisabled),

                // ============ MainMenu ============
                new StyleRule(new SelectorChild(
                    new SelectorElement(typeof(Button), null, "mainMenu", null),
                    new SelectorElement(typeof(Label), null, null, null)),
                    new[] { new StyleProperty("font", f16b) }),
                new StyleRule(new SelectorElement(typeof(BoxContainer), null, "mainMenuVBox", null),
                    new[] { new StyleProperty(BoxContainer.StylePropertySeparation, 2) }),

                // ============ Кнопка disabled — текст полупрозрачный ============
                new StyleRule(new SelectorChild(
                    new SelectorElement(typeof(Button), null, null, new[] { ContainerButton.StylePseudoClassDisabled }),
                    new SelectorElement(typeof(Label), null, null, null)),
                    new[] { new StyleProperty("font-color", Color.FromHex("#E5E5E581")) }),

                // ============ ContextMenu – классы для verb-ов ============
                Element<RichTextLabel>().Class(InteractionVerb.DefaultTextStyleClass)
                    .Prop(Label.StylePropertyFont, f12bi),
                Element<RichTextLabel>().Class(ActivationVerb.DefaultTextStyleClass)
                    .Prop(Label.StylePropertyFont, f12b),
                Element<RichTextLabel>().Class(AlternativeVerb.DefaultTextStyleClass)
                    .Prop(Label.StylePropertyFont, f12i),
                Element<RichTextLabel>().Class(Verb.DefaultTextStyleClass)
                    .Prop(Label.StylePropertyFont, f12),

                // ============ Бумага ============
                new StyleRule(new SelectorElement(typeof(PanelContainer), new[] { "PaperDefaultBorder" }, null, null),
                    new[] { new StyleProperty(PanelContainer.StylePropertyPanel, paperBackground) }),
                new StyleRule(new SelectorElement(typeof(RichTextLabel), new[] { "PaperWrittenText" }, null, null),
                    new[]
                    {
                        new StyleProperty(Label.StylePropertyFont, f12),
                        new StyleProperty(Control.StylePropertyModulateSelf, Color.FromHex("#111111")),
                    }),
                Element<LineEdit>().Class("PaperLineEdit")
                    .Prop(LineEdit.StylePropertyStyleBox, new StyleBoxEmpty()),

                // ============ Радиальное меню ============
                new StyleRule(new SelectorElement(typeof(TextureButton), new[] { "RadialMenuButton" }, null, null),
                    new[] { new StyleProperty(TextureButton.StylePropertyTexture,
                        resCache.GetTexture("/Textures/Interface/Radial/button_normal.png")) }),
                new StyleRule(new SelectorElement(typeof(TextureButton), new[] { "RadialMenuButton" }, null,
                        new[] { TextureButton.StylePseudoClassHover }),
                    new[] { new StyleProperty(TextureButton.StylePropertyTexture,
                        resCache.GetTexture("/Textures/Interface/Radial/button_hover.png")) }),
                new StyleRule(new SelectorElement(typeof(TextureButton), new[] { "RadialMenuCloseButton" }, null, null),
                    new[] { new StyleProperty(TextureButton.StylePropertyTexture,
                        resCache.GetTexture("/Textures/Interface/Radial/close_normal.png")) }),
                new StyleRule(new SelectorElement(typeof(TextureButton), new[] { "RadialMenuBackButton" }, null, null),
                    new[] { new StyleProperty(TextureButton.StylePropertyTexture,
                        resCache.GetTexture("/Textures/Interface/Radial/back_normal.png")) }),

                // ============ Слайдеры ============
                new StyleRule(new SelectorElement(typeof(Slider), null, null, null),
                    new[]
                    {
                        new StyleProperty(Slider.StylePropertyBackground,
                            new StyleBoxFlat { BackgroundColor = DarkBorder }),
                        new StyleProperty(Slider.StylePropertyForeground,
                            new StyleBoxFlat { BackgroundColor = LightBorder }),
                        new StyleProperty(Slider.StylePropertyFill,
                            new StyleBoxFlat { BackgroundColor = ProgressFill }),
                        new StyleProperty(Slider.StylePropertyGrabber,
                            new StyleBoxFlat { BackgroundColor = TextColor }),
                    }),
                new StyleRule(new SelectorElement(typeof(Slider), new []{"Red"}, null, null),
                    new[] { new StyleProperty(Slider.StylePropertyFill,
                        new StyleBoxFlat { BackgroundColor = Color.Red }) }),
                new StyleRule(new SelectorElement(typeof(Slider), new []{"Green"}, null, null),
                    new[] { new StyleProperty(Slider.StylePropertyFill,
                        new StyleBoxFlat { BackgroundColor = Color.LimeGreen }) }),
                new StyleRule(new SelectorElement(typeof(Slider), new []{"Blue"}, null, null),
                    new[] { new StyleProperty(Slider.StylePropertyFill,
                        new StyleBoxFlat { BackgroundColor = Color.Blue }) }),
                new StyleRule(new SelectorElement(typeof(Slider), new []{"White"}, null, null),
                    new[] { new StyleProperty(Slider.StylePropertyFill,
                        new StyleBoxFlat { BackgroundColor = Color.White }) }),

                // ============ OptionButton ============
                new StyleRule(new SelectorElement(typeof(OptionButton), null, null, null),
                    new[] { new StyleProperty(ContainerButton.StylePropertyStyleBox, buttonRect) }),
                new StyleRule(new SelectorElement(typeof(OptionButton), null, null, new[] { ContainerButton.StylePseudoClassNormal }),
                    new[] { new StyleProperty(Control.StylePropertyModulateSelf, Color.White) }),
                new StyleRule(new SelectorElement(typeof(OptionButton), null, null, new[] { ContainerButton.StylePseudoClassHover }),
                    new[] { new StyleProperty(ContainerButton.StylePropertyStyleBox, buttonRectHover) }),
                new StyleRule(new SelectorElement(typeof(OptionButton), null, null, new[] { ContainerButton.StylePseudoClassPressed }),
                    new[] { new StyleProperty(ContainerButton.StylePropertyStyleBox, buttonRectPressed) }),
                new StyleRule(new SelectorElement(typeof(OptionButton), null, null, new[] { ContainerButton.StylePseudoClassDisabled }),
                    new[] { new StyleProperty(ContainerButton.StylePropertyStyleBox, buttonRectDisabled) }),
                new StyleRule(new SelectorElement(typeof(PanelContainer), new[] { "OptionsBackground" }, null, null),
                    new[] { new StyleProperty(PanelContainer.StylePropertyPanel,
                        new StyleBoxFlat { BackgroundColor = Color.FromHex("#2D3528") }) }),
                new StyleRule(new SelectorElement(typeof(Label), new[] { OptionButton.StyleClassOptionButton }, null, null),
                    new[] { new StyleProperty(Label.StylePropertyAlignMode, Label.AlignMode.Center) }),

                // ============ Угловая рамка ============
                Element<PanelContainer>().Class(ClassAngleRect)
                    .Prop(PanelContainer.StylePropertyPanel, new StyleBoxFlat
                    {
                        BackgroundColor = MainBg,
                        BorderColor = DarkBorder,
                        BorderThickness = new Thickness(2),
                    }),

                // ============ Fancy Tree ============
                Element<ContainerButton>().Identifier(TreeItem.StyleIdentifierTreeButton)
                    .Class(TreeItem.StyleClassEvenRow)
                    .Prop(ContainerButton.StylePropertyStyleBox,
                        new StyleBoxFlat { BackgroundColor = Color.FromHex("#3A4535") }),
                Element<ContainerButton>().Identifier(TreeItem.StyleIdentifierTreeButton)
                    .Class(TreeItem.StyleClassOddRow)
                    .Prop(ContainerButton.StylePropertyStyleBox,
                        new StyleBoxFlat { BackgroundColor = Color.FromHex("#2D3528") }),
                Element<ContainerButton>().Identifier(TreeItem.StyleIdentifierTreeButton)
                    .Class(TreeItem.StyleClassSelected)
                    .Prop(ContainerButton.StylePropertyStyleBox,
                        new StyleBoxFlat { BackgroundColor = Color.FromHex("#5A6B4F") }),
                Element<ContainerButton>().Identifier(TreeItem.StyleIdentifierTreeButton)
                    .Pseudo(ContainerButton.StylePseudoClassHover)
                    .Prop(ContainerButton.StylePropertyStyleBox,
                        new StyleBoxFlat { BackgroundColor = Color.FromHex("#5A6B4F") }),

                // ============ Метки состояния ============
                Element<Label>().Class("StatusFieldTitle").Prop("font-color", Accent),
                Element<Label>().Class("Good").Prop("font-color", GoodGreen),
                Element<Label>().Class("Caution").Prop("font-color", WarnOrange),
                Element<Label>().Class("Danger").Prop("font-color", DangerRed),
                Element<Label>().Class("Disabled").Prop("font-color", SubTextColor),

                // APC/SMES power state
                new StyleRule(new SelectorElement(typeof(Label), new[] {"PowerStateNone"}, null, null),
                    new[] { new StyleProperty(Label.StylePropertyFontColor, DangerRed) }),
                new StyleRule(new SelectorElement(typeof(Label), new[] {"PowerStateLow"}, null, null),
                    new[] { new StyleProperty(Label.StylePropertyFontColor, WarnOrange) }),
                new StyleRule(new SelectorElement(typeof(Label), new[] {"PowerStateGood"}, null, null),
                    new[] { new StyleProperty(Label.StylePropertyFontColor, GoodGreen) }),

                // ============ Консольные метки ============
                new StyleRule(new SelectorElement(typeof(Label), new[] { "ConsoleText" }, null, null),
                    new[] { new StyleProperty(Label.StylePropertyFont, mono12) }),
                new StyleRule(new SelectorElement(typeof(Label), new[] { "ConsoleSubHeading" }, null, null),
                    new[] { new StyleProperty(Label.StylePropertyFont, f12b) }),
                new StyleRule(new SelectorElement(typeof(Label), new[] { "ConsoleHeading" }, null, null),
                    new[] { new StyleProperty(Label.StylePropertyFont, f14bd) }),

                // ============ Big Button ============
                new StyleRule(new SelectorChild(
                    new SelectorElement(typeof(Button), new[] {"ButtonBig"}, null, null),
                    new SelectorElement(typeof(Label), null, null, null)),
                    new[] { new StyleProperty("font", f16) }),

                // ============ Верхние кнопки MenuButton (уже задали) ============
                // MonotoneButton (заглушка — плоский)
                new StyleRule(
                    new SelectorElement(typeof(MonotoneButton), null, null, null),
                    new[] { new StyleProperty(Button.StylePropertyStyleBox, buttonBase) }),
                new StyleRule(
                    new SelectorElement(typeof(MonotoneButton), null, null, new[] { Button.StylePseudoClassPressed }),
                    new[] { new StyleProperty(Button.StylePropertyStyleBox, buttonPressed) }),

                // ============ NanoHeading ============
                new StyleRule(
                    new SelectorChild(
                        SelectorElement.Type(typeof(NanoHeading)),
                        SelectorElement.Type(typeof(PanelContainer))),
                    new[]
                    {
                        new StyleProperty(PanelContainer.StylePropertyPanel, new StyleBoxFlat
                        {
                            BackgroundColor = DarkBorder,
                            BorderColor = LightBorder,
                            BorderThickness = new Thickness(1),
                            ContentMarginTopOverride = 2,
                            ContentMarginLeftOverride = 10,
                        }),
                    }),

                // ============ StripeBack ============
                new StyleRule(
                    SelectorElement.Type(typeof(StripeBack)),
                    new[] { new StyleProperty(StripeBack.StylePropertyBackground,
                        new StyleBoxFlat { BackgroundColor = MainBg }) }),

                // ============ StyleClassItemStatus ============
                new StyleRule(SelectorElement.Class("ItemStatus"), new[]
                {
                    new StyleProperty("font", f10),
                }),
                Element<RichTextLabel>()
                    .Class("ItemStatus")
                    .Prop(nameof(RichTextLabel.LineHeightScale), 0.7f)
                    .Prop(nameof(Control.Margin), new Thickness(0, 0, 0, -6)),

                // ============ ColorableSlider ============
                new StyleRule(SelectorElement.Type(typeof(ColorableSlider)), new []
                {
                    new StyleProperty(ColorableSlider.StylePropertyFillWhite,
                        new StyleBoxFlat { BackgroundColor = Color.White }),
                    new StyleProperty(ColorableSlider.StylePropertyBackgroundWhite,
                        new StyleBoxFlat { BackgroundColor = Color.White }),
                }),

                // ============ Help-кнопка ============
                Element<TextureButton>()
                    .Class("HelpButton")
                    .Prop(TextureButton.StylePropertyTexture, resCache.GetTexture("/Textures/Interface/VerbIcons/information.svg.192dpi.png")),

                // ============ BackgroundOpenRight/Left ============
                Element<PanelContainer>().Class("BackgroundOpenRight")
                    .Prop(PanelContainer.StylePropertyPanel, buttonBase)
                    .Prop(Control.StylePropertyModulateSelf, MainBg),
                Element<PanelContainer>().Class("BackgroundOpenLeft")
                    .Prop(PanelContainer.StylePropertyPanel, buttonBase)
                    .Prop(Control.StylePropertyModulateSelf, MainBg),

                // ============ PDA ============
                Element<PanelContainer>().Class("PdaContentBackground")
                    .Prop(PanelContainer.StylePropertyPanel, panelBg),
                Element<PanelContainer>().Class("PdaBackground")
                    .Prop(PanelContainer.StylePropertyPanel, new StyleBoxFlat { BackgroundColor = Color.Black }),
                Element<PanelContainer>().Class("PdaBackgroundRect")
                    .Prop(PanelContainer.StylePropertyPanel, new StyleBoxFlat
                    {
                        BackgroundColor = MainBg,
                        BorderColor = DarkBorder,
                        BorderThickness = new Thickness(1),
                    }),
                Element<PanelContainer>().Class("PdaBorderRect")
                    .Prop(PanelContainer.StylePropertyPanel, new StyleBoxFlat
                    {
                        BackgroundColor = MainBg,
                        BorderColor = DarkBorder,
                        BorderThickness = new Thickness(2),
                    }),
                Element<PanelContainer>().Class("BackgroundDark")
                    .Prop(PanelContainer.StylePropertyPanel, new StyleBoxFlat { BackgroundColor = DarkBorder }),
                Element<Label>().Class("PdaContentFooterText")
                    .Prop(Label.StylePropertyFont, f10)
                    .Prop(Label.StylePropertyFontColor, SubTextColor),
                Element<Label>().Class("PdaWindowFooterText")
                    .Prop(Label.StylePropertyFont, f10)
                    .Prop(Label.StylePropertyFontColor, SubTextColor),

                // ============ Silicon Law ============
                Element<Label>().Class(SiliconLawContainer.StyleClassSiliconLawPositionLabel)
                    .Prop(Label.StylePropertyFontColor, Accent),

                // ============ Pin-кнопки ============
                new StyleRule(
                    new SelectorElement(typeof(TextureButton), new[] { "pinButtonPinned" }, null, null),
                    new[] { new StyleProperty(TextureButton.StylePropertyTexture,
                        resCache.GetTexture("/Textures/Interface/Bwoink/pinned.png")) }),
                new StyleRule(
                    new SelectorElement(typeof(TextureButton), new[] { "pinButtonUnpinned" }, null, null),
                    new[] { new StyleProperty(TextureButton.StylePropertyTexture,
                        resCache.GetTexture("/Textures/Interface/Bwoink/un_pinned.png")) }),

                // ============ Inset ============
                Element<PanelContainer>().Class("Inset")
                    .Prop(PanelContainer.StylePropertyPanel, new StyleBoxFlat
                    {
                        BackgroundColor = Color.FromHex("#202023"),
                        BorderColor = DarkBorder,
                        BorderThickness = new Thickness(2),
                    }),

                // ============ Placeholder ============
                new StyleRule(new SelectorElement(typeof(Placeholder), null, null, null),
                    new[] { new StyleProperty(PanelContainer.StylePropertyPanel, panelBg) }),
                new StyleRule(new SelectorElement(typeof(Label), new[] { "PlaceholderText" }, null, null),
                    new[]
                    {
                        new StyleProperty(Label.StylePropertyFont, f16),
                        new StyleProperty(Label.StylePropertyFontColor, SubTextColor),
                    }),
            }).ToList());
        }
    }
}
