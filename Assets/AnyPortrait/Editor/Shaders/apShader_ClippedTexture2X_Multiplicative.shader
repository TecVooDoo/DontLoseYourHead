/*
*	Copyright (c) 2017-2018. RainyRizzle. All rights reserved
*	contact to : https://www.rainyrizzle.com/ , contactrainyrizzle@gmail.com
*
*	This file is part of AnyPortrait.
*
*	AnyPortrait can not be copied and/or distributed without
*	the express perission of Seungjik Lee.
*/

Shader "AnyPortrait/Editor/Clipped Colored Texture (2X) Multiplicative"
{
	//Masked Colored Texture와 달리
	//Child가 렌더링을 하는 구조이다.
	//Parent가 미리 MaskRenderTexture를 만들어서 줘야한다.
	//Multipass는 아니다.
	Properties
	{
		_Color("2X Color (RGBA Mul)", Color) = (0.5, 0.5, 0.5, 1.0)		// Main Color (2X Multiply) controlled by AnyPortrait
		_MainTex("Base Texture (RGBA)", 2D) = "white" {}				// Main Texture controlled by AnyPortrait
		_ScreenSize("Screen Size (xywh)", Vector) = (0, 0, 1, 1)		// ScreenSize for clipping in Editor
		
		_MaskRenderTexture("Mask Render Texture", 2D) = "black" {}		// Mask Texture for Clipping
		//_MaskColor("Mask Color (A)", Color) = (1, 1, 1, 1)				// Parent Mask Color

		_MaskRatio("Mask Ratio", Range(0, 1)) = 0						// v1.6.0 : Whether to apply mask

		_vColorITP("Vertex Color Ratio (0~1)", Range(0, 1)) = 0			// Vertex Color Interpolation Value for Weight Rendering

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
		//Blend SrcAlpha OneMinusSrcAlpha
		//Blend One One//Add
		//Blend OneMinusDstColor One//Soft Add
		//Blend DstColor Zero//Multiply
		Blend DstColor SrcColor//2X Multiply
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
				//float2 uv1 : TEXCOORD1;
				//float2 uv2 : TEXCOORD2;
				//float2 uv3 : TEXCOORD3;
				float4 screenPos : TEXCOORD4;
				//float4 worldPos : TEXCOORD5;
				//float3 vertColor : TEXCOORD6;
				
			};

			half4 _Color;
			sampler2D _MainTex;
			
			float4 _MainTex_ST;
			float4 _ScreenSize;
			
			sampler2D _MaskRenderTexture;

			float _vColorITP;
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
				o.pos = UnityObjectToClipPos(IN.vertex);

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


			half3 GetSeeThroughColor(half3 mainColor, half4 seeThroughColor)
			{
				float stAlpha = saturate(seeThroughColor.a * _SeeThroughAlpha * _SeeThroughRatio);
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

				//추가 v1.6.0 See-Through 연산을 RGB에 적용한다.
				half4 seeThroughColor = tex2D(_SeeThroughTex, screenUV);
				c.rgb = GetSeeThroughColor(c.rgb, seeThroughColor);

				c.rgb *= _Color.rgb * 2;

				//이전 (클리핑 마스크 하나만 적용)
				//c.a *= _Color.a * maskTexture.a * _MaskColor.a;

				//변경 v1.6.0
				//- 클리핑 마스크 / 채널별 마스크를 Ratio를 이용하여 순차적으로 적용한다.
				c.a *= _Color.a;//색상의 Alpha 채널값

				float maskResult = 1.0f;//마스크 알파값은 1부터 시작해서 줄어든다.

				//(1) 클리핑 마스크 (마스크 값은 R로 체크한다.)
				//maskResult *= GetMaskAlpha(saturate(maskTexture.r * _MaskColor.a), saturate(_MaskRatio));
				maskResult *= GetMaskAlpha(saturate(maskTexture.r), saturate(_MaskRatio));//변경 > 부모의 마스크 색상을 받을 필요가 없다. RT에 이미 반영됨

				//(2) 채널별 마스크
				float maskAlphaCh1 = tex2D(_MaskTex_1, screenUV).r;
				float maskAlphaCh2 = tex2D(_MaskTex_2, screenUV).r;
				float maskAlphaCh3 = tex2D(_MaskTex_3, screenUV).r;
				float maskAlphaCh4 = tex2D(_MaskTex_4, screenUV).r;
				
				maskResult = GetMaskAlphaByOp(maskResult, saturate(maskAlphaCh1), saturate(_MaskRatio_1), _MaskOp_1);
				maskResult = GetMaskAlphaByOp(maskResult, saturate(maskAlphaCh2), saturate(_MaskRatio_2), _MaskOp_2);
				maskResult = GetMaskAlphaByOp(maskResult, saturate(maskAlphaCh3), saturate(_MaskRatio_3), _MaskOp_3);
				maskResult = GetMaskAlphaByOp(maskResult, saturate(maskAlphaCh4), saturate(_MaskRatio_4), _MaskOp_4);

				c.a *= maskResult;
				
				//Vert Color
				c.rgb += IN.color;
				c.rgb = IN.color * _vColorITP + c.rgb * (1.0f - _vColorITP);

				////Additive인 경우
				//c.rgb *= c.a;

				////Multiply인 경우
				c.rgb = c.rgb * (c.a) + float4(0.5f, 0.5f, 0.5f, 1.0f) * (1.0f - c.a);
				c.a = 1.0f;

				return c;
			}
			ENDCG
		}

	}
	FallBack "Diffuse"
}
