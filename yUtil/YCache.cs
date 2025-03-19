namespace yUtil
{
    using CodeWalker.GameFiles;
    using System;
    using System.Collections.Generic;

    internal class YCache
    {
        public RpfManager RpfManager { get; private set; }

        public Dictionary<uint, Archetype> Archetypes { get; private set; } = [];

        public Dictionary<uint, RpfFileEntry> CoreFiles { get; private set; } = [];

        private Action<string>? UpdateStatus;
        private Action<string>? ErrorLog;

        public YCache()
        {
            this.RpfManager = new ()
            {
                BuildExtendedJenkIndex = true,
                EnableMods = true,
            };
        }

        public void Init(string dir, Action<string> updateStatus, Action<string> errorLog)
        {
            this.UpdateStatus = updateStatus;
            this.ErrorLog = errorLog;

            GTA5Keys.LoadFromPath(dir);
            this.RpfManager.Init(dir, updateStatus, errorLog);

            this.UpdateStatus("Building cache...");

            foreach (RpfFile? rpffile in this.RpfManager.AllRpfs)
            {
                if (rpffile.AllEntries == null)
                {
                    continue;
                }

                foreach (RpfEntry? entry in rpffile.AllEntries)
                {
                    if (entry is not RpfFileEntry)
                    {
                        continue;
                    }

                    RpfFileEntry fileEntry = (RpfFileEntry)entry;

                    int index = fileEntry.NameLower.LastIndexOf('.');

                    if (index == -1)
                    {
                        continue;
                    }

                    this.CoreFiles[fileEntry.NameHash] = fileEntry;

                    if (fileEntry.Name.EndsWith("ytyp", StringComparison.CurrentCulture))
                    {
                        this.RegisterArchetypes(this.RpfManager.GetFile<YtypFile>(fileEntry).AllArchetypes);
                    }
                }
            }
        }

        public void RegisterArchetypes(Archetype[] archetypes)
        {
            if ((archetypes == null) || (archetypes.Length == 0))
            {
                return;
            }

            for (int i = 0; i < archetypes.Length; i++)
            {
                Archetype archetype = archetypes[i];
                if (archetype.Hash == 0)
                {
                    this.ErrorLog!("Invalid archetype hash (0)");
                    continue;
                }

                this.Archetypes[archetype.Hash] = archetype;
                JenkIndex.Ensure(archetype.Name);
            }
        }
    }
}
