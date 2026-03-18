using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;

namespace MiniEngine.GraphicsManagement
{
    [Flags]
	public enum BlitOption
	{
		Color = 1 << 0,
		Depth = 1 << 1,
	};

    public enum FrameBufferTextureFormat
    {
        None,
        Depth24Stencil8,
        Depth32F,
        RGBA8,
        RGBA16F,
        RGBA32F,
        RG32F,
        Depth = Depth24Stencil8,
    };

    public struct FrameBufferTextureSpecification
    {
        public FrameBufferTextureFormat format = FrameBufferTextureFormat.None;
        public TextureWrapMode wrap = TextureWrapMode.ClampToEdge;
        public TextureFilterMode filter = TextureFilterMode.Linear;

        public FrameBufferTextureSpecification()
        {
            
        }
    };

    public struct FrameBufferSpecification
    {
        public int width = 512;
        public int height = 512;
        public int samples = 1;
        public bool resizable = true;
        public List<FrameBufferTextureSpecification> attachments = new List<FrameBufferTextureSpecification>();

        public FrameBufferSpecification()
        {
            
        }
    };

    public sealed class FrameBuffer
    {
        private const int MAX_FRAMEBUFFER_SIZE = 8192;
        private int id = 0;
        private int depthAttachment = 0;
        private List<int> colorAttachments;
        private FrameBufferSpecification specification;
        private List<FrameBufferTextureSpecification> colorAttachmentSpecifications;
        private FrameBufferTextureSpecification depthAttachmentSpecification;

        public FrameBuffer()
        {
            colorAttachments = new List<int>();
            colorAttachmentSpecifications = new List<FrameBufferTextureSpecification>();
            depthAttachmentSpecification = new FrameBufferTextureSpecification();
        }

        public void Generate(FrameBufferSpecification specification)
        {
            this.specification = specification;

            if(colorAttachmentSpecifications.Count > 0)
                colorAttachmentSpecifications.Clear();
            
            for(int i = 0; i < specification.attachments.Count; i++)
            {
                if(!IsDepthFormat(specification.attachments[i].format))
                {
                    colorAttachmentSpecifications.Add(specification.attachments[i]);
                }
                else
                {
                    depthAttachmentSpecification = specification.attachments[i];
                }
            }

            Invalidate();
        }

        public void Destroy()
        {
            if(id > 0)
            {
                GL.DeleteFramebuffers(1, id);
                GL.DeleteTextures(colorAttachments.Count, colorAttachments[0]);
                GL.DeleteTextures(1, depthAttachment);
                
                id = 0;
                depthAttachment = 0;
                colorAttachments.Clear();
            }
        }

        public void Resize(int width, int height)
        {
            if(!specification.resizable)
                return;

            if(width == 0 || height == 0 || width > MAX_FRAMEBUFFER_SIZE || height > MAX_FRAMEBUFFER_SIZE)
                return;

            specification.width = width;
            specification.height = height;

            Invalidate();
        }

        public void Bind()
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, id);
            GL.Viewport(0, 0, specification.width, specification.height);
        }

        public void Unbind()
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public void Clear(Color color)
        {
            ClearBufferMask flags = 0;
            
            if(colorAttachments.Count > 0)
                flags |= ClearBufferMask.ColorBufferBit;
            if(depthAttachment > 0)
                flags |= ClearBufferMask.DepthBufferBit;

            GL.ClearColor(color.r, color.g, color.b, color.a);
            GL.Clear(flags);
        }

        public void Blit(FrameBuffer target, BlitOption options)
        {
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, this.id);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, target.id);

            ClearBufferMask mask = 0;

            if((options & BlitOption.Color) != 0)
                mask |= ClearBufferMask.ColorBufferBit;

            if((options & BlitOption.Depth) != 0)
                mask |= ClearBufferMask.DepthBufferBit;
            
            // Blit color and depth
            GL.BlitFramebuffer(
                0, 0, specification.width, specification.height,
                0, 0, target.specification.width, target.specification.height,
                mask,
                BlitFramebufferFilter.Nearest // GL_NEAREST is required when resolving multisampled buffers
            );
        }

        public void Blit(FrameBuffer target, BlitOption options, int attachmentIndex)
        {
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, this.id);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, target.id);

            ClearBufferMask mask = 0;
            
            if((options & BlitOption.Color) != 0)
            {
                mask |= ClearBufferMask.ColorBufferBit;
                
                // We use the attachmentIndex to target GL_COLOR_ATTACHMENT0, 1, 2, etc.
                GL.ReadBuffer((ReadBufferMode)((int)ReadBufferMode.ColorAttachment0 + attachmentIndex));
                GL.DrawBuffer((DrawBufferMode)((int)DrawBufferMode.ColorAttachment0 + attachmentIndex));
            }
            
            if((options & BlitOption.Depth) != 0)
                mask |= ClearBufferMask.DepthBufferBit;

            GL.BlitFramebuffer(
                0, 0, specification.width, specification.height,
                0, 0, target.specification.width, target.specification.height,
                mask,
                BlitFramebufferFilter.Nearest 
            );

            // Reset to default state to avoid side effects in other draw calls
            GL.ReadBuffer(ReadBufferMode.ColorAttachment0);
            GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
        }

        public void Blit(FrameBuffer target, BlitOption options, int attachmentIndexSource, int attachmentIndexDestination)
        {
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, this.id);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, target.id);

            ClearBufferMask mask = 0;
            
            if((options & BlitOption.Color) != 0)
            {
                mask |= ClearBufferMask.ColorBufferBit;
                
                // Map source index to destination index
                GL.ReadBuffer((ReadBufferMode)((int)ReadBufferMode.ColorAttachment0 + attachmentIndexSource));
                GL.DrawBuffer((DrawBufferMode)((int)DrawBufferMode.ColorAttachment0 + attachmentIndexDestination));
            }
            
            if((options & BlitOption.Depth) != 0)
                mask |= ClearBufferMask.DepthBufferBit;

            GL.BlitFramebuffer(
                0, 0, specification.width, specification.height,
                0, 0, target.specification.width, target.specification.height,
                mask,
                BlitFramebufferFilter.Nearest 
            );

            // Reset to defaults
            GL.ReadBuffer(ReadBufferMode.ColorAttachment0);
            GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
        }

        public int GetWidth()
        {
            return specification.width;
        }

        public int GetHeight()
        {
            return specification.height;
        }

        public int GetSamples()
        {
            return specification.samples;
        }

        public int GetColorAttachment(int index)
        {
            if(index >= colorAttachments.Count)
                throw new Exception("Color attachment index must be less than the total number of attachments");
            return colorAttachments[index];
        }

        public int GetDepthAttachment()
        {
            return depthAttachment;
        }

        private void Invalidate()
        {
            if(id > 0)
            {
                GL.DeleteFramebuffers(1, id);
                for(int i = 0; i < colorAttachments.Count; i++)
                    GL.DeleteTextures(1, colorAttachments[i]);
                GL.DeleteTextures(1, depthAttachment);

                colorAttachments.Clear();
                depthAttachment = 0;
            }

            GL.GenFramebuffers(1, ref id);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, id);

            bool multiSample = specification.samples > 1;

            if(colorAttachmentSpecifications.Count > 0)
            {
                //colorAttachments = new List<int>();
                for(int i = 0; i < colorAttachmentSpecifications.Count; i++)
                    colorAttachments.Add(0);

                CreateTextures(multiSample, colorAttachments);

                for(int i = 0; i < colorAttachments.Count; i++)
                {
                    BindTexture(multiSample, colorAttachments[i]);
                    int wrap = GetWrapMode(colorAttachmentSpecifications[i].wrap);
                    int filter = GetFilterMode(colorAttachmentSpecifications[i].filter);

                    switch(colorAttachmentSpecifications[i].format)
                    {
                    case FrameBufferTextureFormat.RGBA8:
                        AttachColorTexture(colorAttachments[i], specification.samples, InternalFormat.Rgba8, wrap, filter, specification.width, specification.height, i);
                        break;
                    case FrameBufferTextureFormat.RGBA16F:
                        AttachColorTexture(colorAttachments[i], specification.samples, InternalFormat.Rgba16f, wrap, filter, specification.width, specification.height, i);
                        break;
                    case FrameBufferTextureFormat.RGBA32F:
                        AttachColorTexture(colorAttachments[i], specification.samples, InternalFormat.Rgba32f, wrap, filter, specification.width, specification.height, i);
                        break;
                    case FrameBufferTextureFormat.RG32F:
                        AttachColorTexture(colorAttachments[i], specification.samples, InternalFormat.Rg32f, wrap, filter, specification.width, specification.height, i);
                        break;
                    default:
                        break;
                    }
                }
            }

            if(depthAttachmentSpecification.format != FrameBufferTextureFormat.None)
            {
                CreateTexture(multiSample, ref depthAttachment);
                BindTexture(multiSample, depthAttachment);
                int wrap = GetWrapMode(depthAttachmentSpecification.wrap);
                int filter = GetFilterMode(depthAttachmentSpecification.filter);

                switch(depthAttachmentSpecification.format)
                {
                case FrameBufferTextureFormat.Depth24Stencil8:
                    AttachDepthTexture(depthAttachment, specification.samples, InternalFormat.Depth24Stencil8, FramebufferAttachment.DepthStencilAttachment, wrap, filter, specification.width, specification.height);
                    break;
                case FrameBufferTextureFormat.Depth32F:
                    AttachDepthTexture(depthAttachment, specification.samples, InternalFormat.DepthComponent32f, FramebufferAttachment.DepthAttachment, wrap, filter, specification.width, specification.height);
                    break;
                default:
                    break;
                }
            }

            if(colorAttachments.Count >= 1)
            {
                if(colorAttachments.Count > 4)
                    throw new Exception("Max 4 color attachments allowed");
                
                DrawBufferMode[] buffers = { 
                    DrawBufferMode.ColorAttachment0, 
                    DrawBufferMode.ColorAttachment1, 
                    DrawBufferMode.ColorAttachment2, 
                    DrawBufferMode.ColorAttachment3 
                };
                
                GL.DrawBuffers(buffers);
            }
            else
            {
                // Only depth
                GL.DrawBuffer(DrawBufferMode.None);
            }

            if(GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferStatus.FramebufferComplete)
                throw new Exception("Frame buffer is incomplete");

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        private static bool IsDepthFormat(FrameBufferTextureFormat format)
        {
            switch(format)
            {
            case FrameBufferTextureFormat.Depth24Stencil8:
            case FrameBufferTextureFormat.Depth32F:
                return true;
            default:
                return false;
            }
        }

        private static TextureTarget GetTextureTarget(bool multiSampled)
        {
            return multiSampled ? TextureTarget.Texture2dMultisample : TextureTarget.Texture2d;
        }

        private static int GetFilterMode(TextureFilterMode filterMode)
        {
            switch(filterMode)
            {
            case TextureFilterMode.Nearest:
                return (int)TextureMinFilter.Nearest;
            case TextureFilterMode.Linear:
                return (int)TextureMinFilter.Linear;
            case TextureFilterMode.Trilinear:
                return (int)TextureMinFilter.LinearMipmapLinear;
            case TextureFilterMode.BilinearMipmap:
                return (int)TextureMinFilter.LinearMipmapNearest;
            default:
                return (int)TextureMinFilter.Linear;
            }
        }

        private static int GetWrapMode(TextureWrapMode wrapMode)
        {
            switch(wrapMode)
            {
            case TextureWrapMode.Repeat:
                return (int)OpenTK.Graphics.OpenGL.TextureWrapMode.Repeat;
            case TextureWrapMode.MirroredRepeat:
                return (int)OpenTK.Graphics.OpenGL.TextureWrapMode.MirroredRepeat;
            case TextureWrapMode.ClampToEdge:
                return (int)OpenTK.Graphics.OpenGL.TextureWrapMode.ClampToEdge;
            case TextureWrapMode.ClampToBorder:
                return (int)OpenTK.Graphics.OpenGL.TextureWrapMode.ClampToBorder;
            default:
                return (int)OpenTK.Graphics.OpenGL.TextureWrapMode.ClampToBorder;
            }
        }

        private static void CreateTextures(bool multiSampled, List<int> textures)
        {
            var span = CollectionsMarshal.AsSpan(textures);
            GL.GenTextures(span);
        }

        private static void CreateTexture(bool multiSampled, ref int texture)
        {
            GL.GenTextures(1, ref texture);
        }

        private static void BindTexture(bool multiSampled, int textureId)
        {
            GL.BindTexture(GetTextureTarget(multiSampled), textureId);
        }

        private static void AttachColorTexture(int textureId, int samples, InternalFormat internalFormat, int wrapMode, int filterMode, int width, int height, int index)
        {
            bool multiSampled = samples > 1;

            if(multiSampled)
            {
                GL.TexImage2DMultisample(TextureTarget.Texture2dMultisample, samples, internalFormat, width, height, false);
            }
            else
            {
                GL.TexImage2D(TextureTarget.Texture2d, 0, internalFormat, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
                GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, filterMode);
                GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, filterMode);
                GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapR, wrapMode);
                GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, wrapMode);
                GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, wrapMode);
            }

            FramebufferAttachment att = (FramebufferAttachment)((int)FramebufferAttachment.ColorAttachment0 + index);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, att, GetTextureTarget(multiSampled), textureId, 0);
        }

        private static void AttachDepthTexture(int textureId, int samples, InternalFormat internalFormat, FramebufferAttachment attachmentType, int wrapMode, int filterMode, int width, int height)
        {
            bool multiSampled = samples > 1;

            if(multiSampled)
            {
                GL.TexImage2DMultisample(TextureTarget.Texture2dMultisample, samples, internalFormat, width, height, false);
            }
            else
            {
                //OpenGL 3.3 approach for depth
                //For depth, format and type usually match the internalFormat requirements
                PixelFormat format = PixelFormat.DepthComponent;
                PixelType type = PixelType.Float;

                if (internalFormat == InternalFormat.Depth24Stencil8)
                {
                    format = PixelFormat.DepthStencil;
                    type = PixelType.UnsignedInt248;
                }

                //glTexStorage2D(GL_TEXTURE_2D, 1, format, width, height); // OpenGL 4.2+
                GL.TexImage2D(TextureTarget.Texture2d, 0, internalFormat, width, height, 0, format, type, IntPtr.Zero);
                GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, filterMode);
                GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, filterMode);
                GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapR, wrapMode);
                GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, wrapMode);
                GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, wrapMode);
            }

            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, attachmentType, GetTextureTarget(multiSampled), textureId, 0);
        }
    }
}