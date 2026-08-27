using Content.Shared.VoidFall;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;

namespace Content.Client.VoidFall;

/// <summary>
/// Plays shrink animation when a player falls into the void.
/// Sprite scales from OriginalScale to Vector2.Zero over FallDuration.
/// </summary>
public sealed class VoidFallVisualsSystem : EntitySystem
{
    [Dependency] private readonly AnimationPlayerSystem _anim = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private const string AnimKey = "void_fall";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VoidFallComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<VoidFallComponent, ComponentRemove>(OnComponentRemove);
    }

    private void OnComponentInit(EntityUid uid, VoidFallComponent component, ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) || TerminatingOrDeleted(uid))
            return;

        // Capture current scale
        component.OriginalScale = sprite.Scale;

        if (!TryComp<AnimationPlayerComponent>(uid, out var player))
            return;

        if (_anim.HasRunningAnimation(player, AnimKey))
            return;

        _anim.Play((uid, player), GetShrinkAnimation(component), AnimKey);
    }

    private void OnComponentRemove(EntityUid uid, VoidFallComponent component, ComponentRemove args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        // Restore scale if component removed early (player returned to ground)
        _sprite.SetScale((uid, sprite), component.OriginalScale);

        if (!TryComp<AnimationPlayerComponent>(uid, out var player))
            return;

        if (_anim.HasRunningAnimation(player, AnimKey))
            _anim.Stop((uid, player), AnimKey);
    }

    private Animation GetShrinkAnimation(VoidFallComponent component)
    {
        return new Animation
        {
            Length = TimeSpan.FromSeconds(component.FallDuration),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Scale),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(component.OriginalScale, 0f),
                        new AnimationTrackProperty.KeyFrame(component.AnimationScale, component.FallDuration),
                    },
                    InterpolationMode = AnimationInterpolationMode.Linear
                }
            }
        };
    }
}
