using Content.Shared.CCVar;
using Content.Shared.Friction;
using Content.Shared.Movement.Events;
using Robust.Shared.Configuration;
using Robust.Shared.Map.Components;

namespace Content.Shared.Particles;

/// <summary>
/// Base particle system. Keeps <see cref="ParticleComponent.AirResistance"/> in charge of the
/// particle's LinearDamping (TileFrictionController overwrites damping of every moving body
/// every tick) and provides the fade-in/fade-out alpha math used by the client.
/// </summary>
public abstract class SharedParticleSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private float _airFriction;
    private float _offgridFriction;

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(CCVars.AirFriction, value => _airFriction = value, true);
        _cfg.OnValueChanged(CCVars.OffgridFriction, value => _offgridFriction = value, true);

        SubscribeLocalEvent<ParticleComponent, TileFrictionEvent>(OnTileFriction);
    }

    /// <summary>
    /// The particle body always flies with <c>bodyStatus: InAir</c>, so the friction controller
    /// uses the air/offgrid cvars as the base value and multiplies it by the event's Modifier.
    /// We pick the multiplier so the resulting LinearDamping is exactly AirResistance,
    /// regardless of which cvars the server runs with.
    /// </summary>
    private void OnTileFriction(Entity<ParticleComponent> ent, ref TileFrictionEvent args)
    {
        var xform = Transform(ent.Owner);
        var baseFriction = xform.GridUid == null || !HasComp<MapGridComponent>(xform.GridUid.Value)
            ? _offgridFriction
            : _airFriction;

        // Cvar is disabled, damping can't be applied anyway.
        if (baseFriction <= 0f)
        {
            args.Modifier = 0f;
            return;
        }

        args.Modifier = ent.Comp.AirResistance / baseFriction;
    }

    /// <summary>
    /// Progress of the particle's life, from 0 (spawn) to 1 (end of lifetime).
    /// </summary>
    public static float GetProgress(ParticleComponent particle, TimeSpan curTime)
    {
        if (particle.Lifetime <= 0f)
            return 1f;

        var elapsed = (float) (curTime - particle.StartTime).TotalSeconds;
        return Math.Clamp(elapsed / particle.Lifetime, 0f, 1f);
    }

    /// <summary>
    /// Alpha of the particle: ramps 0 → 1 over the first <see cref="ParticleComponent.FadeInFraction"/>
    /// of the lifetime, then 1 → 0 over the last <see cref="ParticleComponent.FadeOutFraction"/>.
    /// </summary>
    public static float GetAlpha(ParticleComponent particle, TimeSpan curTime)
    {
        var progress = GetProgress(particle, curTime);

        var fadeIn = particle.FadeInFraction > 0f
            ? Math.Clamp(progress / particle.FadeInFraction, 0f, 1f)
            : 1f;

        var fadeOut = particle.FadeOutFraction > 0f
            ? Math.Clamp((1f - progress) / particle.FadeOutFraction, 0f, 1f)
            : 1f;

        return Math.Min(fadeIn, fadeOut);
    }
}
