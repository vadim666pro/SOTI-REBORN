using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

    public sealed partial class CCVars
    {
    public static readonly CVarDef<int> HudTheme =
        CVarDef.Create("hud.theme", 0, CVar.ARCHIVE | CVar.CLIENTONLY);

    public static readonly CVarDef<bool> HudHeldItemShow =
        CVarDef.Create("hud.held_item_show", true, CVar.ARCHIVE | CVar.CLIENTONLY);

    public static readonly CVarDef<bool> CombatModeIndicatorsPointShow =
        CVarDef.Create("hud.combat_mode_indicators_point_show", true, CVar.ARCHIVE | CVar.CLIENTONLY);

    public static readonly CVarDef<bool> LoocAboveHeadShow =
        CVarDef.Create("hud.show_looc_above_head", true, CVar.ARCHIVE | CVar.CLIENTONLY);

    public static readonly CVarDef<float> HudHeldItemOffset =
        CVarDef.Create("hud.held_item_offset", 28f, CVar.ARCHIVE | CVar.CLIENTONLY);

    /// <summary>
    ///     Displays framerate counter
    /// </summary>
    public static readonly CVarDef<bool> HudFpsCounterVisible =
        CVarDef.Create("hud.fps_counter_visible", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    ///     Displays the fork ID and version number
    /// </summary>
    public static readonly CVarDef<bool> HudVersionWatermark =
        CVarDef.Create("hud.version_watermark", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    ///     Bodycam fisheye intensity (0 = off, 0.05 = default)
    /// </summary>
    public static readonly CVarDef<float> HudBodycamFisheye =
        CVarDef.Create("bodycam.fisheye_intensity", 0f, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    ///     Bodycam grain intensity (0 = off, 0.3 = default)
    /// </summary>
    public static readonly CVarDef<float> HudBodycamGrain =
        CVarDef.Create("bodycam.grain_intensity", 0f, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    ///     HUD accent color as hex string (e.g. "#FF8C00" for HL2 amber).
    ///     Empty string uses theme default.
    /// </summary>
    public static readonly CVarDef<string> HudAccentColor =
        CVarDef.Create("hud.accent_color", "", CVar.CLIENTONLY | CVar.ARCHIVE);
    }
