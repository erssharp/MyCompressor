using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using MyCompressor.Exceptions;

namespace MyCompressor.Compressors
{
    internal class GZIPCompressor: IDisposable
    {
        readonly int kernelCount;
        readonly int blockSize;
        readonly object block = new();
        MemoryStream output;
        public GZIPCompressor(int blockSize = 1024)
        {
            kernelCount = Environment.ProcessorCount;
            this.blockSize = blockSize;
            output = new MemoryStream();
        }

        public void Dispose()
        {
            output.Dispose();
        }

        public (bool, Exception) Compress(string filepath, string resultFilepath)
        {
            FileStream input = File.OpenRead(filepath);

            File.Create(resultFilepath);
            FileStream output = File.OpenWrite(resultFilepath);

            int i = 0;

            CancellationTokenSource cts = new();

            ParallelOptions options = new();
            options.MaxDegreeOfParallelism = kernelCount;
            options.CancellationToken = cts.Token;

            Parallel.For(i, input.Length, options, (j, state) =>
            {
                byte[] buffer = new byte[blockSize];
                int bytesRead = 0;

                lock (block)
                {
                    bytesRead = input.Read(buffer, (int)(blockSize * j), blockSize);
                }

                using (MemoryStream memory = new(buffer))
                {
                    using (GZipStream gzip = new(memory, CompressionLevel.Optimal))
                    {
                        while (j != GetLastFinishedIteration() + 1)
                            Thread.Sleep(10);

                        lock (block)
                        {
                            gzip.CopyTo(output, blockSize);
                            output.
                        }
                    }
                }

                
            });

            //input.
            throw new Exception();
        }
    }
}
