using MyCompressor.Logger;
using MyCompressor.Structures;
using System.Collections.Concurrent;
using System.Configuration;
using System.IO.Compression;

namespace MyCompressor.Services
{
    internal class ReaderAsync : MultiThreadWorker, IReaderAsync
    {
        private readonly ConcurrentQueue<DataBlock> dataFromFile = new();

        private long decompressedOffset = 8;
        public ReaderAsync()
        {
            if (!int.TryParse(ConfigurationManager.AppSettings["readerCapacity"], out maxCapacity))
            {
                MyLogger.AddMessage("Reader can not get max capacity from configuration. It's work was terminated.");
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
                BlockCount = (long)Math.Ceiling((decimal)fileInfo.Length / blockSize);
            }
            else
            {
                using (FileStream fileStream = new(filepath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    byte[] countBytes = new byte[8];
                    fileStream.Read(countBytes, 0, 8);
                    BlockCount = BitConverter.ToInt64(countBytes, 0);
                }
            }

            Task.Run(() => ReadFromFile(filepath), token);
            MyLogger.AddMessage("Reader has started.");
            IsActive = true;
        }

        public async Task<(bool, DataBlock)> ReadNextBlock()
        {
            while (true)
            {
                if (dataFromFile.TryDequeue(out DataBlock data))
                    return (true, data);

                if (!IsActive)
                    return (false, default);

                await Task.Delay(100);
            }
        }

        public void FinishWork()
        {
            cts.Cancel();
            IsActive = false;
        }

        private void ReadFromFile(string filepath)
        {
            CurBlock = 0;
            decompressedOffset = 8;

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
                        while (dataFromFile.Count <= maxCapacity && file.Position < file.Length)
                        {
                            if (mode == CompressionMode.Compress)
                            {
                                long offset = CurBlock * blockSize;
                                file.Seek(offset, SeekOrigin.Begin);

                                int length =
                                    file.Length - file.Position <= blockSize
                                    ? (int)(file.Length - file.Position)
                                    : blockSize;

                                byte[] buffer = new byte[length];
                                file.Read(buffer, 0, length);
                                DataBlock data = new()
                                {
                                    Data = buffer,
                                    OrigignalSize = length,
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

                                file.Seek(decompressedOffset, SeekOrigin.Begin);

                                byte[] originalSizeBytes = new byte[4];
                                file.Read(originalSizeBytes, 0, originalSizeBytes.Length);
                                decompressedOffset += originalSizeBytes.Length;

                                int compressedBlockSize = BitConverter.ToInt32(dataLength);
                                int originalSize = BitConverter.ToInt32(originalSizeBytes);
                                file.Seek(decompressedOffset, SeekOrigin.Begin);

                                byte[] buffer = new byte[compressedBlockSize];
                                file.Read(buffer, 0, compressedBlockSize);

                                DataBlock data = new()
                                {
                                    Data = buffer,
                                    OrigignalSize = originalSize,
                                    Id = CurBlock
                                };
                                decompressedOffset += compressedBlockSize;
                                dataFromFile.Enqueue(data);
                                CurBlock++;
                            }
                            file.Flush();

                            GC.Collect();
                        }

                        Thread.Sleep(50);
                        if (CurBlock == BlockCount)
                        {
                            while (!dataFromFile.IsEmpty)
                                Thread.Sleep(200);

                            MyLogger.AddMessage("Reader finished it's work.");
                            IsActive = false;
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        MyLogger.AddMessage("Exception while reading: " + ex.Message);
                        return;
                    }
                }
            }
        }
    }
}
