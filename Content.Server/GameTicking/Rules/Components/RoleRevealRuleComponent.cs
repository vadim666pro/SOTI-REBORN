using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Content.Shared.GameTicking.Components;

namespace Content.Server.GameTicking.Rules.Components
{
    [RegisterComponent]
    public sealed partial class RoleRevealRuleComponent : Component
    {
        [DataField("image")] public string Image = "/Textures/Objects/counterstrike/startroundscreen/cs.png";
        [DataField("displayTime")] public float DisplayTime = 4f;
        [DataField("fadeTime")] public float FadeTime = 3f;
    }
}
