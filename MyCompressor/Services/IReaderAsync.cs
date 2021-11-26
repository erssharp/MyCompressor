﻿using MyCompressor.Structures;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyCompressor.Services
{
    internal interface IReaderAsync
    {
        ulong BlockCount { get; }
        int CurBlock { get; }
        void StartReader(string filepath, CompressionMode mode);
        bool TryReadNextBlock(out DataBlock data, int tryCounter = 1);
        void FinishWork();
    }
}
