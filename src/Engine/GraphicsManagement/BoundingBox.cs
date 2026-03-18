using System;
using OpenTK.Mathematics;

namespace MiniEngine.GraphicsManagement
{
    public struct BoundingBox
    {
        private Vector3 min;
        private Vector3 max;
        private Vector3 center;
        private Vector3 extents;
        private bool hasPoint;

        public Vector3 Min
        {
            get
            {
                return min;
            }
        }

        public Vector3 Max
        {
            get
            {
                return max;
            }
        }

        public Vector3 Center
        {
            get
            {
                return center;
            }
        }

        public Vector3 Extents
        {
            get
            {
                return extents;
            }
        }

        public bool HasPoint
        {
            get
            {
                return hasPoint;
            }
        }

        public Vector3 Size
        {
            get
            {
                return max - min;
            }
        }

        public BoundingBox()
        {
            Clear();
        }

        public BoundingBox(Vector3 min, Vector3 max)
        {
            Clear();
            this.min = min;
            this.max = max;
            center = (min + max) * 0.5f;
            extents = max - center;
            hasPoint = true;
        }

        public void Clear()
        {
            min = Vector3.One * float.PositiveInfinity;
            max = Vector3.One * float.NegativeInfinity;
            center = (min + max) * 0.5f;
            extents = max - center;
            hasPoint = false;
        }

        public void Grow(Vector3 point)
        {
            min = Vector3Min(min, point);
            max = Vector3Max(max, point);
            center = (min + max) * 0.5f;
            extents = max - center;
            hasPoint = true;
        }

        public void Grow(Vector3 min, Vector3 max)
        {
            if (hasPoint)
            {
                this.min.X = min.X < this.min.X ? min.X : this.min.X;
                this.min.Y = min.Y < this.min.Y ? min.Y : this.min.Y;
                this.min.Z = min.Z < this.min.Z ? min.Z : this.min.Z;
                this.max.X = max.X > this.max.X ? max.X : this.max.X;
                this.max.Y = max.Y > this.max.Y ? max.Y : this.max.Y;
                this.max.Z = max.Z > this.max.Z ? max.Z : this.max.Z;
            }
            else
            {
                hasPoint = true;
                this.min = min;
                this.max = max;
            }
        }

        public void Transform(Matrix4 transformation)
        {
            var vMin = new Vector4(min.X, min.Y, min.Z, 1.0f) * transformation;
            var vMax = new Vector4(max.X, max.Y, max.Z, 1.0f) * transformation;
            min = vMin.Xyz;
            max = vMax.Xyz;
            center = (min + max) * 0.5f;
            extents = max - center;
        }

        private bool IsFloatIsZero(float value)
        {
            const float zeroTolerance = 1e-6f;
            return Math.Abs(value) < zeroTolerance;
        }

        private static Vector3 Vector3Min(Vector3 a, Vector3 b)
        {
            return new Vector3(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Min(a.Z, b.Z));
        }

        private static Vector3 Vector3Max(Vector3 a, Vector3 b)
        {
            return new Vector3(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y), Math.Max(a.Z, b.Z));
        }
    }
}