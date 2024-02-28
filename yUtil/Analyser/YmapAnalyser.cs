namespace yUtil.Analyser
{
    using CodeWalker.GameFiles;
    using SharpDX;
    using System.Collections.Generic;
    using yUtil;

    internal class YmapAnalyser : Analyser
    {
        public YmapAnalyser(YCache cache) : base(cache) { }

        public override HashSet<string> SupportedExtensions => new()
        {
            ".ymap"
        };

        public override async Task AnalyseAsync(string file)
        {
            string name = Path.GetFileName(file);

            RpfEntry entry = this.cache.CoreFiles.FirstOrDefault(e => e.Key == JenkHash.GenHash(name)).Value;

            if (entry == null)
            {
                return;
            }

            RpfFileEntry rpfFileEntry = entry as RpfFileEntry;

            YmapFile ogYmap = new();
            ogYmap.Load(entry.File.ExtractFile(rpfFileEntry), rpfFileEntry);

            YmapFile modYmap = new();
            await modYmap.LoadFileAsync(file, this.cache);

            if (!modYmap.Loaded)
            {
                return;
            }

            float extents = (modYmap.CMapData.streamingExtentsMax + modYmap.CMapData.streamingExtentsMin).Length();

            if (modYmap.CMapData.entitiesExtentsMax == new Vector3(float.MinValue)
                || modYmap.CMapData.entitiesExtentsMin == new Vector3(float.MaxValue)
                || modYmap.CMapData.streamingExtentsMax == new Vector3(float.MinValue)
                || modYmap.CMapData.streamingExtentsMin == new Vector3(float.MaxValue))
            {
                this.AddIssue(IssueSeverity.Error, file, $"Extents: Need to be calculated!");
            }
            else if (extents > 1500f)
            {
                this.AddIssue(IssueSeverity.Warn, file, $"Extents: Large area ({extents:F2})");
            }

            if (ogYmap.AllEntities == null)
            {
                return;
            }

            for (int i = 0; i < ogYmap.AllEntities.Length; i++)
            {
                YmapEntityDef ogDef = ogYmap.AllEntities[i];
                YmapEntityDef? def = modYmap.FindEntityDef(ogDef, i);

                if (def != null)
                {
                    if (ogDef.Position.Z > def.Position.Z && def.CEntityDef.lodLevel == rage__eLodType.LODTYPES_DEPTH_ORPHANHD)
                    {
                        this.AddIssue(IssueSeverity.Info, file, $"Unnecessary Reposition: [{def.Index}] \"{def.Name}\" ({def.Position}) can be deleted.");
                    }

                    if (def.LodDist >= 1000)
                    {
                        this.AddIssue(IssueSeverity.Info, file, $"Large LOD Distance: [{def.Index}] \"{def.Name}\" ({def.LodDist})");
                    }
                }
                else if (def == null && ogDef.CEntityDef.lodLevel != rage__eLodType.LODTYPES_DEPTH_ORPHANHD)
                {
                    this.AddIssue(IssueSeverity.Error, file, $"LOD Disconnect: Deleted linked entity [{ogDef.Index}] \"{ogDef.Name}\" ({ogDef.Position}) ({ogDef.CEntityDef.lodLevel})");
                }
            }
        }
    }
}
