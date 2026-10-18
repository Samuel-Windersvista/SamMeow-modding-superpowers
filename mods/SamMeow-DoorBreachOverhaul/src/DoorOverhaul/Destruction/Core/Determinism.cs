namespace DoorOverhaul.Destruction.Core
{
    public static class StableHash
    {
        public static uint Fnv1a(string text)
        {
            uint hash = 2166136261u;
            if (text != null)
            {
                foreach (char c in text)
                {
                    hash ^= c;
                    hash *= 16777619u;
                }
            }

            return hash;
        }
    }

    public sealed class Pcg32
    {
        private const ulong Multiplier = 6364136223846793005UL;
        private const ulong Increment = 1442695040888963407UL;

        private ulong _state;

        public Pcg32(ulong seed)
        {
            _state = 0UL;
            NextUInt();
            _state += seed;
            NextUInt();
        }

        public uint NextUInt()
        {
            ulong old = _state;
            _state = unchecked(old * Multiplier + Increment);
            uint xorShifted = (uint)(((old >> 18) ^ old) >> 27);
            int rotation = (int)(old >> 59);
            return (xorShifted >> rotation) | (xorShifted << ((-rotation) & 31));
        }

        public float NextFloat()
        {
            return (NextUInt() >> 8) * (1f / 16777216f);
        }
    }
}
