using System.Numerics;
using Content.Server.CounterStrike.Trains;
using Content.Shared.Body.Components;
using Content.Shared.Damage;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Server.CounterStrike.Trains;

public sealed class CsTrainKillZoneSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CsTrainKillZoneComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CsTrainKillZoneComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<CsTrainSpawnerComponent, MapInitEvent>(OnSpawnerMapInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CsTrainSpawnerComponent>();
        while (query.MoveNext(out var uid, out var spawner))
        {
            if (_timing.CurTime < spawner.NextSpawn)
                continue;

            SpawnTrain(uid, spawner);
            spawner.NextSpawn = _timing.CurTime + TimeSpan.FromSeconds(spawner.Interval);
        }
    }

    private void OnSpawnerMapInit(Entity<CsTrainSpawnerComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextSpawn = _timing.CurTime + TimeSpan.FromSeconds(ent.Comp.Interval);
    }

    private void SpawnTrain(EntityUid spawner, CsTrainSpawnerComponent component)
    {
        var coordinates = _transform.GetMapCoordinates(spawner);
        var rotation = _transform.GetWorldRotation(spawner);
        Spawn(component.Prototype, coordinates, rotation: rotation);
    }

    private void OnMapInit(Entity<CsTrainKillZoneComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<PhysicsComponent>(ent, out var physics))
            return;

        _physics.SetBodyStatus(ent, physics, BodyStatus.InAir);
        var direction = _transform.GetWorldRotation(ent).ToWorldVec();
        _physics.SetLinearVelocity(ent, direction * ent.Comp.Speed, body: physics);
    }

    private void OnStartCollide(Entity<CsTrainKillZoneComponent> ent, ref StartCollideEvent args)
    {
        if (!HasComp<BodyComponent>(args.OtherEntity) ||
            !HasComp<DamageableComponent>(args.OtherEntity) ||
            !ent.Comp.HitEntities.Add(args.OtherEntity))
        {
            return;
        }

        _damageable.TryChangeDamage(args.OtherEntity, ent.Comp.Damage, ignoreResistances: true, origin: ent);
    }
}