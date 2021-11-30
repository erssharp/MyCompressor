using System.IO.Compression;

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

        protected long curBlock = 0;

        protected SemaphoreSlim? pool;
        public ManualResetEventSlim ResetEvent { get; protected set; }

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

        public void FinishWork()
        {
            cts.Cancel();
            IsActive = false;
        }
    }
}
