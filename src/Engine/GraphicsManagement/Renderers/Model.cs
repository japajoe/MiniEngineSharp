using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using MiniEngine.Core;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace MiniEngine.GraphicsManagement.Renderers
{
	enum ModelBufferType
	{
		Vertices = 0,
		Normals = 1,
		UVs = 2,
		Indices = 3,
		COUNT
	}

	public class ModelMeshInfo
	{
		public int numVertices;
		public int numIndices;
		public int baseVertex;
		public int baseIndex;
		public int materialIndex;
		public Matrix4 transformation;
		public BoundingBox bounds;
		public string name;
		
        public ModelMeshInfo()
		{
			numVertices = 0;
			numIndices = 0;
			baseVertex = 0;
			baseIndex = 0;
			materialIndex = int.MaxValue; // Invalid
		}
	}

	public class ModelProtoType
	{
		public int vao;
		public int[] buffers;
		public List<ModelMeshInfo> meshes;
		public List<Texture2D> textures;
		public List<string> textureNames;
		public List<Vector3> vertices;
		public List<Vector3> normals;
		public List<Vector2> uvs;
		public List<int> indices;

        public ModelProtoType()
        {
            vao = 0;
            buffers = new int[(int)ModelBufferType.COUNT];
            meshes = new List<ModelMeshInfo>();
            textures = new List<Texture2D>();
            textureNames = new List<string>();
            vertices = new List<Vector3>();
            normals = new List<Vector3>();
            uvs = new List<Vector2>();
            indices = new List<int>();
        }
	}

    public sealed class Model : Renderer
    {
        private ModelProtoType modelProtoType;
		private List<ModelChild> children;
        private static Shader shader = null;
        private static Shader shadowDepthShader = null;

        public Model() : base()
        {
            modelProtoType = null;
            children = new List<ModelChild>();

            if(shader == null)
                shader = Graphics.GetShader(ShaderName.Standard);

            if(shadowDepthShader == null)
                shadowDepthShader = Graphics.GetShader(ShaderName.ShadowDepth);
        }

        public ModelChild GetChild(int index)
        {
            if(index >= children.Count)
                return null;
            return children[index];
        }

        public ModelProtoType GetProtoType()
        {
            return modelProtoType;
        }

        public void Set(ModelProtoType modelProtoType)
        {
            if(modelProtoType == null)
                return;

            this.modelProtoType = modelProtoType;

            if(children.Count == 0)
                children.Clear();

            for(int i = 0; i < modelProtoType.meshes.Count; i++)
                children.Add(new ModelChild());

            var diffuseTexture = Texture2D.GetDiffuseTexture();

            for(int i = 0; i < modelProtoType.meshes.Count; i++)
            {
                ModelChild child = children[i];

                int materialIndex = (int)modelProtoType.meshes[i].materialIndex;
                
                if(materialIndex < modelProtoType.textures.Count)
                {
                    child.Texture = modelProtoType.textures[materialIndex];
                    child.TextureName = modelProtoType.textureNames[materialIndex];
                }
                
                if(child.Texture == null)
                    child.Texture = diffuseTexture;

                Vector3 position = modelProtoType.meshes[i].transformation.ExtractTranslation();
                Quaternion rotation = modelProtoType.meshes[i].transformation.ExtractRotation();
                Vector3 scale = modelProtoType.meshes[i].transformation.ExtractScale();

                child.AlphaBlend = false;
                child.CastShadows = true;
                child.AmbientOcclusion = 10.0f;
                child.Metallic = 0.0f;
                child.Roughness = 0.0f;
                child.Emissive = false;
                child.EmissionFactor = 1.0f;
                child.BrightnessThreshold = 1.0f;
                child.CullFaces = true;
                child.DepthTest = true;
                child.Color = Color.White;
                child.RenderQueue = 1000;
                child.TextureOffset= new Vector2(0.0f, 0.0f);
                child.TextureTiling= new Vector2(1.0f, 1.0f);
                child.transform.position = new Vector3(position.X, position.Y, position.Z);
                child.transform.rotation = rotation;
                child.transform.scale = scale;
                child.transform.parent = transform;
                child.name = modelProtoType.meshes[i].name;
                child.MeshInfoIndex = i;
            }
        }

        public override void OnRenderDepth()
        {
            if(!isActive)
                return;

            if(modelProtoType == null)
                return;

            GL.BindVertexArray(modelProtoType.vao);

            for(int i = 0; i < children.Count; i++)
            {
                ModelChild child = children[i];

                if(child == null)
                    continue;
                
                if(!child.isActive)
                    continue;

                if(!child.CastShadows)
                    continue;

                //int meshIndex = child.MeshInfoIndex;
                int meshIndex = i; // Order doesn't matter with depth rendering

                Matrix4 model = child.transform.GetModelMatrix();
                
                shadowDepthShader.SetMat4(UniformName.Model, model);

                // Need to bind texture in order to properly test for transparent fragments
                Texture2D texture = child.Texture;
                int unit = 0;
                if(texture != null)
                {
                    texture.Bind(unit);
                    shadowDepthShader.SetInt(UniformName.Texture, unit);
                }
               
                GL.Disable(EnableCap.Blend);
                GL.Enable(EnableCap.DepthTest);

                int numIndices = (int)modelProtoType.meshes[meshIndex].numIndices;
                IntPtr offset = new IntPtr(Marshal.SizeOf<uint>() * modelProtoType.meshes[meshIndex].baseIndex);
                int baseVertex = (int)modelProtoType.meshes[meshIndex].baseVertex;
                GL.DrawElementsBaseVertex(PrimitiveType.Triangles, numIndices, DrawElementsType.UnsignedInt, offset, baseVertex);
            }
        }

        public override void OnRender(Matrix4 projection, Matrix4 view, Frustum frustum)
        {
            if(!isActive)
                return;

		    if(modelProtoType == null)
			    return;

            bool globalAO =Graphics.GetAmbientOcclusionSettings().globalEnabled;
            float ao = Graphics.GetAmbientOcclusionSettings().value;

            shader.Use();

            GL.BindVertexArray(modelProtoType.vao);

            for(int i = 0; i < children.Count; i++)
            {
                ModelChild child = children[i];
                
                if(child == null)
                    continue;

                if(!child.isActive)
                    continue;
                
                int meshIndex = child.MeshInfoIndex;

                Matrix4 model = child.transform.GetModelMatrix();
                Matrix3 modelInverted = Matrix3.Transpose(Matrix3.Invert(new Matrix3(model)));
                Matrix4 mvp = model * view * projection;
                Vector2 textureTiling = child.TextureTiling;
                Vector2 textureOffset = child.TextureOffset;
                float ambientOcclusion = child.AmbientOcclusion;
                float metallic = child.Metallic;
                float roughness = child.Roughness;
                bool emissive = child.Emissive;
                float emissionFactor = child.EmissionFactor;
                float brightnessThreshold = child.BrightnessThreshold;
                Color color = child.Color;

                int unit = 0;
                
                if(child.Texture != null)
                {
                    child.Texture.Bind(unit);
                    shader.SetInt(UniformName.Texture, unit);
                    unit++;
                }

                Texture2DArray shadowTexture = Shadow.Texture;

                if(shadowTexture != null)
                {
                    shadowTexture.Bind(unit);
                    shader.SetInt(UniformName.TextureShadow, unit);
                }

                shader.SetMat4(UniformName.Model, model);
                shader.SetMat3(UniformName.ModelInverted, modelInverted);
                shader.SetMat4(UniformName.MVP, mvp);
                shader.SetFloat(UniformName.AmbientOcclusion, globalAO ? ao : ambientOcclusion);
                shader.SetFloat(UniformName.Metallic, metallic);
                shader.SetFloat(UniformName.Roughness, roughness);
                shader.SetFloat2(UniformName.TextureTiling, textureTiling);
                shader.SetFloat2(UniformName.TextureOffset, textureOffset);
                shader.SetFloat4(UniformName.Color, color);
                shader.SetInt(UniformName.Emissive, emissive ? 1 : -1);
                shader.SetFloat(UniformName.EmissionFactor, emissionFactor);
                shader.SetFloat(UniformName.BrightnessThreshold, brightnessThreshold);

                if(child.AlphaBlend)
                    GL.Enable(EnableCap.Blend);
                else
                    GL.Disable(EnableCap.Blend);
                
                if(child.DepthTest)
                    GL.Enable(EnableCap.DepthTest);
                else
                    GL.Disable(EnableCap.DepthTest);

                if(child.CullFaces)
                {
                    GL.Enable(EnableCap.CullFace);
                    GL.CullFace(TriangleFace.Back);
                }
                else
                {
                    GL.Disable(EnableCap.CullFace);
                }

                int numIndices = (int)modelProtoType.meshes[meshIndex].numIndices;
                IntPtr offset = new IntPtr(Marshal.SizeOf<uint>() * modelProtoType.meshes[meshIndex].baseIndex);
                int baseVertex = (int)modelProtoType.meshes[meshIndex].baseVertex;
                GL.DrawElementsBaseVertex(PrimitiveType.Triangles, numIndices, DrawElementsType.UnsignedInt, offset, baseVertex);
            }
        }
    }
}