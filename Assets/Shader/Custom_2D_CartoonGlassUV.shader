Shader "Custom/2D/CartoonGlassUV" {
	Properties {
		_MainTex ("Sprite Texture / Alpha Mask", 2D) = "white" {}
		[Header(Base Colors)] _TopColor ("Top Glass Color", Vector) = (0.18,0.22,0.24,1)
		_BottomColor ("Bottom Glass Color", Vector) = (0.04,0.06,0.07,1)
		[Header(UV Vertical Gradient)] _UVGradientMin ("UV Gradient Min Y", Float) = 0
		_UVGradientMax ("UV Gradient Max Y", Float) = 1
		_GradientPower ("Gradient Power", Range(0.1, 5)) = 1.2
		[Header(UV Stripes)] _StripeColor ("Stripe Color", Vector) = (1,1,1,0.22)
		_StripeScale ("Stripe UV Scale", Float) = 1
		_StripeAngle ("Stripe Angle", Range(-2, 2)) = 0.35
		_StripeWidth ("Stripe Width", Range(0.001, 1)) = 0.12
		_StripeSoftness ("Stripe Softness", Range(0.001, 1)) = 0.08
		_StripeSpacing ("Stripe Spacing", Range(0.01, 5)) = 0.45
		_StripeOffset ("Stripe Offset", Float) = 0
		[Header(Highlights)] _TopHighlightColor ("Top Highlight Color", Vector) = (1,1,1,0.15)
		_TopHighlightSize ("Top Highlight Size", Range(0.01, 1)) = 0.18
		_TopHighlightSoftness ("Top Highlight Softness", Range(0.001, 1)) = 0.15
		[Header(Toon Specular (Smoothness))] _SpecColorTint ("Specular Color", Vector) = (1,1,1,0.8)
		_SpecSize ("Specular Size", Range(0.001, 1)) = 0.1
		_SpecSoftness ("Specular Softness", Range(0.001, 1)) = 0.02
		[Header(Toon Rim Light)] _RimColorTint ("Rim Color", Vector) = (1,1,1,0.4)
		_RimPower ("Rim Power", Range(0.1, 10)) = 3
		_RimSoftness ("Rim Softness", Range(0.001, 1)) = 0.05
		_Alpha ("Alpha", Range(0, 1)) = 1
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

			struct Fragment_Stage_Input
			{
				float2 uv : TEXCOORD0;
			};

			float4 frag(Fragment_Stage_Input input) : SV_TARGET
			{
				return _MainTex.Sample(sampler_MainTex, input.uv.xy);
			}

			ENDHLSL
		}
	}
	Fallback "Sprites/Default"
}