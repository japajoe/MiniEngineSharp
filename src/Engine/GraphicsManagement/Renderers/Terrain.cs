using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using MiniEngine.Core;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace MiniEngine.GraphicsManagement.Renderers
{
    public sealed class Terrain : Renderer
    {
		public enum FilterMode
		{
			Bilinear,
			Point
		}

		public enum  HeightMode
		{
			Overwrite,
			Additive
		}

		public enum FalloffMode
		{
			None,
			Linear
		}

        private int m_vao;
        private int m_vbo;
        private int m_ebo;
        private List<Vertex> m_vertices;
        private List<int> m_indices;
        private float m_scale;
        private int m_resolution;
        private Texture2D m_textureSplat;
        private Texture2D[] m_textures;
        private Vector2[] m_textureTilling;
        private Color m_color;
        private bool m_emissive;
        private float m_emissionFactor;
        private float m_brightnessThreshold;
        private float m_ambientOcclusion;
        private float m_metallic;
        private float m_roughness;
        private int uTextureSplat;
        private int uTexture0;
        private int uTexture1;
        private int uTexture2;
        private int uTexture3;
        private int uTextureTiling0;
        private int uTextureTiling1;
        private int uTextureTiling2;
        private int uTextureTiling3;
        private static Shader shader;
        private static Shader shadowDepthShader;

        public int Resolution => m_resolution;
        public Texture2D SplatTexture
        {
            get => m_textureSplat;
            set => m_textureSplat = value;
        }

        public Texture2D Texture1
        {
            get => m_textures[0];
            set => m_textures[0] = value;
        }

        public Texture2D Texture2
        {
            get => m_textures[1];
            set => m_textures[1] = value;
        }

        public Texture2D Texture3
        {
            get => m_textures[2];
            set => m_textures[2] = value;
        }

        public Texture2D Texture4
        {
            get => m_textures[3];
            set => m_textures[3] = value;
        }

        public Vector2 Texture1Tilling
        {
            get => m_textureTilling[0];
            set => m_textureTilling[0] = value;
        }

        public Vector2 Texture2Tilling
        {
            get => m_textureTilling[1];
            set => m_textureTilling[1] = value;
        }

        public Vector2 Texture3Tilling
        {
            get => m_textureTilling[2];
            set => m_textureTilling[2] = value;
        }

        public Vector2 Texture4Tilling
        {
            get => m_textureTilling[3];
            set => m_textureTilling[3] = value;
        }



        public Terrain() : base()
        {
            m_vao = 0;
            m_vbo = 0;
            m_ebo = 0;
            m_scale = 1024.0f;
            m_resolution = 512;
            m_emissive = false;
            m_emissionFactor = 1.0f;
            m_brightnessThreshold = 1.0f;
            m_ambientOcclusion = 10.0f;
            m_metallic = 0.0f;
            m_roughness = 0.0f;
            m_color = Color.White;

            m_vertices = new List<Vertex>();
            m_indices = new List<int>();
            m_textures = new Texture2D[4];
            m_textureTilling = new Vector2[4];

            if(shader == null)
                shader = Graphics.GetShader(ShaderName.Terrain);

            if(shadowDepthShader == null)
                shadowDepthShader = Graphics.GetShader(ShaderName.ShadowDepth);

            uTextureSplat = GL.GetUniformLocation(shader.Id, "uTextureSplat");
            uTexture0 = GL.GetUniformLocation(shader.Id, "uTexture0");
            uTexture1 = GL.GetUniformLocation(shader.Id, "uTexture1");
            uTexture2 = GL.GetUniformLocation(shader.Id, "uTexture2");
            uTexture3 = GL.GetUniformLocation(shader.Id, "uTexture3");
            uTextureTiling0 = GL.GetUniformLocation(shader.Id, "uTextureTiling0");
            uTextureTiling1 = GL.GetUniformLocation(shader.Id, "uTextureTiling1");
            uTextureTiling2 = GL.GetUniformLocation(shader.Id, "uTextureTiling2");
            uTextureTiling3 = GL.GetUniformLocation(shader.Id, "uTextureTiling3");
        }

        public void Generate(int resolution, float scale)
        {
            if (m_vao > 0)
                return;

            if (resolution < 2)
                resolution = 2;

            m_scale = scale;
            m_resolution = resolution;
            m_textureSplat = Texture2D.GetDiffuseTexture();

            for (int i = 0; i < 4; i++)
            {
                m_textures[i] = Texture2D.GetDiffuseTexture();
                m_textureTilling[i] = new Vector2(1, 1);
            }

            float offset = (float)resolution * 0.5f * scale;

            for (int z = 0; z <= resolution; z++)
            {
                for (int x = 0; x <= resolution; x++)
                {
                    Vertex vertex = new Vertex();

                    //float posX = ((float)x / (float)resolution - 0.5f) * scale;
                    //float posZ = ((float)z / (float)resolution - 0.5f) * scale;
                    float posX = (float)x * scale - offset;
                    float posZ = (float)z * scale - offset;
                    vertex.position = new Vector3(posX, 0.0f, posZ);

                    vertex.normal = new Vector3(0.0f, 1.0f, 0.0f);

                    float u = (float)x / (float)resolution;
                    float v = (float)z / (float)resolution;
                    vertex.uv = new Vector2(u, v);

                    m_vertices.Add(vertex);
                }
            }

            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    int topLeft = z * (resolution + 1) + x;
                    int topRight = topLeft + 1;
                    int bottomLeft = (z + 1) * (resolution + 1) + x;
                    int bottomRight = bottomLeft + 1;

                    m_indices.Add(topLeft);
                    m_indices.Add(bottomLeft);
                    m_indices.Add(topRight);

                    m_indices.Add(topRight);
                    m_indices.Add(bottomLeft);
                    m_indices.Add(bottomRight);
                }
            }

            var pVertices = CollectionsMarshal.AsSpan(m_vertices);
            var pIndices = CollectionsMarshal.AsSpan(m_indices);

            GL.GenVertexArrays(1, ref m_vao);
            GL.GenBuffers(1, ref m_vbo);
            GL.GenBuffers(1, ref m_ebo);

            GL.BindVertexArray(m_vao);

            GL.BindBuffer(BufferTargetARB.ArrayBuffer, m_vbo);
            GL.BufferData(BufferTargetARB.ArrayBuffer, pVertices, BufferUsageARB.StaticDraw);

            GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, m_ebo);
            GL.BufferData(BufferTargetARB.ElementArrayBuffer, pIndices, BufferUsageARB.StaticDraw);

            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, Marshal.SizeOf(typeof(Vertex)), Marshal.OffsetOf(typeof(Vertex), "position"));

            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, Marshal.SizeOf(typeof(Vertex)), Marshal.OffsetOf(typeof(Vertex), "normal"));

            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, Marshal.SizeOf(typeof(Vertex)), Marshal.OffsetOf(typeof(Vertex), "uv"));

            GL.BindVertexArray(0);
        }

        public void SetHeight(int x, int z, float height, HeightMode mode)
        {
            if(x < 0 || z < 0)
                return;
            if (x > m_resolution || z > m_resolution)
                return;

            int index = z * (m_resolution + 1) + x;
            
            if(mode == HeightMode.Overwrite)
            {
                Vertex v = m_vertices[index];
                v.position.Y = height;
                m_vertices[index] = v;
            }
            else
            {
                Vertex v = m_vertices[index];
                v.position.Y += height;
                m_vertices[index] = v;
            }
        }

        public void CommitChanges() 
        {
            if(m_vao == 0)
                return;
            
            for (int i = 0; i < m_vertices.Count; i++)
            {
                Vertex v = m_vertices[i];
                v.normal = new Vector3(0, 0, 0);
                m_vertices[i] = v;
            }

            for (int i = 0; i < m_indices.Count; i += 3)
            {
                int i0 = m_indices[i];
                int i1 = m_indices[i + 1];
                int i2 = m_indices[i + 2];

                Vector3 v0 = m_vertices[i0].position;
                Vector3 v1 = m_vertices[i1].position;
                Vector3 v2 = m_vertices[i2].position;

                Vector3 edge1 = v1 - v0;
                Vector3 edge2 = v2 - v0;
                Vector3 faceNormal = Vector3.Cross(edge1, edge2);

                Vertex vtx0 = m_vertices[i0];
                Vertex vtx1 = m_vertices[i1];
                Vertex vtx2 = m_vertices[i2];

                vtx0.normal += faceNormal;
                vtx1.normal += faceNormal;
                vtx2.normal += faceNormal;

                m_vertices[i0] = vtx0;
                m_vertices[i1] = vtx1;
                m_vertices[i2] = vtx2;
            }

            for (int i = 0; i < m_vertices.Count; i++)
            {
                Vertex v = m_vertices[i];
                v.normal = Vector3.Normalize(v.normal);
                m_vertices[i] = v;
            }

            var pVertices = CollectionsMarshal.AsSpan(m_vertices);

            GL.BindBuffer(BufferTargetARB.ArrayBuffer, m_vbo);
            GL.BufferData(BufferTargetARB.ArrayBuffer, pVertices, BufferUsageARB.StaticDraw);
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        }

        public override void OnRender(Matrix4 projection, Matrix4 view, Frustum frustum)
        {
            if (!isActive)
                return;

            Matrix4 model = transform.GetModelMatrix();
            Matrix3 modelInverted = Matrix3.Transpose(Matrix3.Invert(new Matrix3(model)));
            Matrix4 mvp = model * view * projection;
            Texture2DArray shadowTexture = Shadow.Texture;

            float ambientOcclusion = m_ambientOcclusion;

            if(Graphics.GetAmbientOcclusionSettings().globalEnabled)
                ambientOcclusion = Graphics.GetAmbientOcclusionSettings().value;
            
            shader.Use();
            shader.SetMat4(UniformName.Model, model);
            shader.SetMat4(UniformName.MVP, mvp);
            shader.SetMat3(UniformName.ModelInverted, modelInverted);
            shader.SetFloat(UniformName.AmbientOcclusion, ambientOcclusion);
            shader.SetFloat(UniformName.Metallic, m_metallic);
            shader.SetFloat(UniformName.Roughness, m_roughness);
            shader.SetFloat4(UniformName.Color, m_color);
            shader.SetInt(UniformName.Emissive, m_emissive ? 1 : -1);
            shader.SetFloat(UniformName.EmissionFactor, m_emissionFactor);
            shader.SetFloat(UniformName.BrightnessThreshold, m_brightnessThreshold);

            int unit = 0;
            if(m_textureSplat != null)
            {
                m_textureSplat.Bind(unit);
                shader.SetIntEx(uTextureSplat, unit);
                unit++;
            }

            Span<int> uniforms = stackalloc int[4];
            uniforms[0] = uTexture0;
            uniforms[1] = uTexture1;
            uniforms[2] = uTexture2;
            uniforms[3] = uTexture3;

            for(int i = 0; i < 4; i++)
            {
                if(m_textures[i] != null)
                {
                    m_textures[i].Bind(unit);
                    shader.SetIntEx(uniforms[i], unit);
                    unit++;
                }
            }

            shadowTexture.Bind(unit);
            shader.SetInt(UniformName.TextureShadow, unit);

            uniforms[0] = uTextureTiling0;
            uniforms[1] = uTextureTiling1;
            uniforms[2] = uTextureTiling2;
            uniforms[3] = uTextureTiling3;
            
            for(int i = 0; i < 4; i++)
            {
                shader.SetFloat2Ex(uniforms[i], m_textureTilling[i]);
            }

            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(TriangleFace.Back);

            GL.BindVertexArray(m_vao);
            GL.DrawElements(PrimitiveType.Triangles, m_indices.Count, DrawElementsType.UnsignedInt, IntPtr.Zero);
            GL.BindVertexArray(0);

        }
    }
}