namespace yUtil
{
    using CodeWalker.GameFiles;
    using Pastel;
    using SharpDX;
    using Sharprompt;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using yUtil.Intersection;

    internal class IntersectJob : Job
    {
        private readonly bool checkYmapName;
        private readonly HashSet<string> ignoredProperties = new()
        {
            "entityhash",
            "index",
        };
        private readonly HashSet<Type> nonRecursiveTypes = new()
        {
            typeof(string),
            typeof(Vector2),
            typeof(Vector3),
            typeof(Vector4),
            typeof(Quaternion),
        };
        private readonly string outDir;
        private readonly string ymapName;
        private readonly List<LazyYmapFile> ymaps = new();

        protected override HashSet<string> Extensions { get; set; }

        public IntersectJob(string outDir, string ymapName)
        {
            this.Extensions = new()
            {
                ".ymap",
            };
            this.outDir = outDir;
            this.ymapName = ymapName;
            this.checkYmapName = !string.IsNullOrEmpty(ymapName);
        }

        protected override Task HandleFileAsync(string file)
        {
            if ((!this.checkYmapName || file.EndsWith(this.ymapName, StringComparison.CurrentCulture))
                && !file.StartsWith(this.outDir, StringComparison.CurrentCulture))
            {
                this.ymaps.Add(new()
                {
                    FilePath = file,
                });
            }

            return Task.CompletedTask;
        }

        protected override async Task FinishAsync()
        {
            Dictionary<string, LazyYmapFile> files = new();

            for (int i = 0; i < this.ymaps.Count; i++)
            {
                LazyYmapFile file = this.ymaps[i];
                string name = Path.GetFileName(file.FilePath);
                
                if (!files.ContainsKey(name))
                {
                    files.Add(name, file);
                    continue;
                }

                Console.WriteLine($"Intersecting {file.FilePath.Pastel(ConsoleColor.DarkCyan)}...");

                try
                {
                    await this.IntersectYmaps(files[name], file);
                }
                catch (InvalidDataException)
                {
                    files[name] = file;
                    continue;
                }
                

                if (!Directory.Exists(this.outDir))
                {
                    Console.WriteLine($"Creating directory {this.outDir.Pastel(ConsoleColor.DarkCyan)}...");

                    Directory.CreateDirectory(this.outDir);
                }

                Console.WriteLine($"Saving {Path.Combine(this.outDir, name).Pastel(ConsoleColor.DarkCyan)}...");

                await files[name].YmapFile!.SaveFileAsync(this.outDir);

                Console.WriteLine("Saved".Pastel(ConsoleColor.DarkYellow));
            }
        }

        public async Task IntersectYmaps(LazyYmapFile left, LazyYmapFile right)
        {
            if (left.YmapFile == null)
            {
                left.YmapFile = new();
                await left.YmapFile.LoadFileAsync(left.FilePath, this.cache);
            }

            right.YmapFile = new();
            await right.YmapFile.LoadFileAsync(right.FilePath, this.cache);

            if (!left.YmapFile.Loaded)
            {
                if (right.YmapFile.Loaded)
                {
                    throw new InvalidDataException("Escrowed base file");
                }

                return;
            }

            IntersectBoxOccluders(left.YmapFile, right.YmapFile);
            IntersectCarGens(left.YmapFile, right.YmapFile);
            IntersectOccludeModels(left.YmapFile, right.YmapFile);
            this.IntersectEntityDefs(left.YmapFile, right.YmapFile);

            left.YmapFile.CalcExtents();
            left.YmapFile.CalcFlags();
        }

        private void IntersectEntityDefs(YmapFile left, YmapFile right)
        {
            if (left.AllEntities == null || right.AllEntities == null)
            {
                left.AllEntities = null;
                return;
            }

            List<YmapEntityDef?> entityDefs = left.AllEntities.ToList();

            for (int i = 0; i < entityDefs.Count; i++)
            {
                YmapEntityDef entityDef = entityDefs[i]!;
                YmapEntityDef? otherDef = right.FindEntityDef(entityDef, i);

                if (otherDef == null)
                {
                    Console.WriteLine($"{right.Name.Pastel(ConsoleColor.DarkCyan)} doesn't contain {entityDef.Name.Pastel(ConsoleColor.DarkYellow)}, skipping...");
                    entityDefs[i] = null;
                    continue;
                }

                entityDefs[i] = this.Intersect(entityDef, otherDef, (object left, object right, PropertyInfo propInfo) =>
                {
                    string nameUpper = propInfo.Name.ToLowerInvariant();
                    
                    // Can safely be ignored
                    if (this.ignoredProperties.Contains(nameUpper))
                    {
                        return;
                    }

                    if (nameUpper.StartsWith("position", StringComparison.CurrentCulture))
                    {
                        Vector3 leftVector = (Vector3)propInfo.GetValue(left);
                        Vector3 rightVector = (Vector3)propInfo.GetValue(right);

                        if (leftVector.X == rightVector.X && leftVector.Y == rightVector.Y)
                        {
                            propInfo.SetValue(left, new Vector3(leftVector.X, rightVector.Y, Math.Min(leftVector.Z, rightVector.Z)));
                            return;
                        }
                    }

                    if (nameUpper.StartsWith("scale", StringComparison.CurrentCulture))
                    {
                        object leftValue = propInfo.GetValue(left);
                        object rightValue = propInfo.GetValue(right);

                        if (propInfo.PropertyType.Equals(typeof(Vector3)))
                        {
                            propInfo.SetValue(left, Vector3.Min((Vector3)leftValue, (Vector3)rightValue));
                        }
                        else
                        {
                            propInfo.SetValue(left, Math.Min((float)leftValue, (float)rightValue));
                        }

                        //Console.WriteLine(leftValue + " : " + rightValue + " : " + propInfo.Name + " : " + propInfo.PropertyType);

                        return;
                    }

                    HandleDiff(left, right, entityDef.Name, propInfo);
                });
            }

            left.AllEntities = entityDefs.Where(e => e != null).ToArray();
        }

        private static void IntersectBoxOccluders(YmapFile left, YmapFile right)
        {
            if (left.BoxOccluders == null || right.BoxOccluders == null)
            {
                left.BoxOccluders = null;
                return;
            }

            left.BoxOccluders = left.BoxOccluders.Intersect(right.BoxOccluders, new BoxOccluderComparer())
                .ToArray();
        }

        private static void IntersectCarGens(YmapFile left, YmapFile right)
        {
            if (left.CarGenerators == null || right.CarGenerators == null)
            {
                left.CarGenerators = null;
                return;
            }

            left.CarGenerators = left.CarGenerators.Intersect(right.CarGenerators, new CarGenComparer())
                .ToArray();
        }

        private static void IntersectOccludeModels(YmapFile left, YmapFile right)
        {
            if (left.OccludeModels == null || right.OccludeModels == null)
            {
                left.OccludeModels = null;
                return;
            }

            if (left.OccludeModels.Length != right.OccludeModels.Length)
            {
                Console.WriteLine($"Unable to intersect occlude models ({"differing lengths".Pastel(ConsoleColor.Red)})!");
                return;
            }

            for (int i = 0; i < left.OccludeModels.Length; i++)
            {
                left.OccludeModels[i].Triangles = left.OccludeModels[i].Triangles.Intersect(right.OccludeModels[i].Triangles, new OccludeModelTriangleComparer())
                    .ToArray();
            }
        }

        private U Intersect<U>(U first, U second, Action<object, object, PropertyInfo> diffHandler)
        {
            object RecursiveIntersect(object left, object right)
            {
                Type lType = left.GetType();

                if (lType != right.GetType())
                {
                    return left;
                }

                foreach (PropertyInfo propInfo in lType.GetProperties())
                {
                    //Console.WriteLine(left.GetType() + " : " + propInfo.Name);

                    if (propInfo.GetIndexParameters().Any())
                    {
                        continue;
                    }

                    object leftValue = propInfo.GetValue(left, null);
                    object rightValue = propInfo.GetValue(right, null);

                    if (leftValue == null || rightValue == null
                        || propInfo.PropertyType == typeof(YmapFile) || propInfo.PropertyType == typeof(U) || propInfo.PropertyType == typeof(Archetype)
                        || (typeof(IEnumerable).IsAssignableFrom(propInfo.PropertyType) && propInfo.PropertyType != typeof(string)
                            && (propInfo.PropertyType.GetElementType() == typeof(YmapFile) || propInfo.PropertyType.GetElementType() == typeof(U))))
                    {
                        continue;
                    }

                    if (propInfo.PropertyType.IsPrimitive || this.nonRecursiveTypes.Contains(propInfo.PropertyType))
                    {
                        diffHandler(left, right, propInfo);
                    }
                    else if (leftValue != left)
                    {
                        propInfo.SetValue(left, RecursiveIntersect(propInfo.GetValue(left), propInfo.GetValue(right)));
                    }
                }

                return left;
            }

            return (U)RecursiveIntersect(first, second);
        }

        private static void HandleDiff(object left, object right, string name, PropertyInfo propInfo)
        {
            object leftValue = propInfo.GetValue(left);
            object rightValue = propInfo.GetValue(right);

            if (leftValue.Equals(rightValue))
            {
                return;
            }

            object result = Prompt.Select($"Conflict detected for {name} > {propInfo.Name}", new[] { leftValue, rightValue }, defaultValue: leftValue);

            propInfo.SetValue(left, result);
        }
    }
}
