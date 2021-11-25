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

            if (tryCounter == 100)
                return false;

            else return TryReadNextBlock(out data, ++tryCounter);
        }

        public void FinishWork()
        {
            cts.Cancel();
            IsActive = false;
        }

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
        }

        public void StartReader(string filepath)
        {
            if (IsActive) return;

            FileInfo fileInfo = new(filepath);
            BlockCount = (long)Math.Ceiling((decimal)fileInfo.Length / blockSize);

            Task.Run(() => ReadFromFile(filepath), token);
            MyLogger.AddMessage("Reader has started.");
            IsActive = true;
        }
        
        private async Task ReadFromFile(string filepath)
        {
            CurBlock = 0;         

            while (true)
            {
                if (token.IsCancellationRequested)
                {
                    MyLogger.AddMessage("Reader finished it's work.");
                    IsActive = false;
                    return;
                }
                
                using (FileStream file = File.Open(filepath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    try
                    {
                        while (dataFromFile.Count <= maxCapacity)
                        {
                            int offset = CurBlock * blockSize;
                            file.Seek(offset, SeekOrigin.Begin);
                            byte[] buffer = new byte[blockSize];                           
                            await file.ReadAsync(buffer.AsMemory(0, blockSize));                            
                            dataFromFile.Enqueue((CurBlock, buffer));
                            CurBlock++;
                            if (CurBlock % flushPeriod == 0) file.Flush();
                        }
                        file.Flush();
                        Thread.Sleep(50);
                        if (CurBlock == BlockCount) break;
                    }
                    catch (Exception ex)
                    {
                        MyLogger.AddMessage(ex.Message);
                        break;
                    }
                }
            }
        }
    }
}
