﻿namespace yUtil
{
    using Pastel;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using yUtil.Analyser;

    internal class AnalysisJob : Job
    {
        private Dictionary<string, List<IAnalyser>> analysers = new();

        protected override HashSet<string> Extensions { get; set; }

        public AnalysisJob(string extensions)
        {
            this.Extensions = extensions.Split(',').ToHashSet();
        }

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
            foreach (IAnalyser analyser in this.analysers[Path.GetExtension(file)])
            {
                await analyser.AnalyseAsync(file);
            }
        }

        protected override async Task FinishAsync()
        {
            Write("Generating report...");

            Dictionary<string, IOrderedEnumerable<Issue>> fileIssues = this.analysers.Values
                .SelectMany(a => a)
                .Distinct()
                .SelectMany(a => a.Issues)
                .GroupBy(i => i.Key, i => i.Value)
                .ToDictionary(i => i.Key, i => i.SelectMany(x => x)
                    .OrderByDescending(x => x.Severity)
                    .ThenBy(x => x.Message));

            StringBuilder builder = new();

            foreach (KeyValuePair<string, IOrderedEnumerable<Issue>> file in fileIssues)
            {
                IEnumerable<string> headerBorder = Enumerable.Repeat("=", file.Key.Length);

                builder.AppendLine(string.Concat(headerBorder));
                builder.AppendLine(file.Key);
                builder.AppendLine(string.Concat(headerBorder));

                foreach (Issue issue in file.Value)
                {
                    builder.AppendLine($"[{issue.Severity.ToString().ToUpperInvariant()}] {issue.Message}");
                }

                builder.AppendLine();
            }

            string reportFile = Path.Combine(Directory.GetCurrentDirectory(), $"{DateTime.Now:yyyy_MM_dd-HH_mm_ss}-analysis.txt");

            Write($"Writing results to {reportFile.Pastel(ConsoleColor.DarkCyan)}...");

            await File.WriteAllTextAsync(reportFile, builder.ToString());
        }
    }
}
