using Unity.Entities;

namespace XO.Entityween
{
    public struct PlaybackPauseRequest : IComponentData {}
    public struct PlaybackResumeRequest : IComponentData {}
    public struct PlaybackKillRequest : IComponentData {}
    public struct PlaybackCompleteRequest : IComponentData {}
    public struct PlaybackRewindRequest : IComponentData {}
}
