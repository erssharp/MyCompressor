using MyCompressor.Compressors;
using System.IO.Compression;
using MyCompressor.Logger;

namespace MyCompressor
{
    public class Program
    {
        public static void Main(string[] args)
        {
            int result;
            try
            {
                GZIPCompressor compressor = new();
                CheckArguments(args, out string filepath, out string command, out string resultFilepath);
                CompressionMode mode = command == "compress" ? CompressionMode.Compress : CompressionMode.Decompress;
                result = compressor.Start(filepath, resultFilepath, mode) ? 1 : 0;
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
                throw new ArgumentException("Wrong count of arguments. \nAcceptable signature: [command (compress/decompress)] [filepath] [result filepath].\nCheck if you have spaces in your folder's name.");

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