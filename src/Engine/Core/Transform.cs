using System;
using System.Collections.Generic;
using OpenTK.Mathematics;

namespace MiniEngine.Core
{
    public sealed class Transform
    {
		private Vector3 m_localPosition;
		private Quaternion m_localRotation;
		private Vector3 m_localScale;
		private Vector3 m_previousPosition;
        private Vector3 m_velocity;
		private Vector3 m_eulerRotation;
		private Transform m_parent;
		private Transform m_root;
		private List<Transform> m_children;
		private Matrix4 m_cachedModelMatrix;
        private ulong m_lastFrameCount;
		private bool m_isDirty;
        private Stack<Transform> m_stack;

        public Vector3 position
        {
            get
            {
                return GetModelMatrix().ExtractTranslation();
            }
            set
            {
                if (m_parent != null)
                {
                    Matrix4 parentMatrix = m_parent.GetModelMatrix();
                    Matrix4 invParentMatrix = parentMatrix.Inverted();
                    Vector4 positionV4 = new Vector4(value, 1.0f);
                    Vector4 transformed = positionV4 * invParentMatrix;
                    m_localPosition = transformed.Xyz;
                }
                else
                {
                    m_localPosition = value;
                }

                MarkDirty();
            }
        }

        public Vector3 localPosition
        {
            get
            {
                return m_localPosition;
            }
            set
            {
                m_localPosition = value;

                MarkDirty();
            }
        }

        public Quaternion rotation
        {
            get
            {
                if (m_parent != null)
                {
                    return m_parent.rotation * m_localRotation;
                }
                else
                {
                    return m_localRotation;
                }
            }
            set
            {
                if (m_parent != null)
                {
                    Quaternion parentRotation = m_parent.rotation;
                    m_localRotation = value * parentRotation.Inverted();
                }
                else
                {
                    m_localRotation = value;
                }

                MarkDirty();
            }
        }

        public Quaternion localRotation
        {
            get
            {
                return m_localRotation;
            }
            set
            {
                m_localRotation = value;
                MarkDirty();                
            }
        }

        public Vector3 scale
        {
            get
            {
                return m_localScale;
            }
            set
            {
                m_localScale = value;
                MarkDirty();
            }
        }

        public Vector3 forward
        {
            get
            {
                Vector3 direction = new Vector3(0.0f, 0.0f, -1.0f);
                return Vector3.Transform(direction, rotation);
            }
        }

        public Vector3 right
        {
            get
            {
                Vector3 direction = new Vector3(1.0f, 0.0f, 0.0f);
                return Vector3.Transform(direction, rotation);
            }
        }

        public Vector3 up
        {
            get
            {
                Vector3 direction = new Vector3(0.0f, 1.0f, 0.0f);
                return Vector3.Transform(direction, rotation);
            }
        }

        public Vector3 velocity
        {
            get
            {
                // Use a static frame counter (e.g., Time.FrameCount) to see if we've 
                // already sampled the "previous" state for this specific frame.
                if (m_lastFrameCount != Time.FrameCount)
                {
                    float deltaTime = Time.DeltaTime;
                    Vector3 currentPosition = position;

                    if (deltaTime > 0.0001f)
                        m_velocity = (currentPosition - m_previousPosition) / deltaTime;
                    else
                        m_velocity = Vector3.Zero;

                    // Update trackers for the NEXT time velocity is requested in a NEW frame
                    m_previousPosition = currentPosition;
                    m_lastFrameCount = Time.FrameCount;
                }

                return m_velocity;
            }
        }

        public Transform parent
        {
            get
            {
                return m_parent;
            }
            set
            {
                SetParent(value);
            }
        }

        public Transform root
        {
            get
            {
                return m_root;
            }
        }

        public List<Transform> children
        {
            get
            {
                return m_children;
            }
        }

        public Transform()
        {
            m_children = new List<Transform>();
            m_localPosition = new Vector3(0, 0, 0);
            m_localRotation = new Quaternion(0.0f, 0.0f, 0.0f, 1.0f);
            m_localScale = new Vector3(1, 1, 1);
            m_velocity = new Vector3(0, 0, 0);
            m_previousPosition = new Vector3(0, 0, 0);
            m_eulerRotation = new Vector3(0, 0, 0);
            m_parent = null;
            m_root = this;
            m_lastFrameCount = ulong.MaxValue;
            m_isDirty = true;
            m_stack = new Stack<Transform>();
        }

        public Matrix4 GetModelMatrix()
        {
            if (m_isDirty)
            {
                RecalculateModelMatrix();
            }
            return m_cachedModelMatrix;
        }

        public void SetParent(Transform newParent)
        {
            // Prevent being able to set the same parent as the current
            if (m_parent == newParent)
                return;

            // Can't set parent to itself
            if (newParent == this)
                return;

            // Save current world Transform (using the OpenTK methods we ported)
            Vector3 worldPos = position;
            Quaternion worldRot = rotation;

            // Remove from old parent if any
            if (m_parent != null)
            {
                m_parent.RemoveChild(this);
            }

            // Assign new parent
            m_parent = newParent;

            // Add to new parent's children if not null
            if (m_parent != null)
            {
                m_parent.AddChild(this);
            }

            // Preserve world Transform
            // These methods internally calculate the new m_localPosition 
            // and m_localRotation relative to the new parent.
            position = worldPos;
            rotation = worldRot;

            // Recalculate root
            m_root = this;
            Transform current = m_parent;
            while (current != null)
            {
                m_root = current;
                current = current.m_parent;
            }

            MarkDirty();
        }

        public Transform GetChild(int index)
        {
            if(index >= m_children.Count)
                return null;
            return m_children[index];
        }

        public void Rotate(Quaternion rotation)
        {
            this.rotation = rotation;
        }

        public void Rotate(Vector3 rotation)
        {
            Vector3 currentRotation = this.rotation.ToEulerAngles();

            m_eulerRotation.X += rotation.X;
            m_eulerRotation.Y += rotation.Y;
            m_eulerRotation.Z += rotation.Z;

            var rotationDelta = m_eulerRotation - currentRotation;

            var rott = this.rotation;

            const float Deg2Rad = (float)Math.PI / 180.0f;

            rott = rott * Quaternion.FromAxisAngle(Vector3.UnitX, rotationDelta.X * Deg2Rad);
            rott = rott * Quaternion.FromAxisAngle(Vector3.UnitY, rotationDelta.Y * Deg2Rad);
            rott = rott * Quaternion.FromAxisAngle(Vector3.UnitZ, -rotationDelta.Z * Deg2Rad);

            this.rotation = rott;
        }

        public void LookAt(Transform target)
        {
            LookAt(target, Vector3.UnitY);
        }

        public void LookAt(Transform target, Vector3 worldUp)
        {
            if (target == null)
                return;
            LookAt(target.position, worldUp);
        }

        public void LookAt(Vector3 worldPosition, Vector3 worldUp)
        {
            Vector3 pos = position;
            Matrix4 mat = Matrix4.LookAt(pos, worldPosition, worldUp);
            Quaternion rot = Quaternion.FromMatrix(new Matrix3(mat));
            Rotate(rot);
        }

        public Vector3 InverseTransformDirection(Vector3 direction)
        {
            var dir = direction * new Matrix3(GetModelMatrix().Inverted());
            return new Vector3(dir.X, dir.Y, dir.Z);
        }

        public Vector3 TransformDirection(Vector3 direction)
        {
            Vector3 v = rotation * direction;
            return v;
        }

        public static void TransformDirection(ref Vector3 vector, ref Quaternion rotation, out Vector3 result)
        {
            float x = rotation.X + rotation.X;
            float y = rotation.Y + rotation.Y;
            float z = rotation.Z + rotation.Z;
            float wx = rotation.W * x;
            float wy = rotation.W * y;
            float wz = rotation.W * z;
            float xx = rotation.X * x;
            float xy = rotation.X * y;
            float xz = rotation.X * z;
            float yy = rotation.Y * y;
            float yz = rotation.Y * z;
            float zz = rotation.Z * z;

            result = new Vector3(vector.X * (1.0f - yy - zz) + vector.Y * (xy - wz) + vector.Z * (xz + wy),
                                 vector.X * (xy + wz) + vector.Y * (1.0f - xx - zz) + vector.Z * (yz - wx),
                                 vector.X * (xz - wy) + vector.Y * (yz + wx) + vector.Z * (1.0f - xx - yy));
        }

        public Vector3 WorldToLocal(Vector3 v)
        {
            var invScale = scale;
            if (invScale.X != 0.0f)
                invScale.X = 1.0f / invScale.X;
            if (invScale.Y != 0.0f)
                invScale.Y = 1.0f / invScale.Y;
            if (invScale.Z != 0.0f)
                invScale.Z = 1.0f / invScale.Z;
            Quaternion invRotation = Quaternion.Conjugate(rotation);
            Vector3 result = v - position;
            result = Vector3.Transform(result, invRotation);
            result *= invScale;
            return result;
        }

        public Vector3 WorldToLocalVector(Vector3 v)
        {
            var invScale = scale;
            if (invScale.X != 0.0f)
                invScale.X = 1.0f / invScale.X;
            if (invScale.Y != 0.0f)
                invScale.Y = 1.0f / invScale.Y;
            if (invScale.Z != 0.0f)
                invScale.Z = 1.0f / invScale.Z;
            Quaternion invRotation = Quaternion.Conjugate(rotation);
            Vector3 result = Vector3.Transform(v, invRotation);
            result *= invScale;
            return result;
        }

        public Vector3 LocalToWorld(Vector3 v)
        {
            Vector3 tmp = v * scale;
            tmp = Vector3.Transform(tmp, rotation);
            return tmp + position;
        }

        public Vector3 LocalToWorldVector(Vector3 v)
        {
            Vector3 tmp = v * scale;
            return Vector3.Transform(tmp, rotation);
        }

		private void MarkDirty()
        {
            if (m_isDirty)
                return;

            m_isDirty = true;
            
            m_stack.Push(this);

            while (m_stack.Count > 0)
            {
                Transform current = m_stack.Pop();

                for(int i = 0; i < current.m_children.Count; i++)
                {
                    if (!current.m_children[i].m_isDirty)
                    {
                        current.m_children[i].m_isDirty = true;
                        m_stack.Push(current.m_children[i]);
                    }
                }
            }
        }

		private void RecalculateModelMatrix()
        {
            Matrix4 translation = Matrix4.CreateTranslation(m_localPosition);
            Matrix4 rotation = Matrix4.CreateFromQuaternion(m_localRotation);
            Matrix4 scale = Matrix4.CreateScale(m_localScale);

            Matrix4 localMatrix = scale * rotation * translation;

            if (m_parent != null)
            {
                m_cachedModelMatrix = localMatrix * m_parent.GetModelMatrix();
            }
            else
            {
                m_cachedModelMatrix = localMatrix;
            }

            m_isDirty = false;
        }

		private void AddChild(Transform child)
        {
            m_children.Add(child);
        }

		private void RemoveChild(Transform child)
        {
            m_children.Remove(child);
        }
    }
}