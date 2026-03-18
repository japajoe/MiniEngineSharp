namespace MiniEngine.GraphicsManagement.Shaders.Includes
{
    public static class WorldShader
    {
        public static readonly string Source = @"layout(std140) uniform World {
    vec4 fogColor;
    float fogDensity;
    int fogEnabled;
    float time;
    float padding;
} uWorld;

float calculate_fog(float density, vec3 camPosition, vec3 fragPosition) {
    float fogDistance = length(camPosition - fragPosition);
    float d = (fogDistance * density) * (fogDistance * density);
    float fogVisibility = pow(2.0, -d);
    fogVisibility = clamp(fogVisibility, 0.0f, 1.0f);
    return fogVisibility;
}";
    }
}