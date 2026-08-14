Shader "Custom/WavyNoiseUneven_Aniso_Warp_AstroidStar_ColorPickers" {
	Properties {
		[Header(Color Palette Settings)] [HDR] _Color1 ("Color 1", Vector) = (1,0,0,1)
		[HDR] _Color2 ("Color 2", Vector) = (1,0.5,0,1)
		[HDR] _Color3 ("Color 3", Vector) = (1,1,0,1)
		[HDR] _Color4 ("Color 4", Vector) = (0,1,0,1)
		[HDR] _Color5 ("Color 5", Vector) = (0,0,1,1)
		[HDR] _Color6 ("Color 6", Vector) = (0.3,0,0.5,1)
		[HDR] _Color7 ("Color 7", Vector) = (0.6,0,1,1)
		_Speed ("Rainbow Speed", Range(0.1, 5)) = 1
		_Value ("Overall Brightness Multiplier", Range(0, 3)) = 1
		[Header(Color Band Settings)] _ColorWarpIntensity ("Color Band Warp Intensity", Range(0, 3)) = 1.5
		[Header(World Space Mapping)] _WorldScale ("World Rainbow Scale", Float) = 0.5
		_Direction ("Base Direction (X,Y,Z)", Vector) = (1,0,0,0)
		_Rotation ("Rotation Offset (X,Y,Z Degrees)", Vector) = (0,0,0,0)
		[Header(Wave Settings)] _WaveAmplitude ("Wave Amplitude", Range(0, 2)) = 0.2
		_WaveFrequency ("Wave Frequency", Range(0, 10)) = 3
		_WaveSpeed ("Wave Speed", Range(-5, 5)) = 2
		_WaveDirection ("Wave Direction (X,Y,Z)", Vector) = (0,1,0,0)
		[Header(Base Noise Settings)] _GlobalNoiseScale ("Global Noise Multiplier", Range(0.1, 10)) = 1
		_NoiseScale ("Base Noise Scale", Range(0.1, 10)) = 2
		_NoiseIntensity ("Noise Strength/Distortion", Range(0, 2)) = 0.5
		_NoiseSpeed ("Noise Speed", Range(-5, 5)) = 1
		[Header(Anisotropic Noise Blending)] _AnisoNoiseScale ("Aniso Scale (X,Y,Z)", Vector) = (0.5,10,0.5,0)
		_AnisoBlend ("Aniso Blend Amount", Range(0, 1)) = 0.5
		[Header(Directional Noise Warp)] _WarpDirection ("Warp Direction (X,Y,Z)", Vector) = (1,1,0,0)
		_WarpIntensity ("Warp Intensity/Distance", Range(0, 5)) = 1
		_WarpScale ("Warp Noise Scale", Range(0.1, 10)) = 1.5
		[Header(Sparkle Star Settings)] _SparkleDensity ("Sparkle Density (Lower = Bigger Grid)", Range(0.1, 200)) = 20
		_SparkleSize ("Sparkle Size", Range(0.01, 3)) = 0.3
		_SparkleSpeed ("Twinkle Speed", Range(0.1, 10)) = 3
		[HDR] _SparkleColor ("Sparkle Color", Vector) = (1,1,1,1)
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType" = "Opaque" }
		LOD 200

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			float4x4 unity_ObjectToWorld;
			float4x4 unity_MatrixVP;

			struct Vertex_Stage_Input
			{
				float4 pos : POSITION;
			};

			struct Vertex_Stage_Output
			{
				float4 pos : SV_POSITION;
			};

			Vertex_Stage_Output vert(Vertex_Stage_Input input)
			{
				Vertex_Stage_Output output;
				output.pos = mul(unity_MatrixVP, mul(unity_ObjectToWorld, input.pos));
				return output;
			}

			float4 frag(Vertex_Stage_Output input) : SV_TARGET
			{
				return float4(1.0, 1.0, 1.0, 1.0); // RGBA
			}

			ENDHLSL
		}
	}
	Fallback "Diffuse"
}