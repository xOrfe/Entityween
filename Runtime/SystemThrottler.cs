using Unity.Entities;

namespace XO.Entityween
{
    /// <summary>
    /// A lightweight, GC-allocation-free helper struct to throttle system updates
    /// by frame count or elapsed time. Fully compatible with both ISystem (unmanaged Burst)
    /// and SystemBase (managed).
    /// </summary>
    public struct SystemThrottler
    {
        private int _counter;
        private float _timeAccumulator;

        /// <summary>
        /// Throttles by frame count. Returns true every N frames.
        /// </summary>
        public bool ShouldUpdateFrame(int frameInterval)
        {
            if (frameInterval <= 1) return true;
            
            _counter++;
            if (_counter >= frameInterval)
            {
                _counter = 0;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Throttles by elapsed time in seconds. Returns true every N seconds.
        /// </summary>
        public bool ShouldUpdateTime(float timeInterval, float deltaTime)
        {
            if (timeInterval <= 0f) return true;

            _timeAccumulator += deltaTime;
            if (_timeAccumulator >= timeInterval)
            {
                _timeAccumulator -= timeInterval;
                if (_timeAccumulator >= timeInterval)
                {
                    _timeAccumulator = 0f;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Resets the throttler counters.
        /// </summary>
        public void Reset()
        {
            _counter = 0;
            _timeAccumulator = 0f;
        }
    }
}
