using System.Numerics;
using Content.Shared.CombatMode;
using Content.Shared.Ghost;
using Content.Shared.Mobs.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Client.Fov.Systems;

/// <summary>
/// Lightweight FOV system. Hides mob sprites behind the player (180° half-sphere).
/// In Harm Mode, the FOV cone follows the cursor direction.
/// </summary>
public sealed class SimpleFovSystem : EntitySystem
{
    private const float MaxVisibleDistance = 15f;

    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;

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

        Vector2 playerForward;

        // In Harm Mode, use cursor direction; otherwise use character direction
        if (_combatMode.IsInCombatMode(playerUid.Value))
        {
            var mouseScreenPos = _inputManager.MouseScreenPosition;

            // If cursor is outside the game window, fall back to character direction
            if (mouseScreenPos.Window == WindowId.Invalid)
            {
                var baseAngle = (float)_transform.GetWorldRotation(playerXform).Theta - MathF.PI * 0.5f;
                playerForward = new Vector2(MathF.Cos(baseAngle), MathF.Sin(baseAngle));
            }
            else
            {
                var mouseWorldPos = _eyeManager.PixelToMap(mouseScreenPos);
                var delta = mouseWorldPos.Position - playerPos;

                if (delta.LengthSquared() < 0.001f)
                {
                    var baseAngle = (float)_transform.GetWorldRotation(playerXform).Theta - MathF.PI * 0.5f;
                    playerForward = new Vector2(MathF.Cos(baseAngle), MathF.Sin(baseAngle));
                }
                else
                {
                    playerForward = delta.Normalized();
                }
            }
        }
        else
        {
            // Use same angle source as overlay: Transform.WorldRotation with -90° correction
            var baseAngle = (float)_transform.GetWorldRotation(playerXform).Theta - MathF.PI * 0.5f;
            playerForward = new Vector2(MathF.Cos(baseAngle), MathF.Sin(baseAngle));
        }

        var processed = new HashSet<EntityUid>();

        var query = EntityQueryEnumerator<SpriteComponent, MobStateComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var sprite, out _, out var xform))
        {
            if (uid == playerUid.Value)
                continue;

            if (_entMan.TryGetComponent<GhostComponent>(uid, out _))
                continue;

            var targetPos = _transform.GetWorldPosition(xform);
            var entityDelta = targetPos - playerPos;
            var distance = entityDelta.Length();

            // Too far — hide
            if (distance > MaxVisibleDistance || distance < 0.01f)
            {
                HideSprite(uid, sprite);
                processed.Add(uid);
                continue;
            }

            // 180° check: dot >= 0 means in front半 sphere, dot < 0 means behind
            var dot = Vector2.Dot(playerForward, entityDelta / distance);

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
