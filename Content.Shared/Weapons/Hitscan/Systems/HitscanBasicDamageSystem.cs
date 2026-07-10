using Content.Shared.Damage;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Events;
using Robust.Shared.GameObjects;

namespace Content.Shared.Weapons.Hitscan.Systems;

public sealed class HitscanBasicDamageSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HitscanBasicDamageComponent, HitscanRaycastFiredEvent>(OnHitscanHit);
    }

    private void OnHitscanHit(Entity<HitscanBasicDamageComponent> hitscan, ref HitscanRaycastFiredEvent args)
    {
        if (args.Data.HitEntity == null)
            return;

        var target = args.Data.HitEntity.Value;
        var damage = hitscan.Comp.Damage;

        _damageable.TryChangeDamage(target, damage, origin: args.Data.Gun);

        var ev = new HitscanDamageDealtEvent
        {
            Target = target,
            DamageDealt = damage,
        };
        RaiseLocalEvent(hitscan, ref ev);
    }
}
