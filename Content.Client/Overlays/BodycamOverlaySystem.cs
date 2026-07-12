using System.Numerics;
using Content.Shared.CCVar;
using Content.Shared.Damage;
using Content.Shared.Ghost;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Configuration;

namespace Content.Client.Overlays;

/// <summary>
/// Manages bodycam overlays and drives camera lag + health-based shader parameters.
/// Bodycam is always active for alive players — hidden when ghost/dead.
/// </summary>
public sealed class BodycamOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;

    private BodycamOverlay? _post;
    private BodycamHudOverlay? _hud;
    private bool _overlaysActive;

    // ── Camera lag state ────────────────────────────────────────────────────
    private Vector2 _cameraOffset;
    private Vector2 _cameraOffsetVel;
    private Vector2 _lastPlayerPos;
    private bool _hasLastPos;

    // ── Health / damage state ────────────────────────────────────────────────
    private float _damageLevel;
    private float _damageFlash;
    private float _glitchIntensity;
    private float _lastTotalDamage;

    // ── Configurable via CVar or editable at runtime ─────────────────────────
    public float LagSmoothing { get; set; } = 8f;
    public float LagMaxOffset { get; set; } = 0.25f;
    public float LagDamping { get; set; } = 12f;

    public override void Initialize()
    {
        base.Initialize();

        _post = new BodycamOverlay();
        _hud = new BodycamHudOverlay();

        _cfg.OnValueChanged(CCVars.HudBodycamFisheye, OnFisheyeChanged);
        _cfg.OnValueChanged(CCVars.HudBodycamGrain, OnGrainChanged);

        SubscribeLocalEvent<DamageableComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<DamageableComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _cfg.UnsubValueChanged(CCVars.HudBodycamFisheye, OnFisheyeChanged);
        _cfg.UnsubValueChanged(CCVars.HudBodycamGrain, OnGrainChanged);

        RemoveOverlays();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_post == null)
            return;

        var player = _player.LocalEntity;
        if (player == null || !_entMan.TryGetComponent<TransformComponent>(player.Value, out var xform))
        {
            RemoveOverlays();
            return;
        }

        // ── Ghost check — hide overlays when dead ────────────────────────────
        var isGhost = _entMan.TryGetComponent<GhostComponent>(player.Value, out _);

        if (isGhost)
        {
            RemoveOverlays();
            return;
        }

        // Ensure overlays are active
        if (!_overlaysActive)
            AddOverlays();

        var pos = xform.WorldPosition;

        // ── Camera lag calculation ───────────────────────────────────────────
        if (_hasLastPos)
        {
            var velocity = (pos - _lastPlayerPos) / MathF.Max(frameTime, 0.001f);
            var targetOffset = Vector2.Clamp(
                velocity * 0.015f,
                new Vector2(-LagMaxOffset),
                new Vector2(LagMaxOffset));

            _cameraOffsetVel += (targetOffset - _cameraOffset) * LagSmoothing * frameTime;
            _cameraOffsetVel *= MathF.Max(0f, 1f - LagDamping * frameTime);
            _cameraOffset += _cameraOffsetVel * frameTime;
        }
        _lastPlayerPos = pos;
        _hasLastPos = true;

        // ── Health / damage tracking ─────────────────────────────────────────
        UpdateDamageState(player.Value, frameTime);

        // ── Push parameters to overlay ───────────────────────────────────────
        _post.CameraOffset = _cameraOffset;
        _post.DamageLevel = _damageLevel;
        _post.DamageFlash = _damageFlash;
        _post.GlitchIntensity = _glitchIntensity;
    }

    private void AddOverlays()
    {
        if (_post == null || _hud == null || _overlaysActive)
            return;

        _overlayMan.AddOverlay(_post);
        _overlayMan.AddOverlay(_hud);
        _overlaysActive = true;
    }

    private void RemoveOverlays()
    {
        if (!_overlaysActive)
            return;

        if (_post != null)
            _overlayMan.RemoveOverlay(_post);
        if (_hud != null)
            _overlayMan.RemoveOverlay(_hud);
        _overlaysActive = false;

        // Reset damage state when entering ghost
        _damageLevel = 0f;
        _damageFlash = 0f;
        _glitchIntensity = 0f;
        _lastTotalDamage = 0f;
    }

    private void UpdateDamageState(EntityUid player, float frameTime)
    {
        if (!_entMan.TryGetComponent<DamageableComponent>(player, out var damageable))
            return;

        var totalDamage = damageable.TotalDamage.Float();

        if (totalDamage > _lastTotalDamage)
        {
            var delta = totalDamage - _lastTotalDamage;
            _damageFlash = MathF.Min(1f, delta / 30f);
        }
        _lastTotalDamage = totalDamage;

        var targetDamage = MathF.Min(1f, totalDamage / 100f);
        _damageLevel += (targetDamage - _damageLevel) * MathF.Min(1f, 3f * frameTime);

        _damageFlash *= MathF.Max(0f, 1f - 6f * frameTime);

        if (_damageFlash > 0.05f)
            _glitchIntensity = MathF.Max(_glitchIntensity, _damageFlash * 1.5f);
        else
            _glitchIntensity *= MathF.Max(0f, 1f - 4f * frameTime);

        if (_damageLevel > 0.6f)
            _glitchIntensity = MathF.Max(_glitchIntensity, (_damageLevel - 0.6f) * 0.5f);
    }

    private void OnDamageChanged(EntityUid uid, DamageableComponent component, DamageChangedEvent args)
    {
    }

    private void OnMobStateChanged(EntityUid uid, DamageableComponent component, MobStateChangedEvent args)
    {
    }

    private void OnFisheyeChanged(float value)
    {
        if (_post != null)
            _post.FisheyeStrength = value;
    }

    private void OnGrainChanged(float value)
    {
        if (_post != null)
            _post.GrainStrength = value;
    }
}
