namespace yUtil.Tests
{
    using dotenv.net;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;

    public abstract class TestBase
    {
        internal readonly YCache cache = new();

        [TestInitialize]
        public void Init()
        {
            DotEnv.Load();
            cache.Init(
                Environment.GetEnvironmentVariable("GTA_DIR"),
                (_) => {
                    return;
                }, (_) =>
                {
                    return;
                });

        }
    }
}
