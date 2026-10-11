using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Particles;

/// <summary>
/// Spawns particles (e.g. dust clouds) at this entity's coordinates on a set interval.
/// Every particle gets a random lifetime, a random direction inside the configured sector,
/// a random initial speed and fades in/out over its lifetime.
/// </summary>
[RegisterComponent, EntityCategory("Spawner")]
[AutoGenerateComponentPause]
public sealed partial class ParticleSpawnerComponent : Component
{
    /// <summary>
    /// Prototype of the spawned particle.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Particle;

    [DataField]
    public int MaxParticles;

    /// <summary>
    /// Time between spawns.
    /// </summary>
    [DataField]
    public TimeSpan IntervalMax = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan IntervalMin = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Minimum particle lifetime in seconds.
    /// </summary>
    [DataField]
    public float LifetimeMin = 3f;

    /// <summary>
    /// Maximum particle lifetime in seconds.
    /// </summary>
    [DataField]
    public float LifetimeMax = 5f;

    /// <summary>
    /// Minimum initial speed, units per second.
    /// </summary>
    [DataField]
    public float SpeedMin = 0.4f;

    /// <summary>
    /// Maximum initial speed, units per second.
    /// </summary>
    [DataField]
    public float SpeedMax = 1.2f;

    /// <summary>
    /// Sector of spawn directions: 360 — a full circle, less — a cone.
    /// </summary>
    [DataField]
    public Angle Spread = Angle.FromDegrees(360);

    /// <summary>
    /// Centre of the sector relative to the emitter's world rotation.
    /// </summary>
    [DataField]
    public Angle Direction;

    /// <summary>
    /// Whether spawning is enabled.
    /// </summary>
    [DataField]
    public bool Enabled = true;

    /// <summary>
    /// Game time of the next spawn.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextFire;
}
