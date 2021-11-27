using System.Diagnostics;

namespace MyCompressor.Statistics
{
    internal static class UsageStatistics
    {
        private static readonly PerformanceCounter cpuCounter = new("Processor", "% Processor Time", "_Total");
        private static readonly PerformanceCounter discCounter = new("PhysicalDisk", "% Disk Time", "_Total");
        private static double cpuUsage = 0;
        private static double discUsage = 0;
        private static long countCpuStats = 0;
        private static long countDiscStats = 0;

        static UsageStatistics()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT) //First calling always returns 0
            {
                cpuCounter.NextValue();
                discCounter.NextValue();
            }
        }

        public static (float, float) CollectStats()
        {
            float cpuLoad = 0;
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                cpuLoad = cpuCounter.NextValue();

            float discLoad = 0;
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                discLoad = discCounter.NextValue();

            cpuUsage += cpuLoad;
            discUsage += discLoad;

            countCpuStats++;
            countDiscStats++;

            return (cpuLoad, discLoad);
        }

        public static (double, double) GetAvgStats()
        {
            double avgCpuLoad = cpuUsage / countCpuStats;
            double avgDiscLoad = discUsage / countDiscStats;
            return (avgCpuLoad, avgDiscLoad);
        }

        public static void ResetStats()
        {
            cpuUsage = 0;
            discUsage = 0;
            countDiscStats = 0;
            countCpuStats = 0;
        }
    }
}
