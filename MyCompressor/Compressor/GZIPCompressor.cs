using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using MyCompressor.Logger;
using MyCompressor.Services;
using MyCompressor.Compressor;
using MyCompressor.Structures;

namespace MyCompressor.Compressors
{
    internal class GZIPCompressor : IDisposable
    {

        readonly object block = new();
        private readonly List<Task> tasks = new();

        public GZIPCompressor()
        {
        }

        public void Dispose()
        {

        }

        public bool Compress(string filepath, string resultFilepath, CompressionMode mode)
        {
            IMultiThreadWriter writer = new MultiThreadWriter();
            IReaderAsync reader = new ReaderAsync();
            CompressingScheduler scheduler = new();

            reader.StartReader(filepath, mode);
            ulong blockCount = reader.BlockCount;
            writer.StartWriter(resultFilepath, blockCount, mode);
            object blocker = new object();

            for (ulong i = 0; i < blockCount; i++)
            {
                Task task = new(() =>
                {
                    DataBlock data;

                    if (!reader.TryReadNextBlock(out data))
                    {
                        MyLogger.AddMessage("Can't get next block.");
                        return;
                    }

                    if (data.Data == null)
                    {
                        MyLogger.AddMessage("Attemt to compress/decompress null data.");
                        return;
                    }

                    if (mode == CompressionMode.Compress)
                    {
                        Console.WriteLine("START " + data.Length);
                        using MemoryStream memory = new();
                        using GZipStream compressingStream = new(memory, mode);
                        using MemoryStream dataStream = new(data.Data);

                        dataStream.CopyTo(compressingStream);

                        byte[] compressedData = memory.ToArray();
                        Console.WriteLine("END " + compressedData.Length);
                        DataBlock compressedBlock = new()
                        {
                            Id = data.Id,
                            Data = compressedData
                        };

                        writer.WriteData(compressedBlock);
                    }
                    else
                    {
                        Console.WriteLine($"CM: {data.Length}");
                        using MemoryStream dataStream = new(data.Data);
                        using GZipStream decompressingStream = new(dataStream, mode);
                        using MemoryStream memory = new();

                        decompressingStream.CopyTo(memory);

                        byte[] decompressedData = memory.ToArray();
                        Console.WriteLine($"DCM: {decompressedData.Length}");

                        DataBlock decompressedBlock = new()
                        {
                            Id = data.Id,
                            Data = decompressedData
                        };

                        writer.WriteData(decompressedBlock);
                    }
                });

                tasks.Add(task);
                task.Start(scheduler);
            }

            Task.WaitAll(tasks.ToArray());

            return true;
        }
    }
}
