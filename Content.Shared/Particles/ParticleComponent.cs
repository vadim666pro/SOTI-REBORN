using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Particles;

/// <summary>
/// A particle (e.g. a dust cloud) spawned by <see cref="Content.Server.Particles.ParticleSpawnerComponent"/>.
/// Holds everything needed to fade the sprite in and out over the particle's lifetime.
/// Lifetime and spawn time are set by the server; the alpha is computed client-side every frame.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ParticleComponent : Component
{
    /// <summary>
    /// How long the particle lives, in seconds. Randomized by the spawner on spawn
    /// and kept in sync with <see cref="Robust.Shared.Spawners.TimedDespawnComponent.Lifetime"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Lifetime = 4f;

    /// <summary>
    /// Game time at which the particle was spawned. Networked so that clients that receive
    /// the entity late (PVS) still compute the correct fade progress.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan StartTime;

    /// <summary>
    /// Fraction of the lifetime over which alpha ramps from 0 to 1 (fade-in).
    /// </summary>
    [DataField]
    public float FadeInFraction = 0.175f;

    /// <summary>
    /// Fraction of the lifetime over which alpha drops from 1 to 0 (fade-out).
    /// </summary>
    [DataField]
    public float FadeOutFraction = 0.25f;

    /// <summary>
    /// Air drag of the particle: the final LinearDamping applied to its physics body.
    /// Higher values make the particle slow down faster.
    /// </summary>
    [DataField]
    public float AirResistance = 1.5f;
}
