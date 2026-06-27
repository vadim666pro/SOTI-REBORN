using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.CounterStrike.Events;

[Serializable, NetSerializable]
public sealed partial class CsBombPlantDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class CsBombDefuseDoAfterEvent : SimpleDoAfterEvent;
