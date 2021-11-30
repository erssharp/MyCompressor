using MyCompressor.Structures;
using System.IO.Compression;

namespace MyCompressor.Services
{
    internal interface IMultiThreadWriter
    {
        long BlockCount { get; }
        long CurBlock { get; }
        bool IsActive { get; }
        ManualResetEventSlim ResetEvent { get; }
        void StartWriter(string filepath, long blockCount);
        void WriteData(DataBlock data);      
        void FinishWork();
    }
}
