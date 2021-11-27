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
                GZIPCompressor compressor = new();
#if DEBUG
                if (true)
                {
                    string resultFilepath = @"C:\Users\ernestteregulov\Downloads\SpaceSweepers.mp4";
                    string filepath = @"C:\Users\ernestteregulov\source\repos\MyCompressor\MyCompressor\bin\Debug\net6.0\space.gz";
                    if (File.Exists(resultFilepath)) File.Delete(resultFilepath);
                    result = compressor.Start(filepath, resultFilepath, CompressionMode.Decompress) ? 1 : 0;
                }
                else
                {
                    string filepath = @"C:\Users\ernestteregulov\Downloads\SpaceSweepers.1080p.WEBRip.x264.AAC.Zetflix.mp4";
                    string resultFilepath = @"C:\Users\ernestteregulov\source\repos\MyCompressor\MyCompressor\bin\Debug\net6.0\space.gz";
                    if(File.Exists(resultFilepath)) File.Delete(resultFilepath);
                    result = compressor.Start(filepath, resultFilepath, CompressionMode.Compress) ? 1 : 0;
                }
#else
                CheckArguments(args, out string filepath, out string command, out string resultFilepath);
                CompressionMode mode = command == "compress" ? CompressionMode.Compress : CompressionMode.Decompress;
                compressor.Start(filepath, resultFilepath, mode);
#endif
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unhandled Exception: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
                result = 0;
            }

            if (result == 0)
                MyLogger.ShowLog();

            Console.WriteLine(result);

            Console.ReadKey();
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