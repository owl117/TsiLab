using System;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace Engine
{
    public static class Logger
    {
        private static TelemetryClient TelemetryClient = null;

        public static void InitializeTelemetryClient(string applicationInsightsInstrumentationKey)
        {
            TelemetryClient = new TelemetryClient() { InstrumentationKey = applicationInsightsInstrumentationKey };
        }

        public static void TraceLine(string format, params object[] args)
        {
            string message = String.Format(format, args);

            if (TelemetryClient != null)
            {
                TelemetryClient.TrackTrace(message);
            }

            Console.WriteLine(message);
        }

        public static void TraceException(string component, Exception e)
        {
            if (TelemetryClient != null)
            {
                var exceptionTelemetry = new ExceptionTelemetry(e);
                exceptionTelemetry.Properties["Component"] = component;
                TelemetryClient.TrackException(exceptionTelemetry);
            }

            Console.WriteLine($"Component: {component} {e}");
        }

        public static void Flush()
        {
            if (TelemetryClient != null)
            {
                TelemetryClient.Flush();
            }
        }
    }
}