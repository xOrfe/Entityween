using Unity.Burst;
using Unity.Mathematics;

namespace XO.Entityween
{
    [BurstCompile]
    internal static class PlaybackUtilities
    {
        /// <summary>
        /// Retrieves the correct delta time based on PlaybackTimeType.
        /// </summary>
        [BurstCompile]
        public static float GetDeltaTime(PlaybackTimeType timeType, float deltaTime, float unscaledDeltaTime)
        {
            return timeType == PlaybackTimeType.Unscaled ? unscaledDeltaTime : deltaTime;
        }

        /// <summary>
        /// Updates playback elapsed time and handles complex loop boundaries, PingPong reversing, and completion.
        /// Returns true if the playback has fully finished.
        /// </summary>
        [BurstCompile]
        public static bool CalculateProgress(
            ref float elapsedTime,
            float duration,
            ref PlaybackLoop loop,
            bool hasLoop,
            out float normalizedTime)
        {
            if (elapsedTime < 0f) elapsedTime = 0f;

            if (duration <= 0f)
            {
                normalizedTime = 1f;
                return true;
            }

            float t = elapsedTime / duration;
            bool isFinished = t >= 1f;

            if (hasLoop && loop.LoopType != LoopType.None)
            {
                if (isFinished)
                {
                    int completedLoops = math.max(1, (int)math.floor(t));

                    if (loop.LoopCount == 0)
                    {
                        loop.LoopIndex += completedLoops;
                        elapsedTime -= completedLoops * duration;
                        t = elapsedTime / duration;
                        isFinished = false;
                    }
                    else
                    {
                        int remainingLoops = math.max(0, (int)loop.LoopCount - loop.LoopIndex);
                        if (completedLoops <= remainingLoops)
                        {
                            loop.LoopIndex += completedLoops;
                            elapsedTime -= completedLoops * duration;
                            t = elapsedTime / duration;
                            isFinished = false;
                        }
                        else
                        {
                            loop.LoopIndex += remainingLoops;
                            elapsedTime = duration;
                            t = 1f;
                        }
                    }
                }

                if (loop.LoopType == LoopType.PingPong)
                {
                    if (loop.LoopIndex % 2 == 1)
                    {
                        t = 1f - t;
                    }
                }
            }

            normalizedTime = math.clamp(t, 0f, 1f);
            return isFinished;
        }
    }
}
