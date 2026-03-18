namespace MiniEngine.GraphicsManagement.Shaders.PostProcessing
{
    public static class BloomShader
    {
        public static string fragmentSource = @"in vec2 TexCoords;
out vec4 FragColor;

uniform sampler2D uTexture;
uniform sampler2D uBloomTexture;
uniform float uIntensity;

void main() {
    vec3 fragment = texture(uTexture, TexCoords).rgb;
    vec3 bloom = texture(uBloomTexture, TexCoords).rgb * uIntensity;
    vec3 result = fragment + bloom;
    FragColor = vec4(result, 1.0);
}";
    }
}