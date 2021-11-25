using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyCompressor.Services
{
    internal interface IMultiThreadWriter
    {
        int CurBlock { get; }
        void StartWriter(string filepath);
        void WriteData(int block, byte[] data);
        void FinishWork();
    }
}
