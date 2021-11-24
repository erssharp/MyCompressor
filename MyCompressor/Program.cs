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
            try
            {
                CheckArguments(args, out string filepath, out string command, out string resultFilepath);

                ConfigurationManager.AppSettings["filePath"] = filepath;
                ConfigurationManager.AppSettings["resultPath"] = filepath;

                ServicesHost.StartHost();

                GZIPCompressor compressor = new();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unhandled Exception: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(0);
            }
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