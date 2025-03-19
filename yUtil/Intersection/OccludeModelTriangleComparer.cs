namespace yUtil.Intersection
{
    using CodeWalker.GameFiles;
    using System.Collections.Generic;

    internal class OccludeModelTriangleComparer : IEqualityComparer<YmapOccludeModelTriangle>
    {
        public bool Equals(YmapOccludeModelTriangle? left, YmapOccludeModelTriangle? right)
        {
            return left != null && right != null
                && left.Corner1 == right.Corner1
                && left.Corner2 == right.Corner2
                && left.Corner3 == right.Corner3
                && left.Scale == right.Scale;
        }

        public int GetHashCode(YmapOccludeModelTriangle occludeModelTriangle)
        {
            return (occludeModelTriangle.Corner1, occludeModelTriangle.Corner2, occludeModelTriangle.Corner3).GetHashCode();
        }
    }
}
