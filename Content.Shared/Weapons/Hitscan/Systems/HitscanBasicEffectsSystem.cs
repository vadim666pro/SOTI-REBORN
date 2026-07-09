using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Events;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Hitscan.Systems;

public sealed class HitscanBasicEffectsSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HitscanBasicEffectsComponent, HitscanRaycastFiredEvent>(OnHitscanHit);
    }

    private void OnHitscanHit(Entity<HitscanBasicEffectsComponent> hitscan, ref HitscanRaycastFiredEvent args)
    {
        if (args.Data.HitEntity == null)
            return;

        var target = args.Data.HitEntity.Value;

        // Play hit sound from the hitscan component
        var sound = hitscan.Comp.Sound;
        if (sound != null)
        {
            _audio.PlayPvs(sound, target);
        }

        // Visual hit indicator
        if (hitscan.Comp.HitColor != null)
        {
            var color = hitscan.Comp.HitColor.Value;
            var netTarget = GetNetEntity(target);
            RaiseNetworkEvent(new PlayHitIndicatorEvent(netTarget, color), Filter.Pvs(target));
        }
    }
}

[Serializable, NetSerializable]
public sealed class PlayHitIndicatorEvent : EntityEventArgs
{
    public NetEntity Target;
    public Color Color;

    public PlayHitIndicatorEvent(NetEntity target, Color color)
    {
        Target = target;
        Color = color;
    }
}
