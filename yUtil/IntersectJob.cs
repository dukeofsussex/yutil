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
    using System.Text.RegularExpressions;
    using yUtil.Intersection;

    internal class IntersectJob : Job
    {
        private readonly HashSet<string> ignoredProperties = new()
        {
            "entityhash",
            "index",
            "unused5",
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
        private readonly Regex ymapName;
        private readonly List<LazyYmapFile> ymaps = new();

        protected override HashSet<string> Extensions { get; set; }

        public IntersectJob(string outDir, string ymapName)
        {
            this.Extensions = new()
            {
                ".ymap",
            };
            this.outDir = Path.GetFullPath(outDir);
            this.ymapName = new Regex($".*{ymapName.Replace(".ymap", string.Empty).Replace("*", ".*")}\\.ymap$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        protected override Task HandleFileAsync(string file)
        {
            if (this.ymapName.IsMatch(file) && !Path.GetFullPath(file).StartsWith(this.outDir, StringComparison.OrdinalIgnoreCase))
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
                    Console.WriteLine($"Base {this.ShortenFilePath(file.FilePath).Pastel(ConsoleColor.DarkCyan)}...");
                    files.Add(name, file);
                    continue;
                }

                Console.WriteLine($"Intersecting {this.ShortenFilePath(file.FilePath).Pastel(ConsoleColor.DarkCyan)}...");

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

                Console.WriteLine("Saved.".Pastel(ConsoleColor.DarkGreen));
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
                Console.WriteLine($"Skipping AllEntities ({"null".Pastel(ConsoleColor.DarkYellow)})...");
                left.AllEntities = null;
                return;
            }

            List<YmapEntityDef?> entityDefs = left.AllEntities.ToList();
            int index = -1;

            for (int i = 0; i < entityDefs.Count; i++)
            {
                YmapEntityDef entityDef = entityDefs[i]!;
                YmapEntityDef? otherDef = right.FindEntityDef(entityDef, i);

                if (otherDef == null)
                {
                    Console.WriteLine($"Skipping [{index}] {entityDef.Name.Pastel(ConsoleColor.DarkYellow)} (not in {right.Name.Pastel(ConsoleColor.DarkCyan)})...");
                    entityDefs[i] = null;
                    continue;
                }

                index++;

                entityDefs[i] = this.Intersect(entityDef, otherDef, (object left, object right, PropertyInfo propInfo) =>
                {
                    string nameLower = propInfo.Name.ToLowerInvariant();
                    
                    // Can safely be ignored
                    if (this.ignoredProperties.Contains(nameLower))
                    {
                        return;
                    }

                    if (nameLower.StartsWith("position", StringComparison.CurrentCulture))
                    {
                        Vector3 leftVector = (Vector3)propInfo.GetValue(left);
                        Vector3 rightVector = (Vector3)propInfo.GetValue(right);

                        if (leftVector.X == rightVector.X && leftVector.Y == rightVector.Y)
                        {
                            Vector3 newPosition = new(leftVector.X, leftVector.Y, Math.Min(leftVector.Z, rightVector.Z));
                            propInfo.SetValue(left, newPosition);
                            Console.WriteLine($"Adjusted position for [{index}] {entityDef.Name.Pastel(ConsoleColor.DarkYellow)} ({newPosition}).");
                            return;
                        }
                    }

                    if (nameLower.StartsWith("scale", StringComparison.CurrentCulture))
                    {
                        object leftValue = propInfo.GetValue(left);
                        object rightValue = propInfo.GetValue(right);

                        if (propInfo.PropertyType.Equals(typeof(Vector3)))
                        {
                            Vector3 newScale = Vector3.Min((Vector3)leftValue, (Vector3)rightValue);
                            propInfo.SetValue(left, newScale);
                            Console.WriteLine($"Adjusted scale for [{index}] {entityDef.Name.Pastel(ConsoleColor.DarkYellow)} ({newScale}).");
                        }
                        else
                        {
                            float newScale = Math.Min((float)leftValue, (float)rightValue);
                            propInfo.SetValue(left, newScale);
                            Console.WriteLine($"Adjusted scale for [{index}] {entityDef.Name.Pastel(ConsoleColor.DarkYellow)} ({newScale}).");
                        }

                        //Console.WriteLine(leftValue + " : " + rightValue + " : " + propInfo.Name + " : " + propInfo.PropertyType);

                        return;
                    }

                    HandleDiff(left, right, index, entityDef.Name, propInfo);
                });
            }

            left.AllEntities = entityDefs.Where(e => e != null).ToArray();

            Console.WriteLine("Intersected Entities.".Pastel(ConsoleColor.DarkGreen));
        }

        private static void IntersectBoxOccluders(YmapFile left, YmapFile right)
        {
            if (left.BoxOccluders == null || right.BoxOccluders == null)
            {
                Console.WriteLine($"Skipping BoxOccluders ({"null".Pastel(ConsoleColor.DarkYellow)})...");
                left.BoxOccluders = null;
                left.CBoxOccluders = null;
                return;
            }

            left.BoxOccluders = left.BoxOccluders.Intersect(right.BoxOccluders, new BoxOccluderComparer())
                .ToArray();

            Console.WriteLine("Intersected BoxOccluders.".Pastel(ConsoleColor.DarkGreen));
        }

        private static void IntersectCarGens(YmapFile left, YmapFile right)
        {
            if (left.CarGenerators == null || right.CarGenerators == null)
            {
                Console.WriteLine($"Skipping CarGenerators ({"null".Pastel(ConsoleColor.DarkYellow)})...");
                left.CarGenerators = null;
                return;
            }

            left.CarGenerators = left.CarGenerators.Intersect(right.CarGenerators, new CarGenComparer())
                .ToArray();

            Console.WriteLine("Intersected CarGenerators.".Pastel(ConsoleColor.DarkGreen));
        }

        private static void IntersectOccludeModels(YmapFile left, YmapFile right)
        {
            if (left.OccludeModels == null || right.OccludeModels == null)
            {
                Console.WriteLine($"Skipping OccludeModels ({"null".Pastel(ConsoleColor.DarkYellow)})...");
                left.OccludeModels = null;
                return;
            }

            List<YmapOccludeModel> intersectedOccludeModels = new();

            for (int i = 0; i < left.OccludeModels.Length; i++)
            {
                YmapOccludeModel leftOccludeModel = left.OccludeModels[i];
                YmapOccludeModel rightOccludeModel = right.OccludeModels.Where(om => MathUtil.WithinEpsilon(leftOccludeModel.OccludeModel.bmax.X, om.OccludeModel.bmax.X, 1)
                        && MathUtil.WithinEpsilon(leftOccludeModel.OccludeModel.bmax.Y, om.OccludeModel.bmax.Y, 1)
                        && MathUtil.WithinEpsilon(leftOccludeModel.OccludeModel.bmax.Z, om.OccludeModel.bmax.Z, 1)
                        && MathUtil.WithinEpsilon(leftOccludeModel.OccludeModel.bmin.X, om.OccludeModel.bmin.X, 1)
                        && MathUtil.WithinEpsilon(leftOccludeModel.OccludeModel.bmin.Y, om.OccludeModel.bmin.Y, 1)
                        && MathUtil.WithinEpsilon(leftOccludeModel.OccludeModel.bmin.Z, om.OccludeModel.bmin.Z, 1))
                    .FirstOrDefault();

                if (rightOccludeModel != null) {
                    leftOccludeModel.Triangles = leftOccludeModel.Triangles.Intersect(rightOccludeModel.Triangles, new OccludeModelTriangleComparer())
                        .ToArray();
                    
                    if (leftOccludeModel.Triangles.Length > 0)
                    {
                        intersectedOccludeModels.Add(leftOccludeModel);
                    }
                    else
                    {
                        Console.WriteLine($"Removed OccludeModel ({"no triangles".Pastel(ConsoleColor.DarkYellow)})...");
                    }
                }
            }

            left.OccludeModels = intersectedOccludeModels.ToArray();

            Console.WriteLine("Intersected OccludeModels.".Pastel(ConsoleColor.DarkGreen));
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
                        try
                        {
                            propInfo.SetValue(left, RecursiveIntersect(propInfo.GetValue(left), propInfo.GetValue(right)));
                        }
                        catch
                        {
                            Console.WriteLine($"Unable to set value on {left}!");
                        }
                    }
                }

                return left;
            }

            return (U)RecursiveIntersect(first, second);
        }

        private static void HandleDiff(object left, object right, int index, string name, PropertyInfo propInfo)
        {
            object leftValue = propInfo.GetValue(left);
            object rightValue = propInfo.GetValue(right);

            if (leftValue.Equals(rightValue))
            {
                return;
            }

            object result = Prompt.Select($"Conflict detected for [{index}] {name} > {propInfo.Name}", new[] { leftValue, rightValue }, defaultValue: leftValue);

            propInfo.SetValue(left, result);
        }
    }
}
