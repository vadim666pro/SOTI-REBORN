using Content.Server.Particles;
using Content.Shared.Particles;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Spawners;

namespace Content.IntegrationTests.Tests;

[TestFixture]
[TestOf(typeof(ParticleSpawnerSystem))]
public sealed class ParticleSpawnerTest
{
    [Test]
    public async Task Test()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var testMap = await pair.CreateTestMap();

        var entMan = server.EntMan;
        EntityUid emitter = default;
        await server.WaitPost(() =>
        {
            emitter = entMan.SpawnEntity("CSDustEmitter", new EntityCoordinates(testMap.Grid, 0.5f, 0.5f));
        });

        // The first shot fires immediately (NextFire defaults to zero).
        await server.WaitRunTicks(1);

        EntityUid particleUid = default;
        ParticleComponent particle = default!;
        await server.WaitAssertion(() =>
        {
            var query = entMan.EntityQueryEnumerator<ParticleComponent>();
            Assert.That(query.MoveNext(out particleUid, out particle), Is.True, "Emitter did not spawn a particle.");
            Assert.That(query.MoveNext(out _, out _), Is.False, "Emitter spawned more than one particle.");
        });

        var lifetime = particle.Lifetime;
        var startTime = particle.StartTime;

        await server.WaitAssertion(() =>
        {
            var physics = entMan.GetComponent<PhysicsComponent>(particleUid);
            var speed = physics.LinearVelocity.Length();

            Assert.Multiple(() =>
            {
                Assert.That(lifetime, Is.InRange(3f, 5f), "Lifetime was not randomized in [3, 5].");
                Assert.That(startTime, Is.LessThanOrEqualTo(server.Timing.CurTime), "Particle was spawned in the future.");
                Assert.That(startTime, Is.GreaterThan(server.Timing.CurTime - TimeSpan.FromSeconds(5)),
                    "Particle spawn time is unexpectedly far in the past.");
                Assert.That(speed, Is.InRange(0.4f * 0.9f, 1.2f), "Initial speed is outside the configured range.");

                // TimedDespawn countdown must match the randomized lifetime (it ticks down every frame).
                var despawn = entMan.GetComponent<TimedDespawnComponent>(particleUid);
                Assert.That(despawn.Lifetime, Is.LessThanOrEqualTo(lifetime).Within(0.001f));
                Assert.That(despawn.Lifetime, Is.GreaterThanOrEqualTo(lifetime - 0.5f));
            });
        });

        // Let the friction controller apply air resistance as LinearDamping.
        await server.WaitRunTicks(5);

        await server.WaitAssertion(() =>
        {
            var physics = entMan.GetComponent<PhysicsComponent>(particleUid);
            Assert.Multiple(() =>
            {
                Assert.That(physics.LinearDamping, Is.EqualTo(1.5f).Within(0.01f),
                    "AirResistance did not become the body's LinearDamping.");
                Assert.That(physics.LinearVelocity.Length(), Is.GreaterThan(0f),
                    "Particle lost all speed immediately instead of decelerating gradually.");
            });

            // Fade: 0 at spawn, 1 in the middle of life, 0 at the end of life.
            Assert.Multiple(() =>
            {
                Assert.That(SharedParticleSystem.GetAlpha(particle, startTime), Is.EqualTo(0f).Within(0.001f));
                Assert.That(SharedParticleSystem.GetAlpha(particle, startTime + TimeSpan.FromSeconds(lifetime * 0.5)),
                    Is.EqualTo(1f).Within(0.001f));
                Assert.That(SharedParticleSystem.GetAlpha(particle, startTime + TimeSpan.FromSeconds(lifetime)),
                    Is.EqualTo(0f).Within(0.001f));
                Assert.That(SharedParticleSystem.GetAlpha(particle, startTime + TimeSpan.FromSeconds(lifetime * 0.95)),
                    Is.LessThan(0.3f), "Fade-out should already have reduced alpha near the end of life.");
            });
        });

        // Spawn cadence: interval is 0.75s, so no second particle at 0.5s...
        await server.WaitRunTicks(29);
        await server.WaitAssertion(() =>
        {
            var query = entMan.EntityQueryEnumerator<ParticleComponent>();
            var count = 0;
            while (query.MoveNext(out _, out _))
                count++;
            Assert.That(count, Is.EqualTo(1), "Particle spawned earlier than the configured interval.");
        });

        // ...but a second one by 1.25s.
        await server.WaitRunTicks(45);
        await server.WaitAssertion(() =>
        {
            var query = entMan.EntityQueryEnumerator<ParticleComponent>();
            var count = 0;
            while (query.MoveNext(out _, out _))
                count++;
            Assert.That(count, Is.EqualTo(2), "Emitter did not keep spawning on its interval.");
        });

        Assert.That(entMan.Deleted(emitter), Is.False);

        await pair.CleanReturnAsync();
    }
}
