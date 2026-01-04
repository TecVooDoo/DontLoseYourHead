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
	public class apDialog_SelectShaderProp : EditorWindow
	{
		// Members
		//--------------------------------------------------------------
		public delegate void FUNC_SELECT_SHADER_PROP(bool isSuccess, object loadKey, List<PropInfo> props, apMaterialSet calledMaterialSet);

		private static apDialog_SelectShaderProp s_window = null;

		private apEditor _editor = null;
		private object _loadKey = null;
		private FUNC_SELECT_SHADER_PROP _funcResult = null;

		private apMaterialSet _matSet = null;

		//프로퍼티 정보
		public class PropInfo
		{
			public string _name = "";
			public apMaterialSet.SHADER_PROP_TYPE _type = apMaterialSet.SHADER_PROP_TYPE.Int;

			public PropInfo(string name, apMaterialSet.SHADER_PROP_TYPE type)
			{
				_name = name;
				_type = type;
			}
		}

		private List<PropInfo> _propInfos = null;
		private List<PropInfo> _selectedInfos = null;

		private Vector2 _scrollList = new Vector2();		

		// Show Window
		//--------------------------------------------------------------
		/// <summary>
		/// 다이얼로그 열기 : 재질 라이브러리에서 프로퍼티를 추가하는 경우
		/// </summary>
		public static object ShowDialog_OnMaterialLibrary(apEditor editor, apMaterialSet materialSet, FUNC_SELECT_SHADER_PROP funcResult)
		{
			CloseDialog();

			if (editor == null || editor._portrait == null || editor._portrait._controller == null)
			{
				return null;
			}

			EditorWindow curWindow = EditorWindow.GetWindow(typeof(apDialog_SelectShaderProp), true, "Select Property", true);
			apDialog_SelectShaderProp curTool = curWindow as apDialog_SelectShaderProp;

			object loadKey = new object();
			if (curTool != null && curTool != s_window)
			{
				int width = 350;
				int height = 400;
				s_window = curTool;
				s_window.position = new Rect((editor.position.xMin + editor.position.xMax) / 2 - (width / 2),
												(editor.position.yMin + editor.position.yMax) / 2 - (height / 2),
												width, height);
				s_window.Init_OnMaterialLibrary(editor, loadKey, materialSet, funcResult);

				return loadKey;
			}
			else
			{
				return null;
			}
		}


		/// <summary>
		/// 다이얼로그 열기 : 메시의 마스크 설정 다이얼로그에서 열기 (키워드가 제외된다.)
		/// </summary>
		/// <param name="editor"></param>
		/// <param name="targetMeshTFs">마스크의 대상이 되는 메시들. Child MeshTF 리스트가 포함된다.</param>
		/// <param name="funcResult"></param>
		/// <returns></returns>
		public static object ShowDialog_OnMaskDialog(apEditor editor, List<apTransform_Mesh> targetMeshTFs, apMaterialSet defaultMatSet, FUNC_SELECT_SHADER_PROP funcResult)
		{
			CloseDialog();

			if (editor == null || editor._portrait == null || editor._portrait._controller == null)
			{
				return null;
			}

			EditorWindow curWindow = EditorWindow.GetWindow(typeof(apDialog_SelectShaderProp), true, "Select Property", true);
			apDialog_SelectShaderProp curTool = curWindow as apDialog_SelectShaderProp;

			object loadKey = new object();
			if (curTool != null && curTool != s_window)
			{
				int width = 350;
				int height = 400;
				s_window = curTool;
				s_window.position = new Rect((editor.position.xMin + editor.position.xMax) / 2 - (width / 2),
												(editor.position.yMin + editor.position.yMax) / 2 - (height / 2),
												width, height);
				s_window.Init_OnMaskDialog(editor, loadKey, targetMeshTFs, defaultMatSet, funcResult);

				return loadKey;
			}
			else
			{
				return null;
			}
		}




		private static void CloseDialog()
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

		// Init
		//--------------------------------------------------------------
		public void Init_OnMaterialLibrary(apEditor editor, object loadKey, apMaterialSet materialSet, FUNC_SELECT_SHADER_PROP funcResult)
		{
			_editor = editor;
			_loadKey = loadKey;
			_funcResult = funcResult;

			_matSet = materialSet;

			_scrollList = Vector2.zero;

			

			HashSet<string> propNames = new HashSet<string>();
			_propInfos = new List<PropInfo>();
			_selectedInfos = new List<PropInfo>();

			//기존에 추가된 프로퍼티들은 보여주지 말자
			int nMatSetProps = _matSet._propertySets != null ? _matSet._propertySets.Count : 0;
			if(nMatSetProps > 0)
			{
				apMaterialSet.PropertySet propSet = null;
				for (int i = 0; i < nMatSetProps; i++)
				{
					propSet = _matSet._propertySets[i];
					
					if(!propNames.Contains(propSet._name))
					{
						propNames.Add(propSet._name);
					}
				}
			}

			//재질 세트로부터 프로퍼티들을 수집하자 (키워드 포함)
			FindPropertiesFromMaterialSet(_matSet, true, propNames);
		}


		//커스텀 쉐이더를 사용하는 경우 (Mat Set은 null)
		public void Init_OnMaskDialog(apEditor editor, object loadKey, List<apTransform_Mesh> meshTFs, apMaterialSet defaultMatSet, FUNC_SELECT_SHADER_PROP funcResult)
		{
			_editor = editor;
			_loadKey = loadKey;
			_funcResult = funcResult;

			_matSet = defaultMatSet;//기본 세트를 입력하자

			_scrollList = Vector2.zero;

			HashSet<string> propNames = new HashSet<string>();
			_propInfos = new List<PropInfo>();
			_selectedInfos = new List<PropInfo>();

			bool isDefaultMatSetAdded = false;

			int nMeshTFs = meshTFs != null ? meshTFs.Count : 0;
			if(nMeshTFs > 0)
			{
				apTransform_Mesh curTF = null;
				for (int iMeshTF = 0; iMeshTF < nMeshTFs; iMeshTF++)
				{
					curTF = meshTFs[iMeshTF];
					if(curTF == null)
					{
						continue;
					}

					if(curTF._isCustomShader)
					{
						//1. 커스텀 쉐이더를 사용하는 경우
						if(curTF._customShader != null)
						{
							FindProperties(curTF._customShader, _propInfos, propNames);
						}
					}
					else
					{
						//2. 재질 세트를 이용하는 경우
						if(curTF._isUseDefaultMaterialSet)
						{
							//2-1. 기본 재질 세트를 이용하는 경우
							if(!isDefaultMatSetAdded)
							{
								//기본 재질 세트는 한번만 처리하면 된다.
								isDefaultMatSetAdded = true;

								//기본 세트로부터 프로퍼티를 찾는다. 키워드는 제외
								FindPropertiesFromMaterialSet(defaultMatSet, false, propNames);
							}
						}
						else
						{
							//2-2. 별도의 재질 세트를 이용하는 경우
							if(curTF._linkedMaterialSet != null)
							{
								FindPropertiesFromMaterialSet(curTF._linkedMaterialSet, false, propNames);
							}
						}
					}
				}
			}

			//추가된 프로퍼티 개수가 0개이고 기본 재질 세트가 입력이 되지 않았다면
			int nAddedPropInfos = _propInfos != null ? _propInfos.Count : 0;
			if(nAddedPropInfos == 0 && !isDefaultMatSetAdded)
			{
				//기본 재질 세트의 값들을 입력한다.
				FindPropertiesFromMaterialSet(defaultMatSet, false, propNames);
			}

		}



		private void FindPropertiesFromMaterialSet(apMaterialSet matSet, bool isKeyAddable, HashSet<string> propNames)
		{
			//이제 쉐이더별로 프로퍼티를 찾아서 등록하자
			FindProperties(matSet._shader_Normal_AlphaBlend, _propInfos, propNames);
			FindProperties(matSet._shader_Normal_Additive, _propInfos, propNames);
			FindProperties(matSet._shader_Normal_SoftAdditive, _propInfos, propNames);
			FindProperties(matSet._shader_Normal_Multiplicative, _propInfos, propNames);
			FindProperties(matSet._shader_Clipped_AlphaBlend, _propInfos, propNames);
			FindProperties(matSet._shader_Clipped_Additive, _propInfos, propNames);
			FindProperties(matSet._shader_Clipped_SoftAdditive, _propInfos, propNames);
			FindProperties(matSet._shader_Clipped_Multiplicative, _propInfos, propNames);
			FindProperties(matSet._shader_L_Normal_AlphaBlend, _propInfos, propNames);
			FindProperties(matSet._shader_L_Normal_Additive, _propInfos, propNames);
			FindProperties(matSet._shader_L_Normal_SoftAdditive, _propInfos, propNames);
			FindProperties(matSet._shader_L_Normal_Multiplicative, _propInfos, propNames);
			FindProperties(matSet._shader_L_Clipped_AlphaBlend, _propInfos, propNames);
			FindProperties(matSet._shader_L_Clipped_Additive, _propInfos, propNames);
			FindProperties(matSet._shader_L_Clipped_SoftAdditive, _propInfos, propNames);
			FindProperties(matSet._shader_L_Clipped_Multiplicative, _propInfos, propNames);
			FindProperties(matSet._shader_AlphaMask, _propInfos, propNames);

#if UNITY_2021_2_OR_NEWER
			if (isKeyAddable)
			{
				//프로퍼티에 이어서 키워드도 추가
				FindKeywords(matSet._shader_Normal_AlphaBlend, _propInfos, propNames);
				FindKeywords(matSet._shader_Normal_Additive, _propInfos, propNames);
				FindKeywords(matSet._shader_Normal_SoftAdditive, _propInfos, propNames);
				FindKeywords(matSet._shader_Normal_Multiplicative, _propInfos, propNames);
				FindKeywords(matSet._shader_Clipped_AlphaBlend, _propInfos, propNames);
				FindKeywords(matSet._shader_Clipped_Additive, _propInfos, propNames);
				FindKeywords(matSet._shader_Clipped_SoftAdditive, _propInfos, propNames);
				FindKeywords(matSet._shader_Clipped_Multiplicative, _propInfos, propNames);
				FindKeywords(matSet._shader_L_Normal_AlphaBlend, _propInfos, propNames);
				FindKeywords(matSet._shader_L_Normal_Additive, _propInfos, propNames);
				FindKeywords(matSet._shader_L_Normal_SoftAdditive, _propInfos, propNames);
				FindKeywords(matSet._shader_L_Normal_Multiplicative, _propInfos, propNames);
				FindKeywords(matSet._shader_L_Clipped_AlphaBlend, _propInfos, propNames);
				FindKeywords(matSet._shader_L_Clipped_Additive, _propInfos, propNames);
				FindKeywords(matSet._shader_L_Clipped_SoftAdditive, _propInfos, propNames);
				FindKeywords(matSet._shader_L_Clipped_Multiplicative, _propInfos, propNames);
				FindKeywords(matSet._shader_AlphaMask, _propInfos, propNames);
			}
#endif
		}



		private void FindProperties(Shader shader, List<PropInfo> result, HashSet<string> propNames)
		{
			if(shader == null)
			{
				return;
			}

			
#if UNITY_6000_2_OR_NEWER
			//v1.6.2 : ShaderUtil.GetPropertyCount가 Deprecated 되었다.
			int nProps = shader.GetPropertyCount();
#else
			int nProps = ShaderUtil.GetPropertyCount(shader);
#endif
			
			if(nProps == 0)
			{
				return;
			}

			for (int i = 0; i < nProps; i++)
			{	
#if UNITY_6000_2_OR_NEWER
				//v1.6.2 : ShaderUtil.GetPropertyName이 Deprecated 되었다.
				string strPropName = shader.GetPropertyName(i);
#else
				string strPropName = ShaderUtil.GetPropertyName(shader, i);
#endif
				
				apMaterialSet.SHADER_PROP_TYPE propType = apMaterialSet.SHADER_PROP_TYPE.Float;

#if UNITY_6000_2_OR_NEWER
				//v1.6.2 : ShaderUtil.GetPropertyType이 Deprecated 되었다.
				switch (shader.GetPropertyType(i))
#else
				switch (ShaderUtil.GetPropertyType(shader, i))
#endif
				
				{

					// [ Int 타입 ]
#if UNITY_2021_1_OR_NEWER
#if UNITY_6000_2_OR_NEWER
					case UnityEngine.Rendering.ShaderPropertyType.Int:
#else
					case ShaderUtil.ShaderPropertyType.Int:
#endif
						propType = apMaterialSet.SHADER_PROP_TYPE.Int;
						break;
#endif

					// [ Float 타입 ]
#if UNITY_6000_2_OR_NEWER
					case UnityEngine.Rendering.ShaderPropertyType.Float:
					case UnityEngine.Rendering.ShaderPropertyType.Range:
#else
					case ShaderUtil.ShaderPropertyType.Float:
					case ShaderUtil.ShaderPropertyType.Range:
#endif
						propType = apMaterialSet.SHADER_PROP_TYPE.Float;
						break;

					// [ Texture 타입 ]
#if UNITY_6000_2_OR_NEWER
					case UnityEngine.Rendering.ShaderPropertyType.Texture:
#else
					case ShaderUtil.ShaderPropertyType.TexEnv:
#endif
						propType = apMaterialSet.SHADER_PROP_TYPE.Texture;
						break;

					// [ Vector 타입 ]
#if UNITY_6000_2_OR_NEWER
					case UnityEngine.Rendering.ShaderPropertyType.Vector:
#else
					case ShaderUtil.ShaderPropertyType.Vector:
#endif
						propType = apMaterialSet.SHADER_PROP_TYPE.Vector;
						break;

					// [ Color 타입 ]
#if UNITY_6000_2_OR_NEWER
						case UnityEngine.Rendering.ShaderPropertyType.Color:
#else
					case ShaderUtil.ShaderPropertyType.Color:
#endif
						propType = apMaterialSet.SHADER_PROP_TYPE.Color;
						break;
				}

				if(propNames.Contains(strPropName))
				{
					//이미 등록되었다면
					continue;
				}

				bool isValid = IsValidPropName(strPropName);
				if(!isValid)
				{
					//예약된 프로퍼티는 제외한다.
					//continue;
				}

				//리스트에 추가
				result.Add(new PropInfo(strPropName, propType));

				propNames.Add(strPropName);
			}
		}

		//특정 프로퍼티는 선택이 불가능하다
		private bool IsValidPropName(string propName)
		{
			if(string.Equals(propName, "_Color")) { return false; }
			if(string.Equals(propName, "_MainTex")) { return false; }
			if(string.Equals(propName, "_MaskTex")) { return false; }
			if(string.Equals(propName, "_MaskScreenSpaceOffset")) { return false; }

			return true;
		}



#if UNITY_2021_2_OR_NEWER
		private void FindKeywords(Shader shader, List<PropInfo> result, HashSet<string> propNames)
		{
			if(shader == null)
			{
				return;
			}

			string[] keywords = shader.keywordSpace.keywordNames;
			int nKeywords = keywords != null ? keywords.Length : 0;
			if(nKeywords == 0)
			{
				return;
			}

			for (int i = 0; i < nKeywords; i++)
			{
				string strKeyword = keywords[i];

				if(propNames.Contains(strKeyword))
				{
					//이미 등록되었다면
					continue;
				}

				//리스트에 추가 (키워드 타입으로)
				result.Add(new PropInfo(strKeyword, apMaterialSet.SHADER_PROP_TYPE.Keyword));

				propNames.Add(strKeyword);
			}
		}
#endif
		// GUI
		//--------------------------------------------------------------
		void OnGUI()
		{
			int width = (int)position.width;
			int height = (int)position.height;
			if (_editor == null || _funcResult == null)
			{
				return;
			}

			Color prevColor = GUI.backgroundColor;
			GUI.backgroundColor = new Color(0.9f, 0.9f, 0.9f);
			GUI.Box(new Rect(0, 35, width, height - 90), "");
			GUI.backgroundColor = prevColor;

			EditorGUILayout.BeginVertical();

			Texture2D iconImageCategory = _editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_FoldDown);

			GUIStyle guiStyle_None = new GUIStyle(GUIStyle.none);
			guiStyle_None.normal.textColor = GUI.skin.label.normal.textColor;
			guiStyle_None.alignment = TextAnchor.MiddleLeft;

			GUIStyle guiStyle_Selected = new GUIStyle(GUIStyle.none);
			if(EditorGUIUtility.isProSkin)
			{
				guiStyle_Selected.normal.textColor = Color.cyan;
			}
			else
			{
				guiStyle_Selected.normal.textColor = Color.white;
			}
			guiStyle_Selected.alignment = TextAnchor.MiddleLeft;

			GUIStyle guiStyle_Center = new GUIStyle(GUIStyle.none);
			guiStyle_Center.normal.textColor = GUI.skin.label.normal.textColor;
			guiStyle_Center.alignment = TextAnchor.MiddleCenter;

			GUILayout.Space(10);
			GUILayout.Button(_editor.GetText(TEXT.SelectPropertiesToAdd), guiStyle_Center, GUILayout.Width(width), GUILayout.Height(15));//<투명 버튼//"Select Control Param"
			GUILayout.Space(10);

//Ctrl키나 Shift키를 누르면 여러개를 선택할 수 있다.
			bool isCtrlOrShift = false;

			if(Event.current.shift
#if UNITY_EDITOR_OSX
				|| Event.current.command
#else
				|| Event.current.control
#endif
				)
			{
				isCtrlOrShift = true;
			}

			_scrollList = EditorGUILayout.BeginScrollView(_scrollList, GUILayout.Width(width), GUILayout.Height(height - 90));

			
			GUILayout.Button(new GUIContent(_editor.GetText(TEXT.Properties), iconImageCategory), guiStyle_None, GUILayout.Height(20));//<투명 버튼

			int nPropInfos = _propInfos != null ? _propInfos.Count : 0;
			if(nPropInfos > 0)
			{
				PropInfo curInfo = null;
				for (int i = 0; i < nPropInfos; i++)
				{
					curInfo = _propInfos[i];

					GUIStyle curGUIStyle = guiStyle_None;
					bool isSelected = _selectedInfos.Contains(curInfo);
					if (isSelected)
					{
						Rect lastRect = GUILayoutUtility.GetLastRect();

						//변경 v1.4.2
						apEditorUtil.DrawListUnitBG(lastRect.x + 1, lastRect.y + 20, width - 2, 20, apEditorUtil.UNIT_BG_STYLE.Main);

						curGUIStyle = guiStyle_Selected;
					}

					EditorGUILayout.BeginHorizontal(GUILayout.Width(width - 50));
					GUILayout.Space(15);
					if (GUILayout.Button(new GUIContent(" " + curInfo._name + " (" + curInfo._type.ToString() + ")"), curGUIStyle, GUILayout.Width(width - 35), GUILayout.Height(20)))
					{
						if(!isCtrlOrShift)
						{
							_selectedInfos.Clear();							
						}

						if(isSelected)
						{
							_selectedInfos.Remove(curInfo);
						}
						else
						{
							_selectedInfos.Add(curInfo);
						}
					}

					EditorGUILayout.EndHorizontal();
				}
			}
			

			GUILayout.Space(height);

			EditorGUILayout.EndScrollView();

			EditorGUILayout.EndVertical();

			GUILayout.Space(10);
			EditorGUILayout.BeginHorizontal();
			bool isClose = false;
			if (GUILayout.Button(_editor.GetText(TEXT.DLG_Select), GUILayout.Height(30)))//"Select"
			{
				int nSelected = _selectedInfos != null ? _selectedInfos.Count : 0;
				if(nSelected > 0)
				{
					_funcResult(true, _loadKey, _selectedInfos, _matSet);
				}
				else
				{
					_funcResult(false, _loadKey, null, _matSet);
				}
				
				isClose = true;
			}
			if (GUILayout.Button(_editor.GetText(TEXT.DLG_Close), GUILayout.Height(30)))//"Close"
			{
				isClose = true;
			}
			EditorGUILayout.EndHorizontal();

			if (isClose)
			{
				CloseDialog();
			}


		}
	}
}