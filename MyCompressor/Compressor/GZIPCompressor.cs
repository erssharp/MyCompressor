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
            IMultiThreadWriter? writer = ServicesHost.ServiceProvider.GetService(typeof(IMultiThreadWriter)) as IMultiThreadWriter;
            IReaderAsync? reader = ServicesHost.ServiceProvider.GetService(typeof(IReaderAsync)) as IReaderAsync;
            CompressingScheduler scheduler = new();

            if (reader != null)
                reader.StartReader(filepath);
            else
            {
                MyLogger.AddMessage("Can't get reader service.");
                return false;
            }

            if (writer != null)
                writer.StartWriter(resultFilepath);
            else
            {
                MyLogger.AddMessage("Can't get writer service.");
                return false;
            }

            long blockCount = reader.BlockCount;

            for (long i = 0; i < blockCount; i++)
            {
                Task task = new(() =>
                {
                    if (!reader.TryReadNextBlock(out (int, byte[]) data))
                    {
                        MyLogger.AddMessage("Can't get next block.");
                        return;
                    }

                    if (mode == CompressionMode.Compress)
                    {
                        using MemoryStream memory = new();
                        using GZipStream compressingStream = new(memory, mode);
                        using MemoryStream dataStream = new(data.Item2);

                        dataStream.CopyTo(compressingStream);

                        byte[] compressedData = memory.ToArray();

                        writer.WriteData(data.Item1, compressedData);
                    }
                    else
                    {
                        using MemoryStream dataStream = new(data.Item2);                      
                        using GZipStream decompressingStream = new(dataStream, mode);
                        using MemoryStream memory = new();

                        decompressingStream.CopyTo(memory);

                        byte[] decompressedData = memory.ToArray();

                        writer.WriteData(data.Item1, decompressedData);
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
