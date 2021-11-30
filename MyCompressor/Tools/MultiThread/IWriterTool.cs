using MyCompressor.Structures;
using System.IO.Compression;

namespace MyCompressor.Tools.MultiThread
{
    internal interface IWriterTool
    {
        CompressionMode Mode { get; }
        void Write(FileStream file, DataBlock data, long blockCount, ref long curBlock);
    }
}
