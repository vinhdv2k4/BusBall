Shader "PuzzleBall/TrayLite" {
	Properties {
		_MainTex ("Main Texture", 2D) = "white" {}
		_Color ("Color", Vector) = (1,1,1,1)
		_GeneralAlpha ("General Alpha", Range(0, 1)) = 1
		[Header(Lighting Model)] [Toggle(_PBR_ON)] _PBROn ("PBR Shading", Float) = 0
		_Metallic ("Metallic", Range(0, 1)) = 0
		_Smoothness ("Smoothness", Range(0, 1)) = 0.5
		[Header(Specular)] _Shininess ("Shininess", Range(0.01, 25)) = 8
		_SpecularAtten ("Specular Attenuation", Range(0, 1)) = 0.3
		_SpecularColor ("Specular Color", Vector) = (0.75,0.75,0.75,1)
		[Toggle(_SPECULAR_TOON)] _SpecularToonOn ("Toon Specular", Float) = 0
		_SpecularToonCutoff ("Toon Cutoff", Range(0, 1)) = 0
		_SpecularToonSmoothness ("Toon Smoothness", Range(0, 1)) = 0.5
		[Header(Matcap)] [Toggle(_MATCAP_ON)] _MatcapOn ("Matcap", Float) = 0
		[NoScaleOffset] _MatcapTex ("Matcap Texture", 2D) = "white" {}
		_MatcapIntensity ("Matcap Intensity", Range(0, 10)) = 1
		_MatcapBlend ("Matcap Blend", Range(0, 1)) = 1
		[Header(Emission)] [Toggle(_EMISSION_ON)] _EmissionEnabled ("Emission", Float) = 0
		[HDR] _EmissionColor ("Emission Color", Vector) = (0,0,0,1)
		_EmissionSelfGlow ("Emission Self Glow", Range(0, 20)) = 1
		[Header(Alpha Cutoff)] [Toggle(_ALPHA_CUTOFF_ON)] _AlphaCutoffOn ("Alpha Cutoff", Float) = 1
		_AlphaCutoffValue ("Cutoff Value", Range(0, 1)) = 0.25
		[Header(Outline)] [Toggle(_OUTLINE_ON)] _OutlineOn ("Enable Outline", Float) = 1
		_OutlineColor ("Outline Color", Vector) = (0,0,0,1)
		_OutlineThickness ("Outline Thickness", Float) = 2.5
		[Enum(Basic, 8, Clean, 6)] _OutlineMode ("Outline Mode", Float) = 6
		[IntRange] _StencilRef ("Stencil Ref", Range(1, 255)) = 1
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType"="Opaque" }
		LOD 200

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			float4x4 unity_ObjectToWorld;
			float4x4 unity_MatrixVP;
			float4 _MainTex_ST;

			struct Vertex_Stage_Input
			{
				float4 pos : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Vertex_Stage_Output
			{
				float2 uv : TEXCOORD0;
				float4 pos : SV_POSITION;
			};

			Vertex_Stage_Output vert(Vertex_Stage_Input input)
			{
				Vertex_Stage_Output output;
				output.uv = (input.uv.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
				output.pos = mul(unity_MatrixVP, mul(unity_ObjectToWorld, input.pos));
				return output;
			}

			Texture2D<float4> _MainTex;
			SamplerState sampler_MainTex;
			float4 _Color;

			struct Fragment_Stage_Input
			{
				float2 uv : TEXCOORD0;
			};

			float4 frag(Fragment_Stage_Input input) : SV_TARGET
			{
				return _MainTex.Sample(sampler_MainTex, input.uv.xy) * _Color;
			}

			ENDHLSL
		}
	}
	Fallback "Legacy Shaders/Diffuse"
}