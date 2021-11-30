using MyCompressor.Structures;
using System.IO.Compression;

namespace MyCompressor.Tools.MultiThread
{
    internal class DecompressingTool : ICompressorTool
    {
        public DataBlock Process(DataBlock data)
        {
            using (MemoryStream memory = new(new byte[data.OrigignalSize]))
            {
                using (MemoryStream dataStream = new(data.Data))
                using (GZipStream decompressingStream = new(dataStream, CompressionMode.Decompress))
                    decompressingStream.CopyTo(memory);

                byte[] decompressedData = memory.ToArray();

                DataBlock decompressedBlock = new()
                {
                    Id = data.Id,
                    Data = decompressedData,
                    OrigignalSize = data.OrigignalSize
                };

                return decompressedBlock;
            }
        }
    }
}
