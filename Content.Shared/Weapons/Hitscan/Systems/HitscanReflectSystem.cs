using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Events;
using Content.Shared.Weapons.Reflect;
using Robust.Shared.GameObjects;
using Robust.Shared.Random;

namespace Content.Shared.Weapons.Hitscan.Systems;

public sealed class HitscanReflectSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HitscanReflectComponent, AttemptHitscanRaycastFiredEvent>(OnReflectAttempt);
    }

    private void OnReflectAttempt(Entity<HitscanReflectComponent> hitscan, ref AttemptHitscanRaycastFiredEvent args)
    {
        if (args.Data.HitEntity == null)
            return;

        var target = args.Data.HitEntity.Value;

        // Check if the target can reflect this hitscan
        if (!TryComp<ReflectComponent>(target, out var reflect))
            return;

        // Check if the reflection type matches
        if ((reflect.Reflects & hitscan.Comp.ReflectiveType) == 0)
            return;

        // Check if we can reflect more
        if (hitscan.Comp.CurrentReflections >= hitscan.Comp.MaxReflections)
            return;

        // Check probability
        if (!_random.Prob(reflect.ReflectProb))
            return;

        // Reflection successful
        hitscan.Comp.CurrentReflections++;
        args.Cancelled = true;

        // TODO: Implement actual reflection logic (spawn new hitscan event with reflected direction)
    }
}
