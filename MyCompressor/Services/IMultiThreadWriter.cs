using MyCompressor.Structures;
using System.IO.Compression;

namespace MyCompressor.Services
{
    internal interface IMultiThreadWriter
    {
        long BlockCount { get; }
        long CurBlock { get; }
        bool IsActive { get; }
        void StartWriter(string filepath, long blockCount, CompressionMode mode);
        Task WriteData(DataBlock data);
        void Abort();
        Task FinishWork();
    }
}
