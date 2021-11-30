using MyCompressor.Structures;
using System.IO.Compression;

namespace MyCompressor.Tools.MultiThread
{
    internal class ReaderDecompressingTool : IReaderTool
    {
        public CompressionMode Mode => CompressionMode.Decompress;
        public DataBlock Read(FileStream file, int _, ref long offset, ref long curBlock)
        {
            file.Seek(offset, SeekOrigin.Begin);

            byte[] dataLength = new byte[4];
            file.Read(dataLength, 0, dataLength.Length);
            offset += dataLength.Length;

            file.Seek(offset, SeekOrigin.Begin);

            byte[] originalSizeBytes = new byte[4];
            file.Read(originalSizeBytes, 0, originalSizeBytes.Length);
            offset += originalSizeBytes.Length;

            int compressedBlockSize = BitConverter.ToInt32(dataLength);
            int originalSize = BitConverter.ToInt32(originalSizeBytes);
            file.Seek(offset, SeekOrigin.Begin);

            byte[] buffer = new byte[compressedBlockSize];
            file.Read(buffer, 0, compressedBlockSize);

            DataBlock data = new()
            {
                Data = buffer,
                OrigignalSize = originalSize,
                Id = curBlock
            };
            offset += compressedBlockSize;

            curBlock++;

            return data;
        }
    }
}
