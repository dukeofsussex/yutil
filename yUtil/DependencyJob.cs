namespace yUtil
{
    using CodeWalker.GameFiles;
    using Pastel;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;

    internal class DependencyJob(string extensions) : Job
    {
        protected override HashSet<string> Extensions { get; set; } = [.. extensions.Split(',')];

        private readonly List<GameFile> files = [];

        public override void Init()
        {
            if (CI.Enabled)
            {
                throw new NotSupportedException("[CI] Cannot determine dependencies in CI mode!");
            }

            base.Init();
        }

        protected override async Task HandleFileAsync(string file)
        {
            string ext = Path.GetExtension(file)[1..];
            GameFile gameFile = ext switch
            {
                "ymap" => new YmapFile(),
                "ytyp" => new YtypFile(),
                _ => throw new InvalidOperationException($"Missing gamefile assignment for {ext}!"),
            };

            await gameFile.LoadFileAsync(file, this.cache);

            if (!gameFile.Loaded)
            {
                return;
            }

            // Do it manually
            if (gameFile.Type == GameFileType.Ytyp)
            {
                YtypFile ytypFile = new();
                await ytypFile.LoadFileAsync(file, this.cache);
                this.cache.RegisterArchetypes(ytypFile.AllArchetypes);
            }

            this.files.Add(gameFile);
        }

        protected override async Task FinishAsync()
        {
            StringBuilder sb = new();

            for (int i = 0; i < this.files.Count; i++)
            {
                Dictionary<string, string> dependencies = [];
                GameFile file = this.files[i];

                Write($"Determining dependencies for {file.Name.Pastel(ConsoleColor.DarkCyan)}...");

                if (file.Type == GameFileType.Ymap)
                {
                    if (((YmapFile)file).AllEntities == null)
                    {
                        continue;
                    }

                    foreach (YmapEntityDef def in ((YmapFile)file).AllEntities)
                    {
                        if (dependencies.ContainsKey(def.Name))
                        {
                            continue;
                        }

                        dependencies.Add(def.Name, def.Archetype?.Ytyp.Name ?? "_Unknown_");
                    }
                }
                else if (file.Type == GameFileType.Ytyp)
                {
                    foreach (Archetype archetype in ((YtypFile)file).AllArchetypes)
                    {
                        if (archetype is not MloArchetype mloArchetype)
                        {
                            continue;
                        }

                        foreach (MCEntityDef entityDef in mloArchetype.entities)
                        {
                            if (dependencies.ContainsKey(entityDef.Name))
                            {
                                continue;
                            }

                            dependencies.Add(entityDef.Name, this.cache.Archetypes.ContainsKey(entityDef.Data.archetypeName.Hash) ? this.cache.Archetypes[entityDef.Data.archetypeName.Hash].Ytyp.Name : "_Unknown_");
                        }

                        if (mloArchetype.entitySets != null)
                        {
                            foreach (MCMloEntitySet entitySet in mloArchetype.entitySets)
                            {
                                foreach (MCEntityDef entityDef in entitySet.Entities)
                                {
                                    if (dependencies.ContainsKey(entityDef.Name))
                                    {
                                        continue;
                                    }

                                    dependencies.Add(entityDef.Name, this.cache.Archetypes.ContainsKey(entityDef.Data.archetypeName.Hash) ? this.cache.Archetypes[entityDef.Data.archetypeName.Hash].Ytyp.Name : "_Unknown_");
                                }
                            }
                        }
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Missing gamefile processor for {file.FilePath}!");
                }

                if (dependencies.Count == 0)
                {
                    continue;
                }

                IEnumerable<string> headerBorder = Enumerable.Repeat("=", file.FilePath.Length);

                sb.AppendLine(string.Concat(headerBorder));
                sb.AppendLine(file.FilePath);
                sb.AppendLine(string.Concat(headerBorder));

                foreach (KeyValuePair<string, IEnumerable<string>> dep in dependencies.GroupBy(d => d.Value).ToDictionary(d => d.Key, d => d.Select(e => e.Key)).OrderBy(d => d.Key))
                {
                    foreach (string prop in dep.Value)
                    {
                        sb.AppendLine(CultureInfo.InvariantCulture, $"{dep.Key}: {prop}");
                    }
                }
            }

            string reportFile = Path.Combine(Directory.GetCurrentDirectory(), $"{DateTime.Now:yyyy_MM_dd-HH_mm_ss}-dependencies.txt");

            Write($"Writing results to {reportFile.Pastel(ConsoleColor.DarkCyan)}...");

            await File.WriteAllTextAsync(reportFile, sb.ToString());
        }
    }
}
