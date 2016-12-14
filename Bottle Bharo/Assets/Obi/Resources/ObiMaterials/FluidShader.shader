// Upgrade NOTE: replaced 'unity_World2Shadow' with 'unity_WorldToShadow'

Shader "Hidden/FluidShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{

		Pass
		{

			Name "DielectricFluid"
			Tags {"LightMode" = "ForwardBase"}

			Blend SrcAlpha OneMinusSrcAlpha 

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0

			#pragma multi_compile_fwdbase
			
			#include "ObiParticles.cginc"
			#include "UnityStandardBRDF.cginc"
			#include "UnityStandardUtils.cginc"

			struct vin
			{
				float4 pos : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 pos : POSITION;
			};

			v2f vert (vin v)
			{
				v2f o;
				o.pos = mul(UNITY_MATRIX_MVP, v.pos);
				o.uv = v.uv;

				return o;
			}

			sampler2D _MainTex;	
			sampler2D _Refraction;
			sampler2D _Thickness;
			sampler2D _Normals;
			sampler2D _CameraDepthTexture;
			float4 _MainTex_TexelSize;

			float3 _TransmittedColor;
			float _ThicknessScale;
			float _ThicknessCutoff;
			float _RefractionCoeff;
			float _Smoothness;

			UNITY_DECLARE_SHADOWMAP(_MyShadowMap);

			struct fout {
           	 	half4 color : COLOR;
            	float depth : DEPTH;
        	};

			fout frag (v2f i)
			{
				fout fo;
				fo.color = fixed4(0,0,0,1);
			
				float depth = tex2D(_MainTex, i.uv).r;
				float sceneDepth = tex2D(_CameraDepthTexture,i.uv).r;
				float thickness = tex2D(_Thickness,i.uv).r;

				if (thickness * 10 < _ThicknessCutoff || depth == 1)
					discard;

				fo.depth = depth;

				// reconstruct eye space position/direction from frustum corner and camera depth:
				float3 eyePos = EyePosFromDepth(i.uv,depth);
				float3 eyeDir = normalize(eyePos);

				// get normal: 
				float3 n = (tex2D(_Normals,i.uv)-0.5) * 2;

				// directional light shadow (cascades)
				float4 viewZ = -eyePos.z;
				float4 zNear = float4( viewZ >= _LightSplitsNear );
				float4 zFar = float4( viewZ < _LightSplitsFar );
				float4 weights = zNear * zFar;

				float4 wsPos = float4(mul(_Camera_to_World,half4(eyePos,1)).xyz,1);
				float3 shadowCoord0 = mul( unity_WorldToShadow[0], wsPos);
				float3 shadowCoord1 = mul( unity_WorldToShadow[1], wsPos);
				float3 shadowCoord2 = mul( unity_WorldToShadow[2], wsPos);
				float3 shadowCoord3 = mul( unity_WorldToShadow[3], wsPos);
				float4 shadowCoord = float4(shadowCoord0 * weights[0] + shadowCoord1 * weights[1] + shadowCoord2 * weights[2] + shadowCoord3 * weights[3],1);
				float atten = UNITY_SAMPLE_SHADOW(_MyShadowMap, shadowCoord);		

				// reflection and refraction:
				Unity_GlossyEnvironmentData g;
				g.roughness	= 1-_Smoothness;
				g.reflUVW = mul((float3x3)_Camera_to_World,reflect(eyeDir,n));
				float3 Reflection = Unity_GlossyEnvironment (UNITY_PASS_TEXCUBE(unity_SpecCube0), unity_SpecCube0_HDR, g);
				fixed4 Refraction = tex2D(_Refraction, i.uv + n.xy * thickness * _RefractionCoeff);

				// absorbance, transmittance and reflectance.
				half3 Absorbance = (1-_TransmittedColor) * -thickness * _ThicknessScale;
				half3 Transmittance = Refraction * exp(Absorbance);
				float Reflectance = FresnelTerm(0.1,DotClamped(n,-eyeDir));

				// energy-conserving blinn-phong specular lightning:
				float3 lightDir = normalize(EyeSpaceLightDir(eyePos));
				float specPower = RoughnessToSpecPower(1-_Smoothness);
				half3 h = normalize( lightDir - eyeDir );
				float nh = DotClamped( n, h );
				float normalization = (specPower + 8) / 25.13273;
    			float spec = normalization * pow( nh, specPower ) * fo.color.a;

				fo.color.rgb = lerp(Transmittance,Reflection,Reflectance) + spec * atten;

				return fo;
			}
			ENDCG
		}

		Pass
		{

			Name "OpaqueFluid"
			Tags {"LightMode" = "ForwardBase"}

			Blend SrcAlpha OneMinusSrcAlpha 

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0

			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			
			#include "ObiParticles.cginc"
			#include "UnityStandardCore.cginc"

			struct vin
			{
				float4 pos : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 pos : POSITION;
			};

			v2f vert (vin v)
			{
				v2f o;
				o.pos = mul(UNITY_MATRIX_MVP, v.pos);
				o.uv = v.uv;

				return o;
			}

			//sampler2D _MainTex;
			float3 _TransmittedColor;	
			sampler2D _Thickness;
			sampler2D _Normals;
			sampler2D _CameraDepthTexture;
			float4 _MainTex_TexelSize;

			float _ThicknessCutoff;
			float _Smoothness;

			UNITY_DECLARE_SHADOWMAP(_MyShadowMap);

			struct fout {
           	 	half4 color : COLOR;
            	float depth : DEPTH;
        	};

			fout frag (v2f i)
			{
				fout fo;
				fo.color = fixed4(0,0,0,1);
			
				float depth = tex2D(_MainTex, i.uv).r;
				float sceneDepth = tex2D(_CameraDepthTexture,i.uv).r;
				float thickness = tex2D(_Thickness,i.uv).r;

				if (thickness * 10 < _ThicknessCutoff || depth == 1)
					discard;

				fo.depth = depth;

				// reconstruct eye space position/direction from frustum corner and camera depth:
				float3 eyePos = EyePosFromDepth(i.uv,depth);
				float3 eyeDir = normalize(eyePos);

				// get normal: 
				float3 n = (tex2D(_Normals,i.uv)-0.5) * 2;

				// Get world space position, normal and view direction:
				float4 wsPos = float4(mul(_Camera_to_World,half4(eyePos,1)).xyz,1);
				half3 normalWorld = mul((float3x3)_Camera_to_World,n);
				float3 viewDir = normalize(wsPos - _WorldSpaceCameraPos);

				// directional light shadow (cascades)
				float4 viewZ = -eyePos.z;
				float4 zNear = float4( viewZ >= _LightSplitsNear );
				float4 zFar = float4( viewZ < _LightSplitsFar );
				float4 weights = zNear * zFar;

				float3 shadowCoord0 = mul( unity_WorldToShadow[0], wsPos);
				float3 shadowCoord1 = mul( unity_WorldToShadow[1], wsPos);
				float3 shadowCoord2 = mul( unity_WorldToShadow[2], wsPos);
				float3 shadowCoord3 = mul( unity_WorldToShadow[3], wsPos);
				float4 shadowCoord = float4(shadowCoord0 * weights[0] + shadowCoord1 * weights[1] + shadowCoord2 * weights[2] + shadowCoord3 * weights[3],1);
				float atten = UNITY_SAMPLE_SHADOW(_MyShadowMap, shadowCoord);

				// PBS lighting:
				half oneMinusReflectivity = 0;
				half3 specColor = half3(0.2,0.2,0.2);
				half3 diffColor = EnergyConservationBetweenDiffuseAndSpecular (_TransmittedColor, specColor, oneMinusReflectivity);
				
				UnityGI gi = FragmentGI (wsPos, 1, UNITY_LIGHTMODEL_AMBIENT, atten, _Smoothness, normalWorld, viewDir, MainLight (normalWorld));

				half4 c = UNITY_BRDF_PBS (diffColor, specColor, oneMinusReflectivity, _Smoothness, normalWorld, -viewDir, gi.light, gi.indirect);
				c.rgb += UNITY_BRDF_GI (diffColor, specColor, oneMinusReflectivity, _Smoothness, normalWorld, -viewDir, 1, gi);
			
				UNITY_APPLY_FOG(viewZ, c.rgb); 
				fo.color = OutputForward (c, 1);

				return fo;
			}
			ENDCG
		}		

	}
}
