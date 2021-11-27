namespace MyCompressor.Structures
{
    internal struct DataBlock
    {
        public long Id;
        public int OrigignalSize;
        public int Length => Data == null ? -1 : Data.Length;
        public byte[]? Data;
    }
}