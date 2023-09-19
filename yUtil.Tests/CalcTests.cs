namespace yUtil.Tests
{
    using CodeWalker.GameFiles;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using yUtil;

    [TestClass]
    public class CalcTests : TestBase
    {
        [TestMethod]
        public async Task Calc_Extents()
        {
            YmapFile ymapFile = new();
            await ymapFile.LoadFileAsync("./tests/calc.ymap", this.cache);

            Assert.IsTrue(ymapFile.Loaded);
            Assert.IsNotNull(ymapFile);
            Assert.IsFalse(ymapFile.CalcExtents());
        }
    }
}