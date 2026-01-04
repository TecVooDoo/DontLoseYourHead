/*
*	Copyright (c) 2017-2018. RainyRizzle. All rights reserved
*	contact to : https://www.rainyrizzle.com/ , contactrainyrizzle@gmail.com
*
*	This file is part of AnyPortrait.
*
*	AnyPortrait can not be copied and/or distributed without
*	the express perission of Seungjik Lee.
*/

Shader "AnyPortrait/Editor/Masked Only"
{
	Properties
	{
		_Color("2X Color (RGBA Mul)", Color) = (0.5, 0.5, 0.5, 1.0)		// Main Color (2X Multiply) controlled by AnyPortrait
		_MainTex("Base Texture (RGBA)", 2D) = "white" {}				// Main Texture controlled by AnyPortrait
		_ScreenSize("Screen Size (xywh)", Vector) = (0, 0, 1, 1)		// ScreenSize for clipping in Editor
		_PosOffsetX("PosOffsetX", Float) = 0
		_PosOffsetY("PosOffsetY", Float) = 0


		// 체인 설정 때문에 Clipped 프로퍼티를 갖는다. [v1.6.0]
		_MaskRenderTexture("Mask Render Texture", 2D) = "black" {}
		_MaskRatio("Mask Ratio", Range(0, 1)) = 0

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
			ZTest Always
			Lighting Off
			//Cull Off

			//Stencil : "Z 테스트만 된다면 특정 값(53)을 저장해두자"
			/*stencil
			{
				ref 53
				comp Always
				pass replace
				fail zero
				zfail zero
			}*/

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0

			#include "UnityCG.cginc"

			struct vertexInput
			{
				float4 vertex : POSITION;
				float4 texcoord : TEXCOORD0;
				/*float3 vertColor_Black : TEXCOORD1;
				float3 vertColor_Red : TEXCOORD2;*/
			};

			struct vertexOutput
			{
				float4 pos : POSITION;
				float2 uv : TEXCOORD0;
				float4 screenPos : TEXCOORD1;
			};

			half4 _Color;
			sampler2D _MainTex;
			float _PosOffsetX;
			float _PosOffsetY;

			float4 _MainTex_ST;
			float4 _ScreenSize;

			//체인 설정
			sampler2D _MaskRenderTexture;
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

			vertexOutput vert(vertexInput IN)
			{
				vertexOutput o;
				//o.pos = mul(UNITY_MATRIX_MVP, IN.vertex);
				o.pos = UnityObjectToClipPos(IN.vertex);

				o.pos.x += _PosOffsetX;
				o.pos.y += _PosOffsetY;

				//o.color = IN.color;
				o.uv = TRANSFORM_TEX(IN.texcoord, _MainTex);
				o.screenPos = ComputeScreenPos(o.pos);
				return o;
			}


			// 체인용 Clipped 함수
			//-------------------------------------------------------------------
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
			//-------------------------------------------------------------------

			half4 frag(vertexOutput IN) : COLOR
			{
				half4 c = tex2D(_MainTex, IN.uv);

				//전에는 Clipping Parent의 Alpha값이 Child에 반영되므로,
				//Mask에 Parent(이 Shader)의 Alpha 값을 반영할 필요가 없다.
				c.a *= _Color.a;
				
				float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
				float2 clipScreenUV = screenUV;// *_ScreenParams.xy;
				clipScreenUV.y = 1.0f - clipScreenUV.y;


				float maskResult = 1.0f;//마스크 알파값은 1부터 시작해서 줄어든다.

				//(1) 클리핑 마스크 (마스크 값은 R로 체크한다.)
				half4 maskTexture = tex2D(_MaskRenderTexture, screenUV);
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



				if (clipScreenUV.x < _ScreenSize.x || clipScreenUV.x > _ScreenSize.z)
				{
					c.a = 0;
				}
				if (clipScreenUV.y < _ScreenSize.y || clipScreenUV.y > _ScreenSize.w)
				{
					c.a = 0;
				}
				
				//if (c.a < 0.5f)
				//{
				//	c.a = 0;
				//}

				c.rgb = half3(1, 1, 1);//White로 일반화. 색상값으로 마스크 체크를 하자

				
				return c;
			}
			ENDCG
		}
	}
	FallBack "Diffuse"
}
