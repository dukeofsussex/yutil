﻿namespace yUtil
{
    using CodeWalker.GameFiles;
    using Pastel;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    internal class CalcJob : Job
    {
        private readonly List<string> ymaps = new();

        protected override HashSet<string> Extensions { get; set; }

        public CalcJob()
        {
            this.Extensions = new()
            {
                ".ymap"
            };
        }

        protected override Task HandleFileAsync(string file)
        {
            this.ymaps.Add(file);
            return Task.CompletedTask;
        }

        protected override async Task FinishAsync()
        {
            for (int i = 0; i < this.ymaps.Count; i++)
            {
                YmapFile ymapFile = new();
                await ymapFile.LoadFileAsync(this.ymaps[i], this.cache);

                Console.WriteLine($"Calculating flags and extents for {ymapFile.FilePath.Pastel(ConsoleColor.DarkCyan)}...");

                if (!ymapFile.Loaded)
                {
                    Console.WriteLine($"Unable to load {ymapFile.FilePath.Pastel(ConsoleColor.DarkCyan)}!");
                }

                if (ymapFile.AllEntities != null)
                {
                    for (int j = 0; j < ymapFile.AllEntities.Length; j++)
                    {
                        if (ymapFile.AllEntities[j].Archetype == null)
                        {
                            Console.WriteLine($"Missing archetype definition for {ymapFile.AllEntities[j].Name.Pastel(ConsoleColor.DarkRed)}!");
                        }
                    }
                }
                
                if (ymapFile.CalcExtents() || ymapFile.CalcFlags())
                {
                    Console.WriteLine("Changed".Pastel(ConsoleColor.DarkYellow));
                    await ymapFile.SaveFileAsync();
                }
                else
                {
                    Console.WriteLine("Unchanged".Pastel(ConsoleColor.Gray));
                }
            }
        }
    }
}
