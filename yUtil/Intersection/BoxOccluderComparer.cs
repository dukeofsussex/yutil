namespace yUtil.Intersection
{
    using CodeWalker.GameFiles;
    using System.Collections.Generic;

    internal class BoxOccluderComparer : IEqualityComparer<YmapBoxOccluder>
    {
        public bool Equals(YmapBoxOccluder? left, YmapBoxOccluder? right)
        {
            return left != null && right != null
                && left.Position == right.Position
                && left.Orientation == right.Orientation
                && left.BBMax == right.BBMax
                && left.BBMin == right.BBMin;
        }

        public int GetHashCode(YmapBoxOccluder boxOccluder)
        {
            return boxOccluder.Box.GetHashCode();
        }
    }
}