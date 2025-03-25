namespace yUtil.Analyser
{
    using CodeWalker.GameFiles;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using yUtil;

    internal class TextureAnalyser(YCache cache) : Analyser(cache)
    {
        private const int MIN_MIPMAP_PX = 4;
        private const int MAX_MIPMAP_PX = 1024;

        public override HashSet<string> SupportedExtensions =>
        [
            ".ydd",
            ".ydr",
            ".yft",
            ".ytd",
        ];

        public override async Task AnalyseAsync(string file)
        {
            string ext = Path.GetExtension(file);
            GameFile gameFile = ext switch
            {
                ".ydd" => new YddFile(),
                ".ydr" => new YdrFile(),
                ".yft" => new YftFile(),
                ".ytd" => new YtdFile(),
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
                bool permittedRawTexture = (texture.Format is TextureFormat.D3DFMT_A8R8G8B8 or TextureFormat.D3DFMT_A8B8G8R8)
                    && (texture.Name.StartsWith("script_rt_", StringComparison.Ordinal) || texture.Name.EndsWith("_pal", StringComparison.Ordinal));

                if (texture.Height < MIN_MIPMAP_PX || texture.Width < MIN_MIPMAP_PX)
                {
                    this.AddIssue(IssueSeverity.Error, file, $"Size: {texture.Name}.dds ({texture.Width}x{texture.Height})");
                }
                else if (texture.Height > MAX_MIPMAP_PX || texture.Width > MAX_MIPMAP_PX)
                {
                    this.AddIssue(IssueSeverity.Warn, file, $"Size: {texture.Name}.dds ({texture.Width}x{texture.Height})");
                }

                if (!IsEven(texture.Height) || !IsEven(texture.Width))
                {
                    this.AddIssue(IssueSeverity.Error, file, $"Dimensions: {texture.Name}.dds ({texture.Width}x{texture.Height})");
                }
                else if (!IsPowerOfTwo(texture.Height) || !IsPowerOfTwo(texture.Width))
                {
                    this.AddIssue(IssueSeverity.Warn, file, $"Dimensions: {texture.Name}.dds ({texture.Width}x{texture.Height})");
                }

                if ((texture.Format is not TextureFormat.D3DFMT_DXT1 and not TextureFormat.D3DFMT_DXT5) && !permittedRawTexture)
                {
                    this.AddIssue(IssueSeverity.Warn, file, $"Format: {texture.Name}.dds ({texture.Format})");
                }

                double requiredLevels = CalculateMipMapLevels(Math.Min(texture.Width, texture.Height));

                if (texture.Levels < requiredLevels && !permittedRawTexture)
                {
                    this.AddIssue(IssueSeverity.Error, file, $"Missing Mipmaps: {texture.Name}.dds ({texture.Levels}/{requiredLevels})");
                }
                else if (texture.Levels > requiredLevels)
                {
                    this.AddIssue(IssueSeverity.Error, file, $"Excessive Mipmaps: {texture.Name}.dds ({texture.Levels}/{requiredLevels})");
                }

                if (texture.MemoryUsage > 10000000f)
                {
                    this.AddIssue(IssueSeverity.Warn, file, $"Memory Usage: {texture.Name}.dds ({texture.MemoryUsage / 10000000f:F2}MB)");
                }
            }
        }

        private static int CalculateMipMapLevels(ushort dimensions)
        {
            if (IsPowerOfTwo(dimensions))
            {
                return (int)(Math.Ceiling(Math.Log2(dimensions)) - 1);
            }

            int count = 0;
            double step = dimensions;

            do
            {
                count++;
                step /= 2;
            }
            while (IsEven(step) && dimensions > MIN_MIPMAP_PX);

            return count;
        }

        private static bool IsEven(double x) => x % 2 == 0;

        private static bool IsPowerOfTwo(ushort x) => (x != 0) && ((x & (x - 1)) == 0);
    }
}
