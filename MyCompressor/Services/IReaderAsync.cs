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
        void StartReader(string filepath);
        bool TryReadNextBlock(out (int, byte[]) data, int tryCounter = 1);
        void FinishWork();
    }
}
