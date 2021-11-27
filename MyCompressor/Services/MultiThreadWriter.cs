using System.Collections.Concurrent;
using System.Configuration;
using MyCompressor.Logger;
using System.IO.Compression;
using MyCompressor.Structures;

namespace MyCompressor.Services
{
    internal class MultiThreadWriter : MultiThreadWorker, IMultiThreadWriter
    {
        private readonly static ConcurrentDictionary<long, DataBlock> dataToWrite = new();

        public MultiThreadWriter()
        {
            if (!int.TryParse(ConfigurationManager.AppSettings["writerCapacity"], out maxCapacity))
            {
                MyLogger.AddMessage("Writer can not get max capacity from configuration. It's work was terminated.");
                return;
            }

            token = cts.Token;
        }

        public void StartWriter(string filepath, long blockCount, CompressionMode mode)
        {
            if (IsActive) return;
            this.mode = mode;
            BlockCount = blockCount;
            MyLogger.AddMessage("Writer started it's work");
            IsActive = true;
            Task.Run(() => WriteToFile(filepath), token);
        }

        public async Task WriteData(DataBlock data)
        {
            if (!IsActive) return;

            while (dataToWrite.Count > maxCapacity)
                await Task.Delay(50);

            if (data.Data != null)
            {
                if (!dataToWrite.TryAdd(data.Id, data))
                {
                    MyLogger.AddMessage("Attempt to add another element with same block id.");
                    Abort();
                }
            }
            else
            {
                MyLogger.AddMessage("Attempt to write null.");
                Abort();
            }
        }

        public async Task FinishWork()
        {
            while (true)
                if (!dataToWrite.IsEmpty)
                    await Task.Delay(100);
                else break;

            Abort();
        }

        public void Abort()
        {
            cts.Cancel();
            IsActive = false;
        }

        private void WriteToFile(string filepath)
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

                if (CurBlock == BlockCount && dataToWrite.IsEmpty)
                {
                    MyLogger.AddMessage("Writer finished it's work.");
                    IsActive = false;
                    return;
                }

                using (FileStream file = File.Open(filepath, FileMode.Append, FileAccess.Write, FileShare.Read))
                {
                    try
                    {
                        while (dataToWrite.TryRemove(CurBlock, out DataBlock data))
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
                                byte[] originalSize = BitConverter.GetBytes(data.OrigignalSize);
                                file.Write(originalSize);
                            }

                            if (data.Data != null)
                                file.Write(data.Data);
                            else
                            {
                                MyLogger.AddMessage("Attempt to write null.");
                                IsActive = false;
                                return;
                            }
                            CurBlock++;

                            file.Flush();
                            GC.Collect();
                        }
                    }
                    catch (Exception ex)
                    {
                        MyLogger.AddMessage("Exception while writing: " + ex.Message);
                        return;
                    }

                    Thread.Sleep(50);
                }
            }
        }
    }
}
