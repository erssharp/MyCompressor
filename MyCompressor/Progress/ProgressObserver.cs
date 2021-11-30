using MyCompressor.Services;
using MyCompressor.Statistics;

namespace MyCompressor.Progress
{
    internal static class ProgressObserver
    {
        public static ManualResetEventSlim ResetEvent { get; private set; }
        private readonly static CancellationTokenSource cts = new();
        private readonly static CancellationToken token;

        static ProgressObserver()
        {
            ResetEvent = new ManualResetEventSlim(false);
            token = cts.Token;
        }

        public static void FinishObserving() => cts.Cancel();

        public static Task StartObserving(IReaderAsync reader, IMultiThreadWriter writer)
        {
            return Task.Run(() =>
            {
                while (true)
                {
                    (float cpuLoad, float discLoad) = UsageStatistics.CollectStats();
                    Console.Clear();
                    Console.Out.WriteLine($"Reader Progress: {reader.CurBlock}/{reader.BlockCount}");
                    Console.Out.WriteLine($"Writer Progress: {writer.CurBlock}/{writer.BlockCount}");
                    Console.Out.WriteLine($"CPU Load: {cpuLoad} %");
                    Console.Out.WriteLine($"Disc Drive Load: {discLoad} %");
                    if (token.IsCancellationRequested) return;
                    Thread.Sleep(200);
                }
            }, token).ContinueWith(ShowFinishingStatistics);
        }

        private static void ShowFinishingStatistics(Task _)
        {
            (double avgCpuLoad, double avgDiscLoad) = UsageStatistics.GetAvgStats();
            UsageStatistics.ResetStats();
            Console.WriteLine(new string('-', 80));
            Console.WriteLine("Summary:");
            Console.WriteLine($"Average CPU Load: {avgCpuLoad} %");
            Console.WriteLine($"Average Disc Drive Load: {avgDiscLoad} %");
            Console.WriteLine(new string('-', 80));
            ResetEvent.Set();
        }
    }
}
