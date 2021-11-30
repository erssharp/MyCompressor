using System.IO.Compression;
using MyCompressor.Logger;
using MyCompressor.Services;
using MyCompressor.Structures;
using MyCompressor.Progress;
using MyCompressor.Tools.Scheduler;
using MyCompressor.Tools.Validating;
using MyCompressor.Tools.MultiThread;

namespace MyCompressor.Compressors
{
    internal class GZIPCompressor
    {
        private readonly List<Task> tasks = new();
        private readonly ICompressorTool tool;
        private readonly IMultiThreadWriter writer;
        private readonly IReaderAsync reader;

        public GZIPCompressor(CompressionMode mode)
        {
            writer = new MultiThreadWriter(mode);
            reader = new ReaderAsync(mode);
            tool = mode == CompressionMode.Compress ? new CompressingTool() : new DecompressingTool();
        }

        public bool Start(string filepath, string resultFilepath)
        {
            ProgressObserver.StartObserving(reader, writer);
            reader.StartReader(filepath);
            long blockCount = reader.BlockCount;
            writer.StartWriter(resultFilepath, blockCount);
            LimitedConcurrencyScheduler scheduler = new();

            for (int i = 0; i < blockCount; i++)
            {
                Task task = new(() => ProcessBlock(reader, writer));
                tasks.Add(task);
                task.Start(scheduler);
            }

            reader.ResetEvent.Wait();         
            Task.WaitAll(tasks.ToArray());
            writer.ResetEvent.Wait();

            ProgressObserver.FinishObserving();
            ProgressObserver.ResetEvent.Wait();

            if (Validator.HasExceptions(tasks)) return false;
            if (Validator.WorkNotFinished(reader)) return false;
            if (Validator.WorkNotFinished(writer)) return false;

            return true;
        }

        private void ProcessBlock(IReaderAsync reader, IMultiThreadWriter writer)
        {
            try
            {
                (bool isSucceed, DataBlock data) = reader.ReadNextBlock();
                if (!isSucceed) throw new NullReferenceException("Error while reading");

                DataBlock result = tool.Process(data);
                writer.WriteData(result);
            }
            catch (Exception ex)
            {
                MyLogger.AddMessage("Exception while compressing: " + ex.Message);
            }
        }
    }
}
