using System;

namespace Engine
{
    public static class Utils
    {
        public static TimeSpan Max(TimeSpan x, TimeSpan y)
        {
            return x > y ? x : y;
        }
    }
}