/*
*	Copyright (c) RainyRizzle Inc. All rights reserved
*	Contact to : www.rainyrizzle.com , contactrainyrizzle@gmail.com
*
*	This file is part of [AnyPortrait].
*
*	AnyPortrait can not be copied and/or distributed without
*	the express permission of [Seungjik Lee] of [RainyRizzle team].
*
*	It is illegal to download files from other than the Unity Asset Store and RainyRizzle homepage.
*	In that case, the act could be subject to legal sanctions.
*/

Shader "AnyPortrait/Unlit (v16)/AlphaBlend Clipped"
{
	Properties
	{
		_Color("2X Color (RGBA Mul)", Color) = (0.5, 0.5, 0.5, 1.0)	// Main Color (2X Multiply) controlled by AnyPortrait
		_MainTex("Main Texture (RGBA)", 2D) = "white" {}			// Main Texture controlled by AnyPortrait

		//Clipped
		_MaskTex("Mask Texture (A)", 2D) = "white" {}				// Mask Texture for clipping Rendering (controlled by AnyPortrait)
		_MaskScreenSpaceOffset("Mask Screen Space Offset (XY_Scale)", Vector) = (0, 0, 0, 1)	// Mask Texture's Transform Offset (controlled by AnyPortrait)
		
		// [v16] Whether to apply mask
		_MaskRatio("Mask Ratio", Range(0, 1)) = 0

		// [v16] : Properties by Mask Data
		_MaskRatio_1("Mask Ratio Ch1", Range(0, 1)) = 0
		_MaskTex_1("Mask Texture Ch1", 2D) = "black" {}
		_MaskScreenSpaceOffset_1("Mask Screen Space Offset Ch1", Vector) = (0, 0, 0, 1)
		_MaskOp_1("Mask Operation Ch1", Range(0, 3)) = 0

		_MaskRatio_2("Mask Ratio Ch2", Range(0, 1)) = 0
		_MaskTex_2("Mask Texture Ch2", 2D) = "black" {}
		_MaskScreenSpaceOffset_2("Mask Screen Space Offset Ch2", Vector) = (0, 0, 0, 1)
		_MaskOp_2("Mask Operation Ch2", Range(0, 3)) = 0

		_MaskRatio_3("Mask Ratio Ch3", Range(0, 1)) = 0
		_MaskTex_3("Mask Texture Ch3", 2D) = "black" {}
		_MaskScreenSpaceOffset_3("Mask Screen Space Offset Ch3", Vector) = (0, 0, 0, 1)
		_MaskOp_3("Mask Operation Ch3", Range(0, 3)) = 0

		_MaskRatio_4("Mask Ratio Ch4", Range(0, 1)) = 0
		_MaskTex_4("Mask Texture Ch4", 2D) = "black" {}
		_MaskScreenSpaceOffset_4("Mask Screen Space Offset Ch4", Vector) = (0, 0, 0, 1)
		_MaskOp_4("Mask Operation Ch4", Range(0, 3)) = 0

		_SeeThroughRatio("See-Through Ratio", Range(0, 1)) = 0.0
		_SeeThroughTex("See-Through Texture", 2D) = "black" {}
		_SeeThroughScreenSpaceOffset("See-Through Screen Space Offset (XY_Scale)", Vector) = (0, 0, 0, 1)
		_SeeThroughAlpha("See-Through Alpha", Range(0, 1)) = 0.0
	}
	SubShader
	{
		Tags{ "RenderType" = "Transparent" "Queue" = "Transparent" "PreviewType" = "Plane" }
		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite Off
		LOD 200

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;

				//Screen Pos (Clipped)
				float4 screenPos : TEXCOORD1;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			half4 _Color;

			//Clipped
			sampler2D _MaskTex;
			float4 _MaskScreenSpaceOffset;

			//[v16]
			float _MaskRatio;

			//Mask Data (Channel 1~4)
			float _MaskRatio_1;
			sampler2D _MaskTex_1;
			float4 _MaskScreenSpaceOffset_1;
			float _MaskOp_1;

			float _MaskRatio_2;
			sampler2D _MaskTex_2;
			float4 _MaskScreenSpaceOffset_2;
			float _MaskOp_2;

			float _MaskRatio_3;
			sampler2D _MaskTex_3;
			float4 _MaskScreenSpaceOffset_3;
			float _MaskOp_3;

			float _MaskRatio_4;
			sampler2D _MaskTex_4;
			float4 _MaskScreenSpaceOffset_4;
			float _MaskOp_4;

			float _SeeThroughRatio;
			sampler2D _SeeThroughTex;
			float4 _SeeThroughScreenSpaceOffset;
			float _SeeThroughAlpha;


			// Mask Functions
			//-------------------------------------------------------------------------------------
			half GetMaskAlpha(float alpha, float ratio)
			{
				return saturate((alpha * ratio) + (1.0f * (1.0f - ratio)));
			}


			half GetMaskAlphaByOp(float prevMask, float alpha, float ratio, float op)
			{
				// Each Weight according to OP method (only one of the four values ​​is 1 and the rest are 0)
				float opWeight_And =		saturate(1.0f - abs(op - 0.0f));// op = 0 : AND (Multiply)
				float opWeight_Or =			saturate(1.0f - abs(op - 1.0f));// op = 1 : OR (Prev + Next * (1-Prev))
				float opWeight_InvAnd =		saturate(1.0f - abs(op - 2.0f));// op = 2 : Inverse AND
				float opWeight_InvOr =		saturate(1.0f - abs(op - 3.0f));// op = 3 : Inverse OR

				float inverseAlpha = 1.0f - alpha;

				float nextAlpha_And =		saturate(prevMask * alpha);//Multiply
				float nextAlpha_Or =		saturate(prevMask + (alpha * (1.0f - prevMask)));//Add Blended
				float nextAlpha_InvAnd =	saturate(prevMask * inverseAlpha);//Multiply (Inverse)
				float nextAlpha_InvAOr =	saturate(prevMask + (inverseAlpha * (1.0f - prevMask)));//Add Blended (Inverse)

				float resultMask = (nextAlpha_And * opWeight_And) + (nextAlpha_Or * opWeight_Or) + (nextAlpha_InvAnd * opWeight_InvAnd) + (nextAlpha_InvAOr * opWeight_InvOr);

				return saturate((resultMask * ratio) + (prevMask * (1.0f - ratio)));
			}

			float2 GetMaskScreenUV(float2 screenUV, float4 offset)
			{
				float2 result = screenUV - float2(0.5f, 0.5f);

				result.x *= offset.z;
				result.y *= offset.w;
				result.x += offset.x * offset.z;
				result.y += offset.y * offset.w;

				result += float2(0.5f, 0.5f);

				return result;
			}

			half3 GetSeeThroughColor(half3 mainColor, half4 seeThroughColor)
			{
				float stAlpha = saturate(seeThroughColor.a * _SeeThroughAlpha * _SeeThroughRatio);
				return (mainColor * (1.0f - stAlpha)) + (seeThroughColor.rgb * stAlpha);
			}

			//-------------------------------------------------------------------------------------
			



			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);

				//Screen Pos (Clipped)
				o.screenPos = ComputeScreenPos(o.vertex);

				return o;
			}

			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);

				// [ Clipped UV ]
				float2 screenUV = i.screenPos.xy / max(i.screenPos.w, 0.0001f);

				// [ See-Through ]
				half4 seeThroughColor = tex2D(_SeeThroughTex, GetMaskScreenUV(screenUV, _SeeThroughScreenSpaceOffset));
				col.rgb = GetSeeThroughColor(col.rgb, seeThroughColor);


				// In Gamma Space, col.rgb is multiplied by 2,
				// and in Linear Space, it is multiplied by 4.595 and then raised to the power of 2.2.
#if UNITY_COLORSPACE_GAMMA
				col.rgb *= _Color.rgb * 2.0f;//Gamma
#else
				col.rgb *= _Color.rgb * 4.595f;//Linear : pow(2, 2.2) = 4.595
				col.rgb = pow(col.rgb, 2.2f);//Linear
#endif

				col.a *= _Color.a;

				// [ Clipped / Mask Alpha ]
				// [v16] With Mask
				float maskResult = 1.0f;//Mask alpha values ​​start at 1 and decrease.

				//1. Clipping Mask
				half maskClipped = tex2D(_MaskTex, GetMaskScreenUV(screenUV, _MaskScreenSpaceOffset)).r;
				maskResult *= GetMaskAlpha(saturate(maskClipped), saturate(_MaskRatio));

				//2. Send Data Mask (4-Channels)
				float maskCh1 = tex2D(_MaskTex_1, GetMaskScreenUV(screenUV, _MaskScreenSpaceOffset_1)).r;
				float maskCh2 = tex2D(_MaskTex_2, GetMaskScreenUV(screenUV, _MaskScreenSpaceOffset_2)).r;
				float maskCh3 = tex2D(_MaskTex_3, GetMaskScreenUV(screenUV, _MaskScreenSpaceOffset_3)).r;
				float maskCh4 = tex2D(_MaskTex_4, GetMaskScreenUV(screenUV, _MaskScreenSpaceOffset_4)).r;

				maskResult = GetMaskAlphaByOp(maskResult, saturate(maskCh1), saturate(_MaskRatio_1), _MaskOp_1);
				maskResult = GetMaskAlphaByOp(maskResult, saturate(maskCh2), saturate(_MaskRatio_2), _MaskOp_2);
				maskResult = GetMaskAlphaByOp(maskResult, saturate(maskCh3), saturate(_MaskRatio_3), _MaskOp_3);
				maskResult = GetMaskAlphaByOp(maskResult, saturate(maskCh4), saturate(_MaskRatio_4), _MaskOp_4);

				col.a *= maskResult;
				//col.rgb *= maskColor;//Deleted [v16]

				return col;
			}
			ENDCG
		}

		//Pass added to create shadows (v16)
		Pass
        {
            Tags {"LightMode"="ShadowCaster"}
			ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

            struct v2f
			{ 
				float2 uv : TEXCOORD0;

				//Screen Pos (Clipped)
				float4 screenPos : TEXCOORD1;

                V2F_SHADOW_CASTER;
            };


			sampler2D _MainTex;
			float4 _MainTex_ST;
			half4 _Color;

			//Clipped
			sampler2D _MaskTex;
			float4 _MaskScreenSpaceOffset;
			float _MaskRatio;//[v16]

			//Mask Data (Channel 1~4)
			float _MaskRatio_1;
			sampler2D _MaskTex_1;
			float4 _MaskScreenSpaceOffset_1;
			float _MaskOp_1;

			float _MaskRatio_2;
			sampler2D _MaskTex_2;
			float4 _MaskScreenSpaceOffset_2;
			float _MaskOp_2;

			float _MaskRatio_3;
			sampler2D _MaskTex_3;
			float4 _MaskScreenSpaceOffset_3;
			float _MaskOp_3;

			float _MaskRatio_4;
			sampler2D _MaskTex_4;
			float4 _MaskScreenSpaceOffset_4;
			float _MaskOp_4;


			// Mask Functions
			//-------------------------------------------------------------------------------------
			half GetMaskAlpha(float alpha, float ratio)
			{
				return saturate((alpha * ratio) + (1.0f * (1.0f - ratio)));
			}


			half GetMaskAlphaByOp(float prevMask, float alpha, float ratio, float op)
			{
				// Each Weight according to OP method (only one of the four values ​​is 1 and the rest are 0)
				float opWeight_And =		saturate(1.0f - abs(op - 0.0f));// op = 0 : AND (Multiply)
				float opWeight_Or =			saturate(1.0f - abs(op - 1.0f));// op = 1 : OR (Prev + Next * (1-Prev))
				float opWeight_InvAnd =		saturate(1.0f - abs(op - 2.0f));// op = 2 : Inverse AND
				float opWeight_InvOr =		saturate(1.0f - abs(op - 3.0f));// op = 3 : Inverse OR

				float inverseAlpha = 1.0f - alpha;

				float nextAlpha_And =		saturate(prevMask * alpha);//Multiply
				float nextAlpha_Or =		saturate(prevMask + (alpha * (1.0f - prevMask)));//Add Blended
				float nextAlpha_InvAnd =	saturate(prevMask * inverseAlpha);//Multiply (Inverse)
				float nextAlpha_InvAOr =	saturate(prevMask + (inverseAlpha * (1.0f - prevMask)));//Add Blended (Inverse)

				float resultMask = (nextAlpha_And * opWeight_And) + (nextAlpha_Or * opWeight_Or) + (nextAlpha_InvAnd * opWeight_InvAnd) + (nextAlpha_InvAOr * opWeight_InvOr);

				return saturate((resultMask * ratio) + (prevMask * (1.0f - ratio)));
			}

			float2 GetMaskScreenUV(float2 screenUV, float4 offset)
			{
				float2 result = screenUV - float2(0.5f, 0.5f);

				result.x *= offset.z;
				result.y *= offset.w;
				result.x += offset.x * offset.z;
				result.y += offset.y * offset.w;

				result += float2(0.5f, 0.5f);

				return result;
			}
			//-------------------------------------------------------------------------------------


            v2f vert(appdata_base v)
            {
                v2f o;

                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
				
				o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);

				//Screen Pos (Clipped)
				float4 vertClipPos = UnityObjectToClipPos(v.vertex);
				o.screenPos = ComputeScreenPos(vertClipPos);

                return o;
            }


            float4 frag(v2f i) : SV_Target
            {
				fixed4 col = tex2D(_MainTex, i.uv);
				col.a *= _Color.a;

				// [ Clipped UV ]
				float2 screenUV = i.screenPos.xy / max(i.screenPos.w, 0.0001f);
				
				// Prev-Version
				//col.a *= tex2D(_MaskTex, screenUV).r;

				// [v16] With Mask
				float maskResult = 1.0f;//Mask alpha values ​​start at 1 and decrease.

				//1. Clipping Mask
				half maskClipped = tex2D(_MaskTex, GetMaskScreenUV(screenUV, _MaskScreenSpaceOffset)).r;
				maskResult *= GetMaskAlpha(saturate(maskClipped), saturate(_MaskRatio));

				//2. Send Data Mask (4-Channels)
				float maskCh1 = tex2D(_MaskTex_1, GetMaskScreenUV(screenUV, _MaskScreenSpaceOffset_1)).r;
				float maskCh2 = tex2D(_MaskTex_2, GetMaskScreenUV(screenUV, _MaskScreenSpaceOffset_2)).r;
				float maskCh3 = tex2D(_MaskTex_3, GetMaskScreenUV(screenUV, _MaskScreenSpaceOffset_3)).r;
				float maskCh4 = tex2D(_MaskTex_4, GetMaskScreenUV(screenUV, _MaskScreenSpaceOffset_4)).r;

				maskResult = GetMaskAlphaByOp(maskResult, saturate(maskCh1), saturate(_MaskRatio_1), _MaskOp_1);
				maskResult = GetMaskAlphaByOp(maskResult, saturate(maskCh2), saturate(_MaskRatio_2), _MaskOp_2);
				maskResult = GetMaskAlphaByOp(maskResult, saturate(maskCh3), saturate(_MaskRatio_3), _MaskOp_3);
				maskResult = GetMaskAlphaByOp(maskResult, saturate(maskCh4), saturate(_MaskRatio_4), _MaskOp_4);

				col.a *= maskResult;
				//-------------------------------------------

				if(col.a < 0.05f)
				{
					discard;
				}

                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
	}
}
