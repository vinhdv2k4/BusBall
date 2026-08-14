Shader "CustomUI/SoftNeonBorder" {
	Properties {
		[HDR] _GlowColor ("Màu nê-ông bão hòa", Vector) = (1,0,0,1)
		_FadeWidth ("Độ rộng dải mờ vào trong", Range(0, 0.5)) = 0.2
		_PulseSpeed ("Tốc độ nhấp nháy", Float) = 4
		_PulseMin ("Độ tối của nhịp (0.0 - 1.0)", Range(0, 1)) = 0.4
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
}