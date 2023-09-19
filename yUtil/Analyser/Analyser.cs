namespace yUtil.Analyser
{
    internal abstract class Analyser : IAnalyser
    {
        protected readonly YCache cache;

        public Dictionary<string, List<Issue>> Issues { get; }

        public static HashSet<string> DefaultExtensions => new()
        {
            ".ybd",
            ".ybn",
            ".ycd",
            ".ydr",
            ".ydd",
            ".yed",
            ".yfd",
            ".yft",
            ".yld",
            ".ymap",
            ".ymf",
            ".ymt",
            ".ynd",
            ".ynv",
            ".ypdb",
            ".ypt",
            ".ysc",
            ".ytd",
            ".ytyp",
            ".yvr",
            ".ywr",
        };

        public Analyser(YCache cache)
        {
            this.cache = cache;
            this.Issues = new();
        }

        protected void AddIssue(IssueSeverity severity, string file, string message)
        {
            if (!this.Issues.ContainsKey(file))
            {
                this.Issues[file] = new();
            }

            this.Issues[file].Add(new Issue
            {
                Message = message,
                Severity = severity
            });
        }

        public abstract HashSet<string> SupportedExtensions { get; }

        public abstract Task AnalyseAsync(string file);
    }
}