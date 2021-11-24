using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace MyCompressor.Logger
{
    internal static class MyLogger
    {
        private static readonly ConcurrentBag<string> messages = new();
        public static void AddMessage(string message, [CallerMemberName] string callerName = "") => messages.Add($"[{callerName}] " + message);
        public static void ShowLog()
        {
            foreach(string msg in messages)
                Console.WriteLine(msg);
        }
    }
}
