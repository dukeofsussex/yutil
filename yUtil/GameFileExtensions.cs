namespace yUtil
{
    using CodeWalker.GameFiles;
    using System;
    using System.IO;
    using System.Linq;

    internal static class GameFileExtensions
    {
        private const float Epsilon = 0.001f;

        public static YmapEntityDef? FindEntityDef(this YmapFile ymapFile, YmapEntityDef def, int index)
        {
            if (ymapFile.AllEntities?.Length > index)
            {
                YmapEntityDef indexDef = ymapFile.AllEntities[index];

                if (IsMatchingEntityDef(def, indexDef))
                {
                    return indexDef;
                }
            }

            return ymapFile.AllEntities?.FirstOrDefault((e) => IsMatchingEntityDef(def, e));
        }

        public static void InitYmapEntityArchetypes(this MloInstanceData instance, YCache cache)
        {
            if (instance.Owner == null)
            {
                return;
            }

            Archetype ownerArchetype = instance.Owner.Archetype;

            if (instance.Entities != null)
            {
                for (int i = 0; i < instance.Entities.Length; i++)
                {
                    YmapEntityDef entityDef = instance.Entities[i];
                    if (cache.Archetypes.TryGetValue(entityDef._CEntityDef.archetypeName, out Archetype archetype))
                    {
                        entityDef.SetArchetype(archetype);
                    }
                }

                instance.UpdateBBs(ownerArchetype);
            }

            if (instance.EntitySets != null)
            {
                for (int i = 0; i < instance.EntitySets.Length; i++)
                {
                    MloInstanceEntitySet entitySet = instance.EntitySets[i];
                    List<YmapEntityDef> entities = entitySet.Entities;

                    if (entities == null)
                    {
                        continue;
                    }

                    for (int j = 0; j < entities.Count; j++)
                    {
                        YmapEntityDef entityDef = entities[j];
                        if (cache.Archetypes.TryGetValue(entityDef._CEntityDef.archetypeName, out Archetype archetype))
                        {
                            entityDef.SetArchetype(archetype);
                        }
                    }
                }
            }
        }

        public static void InitYmapEntityArchetypes(this YmapFile ymapFile, YCache cache)
        {
            if (ymapFile.AllEntities != null)
            {
                for (int i = 0; i < ymapFile.AllEntities.Length; i++)
                {
                    YmapEntityDef entityDef = ymapFile.AllEntities[i];
                    if (cache.Archetypes.TryGetValue(entityDef._CEntityDef.archetypeName, out Archetype archetype))
                    {
                        entityDef.SetArchetype(archetype);
                    }

                    if (entityDef.IsMlo && entityDef.MloInstance != null)
                    {
                        entityDef.MloInstance.InitYmapEntityArchetypes(cache);
                    }
                }
            }

            if (ymapFile.GrassInstanceBatches != null)
            {
                for (int i = 0; i < ymapFile.GrassInstanceBatches.Length; i++)
                {
                    YmapGrassInstanceBatch batch = ymapFile.GrassInstanceBatches[i];
                    if (cache.Archetypes.TryGetValue(batch.Batch.archetypeName, out Archetype archetype))
                    {
                        batch.Archetype = archetype;
                    }
                }
            }

            // Ignore TimeCycleModifiers
        }

        public static async Task LoadFileAsync(this GameFile gameFile, string path, YCache cache)
        {
            byte[] data = await File.ReadAllBytesAsync(path);

            try
            {
                switch (gameFile.Type)
                {
                    case GameFileType.Ydd:
                        (gameFile as YddFile)!.Load(data);
                        break;
                    case GameFileType.Ydr:
                        (gameFile as YdrFile)!.Load(data);
                        break;
                    case GameFileType.Yft:
                        (gameFile as YftFile)!.Load(data);
                        break;
                    case GameFileType.Ymap:
                        YmapFile ymapFile = (gameFile as YmapFile)!;
                        ymapFile.Load(data);
                        ymapFile.InitYmapEntityArchetypes(cache);
                        break;
                    case GameFileType.Ytd:
                        (gameFile as YtdFile)!.Load(data);
                        break;
                    case GameFileType.Ytyp:
                        (gameFile as YtypFile)!.Load(data);
                        break;
                    default:
                        throw new InvalidOperationException($"Missing loading logic for {gameFile.Type}!");
                }
            }
            catch (InvalidDataException) // Escrowed
            {
                gameFile.Loaded = false;
                return;
            }
            finally
            {
                gameFile.SetDetails(path);
            }
        }


        public static async Task SaveFileAsync(this YmapFile ymapFile)
        {
            await File.WriteAllBytesAsync(ymapFile.FilePath, ymapFile.Save());
        }

        public static async Task SaveFileAsync(this YmapFile ymapFile, string path)
        {
            await File.WriteAllBytesAsync(Path.Combine(path, ymapFile.Name), ymapFile.Save());
        }

        public static void SetDetails(this GameFile file, string path)
        {
            string name = Path.GetFileName(path);

            file.RpfFileEntry = new RpfResourceFileEntry()
            {
                Name = name,
                NameLower = name.ToLowerInvariant(),
                Path = path,
            };

            file.RpfFileEntry.NameHash = JenkHash.GenHash(file.RpfFileEntry.NameLower);
            file.RpfFileEntry.ShortNameHash = JenkHash.GenHash(Path.GetFileNameWithoutExtension(file.RpfFileEntry.NameLower));
            file.FilePath = file.RpfFileEntry.Path;
            file.Name = file.RpfFileEntry.Name;
        }

        private static bool IsMatchingEntityDef(YmapEntityDef left, YmapEntityDef right)
        {
            // Check guid match
            if (left.CEntityDef.guid != 0)
            {
                return left.CEntityDef.guid == right.CEntityDef.guid;
            }

            // Check position (ignore Z axis shift) and archetype
            return Math.Abs(left.CEntityDef.position.X - right.CEntityDef.position.X) < Epsilon
                    && Math.Abs(left.CEntityDef.position.Y - right.CEntityDef.position.Y) < Epsilon
                    && left.CEntityDef.archetypeName.Hash == right.CEntityDef.archetypeName.Hash;
        }
    }
}
