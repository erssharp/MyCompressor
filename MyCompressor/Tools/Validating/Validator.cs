using MyCompressor.Logger;
using MyCompressor.Services;

namespace MyCompressor.Tools.Validating
{
    internal static class Validator
    {
        public static bool WorkNotFinished(IReaderAsync reader)
        {
            if (reader.CurBlock < reader.BlockCount)
            {
                MyLogger.AddMessage("Reader didn't finish reading.");
                return true;
            }
            return false;
        }

        public static bool WorkNotFinished(IMultiThreadWriter writer)
        {
            if (writer.CurBlock < writer.BlockCount)
            {
                MyLogger.AddMessage("Writer didn't finish reading.");
                return true;
            }
            return false;
        }

        public static bool HasExceptions(List<Task> tasks)
        {
            foreach (var task in tasks)
            {
                if (task.IsFaulted)
                {
                    MyLogger.AddMessage("Unhandled exception while compressing" + task.Exception.Message);
                    return true;
                }
            }

            return false;
        }
    }
}
