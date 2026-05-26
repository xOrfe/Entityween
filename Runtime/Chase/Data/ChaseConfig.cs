namespace XO.Entityween
{
    public struct ChaseConfig
    {
        public ChaseMode Mode;
        public float SmoothTime;
        public float MaxSpeed;
        public bool KillOnChase;

        public static ChaseConfig Default => new()
        {
            Mode = ChaseMode.SmoothStep,
            SmoothTime = 0.15f,
            MaxSpeed = float.PositiveInfinity,
            KillOnChase = false
        };
    }
}
