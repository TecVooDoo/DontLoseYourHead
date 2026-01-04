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

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;

using AnyPortrait;

namespace AnyPortrait
{
	// apGL 중에서 Batch Sub Class만 여기서 정의한다.

	public static partial class apGL
	{
        public class MaterialBatch
		{
			public enum MatType
			{
				None, Color,
				Texture_Normal, Texture_VColorAdd,
				//MaskedTexture,//<<구형 방식
				MaskOnly, Clipped,
				GUITexture,
				ToneColor_Normal, ToneColor_Clipped,
				
				//Capture용 Shader
				Alpha2White,

				//추가 20.3.20 : 본 렌더링의 v2 방식
				BoneV2,
				//추가 20.3.25 : 텍스쳐*VertColor (_Color없음)
				Texture_VColorMul,
				//리깅용 Circle Vert v2 방식
				RigCircleV2,

				//추가 21.2.16 : 비활성화된 객체 선택
				Gray_Normal, Gray_Clipped,

				//추가 22.4.12 [v1.4.0] 버텍스, 핀 그리기
				VertexAndPin,
			}


			private Material _mat_Color = null;
			private Material _mat_MaskOnly = null;

			//추가 : 일반 Texture Transparent같지만 GUI 전용이며 _Color가 없고 Vertex Color를 사용하여 Batch하기에 좋다.
			private Material _mat_GUITexture = null;

			private Material[] _mat_Texture_Normal = null;
			private Material[] _mat_Texture_VColorAdd = null;
			//private Material[] _mat_MaskedTexture = null;
			private Material[] _mat_Clipped = null;

			private Material _mat_ToneColor_Normal = null;
			private Material _mat_ToneColor_Clipped = null;

			private Material _mat_Alpha2White = null;

			private Material _mat_BoneV2 = null;//추가 20.3.20
			private Material _mat_Texture_VColorMul = null;//추가 20.3.25
			private Material _mat_RigCircleV2 = null;

			private Material _mat_Gray_Normal = null;//추가 21.2.16 : 비활성화된 객체를 표현하기 위한 재질
			private Material _mat_Gray_Clipped = null;//추가 21.2.16 : 비활성화된 객체를 표현하기 위한 재질

			private Material _mat_VertAndPin = null;//추가 22.4.12 : 버텍스와 핀 재질

			private MatType _matType = MatType.None;

			//마지막 입력 값
			private Vector4 _glScreenClippingSize = Vector4.zero;

			public Color _color = Color.black;
			private Texture2D _texture = null;


			//추가 21.5.18 : SetPass, Begin, End 호출 횟수를 줄이기 위해서, 이전 요청과 동일하면 Begin을 하지 않는다.
			//설명
			//: 이전에는 무조건 Begin+SetPass > End
			//: Begin-End를 직접 명시하지 않는다.
			//> DynamicBegin > DynamicBegin > ... > ForceEnd 로 호출한다. 즉, 마지막만 End 호출
			/// <summary>이전에 렌더링을 하고 있는 중이었는가.</summary>
			private bool _isRenderingBegun = false;
			private int _lastGLMode = -1;

			private float _lastToneLineThickness = 0.0f;
			private float _lastToneShapeRatio = 0.0f;
			private Vector2 _lastTonePosOffset = Vector2.zero;
			private float _lastVertColorRatio = 0.0f;
			//private Color _lastParentColor = Color.black;
			//클리핑된건 병합을 막자
			//private RenderTexture _lastRenderTexture = null;
			//private Texture2D _lastMaskedTexutre = null;

			private const float PASS_EQUAL_BIAS = 0.001f;


			//마스크 버전은 좀 많다..
			private RenderTexture _renderTexture = null;
			private int _renderTextureSize_Width = -1;
			private int _renderTextureSize_Height = -1;
			public RenderTexture RenderTex { get { return _renderTexture; } }

			public const int ALPHABLEND = 0;
			public const int ADDITIVE = 1;
			public const int SOFT_ADDITIVE = 2;
			public const int MULTIPLICATIVE = 3;

			private apPortrait.SHADER_TYPE _shaderType_Main = apPortrait.SHADER_TYPE.AlphaBlend;
			private int _iShaderType_Main = -1;
			
			//쉐이더 프로퍼티 인덱스
			private int _propertyID__ScreenSize = -1;
			private int _propertyID__Color = -1;
			private int _propertyID__MainTex = -1;
			private int _propertyID__Thickness = -1;
			private int _propertyID__ShapeRatio = -1;
			private int _propertyID__PosOffsetX = -1;
			private int _propertyID__PosOffsetY = -1;
			private int _propertyID__vColorITP = -1;
			private int _propertyID__MaskRenderTexture = -1;
			//private int _propertyID__MaskColor = -1;

			//v1.6.0 : 마스크 프로퍼티 추가
			private int _propertyID__MaskRatio = -1;

			private int _propertyID__MaskRatio_1 = -1;
			private int _propertyID__MaskTex_1= -1;
			private int _propertyID__MaskOp_1 = -1;

			private int _propertyID__MaskRatio_2 = -1;
			private int _propertyID__MaskTex_2 = -1;
			private int _propertyID__MaskOp_2 = -1;

			private int _propertyID__MaskRatio_3 = -1;
			private int _propertyID__MaskTex_3 = -1;
			private int _propertyID__MaskOp_3 = -1;

			private int _propertyID__MaskRatio_4 = -1;
			private int _propertyID__MaskTex_4 = -1;
			private int _propertyID__MaskOp_4 = -1;

			private int _propertyID__SeeThroughRatio = -1;
			private int _propertyID__SeeThroughTex = -1;
			private int _propertyID__SeeThroughAlpha = -1;

			

			public MaterialBatch()
			{
				
			}

			public void SetShader(Shader shader_Color,
									Shader[] shader_Texture_Normal_Set,
									Shader[] shader_Texture_VColorAdd_Set,
									//Shader[] shader_MaskedTexture_Set,
									Shader shader_MaskOnly,
									Shader[] shader_Clipped_Set,
									Shader shader_GUITexture,
									Shader shader_ToneColor_Normal,
									Shader shader_ToneColor_Clipped,
									Shader shader_Alpha2White,
									Shader shader_BoneV2, Texture2D uniformTexture_BoneSpriteSheet,
									Shader shader_Texture_VColorMul,
									Shader shader_RigCircleV2, Texture2D uniformTexture_RigCircle,
									Shader shader_Gray_Normal, Shader shader_Gray_Clipped,
									Shader shader_VertPin, Texture2D uniformTexture_VertPinAtlas
									)
			{
				//_mat_Color = mat_Color;
				//_mat_Texture = mat_Texture;
				//_mat_MaskedTexture = mat_MaskedTexture;

				_mat_Color = new Material(shader_Color);
				_mat_Color.color = new Color(1, 1, 1, 1);

				_mat_MaskOnly = new Material(shader_MaskOnly);
				_mat_MaskOnly.color = new Color(0.5f, 0.5f, 0.5f, 1.0f);

				//추가 : GUI용 텍스쳐
				_mat_GUITexture = new Material(shader_GUITexture);


				//AlphaBlend, Add, SoftAdditive
				_mat_Texture_Normal = new Material[4];
				_mat_Texture_VColorAdd = new Material[4];
				//_mat_MaskedTexture = new Material[4];
				_mat_Clipped = new Material[4];

				for (int i = 0; i < 4; i++)
				{
					_mat_Texture_Normal[i] = new Material(shader_Texture_Normal_Set[i]);
					_mat_Texture_Normal[i].color = new Color(0.5f, 0.5f, 0.5f, 1.0f);

					_mat_Texture_VColorAdd[i] = new Material(shader_Texture_VColorAdd_Set[i]);
					_mat_Texture_VColorAdd[i].color = new Color(0.5f, 0.5f, 0.5f, 1.0f);

					//_mat_MaskedTexture[i] = new Material(shader_MaskedTexture_Set[i]);
					//_mat_MaskedTexture[i].color = new Color(0.5f, 0.5f, 0.5f, 1.0f);

					_mat_Clipped[i] = new Material(shader_Clipped_Set[i]);
					_mat_Clipped[i].color = new Color(0.5f, 0.5f, 0.5f, 1.0f);
				}

				_mat_ToneColor_Normal = new Material(shader_ToneColor_Normal);
				_mat_ToneColor_Normal.color = new Color(0.5f, 0.5f, 0.5f, 1.0f);

				_mat_ToneColor_Clipped = new Material(shader_ToneColor_Clipped);
				_mat_ToneColor_Clipped.color = new Color(0.5f, 0.5f, 0.5f, 1.0f);

				_mat_Alpha2White = new Material(shader_Alpha2White);
				_mat_Alpha2White.color = new Color(0.5f, 0.5f, 0.5f, 1.0f);

				_mat_BoneV2 = new Material(shader_BoneV2);
				if (_mat_BoneV2 != null && uniformTexture_BoneSpriteSheet != null)
				{
					_mat_BoneV2.mainTexture = uniformTexture_BoneSpriteSheet;
				}

				_mat_Texture_VColorMul = new Material(shader_Texture_VColorMul);

				_mat_RigCircleV2 = new Material(shader_RigCircleV2);
				if(_mat_RigCircleV2 != null && uniformTexture_RigCircle != null)
				{
					_mat_RigCircleV2.mainTexture = uniformTexture_RigCircle;
				}

				//추가 21.2.16 : 비활성화된 객체를 표현하기 위한 재질
				_mat_Gray_Normal = new Material(shader_Gray_Normal);
				_mat_Gray_Clipped = new Material(shader_Gray_Clipped);

				//추가 22.4.12 : 핀-버텍스 쉐이더.
				_mat_VertAndPin = new Material(shader_VertPin);
				if(_mat_VertAndPin != null && uniformTexture_VertPinAtlas != null)
				{
					_mat_VertAndPin.mainTexture = uniformTexture_VertPinAtlas;
				}


				//쉐이더 프로퍼티
				_propertyID__ScreenSize =	Shader.PropertyToID("_ScreenSize");
				_propertyID__Color =		Shader.PropertyToID("_Color");
				_propertyID__MainTex =		Shader.PropertyToID("_MainTex");
				_propertyID__Thickness =	Shader.PropertyToID("_Thickness");
				_propertyID__ShapeRatio =	Shader.PropertyToID("_ShapeRatio");
				_propertyID__PosOffsetX =	Shader.PropertyToID("_PosOffsetX");
				_propertyID__PosOffsetY =	Shader.PropertyToID("_PosOffsetY");
				_propertyID__vColorITP =	Shader.PropertyToID("_vColorITP");
				_propertyID__MaskRenderTexture = Shader.PropertyToID("_MaskRenderTexture");
				//_propertyID__MaskColor =	Shader.PropertyToID("_MaskColor");//삭제 v1.6.0

				_propertyID__MaskRatio = Shader.PropertyToID("_MaskRatio");

				_propertyID__MaskRatio_1 = Shader.PropertyToID("_MaskRatio_1");
				_propertyID__MaskTex_1= Shader.PropertyToID("_MaskTex_1");
				_propertyID__MaskOp_1 = Shader.PropertyToID("_MaskOp_1");

				_propertyID__MaskRatio_2 = Shader.PropertyToID("_MaskRatio_2");
				_propertyID__MaskTex_2 = Shader.PropertyToID("_MaskTex_2");
				_propertyID__MaskOp_2 = Shader.PropertyToID("_MaskOp_2");

				_propertyID__MaskRatio_3 = Shader.PropertyToID("_MaskRatio_3");
				_propertyID__MaskTex_3 = Shader.PropertyToID("_MaskTex_3");
				_propertyID__MaskOp_3 = Shader.PropertyToID("_MaskOp_3");

				_propertyID__MaskRatio_4 = Shader.PropertyToID("_MaskRatio_4");
				_propertyID__MaskTex_4 = Shader.PropertyToID("_MaskTex_4");
				_propertyID__MaskOp_4 = Shader.PropertyToID("_MaskOp_4");

				_propertyID__SeeThroughRatio = Shader.PropertyToID("_SeeThroughRatio");
				_propertyID__SeeThroughTex = Shader.PropertyToID("_SeeThroughTex");
				_propertyID__SeeThroughAlpha = Shader.PropertyToID("_SeeThroughAlpha");

				_isRenderingBegun = false;
				_lastGLMode = -1;
			}


			public void SetClippingSize(Vector4 screenSize)
			{
				_glScreenClippingSize = screenSize;

				Material mat = null;
				switch (_matType)
				{
					case MatType.Color: mat = _mat_Color; break;
					case MatType.Texture_Normal: mat = _mat_Texture_Normal[_iShaderType_Main]; break;
					case MatType.Texture_VColorAdd: mat = _mat_Texture_VColorAdd[_iShaderType_Main]; break;
					case MatType.Clipped: mat = _mat_Clipped[_iShaderType_Main]; break;
					case MatType.MaskOnly: mat = _mat_MaskOnly; break;
					case MatType.GUITexture: mat = _mat_GUITexture; break;
					case MatType.ToneColor_Normal: mat = _mat_ToneColor_Normal; break;
					case MatType.ToneColor_Clipped: mat = _mat_ToneColor_Clipped; break;
					case MatType.Alpha2White: mat = _mat_Alpha2White; break;
					case MatType.BoneV2: if(_mat_BoneV2 != null) { mat = _mat_BoneV2; } break;
					case MatType.Texture_VColorMul: mat = _mat_Texture_VColorMul; break;
					case MatType.RigCircleV2: mat = _mat_RigCircleV2; break;
					case MatType.Gray_Normal: mat = _mat_Gray_Normal; break;
					case MatType.Gray_Clipped: mat = _mat_Gray_Clipped; break;
					case MatType.VertexAndPin: mat = _mat_VertAndPin; break;
				}

				if(mat != null)
				{
					mat.SetVector(_propertyID__ScreenSize, _glScreenClippingSize);//_ScreenSize
				}
				//GL.Flush();
			}


			public void SetClippingSizeToAllMaterial(Vector4 screenSize)
			{
				_glScreenClippingSize = screenSize;
				//_ScreenSize

				//TODO : 이게 버그? > 체크할 것
				if(IsNotReady())
				{
					//Debug.LogError("SetClippingSizeToAllMaterial Error");
					return;
				}

				_mat_Color.SetVector(_propertyID__ScreenSize, _glScreenClippingSize);
				for (int i = 0; i < 4; i++)
				{
					_mat_Texture_Normal[i].SetVector(_propertyID__ScreenSize, _glScreenClippingSize);
					_mat_Texture_VColorAdd[i].SetVector(_propertyID__ScreenSize, _glScreenClippingSize);
					_mat_Clipped[i].SetVector(_propertyID__ScreenSize, _glScreenClippingSize);
				}
				
				_mat_MaskOnly.SetVector(_propertyID__ScreenSize, _glScreenClippingSize);
				_mat_GUITexture.SetVector(_propertyID__ScreenSize, _glScreenClippingSize);
				_mat_ToneColor_Normal.SetVector(_propertyID__ScreenSize, _glScreenClippingSize);
				_mat_ToneColor_Clipped.SetVector(_propertyID__ScreenSize, _glScreenClippingSize);
				_mat_Alpha2White.SetVector(_propertyID__ScreenSize, _glScreenClippingSize);
				
				if (_mat_BoneV2 != null)
				{
					//Bone Material은 Null일 수 있다.
					_mat_BoneV2.SetVector(_propertyID__ScreenSize, _glScreenClippingSize);
				}
				_mat_Texture_VColorMul.SetVector(_propertyID__ScreenSize, _glScreenClippingSize);
				_mat_RigCircleV2.SetVector(_propertyID__ScreenSize, _glScreenClippingSize);//_ScreenSize

				_mat_Gray_Normal.SetVector(_propertyID__ScreenSize, _glScreenClippingSize);//_ScreenSize
				_mat_Gray_Clipped.SetVector(_propertyID__ScreenSize, _glScreenClippingSize);//_ScreenSize
				if(_mat_VertAndPin != null)
				{
					_mat_VertAndPin.SetVector(_propertyID__ScreenSize, _glScreenClippingSize);//_ScreenSize
				}
				
			}

			/// <summary>
			/// RenderTexture를 사용하는 GL계열에서는 이 함수를 윈도우 크기 호출시에 같이 호출한다.
			/// </summary>
			/// <param name="windowWidth"></param>
			/// <param name="windowHeight"></param>
			public void CheckMaskTexture(int windowWidth, int windowHeight)
			{
				//if(_renderTexture == null || _renderTextureSize_Width != windowWidth || _renderTextureSize_Height != windowHeight)
				//{
				//	if(_renderTexture != null)
				//	{
				//		//UnityEngine.Object.DestroyImmediate(_renderTexture);
				//		RenderTexture.ReleaseTemporary(_renderTexture);
				//		_renderTexture = null;
				//	}
				//	//_renderTexture = new RenderTexture(windowWidth, windowHeight, 24);
				//	_renderTexture = RenderTexture.GetTemporary(windowWidth, windowHeight, 24);
				//	_renderTexture.wrapMode = TextureWrapMode.Clamp;
				//	_renderTextureSize_Width = windowWidth;
				//	_renderTextureSize_Height = windowHeight;
				//}

				_renderTextureSize_Width = Mathf.Max(windowWidth, 4);
				_renderTextureSize_Height = Mathf.Max(windowHeight, 4);
			}

			//변경 21.5.18 : 다이나믹 Begin-End 방식으로 모두 변경하자
			//렌더링 중이었으면
			// > 연속적으로 Pass를 유지할 수 없다면 > End 후 Pass+Begin 시작
			// > 연속적으로 Pass를 유지할 수 있다면 > 리턴
			//렌더링 중이 아니었다면
			// > Pass 시작

			/// <summary>
			/// 강제로 현재 Pass를 종료한다. (렌더링중인 Pass가 있다면 동작. 그렇지 않으면 무시한다.
			/// 렌더링 단계가 종료되었거나 Screen Space가 바뀌면 꼭 호출한다.
			/// </summary>
			public void EndPass()
			{
				if(!_isRenderingBegun)
				{
					return;
				}

				GL.End();
				GL.Flush();

				_isRenderingBegun = false;
				_lastGLMode = -1;

				_lastToneLineThickness = 0.0f;
				_lastToneShapeRatio = 0.0f;
				_lastTonePosOffset = Vector2.zero;
				_lastVertColorRatio = 0.0f;
			}

			private bool IsColorDifferent(Color colorA, Color colorB)
			{
				return Mathf.Abs(colorA.r - colorB.r) > 0.002f
					|| Mathf.Abs(colorA.g - colorB.g) > 0.002f
					|| Mathf.Abs(colorA.b - colorB.b) > 0.002f
					|| Mathf.Abs(colorA.a - colorB.a) > 0.002f;
					
			}

			public void BeginPass_Color(int GLMode)
			{	
				if(_isRenderingBegun)
				{
					if(_matType != MatType.Color 
						|| _lastGLMode != GLMode)
					{
						//End 후 Pass 시작
						GL.End();
						GL.Flush();
					}
					else
					{
						//Pass 유지
						return;
					}
				}

				//Pass 시작
				_matType = MatType.Color;

				_mat_Color.color = new Color(1, 1, 1, 1);

				_mat_Color.SetPass(0);

				//GL.Begin 및 정보 저장
				GL.Begin(GLMode);

				_isRenderingBegun = true;
				_lastGLMode = GLMode;
			}

			public void BeginPass_GUITexture(int GLMode, Texture2D texture)
			{
				if(_isRenderingBegun)
				{
					if(_matType != MatType.GUITexture 
						|| _lastGLMode != GLMode
						|| _texture != texture)
					{
						//End 후 Pass 시작
						GL.End();
						GL.Flush();
					}
					else
					{
						//Pass 유지
						return;
					}
				}

				_matType = MatType.GUITexture;

				_texture = texture;
				_mat_GUITexture.SetTexture(_propertyID__MainTex, _texture);//_MainTex

				_mat_GUITexture.SetPass(0);				

				//GL.Begin 및 정보 저장
				GL.Begin(GLMode);

				_isRenderingBegun = true;
				_lastGLMode = GLMode;
			}

			public void BeginPass_Texture_Normal(int GLMode, Color color, Texture2D texture, apPortrait.SHADER_TYPE shaderType)
			{
				if(_isRenderingBegun)
				{
					if(_matType != MatType.Texture_Normal 
						|| _shaderType_Main != shaderType
						|| _lastGLMode != GLMode
						|| _texture != texture
						|| IsColorDifferent(_color, color)
						)
					{
						//End 후 Pass 시작
						GL.End();
						GL.Flush();
					}
					else
					{
						//Pass 유지
						return;
					}
				}

				_matType = MatType.Texture_Normal;

				_shaderType_Main = shaderType;
				_iShaderType_Main = (int)_shaderType_Main;
				_color = color;
				_texture = texture;

				Material mat = _mat_Texture_Normal[_iShaderType_Main];
				mat.SetColor(_propertyID__Color, _color);//_Color				
				mat.SetTexture(_propertyID__MainTex, _texture);//_MainTex

				mat.SetPass(0);

				//GL.Begin 및 정보 저장
				GL.Begin(GLMode);

				_isRenderingBegun = true;
				_lastGLMode = GLMode;
			}

			public void BeginPass_ToneColor_Normal(int GLMode, Color color, Texture2D texture)
			{
				if(_isRenderingBegun)
				{
					if(_matType != MatType.ToneColor_Normal 
						|| _lastGLMode != GLMode
						|| _texture != texture
						|| IsColorDifferent(_color, color)
						|| Mathf.Abs(_lastToneLineThickness - _toneLineThickness) > PASS_EQUAL_BIAS
						|| Mathf.Abs(_lastToneShapeRatio - _toneShapeRatio) > PASS_EQUAL_BIAS
						|| Mathf.Abs(_lastTonePosOffset.x - (_tonePosOffset.x * _zoom)) > PASS_EQUAL_BIAS
						|| Mathf.Abs(_lastTonePosOffset.y - (_tonePosOffset.y * _zoom)) > PASS_EQUAL_BIAS
						)
					{
						//End 후 Pass 시작
						GL.End();
						GL.Flush();
					}
					else
					{
						//Pass 유지
						return;
					}
				}

				_matType = MatType.ToneColor_Normal;
				
				_color = color;
				_texture = texture;

				_mat_ToneColor_Normal.SetColor(_propertyID__Color, _color);//_Color
				_mat_ToneColor_Normal.SetFloat(_propertyID__Thickness, _toneLineThickness);//_Thickness
				_mat_ToneColor_Normal.SetFloat(_propertyID__ShapeRatio, _toneShapeRatio);//_ShapeRatio
				_mat_ToneColor_Normal.SetFloat(_propertyID__PosOffsetX, _tonePosOffset.x * _zoom);//_PosOffsetX
				_mat_ToneColor_Normal.SetFloat(_propertyID__PosOffsetY, _tonePosOffset.y * _zoom);//_PosOffsetY
								
				_mat_ToneColor_Normal.SetTexture(_propertyID__MainTex, _texture);//_MainTex

				_mat_ToneColor_Normal.SetPass(0);
				
				//GL.Begin 및 정보 저장
				GL.Begin(GLMode);

				_isRenderingBegun = true;
				_lastGLMode = GLMode;

				_lastToneLineThickness = _toneLineThickness;
				_lastToneShapeRatio = _toneShapeRatio;
				_lastTonePosOffset.x = _tonePosOffset.x * _zoom;
				_lastTonePosOffset.y = _tonePosOffset.y * _zoom;
			}

			public void BeginPass_ToneColor_Custom(int GLMode, Color color, Texture2D texture, float thickness, float shapeRatio)
			{
				if(_isRenderingBegun)
				{
					if(_matType != MatType.ToneColor_Normal 
						|| _lastGLMode != GLMode
						|| _texture != texture
						|| IsColorDifferent(_color, color)
						|| Mathf.Abs(_lastToneLineThickness - thickness) > PASS_EQUAL_BIAS
						|| Mathf.Abs(_lastToneShapeRatio - shapeRatio) > PASS_EQUAL_BIAS
						|| Mathf.Abs(_lastTonePosOffset.x - 0.0f) > PASS_EQUAL_BIAS
						|| Mathf.Abs(_lastTonePosOffset.y - 0.0f) > PASS_EQUAL_BIAS
						)
					{
						//End 후 Pass 시작
						GL.End();
						GL.Flush();
					}
					else
					{
						//Pass 유지
						return;
					}
				}

				_matType = MatType.ToneColor_Normal;

				_color = color;
				_texture = texture;

				_mat_ToneColor_Normal.SetColor(_propertyID__Color, _color);//_Color
				_mat_ToneColor_Normal.SetFloat(_propertyID__Thickness, thickness);//_Thickness
				_mat_ToneColor_Normal.SetFloat(_propertyID__ShapeRatio, shapeRatio);//_ShapeRatio
				_mat_ToneColor_Normal.SetFloat(_propertyID__PosOffsetX, 0.0f);//_PosOffsetX
				_mat_ToneColor_Normal.SetFloat(_propertyID__PosOffsetY, 0.0f);//_PosOffsetY
				_mat_ToneColor_Normal.SetTexture(_propertyID__MainTex, _texture);//_MainTex

				_mat_ToneColor_Normal.SetPass(0);


				//GL.Begin 및 정보 저장
				GL.Begin(GLMode);

				_isRenderingBegun = true;
				_lastGLMode = GLMode;

				_lastToneLineThickness = thickness;
				_lastToneShapeRatio = shapeRatio;
				_lastTonePosOffset.x = 0.0f;
				_lastTonePosOffset.y = 0.0f;
			}

			public void BeginPass_Texture_VColor(	int GLMode, Color color, Texture2D texture, 
													float vertColorRatio, 
													apPortrait.SHADER_TYPE shaderType, 
													bool isSetScreenSize, Vector4 screenSize)
			{
				if(_isRenderingBegun)
				{
					if(_matType != MatType.Texture_VColorAdd 
						|| _shaderType_Main != shaderType
						|| _lastGLMode != GLMode
						|| _texture != texture
						|| IsColorDifferent(_color, color)
						|| Mathf.Abs(_lastVertColorRatio - vertColorRatio) > PASS_EQUAL_BIAS
						)
					{
						//End 후 Pass 시작
						GL.End();
						GL.Flush();
					}
					else
					{
						//Pass 유지
						return;
					}
				}

				_matType = MatType.Texture_VColorAdd;

				_shaderType_Main = shaderType;
				_iShaderType_Main = (int)_shaderType_Main;

				_color = color;
				_texture = texture;

				Material mat = _mat_Texture_VColorAdd[_iShaderType_Main];
				mat.SetColor(_propertyID__Color, _color);//_Color
				mat.SetTexture(_propertyID__MainTex, _texture);//_MainTex
				mat.SetFloat(_propertyID__vColorITP, vertColorRatio);//_vColorITP
				
				if(isSetScreenSize)
				{
					SetClippingSize(screenSize);
				}

				mat.SetPass(0);
				

				//GL.Begin 및 정보 저장
				GL.Begin(GLMode);

				_isRenderingBegun = true;
				_lastGLMode = GLMode;

				_lastVertColorRatio = vertColorRatio;
			}

			

			public void BeginPass_Mask(int GLMode, Color color, Texture2D texture,
									float vertColorRatio, apPortrait.SHADER_TYPE shaderType, 
									
									//Render Mask를 렌더링하는 경우
									bool isRenderMask,
									bool isRenderMaskByAlphaMask,//ShaderType에 무관하게 AlphaMask로 렌더링하는 경우

									//임의의 ScreenSize를 지정한느 경우 (캡쳐)
									bool isSetScreenSize,
									Vector4 screenSize

									)
			{
				//Mask는 무조건 Pass를 시작해야한다.
				//조건 체크후 return하는 구문이 없다.
				if(_isRenderingBegun)
				{
					//End 후 Pass 시작
					GL.End();
					GL.Flush();
				}


				_shaderType_Main = shaderType;
				_iShaderType_Main = (int)_shaderType_Main;

				_color = color;
				_texture = texture;

				if (isRenderMask)
				{
					//RenderTexture로 만든다.
					if(isRenderMaskByAlphaMask)
					{
						//AlphaMask 방식으로 렌더링하는 경우
						_matType = MatType.MaskOnly;
					}
					else
					{
						//일반 재질로 렌더링을 하는 경우
						_matType = MatType.Texture_Normal;
					}


					//RenderTexture를 활성화한다.
					_renderTexture = RenderTexture.GetTemporary(_renderTextureSize_Width, _renderTextureSize_Height, 8);
					_renderTexture.wrapMode = TextureWrapMode.Clamp;

					//RenderTexture를 사용
					RenderTexture.active = _renderTexture;

					//[중요] Temp RenderTexture는 색상 초기화가 안되어있다. 꼭 해준다.
					GL.Clear(true, true, Color.clear, 1.0f);


					_mat_MaskOnly.SetColor(_propertyID__Color, _color);//_Color
					_mat_MaskOnly.SetTexture(_propertyID__MainTex, _texture);//_MainTex
					_mat_MaskOnly.SetFloat(_propertyID__vColorITP, vertColorRatio);//_vColorITP
					_mat_MaskOnly.SetFloat(_propertyID__PosOffsetX, 0);//_PosOffsetX
					_mat_MaskOnly.SetFloat(_propertyID__PosOffsetY, 0);//_PosOffsetY

					if(isSetScreenSize)
					{
						SetClippingSize(screenSize);
					}

					_mat_MaskOnly.SetPass(0);

					//GL.Begin 및 정보 저장
					GL.Begin(GLMode);

					_isRenderingBegun = true;
					_lastGLMode = GLMode;

					_lastVertColorRatio = vertColorRatio;
					_lastTonePosOffset.x = 0.0f;
					_lastTonePosOffset.y = 0.0f;


				}
				else
				{
					_matType = MatType.Texture_VColorAdd;

					Material mat = _mat_Texture_VColorAdd[_iShaderType_Main];
					mat.SetColor(_propertyID__Color, _color);//_Color
					mat.SetTexture(_propertyID__MainTex, _texture);//_MainTex
					mat.SetFloat(_propertyID__vColorITP, vertColorRatio);//_vColorITP

					if(isSetScreenSize)
					{
						SetClippingSize(screenSize);
					}

					mat.SetPass(0);


					//GL.Begin 및 정보 저장
					GL.Begin(GLMode);

					_isRenderingBegun = true;
					_lastGLMode = GLMode;

					_lastVertColorRatio = vertColorRatio;
				}
			}

			/// <summary>
			/// BeginPass_Mask와 유사하지만, 미리 외부에서 생성한 RenderTexture를 이용한다.
			/// </summary>
			public void BeginPass_MaskWithRT(	int GLMode, Color color, Texture2D texture,
												apPortrait.SHADER_TYPE shaderType, 
									
												bool isRenderMaskByAlphaMask,//ShaderType에 무관하게 AlphaMask로 렌더링하는 경우

												//임의의 ScreenSize를 지정한느 경우 (캡쳐)
												bool isSetScreenSize,
												Vector4 screenSize,

												RenderTexture targetRenderTexture,
												bool isNeedToClearRT,
												
												//클리핑-체인된 경우
												bool isChainClipped,
												RenderTexture clippingMask,
												RenderTexture receiveMaskRT_1, float maskOpFloat_Ch1,
												RenderTexture receiveMaskRT_2, float maskOpFloat_Ch2,
												RenderTexture receiveMaskRT_3, float maskOpFloat_Ch3,
												RenderTexture receiveMaskRT_4, float maskOpFloat_Ch4,
												RenderTexture receiveSeeThroughRT, float receiveSeeThroughAlpha
												)
			{
				//Mask는 무조건 Pass를 시작해야한다.
				//조건 체크후 return하는 구문이 없다.
				if(_isRenderingBegun)
				{
					//End 후 Pass 시작
					GL.End();
					GL.Flush();
				}

				_shaderType_Main = shaderType;
				_iShaderType_Main = (int)_shaderType_Main;

				_color = color;
				_texture = texture;

				Material mat = null;

				//RenderTexture로 만든다.
				if(isRenderMaskByAlphaMask)
				{
					//AlphaMask 방식으로 렌더링하는 경우
					_matType = MatType.MaskOnly;
					mat = _mat_MaskOnly;
				}
				else
				{
					//일반 재질로 렌더링을 하는 경우
					if(isChainClipped)
					{
						//클리핑-체인된 경우
						_matType = MatType.Clipped;
						mat = _mat_Clipped[_iShaderType_Main];
					}
					else
					{
						//일반 렌더링
						_matType = MatType.Texture_Normal;
						mat = _mat_Texture_VColorAdd[_iShaderType_Main];
					}
						
				}

				//RenderTexture를 사용
				RenderTexture.active = targetRenderTexture;

				if(isNeedToClearRT)
				{
					//[중요] Temp RenderTexture는 색상 초기화가 안되어있다. 꼭 해준다.
					//단, 요청이 있는 경우
					GL.Clear(true, true, Color.clear, 1.0f);
				}

				mat.SetColor(_propertyID__Color, _color);//_Color
				mat.SetTexture(_propertyID__MainTex, _texture);//_MainTex
				mat.SetFloat(_propertyID__vColorITP, 0.0f);//_vColorITP
				mat.SetFloat(_propertyID__PosOffsetX, 0);//_PosOffsetX
				mat.SetFloat(_propertyID__PosOffsetY, 0);//_PosOffsetY

				if(isSetScreenSize)
				{
					SetClippingSize(screenSize);
				}


				//체인된 경우
				//1. 클리핑 마스크
				if(isChainClipped && clippingMask != null)
				{
					//클리핑 마스크가 있다면
					mat.SetTexture(_propertyID__MaskRenderTexture, clippingMask);
					mat.SetFloat(_propertyID__MaskRatio, 1.0f);//클리핑 마스크 적용함 (1)
				}
				else
				{
					//클리핑 마스크가 없다면
					mat.SetTexture(_propertyID__MaskRenderTexture, null);
					mat.SetFloat(_propertyID__MaskRatio, 0.0f);//클리핑 마스크 적용안함 (0)
				}

				//v1.6.0 : 채널별 임의 마스크
				//채널 1
				if(isChainClipped && receiveMaskRT_1 != null)
				{
					//마스크 채널 1 사용함
					mat.SetFloat(_propertyID__MaskRatio_1, 1.0f);//사용함 (1)
					mat.SetTexture(_propertyID__MaskTex_1, receiveMaskRT_1);
					mat.SetFloat(_propertyID__MaskOp_1, maskOpFloat_Ch1);
				}
				else
				{
					//마스크 채널 1 사용 안함
					mat.SetFloat(_propertyID__MaskRatio_1, 0.0f);//사용 안함 (0)
					mat.SetTexture(_propertyID__MaskTex_1, null);
					mat.SetFloat(_propertyID__MaskOp_1, 0.0f);
				}
					
				//채널 2
				if(isChainClipped && receiveMaskRT_2 != null)
				{
					//마스크 채널 2 사용함
					mat.SetFloat(_propertyID__MaskRatio_2, 1.0f);//사용함 (1)
					mat.SetTexture(_propertyID__MaskTex_2, receiveMaskRT_2);
					mat.SetFloat(_propertyID__MaskOp_2, maskOpFloat_Ch2);
				}
				else
				{
					//마스크 채널 2 사용 안함
					mat.SetFloat(_propertyID__MaskRatio_2, 0.0f);//사용 안함 (0)
					mat.SetTexture(_propertyID__MaskTex_2, null);
					mat.SetFloat(_propertyID__MaskOp_2, 0.0f);
				}

				//채널 3
				if(isChainClipped && receiveMaskRT_3 != null)
				{
					//마스크 채널 3 사용함
					mat.SetFloat(_propertyID__MaskRatio_3, 1.0f);//사용함 (1)
					mat.SetTexture(_propertyID__MaskTex_3, receiveMaskRT_3);
					mat.SetFloat(_propertyID__MaskOp_3, maskOpFloat_Ch3);
				}
				else
				{
					//마스크 채널 3 사용 안함
					mat.SetFloat(_propertyID__MaskRatio_3, 0.0f);//사용 안함 (0)
					mat.SetTexture(_propertyID__MaskTex_3, null);
					mat.SetFloat(_propertyID__MaskOp_3, 0.0f);
				}

				//채널 4
				if(isChainClipped && receiveMaskRT_4 != null)
				{
					//마스크 채널 4 사용함
					mat.SetFloat(_propertyID__MaskRatio_4, 1.0f);//사용함 (1)
					mat.SetTexture(_propertyID__MaskTex_4, receiveMaskRT_4);
					mat.SetFloat(_propertyID__MaskOp_4, maskOpFloat_Ch4);
				}
				else
				{
					//마스크 채널 4 사용 안함
					mat.SetFloat(_propertyID__MaskRatio_4, 0.0f);//사용 안함 (0)
					mat.SetTexture(_propertyID__MaskTex_4, null);
					mat.SetFloat(_propertyID__MaskOp_4, 0.0f);
				}


				if(!isRenderMaskByAlphaMask)
				{
					//텍스쳐 투과
					if(isChainClipped && receiveSeeThroughRT != null)
					{
						//텍스쳐 투과 사용함
						mat.SetFloat(_propertyID__SeeThroughRatio, 1.0f);
						mat.SetTexture(_propertyID__SeeThroughTex, receiveSeeThroughRT);
						mat.SetFloat(_propertyID__SeeThroughAlpha, receiveSeeThroughAlpha);
					}
					else
					{
						//텍스쳐 투과 사용 안함
						mat.SetFloat(_propertyID__SeeThroughRatio, 0.0f);
						mat.SetTexture(_propertyID__SeeThroughTex, null);
						mat.SetFloat(_propertyID__SeeThroughAlpha, 0.0f);
					}
				}
				


				mat.SetPass(0);

				//GL.Begin 및 정보 저장
				GL.Begin(GLMode);

				_isRenderingBegun = true;
				_lastGLMode = GLMode;

				_lastVertColorRatio = 0.0f;
				_lastTonePosOffset.x = 0.0f;
				_lastTonePosOffset.y = 0.0f;
			}


			public void BeginPass_Mask_Gray(int GLMode, Color color, Texture2D texture, bool isRenderMask)
			{
				//Mask는 무조건 Pass를 시작해야한다.
				//조건 체크후 return하는 구문이 없다.
				if(_isRenderingBegun)
				{
					//End 후 Pass 시작
					GL.End();
					GL.Flush();
				}


				_color = color;
				_texture = texture;

				if (isRenderMask)
				{
					//RenderTexture로 만든다.
					_matType = MatType.MaskOnly;

					//RenderTexture를 활성화한다.
					_renderTexture = RenderTexture.GetTemporary(_renderTextureSize_Width, _renderTextureSize_Height, 8);
					_renderTexture.wrapMode = TextureWrapMode.Clamp;

					//RenderTexture를 사용
					RenderTexture.active = _renderTexture;

					//[중요] Temp RenderTexture는 색상 초기화가 안되어있다. 꼭 해준다.
					GL.Clear(true, true, Color.clear, 1.0f);


					_mat_MaskOnly.SetColor(_propertyID__Color, _color);//_Color
					_mat_MaskOnly.SetTexture(_propertyID__MainTex, _texture);//_MainTex
					//_mat_MaskOnly.SetFloat(_propertyID__vColorITP, vertColorRatio);//_vColorITP
					_mat_MaskOnly.SetFloat(_propertyID__PosOffsetX, 0);//_PosOffsetX
					_mat_MaskOnly.SetFloat(_propertyID__PosOffsetY, 0);//_PosOffsetY
					_mat_MaskOnly.SetPass(0);

					//GL.Begin 및 정보 저장
					GL.Begin(GLMode);

					_isRenderingBegun = true;
					_lastGLMode = GLMode;

					_lastTonePosOffset.x = 0.0f;
					_lastTonePosOffset.y = 0.0f;
				}
				else
				{
					_matType = MatType.Gray_Normal;
					
					_mat_Gray_Normal.SetColor(_propertyID__Color, _color);//_Color
					_mat_Gray_Normal.SetTexture(_propertyID__MainTex, _texture);//_MainTex
					_mat_Gray_Normal.SetPass(0);

					//GL.Begin 및 정보 저장
					GL.Begin(GLMode);

					_isRenderingBegun = true;
					_lastGLMode = GLMode;
				}
			}



			/// <summary>
			/// v1.6.0 추가 : 별도의 마스크 (apMaskRT)를 대상으로 렌더링을 한다.
			/// 여기서 사용되는 쉐이더는 AlphaMask 또는 일반 Color Shader이다.
			/// </summary>
			public void BeginPass_MaskRT(	int GLMode,
											Color color,
											Texture2D texture,
											apPortrait.SHADER_TYPE shaderType,//Render Unit의 Shader Type
											bool isAlphaMaskShader,
											apMaskRT maskRT,
											
											//클리핑-체인된 경우
											bool isChainClipped,											
											apMaskRT clippingParentMaskRT,
											apMaskRT maskRT_Ch1, apSendMaskData.ReceivePropertySet receivedPropSet_Ch1,
											apMaskRT maskRT_Ch2, apSendMaskData.ReceivePropertySet receivedPropSet_Ch2,
											apMaskRT maskRT_Ch3, apSendMaskData.ReceivePropertySet receivedPropSet_Ch3,
											apMaskRT maskRT_Ch4, apSendMaskData.ReceivePropertySet receivedPropSet_Ch4,
											apMaskRT seeThroughRT, apSendMaskData.ReceivePropertySet receivedPropSet_SeeThrough
											)
			{
				//Mask는 무조건 Pass를 시작해야한다.
				//조건 체크후 return하는 구문이 없다.
				if(_isRenderingBegun)
				{
					//End 후 Pass 시작
					GL.End();
					GL.Flush();
				}

				//_shaderType_Main = apPortrait.SHADER_TYPE.AlphaBlend;
				_shaderType_Main = shaderType;
				_iShaderType_Main = (int)_shaderType_Main;

				_color = color;
				_texture = texture;

				//RenderTexture로 만든다.
				if(isAlphaMaskShader)
				{
					_matType = MatType.MaskOnly;
				}
				else
				{
					//일반 쉐이더로 마스크를 렌더링할 때
					if(isChainClipped)
					{
						//클리핑-체인이 된 경우
						_matType = MatType.Clipped;
					}
					else
					{
						//일반 렌더링인 경우
						_matType = MatType.Texture_VColorAdd;
					}
					
				}
				
				//MaskRT 사용
				RenderTexture.active = maskRT.GetRenderTexture();

				if(!maskRT.IsBufferCleared)
				{
					//아직 버퍼 초기화가 되지 않았다면
					
					//무조건 Clear로 초기화
					//Debug.Log("RT 클리어 : " + maskRT.GetRenderTexture().GetInstanceID());

					GL.Clear(true, true, Color.clear, 1.0f);

					//버퍼 초기화 완료
					maskRT.SetBufferCleared();
				}

				Material mat = null;
				if(isAlphaMaskShader)
				{
					mat = _mat_MaskOnly;
				}
				else
				{
					if(isChainClipped)
					{
						//클리핑-체인이 된 경우
						mat = _mat_Clipped[_iShaderType_Main];
					}
					else
					{
						mat = _mat_Texture_VColorAdd[_iShaderType_Main];
					}
				}
				

				mat.SetColor(_propertyID__Color, _color);//_Color
				mat.SetTexture(_propertyID__MainTex, _texture);//_MainTex
				mat.SetFloat(_propertyID__vColorITP, 0.0f);//_vColorITP

				mat.SetFloat(_propertyID__PosOffsetX, 0);//_PosOffsetX
				mat.SetFloat(_propertyID__PosOffsetY, 0);//_PosOffsetY

				//체인된 경우
				
				//1. 클리핑 마스크
				if(isChainClipped && clippingParentMaskRT != null)
				{
					//클리핑 마스크가 있다면
					mat.SetTexture(_propertyID__MaskRenderTexture, clippingParentMaskRT.GetRenderTexture());
					mat.SetFloat(_propertyID__MaskRatio, 1.0f);//클리핑 마스크 적용함 (1)
				}
				else
				{
					//클리핑 마스크가 없다면
					mat.SetTexture(_propertyID__MaskRenderTexture, null);
					mat.SetFloat(_propertyID__MaskRatio, 0.0f);//클리핑 마스크 적용안함 (0)
				}

				//v1.6.0 : 채널별 임의 마스크
				//채널 1
				if(isChainClipped && maskRT_Ch1 != null && receivedPropSet_Ch1 != null)
				{
					//마스크 채널 1 사용함
					mat.SetFloat(_propertyID__MaskRatio_1, 1.0f);//사용함 (1)
					mat.SetTexture(_propertyID__MaskTex_1, maskRT_Ch1.GetRenderTexture());
					mat.SetFloat(_propertyID__MaskOp_1, receivedPropSet_Ch1.GetMaskOpFloatValue());
				}
				else
				{
					//마스크 채널 1 사용 안함
					mat.SetFloat(_propertyID__MaskRatio_1, 0.0f);//사용 안함 (0)
					mat.SetTexture(_propertyID__MaskTex_1, null);
					mat.SetFloat(_propertyID__MaskOp_1, 0.0f);
				}
					
				//채널 2
				if(isChainClipped && maskRT_Ch2 != null && receivedPropSet_Ch2 != null)
				{
					//마스크 채널 2 사용함
					mat.SetFloat(_propertyID__MaskRatio_2, 1.0f);//사용함 (1)
					mat.SetTexture(_propertyID__MaskTex_2, maskRT_Ch2.GetRenderTexture());
					mat.SetFloat(_propertyID__MaskOp_2, receivedPropSet_Ch2.GetMaskOpFloatValue());
				}
				else
				{
					//마스크 채널 2 사용 안함
					mat.SetFloat(_propertyID__MaskRatio_2, 0.0f);//사용 안함 (0)
					mat.SetTexture(_propertyID__MaskTex_2, null);
					mat.SetFloat(_propertyID__MaskOp_2, 0.0f);
				}

				//채널 3
				if(isChainClipped && maskRT_Ch3 != null && receivedPropSet_Ch3 != null)
				{
					//마스크 채널 3 사용함
					mat.SetFloat(_propertyID__MaskRatio_3, 1.0f);//사용함 (1)
					mat.SetTexture(_propertyID__MaskTex_3, maskRT_Ch3.GetRenderTexture());
					mat.SetFloat(_propertyID__MaskOp_3, receivedPropSet_Ch3.GetMaskOpFloatValue());
				}
				else
				{
					//마스크 채널 3 사용 안함
					mat.SetFloat(_propertyID__MaskRatio_3, 0.0f);//사용 안함 (0)
					mat.SetTexture(_propertyID__MaskTex_3, null);
					mat.SetFloat(_propertyID__MaskOp_3, 0.0f);
				}

				//채널 4
				if(isChainClipped && maskRT_Ch4 != null && receivedPropSet_Ch4 != null)
				{
					//마스크 채널 4 사용함
					mat.SetFloat(_propertyID__MaskRatio_4, 1.0f);//사용함 (1)
					mat.SetTexture(_propertyID__MaskTex_4, maskRT_Ch4.GetRenderTexture());
					mat.SetFloat(_propertyID__MaskOp_4, receivedPropSet_Ch4.GetMaskOpFloatValue());
				}
				else
				{
					//마스크 채널 4 사용 안함
					mat.SetFloat(_propertyID__MaskRatio_4, 0.0f);//사용 안함 (0)
					mat.SetTexture(_propertyID__MaskTex_4, null);
					mat.SetFloat(_propertyID__MaskOp_4, 0.0f);
				}

				if(!isAlphaMaskShader)
				{
					//텍스쳐 투과 (이건 Alpha Mask에 없는 프로퍼티다)
					if(isChainClipped && seeThroughRT != null && receivedPropSet_SeeThrough != null)
					{
						//텍스쳐 투과 사용함
						mat.SetFloat(_propertyID__SeeThroughRatio, 1.0f);
						mat.SetTexture(_propertyID__SeeThroughTex, seeThroughRT.GetRenderTexture());
						mat.SetFloat(_propertyID__SeeThroughAlpha, receivedPropSet_SeeThrough.GetValue_Float());
					}
					else
					{
						//텍스쳐 투과 사용 안함
						mat.SetFloat(_propertyID__SeeThroughRatio, 0.0f);
						mat.SetTexture(_propertyID__SeeThroughTex, null);
						mat.SetFloat(_propertyID__SeeThroughAlpha, 0.0f);
					}
				}

				mat.SetPass(0);

				//GL.Begin 및 정보 저장
				GL.Begin(GLMode);

				_isRenderingBegun = true;
				_lastGLMode = GLMode;

				_lastVertColorRatio = 0.0f;
				_lastTonePosOffset.x = 0.0f;
				_lastTonePosOffset.y = 0.0f;
			}





			public void BeginPass_Clipped(	int GLMode,
											Color color,
											Texture2D texture,
											float vertColorRatio,
											apPortrait.SHADER_TYPE shaderType/*, Color parentColor*/
				)
			{	
				if(_isRenderingBegun)
				{
					//RenderTexture를 이용하는 경우엔 Pass를 유지하지 않는다.
					//End 후 Pass 시작
					GL.End();
					GL.Flush();
				}


				_matType = MatType.Clipped;

				_shaderType_Main = shaderType;
				_iShaderType_Main = (int)_shaderType_Main;

				_color = color;
				_texture = texture;
				Material mat = _mat_Clipped[_iShaderType_Main];
				mat.SetColor(_propertyID__Color, _color);//_Color
				mat.SetTexture(_propertyID__MainTex, _texture);//_MainTex
				mat.SetFloat(_propertyID__vColorITP, vertColorRatio);//_vColorITP

				//Mask를 넣자
				mat.SetTexture(_propertyID__MaskRenderTexture, _renderTexture);//_MaskRenderTexture : 저장된 임시 RT에서 가져오기
				//mat.SetColor(_propertyID__MaskColor, parentColor);//_MaskColor tkrwp ㅍ1.6.0
				mat.SetFloat(_propertyID__MaskRatio, 1.0f);//v1.6.0 : 클리핑 마스크 비율 (이 함수에서는 무조건 1)

				//v1.6.0 : 채널별 임의 마스크 (이 함수에서는 유효한 값을 입력하지 않는다)
				mat.SetFloat(_propertyID__MaskRatio_1, 0.0f);
				mat.SetTexture(_propertyID__MaskTex_1, null);
				mat.SetFloat(_propertyID__MaskOp_1, 0.0f);

				mat.SetFloat(_propertyID__MaskRatio_2, 0.0f);
				mat.SetTexture(_propertyID__MaskTex_2, null);
				mat.SetFloat(_propertyID__MaskOp_2, 0.0f);

				mat.SetFloat(_propertyID__MaskRatio_3, 0.0f);
				mat.SetTexture(_propertyID__MaskTex_3, null);
				mat.SetFloat(_propertyID__MaskOp_3, 0.0f);

				mat.SetFloat(_propertyID__MaskRatio_4, 0.0f);
				mat.SetTexture(_propertyID__MaskTex_4, null);
				mat.SetFloat(_propertyID__MaskOp_4, 0.0f);


				mat.SetPass(0);


				//GL.Begin 및 정보 저장
				GL.Begin(GLMode);

				_isRenderingBegun = true;
				_lastGLMode = GLMode;

				//_lastParentColor = parentColor;
				_lastVertColorRatio = vertColorRatio;
				//_lastRenderTexture = _renderTexture;
			}


			public void BeginPass_Clipped_WithMaskInfo(	int GLMode,
											Color color,
											Texture2D texture,
											float vertColorRatio,
											apPortrait.SHADER_TYPE shaderType,

											//클리핑
											apMaskRT clippingParentMaskRT,
											//Color clippingParentColor,
											
											//채널별 마스크
											apMaskRT maskRT_Ch1, apSendMaskData.ReceivePropertySet receivedPropSet_Ch1,
											apMaskRT maskRT_Ch2, apSendMaskData.ReceivePropertySet receivedPropSet_Ch2,
											apMaskRT maskRT_Ch3, apSendMaskData.ReceivePropertySet receivedPropSet_Ch3,
											apMaskRT maskRT_Ch4, apSendMaskData.ReceivePropertySet receivedPropSet_Ch4,
											apMaskRT seeThroughRT, apSendMaskData.ReceivePropertySet receivedPropSet_SeeThrough
											)
			{
				BeginPass_Clipped_WithMaskInfo(GLMode,
											color,
											texture,
											vertColorRatio,
											shaderType,

											false, Vector4.zero,

											//클리핑
											clippingParentMaskRT,
											
											//채널별 마스크
											maskRT_Ch1, receivedPropSet_Ch1,
											maskRT_Ch2, receivedPropSet_Ch2,
											maskRT_Ch3, receivedPropSet_Ch3,
											maskRT_Ch4, receivedPropSet_Ch4,
											seeThroughRT, receivedPropSet_SeeThrough);
			}

			/// <summary>
			/// v1.6.0 추가 : 별도로 작성된 MaskRT를 이용해서 클리핑 렌더링을 한다.
			/// </summary>
			public void BeginPass_Clipped_WithMaskInfo(	int GLMode,
											Color color,
											Texture2D texture,
											float vertColorRatio,
											apPortrait.SHADER_TYPE shaderType,

											bool isSetScreenSize, Vector4 screenSize,

											//클리핑
											apMaskRT clippingParentMaskRT,
											//Color clippingParentColor,
											
											//채널별 마스크
											apMaskRT maskRT_Ch1, apSendMaskData.ReceivePropertySet receivedPropSet_Ch1,
											apMaskRT maskRT_Ch2, apSendMaskData.ReceivePropertySet receivedPropSet_Ch2,
											apMaskRT maskRT_Ch3, apSendMaskData.ReceivePropertySet receivedPropSet_Ch3,
											apMaskRT maskRT_Ch4, apSendMaskData.ReceivePropertySet receivedPropSet_Ch4,
											apMaskRT seeThroughRT, apSendMaskData.ReceivePropertySet receivedPropSet_SeeThrough
											)
			{	
				if(_isRenderingBegun)
				{
					//RenderTexture를 이용하는 경우엔 Pass를 유지하지 않는다.
					//End 후 Pass 시작
					GL.End();
					GL.Flush();
				}


				_matType = MatType.Clipped;

				_shaderType_Main = shaderType;
				_iShaderType_Main = (int)_shaderType_Main;

				_color = color;
				_texture = texture;

				Material mat = _mat_Clipped[_iShaderType_Main];

				mat.SetColor(_propertyID__Color, _color);//_Color
				mat.SetTexture(_propertyID__MainTex, _texture);//_MainTex
				mat.SetFloat(_propertyID__vColorITP, vertColorRatio);//_vColorITP

				if(isSetScreenSize)
				{
					SetClippingSize(screenSize);
				}

				//1. 클리핑 마스크
				if(clippingParentMaskRT != null)
				{
					//클리핑 마스크가 있다면
					mat.SetTexture(_propertyID__MaskRenderTexture, clippingParentMaskRT.GetRenderTexture());
					//mat.SetColor(_propertyID__MaskColor, clippingParentColor);//삭제 v1.6.0
					mat.SetFloat(_propertyID__MaskRatio, 1.0f);//클리핑 마스크 적용함 (1)
				}
				else
				{
					//클리핑 마스크가 없다면
					mat.SetTexture(_propertyID__MaskRenderTexture, null);
					//mat.SetColor(_propertyID__MaskColor, _textureColor_Gray); //삭제 v1.6.0
					mat.SetFloat(_propertyID__MaskRatio, 0.0f);//클리핑 마스크 적용안함 (0)
				}
				

				//v1.6.0 : 채널별 임의 마스크
				//채널 1
				if(maskRT_Ch1 != null && receivedPropSet_Ch1 != null)
				{
					//마스크 채널 1 사용함
					mat.SetFloat(_propertyID__MaskRatio_1, 1.0f);//사용함 (1)
					mat.SetTexture(_propertyID__MaskTex_1, maskRT_Ch1.GetRenderTexture());
					mat.SetFloat(_propertyID__MaskOp_1, receivedPropSet_Ch1.GetMaskOpFloatValue());
				}
				else
				{
					//마스크 채널 1 사용 안함
					mat.SetFloat(_propertyID__MaskRatio_1, 0.0f);//사용 안함 (0)
					mat.SetTexture(_propertyID__MaskTex_1, null);
					mat.SetFloat(_propertyID__MaskOp_1, 0.0f);
				}
				
				//채널 2
				if(maskRT_Ch2 != null && receivedPropSet_Ch2 != null)
				{
					//마스크 채널 2 사용함
					mat.SetFloat(_propertyID__MaskRatio_2, 1.0f);//사용함 (1)
					mat.SetTexture(_propertyID__MaskTex_2, maskRT_Ch2.GetRenderTexture());
					mat.SetFloat(_propertyID__MaskOp_2, receivedPropSet_Ch2.GetMaskOpFloatValue());
				}
				else
				{
					//마스크 채널 2 사용 안함
					mat.SetFloat(_propertyID__MaskRatio_2, 0.0f);//사용 안함 (0)
					mat.SetTexture(_propertyID__MaskTex_2, null);
					mat.SetFloat(_propertyID__MaskOp_2, 0.0f);
				}

				//채널 3
				if(maskRT_Ch3 != null && receivedPropSet_Ch3 != null)
				{
					//마스크 채널 3 사용함
					mat.SetFloat(_propertyID__MaskRatio_3, 1.0f);//사용함 (1)
					mat.SetTexture(_propertyID__MaskTex_3, maskRT_Ch3.GetRenderTexture());
					mat.SetFloat(_propertyID__MaskOp_3, receivedPropSet_Ch3.GetMaskOpFloatValue());
				}
				else
				{
					//마스크 채널 3 사용 안함
					mat.SetFloat(_propertyID__MaskRatio_3, 0.0f);//사용 안함 (0)
					mat.SetTexture(_propertyID__MaskTex_3, null);
					mat.SetFloat(_propertyID__MaskOp_3, 0.0f);
				}

				//채널 4
				if(maskRT_Ch4 != null && receivedPropSet_Ch4 != null)
				{
					//마스크 채널 4 사용함
					mat.SetFloat(_propertyID__MaskRatio_4, 1.0f);//사용함 (1)
					mat.SetTexture(_propertyID__MaskTex_4, maskRT_Ch4.GetRenderTexture());
					mat.SetFloat(_propertyID__MaskOp_4, receivedPropSet_Ch4.GetMaskOpFloatValue());
				}
				else
				{
					//마스크 채널 4 사용 안함
					mat.SetFloat(_propertyID__MaskRatio_4, 0.0f);//사용 안함 (0)
					mat.SetTexture(_propertyID__MaskTex_4, null);
					mat.SetFloat(_propertyID__MaskOp_4, 0.0f);
				}


				//텍스쳐 투과
				if(seeThroughRT != null && receivedPropSet_SeeThrough != null)
				{
					//텍스쳐 투과 사용함
					mat.SetFloat(_propertyID__SeeThroughRatio, 1.0f);
					mat.SetTexture(_propertyID__SeeThroughTex, seeThroughRT.GetRenderTexture());
					mat.SetFloat(_propertyID__SeeThroughAlpha, receivedPropSet_SeeThrough.GetValue_Float());
				}
				else
				{
					//텍스쳐 투과 사용 안함
					mat.SetFloat(_propertyID__SeeThroughRatio, 0.0f);
					mat.SetTexture(_propertyID__SeeThroughTex, null);
					mat.SetFloat(_propertyID__SeeThroughAlpha, 0.0f);
				}

				mat.SetPass(0);


				//GL.Begin 및 정보 저장
				GL.Begin(GLMode);

				_isRenderingBegun = true;
				_lastGLMode = GLMode;

				//_lastParentColor = parentColor;
				_lastVertColorRatio = vertColorRatio;
				//_lastRenderTexture = _renderTexture;
			}


			/// <summary>
			/// v1.6.0 추가 : MsskRT대신 직접 생성된 RenderTexture를 이용하여 클리핑 렌더링을 한다. 주로 화면 캡쳐용
			/// </summary>
			public void BeginPass_Clipped_WithMaskRT(	int GLMode,
														Color color,
														Texture2D texture,
														float vertColorRatio,
														apPortrait.SHADER_TYPE shaderType,

														bool isSetScreenSize, Vector4 screenSize,

														//클리핑
														RenderTexture clippingMaskRT,
											
														//채널별 마스크
														RenderTexture maskRT_Ch1, float maskOpFloat_Ch1,
														RenderTexture maskRT_Ch2, float maskOpFloat_Ch2,
														RenderTexture maskRT_Ch3, float maskOpFloat_Ch3,
														RenderTexture maskRT_Ch4, float maskOpFloat_Ch4,

														//투과 텍스쳐
														RenderTexture seeThroughRT, float seeThroughAlpha
														)
			{	
				if(_isRenderingBegun)
				{
					//RenderTexture를 이용하는 경우엔 Pass를 유지하지 않는다.
					//End 후 Pass 시작
					GL.End();
					GL.Flush();
				}


				_matType = MatType.Clipped;

				_shaderType_Main = shaderType;
				_iShaderType_Main = (int)_shaderType_Main;

				_color = color;
				_texture = texture;

				Material mat = _mat_Clipped[_iShaderType_Main];

				mat.SetColor(_propertyID__Color, _color);//_Color
				mat.SetTexture(_propertyID__MainTex, _texture);//_MainTex
				mat.SetFloat(_propertyID__vColorITP, vertColorRatio);//_vColorITP

				if(isSetScreenSize)
				{
					SetClippingSize(screenSize);
				}

				//1. 클리핑 마스크
				if(clippingMaskRT != null)
				{
					//클리핑 마스크가 있다면
					mat.SetTexture(_propertyID__MaskRenderTexture, clippingMaskRT);
					mat.SetFloat(_propertyID__MaskRatio, 1.0f);//클리핑 마스크 적용함 (1)
				}
				else
				{
					//클리핑 마스크가 없다면
					mat.SetTexture(_propertyID__MaskRenderTexture, null);
					mat.SetFloat(_propertyID__MaskRatio, 0.0f);//클리핑 마스크 적용안함 (0)
				}
				

				//v1.6.0 : 채널별 임의 마스크
				//채널 1
				if(maskRT_Ch1 != null)
				{
					//마스크 채널 1 사용함
					mat.SetFloat(_propertyID__MaskRatio_1, 1.0f);//사용함 (1)
					mat.SetTexture(_propertyID__MaskTex_1, maskRT_Ch1);
					mat.SetFloat(_propertyID__MaskOp_1, maskOpFloat_Ch1);
				}
				else
				{
					//마스크 채널 1 사용 안함
					mat.SetFloat(_propertyID__MaskRatio_1, 0.0f);//사용 안함 (0)
					mat.SetTexture(_propertyID__MaskTex_1, null);
					mat.SetFloat(_propertyID__MaskOp_1, 0.0f);
				}
				
				//채널 2
				if(maskRT_Ch2 != null)
				{
					//마스크 채널 2 사용함
					mat.SetFloat(_propertyID__MaskRatio_2, 1.0f);//사용함 (1)
					mat.SetTexture(_propertyID__MaskTex_2, maskRT_Ch2);
					mat.SetFloat(_propertyID__MaskOp_2, maskOpFloat_Ch2);
				}
				else
				{
					//마스크 채널 2 사용 안함
					mat.SetFloat(_propertyID__MaskRatio_2, 0.0f);//사용 안함 (0)
					mat.SetTexture(_propertyID__MaskTex_2, null);
					mat.SetFloat(_propertyID__MaskOp_2, 0.0f);
				}

				//채널 3
				if(maskRT_Ch3 != null)
				{
					//마스크 채널 3 사용함
					mat.SetFloat(_propertyID__MaskRatio_3, 1.0f);//사용함 (1)
					mat.SetTexture(_propertyID__MaskTex_3, maskRT_Ch3);
					mat.SetFloat(_propertyID__MaskOp_3, maskOpFloat_Ch3);
				}
				else
				{
					//마스크 채널 3 사용 안함
					mat.SetFloat(_propertyID__MaskRatio_3, 0.0f);//사용 안함 (0)
					mat.SetTexture(_propertyID__MaskTex_3, null);
					mat.SetFloat(_propertyID__MaskOp_3, 0.0f);
				}

				//채널 4
				if(maskRT_Ch4 != null)
				{
					//마스크 채널 4 사용함
					mat.SetFloat(_propertyID__MaskRatio_4, 1.0f);//사용함 (1)
					mat.SetTexture(_propertyID__MaskTex_4, maskRT_Ch4);
					mat.SetFloat(_propertyID__MaskOp_4, maskOpFloat_Ch4);
				}
				else
				{
					//마스크 채널 4 사용 안함
					mat.SetFloat(_propertyID__MaskRatio_4, 0.0f);//사용 안함 (0)
					mat.SetTexture(_propertyID__MaskTex_4, null);
					mat.SetFloat(_propertyID__MaskOp_4, 0.0f);
				}

				//투과
				if(seeThroughRT != null)
				{
					//투과 텍스쳐 사용함
					mat.SetFloat(_propertyID__SeeThroughRatio, 1.0f);
					mat.SetTexture(_propertyID__SeeThroughTex, seeThroughRT);
					mat.SetFloat(_propertyID__SeeThroughAlpha, seeThroughAlpha);
				}
				else
				{
					//투과 텍스쳐 사용 안함
					mat.SetFloat(_propertyID__SeeThroughRatio, 0.0f);
					mat.SetTexture(_propertyID__SeeThroughTex, null);
					mat.SetFloat(_propertyID__SeeThroughAlpha, 0.0f);
				}

				mat.SetPass(0);


				//GL.Begin 및 정보 저장
				GL.Begin(GLMode);

				_isRenderingBegun = true;
				_lastGLMode = GLMode;

				//_lastParentColor = parentColor;
				_lastVertColorRatio = vertColorRatio;
				//_lastRenderTexture = _renderTexture;
			}


			public void BeginPass_Mask_ToneColor(int GLMode, Color color, Texture2D texture, bool isRenderMask)
			{
				//Mask는 무조건 Pass를 시작해야한다.
				//조건 체크후 return하는 구문이 없다.
				if(_isRenderingBegun)
				{
					//End 후 Pass 시작
					GL.End();
					GL.Flush();
				}

				_color = color;
				_texture = texture;

				if (isRenderMask)
				{
					//RenderTexture로 만든다.
					_matType = MatType.MaskOnly;

					//RenderTexture를 활성화한다.
					_renderTexture = RenderTexture.GetTemporary(_renderTextureSize_Width, _renderTextureSize_Height, 8);
					_renderTexture.wrapMode = TextureWrapMode.Clamp;

					//RenderTexture를 사용
					RenderTexture.active = _renderTexture;

					//[중요] Temp RenderTexture는 색상 초기화가 안되어있다. 꼭 해준다.
					GL.Clear(true, true, Color.clear, 1.0f);


					_mat_MaskOnly.SetColor(_propertyID__Color, _color);//_Color
					_mat_MaskOnly.SetTexture(_propertyID__MainTex, _texture);//_MainTex
					_mat_MaskOnly.SetFloat(_propertyID__vColorITP, 0.0f);//_vColorITP
					_mat_MaskOnly.SetFloat(_propertyID__PosOffsetX, _tonePosOffset.x * _zoom);//_PosOffsetX
					_mat_MaskOnly.SetFloat(_propertyID__PosOffsetY, _tonePosOffset.y * _zoom);//_PosOffsetY
					_mat_MaskOnly.SetPass(0);


					//GL.Begin 및 정보 저장
					GL.Begin(GLMode);

					_isRenderingBegun = true;
					_lastGLMode = GLMode;

					_lastVertColorRatio = 0.0f;
					_lastTonePosOffset.x = _tonePosOffset.x * _zoom;
					_lastTonePosOffset.y = _tonePosOffset.y * _zoom;
				}
				else
				{
					_matType = MatType.ToneColor_Normal;

					_mat_ToneColor_Normal.SetColor(_propertyID__Color, _color);//_Color
					_mat_ToneColor_Normal.SetTexture(_propertyID__MainTex, _texture);//_MainTex
					_mat_ToneColor_Normal.SetFloat(_propertyID__Thickness, _toneLineThickness);//_Thickness
					_mat_ToneColor_Normal.SetFloat(_propertyID__ShapeRatio, _toneShapeRatio);//_ShapeRatio
					_mat_ToneColor_Normal.SetFloat(_propertyID__PosOffsetX, _tonePosOffset.x * _zoom);//_PosOffsetX
					_mat_ToneColor_Normal.SetFloat(_propertyID__PosOffsetY, _tonePosOffset.y * _zoom);//_PosOffsetY
					_mat_ToneColor_Normal.SetPass(0);

					//GL.Begin 및 정보 저장
					GL.Begin(GLMode);

					_isRenderingBegun = true;
					_lastGLMode = GLMode;

					_lastToneLineThickness = _toneLineThickness;
					_lastToneShapeRatio = _toneShapeRatio;
					_lastTonePosOffset.x = _tonePosOffset.x * _zoom;
					_lastTonePosOffset.y = _tonePosOffset.y * _zoom;
				}
			}



			public void BeginPass_Clipped_ToneColor(int GLMode, Color color, Texture2D texture/*, Color parentColor*/)
			{
				if(_isRenderingBegun)
				{
					//RenderTexture를 이용하는 경우엔 Pass를 유지하지 않는다.
					//End 후 Pass 시작
					GL.End();
					GL.Flush();
				}

				_matType = MatType.ToneColor_Clipped;

				_color = color;
				_texture = texture;

				_mat_ToneColor_Clipped.SetColor(_propertyID__Color, _color);//_Color
				_mat_ToneColor_Clipped.SetTexture(_propertyID__MainTex, _texture);//_MainTex
				_mat_ToneColor_Clipped.SetTexture(_propertyID__MaskRenderTexture, _renderTexture);//_MaskRenderTexture
				//_mat_ToneColor_Clipped.SetColor(_propertyID__MaskColor, parentColor);//_MaskColor 삭제 v1.6.0
				_mat_ToneColor_Clipped.SetFloat(_propertyID__Thickness, _toneLineThickness);//_Thickness
				_mat_ToneColor_Clipped.SetFloat(_propertyID__ShapeRatio, _toneShapeRatio);//_ShapeRatio
				_mat_ToneColor_Clipped.SetFloat(_propertyID__PosOffsetX, _tonePosOffset.x * _zoom);//_PosOffsetX
				_mat_ToneColor_Clipped.SetFloat(_propertyID__PosOffsetY, _tonePosOffset.y * _zoom);//_PosOffsetY

				_mat_ToneColor_Clipped.SetPass(0);


				//GL.Begin 및 정보 저장
				GL.Begin(GLMode);

				_isRenderingBegun = true;
				_lastGLMode = GLMode;

				//_lastRenderTexture = _renderTexture;
				//_lastParentColor = parentColor;

				_lastToneLineThickness = _toneLineThickness;
				_lastToneShapeRatio = _toneShapeRatio;
				_lastTonePosOffset.x = _tonePosOffset.x * _zoom;
				_lastTonePosOffset.y = _tonePosOffset.y * _zoom;
			}



			/// <summary>
			/// v1.6.0 추가 : 별도로 작성된 MaskRT를 이용해서 클리핑 렌더링을 한다.
			/// </summary>
			public void BeginPass_Clipped_ToneColor_WithMaskInfo(	int GLMode,
																	Color color,
																	Texture2D texture,
																	
																	//클리핑
																	apMaskRT clippingParentMaskRT,
																	//Color clippingParentColor,//삭제
											
																	//채널별 마스크
																	apMaskRT maskRT_Ch1, apSendMaskData.ReceivePropertySet receivedPropSet_Ch1,
																	apMaskRT maskRT_Ch2, apSendMaskData.ReceivePropertySet receivedPropSet_Ch2,
																	apMaskRT maskRT_Ch3, apSendMaskData.ReceivePropertySet receivedPropSet_Ch3,
																	apMaskRT maskRT_Ch4, apSendMaskData.ReceivePropertySet receivedPropSet_Ch4,
																	apMaskRT seeThroughRT, apSendMaskData.ReceivePropertySet receivedPropSet_SeeThrough
																	)
			{
				if(_isRenderingBegun)
				{
					//RenderTexture를 이용하는 경우엔 Pass를 유지하지 않는다.
					//End 후 Pass 시작
					GL.End();
					GL.Flush();
				}

				_matType = MatType.ToneColor_Clipped;

				_color = color;
				_texture = texture;

				Material mat = _mat_ToneColor_Clipped;

				mat.SetColor(_propertyID__Color, _color);//_Color
				mat.SetTexture(_propertyID__MainTex, _texture);//_MainTex
				mat.SetFloat(_propertyID__Thickness, _toneLineThickness);//_Thickness
				mat.SetFloat(_propertyID__ShapeRatio, _toneShapeRatio);//_ShapeRatio
				mat.SetFloat(_propertyID__PosOffsetX, _tonePosOffset.x * _zoom);//_PosOffsetX
				mat.SetFloat(_propertyID__PosOffsetY, _tonePosOffset.y * _zoom);//_PosOffsetY

				//1. 클리핑 마스크
				if(clippingParentMaskRT != null)
				{
					//클리핑 마스크가 있다면
					mat.SetTexture(_propertyID__MaskRenderTexture, clippingParentMaskRT.GetRenderTexture());
					//mat.SetColor(_propertyID__MaskColor, clippingParentColor);//삭제 v1.6.0
					mat.SetFloat(_propertyID__MaskRatio, 1.0f);//클리핑 마스크 적용함 (1)
				}
				else
				{
					//클리핑 마스크가 없다면
					mat.SetTexture(_propertyID__MaskRenderTexture, null);
					//mat.SetColor(_propertyID__MaskColor, _textureColor_Gray);//삭제 v1.6.0
					mat.SetFloat(_propertyID__MaskRatio, 0.0f);//클리핑 마스크 적용안함 (0)
				}


				//v1.6.0 : 채널별 임의 마스크
				//채널 1
				if(maskRT_Ch1 != null && receivedPropSet_Ch1 != null)
				{
					//마스크 채널 1 사용함
					mat.SetFloat(_propertyID__MaskRatio_1, 1.0f);//사용함 (1)
					mat.SetTexture(_propertyID__MaskTex_1, maskRT_Ch1.GetRenderTexture());
					mat.SetFloat(_propertyID__MaskOp_1, receivedPropSet_Ch1.GetMaskOpFloatValue());
				}
				else
				{
					//마스크 채널 1 사용 안함
					mat.SetFloat(_propertyID__MaskRatio_1, 0.0f);//사용 안함 (0)
					mat.SetTexture(_propertyID__MaskTex_1, null);
					mat.SetFloat(_propertyID__MaskOp_1, 0.0f);
				}
				
				//채널 2
				if(maskRT_Ch2 != null && receivedPropSet_Ch2 != null)
				{
					//마스크 채널 2 사용함
					mat.SetFloat(_propertyID__MaskRatio_2, 1.0f);//사용함 (1)
					mat.SetTexture(_propertyID__MaskTex_2, maskRT_Ch2.GetRenderTexture());
					mat.SetFloat(_propertyID__MaskOp_2, receivedPropSet_Ch2.GetMaskOpFloatValue());
				}
				else
				{
					//마스크 채널 2 사용 안함
					mat.SetFloat(_propertyID__MaskRatio_2, 0.0f);//사용 안함 (0)
					mat.SetTexture(_propertyID__MaskTex_2, null);
					mat.SetFloat(_propertyID__MaskOp_2, 0.0f);
				}

				//채널 3
				if(maskRT_Ch3 != null && receivedPropSet_Ch3 != null)
				{
					//마스크 채널 3 사용함
					mat.SetFloat(_propertyID__MaskRatio_3, 1.0f);//사용함 (1)
					mat.SetTexture(_propertyID__MaskTex_3, maskRT_Ch3.GetRenderTexture());
					mat.SetFloat(_propertyID__MaskOp_3, receivedPropSet_Ch3.GetMaskOpFloatValue());
				}
				else
				{
					//마스크 채널 3 사용 안함
					mat.SetFloat(_propertyID__MaskRatio_3, 0.0f);//사용 안함 (0)
					mat.SetTexture(_propertyID__MaskTex_3, null);
					mat.SetFloat(_propertyID__MaskOp_3, 0.0f);
				}

				//채널 4
				if(maskRT_Ch4 != null && receivedPropSet_Ch4 != null)
				{
					//마스크 채널 4 사용함
					mat.SetFloat(_propertyID__MaskRatio_4, 1.0f);//사용함 (1)
					mat.SetTexture(_propertyID__MaskTex_4, maskRT_Ch4.GetRenderTexture());
					mat.SetFloat(_propertyID__MaskOp_4, receivedPropSet_Ch4.GetMaskOpFloatValue());
				}
				else
				{
					//마스크 채널 4 사용 안함
					mat.SetFloat(_propertyID__MaskRatio_4, 0.0f);//사용 안함 (0)
					mat.SetTexture(_propertyID__MaskTex_4, null);
					mat.SetFloat(_propertyID__MaskOp_4, 0.0f);
				}

				//텍스쳐 투과
				if(seeThroughRT != null && receivedPropSet_SeeThrough != null)
				{
					//텍스쳐 투과 사용함
					mat.SetFloat(_propertyID__SeeThroughRatio, 1.0f);
					mat.SetTexture(_propertyID__SeeThroughTex, seeThroughRT.GetRenderTexture());
					mat.SetFloat(_propertyID__SeeThroughAlpha, receivedPropSet_SeeThrough.GetValue_Float());
				}
				else
				{
					//텍스쳐 투과 사용 안함
					mat.SetFloat(_propertyID__SeeThroughRatio, 0.0f);
					mat.SetTexture(_propertyID__SeeThroughTex, null);
					mat.SetFloat(_propertyID__SeeThroughAlpha, 0.0f);
				}



				mat.SetPass(0);


				//GL.Begin 및 정보 저장
				GL.Begin(GLMode);

				_isRenderingBegun = true;
				_lastGLMode = GLMode;

				//_lastRenderTexture = _renderTexture;
				//_lastParentColor = parentColor;

				_lastToneLineThickness = _toneLineThickness;
				_lastToneShapeRatio = _toneShapeRatio;
				_lastTonePosOffset.x = _tonePosOffset.x * _zoom;
				_lastTonePosOffset.y = _tonePosOffset.y * _zoom;
			}




			#region [미사용 코드]
			//public void BeginPass_ClippedWithMaskedTexture(	int GLMode, 
			//												Color color, Texture2D texture, float vertColorRatio,
			//												apPortrait.SHADER_TYPE shaderType,
			//												//Color parentColor,//삭제
			//												Texture2D maskedTexture, Vector4 screenSize)
			//{
			//	if(_isRenderingBegun)
			//	{
			//		//RenderTexture를 이용하는 경우엔 Pass를 유지하지 않는다.
			//		//End 후 Pass 시작
			//		GL.End();
			//		GL.Flush();
			//	}

			//	_matType = MatType.Clipped;

			//	_shaderType_Main = shaderType;
			//	_iShaderType_Main = (int)_shaderType_Main;

			//	_color = color;
			//	_texture = texture;
			//	_mat_Clipped[_iShaderType_Main].SetColor(_propertyID__Color, _color);//_Color
			//	_mat_Clipped[_iShaderType_Main].SetTexture(_propertyID__MainTex, _texture);//_MainTex
			//	_mat_Clipped[_iShaderType_Main].SetFloat(_propertyID__vColorITP, vertColorRatio);//_vColorITP

			//	////<<Mask를 넣자
			//	_mat_Clipped[_iShaderType_Main].SetTexture(_propertyID__MaskRenderTexture, maskedTexture);//_MaskRenderTexture
			//	//_mat_Clipped[_iShaderType_Main].SetColor(_propertyID__MaskColor, parentColor);//_MaskColor 삭제 v1.6.0

			//	//추가 21.5.19 : ScreenSize 적용
			//	SetClippingSize(screenSize);

			//	_mat_Clipped[_iShaderType_Main].SetPass(0);


			//	//GL.Begin 및 정보 저장
			//	GL.Begin(GLMode);

			//	_isRenderingBegun = true;
			//	_lastGLMode = GLMode;

			//	//_lastMaskedTexutre = maskedTexture;
			//	//_lastParentColor = parentColor;

			//	_lastVertColorRatio = vertColorRatio;
			//} 
			#endregion

			//v1.6.0 : 마스크 기능 포함
			public void BeginPass_Alpha2White(	int GLMode,
												Color color,
												Texture2D texture,
												Vector4 screenSize,
												RenderTexture clippingMask,
												RenderTexture receiveMaskRT_1, apSendMaskData.MASK_OPERATION receiveMaskOp_1,
												RenderTexture receiveMaskRT_2, apSendMaskData.MASK_OPERATION receiveMaskOp_2,
												RenderTexture receiveMaskRT_3, apSendMaskData.MASK_OPERATION receiveMaskOp_3,
												RenderTexture receiveMaskRT_4, apSendMaskData.MASK_OPERATION receiveMaskOp_4)
			{
				if(_isRenderingBegun)
				{
					if(_matType != MatType.Alpha2White 
						|| _lastGLMode != GLMode						
						|| _texture != texture
						|| IsColorDifferent(_color, color)
						)
					{
						//End 후 Pass 시작
						GL.End();
						GL.Flush();
					}
					else
					{
						//Pass 유지
						return;
					}
				}

				_matType = MatType.Alpha2White;
				_shaderType_Main = apPortrait.SHADER_TYPE.AlphaBlend;
				_iShaderType_Main = 0;

				_color = color;
				_texture = texture;

				_mat_Alpha2White.SetColor(_propertyID__Color, _color);//_Color
				_mat_Alpha2White.SetTexture(_propertyID__MainTex, _texture);//_MainTex

				//클리핑 마스크
				if(clippingMask != null)
				{
					_mat_Alpha2White.SetTexture(_propertyID__MaskRenderTexture, clippingMask);//_MaskRenderTexture
					_mat_Alpha2White.SetFloat(_propertyID__MaskRatio, 1.0f);
				}
				else
				{
					_mat_Alpha2White.SetTexture(_propertyID__MaskRenderTexture, null);
					_mat_Alpha2White.SetFloat(_propertyID__MaskRatio, 0.0f);
				}

				//각 채널별 Received Mask
				//채널 1
				if(receiveMaskRT_1 != null)
				{
					_mat_Alpha2White.SetFloat(_propertyID__MaskRatio_1, 1.0f);//사용함 (1)
					_mat_Alpha2White.SetTexture(_propertyID__MaskTex_1, receiveMaskRT_1);
					_mat_Alpha2White.SetFloat(_propertyID__MaskOp_1, apSendMaskData.MaskOperationToFloatValue(receiveMaskOp_1));
				}
				else
				{
					_mat_Alpha2White.SetFloat(_propertyID__MaskRatio_1, 0.0f);
					_mat_Alpha2White.SetTexture(_propertyID__MaskTex_1, null);
					_mat_Alpha2White.SetFloat(_propertyID__MaskOp_1, 0.0f);
				}

				//채널 2
				if(receiveMaskRT_2 != null)
				{
					_mat_Alpha2White.SetFloat(_propertyID__MaskRatio_2, 1.0f);//사용함 (1)
					_mat_Alpha2White.SetTexture(_propertyID__MaskTex_2, receiveMaskRT_2);
					_mat_Alpha2White.SetFloat(_propertyID__MaskOp_2, apSendMaskData.MaskOperationToFloatValue(receiveMaskOp_2));
				}
				else
				{
					_mat_Alpha2White.SetFloat(_propertyID__MaskRatio_2, 0.0f);
					_mat_Alpha2White.SetTexture(_propertyID__MaskTex_2, null);
					_mat_Alpha2White.SetFloat(_propertyID__MaskOp_2, 0.0f);
				}

				//채널 3
				if(receiveMaskRT_3 != null)
				{
					_mat_Alpha2White.SetFloat(_propertyID__MaskRatio_3, 1.0f);//사용함 (1)
					_mat_Alpha2White.SetTexture(_propertyID__MaskTex_3, receiveMaskRT_3);
					_mat_Alpha2White.SetFloat(_propertyID__MaskOp_3, apSendMaskData.MaskOperationToFloatValue(receiveMaskOp_3));
				}
				else
				{
					_mat_Alpha2White.SetFloat(_propertyID__MaskRatio_3, 0.0f);
					_mat_Alpha2White.SetTexture(_propertyID__MaskTex_3, null);
					_mat_Alpha2White.SetFloat(_propertyID__MaskOp_3, 0.0f);
				}

				//채널 4
				if(receiveMaskRT_4 != null)
				{
					_mat_Alpha2White.SetFloat(_propertyID__MaskRatio_4, 1.0f);//사용함 (1)
					_mat_Alpha2White.SetTexture(_propertyID__MaskTex_4, receiveMaskRT_4);
					_mat_Alpha2White.SetFloat(_propertyID__MaskOp_4, apSendMaskData.MaskOperationToFloatValue(receiveMaskOp_4));
				}
				else
				{
					_mat_Alpha2White.SetFloat(_propertyID__MaskRatio_4, 0.0f);
					_mat_Alpha2White.SetTexture(_propertyID__MaskTex_4, null);
					_mat_Alpha2White.SetFloat(_propertyID__MaskOp_4, 0.0f);
				}



				SetClippingSize(screenSize);

				_mat_Alpha2White.SetPass(0);

				//GL.Begin 및 정보 저장
				GL.Begin(GLMode);

				_isRenderingBegun = true;
				_lastGLMode = GLMode;
			}

			public void BeginPass_BoneV2(int GLMode)
			{
				if(_isRenderingBegun)
				{
					if(_matType != MatType.BoneV2 
						|| _lastGLMode != GLMode)
					{
						//End 후 Pass 시작
						GL.End();
						GL.Flush();
					}
					else
					{
						//Pass 유지
						return;
					}
				}

				_matType = MatType.BoneV2;
				_shaderType_Main = apPortrait.SHADER_TYPE.AlphaBlend;
				_iShaderType_Main = 0;
				
				_mat_BoneV2.SetPass(0);


				//GL.Begin 및 정보 저장
				GL.Begin(GLMode);

				_isRenderingBegun = true;
				_lastGLMode = GLMode;
			}

			public void BeginPass_TextureVColorMul(int GLMode, Texture2D texture)
			{
				if(_isRenderingBegun)
				{
					if(_matType != MatType.Texture_VColorMul 
						|| _lastGLMode != GLMode						
						|| _texture != texture
						)
					{
						//End 후 Pass 시작
						GL.End();
						GL.Flush();
					}
					else
					{
						//Pass 유지
						return;
					}
				}

				_matType = MatType.Texture_VColorMul;

				_shaderType_Main = apPortrait.SHADER_TYPE.AlphaBlend;
				_iShaderType_Main = 0;

				_texture = texture;

				_mat_Texture_VColorMul.SetTexture(_propertyID__MainTex, _texture);
				_mat_Texture_VColorMul.SetPass(0);


				//GL.Begin 및 정보 저장
				GL.Begin(GLMode);

				_isRenderingBegun = true;
				_lastGLMode = GLMode;
			}

			public void BeginPass_RigCircleV2(int GLMode)
			{
				if(_isRenderingBegun)
				{
					if(_matType != MatType.RigCircleV2 
						|| _lastGLMode != GLMode)
					{
						//End 후 Pass 시작
						GL.End();
						GL.Flush();
					}
					else
					{
						//Pass 유지
						return;
					}
				}

				_matType = MatType.RigCircleV2;
				_shaderType_Main = 0;

				_mat_RigCircleV2.SetPass(0);


				//GL.Begin 및 정보 저장
				GL.Begin(GLMode);

				_isRenderingBegun = true;
				_lastGLMode = GLMode;
			}


			public void BeginPass_Gray_Normal(int GLMode, Color color, Texture2D texture)
			{
				if(_isRenderingBegun)
				{
					if(_matType != MatType.Gray_Normal 
						|| _lastGLMode != GLMode
						|| _texture != texture
						|| IsColorDifferent(_color, color)
						)
					{
						//End 후 Pass 시작
						GL.End();
						GL.Flush();
					}
					else
					{
						//Pass 유지
						return;
					}
				}

				_matType = MatType.Gray_Normal;

				_color = color;
				_texture = texture;

				_mat_Gray_Normal.SetColor(_propertyID__Color, _color);//_Color
				_mat_Gray_Normal.SetTexture(_propertyID__MainTex, _texture);//_MainTex

				_mat_Gray_Normal.SetPass(0);

				//GL.Begin 및 정보 저장
				GL.Begin(GLMode);

				_isRenderingBegun = true;
				_lastGLMode = GLMode;
			}

			public void BeginPass_Gray_Clipped(	int GLMode,
												Color color,
												Texture2D texture
												//Color parentColor
												)
			{
				if(_isRenderingBegun)
				{
					//RenderTexture를 이용하는 경우엔 Pass를 유지하지 않는다.
					//End 후 Pass 시작
					GL.End();
					GL.Flush();
				}

				_matType = MatType.Gray_Clipped;
				
				_color = color;
				_texture = texture;
				_mat_Gray_Clipped.SetColor(_propertyID__Color, _color);//_Color
				_mat_Gray_Clipped.SetTexture(_propertyID__MainTex, _texture);//_MainTex
				
				//Mask를 넣자
				_mat_Gray_Clipped.SetTexture(_propertyID__MaskRenderTexture, _renderTexture);//_MaskRenderTexture
				//_mat_Gray_Clipped.SetColor(_propertyID__MaskColor, parentColor);//_MaskColor//삭제 v1.6.0

				_mat_Gray_Clipped.SetPass(0);


				//GL.Begin 및 정보 저장
				GL.Begin(GLMode);

				_isRenderingBegun = true;
				_lastGLMode = GLMode;

				//_lastRenderTexture = _renderTexture;
				//_lastParentColor = parentColor;
			}



			public void BeginPass_Gray_Clipped_WithMaskInfo(	int GLMode,
																Color color,
																Texture2D texture,
																
																//클리핑
																apMaskRT clippingParentMaskRT,
																//Color clippingParentColor,//삭제
											
																//채널별 마스크
																apMaskRT maskRT_Ch1, apSendMaskData.ReceivePropertySet receivedPropSet_Ch1,
																apMaskRT maskRT_Ch2, apSendMaskData.ReceivePropertySet receivedPropSet_Ch2,
																apMaskRT maskRT_Ch3, apSendMaskData.ReceivePropertySet receivedPropSet_Ch3,
																apMaskRT maskRT_Ch4, apSendMaskData.ReceivePropertySet receivedPropSet_Ch4,
																apMaskRT seeThroughRT, apSendMaskData.ReceivePropertySet receivedPropSet_SeeThrough

																)
			{
				if(_isRenderingBegun)
				{
					//RenderTexture를 이용하는 경우엔 Pass를 유지하지 않는다.
					//End 후 Pass 시작
					GL.End();
					GL.Flush();
				}

				_matType = MatType.Gray_Clipped;
				
				_color = color;
				_texture = texture;

				Material mat = _mat_Gray_Clipped;

				mat.SetColor(_propertyID__Color, _color);//_Color
				mat.SetTexture(_propertyID__MainTex, _texture);//_MainTex
				
				//Mask를 넣자
				//1. 클리핑 마스크
				if(clippingParentMaskRT != null)
				{
					//클리핑 마스크가 있다면
					mat.SetTexture(_propertyID__MaskRenderTexture, clippingParentMaskRT.GetRenderTexture());
					//mat.SetColor(_propertyID__MaskColor, clippingParentColor);//삭제 v1.6.0
					mat.SetFloat(_propertyID__MaskRatio, 1.0f);//클리핑 마스크 적용함 (1)
				}
				else
				{
					//클리핑 마스크가 없다면
					mat.SetTexture(_propertyID__MaskRenderTexture, null);
					//mat.SetColor(_propertyID__MaskColor, _textureColor_Gray);//삭제 v1.6.0
					mat.SetFloat(_propertyID__MaskRatio, 0.0f);//클리핑 마스크 적용안함 (0)
				}
				

				//v1.6.0 : 채널별 임의 마스크
				//채널 1
				if(maskRT_Ch1 != null && receivedPropSet_Ch1 != null)
				{
					//마스크 채널 1 사용함
					mat.SetFloat(_propertyID__MaskRatio_1, 1.0f);//사용함 (1)
					mat.SetTexture(_propertyID__MaskTex_1, maskRT_Ch1.GetRenderTexture());
					mat.SetFloat(_propertyID__MaskOp_1, receivedPropSet_Ch1.GetMaskOpFloatValue());
				}
				else
				{
					//마스크 채널 1 사용 안함
					mat.SetFloat(_propertyID__MaskRatio_1, 0.0f);//사용 안함 (0)
					mat.SetTexture(_propertyID__MaskTex_1, null);
					mat.SetFloat(_propertyID__MaskOp_1, 0.0f);
				}
				
				//채널 2
				if(maskRT_Ch2 != null && receivedPropSet_Ch2 != null)
				{
					//마스크 채널 2 사용함
					mat.SetFloat(_propertyID__MaskRatio_2, 1.0f);//사용함 (1)
					mat.SetTexture(_propertyID__MaskTex_2, maskRT_Ch2.GetRenderTexture());
					mat.SetFloat(_propertyID__MaskOp_2, receivedPropSet_Ch2.GetMaskOpFloatValue());
				}
				else
				{
					//마스크 채널 2 사용 안함
					mat.SetFloat(_propertyID__MaskRatio_2, 0.0f);//사용 안함 (0)
					mat.SetTexture(_propertyID__MaskTex_2, null);
					mat.SetFloat(_propertyID__MaskOp_2, 0.0f);
				}

				//채널 3
				if(maskRT_Ch3 != null && receivedPropSet_Ch3 != null)
				{
					//마스크 채널 3 사용함
					mat.SetFloat(_propertyID__MaskRatio_3, 1.0f);//사용함 (1)
					mat.SetTexture(_propertyID__MaskTex_3, maskRT_Ch3.GetRenderTexture());
					mat.SetFloat(_propertyID__MaskOp_3, receivedPropSet_Ch3.GetMaskOpFloatValue());
				}
				else
				{
					//마스크 채널 3 사용 안함
					mat.SetFloat(_propertyID__MaskRatio_3, 0.0f);//사용 안함 (0)
					mat.SetTexture(_propertyID__MaskTex_3, null);
					mat.SetFloat(_propertyID__MaskOp_3, 0.0f);
				}

				//채널 4
				if(maskRT_Ch4 != null && receivedPropSet_Ch4 != null)
				{
					//마스크 채널 4 사용함
					mat.SetFloat(_propertyID__MaskRatio_4, 1.0f);//사용함 (1)
					mat.SetTexture(_propertyID__MaskTex_4, maskRT_Ch4.GetRenderTexture());
					mat.SetFloat(_propertyID__MaskOp_4, receivedPropSet_Ch4.GetMaskOpFloatValue());
				}
				else
				{
					//마스크 채널 4 사용 안함
					mat.SetFloat(_propertyID__MaskRatio_4, 0.0f);//사용 안함 (0)
					mat.SetTexture(_propertyID__MaskTex_4, null);
					mat.SetFloat(_propertyID__MaskOp_4, 0.0f);
				}

				//텍스쳐 투과
				if(seeThroughRT != null && receivedPropSet_SeeThrough != null)
				{
					//텍스쳐 투과 사용함
					mat.SetFloat(_propertyID__SeeThroughRatio, 1.0f);
					mat.SetTexture(_propertyID__SeeThroughTex, seeThroughRT.GetRenderTexture());
					mat.SetFloat(_propertyID__SeeThroughAlpha, receivedPropSet_SeeThrough.GetValue_Float());
				}
				else
				{
					//텍스쳐 투과 사용 안함
					mat.SetFloat(_propertyID__SeeThroughRatio, 0.0f);
					mat.SetTexture(_propertyID__SeeThroughTex, null);
					mat.SetFloat(_propertyID__SeeThroughAlpha, 0.0f);
				}

				mat.SetPass(0);


				//GL.Begin 및 정보 저장
				GL.Begin(GLMode);

				_isRenderingBegun = true;
				_lastGLMode = GLMode;

				//_lastRenderTexture = _renderTexture;
				//_lastParentColor = parentColor;
			}




			public void BeginPass_VertexAndPin(int GLMode)
			{
				if(_isRenderingBegun)
				{
					if(_matType != MatType.VertexAndPin 
						|| _lastGLMode != GLMode)
					{
						//End 후 Pass 시작
						GL.End();
						GL.Flush();
					}
					else
					{
						//Pass 유지
						return;
					}
				}

				_matType = MatType.VertexAndPin;
				_shaderType_Main = apPortrait.SHADER_TYPE.AlphaBlend;
				_iShaderType_Main = 0;
				
				_mat_VertAndPin.SetPass(0);


				//GL.Begin 및 정보 저장
				GL.Begin(GLMode);

				_isRenderingBegun = true;
				_lastGLMode = GLMode;
			}





			/// <summary>
			/// MultiPass에서 사용한 RenderTexture를 해제한다.
			/// 다만, 삭제하지는 않는다.
			/// </summary>
			public void DeactiveRenderTexture()
			{
				RenderTexture.active = null;
			}
			/// <summary>
			/// MultiPass의 모든 과정이 끝나면 사용했던 RenderTexture를 해제한다.
			/// </summary>
			public void ReleaseRenderTexture()
			{
				if (_renderTexture != null)
				{
					RenderTexture.active = null;
					RenderTexture.ReleaseTemporary(_renderTexture);
					_renderTexture = null;					
				}
				//_lastRenderTexture = null;
				//_lastMaskedTexutre = null;
			}
			public bool IsNotReady()
			{
				return (_mat_Color == null
					|| _mat_Texture_Normal == null
					|| _mat_Texture_VColorAdd == null
					//|| _mat_MaskedTexture == null
					|| _mat_Clipped == null
					|| _mat_MaskOnly == null
					|| _mat_GUITexture == null
					|| _mat_ToneColor_Normal == null
					|| _mat_ToneColor_Clipped == null
					|| _mat_Alpha2White == null
					|| _mat_Gray_Normal == null
					|| _mat_Gray_Clipped == null
					//|| _mat_VertAndPin == null
					);
			}

			

		}
    }
}
