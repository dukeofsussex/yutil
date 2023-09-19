namespace yUtil
{
    using CodeWalker.GameFiles;
    using Pastel;
    using System;

    internal abstract class Job
    {
        protected readonly YCache cache = new();

        protected abstract HashSet<string> Extensions { get; set; }

        public virtual void Init()
        {
            string? gtaDir = Environment.GetEnvironmentVariable("GTA_DIR");

            if (string.IsNullOrEmpty(gtaDir))
            {
                throw new IOException("Please configure the .env file!");
            }

            this.cache.Init(
                gtaDir,
                (string u) => Write(u[..Math.Min(u.Length, Console.BufferWidth)]),
                (string err) => Console.WriteLine(err.Pastel(ConsoleColor.DarkRed))
            );

            Write("Ready.".Pastel(ConsoleColor.DarkGreen));
            Console.WriteLine();
        }

        public async Task Run(string dir, string pattern = "*.y*")
        {
            foreach (string file in Directory.EnumerateFiles(dir, pattern, SearchOption.AllDirectories))
            {
                string ext = Path.GetExtension(file);

                if (ext == ".ytyp" && !this.Extensions.Contains(".ytyp"))
                {
                    YtypFile ytypFile = new();
                    await ytypFile.LoadFileAsync(file, this.cache);
                    this.cache.RegisterArchetypes(ytypFile.AllArchetypes);
                }

                if (this.Extensions.Contains(ext))
                {
                    Write($"Processing {file[(file.Length - Math.Min(file.Length, Console.BufferWidth - 15))..].Pastel(ConsoleColor.DarkCyan)}...");
                    JenkIndex.Ensure(Path.GetFileName(file));
                    JenkIndex.Ensure(Path.GetFileNameWithoutExtension(file));

                    await this.HandleFileAsync(file);
                }                
            }

            Console.WriteLine();

            await this.FinishAsync();
        }

        protected abstract Task FinishAsync();

        protected abstract Task HandleFileAsync(string file);

        protected static void Write(string text)
        {
            Console.ResetColor();
            Console.Write($"\r{text.PadRight(Console.BufferWidth)}");
        }
    }
}
