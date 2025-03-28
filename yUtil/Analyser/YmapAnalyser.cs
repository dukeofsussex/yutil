namespace yUtil.Analyser
{
    using CodeWalker.GameFiles;
    using SharpDX;
    using System.Collections.Generic;
    using yUtil;

    internal class YmapAnalyser(YCache cache) : Analyser(cache)
    {
        public override HashSet<string> SupportedExtensions =>
        [
            ".ymap"
        ];

        public override async Task AnalyseAsync(string file)
        {
            YmapFile modYmap = new();
            await modYmap.LoadFileAsync(file, this.cache);

            if (!modYmap.Loaded)
            {
                return;
            }

            if (modYmap.CMapData.entitiesExtentsMax == new Vector3(float.MinValue)
                || modYmap.CMapData.entitiesExtentsMin == new Vector3(float.MaxValue)
                || modYmap.CMapData.streamingExtentsMax == new Vector3(float.MinValue)
                || modYmap.CMapData.streamingExtentsMin == new Vector3(float.MaxValue))
            {
                this.AddIssue(IssueSeverity.Error, file, $"Extents: Need to be calculated!");
            }

            string name = Path.GetFileName(file);

            if (!this.cache.CoreFiles.TryGetValue(JenkHash.GenHash(name), out RpfFileEntry entry))
            {
                for (int i = 0; i < modYmap.AllEntities.Length; i++)
                {
                    this.AnalyseEntityDef(file, modYmap.AllEntities[i]);
                }

                return;
            }

            YmapFile ogYmap = new();
            ogYmap.Load(entry.File.ExtractFile(entry), entry);

            if (ogYmap.AllEntities == null)
            {
                if (modYmap.AllEntities != null)
                {
                    this.AddIssue(IssueSeverity.Error, file, "Entities: Original YMAP has none.");
                }

                return;
            }

            for (int i = 0; i < ogYmap.AllEntities.Length; i++)
            {
                YmapEntityDef ogDef = ogYmap.AllEntities[i];
                YmapEntityDef? def = modYmap.FindEntityDef(ogDef, i);

                this.AnalyseEntityDef(file, def, ogDef);
            }
        }

        private void AnalyseEntityDef(string file, YmapEntityDef? def, YmapEntityDef? ogDef = null)
        {
            if (def == null)
            {
                if (ogDef != null && ogDef.CEntityDef.lodLevel != rage__eLodType.LODTYPES_DEPTH_ORPHANHD)
                {
                    this.AddIssue(IssueSeverity.Error, file, $"LOD Disconnect: Deleted linked entity [{ogDef.Index}] \"{ogDef.Name}\" ({ogDef.Position}) ({ogDef.CEntityDef.lodLevel})");
                }

                return;
            }

            if (ogDef != null && ogDef.Position.Z > def.Position.Z && def.CEntityDef.lodLevel == rage__eLodType.LODTYPES_DEPTH_ORPHANHD)
            {
                this.AddIssue(IssueSeverity.Info, file, $"Unnecessary Reposition: [{def.Index}] \"{def.Name}\" ({def.Position}) can be deleted.");
            }

            if (def.CEntityDef.lodDist > 200 && (def.CEntityDef.lodLevel == rage__eLodType.LODTYPES_DEPTH_ORPHANHD || def.CEntityDef.lodLevel == rage__eLodType.LODTYPES_DEPTH_HD))
            {
                this.AddIssue(def.LodDist >= 500 ? IssueSeverity.Error : IssueSeverity.Warn, file, $"Large LOD Distance: [{def.Index}] \"{def.CEntityDef.archetypeName.ToDetailedString()}\" ({def.CEntityDef.lodDist})");
            }
        }
    }
}
