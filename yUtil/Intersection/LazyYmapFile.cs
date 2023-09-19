namespace yUtil.Intersection
{
    using CodeWalker.GameFiles;

    internal class LazyYmapFile
    {
        public YmapFile? YmapFile { get; set; }

        public string FilePath { get; set; } = string.Empty;
    }
}
