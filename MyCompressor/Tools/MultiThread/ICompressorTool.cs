using MyCompressor.Structures;

namespace MyCompressor.Tools.MultiThread
{
    internal interface ICompressorTool
    {
        DataBlock Process(DataBlock block);
    }
}
