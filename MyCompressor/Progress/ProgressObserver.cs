using MyCompressor.Services;
using MyCompressor.Statistics;

namespace MyCompressor.Progress
{
    internal static class ProgressObserver
    {
        private readonly static CancellationTokenSource cts = new();
        private readonly static CancellationToken token;

        static ProgressObserver()
        {
            token = cts.Token;
        }

        public static void FinishObserving() => cts.Cancel();

        public static Task StartObserving(IReaderAsync reader, IMultiThreadWriter writer)
        {
            return Task.Run(() =>
            {
                while (true)
                {
                    (float cpuLoad, float discLoad, float ramUsed) = UsageStatistics.CollectStats();
                    Console.Clear();
                    Console.Out.WriteLine($"Reader Progress: {reader.CurBlock}/{reader.BlockCount}");
                    Console.Out.WriteLine($"Writer Progress: {writer.CurBlock}/{writer.BlockCount}");
                    Console.Out.WriteLine($"RAM Used: {ramUsed} MB");
                    Console.Out.WriteLine($"CPU Load: {cpuLoad} %");
                    Console.Out.WriteLine($"Disc Drive Load: {discLoad} %");
                    if (token.IsCancellationRequested) return;
                    Thread.Sleep(100);
                }
            }, token).ContinueWith(ShowFinishingStatistics);
        }

        private static void ShowFinishingStatistics(Task _)
        {
            (double avgCpuLoad, double avgDiscLoad, float maxRamUsed) = UsageStatistics.GetAvgStats();
            UsageStatistics.ResetStats();
            Console.WriteLine(new string('-', 80));
            Console.WriteLine("Average usage statistics:");
            Console.WriteLine($"Average CPU Load: {avgCpuLoad} %");
            Console.WriteLine($"Average Disc Drive Load: {avgDiscLoad} %");
            Console.WriteLine($"Max RAM Used: {maxRamUsed} MB");
            Console.WriteLine(new string('-', 80));
        }
    }
}
