using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using MyCompressor.Logger;
using System.IO.Compression;
using MyCompressor.Structures;

namespace MyCompressor.Services
{
    internal class MultiThreadWriter : IMultiThreadWriter
    {
        private readonly static ConcurrentDictionary<int, byte[]> dataToWrite = new();
        private readonly CancellationTokenSource cts = new();
        private readonly CancellationToken token;
        private readonly int flushPeriod;
        private CompressionMode mode;

        public ulong BlockCount { get; private set; }
        public int CurBlock { get; private set; }

        public bool IsActive { get; private set; }

        public MultiThreadWriter()
        {
            if (!int.TryParse(ConfigurationManager.AppSettings["flushPeriodWriter"], out flushPeriod))
            {
                MyLogger.AddMessage("Writer can not get flush period from configuration. It's work was terminated.");
                return;
            }

            if (flushPeriod < 1 || flushPeriod > 256)
            {
                MyLogger.AddMessage("Writer: Flush period should be in range [1;256]. It's work was terminated.");
                return;
            }

            token = cts.Token;
        }

        public async Task FinishWork()
        {
            while (true)
                if (!dataToWrite.IsEmpty)
                    await Task.Delay(100);
                else break;

            cts.Cancel();
            IsActive = false;
        }

        public void WriteData(DataBlock data)
        {
            if (!IsActive) return;

            if (data.Data != null)
            {
                if (!dataToWrite.TryAdd(data.Id, data.Data))
                {
                    MyLogger.AddMessage("Attempt to add another element with same block id.");
                    FinishWork();
                }
            }
            else
            {
                MyLogger.AddMessage("Attempt to write null.");
                FinishWork();
            }
        }

        public void StartWriter(string filepath, ulong blockCount, CompressionMode mode)
        {
            if (IsActive) return;
            this.mode = mode;
            BlockCount = blockCount;
            Task.Run(() => WriteToFile(filepath), token);
            MyLogger.AddMessage("Writer started it's work");
            IsActive = true;
        }

        private async Task WriteToFile(string filepath)
        {
            CurBlock = 0;

            while (true)
            {
                if (token.IsCancellationRequested)
                {
                    MyLogger.AddMessage("Writer finished it's work.");
                    IsActive = false;
                    return;
                }

                if (dataToWrite.IsEmpty) continue;

                using (FileStream file = File.Open(filepath, FileMode.Append, FileAccess.Write, FileShare.Read))
                {
                    while (dataToWrite.TryRemove(CurBlock, out byte[]? data))
                    {
                        if (mode == CompressionMode.Compress)
                        {
                            if (CurBlock == 0)
                            {
                                byte[] length = BitConverter.GetBytes(BlockCount);
                                file.Write(length, 0, 8);
                            }

                            byte[] dataLength = BitConverter.GetBytes(data.Length);
                            file.Write(dataLength);
                        }

                        if (data != null)
                            await file.WriteAsync(data);
                        else
                        {
                            MyLogger.AddMessage("Attempt to write null.");
                            IsActive = false;
                            return;
                        }

                        CurBlock++;

                        file.Flush();
                    }
                    file.Flush();
                    Thread.Sleep(50);
                }
            }
        }
    }
}
