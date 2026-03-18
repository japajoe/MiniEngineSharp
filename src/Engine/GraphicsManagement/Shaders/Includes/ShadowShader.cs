namespace MiniEngine.GraphicsManagement.Shaders.Includes
{
    public static class ShadowShader
    {
        public static readonly string Source = @"layout (std140) uniform Shadow {
    int cascadeCount;
    float shadowBias;
    float farPlane;
    int enabled;
    mat4 lightSpaceMatrices[16];
    float cascadePlaneDistances[16];
} uShadow;

float calculate_shadow(sampler2DArray textureShadow, vec3 fragPosWorld, mat4 view, vec3 normal, vec3 lightDirection) {
    if(uShadow.enabled < 1)
        return 0.0;

    // 1. Determine the cascade layer
    vec4 fragPosViewSpace = view * vec4(fragPosWorld, 1.0);
    float depthValue = abs(fragPosViewSpace.z);

    int layer = -1;
    for (int i = 0; i < uShadow.cascadeCount; ++i) {
        if (depthValue < uShadow.cascadePlaneDistances[i]) {
            layer = i;
            break;
        }
    }
    if (layer == -1) 
		layer = uShadow.cascadeCount;

    // 2. Project to light space
    vec3 offsetPos = fragPosWorld + (normalize(normal) * 0.1); 
    vec4 fragPosLightSpace = uShadow.lightSpaceMatrices[layer] * vec4(offsetPos, 1.0);
    //vec4 fragPosLightSpace = uShadow.lightSpaceMatrices[layer] * vec4(fragPosWorld, 1.0);
    vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;
    projCoords = projCoords * 0.5 + 0.5;

    // If outside the light's frustum, no shadow
    if (projCoords.z > 1.0) 
        return 0.0;

    // 3. Precise Bias Logic
    // Slope-scaled bias prevents acne on angled surfaces
    float maxBias = uShadow.shadowBias; //default 0.0005f;
    float bias = max(0.005 * (1.0 - dot(normalize(normal), lightDirection)), maxBias);
    
    // Scale bias by a small factor per layer to account for lower precision far away
    // We do NOT multiply by the world distance here; we use a small multiplier.
    // bias *= (layer == 0) ? 1.0 : (1.0 + float(layer) * 0.5);

	// For Cascaded Shadow Maps, far cascades need a larger bias than near ones
	bias *= (1.0 / (uShadow.cascadePlaneDistances[layer] * 1.0));

    // 4. Manual PCF Loop
    float shadow = 0.0;
    vec2 texelSize = 1.0 / vec2(textureSize(textureShadow, 0));

    for(int x = -1; x <= 1; ++x) {
        for(int y = -1; y <= 1; ++y) {
            float pcfDepth = texture(textureShadow, vec3(projCoords.xy + vec2(x, y) * texelSize, layer)).r; 
            // If the current fragment's depth is greater than the map's depth, it's in shadow
            shadow += (projCoords.z - bias) > pcfDepth ? 1.0 : 0.0;
        }    
    }
    
    //return shadow / 9.0;
    return shadow * 0.111111111; // same as dividing by 9
}";
    }
}