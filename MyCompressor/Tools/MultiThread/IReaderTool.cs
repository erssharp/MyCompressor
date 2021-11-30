using MyCompressor.Structures;
using System.IO.Compression;

namespace MyCompressor.Tools.MultiThread
{
    internal interface IReaderTool
    {
        CompressionMode Mode { get; }
        DataBlock Read(FileStream file, int blockSize, ref long offset, ref long curBlock);
    }
}
