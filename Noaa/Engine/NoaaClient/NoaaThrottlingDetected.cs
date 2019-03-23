using System;

namespace Engine
{
    /// <summary>
    /// weather.gov limits to 10,000 calls 'per day per caller'.
    /// Throttling is indicated by HTTP 403.
    /// </summary>
    public sealed class NoaaThrottlingDetected : Exception
    {
    }
}