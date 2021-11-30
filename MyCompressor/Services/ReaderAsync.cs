using MyCompressor.Logger;
using MyCompressor.Structures;
using MyCompressor.Tools.MultiThread;
using System.Collections.Concurrent;
using System.Configuration;
using System.IO.Compression;

namespace MyCompressor.Services
{
    internal class ReaderAsync : MultiThreadWorker, IReaderAsync
    {
        private readonly ConcurrentQueue<DataBlock> dataFromFile = new();
        readonly IReaderTool tool;
        private long decompressedOffset = lengthOfBlockCount;
        private const int lengthOfBlockCount = 8;

        public ReaderAsync(CompressionMode mode)
        {
            ResetEvent = new ManualResetEventSlim(false);
            tool = mode == CompressionMode.Compress ? new ReaderCompressingTool() : new ReaderDecompressingTool();

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

        public void StartReader(string filepath)
        {
            if (IsActive) return;

            BlockCount = CalcBlockCount(filepath);
            pool = new SemaphoreSlim(0, (int)blockCount);
            Task.Run(() => ReadFromFile(filepath), token);
            MyLogger.AddMessage("Reader has started.");
            IsActive = true;
        }

        private long CalcBlockCount(string filepath)
        {
            FileInfo fileInfo = new(filepath);
            if (tool.Mode == CompressionMode.Compress)
            {
                return (long)Math.Ceiling((decimal)fileInfo.Length / blockSize);
            }
            else
            {
                using (FileStream fileStream = new(filepath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    byte[] countBytes = new byte[lengthOfBlockCount];
                    fileStream.Read(countBytes, 0, lengthOfBlockCount);
                    return BitConverter.ToInt64(countBytes, 0);
                }
            }
        }

        public (bool, DataBlock) ReadNextBlock()
        {
            if (pool != null)
                pool.Wait();
            else
            {
                MyLogger.AddMessage("Trying to read data before reader is started.");
                return (false, default);
            }

            if (dataFromFile.TryDequeue(out DataBlock data))
                return (true, data);

            return (false, default);
        }

        private void Stop()
        {
            MyLogger.AddMessage("Reader finished it's work.");
            IsActive = false;
            ResetEvent.Set();
        }

        private void ReadFromFile(string filepath)
        {
            CurBlock = 0;
            decompressedOffset = 8;

            while (true)
            {
                if (token.IsCancellationRequested)
                {
                    Stop();
                    return;
                }

                using (FileStream file = File.Open(filepath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    try
                    {
                        while (dataFromFile.Count <= maxCapacity && file.Position < file.Length)
                        {
                            DataBlock data = tool.Read(file, blockSize, ref decompressedOffset, ref curBlock);

                            dataFromFile.Enqueue(data);

                            pool?.Release();

                            file.Flush();
                            GC.Collect();
                        }

                        if (CurBlock == BlockCount)
                        {
                            Stop();
                            return;
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
