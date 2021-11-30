using System.Collections.Concurrent;
using System.Configuration;
using MyCompressor.Logger;
using System.IO.Compression;
using MyCompressor.Structures;
using MyCompressor.Tools.MultiThread;

namespace MyCompressor.Services
{
    internal class MultiThreadWriter : MultiThreadWorker, IMultiThreadWriter
    {
        private readonly static ConcurrentDictionary<long, DataBlock> dataToWrite = new();
        private readonly IWriterTool tool;

        public MultiThreadWriter(CompressionMode mode)
        {
            tool = mode == CompressionMode.Compress ? new WriterCompressingTool() : new WriterDecompressingTool();
            ResetEvent = new ManualResetEventSlim(false);
            
            if (!int.TryParse(ConfigurationManager.AppSettings["writerCapacity"], out maxCapacity))
            {
                MyLogger.AddMessage("Writer can not get max capacity from configuration. It's work was terminated.");
                return;
            }           

            pool = new SemaphoreSlim(0, maxCapacity);

            token = cts.Token;
        }

        public void StartWriter(string filepath, long blockCount)
        {
            if (IsActive) return;
            BlockCount = blockCount;
            MyLogger.AddMessage("Writer started it's work");
            IsActive = true;
            Task.Run(() => WriteToFile(filepath), token);
        }

        public void WriteData(DataBlock data)
        {
            dataToWrite.TryAdd(data.Id, data);
            pool?.Release();
        }

        private void Stop()
        {
            MyLogger.AddMessage("Writer finished it's work.");
            IsActive = false;
            ResetEvent.Set();
        }

        private void WriteToFile(string filepath)
        {
            CurBlock = 0;

            while (true)
            {
                if (token.IsCancellationRequested || (CurBlock == BlockCount && dataToWrite.IsEmpty))
                {
                    Stop();
                    return;
                }

                using (FileStream file = File.Open(filepath, FileMode.Append, FileAccess.Write, FileShare.Read))
                {
                    try
                    {
                        while (dataToWrite.TryRemove(CurBlock, out DataBlock data))
                        {                        
                            tool.Write(file, data, BlockCount, ref curBlock);
                            file.Flush();
                            GC.Collect();

                            pool?.Wait();
                        }
                    }
                    catch (Exception ex)
                    {
                        MyLogger.AddMessage("Exception while writing: " + ex.Message);
                        return;
                    }
                }
            }
        }
    }
}
