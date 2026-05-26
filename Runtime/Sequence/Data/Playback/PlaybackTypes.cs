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

    public enum LoopEaseMode : byte
    {
        Mirror,
        Repeat
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
