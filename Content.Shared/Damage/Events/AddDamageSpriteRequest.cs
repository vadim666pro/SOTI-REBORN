using System;
using Robust.Shared.Serialization;
using Robust.Shared.GameObjects;

namespace Content.Shared.Damage.Events
{
    [Serializable, NetSerializable]
    public sealed class AddDamageSpriteRequest : EntityEventArgs
    {
        public readonly Robust.Shared.GameObjects.NetEntity NetTarget;
        public readonly float Scale;
        public readonly int LifetimeMs;

        public AddDamageSpriteRequest(Robust.Shared.GameObjects.NetEntity netTarget, float scale, int lifetimeMs)
        {
            NetTarget = netTarget;
            Scale = scale;
            LifetimeMs = lifetimeMs;
        }
    }
}
