using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace MiniEngine.GraphicsManagement.PostProcessing
{
	public class PingPongBuffer
	{
		public FrameBuffer sourceFBO;
		public FrameBuffer destinationFBO;
		
        public void Swap()
        {
            FrameBuffer src = sourceFBO;
            sourceFBO = destinationFBO;
            destinationFBO = src;
        }
	}

    public abstract class PostProcessingEffect
    {
        public PingPongBuffer buffer;
		public Shader shader;
		public int vao;
		public int depthTexture;
		private bool isActive = true;

        public bool IsActive
        {
            get => isActive;
            set => isActive = value;
        }

        public PostProcessingEffect()
        {
            isActive = true;
        }
		
        public void Bind()
        {
            buffer.destinationFBO.Bind();
        }
		
        public void Render()
        {
		    GL.BindVertexArray(vao);
		    GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
        }
		
        public void SwapBuffers()
        {
            buffer.Swap();
        }

		public virtual void Initialize()
        {

        }

		public virtual void OnProcess(Matrix4 projection, Matrix4 view)
        {

        }
    }
}