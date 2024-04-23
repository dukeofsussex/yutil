namespace yUtil.Intersection
{
    using CodeWalker.GameFiles;
    using SharpDX;
    using System.Collections.Generic;

    internal class BoxOccluderComparer : IEqualityComparer<YmapBoxOccluder>
    {
        public bool Equals(YmapBoxOccluder? left, YmapBoxOccluder? right)
        {
            return left != null && right != null
                && MathUtil.WithinEpsilon(left.Box.iCenterX, right.Box.iCenterX, 1)
                && MathUtil.WithinEpsilon(left.Box.iCenterY, right.Box.iCenterY, 1)
                && MathUtil.WithinEpsilon(left.Box.iCenterZ, right.Box.iCenterZ, 1)
                && MathUtil.WithinEpsilon(left.Box.iCosZ, right.Box.iCosZ, 1)
                && MathUtil.WithinEpsilon(left.Box.iHeight, right.Box.iHeight, 1)
                && MathUtil.WithinEpsilon(left.Box.iLength, right.Box.iLength, 1)
                && MathUtil.WithinEpsilon(left.Box.iSinZ, right.Box.iSinZ, 1)
                && MathUtil.WithinEpsilon(left.Box.iWidth, right.Box.iWidth, 1);
        }

        public int GetHashCode(YmapBoxOccluder boxOccluder)
        {
            return (boxOccluder.Position, boxOccluder.Orientation).GetHashCode();
        }
    }
}