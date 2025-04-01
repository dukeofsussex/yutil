namespace yUtil
{
    using CodeWalker.GameFiles;
    using Pastel;
    using System;

    internal abstract class Job
    {
        protected readonly YCache cache = new();

        protected abstract HashSet<string> Extensions { get; set; }

        protected string? RunDir { get; private set; }

        public virtual void Init()
        {
            this.cache.Init(
                (string u) => Write(u[..Math.Min(u.Length, Console.BufferWidth)]),
                (string err) => Console.WriteLine(err.Pastel(ConsoleColor.DarkRed))
            );
        }

        public async Task Run(string dir, string pattern = "*.y*")
        {
            this.RunDir = dir;

            if (File.Exists(this.RunDir))
            {
                await this.ProcessFile(this.RunDir);
            }
            else if (Directory.Exists(this.RunDir))
            {
                foreach (string file in Directory.EnumerateFiles(dir, pattern, SearchOption.AllDirectories))
                {
                    await this.ProcessFile(file);
                }

                Write("Processed.".Pastel(ConsoleColor.DarkGreen));
            }
            else
            {
                throw new EntryPointNotFoundException($"Directory/File doesn't exist: \"{dir}\"!");
            }

            Console.WriteLine();

            await this.FinishAsync();
        }

        protected abstract Task FinishAsync();

        protected abstract Task HandleFileAsync(string file);

        protected string ShortenFilePath(string path) => path == this.RunDir ? Path.GetFileName(path) : $".{path[this.RunDir!.Length..]}";

        protected static void Write(string text)
        {
            Console.ResetColor();
            Console.Write($"\r{text.PadRight(Console.BufferWidth)}");
        }

        private async Task ProcessFile(string file)
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
                string shortened = ShortenFilePath(file);
                if ((Console.BufferWidth > 0) && (shortened.Length > (Console.BufferWidth - 15)))
                {
                    shortened = shortened[(shortened.Length - Console.BufferWidth - 15)..];
                }
                Write($"Processing {shortened.Pastel(ConsoleColor.DarkCyan)}...");
                JenkIndex.Ensure(Path.GetFileName(file));
                JenkIndex.Ensure(Path.GetFileNameWithoutExtension(file));

                await this.HandleFileAsync(file);
            }
        }
    }
}
