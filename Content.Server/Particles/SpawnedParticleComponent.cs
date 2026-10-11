using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Particles;

[RegisterComponent]
public sealed partial class SpawnedParticleComponent : Component
{
    public EntityUid Emmitter;
}

