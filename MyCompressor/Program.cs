using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyCompressor.Compressors;
using MyCompressor.Services;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Configuration;
using MyCompressor.Logger;

namespace MyCompressor
{
    public class Program
    {
        readonly static HashSet<string> availableCommands = new() { };
        public static void Main(string[] args)
        {
            int result = -1;
            try
            {
                //CheckArguments(args, out string filepath, out string command, out string resultFilepath);
                string filepath = "tsetup-x64.3.2.5.exe";
                string resultFilepath = @"C:\Users\ernestteregulov\source\repos\MyCompressor\MyCompressor\bin\Debug\net6.0\telegram.gz";
                string command = "compress";

                ServicesHost.StartHost();

                result = StartCompression(filepath, resultFilepath) ? 1 : 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unhandled Exception: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
                result = 0;
            }

            MyLogger.ShowLog();
            Console.WriteLine(result);

            Console.ReadKey();
        }

        private static bool StartCompression(string filepath, string resultFilepath)
        {
            GZIPCompressor compressor = new();

            return compressor.Compress(filepath, resultFilepath);
        }

        private static void CheckArguments(string[] args, out string filepath, out string command, out string resultFilepath)
        {
            if (args.Length != 3)
                throw new ArgumentException("Wrong count of arguments. \nAcceptable signature: [command (compress/decompress)] [filepath] [result filepath]");

            command = args[0];
            filepath = args[1];
            resultFilepath = args[2];

            if (command != "compress" && command != "decompress")
                throw new ArgumentException("Wrong command. Please be sure that you are using compress/decompress command and try again.");

            if (!File.Exists(filepath))
                throw new ArgumentException("File doesn't exist. Please check your filepath and try again.");

            if (!Directory.Exists(Path.GetDirectoryName(resultFilepath)))
                throw new ArgumentException("Result directory doesn't exist. Please check your result path or create directory and try again.");
        }
    }
}