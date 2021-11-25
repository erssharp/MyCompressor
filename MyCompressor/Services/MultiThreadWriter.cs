using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using MyCompressor.Logger;

namespace MyCompressor.Services
{
    internal class MultiThreadWriter : IMultiThreadWriter
    {
        private readonly static ConcurrentDictionary<int, byte[]> dataToWrite = new();
        private readonly CancellationTokenSource cts = new();
        private readonly CancellationToken token;
        private readonly int flushPeriod;

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
            Task.Run(WriteToFile, token);
            MyLogger.AddMessage("Writer started it's work");
            IsActive = true;
        }

        public void FinishWork() => cts.Cancel();

        public void WriteData(int block, byte[] data)
        {
            if (!dataToWrite.TryAdd(block, data))
            {
                MyLogger.AddMessage("Attempt to add another element with same block id.");
                FinishWork();
            }
        }

        private async Task WriteToFile()
        {
            CurBlock = 0;
            string? filepath = ConfigurationManager.AppSettings["resultPath"];

            if (filepath == null)
            {
                MyLogger.AddMessage("Could not get the result path from configuration. Writer's work was terminated.");
                IsActive = false;
                return;
            }

            while (true)
            {
                if (token.IsCancellationRequested)
                {
                    MyLogger.AddMessage("Writer finished it's work.");
                    IsActive = false;
                    return;
                }

                using (FileStream file = File.Open(filepath, FileMode.Append, FileAccess.Write, FileShare.Read))
                {
                    while (dataToWrite.TryRemove(CurBlock, out byte[]? data))
                    {                      
                        if (data != null)
                            await file.WriteAsync(data);
                        else
                        {
                            MyLogger.AddMessage("Attempt to write null.");
                            IsActive = false;
                            return;
                        }

                        CurBlock++;

                        if (CurBlock % flushPeriod == 0) file.Flush();
                    }
                    file.Flush();
                    Thread.Sleep(50);
                }
            }
        }
    }
}
