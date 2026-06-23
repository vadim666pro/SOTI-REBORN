using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using System;

namespace Content.Shared.Damage.Components
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class DamageSpriteSettingsComponent : Component
    {
        [DataField]
        public TimeSpan SpriteLifetime = TimeSpan.FromSeconds(0.5);

        [DataField]
        public float SpriteScale = 0.5f;
    }
}
