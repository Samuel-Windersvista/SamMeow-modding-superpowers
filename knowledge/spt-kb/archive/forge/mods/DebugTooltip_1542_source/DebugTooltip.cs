namespace DebugTooltip
{
    public static class DebugTooltip
    {
        private static DebugInfo NextDebugInfo = null;

        public static void SetDebugInfo(DebugInfo debugInfo)
        {
            NextDebugInfo = debugInfo;
        }

        public static void SetDebugInfo(string debugInfo)
        {
            NextDebugInfo = new BasicDebugInfo(debugInfo);
        }

        public static DebugInfo GetDebugInfo()
        {
            return NextDebugInfo;
        }

        public static void Clear()
        {
            NextDebugInfo = null;
        }

        private class BasicDebugInfo(string text) : DebugInfo
        {
            private readonly string text = text;

            public override string ToShortString()
            {
                return text;
            }

            public override string ToString()
            {
                return text;
            }
        }
    }
}
