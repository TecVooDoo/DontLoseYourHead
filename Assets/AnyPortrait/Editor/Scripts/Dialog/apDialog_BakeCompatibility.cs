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
using System;
using System.Collections.Generic;
using AnyPortrait;

namespace AnyPortrait
{
	public class apDialog_BakeCompatibility : EditorWindow
	{
		// Static
		//-----------------------------------------------------------
		private static apDialog_BakeCompatibility s_window = null;

		// Show Window
		//------------------------------------------------------------------
		public static void ShowDialog(apEditor editor, apPortrait portrait)
		{
			
			CloseDialog();

			if (editor == null || portrait == null)
			{
				return;
			}
			
			
			EditorWindow curWindow = EditorWindow.GetWindow(typeof(apDialog_BakeCompatibility), true, "Check Options", true);
			apDialog_BakeCompatibility curTool = curWindow as apDialog_BakeCompatibility;

			//object loadKey = new object();
			if (curTool != null && curTool != s_window)
			{
				int width = 650;
				int height = 500;
				s_window = curTool;
				
				s_window.position = new Rect(	(editor.position.xMin + editor.position.xMax) / 2 - (width / 2),
												(editor.position.yMin + editor.position.yMax) / 2 - (height / 2),
												width, height);

				s_window.Init(editor, portrait);
			}
		}

		public static void CloseDialog()
		{
			if (s_window != null)
			{
				try
				{
					s_window.Close();
				}
				catch (Exception ex)
				{
					Debug.LogError("Close Exception : " + ex);
				}
				s_window = null;
			}
		}

		// Members
		//------------------------------------------------------------------
		private apEditor _editor = null;
		private apPortrait _portrait = null;

		private Texture2D _img_Icon_Okay = null;
		private Texture2D _img_Icon_Warning = null;

		private Texture2D _img_Icon_Fix = null;
		private Texture2D _img_Icon_Manual = null;
		private Texture2D _img_Icon_OpenMatLib = null;

		//유효성 검사 여부와 결과
		private bool _isAnyValidated = false;

		private apEditorUtil.CHECK_ENV_RENDER_PIPELINE _validResult_RP = apEditorUtil.CHECK_ENV_RENDER_PIPELINE.None;
		private apEditorUtil.CHECK_ENV_VALID_MATERIAL _validResult_Mat = apEditorUtil.CHECK_ENV_VALID_MATERIAL.None;
		private apEditorUtil.CHECK_ENV_COLOR_SPACE _validResult_ColorSpace = apEditorUtil.CHECK_ENV_COLOR_SPACE.None;
		private apEditorUtil.CHECK_ENV_CAMERA _validResult_Camera = apEditorUtil.CHECK_ENV_CAMERA.None;
		private apEditorUtil.RENDER_PIPELINE_ENV_RESULT _validCurProjectRP = apEditorUtil.RENDER_PIPELINE_ENV_RESULT.Unknown;
		private ColorSpace _validCurProjectColorSpace = ColorSpace.Gamma;
		private int _validCurNumCameras = 0;

		private GUIStyle _guiStyle_CenterIcon = null;
		private GUIStyle _guiStyle_Text_Title = null;
		private GUIStyle _guiStyle_Text_Body = null;

		private apGUIContentWrapper _guiContent_Img_Okay = null;
		private apGUIContentWrapper _guiContent_Img_Warning = null;
		private apGUIContentWrapper _guiContent_Button_Fix = null;
		private apGUIContentWrapper _guiContent_Button_Manual = null;
		private apGUIContentWrapper _guiContent_Button_OpenMatLib = null;

		private Vector2 _scroll = Vector2.zero;

		// Init
		//------------------------------------------------------------------
		public void Init(apEditor editor, apPortrait portrait)
		{
			_editor = editor;
			_portrait = portrait;
			
		}

		void OnGUI()
		{
			if (_editor == null || _portrait == null)
			{
				CloseDialog();
				return;
			}

			int width = (int)position.width;
			int height = (int)position.height;

			bool isClose = false;
			

			if(_img_Icon_Okay == null)
			{
				_img_Icon_Okay = _editor.ImageSet.Get(apImageSet.PRESET.Compatibility_Okay);
			}
			
			if(_img_Icon_Warning == null)
			{
				_img_Icon_Warning = _editor.ImageSet.Get(apImageSet.PRESET.Compatibility_Warning);
			}

			if (_img_Icon_Fix == null)
			{
				_img_Icon_Fix = _editor.ImageSet.Get(apImageSet.PRESET.Compatibility_20px_Fix);
			}

			if (_img_Icon_Manual == null)
			{
				_img_Icon_Manual = _editor.ImageSet.Get(apImageSet.PRESET.Compatibility_20px_Manual);
			}

			if (_img_Icon_OpenMatLib == null)
			{
				_img_Icon_OpenMatLib = _editor.ImageSet.Get(apImageSet.PRESET.Compatibility_20px_MatLib);
			}

			if (_guiStyle_CenterIcon == null)
			{
				_guiStyle_CenterIcon = new GUIStyle(GUI.skin.label);
				_guiStyle_CenterIcon.alignment = TextAnchor.MiddleCenter;
			}

			if(_guiStyle_Text_Title == null)
			{
				_guiStyle_Text_Title = new GUIStyle(GUI.skin.label);
				_guiStyle_Text_Title.alignment = TextAnchor.MiddleLeft;
			}

			if(_guiStyle_Text_Body == null)
			{
				_guiStyle_Text_Body = new GUIStyle(GUI.skin.label);
				_guiStyle_Text_Body.wordWrap = true;
				_guiStyle_Text_Body.alignment = TextAnchor.UpperLeft;
			}

			if(_guiContent_Img_Okay == null)
			{
				_guiContent_Img_Okay = apGUIContentWrapper.Make(_img_Icon_Okay);
			}

			if(_guiContent_Img_Warning == null)
			{
				_guiContent_Img_Warning = apGUIContentWrapper.Make(_img_Icon_Warning);
			}

			if(_guiContent_Button_Fix == null)
			{
				_guiContent_Button_Fix = apGUIContentWrapper.Make(_img_Icon_Fix);
			}

			if(_guiContent_Button_Manual == null)
			{
				_guiContent_Button_Manual = apGUIContentWrapper.Make(_img_Icon_Manual);
			}

			if(_guiContent_Button_OpenMatLib == null)
			{
				_guiContent_Button_OpenMatLib = apGUIContentWrapper.Make(_img_Icon_OpenMatLib);
			}
			

			if(!_isAnyValidated)
			{
				//유효성 테스트를 1회 하자
				ValidateOptions();
			}

			GUILayout.Space(5);

			//이제 하나씩 출력하자
			//1. 타이틀
			EditorGUILayout.Foldout(true, _editor.GetText(TEXT.Checklist));//"호환성 체크 리스트"
			GUILayout.Space(10);
			int height_Scroll = height - 110;

			_scroll = EditorGUILayout.BeginScrollView(_scroll, false, true, GUILayout.Width(width), GUILayout.Height(height_Scroll));

			int width_InScroll = width - 30;

			EditorGUILayout.BeginVertical(GUILayout.Width(width_InScroll));

			//2. 순서대로 그리자
			//2-1. 렌더 파이프라인
			string strRP_Title = _editor.GetText(TEXT.RenderPipeline);//"렌더 파이프라인"
			
			string strRP_Body = "";

			string strCurRP = "";
			switch (_validCurProjectRP)
			{
				case apEditorUtil.RENDER_PIPELINE_ENV_RESULT.BuiltIn:
					strCurRP = "Built-In";
					break;

				case apEditorUtil.RENDER_PIPELINE_ENV_RESULT.URP:
					strCurRP = "URP";
					break;

				case apEditorUtil.RENDER_PIPELINE_ENV_RESULT.Unknown:
					strCurRP = "Unknown";
					break;
			}

			switch(_validResult_RP)
			{
				case apEditorUtil.CHECK_ENV_RENDER_PIPELINE.None:
					{
						//strRP_Body = "현재 렌더 파이프라인 (" + _validCurProjectRP.ToString() + ")에 맞게 설정되어 있습니다.";
						strRP_Body = _editor.GetTextFormat(TEXT.BakeValidation_RP_Pass, strCurRP);
					}
					break;

				case apEditorUtil.CHECK_ENV_RENDER_PIPELINE.NeedChangeToURP:
					{
						//strRP_Body = "현재 렌더 파이프라인 (" + _validCurProjectRP.ToString() + ")과 설정이 맞지 않습니다.\n" +
						//		 "Bake의 Settings 탭에서 [Render Pipeline]을 [Scriptable Render Pipeline]으로 변경해주세요.";
						strRP_Body = _editor.GetTextFormat(TEXT.BakeValidation_RP_Failed, strCurRP);
						if(_editor._language == apEditor.LANGUAGE.Korean)
						{
							strRP_Body += "\n" + _editor.GetTextFormat(TEXT.BakeValidation_PleaseSetOption, _editor.GetText(TEXT.RenderPipeline), "Scriptable Render Pipeline", "으");
						}
						else
						{
							strRP_Body += "\n" + _editor.GetTextFormat(TEXT.BakeValidation_PleaseSetOption, _editor.GetText(TEXT.RenderPipeline), "Scriptable Render Pipeline");
						}
					}
					break;

				case apEditorUtil.CHECK_ENV_RENDER_PIPELINE.NeedChangeToBuiltIn:
					{
						//strRP_Body = "현재 렌더 파이프라인 (" + _validCurProjectRP.ToString() + ")과 설정이 맞지 않습니다.\n" +
						//		 "Bake의 Settings 탭에서 [Render Pipeline]을 [Default]로 변경해주세요.";
						strRP_Body = _editor.GetTextFormat(TEXT.BakeValidation_RP_Failed, strCurRP);
						if(_editor._language == apEditor.LANGUAGE.Korean)
						{
							strRP_Body += "\n" + _editor.GetTextFormat(TEXT.BakeValidation_PleaseSetOption, _editor.GetText(TEXT.RenderPipeline), "Default", "");
						}
						else
						{
							strRP_Body += "\n" + _editor.GetTextFormat(TEXT.BakeValidation_PleaseSetOption, _editor.GetText(TEXT.RenderPipeline), "Default");
						}
					}
					break;

				case apEditorUtil.CHECK_ENV_RENDER_PIPELINE.UnknownRP:
					{
						//strRP_Body = "알 수 없는 렌더 파이프라인입니다.\n정상적으로 렌더링이 되지 않을 수 있습니다.";
						strRP_Body = _editor.GetText(TEXT.BakeValidation_RP_UnknownRP);
					}
					break;
			}

			bool isValid_RP = (_validResult_RP == apEditorUtil.CHECK_ENV_RENDER_PIPELINE.None);

			DrawInfo(	isValid_RP, 
						strRP_Title,
						strRP_Body,
						width_InScroll,
						new ButtonActionInfo(_editor.GetText(TEXT.FixNow), BUTTON_TYPE.Fix, !isValid_RP, OnAction_RenderPipeline_Fix),//"고치기"
						new ButtonActionInfo(_editor.GetText(TEXT.BakeDialog), BUTTON_TYPE.Manual, true, OnAction_RenderPipeline_Manual_BakeDialog),//"Bake 다이얼로그"
						new ButtonActionInfo("URP", BUTTON_TYPE.Manual, _validCurProjectRP == apEditorUtil.RENDER_PIPELINE_ENV_RESULT.URP, OnAction_RenderPipeline_Manual_URP));

			GUILayout.Space(10);
			apEditorUtil.GUI_DelimeterBoxH(width_InScroll - 10);
			GUILayout.Space(10);


			//2-2. 재질
			bool isValid_Mat = _validResult_Mat == apEditorUtil.CHECK_ENV_VALID_MATERIAL.None;
			string strMat_Title = _editor.GetText(TEXT.Shaders);//"쉐이더"
			string strMat_Body = "";

			switch (_validResult_Mat)
			{
				case apEditorUtil.CHECK_ENV_VALID_MATERIAL.None:
					strMat_Body =  _editor.GetText(TEXT.BakeValidation_Shader_Pass);//"쉐이더들이 정상적으로 설정되어 있습니다."

					if(_validCurProjectRP != apEditorUtil.RENDER_PIPELINE_ENV_RESULT.URP)
					{
						//strMat_Body += "\n프로젝트의 렌더 파이프라인 (" + _validCurProjectRP.ToString() + ")에서는 유효성 검사가 정확하지 않을 수 있습니다.";
						//strMat_Body += "\n문제가 발생한다면 [재질 라이브러리]에서 쉐이더 에셋들을 검토해보세요.";
						strMat_Body += "\n" + _editor.GetText(TEXT.BakeValidation_Shader_BTWarning);
						
					}
					break;

				case apEditorUtil.CHECK_ENV_VALID_MATERIAL.InvalidMaterials:
					//strMat_Body = "일부 쉐이더들이 현재 렌더 파이프라인 (" + _validCurProjectRP.ToString() + ")에 맞지 않습니다.";
					//strMat_Body += "\n[재질 라이브러리]를 열고 현재 렌더 파이프라인에 맞는 [재질 세트]를 추가하고 적용하세요.";
					strMat_Body = _editor.GetTextFormat(TEXT.BakeValidation_Shader_Failed, strCurRP);
					break;
			}

			DrawInfo(	isValid_Mat,
						strMat_Title,
						strMat_Body,
						width_InScroll,
						new ButtonActionInfo(_editor.GetText(TEXT.BakeValidation_OpenMaterialLibrary), BUTTON_TYPE.OpenMatLib, true, OnAction_Shaders_OpenMaterialLibrary),//"재질 라이브러리 열기"
						new ButtonActionInfo(_editor.GetUIWord(UIWORD.MaterialLibrary), BUTTON_TYPE.Manual, true, OnAction_Shaders_Manual_MatLibrary),//"재질 라이브러리"
						new ButtonActionInfo("URP", BUTTON_TYPE.Manual, _validCurProjectRP == apEditorUtil.RENDER_PIPELINE_ENV_RESULT.URP, OnAction_Shaders_Manual_URP));



			GUILayout.Space(10);
			apEditorUtil.GUI_DelimeterBoxH(width_InScroll - 10);
			GUILayout.Space(10);

			//2-3. 색상 공간
			bool isValid_ColorSpace = _validResult_ColorSpace == apEditorUtil.CHECK_ENV_COLOR_SPACE.None;
			string strColorSpace_Title = _editor.GetUIWord(UIWORD.ColorSpace);//"색상 공간"
			string strColorSpace_Body = "";
			string strCurColorSpace = "";
			switch (_validCurProjectColorSpace)
			{
				case ColorSpace.Gamma:
					strCurColorSpace = "Gamma";
					break;

				case ColorSpace.Linear:
					strCurColorSpace = "Linear";
					break;

				case ColorSpace.Uninitialized:
					strCurColorSpace = "Unknown";
					break;
			}


			switch (_validResult_ColorSpace)
			{
				case apEditorUtil.CHECK_ENV_COLOR_SPACE.None:
					//strColorSpace_Body = "현재 색상 공간 (" + _validCurProjectColorSpace.ToString() + ")에 맞게 설정되어 있습니다.";
					strColorSpace_Body = _editor.GetTextFormat(TEXT.BakeValidation_ColorSpace_Pass, strCurColorSpace);
					break;

				case apEditorUtil.CHECK_ENV_COLOR_SPACE.NeedChangeToLinear:
					//strColorSpace_Body = "현재 색상 공간 (" + _validCurProjectColorSpace.ToString() + ")과 설정이 맞지 않습니다.\n" +
					//					"Bake의 Settings 탭에서 [Color Space]을 [Linear]로 변경해주세요.";
					strColorSpace_Body = _editor.GetTextFormat(TEXT.BakeValidation_ColorSpace_Failed, "Linear");

					if(_editor._language == apEditor.LANGUAGE.Korean)
					{
						strColorSpace_Body += "\n" + _editor.GetTextFormat(TEXT.BakeValidation_PleaseSetOption, _editor.GetUIWord(UIWORD.ColorSpace), "Linear", "");
					}
					else
					{
						strColorSpace_Body += "\n" + _editor.GetTextFormat(TEXT.BakeValidation_PleaseSetOption, _editor.GetText(TEXT.RenderPipeline), "Linear");
					}
					break;

				case apEditorUtil.CHECK_ENV_COLOR_SPACE.NeedChangeToGamma:
					//strColorSpace_Body = "현재 색상 공간 (" + _validCurProjectColorSpace.ToString() + ")과 설정이 맞지 않습니다.\n" +
					//					"Bake의 Settings 탭에서 [Color Space]을 [Gamma]로 변경해주세요.";

					strColorSpace_Body = _editor.GetTextFormat(TEXT.BakeValidation_ColorSpace_Failed, "Gamma");

					if(_editor._language == apEditor.LANGUAGE.Korean)
					{
						strColorSpace_Body += "\n" + _editor.GetTextFormat(TEXT.BakeValidation_PleaseSetOption, _editor.GetUIWord(UIWORD.ColorSpace), "Gamma", "");
					}
					else
					{
						strColorSpace_Body += "\n" + _editor.GetTextFormat(TEXT.BakeValidation_PleaseSetOption, _editor.GetText(TEXT.RenderPipeline), "Gamma");
					}
					break;
			}

			DrawInfo(	isValid_ColorSpace, 
						strColorSpace_Title,
						strColorSpace_Body,
						width_InScroll,
						new ButtonActionInfo(_editor.GetText(TEXT.FixNow), BUTTON_TYPE.Fix, !isValid_ColorSpace, OnAction_ColorSpace_Fix),//"고치기"
						new ButtonActionInfo(_editor.GetUIWord(UIWORD.ColorSpace), BUTTON_TYPE.Manual, true, OnAction_ColorSpace_Manual));//"색상 공간"


			GUILayout.Space(10);
			apEditorUtil.GUI_DelimeterBoxH(width_InScroll - 10);
			GUILayout.Space(10);


			//2-4. 카메라
			bool isValid_Camera = _validResult_Camera == apEditorUtil.CHECK_ENV_CAMERA.None;
			string strCamera_Title = _editor.GetText(TEXT.Camera);//"카메라"
			string strCamera_Body = "";
			switch (_validResult_Camera)
			{
				case apEditorUtil.CHECK_ENV_CAMERA.None:
					{
						//strCamera_Body = "씬에 배치된 카메라의 개수에 맞게 옵션이 설정되어 있습니다.";
						strCamera_Body = _editor.GetText(TEXT.BakeValidation_Camera_Pass);
						if(_validCurNumCameras > 1)
						{
							//strCamera_Body += "\n카메라가 2개 이상일 경우 클리핑 마스크의 품질이 낮아질 수 있습니다. 메뉴얼을 참고하세요.";
							strCamera_Body += "\n" + _editor.GetText(TEXT.BakeValidation_Camera_MultiClipped);
						}
						//strCamera_Body += "\n만약 VR 환경이라면 [VR/Multi-Camera]를 [Single Camera and Eye Textures (Unity VR)]로 설정하세요.";
						strCamera_Body += "\n" + _editor.GetText(TEXT.BakeValidation_Camera_VR);
						
					}
					break;

				case apEditorUtil.CHECK_ENV_CAMERA.RecommendToMultiCamera:
					{
						//strCamera_Body = "캐릭터를 렌더링하는 카메라가 " + _validCurNumCameras + "개 배치되어 있습니다.\n"
						//				+ "Bake의 Settings 탭에서 [VR/Multi-Camera]를 [Multiple Camera]로 설정하는 것을 추천합니다.";

						strCamera_Body = _editor.GetTextFormat(TEXT.BakeValidation_Camera_Failed, _validCurNumCameras);
						strCamera_Body += "\n" + _editor.GetTextFormat(TEXT.BakeValidation_Camera_FailedSolution, _editor.GetText(TEXT.VROption), "Multiple Cameras");
					}
					break;
			}

			DrawInfo(isValid_Camera,
						strCamera_Title,
						strCamera_Body,
						width_InScroll,
						new ButtonActionInfo(_editor.GetText(TEXT.FixNow), BUTTON_TYPE.Fix, !isValid_Camera, OnAction_Camera_Fix),//"고치기"
						new ButtonActionInfo(_editor.GetText(TEXT.MultipleCameras), BUTTON_TYPE.Manual, true, OnAction_Camera_MultipleCamera),//"다중 카메라"
						new ButtonActionInfo("VR", BUTTON_TYPE.Manual, true, OnAction_Camera_VR)
						);


			GUILayout.Space(Mathf.Max(height_Scroll - 100, 10));

			EditorGUILayout.EndVertical();
			EditorGUILayout.EndScrollView();//스크롤 끝

			GUILayout.Space(20);

			EditorGUILayout.BeginHorizontal(GUILayout.Width(width));
			GUILayout.Space(2);
			EditorGUILayout.LabelField(_editor.GetText(TEXT.AlwaysCheckBakeOption), GUILayout.Width(width - 40));//"Bake시 항상 검사하기"
			EditorGUI.BeginChangeCheck();
			bool nextValidate = EditorGUILayout.Toggle(_editor._option_ValidateEnvironmentWhenBake, GUILayout.Width(30));
			if(EditorGUI.EndChangeCheck())
			{
				if(nextValidate != _editor._option_ValidateEnvironmentWhenBake)
				{
					_editor._option_ValidateEnvironmentWhenBake = nextValidate;
					_editor.SaveEditorPref();
				}
			}
			EditorGUILayout.EndHorizontal();
			

			if(GUILayout.Button(_editor.GetText(TEXT.Close), GUILayout.Height(25)))
			{
				isClose = true;
			}


			if(isClose)
			{
				CloseDialog();
			}

		}





		// Draw 항목
		//---------------------------------------------------------------------
		private delegate void ON_BUTTON_CLICK();

		private enum BUTTON_TYPE
		{
			Fix, Manual, OpenMatLib
		}
		private struct ButtonActionInfo
		{
			public string _strButton;
			public ON_BUTTON_CLICK _buttonEvent;
			public bool _isShown;
			public BUTTON_TYPE _type;


			public ButtonActionInfo(string strButton, BUTTON_TYPE btnType, bool isShown, ON_BUTTON_CLICK buttonEvent)
			{
				_strButton = strButton;
				_buttonEvent = buttonEvent;
				_type = btnType;
				_isShown = isShown;
			}
		}


		private void DrawInfo(	bool isValidated, 
								string str_Title,
								string str_Body,
								int width_Item,								
								params ButtonActionInfo[] buttons)
		{
			int height_Item = 50;
			int width_Icon = 40;
			int width_ActionButton = 180;
			int width_Text = width_Item - (width_Icon + width_ActionButton + 20 + 6);

			EditorGUILayout.BeginHorizontal(GUILayout.Width(width_Item));

			GUILayout.Space(5);

			//1열 - 아이콘
			EditorGUILayout.BeginVertical(GUILayout.Width(width_Icon));
			
			//아이콘을 그리자
			if (isValidated)
			{
				//이 항목이 유효하다면
				EditorGUILayout.LabelField(_guiContent_Img_Okay.Content, _guiStyle_CenterIcon, GUILayout.Width(width_Icon), GUILayout.Height(height_Item));
			}
			else
			{
				//이 항목이 유효하지 않다면
				EditorGUILayout.LabelField(_guiContent_Img_Warning.Content, _guiStyle_CenterIcon, GUILayout.Width(width_Icon), GUILayout.Height(height_Item));
			}
			EditorGUILayout.EndVertical();

			GUILayout.Space(2);

			//2열 - 텍스트
			EditorGUILayout.BeginVertical(GUILayout.Width(width_Text));
			EditorGUILayout.LabelField(str_Title, _guiStyle_Text_Title, GUILayout.Width(width_Text), GUILayout.Height(22));

			

			EditorGUILayout.LabelField(str_Body, _guiStyle_Text_Body, GUILayout.Width(width_Text));
			EditorGUILayout.EndVertical();

			GUILayout.Space(2);

			//3열 - 버튼들
			EditorGUILayout.BeginVertical(GUILayout.Width(width_ActionButton));

			//지금 고치기 버튼
			int width_Button = width_ActionButton - 4;
			int height_Button_Fix = 28;
			int height_Button_Manual = 24;

			bool isManualSpace = false;
			bool isAnyFixButton = false;

			Color prevColor = GUI.backgroundColor;

			int nButtons = buttons != null ? buttons.Length : 0;
			if(nButtons > 0)
			{
				for (int i = 0; i < nButtons; i++)
				{
					ButtonActionInfo btnInfo = buttons[i];

					if(!btnInfo._isShown)
					{
						continue;
					}

					if(btnInfo._type == BUTTON_TYPE.Manual)
					{
						if(isAnyFixButton && !isManualSpace)
						{
							//여백을 주자
							isManualSpace = true;

							GUILayout.Space(5);
						}
					}
					else
					{
						isAnyFixButton = true;
					}

					int curHeight = btnInfo._type != BUTTON_TYPE.Manual ? height_Button_Fix : height_Button_Manual;
					apGUIContentWrapper curGUIContent = null;

					if(btnInfo._type == BUTTON_TYPE.Fix)
					{
						curGUIContent = _guiContent_Button_Fix;
						GUI.backgroundColor = new Color(1.5f, 0.9f, 0.9f, 1.0f);
					}
					else if (btnInfo._type == BUTTON_TYPE.Manual)
					{
						curGUIContent = _guiContent_Button_Manual;
					}
					else
					{
						curGUIContent = _guiContent_Button_OpenMatLib;
						if(isValidated)
						{
							GUI.backgroundColor = new Color(0.9f, 1.2f, 1.5f, 1.0f);
						}
						else
						{
							GUI.backgroundColor = new Color(1.5f, 0.9f, 0.9f, 1.0f);
						}
							
					}

					curGUIContent.ClearText(false);
					curGUIContent.AppendSpaceText(1, false);
					curGUIContent.AppendText(btnInfo._strButton, true);



					if(GUILayout.Button(curGUIContent.Content, GUILayout.Width(width_Button), GUILayout.Height(curHeight)))
					{
						//버튼 이벤트를 호출한다.
						if (btnInfo._buttonEvent != null)
						{
							btnInfo._buttonEvent();
						}
					}

					GUI.backgroundColor = prevColor;
				}
			}

			EditorGUILayout.EndVertical();

			EditorGUILayout.EndHorizontal();
		}

		


		// Functions
		//---------------------------------------------------------------------
		private void ValidateOptions()
		{
			//유효성 테스트를 하자
			apEditorUtil.ValidateBakeOptionOnProject(_portrait, _editor,
					out _validResult_RP,
					out _validResult_Mat,
					out _validResult_ColorSpace,
					out _validResult_Camera,
					out _validCurProjectRP,
					out _validCurProjectColorSpace,
					out _validCurNumCameras);

			_isAnyValidated = true;
		}

		// 버튼 이벤트
		//---------------------------------------------------------------------
		//렌더 파이프라인 - 고치기
		private void OnAction_RenderPipeline_Fix()
		{
			//다시 검사를 1회 한 후
			ValidateOptions();

			//현재 파이프라인에 맞게 변경한다.
			if(_validCurProjectRP == apEditorUtil.RENDER_PIPELINE_ENV_RESULT.BuiltIn)
			{
				_editor.ProjectSettingData.SetUseSRP(false);
			}
			else if(_validCurProjectRP == apEditorUtil.RENDER_PIPELINE_ENV_RESULT.URP)
			{
				_editor.ProjectSettingData.SetUseSRP(true);
			}

			//검사를 하고 갱신을 한다.
			ValidateOptions();

			Repaint();
		}

		//렌더 파이프라인 - 메뉴얼보기 (Bake Dialog)
		private void OnAction_RenderPipeline_Manual_BakeDialog()
		{
			if(_editor._language == apEditor.LANGUAGE.Korean)
			{
				Application.OpenURL("https://rainyrizzle.github.io/kr/AdvancedManual/AD_BakeDialog.html");
			}
			else if(_editor._language == apEditor.LANGUAGE.Japanese)
			{
				Application.OpenURL("https://rainyrizzle.github.io/jp/AdvancedManual/AD_BakeDialog.html");
			}
			else
			{
				Application.OpenURL("https://rainyrizzle.github.io/en/AdvancedManual/AD_BakeDialog.html");
			}
		}

		//렌더 파이프라인 - 메뉴얼보기 (URP)
		private void OnAction_RenderPipeline_Manual_URP()
		{
			if(_editor._language == apEditor.LANGUAGE.Korean)
			{
				Application.OpenURL("https://rainyrizzle.github.io/kr/AdvancedManual/AD_URP.html");
			}
			else if(_editor._language == apEditor.LANGUAGE.Japanese)
			{
				Application.OpenURL("https://rainyrizzle.github.io/jp/AdvancedManual/AD_URP.html");
			}
			else
			{
				Application.OpenURL("https://rainyrizzle.github.io/en/AdvancedManual/AD_URP.html");
			}
		}


		//쉐이더 - 재질 라이브러리 열기
		private void OnAction_Shaders_OpenMaterialLibrary()
		{
			apDialog_MaterialLibrary.ShowDialog(_editor, _portrait);
		}

		//쉐이더 - 메뉴얼보기 (재질 라이브러리)
		private void OnAction_Shaders_Manual_MatLibrary()
		{
			if(_editor._language == apEditor.LANGUAGE.Korean)
			{
				Application.OpenURL("https://rainyrizzle.github.io/kr/AdvancedManual/AD_MaterialLibrary.html");
			}
			else if(_editor._language == apEditor.LANGUAGE.Japanese)
			{
				Application.OpenURL("https://rainyrizzle.github.io/jp/AdvancedManual/AD_MaterialLibrary.html");
			}
			else
			{
				Application.OpenURL("https://rainyrizzle.github.io/en/AdvancedManual/AD_MaterialLibrary.html");
			}
		}

		//쉐이더 - 메뉴얼보기 (URP)
		private void OnAction_Shaders_Manual_URP()
		{
			if(_editor._language == apEditor.LANGUAGE.Korean)
			{
				Application.OpenURL("https://rainyrizzle.github.io/kr/AdvancedManual/AD_URP.html");
			}
			else if(_editor._language == apEditor.LANGUAGE.Japanese)
			{
				Application.OpenURL("https://rainyrizzle.github.io/jp/AdvancedManual/AD_URP.html");
			}
			else
			{
				Application.OpenURL("https://rainyrizzle.github.io/en/AdvancedManual/AD_URP.html");
			}
		}

		//색상 공간 - 고치기
		private void OnAction_ColorSpace_Fix()
		{
			//다시 검사를 1회 한 후
			ValidateOptions();

			//현재 색상 공간에 맞게 변경한다.
			if(_validCurProjectColorSpace == ColorSpace.Gamma)
			{
				_editor.ProjectSettingData.SetColorSpaceGamma(true);
			}
			else if(_validCurProjectColorSpace == ColorSpace.Linear)
			{
				_editor.ProjectSettingData.SetColorSpaceGamma(false);
			}

			//검사를 하고 갱신을 한다.
			ValidateOptions();

			Repaint();
		}


		//색상 공간 - 메뉴얼보기
		private void OnAction_ColorSpace_Manual()
		{
			if(_editor._language == apEditor.LANGUAGE.Korean)
			{
				Application.OpenURL("https://rainyrizzle.github.io/kr/AdvancedManual/AD_LinearSpace.html");
			}
			else if(_editor._language == apEditor.LANGUAGE.Japanese)
			{
				Application.OpenURL("https://rainyrizzle.github.io/jp/AdvancedManual/AD_LinearSpace.html");
			}
			else
			{
				Application.OpenURL("https://rainyrizzle.github.io/en/AdvancedManual/AD_LinearSpace.html");
			}
		}

		//카메라 - 고치기 (다중 카메라)
		private void OnAction_Camera_Fix()
		{
			//다시 검사를 1회 한 후
			ValidateOptions();

			//카메라가 2개 이상인 경우
			if(_validCurNumCameras > 1)
			{
				apEditorUtil.SetRecord_Portrait(	apUndoGroupData.ACTION.Portrait_SettingChanged,
													_editor, _portrait,
													false, 
													apEditorUtil.UNDO_STRUCT.ValueOnly);

				_portrait._vrSupportMode = apPortrait.VR_SUPPORT_MODE.MultiCamera;
			}

			//검사를 하고 갱신을 한다.
			ValidateOptions();

			Repaint();
		}

		//카메라 - 메뉴얼 (여러개의 카메라)
		private void OnAction_Camera_MultipleCamera()
		{
			if(_editor._language == apEditor.LANGUAGE.Korean)
			{
				Application.OpenURL("https://rainyrizzle.github.io/kr/AdvancedManual/AD_MultipleCameras.html");
			}
			else if(_editor._language == apEditor.LANGUAGE.Japanese)
			{
				Application.OpenURL("https://rainyrizzle.github.io/jp/AdvancedManual/AD_MultipleCameras.html");
			}
			else
			{
				Application.OpenURL("https://rainyrizzle.github.io/en/AdvancedManual/AD_MultipleCameras.html");
			}
		}

		//카메라 - 메뉴얼 (VR)
		private void OnAction_Camera_VR()
		{
			if(_editor._language == apEditor.LANGUAGE.Korean)
			{
				Application.OpenURL("https://rainyrizzle.github.io/kr/AdvancedManual/AD_VR.html");
			}
			else if(_editor._language == apEditor.LANGUAGE.Japanese)
			{
				Application.OpenURL("https://rainyrizzle.github.io/jp/AdvancedManual/AD_VR.html");
			}
			else
			{
				Application.OpenURL("https://rainyrizzle.github.io/en/AdvancedManual/AD_VR.html");
			}
		}
	}
}