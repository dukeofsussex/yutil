namespace yUtil.Analyser
{
    internal abstract class Analyser(YCache cache) : IAnalyser
    {
        protected readonly YCache cache = cache;

        public Dictionary<string, List<Issue>> Issues { get; } = [];

        public static HashSet<string> DefaultExtensions =>
        [
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
        ];

        protected void AddIssue(IssueSeverity severity, string file, string message)
        {
            if (!this.Issues.ContainsKey(file))
            {
                this.Issues[file] = [];
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
