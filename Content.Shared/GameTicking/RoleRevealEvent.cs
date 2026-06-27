using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameTicking
{
    [Serializable, NetSerializable]
    public sealed class RoleRevealEvent : EntityEventArgs
    {
        public string ImagePath { get; }
        public string RoleName { get; }
        public string? AntagName { get; }
        public float DisplayTime { get; }
        public float FadeTime { get; }

        public RoleRevealEvent(string imagePath, string roleName, string? antagName, float displayTime, float fadeTime)
        {
            ImagePath = imagePath;
            RoleName = roleName;
            AntagName = antagName;
            DisplayTime = displayTime;
            FadeTime = fadeTime;
        }
    }
}
