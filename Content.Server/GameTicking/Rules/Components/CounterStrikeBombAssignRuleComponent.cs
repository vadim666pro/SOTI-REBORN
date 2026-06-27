using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Gives the Counter-Strike bomb to a random Terrorist when the rule starts.
/// </summary>
[RegisterComponent]
public sealed partial class CounterStrikeBombAssignRuleComponent : Component
{
    [DataField]
    public EntProtoId BombPrototype = "CsBomb";
}
