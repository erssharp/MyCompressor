using System.IO.Compression;
using MyCompressor.Logger;
using MyCompressor.Services;
using MyCompressor.Structures;
using MyCompressor.Progress;

namespace MyCompressor.Compressors
{
    internal class GZIPCompressor
    {
        private readonly List<Task> tasks = new();

        public bool Start(string filepath, string resultFilepath, CompressionMode mode)
        {
            IMultiThreadWriter writer = new MultiThreadWriter();
            IReaderAsync reader = new ReaderAsync();
            ProgressObserver.StartObserving(reader, writer);

            reader.StartReader(filepath, mode);
            long blockCount = reader.BlockCount;
            writer.StartWriter(resultFilepath, blockCount, mode);

            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                Task task = new(() =>
                {
                    try
                    {
                        while (reader.IsActive)
                        {
                            (bool isSucceed, DataBlock data) = reader.ReadNextBlock().Result;
                            if (!isSucceed) break;

                            if (data.Data == null)
                            {
                                MyLogger.AddMessage("Attemt to compress/decompress null data.");
                                return;
                            }

                            if (mode == CompressionMode.Compress)
                            {
                                using (MemoryStream memory = new())
                                {
                                    using (MemoryStream dataStream = new(data.Data))
                                    using (GZipStream compressingStream = new(memory, mode))
                                        dataStream.CopyTo(compressingStream);

                                    byte[] compressedData = memory.ToArray();

                                    DataBlock compressedBlock = new()
                                    {
                                        Id = data.Id,
                                        Data = compressedData,
                                        OrigignalSize = data.OrigignalSize
                                    };

                                    writer.WriteData(compressedBlock).Wait();
                                }
                            }
                            else
                            {
                                using (MemoryStream memory = new(new byte[data.OrigignalSize]))
                                {
                                    using (MemoryStream dataStream = new(data.Data))
                                    using (GZipStream decompressingStream = new(dataStream, mode))
                                        decompressingStream.CopyTo(memory);

                                    byte[] decompressedData = memory.ToArray();

                                    DataBlock decompressedBlock = new()
                                    {
                                        Id = data.Id,
                                        Data = decompressedData,
                                        OrigignalSize = data.OrigignalSize
                                    };

                                    writer.WriteData(decompressedBlock).Wait();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MyLogger.AddMessage("Exception while compressing: " + ex.Message);
                    }

                    while (writer.CurBlock < writer.BlockCount)
                        Thread.Sleep(100);
                });

                tasks.Add(task);
                task.Start();
            }

            Task.WaitAll(tasks.ToArray());

            foreach (var task in tasks)
            {
                if (task.IsFaulted)
                {
                    MyLogger.AddMessage("Unhandled exception while compressing.");
                    return false;
                }
            }

            ProgressObserver.FinishObserving();

            if (reader.CurBlock < reader.BlockCount)
            {
                MyLogger.AddMessage("Reader didn't finish reading.");
                return false;
            }

            if (writer.CurBlock < writer.BlockCount)
            {
                MyLogger.AddMessage("Writer didn't finish writing.");
                return false;
            }

            if (reader.IsActive)
                reader.FinishWork();
            if (writer.IsActive)
                writer.FinishWork();

            return true;
        }
    }
}
