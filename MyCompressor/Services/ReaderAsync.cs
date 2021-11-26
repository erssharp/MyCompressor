using MyCompressor.Logger;
using MyCompressor.Structures;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyCompressor.Services
{
    internal class ReaderAsync : IReaderAsync
    {
        private readonly ConcurrentQueue<DataBlock> dataFromFile = new();
        private readonly CancellationTokenSource cts = new();
        private readonly CancellationToken token;
        private readonly int maxCapacity;
        private readonly int blockSize;
        private readonly int flushPeriod;
        private CompressionMode mode;
        private int decompressedOffset = 8;

        public int CurBlock { get; private set; }
        public ulong BlockCount { get; private set; }

        public bool IsActive { get; private set; }

        public async Task<DataBlock> ReadNextBlock()
        {
            while (true)
            {
                lock (dataFromFile)
                {
                    if (dataFromFile.TryDequeue(out DataBlock data))
                        return data;
                }

                await Task.Delay(100);
            }
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

        public void StartReader(string filepath, CompressionMode mode)
        {
            if (IsActive) return;

            this.mode = mode;
            FileInfo fileInfo = new(filepath);
            if (mode == CompressionMode.Compress)
            {
                BlockCount = (ulong)Math.Ceiling((decimal)fileInfo.Length / blockSize);
            }
            else
            {
                using (FileStream fileStream = new(filepath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    byte[] countBytes = new byte[8];
                    fileStream.Read(countBytes, 0, 8);
                    BlockCount = BitConverter.ToUInt64(countBytes, 0);
                }
            }

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
                            if (mode == CompressionMode.Compress)
                            {
                                int offset = CurBlock * blockSize;
                                file.Seek(offset, SeekOrigin.Begin);
                                byte[] buffer = new byte[blockSize];
                                await file.ReadAsync(buffer.AsMemory(0, blockSize));
                                DataBlock data = new()
                                {
                                    Data = buffer,
                                    Offset = offset,
                                    Id = CurBlock
                                };
                                dataFromFile.Enqueue(data);
                                CurBlock++;
                            }
                            else
                            {
                                file.Seek(decompressedOffset, SeekOrigin.Begin);

                                byte[] dataLength = new byte[4];
                                file.Read(dataLength, 0, dataLength.Length);
                                decompressedOffset += dataLength.Length;

                                int compressedBlockSize = BitConverter.ToInt32(dataLength);
                                file.Seek(decompressedOffset, SeekOrigin.Begin);

                                byte[] buffer = new byte[compressedBlockSize];
                                await file.ReadAsync(buffer.AsMemory(0, compressedBlockSize));

                                DataBlock data = new()
                                {
                                    Data = buffer,
                                    Offset = decompressedOffset,
                                    Id = CurBlock
                                };

                                decompressedOffset += compressedBlockSize;
                                dataFromFile.Enqueue(data);
                                CurBlock++;
                            }
                            if (CurBlock % flushPeriod == 0) file.Flush();
                        }
                        file.Flush();
                        Thread.Sleep(50);
                        if ((ulong)CurBlock == BlockCount) break;
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
