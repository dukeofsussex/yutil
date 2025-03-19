namespace yUtil.Analyser
{
    internal interface IAnalyser
    {
        Dictionary<string, List<Issue>> Issues { get; }

        HashSet<string> SupportedExtensions { get; }

        Task AnalyseAsync(string file);
    }
}
