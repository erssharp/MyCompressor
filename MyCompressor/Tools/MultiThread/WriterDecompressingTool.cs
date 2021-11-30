using MyCompressor.Structures;
using System.IO.Compression;

namespace MyCompressor.Tools.MultiThread
{
    internal class WriterDecompressingTool : IWriterTool
    {
        public CompressionMode Mode => CompressionMode.Decompress;
        public void Write(FileStream file, DataBlock data, long _, ref long curBlock)
        {
            file.Write(data.Data);

            curBlock++;
        }
    }
}
