using System.Numerics;

namespace Content.Shared.Weapons.Hitscan.Events;

[ByRefEvent]
public record struct HitScanRicochetAttemptEvent(float Chance, Vector2 Pos, Vector2 Dir, bool Ricocheted)
{
}
