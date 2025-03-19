namespace yUtil.Analyser
{
    using System.Collections.Generic;

    internal class DuplicateAnalyser(YCache cache) : Analyser(cache)
    {
        private readonly Dictionary<string, string> files = [];

        public override HashSet<string> SupportedExtensions => CI.Enabled ? [] : DefaultExtensions;

        public override Task AnalyseAsync(string file)
        {
            string name = Path.GetFileName(file);

            if (this.files.ContainsKey(name))
            {
                this.AddIssue(IssueSeverity.Warn, this.files[name], $"Naming Collision: {file}");
            }
            else
            {
                this.files.Add(name, file);
            }

            return Task.CompletedTask;
        }
    }
}
