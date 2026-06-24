using System.Numerics;
using Content.Shared.Damage.Components;
using Robust.Shared.Random;

namespace Content.Shared.Damage.Systems;

/// <summary>
/// System that manages damage sprites on entities.
/// </summary>
public sealed class DamageSpriteSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    /// <summary>
    /// Adds a damage sprite to the entity.
    /// </summary>
    public void AddDamageSprite(EntityUid uid, TimeSpan lifetime, float scale = 1.0f)
    {
        var comp = EnsureComp<DamageSpriteComponent>(uid);

        // If a sprite is already active, do not add another — keep the existing one until it expires.
        if (comp.Sprites.Count > 0)
            return;

        var sprite = new DamageSpriteData
        {
            Opacity = 1.0f,
            Scale = scale,
            RemainingTime = lifetime,
            TotalLifetime = lifetime
        };

        // Determine sprite position: only pick a new random position if the cooldown has elapsed (3.5s).
        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        const long cooldownMs = 1500;

        if (comp.LastPositionChangeMs == 0 || nowMs - comp.LastPositionChangeMs >= cooldownMs)
        {
            // choose a new random position and record the time
            var pos = new Vector2(_random.NextFloat(), _random.NextFloat());
            sprite.Position = pos;
            comp.LastPosition = pos;
            comp.LastPositionChangeMs = nowMs;
        }
        else
        {
            // reuse last position so it doesn't jump around
            sprite.Position = comp.LastPosition == default ? new Vector2(0.5f, 0.5f) : comp.LastPosition;
        }

        // Pick a stable state index (0..2) so clients render the same frame each update.
        sprite.StateIndex = _random.Next(0, 3);

        comp.Sprites.Add(sprite);
        Dirty(uid, comp);
    }

    /// <summary>
    /// Updates all damage sprites, reducing their remaining time and opacity.
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DamageSpriteComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            var deltaTime = TimeSpan.FromSeconds(frameTime);
            var modified = false;

            for (var i = comp.Sprites.Count - 1; i >= 0; i--)
            {
                var sprite = comp.Sprites[i];
                sprite.RemainingTime -= deltaTime;

                if (sprite.RemainingTime <= TimeSpan.Zero)
                {
                    comp.Sprites.RemoveAt(i);
                    modified = true;
                    continue;
                }

                // Calculate opacity based on remaining time
                sprite.Opacity = (float)(sprite.RemainingTime.TotalSeconds / sprite.TotalLifetime.TotalSeconds);
                comp.Sprites[i] = sprite;
                modified = true;
            }

            if (modified)
                Dirty(uid, comp);

            // Remove component if no sprites left
            if (comp.Sprites.Count == 0)
                RemCompDeferred<DamageSpriteComponent>(uid);
        }
    }
}
