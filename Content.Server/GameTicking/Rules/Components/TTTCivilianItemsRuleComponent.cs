using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Game rule component for TTT mode that gives civilian items to all players
/// except the Assassin and Sheriff (Quartermaster).
/// </summary>
[RegisterComponent]
public sealed partial class TTTCivilianItemsRuleComponent : Component
{
    /// <summary>
    /// List of civilian item prototypes that can be given to players.
    /// One random item from this list will be given to each eligible player.
    /// </summary>
    [DataField("civilianItems", required: true)]
    public List<EntProtoId> CivilianItems = new();
}
