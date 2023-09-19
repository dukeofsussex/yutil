namespace yUtil.Tests
{
    using CodeWalker.GameFiles;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using SharpDX;
    using yUtil.Intersection;

    [TestClass]
    public class IntersectionTests : TestBase
    {
        private readonly IntersectJob intersectJob = new("./tests/output", string.Empty);

        [TestMethod]
        public async Task Intersect_Box_Occluders()
        {
            LazyYmapFile initial = new()
            {
                FilePath = "./tests/intersect.ymap"
            };
            LazyYmapFile next = new()
            {
                FilePath = "./tests/intersect_boxoccluders.ymap"
            };

            await this.intersectJob.IntersectYmaps(initial, next);
            Assert.IsNotNull(next.YmapFile);
            Assert.IsNotNull(next.YmapFile.BoxOccluders);

            Assert.IsNotNull(initial.YmapFile);
            Assert.IsTrue(initial.YmapFile.BoxOccluders.Length == 2);
            Assert.IsTrue(initial.YmapFile.BoxOccluders[0].Position == (new Vector3(1, 1, 1) / 4f));
            Assert.IsTrue(initial.YmapFile.BoxOccluders[1].Position == (new Vector3(3, 3, 3) / 4f));
        }

        [TestMethod]
        public async Task Intersect_Car_Generators()
        {
            LazyYmapFile initial = new()
            {
                FilePath = "./tests/intersect.ymap"
            };
            LazyYmapFile next = new()
            {
                FilePath = "./tests/intersect_cargens.ymap"
            };

            await this.intersectJob.IntersectYmaps(initial, next);
            Assert.IsNotNull(next.YmapFile);
            Assert.IsNotNull(next.YmapFile.CarGenerators);

            Assert.IsNotNull(initial.YmapFile);
            Assert.IsTrue(initial.YmapFile.CarGenerators.Length == 2);
            Assert.IsTrue(initial.YmapFile.CarGenerators[0].CCarGen.carModel == JenkHash.GenHash("adder"));
            Assert.IsTrue(initial.YmapFile.CarGenerators[1].CCarGen.carModel == JenkHash.GenHash("zentorno"));
        }

        [TestMethod]
        public async Task Intersect_Entities()
        {
            LazyYmapFile initial = new()
            {
                FilePath = "./tests/intersect.ymap"
            };
            LazyYmapFile next = new()
            {
                FilePath = "./tests/intersect_entities.ymap"
            };

            await this.intersectJob.IntersectYmaps(initial, next);
            Assert.IsNotNull(next.YmapFile);
            Assert.IsNotNull(next.YmapFile.AllEntities);

            Assert.IsNotNull(initial.YmapFile);
            Assert.IsTrue(initial.YmapFile.AllEntities[0].Position.Z == 0);
            Assert.IsTrue(initial.YmapFile.AllEntities[0].Scale == Vector3.Zero);
            Assert.IsFalse(initial.YmapFile.CMapData.entitiesExtentsMax == new Vector3(float.MinValue));
            Assert.IsFalse(initial.YmapFile.CMapData.entitiesExtentsMin == new Vector3(float.MaxValue));
            Assert.IsFalse(initial.YmapFile.CMapData.streamingExtentsMax == new Vector3(float.MinValue));
            Assert.IsFalse(initial.YmapFile.CMapData.streamingExtentsMin == new Vector3(float.MaxValue));
        }

        [TestMethod]
        public async Task Intersect_Empty()
        {
            LazyYmapFile initial = new()
            {
                FilePath = "./tests/intersect.ymap"
            };
            LazyYmapFile next = new()
            {
                FilePath = "./tests/intersect_empty.ymap"
            };

            await this.intersectJob.IntersectYmaps(initial, next);
            Assert.IsNotNull(next.YmapFile);
            Assert.IsNull(next.YmapFile.AllEntities);
            Assert.IsNull(next.YmapFile.CarGenerators);

            Assert.IsNotNull(initial.YmapFile);
            Assert.IsNull(initial.YmapFile.AllEntities);
            Assert.IsNull(initial.YmapFile.CarGenerators);
            Assert.IsTrue(initial.YmapFile.CMapData.entitiesExtentsMax == new Vector3(float.MinValue));
            Assert.IsTrue(initial.YmapFile.CMapData.entitiesExtentsMin == new Vector3(float.MaxValue));
            Assert.IsTrue(initial.YmapFile.CMapData.streamingExtentsMax == new Vector3(float.MinValue));
            Assert.IsTrue(initial.YmapFile.CMapData.streamingExtentsMin == new Vector3(float.MaxValue));
        }
    }
}