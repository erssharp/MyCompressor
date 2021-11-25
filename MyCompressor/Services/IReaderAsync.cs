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
        bool TryReadNextBlock(out (int, byte[]) data, int tryCounter);
        void FinishWork();
    }
}
