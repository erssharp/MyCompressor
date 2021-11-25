using MyCompressor.Logger;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyCompressor.Compressor
{
    internal class CompressingScheduler : TaskScheduler
    {
        [ThreadStatic]
        private static bool currentThreadIsProcessingItems;
        private readonly LinkedList<Task> tasks = new();
        private readonly int concurrencyLevel = Environment.ProcessorCount;

        private int runningTasks = 0;

        public sealed override int MaximumConcurrencyLevel => concurrencyLevel;

        public CompressingScheduler()
        {

        }

        protected override IEnumerable<Task>? GetScheduledTasks()
        {
            lock (tasks)
            {
                return tasks.ToList();
            }
        }

        protected override sealed bool TryDequeue(Task task)
        {
            lock (tasks)
            {
                return tasks.Remove(task);
            }
        }

        protected override void QueueTask(Task task)
        {
            lock (tasks)
            {
                tasks.AddLast(task);

                if (runningTasks < concurrencyLevel)
                {
                    ++runningTasks;
                    NotifyThreadPoolOfPendingWork();
                }
            }
        }

        private void NotifyThreadPoolOfPendingWork()
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    while (true)
                    {
                        Task? task;
                        lock (tasks)
                        {
                            if (tasks.Count == 0)
                            {
                                --runningTasks;
                                break;
                            }

                            if ((task = tasks.First?.Value) == null)
                            {
                                --runningTasks;
                                MyLogger.AddMessage("Task in queue had null reference.");
                                break;
                            }

                            tasks.RemoveFirst();
                        }

                        TryExecuteTask(task);
                    }
                }
                finally
                {
                    currentThreadIsProcessingItems = false;
                }
            }, null);
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            if (!currentThreadIsProcessingItems)
            {
                return false;
            }

            if (taskWasPreviouslyQueued)
            {
                TryDequeue(task);
            }

            return TryExecuteTask(task);
        }
    }
}
