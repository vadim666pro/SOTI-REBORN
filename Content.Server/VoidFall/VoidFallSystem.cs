using System.Numerics;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.VoidFall;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.VoidFall;

/// <summary>
/// Detects players standing on space tiles (out of bounds) and applies void fall.
/// After FallDuration seconds the entity is deleted.
/// </summary>
public sealed class VoidFallSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;

    private const float CheckInterval = 0.1f;
    private float _accumulator;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _accumulator += frameTime;
        if (_accumulator < CheckInterval)
            return;
        _accumulator = 0f;

        var query = EntityQueryEnumerator<VoidFallComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var fall, out var xform))
        {
            fall.FallTime += CheckInterval;
            Dirty(uid, fall);

            if (fall.FallTime >= fall.FallDuration)
            {
                // Animation finished — delete the entity
                QueueDel(uid);
            }
        }

        // Check for new players entering the void
        var mobQuery = EntityQueryEnumerator<MobStateComponent, TransformComponent>();
        while (mobQuery.MoveNext(out var uid, out var mobState, out var xform))
        {
            if (!_mobState.IsAlive(uid, mobState))
                continue;

            if (HasComp<VoidFallComponent>(uid))
                continue;

            if (IsInVoid(xform))
            {
                var fall = EnsureComp<VoidFallComponent>(uid);
                fall.OriginalScale = Vector2.One; // will be overridden by client
                Dirty(uid, fall);
            }
        }
    }

    private bool IsInVoid(TransformComponent xform)
    {
        if (xform.GridUid == null)
            return true;

        if (TryComp<MapGridComponent>(xform.GridUid, out var grid))
        {
            var tileRef = _mapSystem.GetTileRef(xform.GridUid.Value, grid, xform.Coordinates);
            if (tileRef.Tile.IsEmpty)
                return true;
        }

        return false;
    }
}
