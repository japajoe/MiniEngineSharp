namespace MiniEngine.GraphicsManagement.Shaders
{
    public static class TerrainShader
    {
        public static readonly string vertexSource = @"layout(location = 0) in vec3 aPos;
        layout(location = 1) in vec3 aNormal;
        layout(location = 2) in vec2 aTexCoords;

        uniform mat4 uModel;
        uniform mat3 uModelInverted;
        uniform mat4 uMVP;

        out vec3 FragPos;
        out vec3 Normal;
        out vec2 TexCoords;

        void main() {
            vec4 clipPosition = uMVP * vec4(aPos, 1.0);
            vec4 worldPos = uModel * vec4(aPos, 1.0);
            FragPos = worldPos.xyz;
            Normal = normalize(uModelInverted * aNormal);
            TexCoords = aTexCoords;
            gl_Position = clipPosition;
        }";

        public static readonly string fragmentSource = @"layout (location = 0) out vec4 FragColor;
layout (location = 1) out vec4 BrightnessColor;
layout (location = 2) out vec4 VelocityColor;

#include <light>
#include <camera>
#include <world>
#include <pbr>
#include <shadow>

// material parameters
uniform sampler2DArray uTextureShadow;
uniform sampler2D uTextureSplat;
uniform sampler2D uTexture0;
uniform sampler2D uTexture1;
uniform sampler2D uTexture2;
uniform sampler2D uTexture3;
uniform vec2 uTextureTiling0;
uniform vec2 uTextureTiling1;
uniform vec2 uTextureTiling2;
uniform vec2 uTextureTiling3;
uniform vec4 uColor;
uniform int uEmissive;
uniform float uEmissionFactor;
uniform float uBrightnessThreshold;
uniform float uMetallic;
uniform float uRoughness;
uniform float uAmbientOcclusion;

in vec2 TexCoords;
in vec3 FragPos;
in vec3 Normal;

const float PI = 3.14159265359;

vec4 calculate_texture_color() {
    vec4 blendMapColor = texture(uTextureSplat, TexCoords);
    float backgroundTextureAmount = 1.0 - (blendMapColor.r + blendMapColor.g + blendMapColor.b);
    vec4 backgroundTextureColor = texture(uTexture0, TexCoords * uTextureTiling0) * backgroundTextureAmount;
    vec4 rTextureColor 			= texture(uTexture1, TexCoords * uTextureTiling1) * blendMapColor.r;
    vec4 gTextureColor 			= texture(uTexture2, TexCoords * uTextureTiling2) * blendMapColor.g;
    vec4 bTextureColor 			= texture(uTexture3, TexCoords * uTextureTiling3) * blendMapColor.b;
    vec4 result = backgroundTextureColor + rTextureColor + gTextureColor + bTextureColor;
    return result;
}

void output_brightness_color(vec3 albedo) {
    if(uEmissive > 0) {
        vec3 brightnessColor = FragColor.rgb + (albedo * uEmissionFactor);

        float brightness = dot(brightnessColor.rgb, vec3(0.2126, 0.7152, 0.0722));

        if(brightness > uBrightnessThreshold)
            BrightnessColor = vec4(brightnessColor.rgb, 1.0);
        else
            BrightnessColor = vec4(0.0, 0.0, 0.0, 1.0);    
    } else {
        vec3 brightnessColor = FragColor.rgb * uEmissionFactor;

        float brightness = dot(brightnessColor.rgb, vec3(0.2126, 0.7152, 0.0722));

        if(brightness > uBrightnessThreshold && FragColor.a > 0.1)
            BrightnessColor = vec4(brightnessColor.rgb, 1.0);
        else
            BrightnessColor = vec4(0.0, 0.0, 0.0, 1.0);
    }
}

void main() {
    vec4 textureColor = calculate_texture_color() * uColor;
    vec3 albedo = pow(textureColor.rgb, vec3(2.2));
    
    float metallic = uMetallic;
    float roughness = uRoughness;
    float ao = uAmbientOcclusion;
    
    vec3 N = normalize(Normal);
    vec3 V = normalize(uCamera.position.xyz - FragPos);
    
    // calculate reflectance at normal incidence; if dia-electric (like plastic) use F0 
    // of 0.04 and if it's a metal, use the albedo color as F0 (metallic workflow)    
    vec3 F0 = vec3(0.04); 
    F0 = mix(F0, albedo, metallic);
    
    float shadow = 1.0 - calculate_shadow(uTextureShadow, FragPos, uCamera.view, N, normalize(uLights.lights[0].direction.xyz));

    // reflectance equation
    vec3 Lo = vec3(0.0);
    for(int i = 0; i < uLights.activeLights; i++) {
        // calculate per-light radiance
        vec3 L = vec3(0, 0, -1);
        float distance = length(uLights.lights[i].position.xyz - FragPos);
        float attenuation = 1.0;
        
        if(uLights.lights[i].type == 0) { //Directional
            L = normalize(uLights.lights[i].direction.xyz);
        } else {
            L = normalize(uLights.lights[i].position.xyz - FragPos);
            if(uLights.lights[i].fallOffMode == 0) { // Linear
                // Linear-to-Zero Falloff
                // This makes 'range' much more impactful and 'strength' easier to manage
                float lightRange = max(uLights.lights[i].range, 0.001);
                
                // Calculate a basic linear falloff (1.0 at center, 0.0 at range)
                attenuation = clamp(1.0 - (distance / lightRange), 0.0, 1.0);
                
                // Square the attenuation for a smoother, more natural 'look'
                // while still hitting zero exactly at the range.
                attenuation *= attenuation;
            } else {
                attenuation = 1.0 / (distance * distance);
            }
        }
        vec3 H = normalize(V + L);
        vec3 radiance = uLights.lights[i].color.xyz * attenuation * uLights.lights[i].strength * 1.0;

        // Cook-Torrance BRDF
        float NDF = distribution_ggx(N, H, roughness);   
        float G   = geometry_smith(N, V, L, roughness);      
        vec3 F    = fresnel_schlick(max(dot(H, V), 0.0), F0);
        
        vec3 numerator    = NDF * G * F; 
        float denominator = 4.0 * max(dot(N, V), 0.0) * max(dot(N, L), 0.0) + 0.0001; // + 0.0001 to prevent divide by zero
        vec3 specular = numerator / denominator;
        
        // kS is equal to Fresnel
        vec3 kS = F;
        // for energy conservation, the diffuse and specular light can't
        // be above 1.0 (unless the surface emits light); to preserve this
        // relationship the diffuse component (kD) should equal 1.0 - kS.
        vec3 kD = vec3(1.0) - kS;
        // multiply kD by the inverse metalness such that only non-metals 
        // have diffuse lighting, or a linear blend if partly metal (pure metals
        // have no diffuse light).
        kD *= 1.0 - metallic;	  

        // scale light by NdotL
        float NdotL = max(dot(N, L), 0.0);        

        // add to outgoing radiance Lo
        Lo += (kD * albedo / PI + specular) * radiance * NdotL * shadow;  // note that we already multiplied the BRDF by the Fresnel (kS) so we won't multiply by kS again
    }   
    
    // ambient lighting (note that the next IBL tutorial will replace 
    // this ambient lighting with environment lighting).
    vec3 ambient = vec3(0.03) * albedo * ao;
    
    vec3 color = ambient + Lo;

    float visibility = calculate_fog(uWorld.fogDensity, uCamera.position.xyz, FragPos);
    color.rgb = mix(uWorld.fogColor.rgb, color.rgb, visibility);
    FragColor = vec4(color, textureColor.a);

    output_brightness_color(albedo);
}";
    }
}