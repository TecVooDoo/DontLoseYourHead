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
	// apGL 중에서 멤버 변수들을 여기에 정의한다.
	public static partial class apGL
	{
		//------------------------------------
        // 윈도우 크기 / 줌 / 스크롤
		//------------------------------------
		public static int _windowWidth = 0;
		public static int _windowHeight = 0;
		public static int _totalEditorWidth = 0;
		public static int _totalEditorHeight = 0;
		public static Vector2 _scrol_NotCalculated = Vector2.zero;
		public static int _posX_NotCalculated = 0;
		public static int _posY_NotCalculated = 0;

		public static Vector2 _windowScroll = Vector2.zero;

		public static float _zoom = 1.0f;
		public static float Zoom { get { return _zoom; } }

		public static Vector2 WindowSize { get { return new Vector2(_windowWidth, _windowHeight); } }
		public static Vector2 WindowSizeHalf { get { return new Vector2(_windowWidth / 2, _windowHeight / 2); } }

		private static Vector4 _glScreenClippingSize = Vector4.zero;


		//------------------------------------
		// GUI 요소 / 이벤트
		//------------------------------------
		private static GUIStyle _textStyle = GUIStyle.none;

		private static bool _isAnyCursorEvent = false;
		private static bool _isDelayedCursorEvent = false;
		private static Vector2 _delayedCursorPos = Vector2.zero;
		private static MouseCursor _delayedCursorType = MouseCursor.Zoom;


		//------------------------------------
		// 랜더 방식에 대한 요청 (서브 클래스)
		//------------------------------------
		/// <summary>
		/// 변경 22.3.3 (v1.4.0) 클래스 타입의 렌더 방식.
		/// 기존의 Flag 타입의 RenderType 대신 그 이상의 요청을 모두 담을 수 있는 클래스 작성
		/// </summary>
		public class RenderTypeRequest
		{
			// 프리셋 (Static)
			private static RenderTypeRequest s_preset_Default = Make_Default();
			private static RenderTypeRequest s_preset_ToneColor = Make_ToneColor();
			public static RenderTypeRequest Preset_Default		{ get { return s_preset_Default; } }
			public static RenderTypeRequest Preset_ToneColor	{ get { return s_preset_ToneColor; } }

			public enum VISIBILITY
			{
				Shown,
				Hidden,
				Transparent
			}

			public enum SHOW_OPTION
			{
				Normal, Transparent
			}

			// Members
			//--------------------------------------------------------------
			private bool _isShadeAllMesh = false;
			private bool _isAllMesh = false;
			private VISIBILITY _visibility_Vertex = VISIBILITY.Hidden;
			private bool _isOutlines = false;
			private bool _isAllEdges = false;
			private bool _isTransparentEdges = false;
			private bool _isVolumeWeightColor = false;
			private bool _isPhysicsWeightColor = false;
			private bool _isBoneRigWeightColor = false;
			private bool _isTransformBorderLine = false;
			private bool _isPolygonOutline = false;
			private bool _isToneColor = false;
			private bool _isBoneOutlineOnly = false;
			private VISIBILITY _visibility_Pin = VISIBILITY.Hidden;			//핀
			private bool _isTestPinWeight = false;//핀 가중치를 테스트하는 상태 (다른 좌표계를 사용한다.)
			private bool _isPinVertWeight = false;//버텍스에 핀 가중치를 표시하는 상태
			private bool _isPinRange = false;//핀의 Range를 표시할지 여부

			public RenderTypeRequest()
			{
				Reset();
			}

			/// <summary>
			/// 모든 렌더링 옵션을 초기화한다.
			/// </summary>
			public void Reset()
			{
				_isShadeAllMesh = false;
				_isAllMesh = false;
				_visibility_Vertex = VISIBILITY.Hidden;
				_isOutlines = false;
				_isAllEdges = false;
				_isTransparentEdges = false;
				_isVolumeWeightColor = false;
				_isPhysicsWeightColor = false;
				_isBoneRigWeightColor = false;
				_isTransformBorderLine = false;
				_isPolygonOutline = false;
				_isToneColor = false;
				_isBoneOutlineOnly = false;
				_visibility_Pin = VISIBILITY.Hidden;
				_isTestPinWeight = false;
				_isPinVertWeight = false;
				_isPinRange = false;
			}

			public void SetShadeAllMesh()			{ _isShadeAllMesh = true; }
			public void SetAllMesh()				{ _isAllMesh = true; }
			public void SetVertex(SHOW_OPTION showOption)
			{
				_visibility_Vertex = (showOption == SHOW_OPTION.Normal) ? VISIBILITY.Shown : VISIBILITY.Transparent;
			}
			public void SetOutlines()				{ _isOutlines = true; }
			public void SetAllEdges()				{ _isAllEdges = true; }
			public void SetTransparentEdges()		{ _isTransparentEdges = true; }
			public void SetVolumeWeightColor()		{ _isVolumeWeightColor = true; }
			public void SetPhysicsWeightColor()		{ _isPhysicsWeightColor = true; }
			public void SetBoneRigWeightColor()		{ _isBoneRigWeightColor = true; }
			public void SetTransformBorderLine()	{ _isTransformBorderLine = true; }
			public void SetPolygonOutline()			{ _isPolygonOutline = true; }
			public void SetToneColor()				{ _isToneColor = true; }
			public void SetBoneOutlineOnly()		{ _isBoneOutlineOnly = true; }
			public void SetPin(SHOW_OPTION showOption)
			{
				_visibility_Pin = showOption == SHOW_OPTION.Normal ? VISIBILITY.Shown : VISIBILITY.Transparent;
			}
			
			public void SetTestPinWeight()			{ _isTestPinWeight = true; }
			public void SetPinVertWeight()			{ _isPinVertWeight = true; }
			public void SetPinRange()				{ _isPinRange = true; }

			public bool ShadeAllMesh		{ get { return _isShadeAllMesh; } }
			public bool AllMesh				{ get { return _isAllMesh; } }
			public VISIBILITY Vertex		{ get { return _visibility_Vertex; } }
			public bool Outlines			{ get { return _isOutlines; } }
			public bool AllEdges			{ get { return _isAllEdges; } }
			public bool TransparentEdges	{ get { return _isTransparentEdges; } }//AllEdges의 반투명 버전
			public bool VolumeWeightColor	{ get { return _isVolumeWeightColor; } }
			public bool PhysicsWeightColor	{ get { return _isPhysicsWeightColor; } }
			public bool BoneRigWeightColor	{ get { return _isBoneRigWeightColor; } }
			public bool TransformBorderLine	{ get { return _isTransformBorderLine; } }
			public bool PolygonOutline		{ get { return _isPolygonOutline; } }
			public bool ToneColor			{ get { return _isToneColor; } }
			public bool BoneOutlineOnly		{ get { return _isBoneOutlineOnly; } }
			public VISIBILITY Pin			{ get { return _visibility_Pin; } }
			public bool TestPinWeight		{ get { return _isTestPinWeight; } }
			public bool PinVertWeight		{ get { return _isPinVertWeight; } }
			public bool PinRange			{ get { return _isPinRange; } }

			//Static 프리셋 생성
			private static RenderTypeRequest Make_Default()
			{
				RenderTypeRequest newRequest = new RenderTypeRequest();
				newRequest.Reset();
				return newRequest;
			}

			private static RenderTypeRequest Make_ToneColor()
			{
				RenderTypeRequest newRequest = new RenderTypeRequest();
				newRequest.Reset();
				newRequest.SetToneColor();
				return newRequest;
			}
		}

		//------------------------------------
		// 계산 변수들
		//------------------------------------
		//본 그리기를 위한 임시 Matrix
		private static apMatrix _cal_TmpMatrix;




		//------------------------------------
		// 색상 값 / 변수
		//------------------------------------
		private static Color _textureColor_Gray = new Color(0.5f, 0.5f, 0.5f, 1.0f);
		private static Color _textureColor_Shade = new Color(0.3f, 0.3f, 0.3f, 1.0f);

		private static Color _vertColor_NextSelected = new Color(1.0f, 0.0f, 1.0f, 0.6f);

		//Weight인 경우 보(0)-파(25)-초(50)-노(75)-빨(100)로 이어진다.
		private static Color _vertColor_Weighted_0 = new Color(1.0f, 0.0f, 1.0f, 1.0f);
		private static Color _vertColor_Weighted_25 = new Color(0.0f, 0.5f, 1.0f, 1.0f);
		private static Color _vertColor_Weighted_50 = new Color(0.0f, 1.0f, 0.5f, 1.0f);
		private static Color _vertColor_Weighted_75 = new Color(1.0f, 1.0f, 0.0f, 1.0f);
		
		//리깅 가중치 > 기본 (Vert는 약간 더 밝다)
		private static Color _vertColor_Weighted3_0 = new Color(0.0f, 0.0f, 0.0f, 1.0f);//검은색
		private static Color _vertColor_Weighted3_25 = new Color(0.0f, 0.0f, 1.0f, 1.0f);//파랑
		private static Color _vertColor_Weighted3_50 = new Color(1.0f, 1.0f, 0.0f, 1.0f);//노랑
		private static Color _vertColor_Weighted3_75 = new Color(1.0f, 0.5f, 0.0f, 1.0f);//주황
		private static Color _vertColor_Weighted3_100 = new Color(1.0f, 0.0f, 0.0f, 1.0f);//빨강

		private static Color _vertColor_Weighted3Vert_0 = new Color(0.2f, 0.2f, 0.2f, 1.0f);
		private static Color _vertColor_Weighted3Vert_25 = new Color(0.2f, 0.2f, 1.0f, 1.0f);
		private static Color _vertColor_Weighted3Vert_50 = new Color(1.0f, 1.0f, 0.2f, 1.0f);
		private static Color _vertColor_Weighted3Vert_75 = new Color(1.0f, 0.5f, 0.2f, 1.0f);
		private static Color _vertColor_Weighted3Vert_100 = new Color(1.0f, 0.2f, 0.2f, 1.0f);

		//리깅 가중치 > Vivid : HSV 값에 의해서 보간하기 때문에 보간 도중에 색이 탁해지는 것이 덜하다. (20.3.288)
		private static Color _vertHSV_Weighted3_NULL = new Color(0.0f, 0.0f, 0.0f);//검은색 (RGB)
		//H : 0 - 0.167 - 0.33 (빨강 > 노랑 > 초록)
		//H : 0.5 - 0.667 - 0.83 (하늘색 > 파랑 > 보라)

		//색상을 리턴하는 함수를 Delegate로 만들어서 빠르게 전환하도록 한다. (20.3.28)
		public delegate Color FUNC_GET_GRADIENT_COLOR(float weight);
		private static FUNC_GET_GRADIENT_COLOR _func_GetWeightColor3 = null;
		private static FUNC_GET_GRADIENT_COLOR _func_GetWeightColor3_Vert = null;

		private static Color _vertColor_Weighted4_0_Null = new Color(0.0f, 0.0f, 0.0f, 1.0f);
		private static Color _vertColor_Weighted4_0 = new Color(1.0f, 0.5f, 0.0f, 1.0f);
		private static Color _vertColor_Weighted4_33 = new Color(0.0f, 1.0f, 0.0f, 1.0f);
		private static Color _vertColor_Weighted4_66 = new Color(0.0f, 1.0f, 1.0f, 1.0f);
		private static Color _vertColor_Weighted4_100 = new Color(1.0f, 0.0f, 1.0f, 1.0f);

		private static Color _vertColor_Weighted4Vert_Null = new Color(0.2f, 0.2f, 0.2f, 1.0f);
		private static Color _vertColor_Weighted4Vert_0 = new Color(1.0f, 0.5f, 0.2f, 1.0f);
		private static Color _vertColor_Weighted4Vert_33 = new Color(0.2f, 1.0f, 0.2f, 1.0f);
		private static Color _vertColor_Weighted4Vert_66 = new Color(0.2f, 1.0f, 1.0f, 1.0f);
		private static Color _vertColor_Weighted4Vert_100 = new Color(1.0f, 0.2f, 1.0f, 1.0f);


		//본 선택 색상 
		//V1
		//- 메인 : 붉은색 / R: 노란색
		//- 서브 : 밝은 주황색 / R: 연두색 > 서브 색상은 사용하지 않는다.
		//- 링크 : 붉은색 / R: 노란색 (더 투명함) 
		private static Color _lineColor_BoneOutline_V1_Default = new Color(1.0f, 0.0f, 0.2f, 0.8f);
		private static Color _lineColor_BoneOutline_V1_Reverse = new Color(1.0f, 0.8f, 0.0f, 0.8f);//Default색과 유사한 경우 두번째 색상을 이용한다.
		private static Color _lineColor_BoneOutlineRollOver_V1_Default = new Color(1.0f, 0.2f, 0.0f, 0.5f);
		private static Color _lineColor_BoneOutlineRollOver_V1_Reverse = new Color(1.0f, 1.0f, 0.0f, 0.5f);

		//V2
		//- 메인 : 붉은색 / R: 밝은 노란색
		//- 서브 : 밝은 주황색 / R: 밝은 연두색
		//- 링크 : 붉은 주황색 / R: 밝은 노란색 (더 투명함)
		private static Color _lineColor_BoneOutline_V2_Default = new Color(1.0f, 0.0f, 0.1f, 0.9f);
		private static Color _lineColor_BoneOutline_V2_Reverse = new Color(1.0f, 0.9f, 0.5f, 0.9f);
		private static Color _lineColor_BoneOutlineRollOver_V2_Default = new Color(1.0f, 0.4f, 0.0f, 0.7f);
		private static Color _lineColor_BoneOutlineRollOver_V2_Reverse = new Color(1.0f, 1.0f, 0.5f, 0.7f);
		
		//선택된 본 외곽선의 반짝임
		private static float _animRatio_BoneOutlineAlpha = 0.0f;
		private static float _animCount_BoneOutlineAlpha = 0.0f;
		private const float ANIM_LENGTH_BONE_OUTLINE_ALPHA = 2.4f;

		//선택된 본의 리깅 영역의 반짝임 (옵션)
		private static float _animRatio_SelectedRigFlashing = 0.0f;
		private static float _animCount_SelectedRigFlashing = 0.0f;
		private const float ANIM_LENGTH_SELECTED_RIG_FLASHING = 0.6f;

		private const float COLOR_SIMILAR_BIAS = 0.3f;
		private const float BRIGHTNESS_OUTLINE = 0.3f;//너무 어두워도 Reverse 색상을 사용하자

		private static Texture2D _img_VertPhysicMain = null;
		private static Texture2D _img_VertPhysicConstraint = null;
		//private static Texture2D _img_RigCircle = null;

		private static Color _toneColor = new Color(0.1f, 0.3f, 0.5f, 0.7f);
		private static float _toneLineThickness = 0.0f;
		private static float _toneShapeRatio = 0.0f;
		private static Vector2 _tonePosOffset = Vector2.zero;
		private static Color _toneBoneColor = new Color(1, 1, 1, 0.9f);


		//------------------------------------
		// 애니메이션 GUI를 위한 타이머
		//------------------------------------
		private static System.Diagnostics.Stopwatch _stopWatch = new System.Diagnostics.Stopwatch();
		private static float _animationTimeRatio = 0.0f;
		private static float _animationTimeCount = 0.0f;
		private const float ANIMATION_TIME_LENGTH = 0.9f;
		private const float ANIMATED_LINE_UNIT_LENGTH = 10.0f;
		private const float ANIMATED_LINE_SPACE_LENGTH = 6.0f;


		//------------------------------------
		// 본 리깅 관련 변수들
		//------------------------------------
		//리깅 버텍스의 크기를 별도의 변수로 두어서 옵션으로 컨트롤할 수 있게 만들자. (20.3.25)
		private const float RIG_CIRCLE_SIZE_NORIG = 14.0f;
		private const float RIG_CIRCLE_SIZE_NORIG_SELECTED = 18.0f;
		public const float RIG_CIRCLE_SIZE_NORIG_CLICK_SIZE = 12.0f;
		private const float RIG_CIRCLE_SIZE_DEF = 16.0f;
		private const float RIG_CIRCLE_ENLARGED_SCALE_RATIO = 1.5f;

		private static float _rigCircleSize_NoSelectedVert = 14.0f;
		private static float _rigCircleSize_NoSelectedVert_Enlarged = 14.0f;
		private static float _rigCircleSize_SelectedVert = 14.0f;
		private static float _rigCircleSize_SelectedVert_Enlarged = 24.0f;

		//클릭 영역을 정하자. 가능한 작게 설정
		private static float _rigCircleSize_ClickSize_Rigged = 10.0f;
		public static float RigCircleSize_Clickable
		{
			get
			{	
				return Mathf.Max((_isRigCircleScaledByZoom ? (_rigCircleSize_ClickSize_Rigged * _zoom) : _rigCircleSize_ClickSize_Rigged), 12.0f);
			}
		}

		private static bool _isRigCircleScaledByZoom = false;
		private static bool _isRigSelectedWeightArea_Enlarged = false;
		private static bool _isRigSelectedWeightArea_Flashing = false;
		private static apEditor.RIG_WEIGHT_GRADIENT_COLOR _rigGradientColorType = apEditor.RIG_WEIGHT_GRADIENT_COLOR.Default;


		//------------------------------------
		// 버텍스 / 핀 렌더링
		//------------------------------------
		//버텍스, 핀 렌더링 크기 (옵션에 의해 결정된다.)
		private static float _vertexRenderSizeHalf = 0.0f;
		private static float _pinRenderSizeHalf = 0.0f;
		private static float _pinLineThickness = 0.0f;


		//------------------------------------
		// Material Batch
		//------------------------------------
		private static MaterialBatch _matBatch = new MaterialBatch();
		public static MaterialBatch MatBatch { get { return _matBatch; } }
    }
}
