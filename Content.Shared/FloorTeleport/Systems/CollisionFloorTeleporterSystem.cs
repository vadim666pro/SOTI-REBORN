using System.Linq;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.FloorTeleport.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Teleportation.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.FloorTeleport.Systems;

/// <summary>
///     Handles floor teleportation when entities step on teleporter markers.
///     Uses LinkedEntityComponent for linking teleporters.
/// </summary>
public sealed class CollisionFloorTeleporterSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<CollisionFloorTeleporterComponent, StepTriggeredOffEvent>(OnStepTrigger);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<TeleportCooldownComponent>();
        while (query.MoveNext(out var uid, out var cooldown))
        {
            if (_gameTiming.CurTime.TotalSeconds >= cooldown.ExpiresAt)
            {
                RemCompDeferred(uid, cooldown);
            }
        }
    }

    private void OnStepTrigger(EntityUid uid, CollisionFloorTeleporterComponent component, StepTriggeredOffEvent args)
    {
        var subject = args.Tripper;

        // Don't teleport anchored entities
        if (Transform(subject).Anchored)
            return;

        // Check cooldown
        if (HasComp<TeleportCooldownComponent>(subject))
            return;

        // Find linked teleporter
        if (!TryComp<LinkedEntityComponent>(uid, out var link) || link.LinkedEntities.Count == 0)
            return;

        var target = link.LinkedEntities.First();

        // Break pulls before teleport
        BreakPulls(subject);

        // Unbuckle if buckled
        UnbuckleIfNeeded(subject);

        // Teleport the entity
        TeleportEntity(subject, target, uid, component);
    }

    private void BreakPulls(EntityUid subject)
    {
        if (TryComp<PullableComponent>(subject, out var pullable) && pullable.BeingPulled)
        {
            _pulling.TryStopPull(subject, pullable);
        }

        if (TryComp<PullerComponent>(subject, out var puller) && puller.Pulling.HasValue)
        {
            if (TryComp<PullableComponent>(puller.Pulling.Value, out var pulling))
            {
                _pulling.TryStopPull(puller.Pulling.Value, pulling);
            }
        }
    }

    private void UnbuckleIfNeeded(EntityUid subject)
    {
        if (TryComp<BuckleComponent>(subject, out var buckle) && buckle.Buckled)
        {
            _buckle.TryUnbuckle(subject, subject);
        }
    }

    private void TeleportEntity(EntityUid subject, EntityUid target, EntityUid source, CollisionFloorTeleporterComponent component)
    {
        var targetCoords = Transform(target).Coordinates;

        // Play sound
        if (component.TeleportSound != null)
        {
            _audio.PlayPredicted(component.TeleportSound, subject, subject);
        }

        // Teleport
        _transform.SetCoordinates(subject, targetCoords);

        // Apply cooldown
        var cooldown = EnsureComp<TeleportCooldownComponent>(subject);
        cooldown.ExpiresAt = (float)(_gameTiming.CurTime.TotalSeconds + component.CooldownTime);
        Dirty(subject, cooldown);
    }
}
