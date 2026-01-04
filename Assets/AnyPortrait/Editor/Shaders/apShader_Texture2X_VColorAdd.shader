/*
*	Copyright (c) 2017-2018. RainyRizzle. All rights reserved
*	contact to : https://www.rainyrizzle.com/ , contactrainyrizzle@gmail.com
*
*	This file is part of AnyPortrait.
*
*	AnyPortrait can not be copied and/or distributed without
*	the express perission of Seungjik Lee.
*/
Shader "AnyPortrait/Editor/Colored Texture VColor Add(2X)"
{
	Properties
	{
		_Color("2X Color (RGBA Mul)", Color) = (0.5, 0.5, 0.5, 1.0)		// Main Color (2X Multiply) controlled by AnyPortrait
		_MainTex("Albedo (RGBA)", 2D) = "white" {}						// Main Texture controlled by AnyPortrait
		_ScreenSize("Screen Size (xywh)", Vector) = (0, 0, 1, 1)		// ScreenSize for clipping in Editor
		_vColorITP("Vertex Color Ratio (0~1)", Range(0, 1)) = 0			// Vertex Color Interpolation Value for Weight Rendering
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent" "Queue" = "Transparent" "PreviewType" = "Plane" }
		Blend SrcAlpha OneMinusSrcAlpha
		LOD 500

		//----------------------------------------------------------------------------------
		Pass
		{
			//ColorMask 0
			ZWrite off//<이 단계에서는 Zwrite를 하지 않는다.
			Lighting Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0

			#include "UnityCG.cginc"

			struct vertexInput
			{
				float4 vertex : POSITION;
				float4 texcoord : TEXCOORD0;
				float3 vColor : COLOR;
				/*float3 vertColor_Black : TEXCOORD1;
				float3 vertColor_Red : TEXCOORD2;*/
			};

			struct vertexOutput
			{
				float4 pos : POSITION;
				float2 uv : TEXCOORD0;
				float4 screenPos : TEXCOORD1;
				float3 vColor : TEXCOORD2;
			};

			half4 _Color;
			sampler2D _MainTex;
			float4 _MainTex_ST;

			float4 _ScreenSize;
			float _vColorITP;

			vertexOutput vert(vertexInput IN)
			{
				vertexOutput o;
				//o.pos = mul(UNITY_MATRIX_MVP, IN.vertex);
				o.pos = UnityObjectToClipPos(IN.vertex);

				//o.color = IN.color;
				o.uv = TRANSFORM_TEX(IN.texcoord, _MainTex);
				o.screenPos = ComputeScreenPos(o.pos);

				o.vColor = IN.vColor;
				return o;
			}

			half4 frag(vertexOutput IN) : COLOR
			{
				float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
				screenUV.y = 1.0f - screenUV.y;

				half4 c = tex2D(_MainTex, IN.uv);

				c.rgb *= _Color.rgb * 2.0f;
				c.rgb += IN.vColor;
			
				c.rgb = IN.vColor * _vColorITP + c.rgb * (1.0f - _vColorITP);

				c.a *= _Color.a;
			
				if (screenUV.x < _ScreenSize.x || screenUV.x > _ScreenSize.z)
				{
					c.a = 0;
					discard;
				}
				if (screenUV.y < _ScreenSize.y || screenUV.y > _ScreenSize.w)
				{
					c.a = 0;
					discard;
				}
				
				return c;
			}
			ENDCG
		}


		//Tags{ "RenderType" = "Transparent" "Queue" = "Transparent" }
		//Blend SrcAlpha OneMinusSrcAlpha
		
		////Cull Off

		//LOD 200

		//CGPROGRAM
		//// Physically based Standard lighting model, and enable shadows on all light types
		//#pragma surface surf SimpleColor alpha

		//// Use shader model 3.0 target, to get nicer looking lighting
		//#pragma target 3.0

		//half4 LightingSimpleColor(SurfaceOutput s, half3 lightDir, half atten)
		//{
		//	half4 c;
		//	c.rgb = s.Albedo;
		//	c.a = s.Alpha;
		//	return c;

		//}

		//half4 _Color;
		//sampler2D _MainTex;
		//float4 _ScreenSize;
		//float _vColorITP;

		//struct Input
		//{
		//	float2 uv_MainTex;
		//	float4 color : COLOR;
		//	float4 screenPos;
		//};


		//void surf(Input IN, inout SurfaceOutput o)
		//{
		//	float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
		//	screenUV.y = 1.0f - screenUV.y;

		//	half4 c = tex2D(_MainTex, IN.uv_MainTex);
		//	c.rgb *= _Color.rgb * 2.0f;
		//	c.rgb += IN.color;
			
		//	c.rgb = IN.color * _vColorITP + c.rgb * (1.0f - _vColorITP);

			
		//	o.Alpha = c.a * _Color.a;
			
		//	if (screenUV.x < _ScreenSize.x || screenUV.x > _ScreenSize.z)
		//	{
		//		o.Alpha = 0;
		//		discard;
		//	}
		//	if (screenUV.y < _ScreenSize.y || screenUV.y > _ScreenSize.w)
		//	{
		//		o.Alpha = 0;
		//		discard;
		//	}
		//	o.Albedo = c.rgb;
		//}
		//ENDCG
	}
	FallBack "Diffuse"
}
