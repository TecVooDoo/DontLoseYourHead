/*
*	Copyright (c) 2017-2018. RainyRizzle. All rights reserved
*	contact to : https://www.rainyrizzle.com/ , contactrainyrizzle@gmail.com
*
*	This file is part of AnyPortrait.
*
*	AnyPortrait can not be copied and/or distributed without
*	the express perission of Seungjik Lee.
*/

Shader "AnyPortrait/Editor/Linear/Clipped Colored Texture ToneColor (2X)"
{
	//Masked Colored Texture와 달리
	//Child가 렌더링을 하는 구조이다.
	//Parent가 미리 MaskRenderTexture를 만들어서 줘야한다.
	//Multipass는 아니다.
	Properties
	{
		_Color("2X Tone Color (RGBA Mul)", Color) = (0.5, 0.5, 0.5, 1.0)	// Main Color (2X Multiply) controlled by AnyPortrait
		_MainTex("Base Texture (RGBA)", 2D) = "white" {}					// Main Texture controlled by AnyPortrait
		_ScreenSize("Screen Size (xywh)", Vector) = (0, 0, 1, 1)			// ScreenSize for clipping in Editor
		_MaskRenderTexture("Mask Render Texture", 2D) = "black" {}			// Mask Texture for Clipping
		//_MaskColor("Mask Color (A)", Color) = (1, 1, 1, 1)					// Parent Mask Color
		_MaskRatio("Mask Ratio", Range(0, 1)) = 0						// v1.6.0 : Whether to apply mask

		//_vColorITP("Vertex Color Ratio (0~1)", Range(0, 1)) = 0
		_Thickness("Thickness (0~1)", Range(0, 1)) = 0.5
		_ShapeRatio("ShapeRatio(0 : Outline / 1 : Solid)", Range(0, 1)) = 0
		_PosOffsetX("PosOffsetX", Float) = 0
		_PosOffsetY("PosOffsetY", Float) = 0

		// v1.6.0 : Properties by Mask Data
		_MaskRatio_1("Mask Ratio Ch1", Range(0, 1)) = 0
		_MaskTex_1("Mask Texture Ch1", 2D) = "black" {}
		_MaskOp_1("Mask Operation Ch1", Range(0, 3)) = 0

		_MaskRatio_2("Mask Ratio Ch2", Range(0, 1)) = 0
		_MaskTex_2("Mask Texture Ch2", 2D) = "black" {}
		_MaskOp_2("Mask Operation Ch2", Range(0, 3)) = 0

		_MaskRatio_3("Mask Ratio Ch3", Range(0, 1)) = 0
		_MaskTex_3("Mask Texture Ch3", 2D) = "black" {}
		_MaskOp_3("Mask Operation Ch3", Range(0, 3)) = 0

		_MaskRatio_4("Mask Ratio Ch4", Range(0, 1)) = 0
		_MaskTex_4("Mask Texture Ch4", 2D) = "black" {}
		_MaskOp_4("Mask Operation Ch4", Range(0, 3)) = 0

		_SeeThroughRatio("See-Through Ratio", Range(0, 1)) = 0.0
		_SeeThroughTex("See-Through Texture", 2D) = "black" {}
		_SeeThroughAlpha("See-Through Alpha", Range(0, 1)) = 0.0
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent" "Queue" = "Transparent" "PreviewType" = "Plane" }
		Blend SrcAlpha OneMinusSrcAlpha
		//Blend One One//Add
		//Blend OneMinusDstColor One//Soft Add
		//Blend DstColor Zero//Multiply
		//Blend DstColor SrcColor//2X Multiply
		LOD 200

		

		//----------------------------------------------------------------------------------
		Pass
		{
			//ColorMask RGB
			ZWrite off
			//ZTest Always
			//Cull Off

			LOD 200

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0

			#include "UnityCG.cginc"

			struct vertexInput
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
				float4 texcoord : TEXCOORD0;
				//float3 vertColor : TEXCOORD1;
			};

			struct vertexOutput
			{
				float4 pos : POSITION;
				float4 color : COLOR;
				float2 uv : TEXCOORD0;
				float4 screenPos : TEXCOORD4;
				
				
			};

			half4 _Color;
			sampler2D _MainTex;
			
			float4 _MainTex_ST;
			float4 _ScreenSize;
			float _Thickness;
			fixed _ShapeRatio;
			float _PosOffsetX;
			float _PosOffsetY;
			
			sampler2D _MaskRenderTexture;

			//float4 _MaskColor;

			float _MaskRatio;

			//Mask Data (Channel 1~4)
			float _MaskRatio_1;
			sampler2D _MaskTex_1;
			float _MaskOp_1;

			float _MaskRatio_2;
			sampler2D _MaskTex_2;
			float _MaskOp_2;

			float _MaskRatio_3;
			sampler2D _MaskTex_3;
			float _MaskOp_3;

			float _MaskRatio_4;
			sampler2D _MaskTex_4;
			float _MaskOp_4;

			float _SeeThroughRatio;
			sampler2D _SeeThroughTex;
			float _SeeThroughAlpha;




			vertexOutput vert(vertexInput IN)
			{
				vertexOutput o;
				//o.pos = mul(UNITY_MATRIX_MVP, IN.vertex);
				o.pos = UnityObjectToClipPos(IN.vertex + float4(_PosOffsetX, _PosOffsetY, 0, 0));

				//o.pos.x += _PosOffsetX;
				//o.pos.y += _PosOffsetY;

				o.color = IN.color;

				o.uv = TRANSFORM_TEX(IN.texcoord, _MainTex);
				o.screenPos = ComputeScreenPos(o.pos);
				//o.worldPos = o.pos;
				return o;
			}


			half GetMaskAlpha(float alpha, float ratio)
			{
				return saturate((alpha * ratio) + (1.0f * (1.0f - ratio)));
			}

			half GetMaskAlphaByOp(float prevMask, float alpha, float ratio, float op)
			{
				// OP 방식에 따른 각각의 Weight (4개 값중 하나만 1이고 나머지는 0)
				float opWeight_And =		saturate(1.0f - abs(op - 0.0f));// AND : op = 0 (Multiply 연산)
				float opWeight_Or =			saturate(1.0f - abs(op - 1.0f));// OR : op = 1 (Blended Add 연산 : Prev + Next * (1-Prev))
				float opWeight_InvAnd =		saturate(1.0f - abs(op - 2.0f));// Inverse AND : op = 2 (값 반전 후 Min 연산)
				float opWeight_InvOr =		saturate(1.0f - abs(op - 3.0f));// Inverse OR : op = 3 (값 반전 후 Max 연산)

				float inverseAlpha = 1.0f - alpha;

				float nextAlpha_And =		saturate(prevMask * alpha);//Multiply
				float nextAlpha_Or =		saturate(prevMask + (alpha * (1.0f - prevMask)));//Add Blended
				float nextAlpha_InvAnd =	saturate(prevMask * inverseAlpha);//Multiply (Inverse)
				float nextAlpha_InvAOr =	saturate(prevMask + (inverseAlpha * (1.0f - prevMask)));//Add Blended (Inverse)

				float resultMask = (nextAlpha_And * opWeight_And) + (nextAlpha_Or * opWeight_Or) + (nextAlpha_InvAnd * opWeight_InvAnd) + (nextAlpha_InvAOr * opWeight_InvOr);

				return saturate((resultMask * ratio) + (prevMask * (1.0f - ratio)));
			}

			half3 GetSeeThroughColor(half3 mainColor, half4 seeThroughColor, float seeThroughRatio)
			{
				//먼저 SeeThrough를 Alpha Blend로 더하기
				float stAlpha = saturate(seeThroughColor.a * _SeeThroughAlpha);
				return (mainColor * (1.0f - stAlpha)) + (seeThroughColor.rgb * stAlpha);
			}



			half4 frag(vertexOutput IN) : COLOR
			{
				half4 c = tex2D(_MainTex, IN.uv);

				float2 screenUV = IN.screenPos.xy;// *_ScreenParams.xy;
				float2 clipScreenUV = screenUV;// *_ScreenParams.xy;
				clipScreenUV.y = 1.0f - clipScreenUV.y;
				//float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
				//screenUV.y = 1.0f - screenUV.y;

				half4 maskTexture = tex2D(_MaskRenderTexture, screenUV);

				if (clipScreenUV.x < _ScreenSize.x || clipScreenUV.x > _ScreenSize.z)
				{
					c.a = 0.0f;
					discard;
				}
				if (clipScreenUV.y < _ScreenSize.y || clipScreenUV.y > _ScreenSize.w)
				{
					c.a = 0.0f;
					discard;
				}

				float sampleDigonal = (0.005f * (1.0f - _Thickness)) + (0.015f * _Thickness);//기본 0.01
				float sampleCross = (0.005f * (1.0f - _Thickness)) + (0.015f * _Thickness);//기본 0.015

				half a_0 = tex2D(_MainTex, IN.uv + float2(sampleDigonal, sampleDigonal)).a;
				half a_1 = tex2D(_MainTex, IN.uv + float2(-sampleDigonal, sampleDigonal)).a;
				half a_2 = tex2D(_MainTex, IN.uv + float2(sampleDigonal, -sampleDigonal)).a;
				half a_3 = tex2D(_MainTex, IN.uv + float2(-sampleDigonal, -sampleDigonal)).a;

				half a_4 = tex2D(_MainTex, IN.uv + float2(sampleCross, 0)).a;
				half a_5 = tex2D(_MainTex, IN.uv + float2(0, sampleCross)).a;
				half a_6 = tex2D(_MainTex, IN.uv + float2(-sampleCross, 0)).a;
				half a_7 = tex2D(_MainTex, IN.uv + float2(0, -sampleCross)).a;

				half outlineItp = 1 - ((a_0 + a_1 + a_2 + a_3 + a_4 + a_5 + a_6 + a_7) / 8.0f); // 0~1 => 0.2 ~ 1 
				outlineItp = (outlineItp * 0.8f) + 0.2f;


				float grayScale = c.r * 0.3f + c.g * 0.6f + c.b * 0.1f;
				c.rgb = grayScale;//<<GrayScale
				c.rgb *= 2.0f;

				//c.rgb *= _Color.rgb * 2;
				c.rgb *= _Color.rgb * 4.595f;//Linear
				
				//이전 : 단순 클리핑 마스크 연산 x 톤 색상
				//c.a = c.a * _Color.a * maskTexture.a * (outlineItp * (1 - _ShapeRatio) + _ShapeRatio);

				//변경 v1.6.0 > 마스크 좌표 오류로, Tone Color에서는 클리핑 마스크 처리를 하지 않는다.
				//톤 색상만 적용
				c.a *= outlineItp * (1 - _ShapeRatio) + _ShapeRatio;

				
				return c;
			}
			ENDCG
		}

	}
	FallBack "Diffuse"
}
