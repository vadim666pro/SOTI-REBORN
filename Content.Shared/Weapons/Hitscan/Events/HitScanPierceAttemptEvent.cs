using System.Numerics;
using Content.Shared.Combat.Ranged.Pierce;
using Content.Shared.Inventory;
using Content.Shared.Weapons.Reflect;

namespace Content.Shared.Weapons.Hitscan.Events;

[ByRefEvent]
public record struct HitScanPierceAttemptEvent(PierceLevel Level, bool Pierced) : IInventoryRelayEvent
{
    public SlotFlags TargetSlots => ~SlotFlags.POCKET;
}
