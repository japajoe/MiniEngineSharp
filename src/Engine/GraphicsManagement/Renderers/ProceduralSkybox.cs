using System;
using System.Collections.Generic;
using MiniEngine.Core;
using MiniEngine.Utilities;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace MiniEngine.GraphicsManagement.Renderers
{
    public struct ProceduralSkyboxUniforms
    {
        public int uCloudCoverage;
        public int uCloudDensity;
        public int uCloudElevation;
        public int uCloudScale;
        public int uCloudSpeed;
        public int uExposure;
        public int uMieCoefficient;
        public int uMieDirectionalG;
        public int uRayleigh;
        public int uSunPosition;
        public int uTurbidity;

        public ProceduralSkyboxUniforms()
        {
            uCloudCoverage = 1;
            uCloudDensity = 1;
            uCloudElevation = 1;
            uCloudScale = 1;
            uCloudSpeed = 1;
            uExposure = 1;
            uMieCoefficient = 1;
            uMieDirectionalG = 1;
            uRayleigh = 1;
            uSunPosition = 1;
            uTurbidity = 1;
        }
    }

    public sealed class ProceduralSkybox : Renderer
    {
        private static TextureCubeMap texture = null;
        private static Shader shader = null;
        private static ProceduralSkyboxUniforms uniforms;
        private ModelProtoType modelProtoType;
		private float emissionFactor;
		private float brightnessThreshold;
		private float rayleigh;
		private float turbidity;
		private float mieCoefficient;
		private float mieDirectionalG;
		private float cloudScale;
		private float cloudSpeed;
		private float cloudCoverage;
		private float cloudDensity;
		private float cloudElevation;
		private float exposure;
		private Vector3 sunPosition;
        private Color skyColor;

        public float EmissionFactor
        {
            get => emissionFactor;
            set => emissionFactor = value;
        }

        public float BrightnessThreshold
        {
            get => brightnessThreshold;
            set => brightnessThreshold = value;
        }

        public float Rayleigh
        {
            get => rayleigh;
            set 
            {
                rayleigh = value;
                SetSkyColor();
            }
        }

        public float Turbidity
        {
            get => turbidity;
            set
            {
                turbidity = value;
                SetSkyColor();
            }
        }

        public float MieCoefficient
        {
            get => mieCoefficient;
            set
            {
                mieCoefficient = value;
                SetSkyColor();
            }
        }

        public float MieDirectionalG
        {
            get => mieDirectionalG;
            set
            {
                mieDirectionalG = value;
                SetSkyColor();
            }
        }

        public float CloudScale
        {
            get => cloudScale;
            set
            {
                cloudScale = value;
                SetSkyColor();
            }
        }

        public float CloudSpeed
        {
            get => cloudSpeed;
            set => cloudSpeed = value;
        }

        public float CloudCoverage
        {
            get => cloudCoverage;
            set => cloudCoverage = value;
        }

        public float CloudDensity
        {
            get => cloudDensity;
            set => cloudDensity = value;
        }

        public float CloudElevation
        {
            get => cloudElevation;
            set => cloudElevation = value;
        }

        public float Exposure
        {
            get => exposure;
            set
            {
                exposure = value;
                SetSkyColor();
            }
        }

        public Color SkyColor
        {
            get => skyColor;
        }

        public ProceduralSkybox() : base()
        {
            if(shader == null)
                shader = Graphics.GetShader(ShaderName.ProceduralSkybox);

            float elevation = 2.0f;
            float azimuth = 180.0f;
            float phi = MathHelper.DegreesToRadians(90.0f - elevation);
            float theta = MathHelper.DegreesToRadians(azimuth);

            brightnessThreshold = 1.0f;
            emissionFactor = 1.0f;
            sunPosition = FromSphericalCoordinates(1.0f, phi, theta);
            rayleigh = 3.0f;
            turbidity = 10.0f;
            mieCoefficient = 0.005f;
            mieDirectionalG = 0.1f;
            cloudScale = 0.0002f;
            cloudSpeed = 0.00005f;
            cloudCoverage = 0.4f;
            cloudDensity = 0.4f;
            cloudElevation = 1.0f;
            exposure = 1.0f;

            SetSkyColor();
        }

        public void SetSunPositionFromDirection(Vector3 direction)
        {
            // Calculate elevation (phi) and azimuth (theta)
            float elevation = (float)MathHelper.RadiansToDegrees(Math.Asin(direction.Y)); // Elevation in degrees
            float azimuth = (float)MathHelper.RadiansToDegrees(Math.Atan2(direction.Z, direction.X)); // Azimuth in degrees

            // Optionally, n adjust the azimuth to be in the range [0, 360)
            if (azimuth < 0.0f)
                azimuth += 360.0f;

            float phi = MathHelper.DegreesToRadians(90.0f - elevation);
            float theta = MathHelper.DegreesToRadians(90.0f - azimuth);
            sunPosition = FromSphericalCoordinates(1.0f, phi, theta);
            SetSkyColor();
        }

        public void SetSunPosition(float azimuthDeg, float elevationDeg)
        {
            // Convert Elevation to Phi (Polar angle)
            // Elevation 90° (overhead) -> Phi 0
            // Elevation 0° (horizon)  -> Phi 90°
            float phi = MathHelper.DegreesToRadians(90.0f - elevationDeg);

            // Convert Azimuth to Theta (Azimuthal angle)
            float theta = MathHelper.DegreesToRadians(azimuthDeg);

            sunPosition = FromSphericalCoordinates(1.0f, phi, theta);
            SetSkyColor();
        }

        public override void OnRender(Matrix4 projection, Matrix4 view, Frustum frustum)
        {
            if(!isActive)
                return;

            if(modelProtoType == null)
            {
                modelProtoType = ModelGenerator.Get(ModelName.Cube).GetProtoType();
                uniforms = new ProceduralSkyboxUniforms();
                uniforms.uCloudCoverage = GL.GetUniformLocation(shader.Id, "uCloudCoverage");
                uniforms.uCloudDensity = GL.GetUniformLocation(shader.Id, "uCloudDensity");
                uniforms.uCloudElevation = GL.GetUniformLocation(shader.Id, "uCloudElevation");
                uniforms.uCloudScale = GL.GetUniformLocation(shader.Id, "uCloudScale");
                uniforms.uCloudSpeed = GL.GetUniformLocation(shader.Id, "uCloudSpeed");
                uniforms.uExposure = GL.GetUniformLocation(shader.Id, "uExposure");
                uniforms.uMieCoefficient = GL.GetUniformLocation(shader.Id, "uMieCoefficient");
                uniforms.uMieDirectionalG = GL.GetUniformLocation(shader.Id, "uMieDirectionalG");
                uniforms.uRayleigh = GL.GetUniformLocation(shader.Id, "uRayleigh");
                uniforms.uSunPosition = GL.GetUniformLocation(shader.Id, "uSunPosition");
                uniforms.uTurbidity = GL.GetUniformLocation(shader.Id, "uTurbidity");
            }

            if(modelProtoType.vao == 0)
                return;

            if(texture == null)
                texture = TextureHelper.CreateStarsTexture(1024, 1024);

            shader.Use();

            Matrix4 model = transform.GetModelMatrix();

            shader.SetMat4(UniformName.Model, model);
            shader.SetFloat(UniformName.EmissionFactor, emissionFactor);
            shader.SetFloat(UniformName.BrightnessThreshold, brightnessThreshold);
            shader.SetFloat3Ex(uniforms.uSunPosition, sunPosition);
            shader.SetFloatEx(uniforms.uRayleigh, rayleigh);
            shader.SetFloatEx(uniforms.uTurbidity, turbidity);
            shader.SetFloatEx(uniforms.uMieCoefficient, mieCoefficient);
            shader.SetFloatEx(uniforms.uMieDirectionalG, mieDirectionalG);
            shader.SetFloatEx(uniforms.uCloudScale, cloudScale);
            shader.SetFloatEx(uniforms.uCloudSpeed, cloudSpeed);
            shader.SetFloatEx(uniforms.uCloudCoverage, cloudCoverage);
            shader.SetFloatEx(uniforms.uCloudDensity, cloudDensity);
            shader.SetFloatEx(uniforms.uCloudElevation, cloudElevation);
            shader.SetFloatEx(uniforms.uExposure, exposure);

            if(texture != null)
            {
                texture.Bind(0);
                shader.SetInt(UniformName.Texture, 0);
            }
            
            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.CullFace);
            GL.Disable(EnableCap.Blend);

            GL.BindVertexArray(modelProtoType.vao);
            GL.DrawElements(PrimitiveType.Triangles, modelProtoType.indices.Count, DrawElementsType.UnsignedInt, 0);
        }

        private static Vector3 FromSphericalCoordinates(float radius, float phi, float theta)
        {
            float sinPhiRadius = (float)Math.Sin(phi) * radius;
            float x = sinPhiRadius * (float)Math.Sin(theta);
            float y = (float)Math.Cos(phi) * radius;
            float z = sinPhiRadius * (float)Math.Cos(theta);
            return new Vector3(x, y, z);
        }

        private void SetSkyColor()
        {
            Vector3 sunPosition = Vector3.Normalize(this.sunPosition);
            float sunFade = 1.0f - Math.Clamp(1.0f - MathF.Exp((sunPosition.Y / 450000.0f ) ), 0.0f, 1.0f);
            float rayleighCoefficient = rayleigh - (1.0f * (1.0f - sunFade ));
            Vector3 totalRayleigh = new Vector3(5.804542996261093E-6f, 1.3562911419845635E-5f, 3.0265902468824876E-5f);
            Vector3 betaR = totalRayleigh * rayleighCoefficient;
            Vector3 betaM = TotalMie( turbidity) * mieCoefficient;
            float sunE = SunIntensity(Vector3.Dot(sunPosition, new Vector3(0, 1, 0)));
            skyColor = ComputeSkyColor(sunPosition, sunFade, betaR, betaM, sunE, mieDirectionalG, exposure);
        }

        private static Vector3 TotalMie(float t) 
        {
            Vector3 MieConst = new Vector3(1.8399918514433978E14f, 2.7798023919660528E14f, 4.0790479543861094E14f);
            float c = ( 0.2f * t ) * 10E-18f;
            return 0.434f * c * MieConst;
        }

        private static float SunIntensity(float zenithAngleCos) 
        {
            float e = 2.71828182845904523536028747135266249775724709369995957f;
            float cutoffAngle = 1.6110731556870734f;
            float steepness = 1.5f;
            float EE = 1000.0f;

            zenithAngleCos = Math.Clamp( zenithAngleCos, -1.0f, 1.0f);
            return EE * MathF.Max( 0.0f, 1.0f - MathF.Pow(e, -((cutoffAngle - MathF.Acos(zenithAngleCos)) / steepness)));
        }

        private static Vector3 Exp(Vector3 v)
        {
            float x = MathF.Exp(v.X);
            float y = MathF.Exp(v.Y);
            float z = MathF.Exp(v.Z);
            return new Vector3(x, y, z);
        }

        private static Vector3 Pow(Vector3 v, Vector3 power)
        {
            float x = MathF.Pow(v.X, power.X);
            float y = MathF.Pow(v.Y, power.Y);
            float z = MathF.Pow(v.Z, power.Z);
            return new Vector3(x, y, z);
        }

        private static Vector3 Mix(Vector3 a, Vector3 b, float t)
        {
            return Vector3.Lerp(a, b, t);
        }

        private static float RayleighPhase(float cosTheta)
        {
            const float threeOverSixteenPi = 0.05968310365f;
            return threeOverSixteenPi * (1.0f + MathF.Pow(cosTheta, 2.0f));
        }

        private static float HGPhase(float cosTheta, float g)
        {
            const float oneOverFourPi = 0.07957747154f;
            float g2 = MathF.Pow(g, 2.0f);
            float inverse = 1.0f / MathF.Pow(1.0f - 2.0f * g * cosTheta + g2, 1.5f);
            return oneOverFourPi * ((1.0f - g2) * inverse);
        }

        private static Color ComputeSkyColor(Vector3 sunDirection, float sunFade, Vector3 betaR, Vector3 betaM, float sunE, float mieDirectionalG, float exposure)
        {
            float pi = 3.14159265359f;
            float rayleighZenithLength = 8.4E3f;
            float mieZenithLength = 1.25E3f;
            Vector3 up = new Vector3(0.0f, 1.0f, 0.0f);

            // 1. Setup direction for the horizon
            // We pick a direction perpendicular to 'up'
            Vector3 direction = new Vector3(1.0f, 0.0f, 0.0f);

            // 2. Optical Length at Horizon
            float zenithAngle = MathF.Acos(MathF.Max(0.0f, Vector3.Dot(up, direction)));
            float angleDeg = (zenithAngle * 180.0f) / pi;
            float inverse = 1.0f / (MathF.Cos(zenithAngle) + 0.15f * MathF.Pow(93.885f - angleDeg, -1.253f));
            
            float sR = rayleighZenithLength * inverse;
            float sM = mieZenithLength * inverse;

            // 3. Combined Extinction
            Vector3 fEx = Exp(-(betaR * sR + betaM * sM));

            // 4. In-scattering
            float cosTheta = Vector3.Dot(direction, sunDirection);

            float rPhase = RayleighPhase(cosTheta * 0.5f + 0.5f);
            Vector3 betaRTheta = betaR * rPhase;

            float mPhase = HGPhase(cosTheta, mieDirectionalG);
            Vector3 betaMTheta = betaM * mPhase;

            // Lin calculation
            Vector3 lin = Pow(sunE * ((betaRTheta + betaMTheta) / (betaR + betaM)) * (new Vector3(1.0f) - fEx), new Vector3(1.5f));
            
            // Solar influence adjustment
            float sunInfluence = Math.Clamp(MathF.Pow(1.0f - Vector3.Dot(up, sunDirection), 5.0f), 0.0f, 1.0f);
            Vector3 linModifier = Pow(sunE * ((betaRTheta + betaMTheta) / (betaR + betaM)) * fEx, new Vector3(0.5f));
            lin *= Mix(new Vector3(1.0f), linModifier, sunInfluence);

            // 5. Final Composition (matches shader's magic numbers)
            Vector3 l0 = new Vector3(0.1f) * fEx;
            Vector3 texColor = (lin + l0) * 0.04f + new Vector3(0.0f, 0.0003f, 0.00075f);

            // 6. Color Correction
            //float exponent = 1.0f / (1.2f + (1.2f * sunFade));
            //Vector3 retColor = glm::pow(texColor, Vector3(exponent));

            // 7. Exposure Tone Mapping
            //Vector3 mapped = Vector3(1.0f) - glm::exp(-retColor * exposure);
            Vector3 mapped = new Vector3(1.0f) - Exp(-texColor * exposure);

            return new Color(mapped.X, mapped.Y, mapped.Z, 1.0f);
        }

        
    }
}