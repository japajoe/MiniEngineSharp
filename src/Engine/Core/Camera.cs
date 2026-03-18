using System;
using System.Runtime.InteropServices;
using MiniEngine.GraphicsManagement;
using OpenTK.Mathematics;

namespace MiniEngine.Core
{
    public sealed class Camera : Entity
    {
        public static readonly uint UBO_BINDING_INDEX = 1;
        public static readonly string UBO_NAME = "Camera";

        private float fieldOfView;
        private float near;
        private float far;
        private float aspectRatio;
        private int m_screenWidth;
        private int m_screenHeight;
        private Color clearColor;
        private Matrix4 projection;
        private Frustum frustum;

        private static UniformBuffer ubo;

        public float FieldOfView
        {
            get
            {
                return fieldOfView;
            }
            set
            {
                fieldOfView = value;
                Initialize();
            }
        }

        public float NearClippingPlane
        {
            get
            {
                return near;
            }
            set
            {
                near = value;
                Initialize();
            }
        }

        public float FarClippingPlane
        {
            get
            {
                return far;
            }
            set
            {
                far = value;
                Initialize();
            }
        }

        public float AspectRatio
        {
            get => aspectRatio;
        }

        public Color ClearColor
        {
            get
            {
                return clearColor;
            }
            set
            {
                clearColor = value;
            }
        }

        public Frustum Frustum
        {
            get
            {
                return frustum;
            }
        }

        public Camera() : base()
        {
            fieldOfView = 70.0f;
            near = 0.1f;
            far = 1000.0f;
            clearColor = Color.White;
            frustum = new Frustum();
            Initialize();
        }

        public Matrix4 GetProjectionMatrix()
        {
            return projection;
        }

        public Matrix4 GetViewMatrix()
        {
            var m = transform.GetModelMatrix();
            return Matrix4.Invert(m);
        }

        private void Initialize()
        {
            m_screenWidth = Graphics.GetScreenWidth();
            m_screenHeight = Graphics.GetScreenHeight();

            float viewportWidth = (float)m_screenWidth;
            float viewportHeight = (float)m_screenHeight;

            float fov = MathHelper.DegreesToRadians(fieldOfView);
            aspectRatio = viewportWidth / viewportHeight;
            projection = Matrix4.CreatePerspectiveFieldOfView(fov, aspectRatio, near, far);
        }

        private void Update()
        {
            int width = Graphics.GetScreenWidth();
            int height = Graphics.GetScreenHeight();

            if(width != m_screenWidth || height != m_screenHeight)
            {
                m_screenWidth = width;
                m_screenHeight = height;
                Initialize();
            }
            frustum.Initialize(GetViewMatrix() * GetProjectionMatrix());
        }

        internal static void CreateUniformBuffer()
        {
            if(ubo != null)
                return;

            ubo = UniformBuffer.Create<UniformCameraInfo>(UBO_BINDING_INDEX, 1, UBO_NAME);
            ubo.ObjectLabel(UBO_NAME);
        }

        internal static void UpdateUniformBuffer(Camera camera)
        {
            if(ubo == null)
                return;
            
            if(camera == null)
                return;

            camera.Update();

            UniformCameraInfo info = new UniformCameraInfo();
            info.view = camera.GetViewMatrix();
            info.projection = camera.GetProjectionMatrix();
            info.viewProjection = info.view * info.projection;
            info.viewProjectionInverse = Matrix4.Invert(info.view * info.projection);
            info.position = new Vector4(camera.m_transform.position);
            info.direction = new Vector4(camera.m_transform.forward);
            info.resolution = new Vector2(Graphics.GetScreenWidth(),  Graphics.GetScreenHeight());
            info.near = camera.near;
            info.far = camera.far;

            var pInfo = new ReadOnlySpan<UniformCameraInfo>(ref info);
            ubo.Bind();
            ubo.BufferSubData(pInfo, 0);
            ubo.Unbind();
        }

        public static UniformBuffer GetUniformBuffer()
        {
            return ubo;
        }
    }

    public sealed class Frustum
    {
        private Vector4[] planes;

        public Frustum()
        {
            planes = new Vector4[6];
        }
        
        public void Initialize(Matrix4 viewProjection)
        {
            planes[0] = new Vector4(
                viewProjection[0,3] + viewProjection[0,0],
                viewProjection[1,3] + viewProjection[1,0],
                viewProjection[2,3] + viewProjection[2,0],
                viewProjection[3,3] + viewProjection[3,0]
            );

            // Right plane
            planes[1] = new Vector4(
                viewProjection[0,3] - viewProjection[0,0],
                viewProjection[1,3] - viewProjection[1,0],
                viewProjection[2,3] - viewProjection[2,0],
                viewProjection[3,3] - viewProjection[3,0]
            );
            // Bottom plane
            planes[2] = new Vector4(
                viewProjection[0,3] + viewProjection[0,1],
                viewProjection[1,3] + viewProjection[1,1],
                viewProjection[2,3] + viewProjection[2,1],
                viewProjection[3,3] + viewProjection[3,1]
            );

            // // Top plane
            planes[3] = new Vector4(
                viewProjection[0,3] - viewProjection[0,1],
                viewProjection[1,3] - viewProjection[1,1],
                viewProjection[2,3] - viewProjection[2,1],
                viewProjection[3,3] - viewProjection[3,1]
            );

            // // Near plane
            planes[4] = new Vector4(
                viewProjection[0,2],
                viewProjection[1,2],
                viewProjection[2,2],
                viewProjection[3,2]
            );

            // // Far plane
            planes[5] = new Vector4(
                viewProjection[0,3] - viewProjection[0,2],
                viewProjection[1,3] - viewProjection[1,2],
                viewProjection[2,3] - viewProjection[2,2],
                viewProjection[3,3] - viewProjection[3,2]
            );

            // Normalize the planes
            for (int i = 0; i < 6; ++i) 
            {
                planes[i] = Vector4.Normalize(planes[i]);
            }
        }

        public bool Contains(BoundingBox bounds)
        {
            var min = bounds.Min;
            var max = bounds.Max;

            Vector3[] corners = new Vector3[8];
            corners[0] = new Vector3(min.X, min.Y, min.Z);
            corners[1] = new Vector3(max.X, min.Y, min.Z);
            corners[2] = new Vector3(min.X, max.Y, min.Z);
            corners[3] = new Vector3(max.X, max.Y, min.Z);
            corners[4] = new Vector3(min.X, min.Y, max.Z);
            corners[5] = new Vector3(max.X, min.Y, max.Z);
            corners[6] = new Vector3(min.X, max.Y, max.Z);
            corners[7] = new Vector3(max.X, max.Y, max.Z);

            for (int i = 0; i < 6; i++) 
            {
                Vector4 plane = planes[i];
                int vOut = 0;

                // Check all corners against the current frustum plane
                for (int j = 0; j < 8; j++)
                {
                    if (Vector3.Dot(new Vector3(plane.X, plane.Y, plane.Z), corners[j]) + plane.W < 0.0f)
                    {
                        vOut++;
                    }
                    else
                    {
                        break; // Exit early if any corner is inside this plane
                    }
                }

                if (vOut == 8)
                {
                    return false; // All corners are outside this frustum plane
                }
            }

            return true;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct UniformCameraInfo
    {
        public Matrix4 view;
        public Matrix4 projection;
        public Matrix4 viewProjection;
        public Matrix4 viewProjectionInverse;
        public Vector4 position;
        public Vector4 direction;
        public Vector2 resolution;
        public float near;
        public float far;
    }
}