namespace MiniEngine.GraphicsManagement.Shaders.Includes
{
    public static class CameraShader
    {
        public static readonly string Source = @"layout(std140) uniform Camera {
    mat4 view;
    mat4 projection;
    mat4 viewProjection;
    mat4 viewProjectionInverse;
    vec4 position;
    vec4 direction;
    vec2 resolution;
    float near;
    float far;
} uCamera;";
    }
}