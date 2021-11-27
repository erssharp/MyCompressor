using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace MyCompressor.Statistics
{
    internal static class UsageStatistics
    {
        private static readonly PerformanceCounter cpuCounter = new("Processor", "% Processor Time", "_Total");
        private static readonly PerformanceCounter discCounter = new("PhysicalDisk", "% Disk Time", "_Total");
        private static readonly PerformanceCounter ramCounter = new("Memory", "Available MBytes", null);
        private static double cpuUsage = 0;
        private static double discUsage = 0;
        private static float maxRamUsed = 0;
        private static long countCpuStats = 0;
        private static long countDiscStats = 0;

        static UsageStatistics()
        {

        }

        public static (float, float, float) CollectStats()
        {
            float cpuLoad = cpuCounter.NextValue();
            float discLoad = discCounter.NextValue();
            float ramUsed = ramCounter.NextValue();

            cpuUsage += cpuLoad;
            discUsage += discLoad;

            countCpuStats++;
            countDiscStats++;

            if (ramUsed > maxRamUsed) maxRamUsed = ramUsed;

            return (cpuLoad, discLoad, ramUsed);
        }

        public static (double, double, float) GetAvgStats()
        {
            double avgCpuLoad = cpuUsage / countCpuStats;
            double avgDiscLoad = discUsage / countDiscStats;
            return (avgCpuLoad, avgDiscLoad, maxRamUsed);
        }

        public static void ResetStats()
        {
            cpuUsage = 0;
            discUsage = 0;
            countDiscStats = 0;
            countCpuStats = 0;
            maxRamUsed = 0;
        }
    }
}
