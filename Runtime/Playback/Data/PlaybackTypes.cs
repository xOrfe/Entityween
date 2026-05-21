using Unity.Entities;
using XO.Curve;

namespace XO.Entityween
{
    public enum LoopType : byte
    {
        None,
        Repeat,
        PingPong,
        Random
    }

    public enum PlaybackTimeType : byte
    {
        Scaled,
        Unscaled,
        Fixed
    }

    public enum PlaybackState : byte
    {
        Stopped,
        Playing,
        Paused,
        Completed
    }
}
