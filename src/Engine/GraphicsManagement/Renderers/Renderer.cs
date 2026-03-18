using MiniEngine.Core;
using OpenTK.Mathematics;

namespace MiniEngine.GraphicsManagement.Renderers
{
    public abstract class Renderer : Entity
    {
        public Renderer() : base()
        {
            
        }

        public virtual void OnRenderDepth()
        {
            
        }

        public virtual void OnRender(Matrix4 projection, Matrix4 view, Frustum frustum)
        {
            
        }
    }
}