using MyCompressor.Structures;
using System.IO.Compression;

namespace MyCompressor.Services
{
    internal interface IReaderAsync
    {
        long BlockCount { get; }
        long CurBlock { get; }
        bool IsActive { get; }
        ManualResetEventSlim ResetEvent { get; }
        void StartReader(string filepath);
        (bool, DataBlock) ReadNextBlock();
        void FinishWork();
    }
}
