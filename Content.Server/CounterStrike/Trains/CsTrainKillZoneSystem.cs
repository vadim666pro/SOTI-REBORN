using System.Numerics;
using Content.Server.CounterStrike.Trains;
using Content.Shared.Body.Components;
using Content.Shared.Damage;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Server.CounterStrike.Trains;

public sealed class CsTrainKillZoneSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CsTrainKillZoneComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CsTrainKillZoneComponent, StartCollideEvent>(OnStartCollide);
    }

    private void OnMapInit(Entity<CsTrainKillZoneComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<PhysicsComponent>(ent, out var physics))
            return;

        _physics.SetBodyStatus(ent, physics, BodyStatus.InAir);
        _physics.SetLinearVelocity(ent, new Vector2(ent.Comp.Speed, 0f), body: physics);
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