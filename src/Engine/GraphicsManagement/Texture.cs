using OpenTK.Graphics.OpenGL;

namespace MiniEngine.GraphicsManagement
{
	public enum TextureFilterMode 
	{
		Nearest,
		Linear,
		Trilinear,      // Maps to GL_LINEAR_MIPMAP_LINEAR
		BilinearMipmap  // Maps to GL_LINEAR_MIPMAP_NEAREST
	}

	public struct TextureSettings 
	{
		public TextureWrapMode wrapS = TextureWrapMode.Repeat;
		public TextureWrapMode wrapT = TextureWrapMode.Repeat;
		public TextureFilterMode minFilter = TextureFilterMode.Trilinear;
		public TextureFilterMode magFilter = TextureFilterMode.Linear;

        public TextureSettings()
        {
            
        }
	}   
}