namespace yUtil.Intersection
{
    using CodeWalker.GameFiles;
    using System.Collections.Generic;

    internal class CarGenComparer : IEqualityComparer<YmapCarGen>
    {
        public bool Equals(YmapCarGen? left, YmapCarGen? right)
        {
            return left != null && right != null
                && left.CCarGen.carModel == right.CCarGen.carModel
                && left.CCarGen.position == right.CCarGen.position;
        }

        public int GetHashCode(YmapCarGen carGen)
        {
            return carGen.CCarGen.GetHashCode();
        }
    }
}