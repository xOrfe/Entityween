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
        public static void CalculateProgress(
            ref float elapsedTime,
            float duration,
            ref PlaybackProgress progress,
            out float normalizedTime,
            out bool isFinished)
        {
            isFinished = false;
            if (duration <= 0f)
            {
                normalizedTime = progress.Direction >= 0 ? 1f : 0f;
                isFinished = true;
                return;
            }

            int dir = progress.Direction == 0 ? 1 : progress.Direction;

            if (progress.LoopType == LoopType.None)
            {
                isFinished = dir >= 0 ? (elapsedTime >= duration) : (elapsedTime <= 0f);
                if (isFinished)
                {
                    elapsedTime = dir >= 0 ? duration : 0f;
                }
            }
            else
            {
                int loopDelta = (int)math.floor(elapsedTime / duration);
                if (loopDelta != 0)
                {
                    if (dir >= 0)
                    {
                        int maxDelta = progress.LoopCount > 0 ? (int)progress.LoopCount - progress.LoopIndex : loopDelta;
                        bool exceeded = loopDelta > maxDelta;
                        loopDelta = exceeded ? maxDelta : loopDelta;
                        elapsedTime = exceeded ? duration : (elapsedTime - loopDelta * duration);
                        isFinished = exceeded;
                    }
                    else
                    {
                        int minDelta = progress.LoopCount > 0 ? -progress.LoopIndex : loopDelta;
                        bool exceeded = loopDelta < minDelta;
                        loopDelta = exceeded ? minDelta : loopDelta;
                        elapsedTime = exceeded ? 0f : (elapsedTime - loopDelta * duration);
                        isFinished = exceeded;
                    }
                    progress.LoopIndex += loopDelta;
                }
            }

            float t = elapsedTime / duration;
            if (progress.LoopType == LoopType.PingPong && (progress.LoopIndex % 2 == 1))
            {
                t = 1f - t;
            }

            normalizedTime = math.clamp(t, 0f, 1f);
        }
    }
}
