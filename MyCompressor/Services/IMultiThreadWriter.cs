using MyCompressor.Structures;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyCompressor.Services
{
    internal interface IMultiThreadWriter
    {
        int CurBlock { get; }
        void StartWriter(string filepath, ulong blockCount, CompressionMode mode);
        void WriteData(DataBlock data);
        Task FinishWork();
    }
}
