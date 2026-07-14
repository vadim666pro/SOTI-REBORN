using System.Numerics;
using Content.Shared.Ghost;
using Content.Shared.Mobs.Components;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Client.Fov.Systems;

/// <summary>
/// Lightweight FOV system. Hides mob sprites behind the player (180° half-sphere).
/// Uses pure dot-product math — no raycasts, no physics queries.
/// </summary>
public sealed class SimpleFovSystem : EntitySystem
{
    private const float MaxVisibleDistance = 15f;

    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    /// <summary>Toggle sprite hiding on/off. Set by togglefov command.</summary>
    public bool Enabled { get; set; } = true;

    private readonly HashSet<EntityUid> _hiddenEntities = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!Enabled)
        {
            RestoreAll();
            return;
        }

        var playerUid = _player.LocalEntity;
        if (playerUid == null)
        {
            RestoreAll();
            return;
        }

        if (_entMan.TryGetComponent<GhostComponent>(playerUid.Value, out _))
        {
            RestoreAll();
            return;
        }

        if (!_entMan.TryGetComponent<TransformComponent>(playerUid.Value, out var playerXform))
            return;

        var playerPos = _transform.GetWorldPosition(playerXform);

        // Use same angle source as overlay: Transform.WorldRotation with -90° correction
        var baseAngle = (float)_transform.GetWorldRotation(playerXform).Theta - MathF.PI * 0.5f;
        var playerForward = new Vector2(MathF.Cos(baseAngle), MathF.Sin(baseAngle));

        var processed = new HashSet<EntityUid>();

        var query = EntityQueryEnumerator<SpriteComponent, MobStateComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var sprite, out _, out var xform))
        {
            if (uid == playerUid.Value)
                continue;

            if (_entMan.TryGetComponent<GhostComponent>(uid, out _))
                continue;

            var targetPos = _transform.GetWorldPosition(xform);
            var delta = targetPos - playerPos;
            var distance = delta.Length();

            // Too far — hide
            if (distance > MaxVisibleDistance || distance < 0.01f)
            {
                HideSprite(uid, sprite);
                processed.Add(uid);
                continue;
            }

            // 180° check: dot >= 0 means in front半 sphere, dot < 0 means behind
            var dot = Vector2.Dot(playerForward, delta / distance);

            if (dot >= 0f)
                RestoreSprite(uid, sprite);
            else
                HideSprite(uid, sprite);

            processed.Add(uid);
        }

        // Restore entities that left the query
        var toRestore = new List<EntityUid>();
        foreach (var uid in _hiddenEntities)
        {
            if (!processed.Contains(uid))
                toRestore.Add(uid);
        }
        foreach (var uid in toRestore)
        {
            _hiddenEntities.Remove(uid);
            if (_entMan.TryGetComponent<SpriteComponent>(uid, out var spr) && !spr.Visible)
                spr.Visible = true;
        }
    }

    private void HideSprite(EntityUid uid, SpriteComponent sprite)
    {
        if (sprite.Visible)
        {
            sprite.Visible = false;
            _hiddenEntities.Add(uid);
        }
    }

    private void RestoreSprite(EntityUid uid, SpriteComponent sprite)
    {
        if (_hiddenEntities.Remove(uid) && !sprite.Visible)
            sprite.Visible = true;
    }

    /// <summary>Restore all hidden sprites. Called externally when FOV is disabled.</summary>
    public void RestoreAll()
    {
        foreach (var uid in _hiddenEntities)
        {
            if (_entMan.TryGetComponent<SpriteComponent>(uid, out var sprite) && !sprite.Visible)
                sprite.Visible = true;
        }
        _hiddenEntities.Clear();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        RestoreAll();
    }
}
