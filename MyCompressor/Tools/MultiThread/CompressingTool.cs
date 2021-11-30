using MyCompressor.Structures;
using System.IO.Compression;

namespace MyCompressor.Tools.MultiThread
{
    internal class CompressingTool : ICompressorTool
    {
        public DataBlock Process(DataBlock data)
        {
            using (MemoryStream memory = new())
            {
                using (MemoryStream dataStream = new(data.Data))
                using (GZipStream compressingStream = new(memory, CompressionMode.Compress))
                    dataStream.CopyTo(compressingStream);

                byte[] compressedData = memory.ToArray();

                DataBlock compressedBlock = new()
                {
                    Id = data.Id,
                    Data = compressedData,
                    OrigignalSize = data.OrigignalSize
                };

                return compressedBlock;
            }
        }
    }
}
