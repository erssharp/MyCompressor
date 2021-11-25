using MyCompressor.Logger;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyCompressor.Services
{
    internal class ReaderAsync : IReaderAsync
    {
        private readonly ConcurrentQueue<(int, byte[])> dataFromFile = new();
        private readonly CancellationTokenSource cts = new();
        private readonly CancellationToken token;
        private readonly int maxCapacity;
        private readonly int blockSize;
        private readonly int flushPeriod;

        public int CurBlock { get; private set; }
        public long BlockCount { get; private set; }

        public bool IsActive { get; private set; }

        public bool TryReadNextBlock(out (int, byte[]) data, int tryCounter = 1)
        {
            if (dataFromFile.TryDequeue(out data))            
                return true;

            MyLogger.AddMessage($"Failed attemt to read next block. #{tryCounter}");
            Thread.Sleep(100);

            if (tryCounter == 10)
                return false;
            else return TryReadNextBlock(out data, ++tryCounter);
        }

        public void FinishWork() => cts.Cancel();

        public ReaderAsync()
        {
            if (!int.TryParse(ConfigurationManager.AppSettings["readerCapacity"], out maxCapacity))
            {
                MyLogger.AddMessage("Reader can not get max capacity from configuration. It's work was terminated.");
                return;
            }

            if (!int.TryParse(ConfigurationManager.AppSettings["flushPeriodReader"], out flushPeriod))
            {
                MyLogger.AddMessage("Reader can not get flush period from configuration. It's work was terminated.");
                return;
            }

            if (flushPeriod < 1 || flushPeriod > 256)
            {
                MyLogger.AddMessage("Reader: Flush period should be in range [1;256]. It's work was terminated.");
                return;
            }

            if (!int.TryParse(ConfigurationManager.AppSettings["blockSize"], out blockSize))
            {
                MyLogger.AddMessage("Reader can not get mblock size from configuration. It's work was terminated.");
                return;
            }

            token = cts.Token;

            Task.Run(ReadFromFile, token);
        }

        private async Task ReadFromFile()
        {
            CurBlock = 0;
            string? filepath = ConfigurationManager.AppSettings["filePath"];

            if (filepath == null)
            {
                MyLogger.AddMessage("Could not get the file path from configuration. Reader's work was terminated.");
                IsActive = false;
                return;
            }

            while (true)
            {
                if (token.IsCancellationRequested)
                {
                    MyLogger.AddMessage("Reader finished it's work.");
                    IsActive = false;
                    return;
                }

                FileInfo fileInfo = new(filepath);
                BlockCount = (long)Math.Floor((decimal)fileInfo.Length / blockSize);

                using (FileStream file = File.Open(filepath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    while (dataFromFile.Count <= maxCapacity)
                    {
                        byte[] buffer = new byte[blockSize];
                        await file.ReadAsync(buffer.AsMemory(CurBlock * blockSize, blockSize), token);
                        CurBlock++;
                        dataFromFile.Enqueue((CurBlock, buffer));
                        if (CurBlock % flushPeriod == 0) file.Flush();
                    }
                    file.Flush();
                    Thread.Sleep(50);
                }
            }
        }
    }
}
