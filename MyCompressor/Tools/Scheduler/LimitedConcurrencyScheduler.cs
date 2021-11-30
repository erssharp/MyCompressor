namespace MyCompressor.Tools.Scheduler
{
    internal class LimitedConcurrencyScheduler : TaskScheduler
    {
        [ThreadStatic]
        private static bool currentThreadIsProcessingItems;
        private readonly LinkedList<Task> tasks = new();
        private readonly int concurrencyLevel = Environment.ProcessorCount;

        private int runningTasks = 0;

        public sealed override int MaximumConcurrencyLevel => concurrencyLevel;

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