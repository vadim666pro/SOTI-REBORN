using Content.Shared.Particles;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;

namespace Content.Client.Particles;

/// <summary>
/// Applies the fade-in/fade-out alpha to the sprite of every <see cref="ParticleComponent"/>
/// entity each frame. The alpha is derived from absolute game time, so it stays correct even
/// for entities that enter PVS late.
/// </summary>
public sealed class ClientParticleSystem : SharedParticleSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<ParticleComponent, SpriteComponent>();

        while (query.MoveNext(out var uid, out var particle, out var sprite))
        {
            var color = sprite.Color.WithAlpha(GetAlpha(particle, curTime));

            // Nothing changed since the last frame.
            if (sprite.Color == color)
                continue;

            _sprite.SetColor((uid, sprite), color);
        }
    }
}
