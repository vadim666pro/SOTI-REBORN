using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Events;
using Content.Shared.Combat.Ranged.Pierce;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Random;
using System.Numerics;

namespace Content.Shared.Combat.Ranged;

public sealed partial class HitscanRicochetSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _rand = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<HitscanReflectComponent> _reflectQuery;

    public override void Initialize()
    {
        _reflectQuery = GetEntityQuery<HitscanReflectComponent>();

        SubscribeLocalEvent<HitscanRicochetComponent, AttemptHitscanRaycastFiredEvent>(OnHitscanHit);
        SubscribeLocalEvent<RicochetableComponent, HitScanRicochetAttemptEvent>(OnRicochetPierce);
        base.Initialize();
    }

    private void OnHitscanHit(Entity<HitscanRicochetComponent> hitscan, ref AttemptHitscanRaycastFiredEvent args)
    {
        var data = args.Data;

        if (hitscan.Comp.Chance <= 0 || data.HitEntity == null)
            return;

        // If we're at our maximum recursion depth, don't try to pierce
        if (!_reflectQuery.TryComp(hitscan.Owner, out var reflect) || reflect.CurrentReflections > reflect.MaxReflections)
            return;

        var ev = new HitScanRicochetAttemptEvent(hitscan.Comp.Chance, Vector2.Zero, data.ShotDirection, false);
        RaiseLocalEvent(data.HitEntity.Value, ref ev);

        if (!ev.Ricocheted)
            return;

        reflect.CurrentReflections++;

        args.Cancelled = true;

        var fromEffect = Transform(data.HitEntity.Value).Coordinates;

        var hitFiredEvent = new HitscanTraceEvent
        {
            FromCoordinates = fromEffect,
            ToCoordinates = fromEffect.Offset(ev.Dir),
            ShotDirection = ev.Dir,
            Gun = data.Gun,
            Shooter = data.HitEntity.Value,
        };

        RaiseLocalEvent(hitscan, ref hitFiredEvent);
    }

    private void OnRicochetPierce(Entity<RicochetableComponent> ent, ref HitScanRicochetAttemptEvent args)
    {
        var chance = Math.Clamp(args.Chance * ent.Comp.Chance, 0f, 1f);
        if (chance == 0) return;

        // Simplified ricochet logic - bounce randomly within a reasonable angle
        var spreadDegrees = _rand.NextFloat(-45f, 45f);
        var spreadAngle = Angle.FromDegrees(spreadDegrees);
        var reflectedDir = spreadAngle.RotateVec(args.Dir).Normalized();

        args.Dir = reflectedDir;
        args.Ricocheted = true;
    }
}
