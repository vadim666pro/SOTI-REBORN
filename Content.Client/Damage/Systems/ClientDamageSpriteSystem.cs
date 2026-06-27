using System.Numerics;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client.Damage.Systems;

/// <summary>
/// Client-side system that adds damage sprites when the local player takes brute damage.
/// </summary>
public sealed class ClientDamageSpriteSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private DamageGroupPrototype? _bruteGroup;

    private TimeSpan _lastPositionChangeTime = TimeSpan.Zero;
    private Vector2 _lastPosition = new Vector2(0.5f, 0.5f);
    private int _lastStateIndex = 0;

    public override void Initialize()
    {
        base.Initialize();

        _prototypeManager.TryIndex<DamageGroupPrototype>("Brute", out _bruteGroup);

        SubscribeLocalEvent<DamageSpriteSettingsComponent, ComponentInit>(OnSettingsInit);
        SubscribeLocalEvent<DamageSpriteSettingsComponent, DamageChangedEvent>(OnDamageChanged);
    }

    private void OnSettingsInit(EntityUid uid, DamageSpriteSettingsComponent component, ComponentInit args)
    {
        // nothing to do
    }

    private void OnDamageChanged(EntityUid uid, DamageSpriteSettingsComponent component, DamageChangedEvent args)
    {
        // Only add sprites for the local player
        if (_playerManager.LocalEntity != uid)
            return;

        if (args.DamageDelta == null || args.DamageDelta.Empty)
            return;

        // Calculate brute damage (Blunt + Slash + Piercing)
        var bruteDamage = FixedPoint2.Zero;
        if (_bruteGroup != null)
        {
            args.DamageDelta.TryGetDamageInGroup(_bruteGroup, out bruteDamage);
        }

        // Only show sprites if brute damage exceeds threshold (> 7)
        if (bruteDamage <= FixedPoint2.New(7))
            return;

        var spriteComp = EnsureComp<DamageSpriteComponent>(uid);

        // If a sprite is already active, do not add another — keep the existing one until it expires.
        if (spriteComp.Sprites.Count > 0)
            return;

        var sprite = new DamageSpriteData
        {
            Opacity = 1.0f,
            Scale = component.SpriteScale,
            RemainingTime = component.SpriteLifetime,
            TotalLifetime = component.SpriteLifetime
        };

        // Only pick a new position/state if 1.5s cooldown elapsed; otherwise reuse last position and state.
        var curTime = _timing.CurTime;
        const float positionCooldown = 1.5f;

        if (_lastPositionChangeTime == TimeSpan.Zero || curTime - _lastPositionChangeTime >= TimeSpan.FromSeconds(positionCooldown))
        {
            var pos = new Vector2(_random.NextFloat(), _random.NextFloat());
            sprite.Position = pos;
            _lastPosition = pos;
            _lastPositionChangeTime = curTime;
            _lastStateIndex = _random.Next(0, 3);
        }
        else
        {
            sprite.Position = _lastPosition;
        }

        sprite.StateIndex = _lastStateIndex;

        spriteComp.Sprites.Add(sprite);
        Dirty(uid, spriteComp);
    }
}
