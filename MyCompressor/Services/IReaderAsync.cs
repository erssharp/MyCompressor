using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyCompressor.Services
{
    internal interface IReaderAsync
    {
        long BlockCount { get; }
        int CurBlock { get; }
        bool TryReadNextBlock(out byte[]? data);
        void FinishWork();
    }
}
