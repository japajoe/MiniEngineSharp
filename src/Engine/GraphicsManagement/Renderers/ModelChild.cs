using MiniEngine.Core;
using OpenTK.Mathematics;

namespace MiniEngine.GraphicsManagement.Renderers
{
    public class ModelChild : Entity
    {
        private Texture2D texture;
        private Color color;
        private float ambientOcclusion;
        private float metallic;
        private float roughness;
        private bool emissive;
        private float emissionFactor;
        private float brightnessThreshold;
        private Vector2 textureOffset;
        private Vector2 textureTiling;
        private bool alphaBlend;
        private bool depthTest;
        private bool cullFaces;
        private bool castShadows;
        private bool mustSort;
        private int renderQueue;
        private int meshInfoIndex;
        private string textureName;

        public ModelChild() : base()
        {
            texture = null;
            color = Color.White;
            ambientOcclusion = 1.0f;
            metallic = 0.0f;
            roughness = 0.0f;
            emissive = false;
            emissionFactor = 1.0f;
            brightnessThreshold = 1.0f;
            textureOffset = new Vector2(0, 0);
            textureTiling = new Vector2(1, 1);
            alphaBlend = false;
            depthTest = true;
            cullFaces = true;
            castShadows = true;
            mustSort = false;
            renderQueue = 1000;
            meshInfoIndex = 0;
        }

        public Texture2D Texture
        {
            get => texture;
            set => texture = value;
        }

        public Color Color
        {
            get => color;
            set => color = value;
        }

        public float AmbientOcclusion
        {
            get => ambientOcclusion;
            set => ambientOcclusion = value;
        }

        public float Metallic
        {
            get => metallic;
            set => metallic = value;
        }

        public float Roughness
        {
            get => roughness;
            set => roughness = value;
        }

        public bool Emissive
        {
            get => emissive;
            set => emissive = value;
        }

        public float EmissionFactor
        {
            get => emissionFactor;
            set => emissionFactor = value;
        }

        public float BrightnessThreshold
        {
            get => brightnessThreshold;
            set => brightnessThreshold = value;
        }

        public Vector2 TextureTiling
        {
            get => textureTiling;
            set => textureTiling = value;
        }

        public Vector2 TextureOffset
        {
            get => textureOffset;
            set => textureOffset = value;
        }

        public bool AlphaBlend
        {
            get => alphaBlend;
            set => alphaBlend = value;
        }

        public bool DepthTest
        {
            get => depthTest;
            set => depthTest = value;
        }

        public bool CullFaces
        {
            get => cullFaces;
            set => cullFaces = value;
        }

        public bool CastShadows
        {
            get => castShadows;
            set => castShadows = value;
        }

        public string TextureName
        {
            get => textureName;
            set => textureName = value;
        }

        public int RenderQueue
        {
            get => renderQueue;
            set
            {
                renderQueue = value;
                mustSort = true;
            }
        }

        public int MeshInfoIndex
        {
            get => meshInfoIndex;
            set => meshInfoIndex = value;
        }
    }
}