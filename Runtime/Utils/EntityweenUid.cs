using UnityEngine;

namespace XO.Entityween.Utils
{
    public static class EntityweenUid
    {
        private static uint _tweenUid;
        private static uint _sequenceUid;
        private static uint _chunkUid;
        
        public static uint GetTweenUid()
        {
            if (_tweenUid == uint.MaxValue)
            {
                Debug.LogError("Tween Uid reached max capacity");
                _tweenUid = 0;
            }
            return _tweenUid++;
        }
        public static uint GetSequenceUid()
        {
            if (_sequenceUid == uint.MaxValue)
            {
                Debug.LogError("Sequence Uid reached max capacity");
                _tweenUid = 0;
            }
            return _sequenceUid++;
        }
        public static uint GetChunkUid()
        {
            if (_chunkUid == uint.MaxValue)
            {
                Debug.LogError("Chunk Uid reached max capacity");
                _tweenUid = 0;
            }
            return _chunkUid++;
        }
    }
}