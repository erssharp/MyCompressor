using MyCompressor.Structures;
using System.IO.Compression;

namespace MyCompressor.Services
{
    internal interface IReaderAsync
    {
        long BlockCount { get; }
        long CurBlock { get; }
        bool IsActive { get; }
        void StartReader(string filepath, CompressionMode mode);
        Task<(bool, DataBlock)> ReadNextBlock();
        void FinishWork();
    }
}
