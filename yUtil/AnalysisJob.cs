namespace yUtil
{
    using Pastel;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using yUtil.Analyser;

    internal class AnalysisJob(string extensions) : Job
    {
        private Dictionary<string, List<IAnalyser>> analysers = [];

        protected override HashSet<string> Extensions { get; set; } = [.. extensions.Split(',')];

        public override void Init()
        {
            base.Init();

            Write("Loading analysers...");

            this.analysers = Assembly.GetEntryAssembly()
                .GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(IAnalyser).IsAssignableFrom(t))
                .Select(a => Activator.CreateInstance(a, this.cache) as IAnalyser)
                .Where(a => a.SupportedExtensions.Any(e => this.Extensions.Contains(e)))
                .SelectMany(a => a.SupportedExtensions, (analyser, ext) => new { analyser, ext })
                .GroupBy(a => a.ext, a => a.analyser)
                .ToDictionary(a => a.Key, a => a.ToList());
        }

        protected override async Task HandleFileAsync(string file)
        {

            if (this.analysers.TryGetValue(Path.GetExtension(file), out List<IAnalyser> analysers)) {
                foreach (IAnalyser analyser in analysers)
                {
                    await analyser.AnalyseAsync(file);
                }
            }
        }

        protected override async Task FinishAsync()
        {
            Dictionary<string, IOrderedEnumerable<Issue>> fileIssues = this.analysers.Values
                .SelectMany(a => a)
                .Distinct()
                .SelectMany(a => a.Issues)
                .GroupBy(i => i.Key, i => i.Value)
                .ToDictionary(i => i.Key, i => i.SelectMany(x => x)
                    .OrderByDescending(x => x.Severity)
                    .ThenBy(x => x.Message));

            if (fileIssues.Count == 0)
            {
                Console.WriteLine("OK.".Pastel(ConsoleColor.DarkGreen));
                return;
            }

            StringBuilder builder = new();
            int ciExitCode = CI.ExitCode;

            foreach (KeyValuePair<string, IOrderedEnumerable<Issue>> file in fileIssues)
            {
                IEnumerable<string> headerBorder = Enumerable.Repeat("=", file.Key.Length);

                builder.AppendLine(string.Concat(headerBorder));
                builder.AppendLine(file.Key);
                builder.AppendLine(string.Concat(headerBorder));

                foreach (Issue issue in file.Value)
                {
                    if (ciExitCode != 1 && issue.Severity > IssueSeverity.Info)
                    {
                        ciExitCode = issue.Severity == IssueSeverity.Error ? 1 : 2;
                    }

                    builder.AppendLine(CultureInfo.InvariantCulture, $"[{issue.Severity.ToString().ToUpperInvariant()}] {issue.Message}");
                }

                builder.AppendLine();
            }

            if (CI.Enabled)
            {
                Console.WriteLine();
                Console.WriteLine(builder.ToString());
                CI.ExitCode = ciExitCode;
            }
            else
            {
                string reportFile = Path.Combine(Directory.GetCurrentDirectory(), $"{DateTime.Now:yyyy_MM_dd-HH_mm_ss}-analysis.txt");
                Write($"Writing results to {reportFile.Pastel(ConsoleColor.DarkCyan)}...");
                await File.WriteAllTextAsync(reportFile, builder.ToString());
            }
        }
    }
}
