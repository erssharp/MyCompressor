using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyCompressor.Services
{
    internal abstract class MultiThreadWorker
    {
        protected readonly CancellationTokenSource cts = new();
        protected readonly object blockCnt = new();
        protected readonly object blockAct = new();
        protected readonly object blockCur = new();

        protected CancellationToken token;
        protected int maxCapacity;
        protected int blockSize;
        protected int flushPeriod;
        protected CompressionMode mode;

        protected long curBlock = 0;
        public long CurBlock
        {
            get
            {
                lock (blockCur)
                {
                    return curBlock;
                }
            }
            protected set
            {
                lock (blockCur)
                {
                    curBlock = value;
                }
            }
        }

        protected long blockCount = 0;
        public long BlockCount
        {
            get
            {
                lock (blockCnt)
                {
                    return blockCount;
                }
            }
            protected set
            {
                lock (blockCnt)
                {
                    blockCount = value;
                }
            }
        }

        protected bool isActive;
        public bool IsActive
        {
            get
            {
                lock (blockAct)
                {
                    return isActive;
                }
            }
            protected set
            {
                lock (blockAct)
                {
                    isActive = value;
                }

            }
        }
    }
}
