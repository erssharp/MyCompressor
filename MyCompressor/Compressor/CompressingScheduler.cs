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
        private readonly ConcurrentQueue<Task> tasks = new();

        public CompressingScheduler()
        {

        }

        protected override IEnumerable<Task>? GetScheduledTasks()
        {
            return tasks.ToList();
        }

        protected override void QueueTask(Task task)
        {
            tasks.Enqueue(task);
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            throw new NotImplementedException();
        }
    }
}
