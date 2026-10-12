using Content.Shared.Particles;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;

namespace Content.Server.Particles;

/// <summary>
/// Spawns particles on the interval configured in <see cref="ParticleSpawnerComponent"/> and
/// gives each one a random lifetime, a random direction inside the spread sector and a random
/// initial velocity (which then decays via LinearDamping / air resistance).
/// </summary>
public sealed class ParticleSpawnerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<ParticleSpawnerComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (!comp.Enabled || curTime < comp.NextFire)
                continue;

            if (CountParticles(uid) >= comp.MaxParticles)
                continue;
            var min = (int)comp.IntervalMin.TotalSeconds;
            var max = (int)comp.IntervalMax.TotalSeconds;

            if (max < min)
                (min, max) = (max, min);

            var interval = min + (double)(_random.NextDouble() * (max - min));

            var finalInterval = Math.Round(interval, 2, MidpointRounding.AwayFromZero);

            comp.NextFire = curTime + TimeSpan.FromSeconds(finalInterval);
            SpawnParticle(uid, comp, xform, curTime);

        }
    }   

    private int CountParticles(EntityUid emmiter)
    {
        var count = 0;
        var query = EntityQueryEnumerator<SpawnedParticleComponent>();

        while (query.MoveNext(out var particleUid, out var spawned))
        {
            if (spawned.Emmitter == emmiter)
                count++;
        }
        return count;
    }

    private void SpawnParticle(EntityUid uid, ParticleSpawnerComponent comp, TransformComponent xform, TimeSpan curTime)
    {
        var particle = Spawn(comp.Particle, xform.Coordinates);
        AddComp<SpawnedParticleComponent>(particle).Emmitter = uid; // ДОБАВЛЕНИЯ КОМПОНЕНТА ДЛЯ ПОДСЧЕТА КОЛ-ВА ЗАСПАВНЕННЫХ ЧАСТИЧЕК

        // One random lifetime for both the despawn timer and the fade.
        var min = Math.Min(comp.LifetimeMin, comp.LifetimeMax);
        var max = Math.Max(comp.LifetimeMin, comp.LifetimeMax);
        var lifetime = _random.NextFloat(min, max);

        if (TryComp<TimedDespawnComponent>(particle, out var despawn))
            despawn.Lifetime = lifetime;

        if (TryComp<ParticleComponent>(particle, out var particleComp))
        {
            particleComp.Lifetime = lifetime;
            particleComp.StartTime = curTime;
            Dirty(particle, particleComp);
        }

        // Random direction inside the spread sector around the emitter's rotation.
        var baseAngle = _transform.GetWorldRotation(uid) + comp.Direction;
        var theta = baseAngle.Theta - comp.Spread.Theta * 0.5 + _random.NextDouble() * comp.Spread.Theta;

        // Speed is always controlled by SpeedMin/SpeedMax
        // MaxDistance is a separate limit that despawns the particle when reached
        var minSpeed = Math.Min(comp.SpeedMin, comp.SpeedMax);
        var maxSpeed = Math.Max(comp.SpeedMin, comp.SpeedMax);
        var speed = _random.NextFloat(minSpeed, maxSpeed);

        var velocity = new Angle(theta).ToWorldVec() * speed;

        if (TryComp<PhysicsComponent>(particle, out var body))
            _physics.SetLinearVelocity(particle, velocity, body: body);
    }
}
