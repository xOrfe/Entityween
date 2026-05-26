namespace XO.Entityween
{
    public struct LoopConfig
    {
        public LoopType Type;
        public uint Count;
        public LoopEaseMode EaseMode;

        public static LoopConfig Default => new()
        {
            Type = LoopType.None,
            Count = 1,
            EaseMode = LoopEaseMode.Mirror
        };
    }
}
