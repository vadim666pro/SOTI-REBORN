using Robust.Shared.Prototypes;

namespace Content.Server.CounterStrike.Trains;

[RegisterComponent]
public sealed partial class CsTrainSpawnerComponent : Component
{
    [DataField]
    public EntProtoId Prototype = "CsTrainSpeedingKillZone";

    [DataField]
    public float Interval = 180f;

    public TimeSpan NextSpawn;
}
