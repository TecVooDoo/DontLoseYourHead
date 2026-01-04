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

	public static partial class apGL
	{	
		// 초기화 함수들
		//------------------------------------------------------------------------
		/// <summary>
		/// 초기화시 Shader 에셋들을 로드한다.
		/// </summary>
		public static void SetShader(Shader shader_Color,
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
									Shader shader_TextureVColorMul,
									Shader shader_RigCircleV2, Texture2D uniformTexture_RigCircleV2,
									Shader shader_Gray_Normal, Shader shader_Gray_Clipped,
									Shader shader_VertPin, Texture2D uniformTexture_VertAndPinAtlas
			)
		{
			_matBatch.SetShader(shader_Color,
								shader_Texture_Normal_Set,
								shader_Texture_VColorAdd_Set,
								shader_MaskOnly,
								shader_Clipped_Set,
								shader_GUITexture,
								shader_ToneColor_Normal,
								shader_ToneColor_Clipped,
								shader_Alpha2White,
								shader_BoneV2, uniformTexture_BoneSpriteSheet,
								shader_TextureVColorMul,
								shader_RigCircleV2, uniformTexture_RigCircleV2,
								shader_Gray_Normal, shader_Gray_Clipped,
								shader_VertPin, uniformTexture_VertAndPinAtlas);

			if(_func_GetWeightColor3 == null)
			{
				_func_GetWeightColor3 = GetWeightColor3;
			}
			if(_func_GetWeightColor3_Vert == null)
			{
				_func_GetWeightColor3_Vert = GetWeightColor3_Vert;
			}
		}

		/// <summary>
		/// 렌더링에 사용되는 텍스쳐 에셋들을 로드한다.
		/// </summary>
		public static void SetTexture(	Texture2D img_VertPhysicMain, 
										Texture2D img_VertPhysicConstraint
			)
		{
			_img_VertPhysicMain = img_VertPhysicMain;
			_img_VertPhysicConstraint = img_VertPhysicConstraint;
		}



		// 매 렌더링시 갱신 함수들
		//------------------------------------------------------------------------
		/// <summary>
		/// 매 렌더링마다 윈도우 크기 및 정보를 입력한다.
		/// </summary>
		public static void SetWindowSize(	int windowWidth, int windowHeight,
											Vector2 scroll, float zoom,
											int posX, int posY,
											int totalEditorWidth, int totalEditorHeight)
		{
			// 윈도우 / 스크롤 등을 갱신
			_windowWidth = windowWidth;
			_windowHeight = windowHeight;
			_scrol_NotCalculated = scroll;
			_windowScroll.x = scroll.x * _windowWidth * 0.1f;
			_windowScroll.y = scroll.y * windowHeight * 0.1f;
			_totalEditorWidth = totalEditorWidth;
			_totalEditorHeight = totalEditorHeight;

			_posX_NotCalculated = posX;
			_posY_NotCalculated = posY;

			_zoom = zoom;

			totalEditorHeight += 30;
			posY += 30;
			posX += 5;
			windowWidth -= 25;
			windowHeight -= 20;

			// 작업 공간의 렌더링 영역 (클리핑)
			_glScreenClippingSize.x = (float)posX / (float)totalEditorWidth;
			_glScreenClippingSize.y = (float)(posY) / (float)totalEditorHeight;
			_glScreenClippingSize.z = (float)(posX + windowWidth) / (float)totalEditorWidth;
			_glScreenClippingSize.w = (float)(posY + windowHeight) / (float)totalEditorHeight;

			_matBatch.CheckMaskTexture(_windowWidth, _windowHeight);


			// 타이머
			float timer = (float)_stopWatch.Elapsed.TotalSeconds;
			_animationTimeCount += timer;
			_animCount_BoneOutlineAlpha += timer;
			_animCount_SelectedRigFlashing += timer;

			_stopWatch.Stop();
			_stopWatch.Reset();
			_stopWatch.Start();

			while(_animationTimeCount > ANIMATION_TIME_LENGTH)
			{
				_animationTimeCount -= ANIMATION_TIME_LENGTH;
			}
			_animationTimeRatio = Mathf.Clamp01(_animationTimeCount / ANIMATION_TIME_LENGTH);


			while(_animCount_BoneOutlineAlpha > ANIM_LENGTH_BONE_OUTLINE_ALPHA)
			{
				_animCount_BoneOutlineAlpha -= ANIM_LENGTH_BONE_OUTLINE_ALPHA;
			}
			float outlineLerp = (Mathf.Cos(Mathf.Clamp01(_animCount_BoneOutlineAlpha / ANIM_LENGTH_BONE_OUTLINE_ALPHA) * Mathf.PI * 2.0f) * 0.5f) + 0.5f;
			_animRatio_BoneOutlineAlpha = (0.5f * (1.0f - outlineLerp)) + (1.0f * outlineLerp);//최소 Alpha는 0이 아닌 0.5


			while(_animCount_SelectedRigFlashing > ANIM_LENGTH_SELECTED_RIG_FLASHING)
			{
				_animCount_SelectedRigFlashing -= ANIM_LENGTH_SELECTED_RIG_FLASHING;
			}
			_animRatio_SelectedRigFlashing = (Mathf.Cos(Mathf.Clamp01(_animCount_SelectedRigFlashing / ANIM_LENGTH_SELECTED_RIG_FLASHING) * Mathf.PI * 2.0f) * 0.5f) + 0.5f;
			

			//추가 21.5.18 : 스크린 크기는 여기서 일괄 수정한다.
			_matBatch.SetClippingSizeToAllMaterial(_glScreenClippingSize);
		}


		/// <summary>
		/// Onion 등에 의해서 색상 톤 옵션을 갱신한다.
		/// </summary>
		public static void SetToneOption(Color toneColor, float toneLineThickness, bool isToneOutlineRender, float tonePosOffsetX, float tonePosOffsetY, Color toneBoneColor)
		{
			_toneColor = toneColor;
			_toneLineThickness = Mathf.Clamp01(toneLineThickness);
			_toneShapeRatio = isToneOutlineRender ? 0.0f : 1.0f;
			_tonePosOffset.x = tonePosOffsetX;
			_tonePosOffset.y = tonePosOffsetY;
			_toneBoneColor = toneBoneColor;
		}


		//추가 20.3.25 : RigCircle에 대한 옵션을 설정할 수 있다.
		/// <summary>
		/// 리깅 작업 과정에서의 렌더링 설정을 갱신한다.
		/// </summary>
		public static void SetRiggingOption(	int rigCircleScale_x100, 
												int rigCircleScale_x100_Selected, 
												bool isScaledByZoom, 
												apEditor.RIG_SELECTED_WEIGHT_GUI_TYPE rigSelectedWeightGUIType,
												apEditor.RIG_WEIGHT_GRADIENT_COLOR rigGradientColorType)
		{
			float rigCircleScaleRatio = ((float)rigCircleScale_x100) * 0.01f;
			float rigCircleScaleRatio_Selected = ((float)rigCircleScale_x100_Selected) * 0.01f;

			_rigCircleSize_NoSelectedVert = RIG_CIRCLE_SIZE_DEF * rigCircleScaleRatio;
			_rigCircleSize_SelectedVert = RIG_CIRCLE_SIZE_DEF * rigCircleScaleRatio_Selected;
			
			_isRigCircleScaledByZoom = isScaledByZoom;


			_isRigSelectedWeightArea_Enlarged = rigSelectedWeightGUIType == apEditor.RIG_SELECTED_WEIGHT_GUI_TYPE.Enlarged || rigSelectedWeightGUIType == apEditor.RIG_SELECTED_WEIGHT_GUI_TYPE.EnlargedAndFlashing;
			_isRigSelectedWeightArea_Flashing = rigSelectedWeightGUIType == apEditor.RIG_SELECTED_WEIGHT_GUI_TYPE.Flashing || rigSelectedWeightGUIType == apEditor.RIG_SELECTED_WEIGHT_GUI_TYPE.EnlargedAndFlashing;

			//_rigSelectedWeightGUIType = rigSelectedWeightGUIType;
			_rigGradientColorType = rigGradientColorType;

			if (_isRigSelectedWeightArea_Enlarged)
			{
				//선택된 영역의 크기가 커지는 경우
				_rigCircleSize_NoSelectedVert_Enlarged = _rigCircleSize_NoSelectedVert * RIG_CIRCLE_ENLARGED_SCALE_RATIO;
				_rigCircleSize_SelectedVert_Enlarged = _rigCircleSize_SelectedVert * RIG_CIRCLE_ENLARGED_SCALE_RATIO;
			}
			else
			{
				//선택된 영역도 크기가 동일할 경우
				_rigCircleSize_NoSelectedVert_Enlarged = _rigCircleSize_NoSelectedVert;
				_rigCircleSize_SelectedVert_Enlarged = _rigCircleSize_SelectedVert;
			}

			//클릭 범위는 작은 범위를 기준으로 한다.
			_rigCircleSize_ClickSize_Rigged = (_rigCircleSize_NoSelectedVert < _rigCircleSize_SelectedVert) ? _rigCircleSize_NoSelectedVert : _rigCircleSize_SelectedVert;
			//_rigCircleSize_ClickSize_Rigged = Mathf.Max(_rigCircleSize_ClickSize_Rigged * 0.9f, _rigCircleSize_ClickSize_Rigged - 2.0f);//원형이므로 크기를 조금 축소한다.


			if (_rigGradientColorType == apEditor.RIG_WEIGHT_GRADIENT_COLOR.Vivid)
			{
				//Vivid 방식이다.
				_func_GetWeightColor3 = GetWeightColor3_Vivid;;
				_func_GetWeightColor3_Vert = GetWeightColor3_Vivid;
			}
			else
			{
				_func_GetWeightColor3 = GetWeightColor3;;
				_func_GetWeightColor3_Vert = GetWeightColor3_Vert;
			}
			
		}

		/// <summary>
		/// v1.4.2 : 버텍스, 핀의 크기값을 입력한다.
		/// </summary>
		public static void SetVertexPinRenderOption(	float vertRenderSizeHalf,
														float pinRenderSizeHalf,
														float pinLineThickness)
		{
			_vertexRenderSizeHalf = vertRenderSizeHalf;
			_pinRenderSizeHalf = pinRenderSizeHalf;
			_pinLineThickness = pinLineThickness;
		}


		#region [미사용 코드] 클리핑 레이어용 RT의 정밀 크기 보정 코드인데 일단 보류

		///// <summary>
		///// [v1.4.6] 렌더 텍스쳐용 캘리브레이션.
		///// 마스크 텍스쳐나 캡쳐에서 사용되는 "전체 화면 크기의 RT"의 실제 크기를 구하기 위해서 사용되는 함수다.
		///// 원래는 editor.position.width / height를 사용했는데, 그게 정확한 크기를 주지 않는 버그가 있었다.
		///// 테스트 렌더링을 1회 하여 캘리브레이션을 한번 한 후, 색상 측정을 통해서 비율을 다시 조절한다.
		///// </summary>
		///// <param name="isForce"></param>
		//public static void CalibrateScreenRTSize(bool isForce, apEditor editor)
		//{
		//	//목표
		//	//기존의 에디터 윈도우 크기로 RT를 생성하는건 그래픽 디바이스에서 화면에 출력하는 크기와 다르기 때문에, 종횡비가 왜곡이 된다.
		//	//유니티 5.5, FHD 기준으로 Width가 약 1.023392 정도 더 크게 보여진다. (Height는 멀쩡하당)
		//	//RT 종횡비를 실제 디바이스에 맞게 보정해야한다.

		//	//측정 방법
		//	//(1) 기존 크기의 2배로 RT를 생성한다. (흰색)
		//	//(2) "GL 좌표"를 이용하여 PixelPerfect하게 "정사각형" 박스 렌더링을 먼저 한다.
		//	//(3) 색상 측정으로 박스의 RT 상의 크기를 체크한다. (픽셀 좌표)
		//	//(4) 종횡비 왜곡을 계산한다
		//	if(!isForce 
		//		&& _isFullScreenRTCalibrateCalculated)
		//	{
		//		//강제가 아니고 이미 계산되었다면
		//		return;
		//	}

		//	//샘플 사이즈는 에디터 크기를 가져오며, 최소 값은 넘겨야 한다.
		//	//종횡비만 계산하면 되므로, 에디터 크기와 완전히 같을 필요는 없다.
		//	//
		//	int editorSize_Width = Mathf.Max((int)editor.position.width, 1000);
		//	int editorSize_Height = Mathf.Max((int)editor.position.height, 1000);

		//	//정확한 측정을 위해 두배로 늘린다.
		//	int baseRTSize_Width = editorSize_Width * 2;
		//	int baseRTSize_Height = editorSize_Height * 2;

		//	//R 채널만 가진 Render Texture를 만든다.
		//	RenderTexture renderTexture = RenderTexture.GetTemporary(baseRTSize_Width, baseRTSize_Height, 8, RenderTextureFormat.ARGB32);
		//	renderTexture.isPowerOfTwo = false;
		//	renderTexture.wrapMode = TextureWrapMode.Clamp;
		//	renderTexture.filterMode = FilterMode.Point;

		//	RenderTexture.active = null;
		//	RenderTexture.active = renderTexture;

		//	//배경을 검은색으로 만들자
		//	Color black = Color.black;
		//	GL.Clear(true, true, black);
		//	DrawBoxGL(Vector2.zero, baseRTSize_Width * 2, baseRTSize_Height * 2, black, false, true);
		//	GL.Flush();

		//	//가운데에 흰색 박스를 그리자

		//	Vector2 centerPos = new Vector2(editorSize_Width / 2, editorSize_Height / 2);
		//	//Rect Size는 EditorSize의 짧은 축의 60%로 설정한다.
		//	int minAxis = Mathf.Min(editorSize_Width, editorSize_Height);
		//	float rectSize = (float)minAxis * 0.6f;

		//	DrawBoxGL_PixelPerfect(centerPos, rectSize, rectSize, Color.white, false, true);
		//	GL.Flush();

		//	//텍스쳐 2D로 가져온다.
		//	Texture2D resultTex = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false);
		//	resultTex.filterMode = FilterMode.Point;
		//	resultTex.wrapMode = TextureWrapMode.Clamp;

		//	resultTex.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
		//	resultTex.Apply();

		//	RenderTexture.active = null;

		//	//렌더 텍스쳐를 해제한다.
		//	RenderTexture.ReleaseTemporary(renderTexture);

		//	//색상을 검토하자
		//	Color[] colors = resultTex.GetPixels();

		//	int textureWidth = resultTex.width;
		//	int textureHeight = resultTex.height;
		//	int arrTotalSize = colors.Length;

		//	//색상 X의 Min-Max를 구한다.
		//	bool isAnyMinMax = false;
		//	int minX = 0;
		//	int minY = 0;
		//	int maxX = 0;
		//	int maxY = 0;

		//	Color curColor = Color.white;
		//	float bias = 0.5f;
		//	for (int iX = 0; iX < textureWidth; iX++)
		//	{
		//		for (int iY = 0; iY < textureHeight; iY++)
		//		{
		//			int iColor = (iY * textureWidth) + iX;
		//			if(iColor >= arrTotalSize)
		//			{
		//				continue;
		//			}

		//			curColor = colors[iColor];

		//			if(curColor.r > bias)
		//			{
		//				//사각형 데이터가 있다면
		//				if(!isAnyMinMax)
		//				{
		//					//초기화
		//					isAnyMinMax = true;
		//					minX = iX;
		//					maxX = iX;

		//					minY = iY;
		//					maxY = iY;							
		//				}
		//				else
		//				{
		//					minX = Mathf.Min(iX, minX);
		//					maxX = Mathf.Max(iX, maxX);

		//					minY = Mathf.Min(iY, minY);
		//					maxY = Mathf.Max(iY, maxY);
		//				}
		//			}

		//		}
		//	}

		//	int test_Day = DateTime.Now.Day;
		//	int test_Hour = DateTime.Now.Hour;
		//	int test_Min = DateTime.Now.Minute;
		//	int test_Sec = DateTime.Now.Second;
		//	string testFilePath = "C:\\AnyWorks\\Auto-Calibration-" + test_Day + "-" + test_Hour + "-" + test_Min + "-" + test_Sec;

		//	System.IO.File.WriteAllBytes(testFilePath + ".png", resultTex.EncodeToPNG());
		//	System.IO.FileInfo test_fi = new System.IO.FileInfo(testFilePath + ".png");//Path 빈 문자열 확인했음 (21.9.10)

		//	Application.OpenURL("file://" + test_fi.Directory.FullName);
		//	//Application.OpenURL("file://" + test_fi);



		//	//색상 인식용 텍스쳐를 삭제한다.
		//	UnityEngine.Object.DestroyImmediate(resultTex);

		//	Debug.Log("RT Calibrate 결과 [" + isAnyMinMax + "]");
		//	Debug.Log("- 크기 : " + baseRTSize_Width + "x" + baseRTSize_Height);
		//	Debug.Log("- Rect Size : " + rectSize);

		//	//이제 RenderTexture 크기를 계산해야한다.
		//	if(isAnyMinMax)
		//	{
		//		int rectWidth = maxX - minX;
		//		int rectHeight = maxY - minY;

		//		Debug.Log("- 사각형이 측정됨");

		//		//측정된 Rect를 계산하자
		//		if(rectWidth > 0 && rectHeight > 0)
		//		{
		//			_isFullScreenRTCalibrated = true;//보정이 완료되었다.

		//			_calibratedRTSize_Width = textureWidth;
		//			_calibratedRTSize_Height = textureHeight;


		//			Debug.Log("- 측정된 사각형 크기 : " + rectWidth + "x" + rectHeight);
		//			Debug.Log("- 보정 전 RT 기본 크기 : " + _calibratedRTSize_Width + "x" + _calibratedRTSize_Height);

		//			//측정된 사각형의 종횡비를 구하자
		//			if(rectWidth != rectHeight)
		//			{
		//				//Rect가 실제와 RT간의 비율이 같지 않다면, 
		//				//이후에 RT를 생성할 때 크기를 변경해야 한다.
		//				//Width가 더 길다면 (AspectRatio > 1)
		//				//RT Width는 1/AspectRatio 만큼 줄여야 한다.
		//				//또는 그 반대
		//				float aspectRatio = (float)rectWidth / (float)rectHeight;

		//				//AspectRatio의 반대만큼 Width를 가감한다.
		//				_calibratedRTSize_Width = (int)((float)_calibratedRTSize_Width / aspectRatio);

		//				Debug.Log("- AspectRatio : " + aspectRatio);
		//				Debug.Log("- 보정 후 RT 기본 크기 : " + _calibratedRTSize_Width + "x" + _calibratedRTSize_Height);
		//			}
		//		}
		//	}

		//	//결과에 상관없이 Calculated는 완료되었다.
		//	_isFullScreenRTCalibrateCalculated = true;
		//}

		//public static bool IsFullScreenRTCalibrated()
		//{
		//	return _isFullScreenRTCalibrated;
		//}
		////private static bool _isFullScreenRTCalibrated = false;//캘리브레이션 여부
		////private static int _calibratedRTSize_Width = 0;//캘리브레이션시 계산된 Width/Height (비율 계산 용)
		////private static int _calibratedRTSize_Height = 0;
		////private static float _calibratedRTAspectRatio = 0.0f;//캘리브레이션된 종횡비 (X/Y) > 이게 중요하다.

		#endregion



		// 좌표계 변환 함수들
		//------------------------------------------------------------------------
		
		//World 2 GL 함수들
		public static Vector2 World2GL(Vector2 pos)
		{
			return new Vector2(
				(pos.x * _zoom) + (_windowWidth * 0.5f) - _windowScroll.x,
				(_windowHeight - (pos.y * _zoom)) - (_windowHeight * 0.5f) - _windowScroll.y);
		}

		public static void World2GL(ref Vector2 dst, ref Vector2 srcPos)
		{
			dst.x = (srcPos.x * _zoom) + (_windowWidth * 0.5f) - _windowScroll.x;
			dst.y = (_windowHeight - (srcPos.y * _zoom)) - (_windowHeight * 0.5f) - _windowScroll.y;
		}

		/// <summary>리턴값이 Vector3인 경우의 변환 함수. Ref 이용</summary>
		public static void World2GL_Vec3(ref Vector3 dst, ref Vector2 srcPos)
		{
			dst.x = (srcPos.x * _zoom) + (_windowWidth * 0.5f) - _windowScroll.x;
			dst.y = (_windowHeight - (srcPos.y * _zoom)) - (_windowHeight * 0.5f) - _windowScroll.y;
			dst.z = 0.0f;
		}

		/// <summary>Src와 Dst가 같은 경우의 World2GL</summary>
		public static void World2GL_Self(ref Vector2 pos)
		{
			pos.x = (pos.x * _zoom) + (_windowWidth * 0.5f) - _windowScroll.x;
			pos.y = (_windowHeight - (pos.y * _zoom)) - (_windowHeight * 0.5f) - _windowScroll.y;
		}



		// Local > World > GL 함수
		private static Vector2 s_tmp_L2G_WorldPos = Vector2.zero;
		public static void Local2GL(ref Vector3 dstVector3, ref apMatrix3x3 worldMatrix, ref Vector2 srcPos, float zDepth = 0.0f)
		{
			apMatrix3x3.MultiplyPoint(ref s_tmp_L2G_WorldPos, ref worldMatrix, ref srcPos);

			dstVector3.x = (s_tmp_L2G_WorldPos.x * _zoom) + (_windowWidth * 0.5f) - _windowScroll.x;
			dstVector3.y = (_windowHeight - (s_tmp_L2G_WorldPos.y * _zoom)) - (_windowHeight * 0.5f) - _windowScroll.y;
			dstVector3.z = zDepth;
		}



		//GL 2 World 함수들
		public static Vector2 GL2World(Vector2 glPos)
		{
			return new Vector2(
				(glPos.x + (_windowScroll.x) - (_windowWidth * 0.5f)) / _zoom,
				(-1.0f * (glPos.y + _windowScroll.y + (_windowHeight * 0.5f) - (_windowHeight))) / _zoom
				);
		}

		public static void GL2World(ref Vector2 dstPos, ref Vector2 glPos)
		{
			dstPos.x = (glPos.x + (_windowScroll.x) - (_windowWidth * 0.5f)) / _zoom;
			dstPos.y = (-1.0f * (glPos.y + _windowScroll.y + (_windowHeight * 0.5f) - (_windowHeight))) / _zoom;
		}


		/// <summary>버텍스가 렌더 영역 밖에 있는지 여부</summary>
		private static bool IsVertexClipped(Vector2 posGL)
		{
			return (posGL.x < 1.0f || posGL.x > _windowWidth - 1 ||
									posGL.y < 1.0f || posGL.y > _windowHeight - 1);
		}

		/// <summary>버텍스가 렌더 영역 밖에 있는지 여부</summary>
		private static bool Is2VertexClippedAll(Vector2 pos1GL, Vector2 pos2GL)
		{
			bool isPos1Clipped = IsVertexClipped(pos1GL);

			bool isPos2Clipped = IsVertexClipped(pos2GL);


			if (!isPos1Clipped || !isPos2Clipped)
			{
				//둘중 하나라도 안에 들어있다.
				return false;
			}


			//두 점이 밖에 나갔어도, 중간 점이 걸쳐서 들어올 수 있다.
			Vector2 posDir = pos2GL - pos1GL;
			for (int i = 1; i < 5; i++)
			{
				Vector2 posSub = pos1GL + posDir * ((float)i / 5.0f);

				bool isPosSubClipped = IsVertexClipped(posSub);

				//중간점 하나가 들어와있다.
				if (!isPosSubClipped)
				{
					return false;
				}
			}
			return true;
		}


		private static Vector2 GetClippedVertex(Vector2 posTargetGL, Vector2 posBaseGL)
		{
			Vector2 pos1_Real = posTargetGL;
			Vector2 pos2_Real = posBaseGL;

			Vector2 dir1To2 = (pos2_Real - pos1_Real).normalized;
			Vector2 dir2To1 = -dir1To2;

			if (pos1_Real.x < 0.0f || pos1_Real.x > _windowWidth ||
				pos1_Real.y < 0.0f || pos1_Real.y > _windowHeight)
			{
				//2 + dir(2 -> 1) * t = 1'
				//dir * t = 1' - 2
				//t = (1' - 2) / dir

				float tX = 0.0f;
				float tY = 0.0f;
				float tResult = 0.0f;

				bool isClipX = false;
				bool isClipY = false;


				if (posTargetGL.x < 0.0f)
				{
					pos1_Real.x = 0.0f;
					isClipX = true;
				}
				else if (posTargetGL.x > _windowWidth)
				{
					pos1_Real.x = _windowWidth;
					isClipX = true;
				}

				if (posTargetGL.y < 0.0f)
				{
					pos1_Real.y = 0.0f;
					isClipY = true;
				}
				else if (posTargetGL.y > _windowHeight)
				{
					pos1_Real.y = _windowHeight;
					isClipY = true;
				}

				if (isClipX)
				{
					if (Mathf.Abs(dir2To1.x) > 0.0f)
					{ tX = (pos1_Real.x - pos2_Real.x) / dir2To1.x; }
					else
					{ return new Vector2(-100.0f, -100.0f); }//둘다 나갔다...
				}

				if (isClipY)
				{
					if (Mathf.Abs(dir2To1.y) > 0.0f)
					{ tY = (pos1_Real.y - pos2_Real.y) / dir2To1.y; }
					else
					{ return new Vector2(-100.0f, -100.0f); }//둘다 나갔다...
				}
				if (isClipX && isClipY)
				{
					if (Mathf.Abs(tX) < Mathf.Abs(tY))
					{
						tResult = tX;
					}
					else
					{
						tResult = tY;
					}
				}
				else if (isClipX)
				{
					tResult = tX;
				}
				else if (isClipY)
				{
					tResult = tY;
				}

				//2 + dir(2 -> 1) * t = 1'
				pos1_Real = pos2_Real + dir2To1 * tResult;
				return pos1_Real;
			}
			else
			{
				return pos1_Real;
			}
		}


		private static Vector2 GetClippedVertexNoBase(Vector2 posTargetGL)
		{
			Vector2 pos1_Real = posTargetGL;

			if (pos1_Real.x < 0.0f || pos1_Real.x > _windowWidth ||
				pos1_Real.y < 0.0f || pos1_Real.y > _windowHeight)
			{
				if (posTargetGL.x < 0.0f)
				{
					pos1_Real.x = 0.0f;
				}
				else if (posTargetGL.x > _windowWidth)
				{
					pos1_Real.x = _windowWidth;
				}

				if (posTargetGL.y < 0.0f)
				{
					pos1_Real.y = 0.0f;
				}
				else if (posTargetGL.y > _windowHeight)
				{
					pos1_Real.y = _windowHeight;
				}
				return pos1_Real;
			}
			else
			{
				return pos1_Real;
			}
		}


		//------------------------------------------------------------------------------------------------
		// Draw Grid
		//------------------------------------------------------------------------------------------------
		public static void DrawGrid(Color lineColor_Center, Color lineColor)
		{
			int pixelSize = 50;

			//Color lineColor = new Color(0.3f, 0.3f, 0.3f, 1.0f);
			//Color lineColor_Center = new Color(0.7f, 0.7f, 0.3f, 1.0f);

			if (_zoom < 0.2f + 0.05f)
			{
				pixelSize = 200;
				lineColor.a = 0.4f;
			}
			else if (_zoom < 0.5f + 0.05f)
			{
				pixelSize = 100;
				lineColor.a = 0.7f;
			}

			//Vector2 centerPos = World2GL(Vector2.zero);

			//Screen의 Width, Height에 해당하는 극점을 찾자
			//Vector2 pos_LT = GL2World(new Vector2(0, 0));
			//Vector2 pos_RB = GL2World(new Vector2(_windowWidth, _windowHeight));
			Vector2 pos_LT = GL2World(new Vector2(-500, -500));
			Vector2 pos_RB = GL2World(new Vector2(_windowWidth + 500, _windowHeight + 500));

			float yWorld_Max = Mathf.Max(pos_LT.y, pos_RB.y) + 100;
			float yWorld_Min = Mathf.Min(pos_LT.y, pos_RB.y) - 200;
			float xWorld_Max = Mathf.Max(pos_LT.x, pos_RB.x);
			float xWorld_Min = Mathf.Min(pos_LT.x, pos_RB.x);

			// 가로줄 먼저 (+- Y로 움직임)
			Vector2 curPos = Vector2.zero;
			//Vector2 curPosGL = Vector2.zero;
			Vector2 posA, posB;

			curPos.y = (int)(yWorld_Min / pixelSize) * pixelSize;


			//추가 21.5.18
			_matBatch.EndPass();
			_matBatch.BeginPass_Color(GL.LINES);

			// + Y 방향 (아래)
			while (true)
			{
				//curPosGL = World2GL(curPos);

				//if(curPosGL.y < 0 || curPosGL.y > _windowHeight)
				//{
				//	break;
				//}
				if (curPos.y > yWorld_Max)
				{
					break;
				}


				posA.x = pos_LT.x;
				posA.y = curPos.y;

				posB.x = pos_RB.x;
				posB.y = curPos.y;

				DrawLine(posA, posB, lineColor, false);

				curPos.y += pixelSize;
			}


			curPos = Vector2.zero;
			curPos.x = (int)(xWorld_Min / pixelSize) * pixelSize;

			// + X 방향 (오른쪽)
			while (true)
			{
				//curPosGL = World2GL(curPos);

				//if(curPosGL.x < 0 || curPosGL.x > _windowWidth)
				//{
				//	break;
				//}
				if (curPos.x > xWorld_Max)
				{
					break;
				}

				posA.y = pos_LT.y;
				posA.x = curPos.x;

				posB.y = pos_RB.y;
				posB.x = curPos.x;

				DrawLine(posA, posB, lineColor, false);

				curPos.x += pixelSize;
			}

			//중앙선

			curPos = Vector2.zero;

			posA.x = pos_LT.x;
			posA.y = curPos.y;

			posB.x = pos_RB.x;
			posB.y = curPos.y;

			DrawLine(posA, posB, lineColor_Center, false);


			posA.y = pos_LT.y;
			posA.x = curPos.x;

			posB.y = pos_RB.y;
			posB.x = curPos.x;

			DrawLine(posA, posB, lineColor_Center, false);

			_matBatch.EndPass();
		}


		// Editing Border
		public static void DrawEditingBorderline()
		{
			//Vector2 pos = new Vector2(_windowPosX + (_windowWidth / 2), _windowPosY + (_windowHeight / 2));
			Vector2 pos = new Vector2((_windowWidth / 2), (_windowHeight));

			Color borderColor = new Color(0.7f, 0.0f, 0.0f, 0.8f);
			DrawBox(GL2World(pos), (float)(_windowWidth + 100) / _zoom, 50.0f / _zoom, borderColor, false);

			pos.y = -12;

			DrawBox(GL2World(pos), (float)(_windowWidth + 100) / _zoom, 50.0f / _zoom, borderColor, false);
		}

		//-----------------------------------------------------------------------------------------
		public static void ResetCursorEvent()
		{
			_isAnyCursorEvent = false;
			_isDelayedCursorEvent = false;
			_delayedCursorPos = Vector2.zero;
			_delayedCursorType = MouseCursor.Zoom;
		}

		/// <summary>
		/// 마우스 커서를 나오게 하자
		/// </summary>
		/// <param name="mousePos"></param>
		/// <param name="pos"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="cursorType"></param>
		private static void AddCursorRect(Vector2 mousePos, Vector2 pos, float width, float height, MouseCursor cursorType)
		{
			if (pos.x < 0 || pos.x > _windowWidth || pos.y < 0 || pos.y > _windowHeight)
			{
				return;
			}

			if (mousePos.x < pos.x - width * 2 ||
				mousePos.x > pos.x + width * 2 ||
				mousePos.y < pos.y - height * 2 ||
				mousePos.y > pos.y + height * 2)
			{
				//영역을 벗어났다.
				return;
			}

			pos.x -= width / 2;
			pos.y -= height / 2;

			EditorGUIUtility.AddCursorRect(new Rect(pos.x, pos.y, width, height), cursorType);

			_isAnyCursorEvent = true;

		}

		/// <summary>
		/// 마우스 커서를 바꾼다.
		/// 범위는 GL영역 전부이다.
		/// 한번만 호출되며, GUI가 모두 끝날 때 ProcessDelayedCursor()함수를 호출해야한다.
		/// </summary>
		/// <param name="mousePos"></param>
		/// <param name="cursorType"></param>
		public static void AddCursorRectDelayed(Vector2 mousePos, MouseCursor cursorType)
		{
			if(_isAnyCursorEvent || _isDelayedCursorEvent)
			{
				return;
			}
			if (mousePos.x < 0 || mousePos.x > _windowWidth || mousePos.y < 0 || mousePos.y > _windowHeight)
			{
				return;
			}

			_isDelayedCursorEvent = true;
			_delayedCursorPos = mousePos;
			_delayedCursorType = cursorType;

			//Debug.Log("AddCursorRectDelayed : " + mousePos);

			
		}

		public static void ProcessDelayedCursor()
		{
			//Debug.Log("ProcessDelayedCursor : " + _isDelayedCursorEvent);

			if(!_isDelayedCursorEvent
				|| _isAnyCursorEvent
				)
			{
				_isDelayedCursorEvent = false;
				return;
			}
			_isDelayedCursorEvent = false;
			float bias = 20;
			
			EditorGUIUtility.AddCursorRect(
								new Rect(
									_posX_NotCalculated + _delayedCursorPos.x - bias, 
									_posY_NotCalculated + _delayedCursorPos.y - bias, 
									bias * 2, bias * 2), 
								//new Rect(mousePos.x - bias, mousePos.y - bias, bias * 2, bias * 2), 
								//new Rect(_posX_NotCalculated, _posY_NotCalculated, _windowWidth, _windowHeight),
								_delayedCursorType);
			//Debug.Log("AddCurRect : " + _delayedCursorType);
		}


		//-----------------------------------------------------------------------------------------
		private static Color _weightColor_Gray = new Color(0.2f, 0.2f, 0.2f, 1.0f);
		private static Color _weightColor_Blue = new Color(0.0f, 0.2f, 1.0f, 1.0f);
		private static Color _weightColor_Yellow = new Color(1.0f, 1.0f, 0.0f, 1.0f);
		private static Color _weightColor_Red = new Color(1.0f, 0.0f, 0.0f, 1.0f);
		public static Color GetWeightColor(float weight)
		{
			if (weight < 0.0f)
			{
				return _weightColor_Gray;
			}
			else if (weight < 0.5f)
			{
				return _weightColor_Blue * (1.0f - weight * 2.0f) + _weightColor_Yellow * (weight * 2.0f);
			}
			else if (weight < 1.0f)
			{
				return _weightColor_Yellow * (1.0f - (weight - 0.5f) * 2.0f) + _weightColor_Red * ((weight - 0.5f) * 2.0f);
			}
			else
			{
				return _weightColor_Red;
			}
		}

		public static Color GetWeightColor2(float weight, apEditor editor)
		{
			if (weight < 0.0f)
			{
				return editor._colorOption_VertColor_NotSelected;
			}
			else if (weight < 0.25f)
			{
				return (_vertColor_Weighted_0 * (0.25f - weight) + _vertColor_Weighted_25 * (weight)) / 0.25f;
			}
			else if (weight < 0.5f)
			{
				return (_vertColor_Weighted_25 * (0.25f - (weight - 0.25f)) + _vertColor_Weighted_50 * (weight - 0.25f)) / 0.25f;
			}
			else if (weight < 0.75f)
			{
				return (_vertColor_Weighted_50 * (0.25f - (weight - 0.5f)) + _vertColor_Weighted_75 * (weight - 0.5f)) / 0.25f;
			}
			else if (weight < 1.0f)
			{
				return (_vertColor_Weighted_75 * (0.25f - (weight - 0.75f)) + editor._colorOption_VertColor_Selected * (weight - 0.75f)) / 0.25f;
			}
			else
			{
				//return _weightColor_Red;
				return editor._colorOption_VertColor_Selected;
			}
		}

		public static Color GetWeightColor3(float weight)
		{

			if (weight < 0.0f)
			{
				return _vertColor_Weighted3_0;
			}
			else if (weight < 0.25f)
			{
				return (_vertColor_Weighted3_0 * (0.25f - weight) + _vertColor_Weighted3_25 * (weight)) / 0.25f;
			}
			else if (weight < 0.5f)
			{
				return (_vertColor_Weighted3_25 * (0.25f - (weight - 0.25f)) + _vertColor_Weighted3_50 * (weight - 0.25f)) / 0.25f;
			}
			else if (weight < 0.75f)
			{
				return (_vertColor_Weighted3_50 * (0.25f - (weight - 0.5f)) + _vertColor_Weighted3_75 * (weight - 0.5f)) / 0.25f;
			}
			else if (weight < 1.0f)
			{
				return (_vertColor_Weighted3_75 * (0.25f - (weight - 0.75f)) + _vertColor_Weighted3_100 * (weight - 0.75f)) / 0.25f;
			}
			else
			{
				//return _weightColor_Red;
				return _vertColor_Weighted3_100;
			}
		}


		public static Color GetWeightColor3_Vert(float weight)
		{
			if (weight < 0.0f)
			{
				return _vertColor_Weighted3Vert_0;
			}
			else if (weight < 0.25f)
			{
				return (_vertColor_Weighted3Vert_0 * (0.25f - weight) + _vertColor_Weighted3Vert_25 * (weight)) / 0.25f;
			}
			else if (weight < 0.5f)
			{
				return (_vertColor_Weighted3Vert_25 * (0.25f - (weight - 0.25f)) + _vertColor_Weighted3Vert_50 * (weight - 0.25f)) / 0.25f;
			}
			else if (weight < 0.75f)
			{
				return (_vertColor_Weighted3Vert_50 * (0.25f - (weight - 0.5f)) + _vertColor_Weighted3Vert_75 * (weight - 0.5f)) / 0.25f;
			}
			else if (weight < 1.0f)
			{
				return (_vertColor_Weighted3Vert_75 * (0.25f - (weight - 0.75f)) + _vertColor_Weighted3Vert_100 * (weight - 0.75f)) / 0.25f;
			}
			else
			{
				//return _weightColor_Red;
				return _vertColor_Weighted3Vert_100;
			}
		}



		//추가 20.3.28 : Vivid 방식의 리깅 가중치 그라디언트. (HSV를 이용한다.)
		public static Color GetWeightColor3_Vivid(float weight)
		{
			if(weight < 0.00001f)
			{
				return _vertHSV_Weighted3_NULL;//검은색. 이건 RGB타입이다.
			}
			Vector3 curHSV = Vector3.zero;
			//Hue는 0 (빨강) ~ 0.167 (노랑) ~ 0.667 (파랑)
			//Sat는 1.0 (고정)
			//Value는 Weight = 0.5까지는 1, 그 이하는 서서히 0.5로 수렴
			
			curHSV.y = 1.0f;
			float lerp = 0.0f;
			if(weight < 0.5f)
			{
				lerp = weight * 2.0f;
				curHSV.x = (0.667f * (1.0f - lerp)) + (0.167f * lerp);
				curHSV.z = (0.5f * (1.0f - lerp)) + (1.0f * lerp);
			}
			else
			{
				lerp = (weight - 0.5f) * 2.0f;
				curHSV.x = (0.167f * (1.0f - lerp)) + (0.0f * lerp);
				curHSV.z = 1.0f;
			}
			
			return Color.HSVToRGB(curHSV.x, curHSV.y, curHSV.z, false);
		}




		public static Color GetWeightColor4(float weight)
		{
			if (weight <= 0.0001f)
			{
				return _vertColor_Weighted4_0_Null;
			}
			else if (weight < 0.33f)
			{
				return (_vertColor_Weighted4_0 * (0.33f - weight) + _vertColor_Weighted4_33 * (weight)) / 0.33f;
			}
			else if (weight < 0.66f)
			{
				return (_vertColor_Weighted4_33 * (0.33f - (weight - 0.33f)) + _vertColor_Weighted4_66 * (weight - 0.33f)) / 0.33f;
			}
			else if (weight < 1.0f)
			{
				return (_vertColor_Weighted4_66 * (0.34f - (weight - 0.66f)) + _vertColor_Weighted4_100 * (weight - 0.66f)) / 0.34f;
			}
			else
			{
				return _vertColor_Weighted4_100;
			}
		}


		public static Color GetWeightColor4_Vert(float weight)
		{
			if (weight <= 0.0001f)
			{
				return _vertColor_Weighted4Vert_Null;
			}
			else if (weight < 0.33f)
			{
				return (_vertColor_Weighted4Vert_0 * (0.33f - weight) + _vertColor_Weighted4Vert_33 * (weight)) / 0.33f;
			}
			else if (weight < 0.66f)
			{
				return (_vertColor_Weighted4Vert_33 * (0.33f - (weight - 0.33f)) + _vertColor_Weighted4Vert_66 * (weight - 0.33f)) / 0.33f;
			}
			else if (weight < 1.0f)
			{
				return (_vertColor_Weighted4Vert_66 * (0.34f - (weight - 0.66f)) + _vertColor_Weighted4Vert_100 * (weight - 0.66f)) / 0.34f;
			}
			else
			{
				return _vertColor_Weighted4Vert_100;
			}
		}

		public static Color GetWeightGrayscale(float weight)
		{
			//return _weightColor_Gray * (1.0f - weight) + Color.black * weight;
			return Color.black * (1.0f - weight) + Color.white * weight;
		}

		// Window 파라미터 복사 및 복구
		//------------------------------------------------------------------------------------
		public class WindowParameters
		{
			public int _windowWidth;
			public int _windowHeight;
			public Vector2 _scrol_NotCalculated;
			public Vector2 _windowScroll;
			public int _totalEditorWidth;
			public int _totalEditorHeight;
			public int _posX_NotCalculated;
			public int _posY_NotCalculated;
			public float _zoom;
			public Vector4 _glScreenClippingSize;
			public float _animationTimeCount;
			public float _animationTimeRatio;

			public WindowParameters() { }
		}

		public static void GetWindowParameters(WindowParameters inParam)
		{
			inParam._windowWidth = _windowWidth;
			inParam._windowHeight = _windowHeight;
			inParam._scrol_NotCalculated = _scrol_NotCalculated;
			inParam._windowScroll = _windowScroll;
			inParam._totalEditorWidth = _totalEditorWidth;
			inParam._totalEditorHeight = _totalEditorHeight;
			inParam._posX_NotCalculated = _posX_NotCalculated;
			inParam._posY_NotCalculated = _posY_NotCalculated;
			inParam._zoom = _zoom;
			inParam._glScreenClippingSize = _glScreenClippingSize;
			inParam._animationTimeCount = _animationTimeCount;
			inParam._animationTimeRatio = _animationTimeRatio;
		}

		public static void RecoverWindowSize(WindowParameters winParam)
		{
			_windowWidth = winParam._windowWidth;
			_windowHeight = winParam._windowHeight;
			_scrol_NotCalculated = winParam._scrol_NotCalculated;
			_windowScroll = winParam._windowScroll;
			_totalEditorWidth = winParam._totalEditorWidth;
			_totalEditorHeight = winParam._totalEditorHeight;
			_posX_NotCalculated = winParam._posX_NotCalculated;
			_posY_NotCalculated = winParam._posY_NotCalculated;
			_zoom = winParam._zoom;
			_glScreenClippingSize = winParam._glScreenClippingSize;
			_animationTimeCount = winParam._animationTimeCount;
			_animationTimeRatio = winParam._animationTimeRatio;
		}

		public static void SetScreenClippingSizeTmp(Vector4 clippingSize)
		{
			_glScreenClippingSize = clippingSize;
		}
	}

}