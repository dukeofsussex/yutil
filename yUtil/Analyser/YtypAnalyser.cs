namespace yUtil.Analyser
{
    using CodeWalker.GameFiles;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using yUtil;

    internal class YtypAnalyser(YCache cache) : Analyser(cache)
    {
        private readonly Dictionary<uint, Archetype> customArchetypes = [];

        public override HashSet<string> SupportedExtensions =>
        [
            ".ytyp",
        ];

        public override async Task AnalyseAsync(string file)
        {
            YtypFile ytypFile = new();
            await ytypFile.LoadFileAsync(file, this.cache);

            if (!ytypFile.Loaded)
            {
                return;
            }

            if (ytypFile.AllArchetypes == null || ytypFile.AllArchetypes.Length == 0)
            {
                this.AddIssue(IssueSeverity.Error, file, "No archetype definitions");
                return;
            }

            bool original = this.cache.CoreFiles.ContainsKey(ytypFile.NameHash);

            for (int i = 0; i < ytypFile.AllArchetypes.Length; i++)
            {
                Archetype archetype = ytypFile.AllArchetypes[i];

                if (original && !this.cache.Archetypes.ContainsKey(archetype.Hash))
                {
                    this.AddIssue(IssueSeverity.Warn, file, $"Diversion: \"{archetype.Hash.ToDetailedString()}\" not in original.");
                }
                else if (customArchetypes.ContainsKey(archetype.Hash))
                {
                    string a = archetype.Hash.ToString();
                    this.AddIssue(IssueSeverity.Error, file, $"Redefinition: {archetype.Hash.ToDetailedString()} ({customArchetypes[archetype.Hash].Ytyp.FilePath})");
                }
                else if (!customArchetypes.ContainsKey(archetype.Hash))
                {
                    customArchetypes.Add(archetype.Hash, archetype);
                }
            }
        }
    }
}
