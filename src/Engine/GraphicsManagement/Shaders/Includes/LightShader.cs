namespace MiniEngine.GraphicsManagement.Shaders.Includes
{
    public static class LightShader
    {
        public static readonly string Source = @"#define MAX_NUM_LIGHTS 32
struct LightInfo {
    int type;
    int fallOffMode;
    float strength;
    float range;
    vec4 position;
    vec4 direction;
    vec4 color;
};

layout(std140) uniform Lights {
    int activeLights;
    int padding1;
    int padding2;
    int padding3;
    LightInfo lights[MAX_NUM_LIGHTS];
} uLights;";
    }
}