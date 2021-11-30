using MyCompressor.Structures;
using System.IO.Compression;

namespace MyCompressor.Tools.MultiThread
{
    internal class ReaderCompressingTool : IReaderTool
    {
        public CompressionMode Mode => CompressionMode.Compress;
        public DataBlock Read(FileStream file, int blockSize, ref long _, ref long curBlock)
        {
            long offset = curBlock * blockSize;
            file.Seek(offset, SeekOrigin.Begin);

            int length =
                file.Length - file.Position <= blockSize
                ? (int)(file.Length - file.Position)
                : blockSize;

            byte[] buffer = new byte[length];
            file.Read(buffer, 0, length);
            DataBlock data = new()
            {
                Data = buffer,
                OrigignalSize = length,
                Id = curBlock
            };
           
            curBlock++;
            return data;
        }
    }
}
