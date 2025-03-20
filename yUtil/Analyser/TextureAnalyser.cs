namespace yUtil.Analyser
{
    using CodeWalker.GameFiles;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using yUtil;

    internal class TextureAnalyser(YCache cache) : Analyser(cache)
    {
        public override HashSet<string> SupportedExtensions =>
        [
            ".ydr",
            ".ydd",
            ".yft",
            ".ytd",
        ];

        public override async Task AnalyseAsync(string file)
        {
            string ext = Path.GetExtension(file)[1..];
            GameFile gameFile = ext switch
            {
                "ydd" => new YddFile(),
                "ydr" => new YdrFile(),
                "yft" => new YftFile(),
                "ytd" => new YtdFile(),
                _ => throw new InvalidOperationException($"Missing gamefile assignment for \"{ext}\"!"),
            };

            await gameFile.LoadFileAsync(file, this.cache);

            if (!gameFile.Loaded)
            {
                return;
            }

            switch (gameFile.Type)
            {
                case GameFileType.Ydd:
                    foreach (Drawable drawable in ((YddFile)gameFile).Drawables)
                    {
                        this.AnalyseTextures(file, drawable.ShaderGroup?.TextureDictionary?.Dict.Values);
                    }
                    break;
                case GameFileType.Ydr:
                    this.AnalyseTextures(file, ((YdrFile)gameFile).Drawable.ShaderGroup?.TextureDictionary?.Dict.Values);
                    break;
                case GameFileType.Yft:
                    this.AnalyseTextures(file, ((YftFile)gameFile).Fragment.Drawable.ShaderGroup?.TextureDictionary?.Dict.Values);
                    break;
                case GameFileType.Ytd:
                    this.AnalyseTextures(file, ((YtdFile)gameFile).TextureDict?.Dict.Values);
                    break;
            }
        }

        private void AnalyseTextures(string file, IEnumerable<Texture>? textures)
        {
            if (textures == null)
            {
                return;
            }

            foreach (Texture texture in textures)
            {
                bool isScriptDial = (texture.Format is TextureFormat.D3DFMT_A8R8G8B8 or TextureFormat.D3DFMT_A8B8G8R8) && texture.Name.StartsWith("script_rt_", StringComparison.Ordinal);

                if (texture.Height > 1024 || texture.Width > 1024)
                {
                    this.AddIssue(IssueSeverity.Warn, file, $"Size: {texture.Name}.dds ({texture.Width}x{texture.Height})");
                }

                if (!IsPowerOfTwo(texture.Height) || !IsPowerOfTwo(texture.Width))
                {
                    this.AddIssue(IssueSeverity.Error, file, $"Dimensions: {texture.Name}.dds ({texture.Width}x{texture.Height})");
                }

                if ((texture.Format is not TextureFormat.D3DFMT_DXT1 and not TextureFormat.D3DFMT_DXT5) && !isScriptDial)
                {
                    this.AddIssue(IssueSeverity.Warn, file, $"Format: {texture.Name}.dds ({texture.Format})");
                }

                double requiredLevels = Math.Ceiling(Math.Log2(Math.Min(texture.Width, texture.Height))) - 1;

                if (texture.Levels < requiredLevels && !isScriptDial)
                {
                    this.AddIssue(IssueSeverity.Error, file, $"Missing Mipmaps: {texture.Name}.dds ({texture.Levels}/{requiredLevels})");
                }
                else if (texture.Levels > requiredLevels)
                {
                    this.AddIssue(IssueSeverity.Error, file, $"Excessive Mipmaps: {texture.Name}.dds ({texture.Levels}/{requiredLevels})");
                }

                if (texture.MemoryUsage > 10000000f)
                {
                    this.AddIssue(IssueSeverity.Info, file, $"Memory Usage: {texture.Name}.dds ({texture.MemoryUsage / 10000000f:F2}MB)");
                }
            }
        }

        private static bool IsPowerOfTwo(ushort x) => (x != 0) && ((x & (x - 1)) == 0);
    }
}
