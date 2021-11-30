using MyCompressor.Structures;
using System.IO.Compression;

namespace MyCompressor.Tools.MultiThread
{
    internal class WriterCompressingTool : IWriterTool
    {
        public CompressionMode Mode => CompressionMode.Compress;
        public void Write(FileStream file, DataBlock data, long blockCount, ref long curBlock)
        {
            if (curBlock == 0)
            {
                byte[] length = BitConverter.GetBytes(blockCount);
                file.Write(length, 0, 8);
            }

            byte[] dataLength = BitConverter.GetBytes(data.Length);
            file.Write(dataLength);
            byte[] originalSize = BitConverter.GetBytes(data.OrigignalSize);
            file.Write(originalSize);

            file.Write(data.Data);

            curBlock++;
        }
    }
}
