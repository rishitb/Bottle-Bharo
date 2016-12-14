Shader "Hidden/FluidThickness"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}

	SubShader { 

		Pass { 
			Name "FluidThickness"
			Tags {"Queue"="Geometry" "IgnoreProjector"="True" "RenderType"="Transparent"}
			
			Blend One One  
			ZWrite Off //ZTest Always

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "ObiParticles.cginc"

			struct vin{
				float4 pos   : POSITION;
				float3 corner   : NORMAL;
				fixed4 color    : COLOR;
				float2 texcoord  : TEXCOORD0;
				float2 texcoord2  : TEXCOORD1;
			};

			struct v2f
			{
				float4 pos   : POSITION;
				fixed4 color    : COLOR;
				float2 texcoord  : TEXCOORD0;
				float2 data  : TEXCOORD1;
				float4 projPos : TEXCOORD2;
			};

			sampler2D _CameraDepthTexture;

			v2f vert(vin v)
			{ 
				v2f o;
				float4 worldpos = mul(UNITY_MATRIX_MV, v.pos) + float4(v.corner.x, v.corner.y, 0, 0);
				o.pos = mul(UNITY_MATRIX_P, worldpos);
				o.projPos = ComputeScreenPos(o.pos);
				o.texcoord = v.texcoord;
				o.color = v.color;
				o.data = v.texcoord2;

				return o;
			} 

			float4 frag(v2f i) : SV_Target
			{
				float sceneDepth = LinearEyeDepth (tex2Dproj(_CameraDepthTexture,
                                                         UNITY_PROJ_COORD(i.projPos)).r);

				if (sceneDepth < i.projPos.z)
					discard;

				return BillboardSphereThickness(i.texcoord) * i.data.y;
			}
			 
			ENDCG

		}

		Pass { 
			Name "ThicknessHorizontalBlur"

			Cull Off ZWrite Off ZTest Always

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.uv = v.uv;
				return o;
			}

			sampler2D _MainTex;
			float4 _MainTex_TexelSize;

			float4 frag(v2f i) : SV_Target
			{
				float2 offset = float2(_MainTex_TexelSize.x,0);

				half sample1 = tex2D(_MainTex,i.uv+offset*3).r * .006;
				half sample2 = tex2D(_MainTex,i.uv+offset*2).r * .061;
				half sample3 = tex2D(_MainTex,i.uv+offset).r * .242;
				half sample4 = tex2D(_MainTex,i.uv).r * .383;
				half sample5 = tex2D(_MainTex,i.uv-offset).r * .242;
				half sample6 = tex2D(_MainTex,i.uv-offset*2).r * .061;
				half sample7 = tex2D(_MainTex,i.uv-offset*3).r * .006;

				return sample1 + sample2 + sample3 + sample4 + sample5 + sample6 + sample7;
			}
			 
			ENDCG

		}

		Pass { 

			Name "ThicknessVerticalBlur"

			Cull Off ZWrite Off ZTest Always

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_TexelSize;	

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.uv = v.uv;
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				float2 offset = float2(0,_MainTex_TexelSize.y);

				half sample1 = tex2D(_MainTex,i.uv+offset*3).r * .006;
				half sample2 = tex2D(_MainTex,i.uv+offset*2).r * .061;
				half sample3 = tex2D(_MainTex,i.uv+offset).r * .242;
				half sample4 = tex2D(_MainTex,i.uv).r * .383;
				half sample5 = tex2D(_MainTex,i.uv-offset).r * .242;
				half sample6 = tex2D(_MainTex,i.uv-offset*2).r * .061;
				half sample7 = tex2D(_MainTex,i.uv-offset*3).r * .006;

				return sample1 + sample2 + sample3 + sample4 + sample5 + sample6 + sample7;
			}
			 
			ENDCG

		}

	} 
FallBack "Diffuse"
}
