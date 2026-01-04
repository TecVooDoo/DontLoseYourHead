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
	public class apDialog_SendMaskData : EditorWindow
	{
		// Members
		//--------------------------------------------------------------
		//콜백은 없다.

		private static apDialog_SendMaskData s_window = null;

		//대상
		private apEditor _editor = null;
		private apPortrait _portrait = null;
		//private apTransform_Mesh _meshTF = null;//이건 단일 객체의 마스크만 가져오기
		private apMeshGroup _rootMeshGroup = null;

		//전체 리스트
		private Vector2 _scroll_DataSet = new Vector2();
		private Vector2 _scroll_Targets = new Vector2();
		private Vector2 _scroll_Properties = new Vector2();
		private Vector2 _scroll_MaskCopyProps = new Vector2();

		//private apSendMaskData _selectedSendMaskData = null;

		private GUIStyle _guiStyle_None = null;
		private GUIStyle _guiStyle_LabelCenter = null;
		private GUIStyle _guiStyle_Selected = null;
		private GUIStyle _guiStyle_CenterBox = null;
		private GUIStyle _guiStyle_ListIcon = null;
		private GUIStyle _guiStyle_None_Invalid = null;
		//private GUIStyle _guiStyle_LeftBox = null;

		private GUIStyle _guiStyle_RemoveBtn = null;

		private Texture2D _icon_Data_AlphaMask = null;
		private Texture2D _icon_Data_MainTexOnly = null;
		private Texture2D _icon_Data_MainTexWithColor = null;
		private Texture2D _icon_Data_Custom = null;
		private Texture2D _icon_Data_Error = null;
		private Texture2D _icon_Data_Clipping = null;
		private Texture2D _icon_Data_Shared = null;
		private Texture2D _icon_Data_Chained = null;

		private Texture2D _icon_Category = null;
		private Texture2D _icon_Mesh = null;
		private Texture2D _icon_Remove = null;
		private Texture2D _icon_Add = null;

		private apGUIContentWrapper _guiContent_ListLabel = null;
		private apGUIContentWrapper _guiContent_Item_Send = null;
		private apGUIContentWrapper _guiContent_Item_Recv = null;
		private apGUIContentWrapper _guiContent_TargetInfo = null;
		private apGUIContentWrapper _guiContent_ControlParam = null;

		private apGUIContentWrapper _guiContent_AddButton = null;

		private apGUIContentWrapper _guiContent_Icon_None = null;
		private apGUIContentWrapper _guiContent_Icon_Shared = null;
		private apGUIContentWrapper _guiContent_Icon_Shared_Warning = null;
		private apGUIContentWrapper _guiContent_Icon_Chained = null;
		private apGUIContentWrapper _guiContent_Icon_Chained_Warning = null;


		private object _loadKey_SelectTFs = null;
		private object _loadKey_SelectControlParam = null;
		private object _loadKey_SelectShaderProp = null;


		private string[] _propLabel_RTTextureSize = new string[]
		{
			//고정 숫자 크기 (정사각형)
			"64", "128", "256", "512", "1024",
			//화면 비율
			"Full Screen", "Half Screen", "Quarter Screen",
			//해상도 제한 화면 비율
			"FHD Size or Less", "HD Size or Less"
		};
		
		private string[] _propLabel_ShaderPropType = new string[]
		{
			"Render Texture",
			"Screen Space Offset",

			//프리셋 지원 값
			"Mask Operation",

			//자동 생성값 (추가)
			"Mesh Color",
			
			//커스텀 프로퍼티
			"Float",
			"Integer",
			"Vector",
			"Texture",
			"Color",

			//VR용 특수값
			"Render Texture VR EyeLeft",
			"Render Texture VR EyeRight",
		};


		public enum CHAIN_TYPE
		{
			None,
			Chained,
			Warning//정상적으로 Chained가 되지 못한 경우
		}

		public enum SHARED_TYPE
		{
			None,
			Shared,
			Warning,//Shared인데 Phase가 동일하지 않는 경우
		}

		//전체 리스트
		public class SendMaskDataInfo
		{
			public apTransform_Mesh _meshTF = null;
			public apSendMaskData _sendMaskData = null;

			//체인 여부를 여기서 정하자 (Send 기준)
			public CHAIN_TYPE _chainType = CHAIN_TYPE.None;

			//공유된 경우
			public SHARED_TYPE _sharedType = SHARED_TYPE.None;
			public List<SendMaskDataInfo> _linkedSharedInfo = null;
			

			public SendMaskDataInfo(apTransform_Mesh meshTF, apSendMaskData sendMaskData)
			{
				_meshTF = meshTF;
				_sendMaskData = sendMaskData;
				
				_chainType = CHAIN_TYPE.None;
				_sharedType = SHARED_TYPE.None;
				if(_linkedSharedInfo == null)
				{
					_linkedSharedInfo = new List<SendMaskDataInfo>();
				}
				_linkedSharedInfo.Clear();
			}

			public void ClearMetaData()
			{
				_chainType = CHAIN_TYPE.None;
				_sharedType = SHARED_TYPE.None;
				if(_linkedSharedInfo == null)
				{
					_linkedSharedInfo = new List<SendMaskDataInfo>();
				}
				_linkedSharedInfo.Clear();
			}

			public void AddSharedInfo(SendMaskDataInfo sharedInfo)
			{
				if(sharedInfo == this)
				{
					return;
				}

				if(_linkedSharedInfo == null)
				{
					_linkedSharedInfo = new List<SendMaskDataInfo>();
				}

				if(_linkedSharedInfo.Contains(sharedInfo))
				{
					return;
				}

				_linkedSharedInfo.Add(sharedInfo);
			}

			
		}

		public class ClippingDataInfo
		{
			public apTransform_Mesh _maskParentMeshTF = null;
			public List<apTransform_Mesh> _clippedMeshTFs = null;

			public ClippingDataInfo(apTransform_Mesh meshTF)
			{
				_maskParentMeshTF = meshTF;
				_clippedMeshTFs = new List<apTransform_Mesh>();

				int nClipped = _maskParentMeshTF._clipChildMeshes != null ? _maskParentMeshTF._clipChildMeshes.Count : 0;
				for (int i = 0; i < nClipped; i++)
				{
					apTransform_Mesh.ClipMeshSet curClipSet = _maskParentMeshTF._clipChildMeshes[i];
					if(curClipSet == null
						|| curClipSet._meshTransform == null)
					{
						continue;
					}

					_clippedMeshTFs.Add(curClipSet._meshTransform);
				}
			}
		}

		private List<SendMaskDataInfo> _infos = null;
		private SendMaskDataInfo _selectedInfo = null;

		//리스트는 카테고리별로 묶자 (클리핑 + 페이즈 3개)
		//- 클리핑은 선택 불가이며 보여주기 위한 용도
		private List<ClippingDataInfo> _infos_Clipping = null;
		private ClippingDataInfo _selectedClippingInfo = null;



		//이 에디터를 실행할 때의 MeshTF를 우선적으로 적용하도록 한다.
		private apTransform_Mesh _lastSelectedTF = null;
		private apTransform_Mesh _calledMeshTF = null;


		// Show Window
		//--------------------------------------------------------------
		public static void ShowDialog(apEditor editor, apPortrait portrait, apMeshGroup meshGroup, apTransform_Mesh meshTF)
		{
			CloseDialog();

			if (editor == null || editor._portrait == null || meshGroup == null || portrait == null || meshTF == null)
			{
				return;
			}

			EditorWindow curWindow = EditorWindow.GetWindow(typeof(apDialog_SendMaskData), true, "Mask Settings", true);
			apDialog_SendMaskData curTool = curWindow as apDialog_SendMaskData;

			if (curTool != null && curTool != s_window)
			{
				int width = 1100;
				int height = 850;
				s_window = curTool;
				s_window.position = new Rect((editor.position.xMin + editor.position.xMax) / 2 - (width / 2),
												(editor.position.yMin + editor.position.yMax) / 2 - (height / 2),
												width, height);
				s_window.Init(editor, portrait, meshGroup, meshTF);
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

		void OnDisable()
		{
			Undo.undoRedoPerformed -= OnUndoRedoPerformed;
		}

		void OnEnable()
		{
			Undo.undoRedoPerformed -= OnUndoRedoPerformed;
			Undo.undoRedoPerformed += OnUndoRedoPerformed;
		}

		//Undo 발생시 리스트 갱신
		private void OnUndoRedoPerformed()
		{
			if(_editor == null || _portrait == null || _rootMeshGroup == null)
			{
				return;
			}

			RefreshSendMaskDataList();

			Repaint();
		}

		// Init
		//--------------------------------------------------------------
		public void Init(apEditor editor, apPortrait portrait, apMeshGroup meshGroup, apTransform_Mesh meshTF)
		{
			_editor = editor;

			_portrait = portrait;
			_rootMeshGroup = meshGroup.FindRootMeshGroup();
			//_meshTF = meshTF;		
			_lastSelectedTF = meshTF;
			_calledMeshTF = meshTF;

			_scroll_DataSet = new Vector2();
			_scroll_Properties = new Vector2();
			_scroll_MaskCopyProps = new Vector2();

			//int nSendMaskList = _meshTF._sendMaskDataList != null ? _meshTF._sendMaskDataList.Count : 0;
			//if(nSendMaskList > 0)
			//{
			//	//데이터가 있다면 첫번째를 기본값으로 선택하자
			//	_selectedSendMaskData = _meshTF._sendMaskDataList[0];
			//}

			//데이터를 가져온다.
			_infos = new List<SendMaskDataInfo>();
			_infos_Clipping = new List<ClippingDataInfo>();
			_selectedInfo = null;
			_selectedClippingInfo = null;

			RefreshSendMaskDataList();

			//호출된 Mesh TF를 가진 Mask Data가 있다면 그중 첫번째 것을 선택한다.
			List<SendMaskDataInfo> targetInfos = _infos.FindAll(delegate (SendMaskDataInfo a)
			{
				return a._meshTF == _calledMeshTF;
			});

			int nTargetInfos = targetInfos != null ? targetInfos.Count : 0;
			if(nTargetInfos > 0)
			{
				_selectedInfo = targetInfos[0];
			}
		}

		


		// GUI
		//----------------------------------------------------------------------
		void OnGUI()
		{
			int width = (int)position.width;
			int height = (int)position.height;
			if (_editor == null || _rootMeshGroup == null)
			{
				return;
			}

			if (_guiStyle_None == null)
			{
				_guiStyle_None = new GUIStyle(GUIStyle.none);
				_guiStyle_None.normal.textColor = GUI.skin.label.normal.textColor;
				_guiStyle_None.alignment = TextAnchor.MiddleLeft;
			}

			if (_guiStyle_None_Invalid == null)
			{
				_guiStyle_None_Invalid = new GUIStyle(GUI.skin.label);
				Color textColor = GUI.skin.label.normal.textColor;
				textColor.r *= 0.6f;
				textColor.g *= 0.6f;
				textColor.b *= 0.6f;
				_guiStyle_None_Invalid.normal.textColor = textColor;
				_guiStyle_None_Invalid.alignment = TextAnchor.MiddleLeft;
			}

			

			if(_guiStyle_ListIcon == null)
			{
				_guiStyle_ListIcon = new GUIStyle(GUIStyle.none);
				_guiStyle_ListIcon.alignment = TextAnchor.MiddleCenter;
				_guiStyle_ListIcon.padding = new RectOffset(1, 1, 1, 1);
				_guiStyle_ListIcon.margin = new RectOffset(0, 0, 0, 0);
			}

			if (_guiStyle_Selected == null)
			{
				_guiStyle_Selected = new GUIStyle(GUIStyle.none);
				if (EditorGUIUtility.isProSkin)
				{
					_guiStyle_Selected.normal.textColor = Color.cyan;
				}
				else
				{
					_guiStyle_Selected.normal.textColor = Color.white;
				}
				_guiStyle_Selected.alignment = TextAnchor.MiddleLeft;
			}

			if(_guiStyle_RemoveBtn == null)
			{
				_guiStyle_RemoveBtn = new GUIStyle(GUI.skin.button);
				_guiStyle_RemoveBtn.margin = new RectOffset(2, 2, 2, 2);
				_guiStyle_RemoveBtn.padding	= new RectOffset(2, 2, 2, 2);
			}

			if (_icon_Category == null)
			{
				_icon_Category = _editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_FoldDown);
			}

			if (_icon_Mesh == null)
			{
				_icon_Mesh = _editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_Mesh);
			}

			if (_icon_Remove == null)
			{
				_icon_Remove = _editor.ImageSet.Get(apImageSet.PRESET.Controller_RemoveRecordKey);
			}

			if(_icon_Add == null)
			{
				_icon_Add = _editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_AddTransform);
			}


			if(_icon_Data_AlphaMask == null)
			{
				_icon_Data_AlphaMask = _editor.ImageSet.Get(apImageSet.PRESET.SendMaskDataIcon_Alpha);
			}

			if(_icon_Data_MainTexOnly == null)
			{
				_icon_Data_MainTexOnly = _editor.ImageSet.Get(apImageSet.PRESET.SendMaskDataIcon_MainTexOnly);
			}

			if(_icon_Data_MainTexWithColor == null)
			{
				_icon_Data_MainTexWithColor = _editor.ImageSet.Get(apImageSet.PRESET.SendMaskDataIcon_MainTexWithColor);
			}

			if(_icon_Data_Custom == null)
			{
				_icon_Data_Custom = _editor.ImageSet.Get(apImageSet.PRESET.SendMaskDataIcon_Custom);
			}

			if(_icon_Data_Error == null)
			{
				_icon_Data_Error = _editor.ImageSet.Get(apImageSet.PRESET.SendMaskDataIcon_Error);
			}

			if(_icon_Data_Clipping == null)
			{
				_icon_Data_Clipping = _editor.ImageSet.Get(apImageSet.PRESET.SendMaskDataIcon_Clipping);
			}

			if(_icon_Data_Shared == null)
			{
				_icon_Data_Shared = _editor.ImageSet.Get(apImageSet.PRESET.SendMaskDataIcon_Shared);
			}
			if(_icon_Data_Chained == null)
			{
				_icon_Data_Chained = _editor.ImageSet.Get(apImageSet.PRESET.SendMaskDataIcon_Chain);
			}


			if (_guiContent_ListLabel == null)
			{
				_guiContent_ListLabel = apGUIContentWrapper.Make(_editor.GetText(TEXT.Masks), false, _icon_Category);
			}

			if(_guiContent_AddButton == null)
			{
				_guiContent_AddButton = apGUIContentWrapper.Make("", false, _icon_Add);
			}

			if(_guiContent_Icon_None == null)
			{
				_guiContent_Icon_None = apGUIContentWrapper.Make("", false);
			}
			if(_guiContent_Icon_Shared == null)
			{
				_guiContent_Icon_Shared = apGUIContentWrapper.Make(_icon_Data_Shared, "Multiple meshes generate a mask texture.");
			}

			if (_guiContent_Icon_Shared_Warning == null)
			{
				_guiContent_Icon_Shared_Warning = apGUIContentWrapper.Make(_icon_Data_Error, "Shared mask textures should not be created with different Render Orders.");
			}
		
			if(_guiContent_Icon_Chained == null)
			{
				_guiContent_Icon_Chained = apGUIContentWrapper.Make(_icon_Data_Chained, "This is a Chained state where a mesh receives a mask from another mesh and creates a mask.");
			}
		
			if(_guiContent_Icon_Chained_Warning == null)
			{
				_guiContent_Icon_Chained_Warning = apGUIContentWrapper.Make(_icon_Data_Error, "You need to change the Render Order to make the mask generation chain work properly.");
			}


			if(_guiStyle_CenterBox == null)
			{
				_guiStyle_CenterBox = new GUIStyle(GUI.skin.box);
				_guiStyle_CenterBox.alignment = TextAnchor.MiddleCenter;
			}

			EditorGUILayout.BeginVertical();

			Color prevColor = GUI.backgroundColor;



			//레이아웃 구조
			//----------------------------------------------------------
			//               마스크 데이터 세트 리스트
			//               (마스크 데이터 추가 버튼)
			//----------------------------------------------------------
			//                     선택된 데이터
			//----------------------------------------------------------
			//     기본 설정      |  전송 대상  |   프로퍼티 리스트
			// (데이터 삭제/복제) |             |   (프로퍼티 추가)
			//----------------------------------------------------------
			// 닫기 버튼

			//int height_Upper = (int)(height * 0.35f);
			//int height_Lower = height - (height_Upper + 80);
			int height_Lower = 500;
			int height_Upper = height - (height_Lower + 80);

			int width_LowerLeft = (int)((width - 30) * 0.32f);
			int width_LowerCenter = (int)((width - 30) * 0.25f);
			int width_LowerRight = width - (30 + width_LowerLeft + width_LowerCenter);

			
			Color listBGColor = new Color(0.9f, 0.9f, 0.9f);

			//삭제 요청
			SendMaskDataInfo removeMaskDataInfo = null;

			//1. 위쪽 리스트
			// - Send Mask Info 리스트를 출력한다.
			GUILayout.Space(10);

			EditorGUILayout.BeginVertical(GUILayout.Width(width), GUILayout.Height(height_Upper));

			//위쪽의 Send Mask Data 리스트를 출력한다.
			SendMaskDataInfo removeInfo = null;
			DrawUI_Upper(width, height_Upper, listBGColor, out removeInfo);

			if(removeInfo != null)
			{
				removeMaskDataInfo = removeInfo;
			}

			EditorGUILayout.EndVertical();



			GUILayout.Space(10);
			apEditorUtil.GUI_DelimeterBoxH(width);
			GUILayout.Space(10);


			// 아래쪽 UI들 (횡으로 3칸)

			EditorGUILayout.BeginHorizontal(GUILayout.Width(width), GUILayout.Height(height_Lower));
			GUILayout.Space(5);

			EditorGUILayout.BeginVertical(GUILayout.Width(width_LowerLeft));


			// 아래 중 첫번째 UI

			// Lower Left : RT 생성 속성			
			DrawUI_LowerLeft_BasicOptions(width_LowerLeft, height_Lower, listBGColor);


			EditorGUILayout.EndVertical();

			GUILayout.Space(5);

			EditorGUILayout.BeginVertical(GUILayout.Width(width_LowerCenter));

			//아래 중 두번째 UI
			// Lower Center : 대상 메시 TF들
			DrawUI_LowerCenter_TargetMeshTFs(width_LowerCenter, height_Lower, listBGColor);


			EditorGUILayout.EndVertical();

			GUILayout.Space(5);



			EditorGUILayout.BeginVertical(GUILayout.Width(width_LowerRight));

			// 아래 중 세번째 UI
			// Lower Right : 프로퍼티 리스트
			DrawUI_LowerRight(width_LowerRight, height_Lower, listBGColor);

			EditorGUILayout.EndVertical();

			EditorGUILayout.EndHorizontal();

			GUILayout.Space(5);


			bool isClose = false;
			if (GUILayout.Button(_editor.GetText(TEXT.Close), GUILayout.Height(25)))
			{
				isClose = true;
			}

			EditorGUILayout.EndVertical();


			if (removeMaskDataInfo != null)
			{
				//"마스크 요청 삭제"
				bool result = EditorUtility.DisplayDialog(	_editor.GetText(TEXT.DLG_RemoveMask_Title),
															_editor.GetText(TEXT.DLG_RemoveMask_Body),
															_editor.GetText(TEXT.Remove),
															_editor.GetText(TEXT.Cancel));

				if (result)
				{
					SetUndo();//>

					apSendMaskData removeMaskData = removeMaskDataInfo._sendMaskData;
					apTransform_Mesh removeMeshTF = removeMaskDataInfo._meshTF;

					if(_selectedInfo == removeMaskDataInfo)
					{
						//선택된 데이터가 삭제되었다면 선택을 해제한다.
						_selectedInfo = null;
					}

					if (removeMaskData != null && removeMeshTF != null)
					{
						if (removeMeshTF._sendMaskDataList != null)
						{
							removeMeshTF._sendMaskDataList.Remove(removeMaskData);
						}

						//링크를 다시 한다.
						_rootMeshGroup.LinkSendMaskData();

						//리스트를 갱신한다.
						RefreshSendMaskDataList();

						//선택된게 삭제되었다면 첫번째 것을 선택한다.
						if(_selectedInfo == null && _selectedClippingInfo == null)
						{
							if (_infos.Count > 0)
							{
								_selectedInfo = _infos[0];
							}
						}
					}

					apEditorUtil.ReleaseGUIFocus();

					//에디터 Hierarchy 갱신
					RefreshEditorHierarchy();
				}
			}


			if (isClose)
			{
				Close();
			}
		}


		private void DrawUI_Upper(int width, int height, Color listBGColor, out SendMaskDataInfo removeInfo)
		{
			removeInfo = null;

			int height_SetList = height - 30;

			Color prevColor = GUI.backgroundColor;

			GUI.backgroundColor = listBGColor;
			GUI.Box(new Rect(0, 10, width, height_SetList), "");
			GUI.backgroundColor = prevColor;

			_scroll_DataSet = EditorGUILayout.BeginScrollView(_scroll_DataSet, false, true, GUILayout.Width(width), GUILayout.Height(height_SetList));

			int width_DataSetListWidth = width - 30;
			EditorGUILayout.BeginVertical(GUILayout.Width(width_DataSetListWidth));


			//Clipping / Phase 1~3 순서대로 보여주자
			//1. 클리핑
			_guiContent_ListLabel.SetText(_editor.GetText(TEXT.DLG_PSD_Clipping));

			int nClippingInfo = _infos_Clipping != null ? _infos_Clipping.Count : 0;
			if(nClippingInfo > 0)
			{
				//투명 버튼
				GUILayout.Button(_guiContent_ListLabel.Content, _guiStyle_None, GUILayout.Height(22));//<투명 버튼

				ClippingDataInfo clippingInfo = null;

				for (int iClip = 0; iClip < nClippingInfo; iClip++)
				{
					clippingInfo = _infos_Clipping[iClip];
					DrawClippingDataItem(clippingInfo, width_DataSetListWidth);
				}

				GUILayout.Space(10);
			}


			//페이즈별로 리스트를 보여주자
			for (int iPhase = 0; iPhase < 3; iPhase++)
			{
				apSendMaskData.RT_RENDER_ORDER renderPhase = (apSendMaskData.RT_RENDER_ORDER)iPhase;

				_guiContent_ListLabel.SetText(_editor.GetText(TEXT.Phase) + " " + (iPhase + 1));
				GUILayout.Button(_guiContent_ListLabel.Content, _guiStyle_None, GUILayout.Height(22));//<투명 버튼

				//데이터 세트 리스트
				int nMaskDataList = _infos != null ? _infos.Count : 0;
				if (nMaskDataList > 0)
				{
					SendMaskDataInfo curMaskDataInfo = null;
					for (int iMaskData = 0; iMaskData < nMaskDataList; iMaskData++)
					{
						curMaskDataInfo = _infos[iMaskData];

						if(curMaskDataInfo._sendMaskData._rtRenderOrder != renderPhase)
						{
							continue;
						}
					
						bool isRemove = false;
						DrawMaskDataItem(curMaskDataInfo, width_DataSetListWidth, out isRemove);//Mask Data를 그린다.

						if(isRemove)
						{
							removeInfo = curMaskDataInfo;
						}
					}
				}

				GUILayout.Space(10);
			}
			

			GUILayout.Space(height_SetList + 50);
			EditorGUILayout.EndVertical();

			EditorGUILayout.EndScrollView();

			GUILayout.Space(5);
			//데이터 추가 버튼

			//"+ 마스크 정보 추가"
			_guiContent_AddButton.ClearText(false);
			_guiContent_AddButton.AppendSpaceText(1, false);
			_guiContent_AddButton.AppendText(_editor.GetText(TEXT.AddMask), true);
			if (GUILayout.Button(_guiContent_AddButton.Content, GUILayout.Height(25)))
			{
				MakeNewSendMaskData();
			}
		}

		private void DrawClippingDataItem(ClippingDataInfo info, int width)
		{
			if(info == null
				|| info._maskParentMeshTF == null)
			{
				return;
			}

			apTransform_Mesh parentMeshTF = info._maskParentMeshTF;
			List<apTransform_Mesh> clipped = info._clippedMeshTFs;

			int itemHeight = 22;

			GUIStyle curGUIStyle = _guiStyle_None;

			//선택되었다면
			Rect lastRect = GUILayoutUtility.GetLastRect();
			if (info == _selectedClippingInfo)
			{	
				apEditorUtil.DrawListUnitBG(lastRect.x + 1, lastRect.y + itemHeight, width + 30, itemHeight, apEditorUtil.UNIT_BG_STYLE.Main);

				curGUIStyle = _guiStyle_Selected;
			}


			if(_guiContent_Item_Send == null)
			{
				_guiContent_Item_Send = new apGUIContentWrapper();
			}
			if(_guiContent_Item_Recv == null)
			{
				_guiContent_Item_Recv = new apGUIContentWrapper();
			}
			_guiContent_Item_Send.ClearAll();
			_guiContent_Item_Recv.ClearAll();

			//전송하는 객체
			_guiContent_Item_Send.AppendText(parentMeshTF._nickName, true);

			int nClipped = clipped != null ? clipped.Count : 0;
			if(nClipped == 1)
			{
				_guiContent_Item_Recv.AppendText(clipped[0]._nickName, true);
			}
			else if(nClipped > 1)
			{
				for (int iInfo = 0; iInfo < nClipped; iInfo++)
				{
					if (iInfo >= nClipped - 1)
					{
						//마지막 연결 정보
						_guiContent_Item_Recv.AppendText(clipped[iInfo]._nickName, true);
					}
					else
					{
						//리스트 중간
						_guiContent_Item_Recv.AppendText(clipped[iInfo]._nickName, false);

						//만약 더 출력 가능하다면 쉼표
						if (iInfo < 4)
						{
							_guiContent_Item_Recv.AppendText(", ", false);
						}
						else
						{
							//아직 남았지만 여기서 중단
							_guiContent_Item_Recv.AppendText("...", false);
							break;
						}
					}
				}

				_guiContent_Item_Recv.AppendText(" ( ", false);
				_guiContent_Item_Recv.AppendText(nClipped.ToString(), false);
				_guiContent_Item_Recv.AppendText(" )", true);
			}
			else
			{
				_guiContent_Item_Recv.AppendText("", true);
			}
			
			int iconSize = itemHeight;
			int width_Label_Send = Mathf.Max((int)(width * 0.25f), 100);
			int width_Label_Recv = width - (15 + 15 + itemHeight + 4 + (iconSize * 3) + 6 + 10 + width_Label_Send + 2);

			//항목을 보여주자
			EditorGUILayout.BeginHorizontal(GUILayout.Width(width - 20), GUILayout.Height(itemHeight));
			GUILayout.Space(15);

			bool isClick = false;
			//아이콘을 그리자
			//아이콘 1 : 타입
			if(GUILayout.Button(_icon_Data_Clipping, _guiStyle_ListIcon, GUILayout.Width(iconSize), GUILayout.Height(iconSize)))
			{
				isClick = true;
			}

			if(GUILayout.Button(_guiContent_Icon_None.Content, _guiStyle_ListIcon, GUILayout.Width(iconSize), GUILayout.Height(iconSize)))
			{
				isClick = true;
			}

			if(GUILayout.Button(_guiContent_Icon_None.Content, _guiStyle_ListIcon, GUILayout.Width(iconSize), GUILayout.Height(iconSize)))
			{
				isClick = true;
			}

			GUILayout.Space(10);


			if(GUILayout.Button(_guiContent_Item_Send.Content, curGUIStyle, GUILayout.Width(width_Label_Send), GUILayout.Height(itemHeight)))
			{
				isClick = true;
			}

			if(GUILayout.Button(_guiContent_Item_Recv.Content, curGUIStyle, GUILayout.Width(width_Label_Recv), GUILayout.Height(itemHeight)))
			{
				isClick = true;
			}

			EditorGUILayout.EndHorizontal();

			if(isClick)
			{
				_selectedClippingInfo = info;
				_selectedInfo = null;
			}

		}

		private void DrawMaskDataItem(SendMaskDataInfo maskDataInfo, int width, out bool isRemoveData)
		{
			isRemoveData = false;

			if (maskDataInfo == null)
			{
				return;
			}
			if (maskDataInfo._sendMaskData == null
				|| maskDataInfo._meshTF == null)
			{
				return;
			}

			apTransform_Mesh meshTF = maskDataInfo._meshTF;
			apSendMaskData maskData = maskDataInfo._sendMaskData;

			int itemHeight = 22;

			GUIStyle curGUIStyle = _guiStyle_None;

			//선택되었다면
			Rect lastRect = GUILayoutUtility.GetLastRect();
			if (maskDataInfo == _selectedInfo)
			{	
				apEditorUtil.DrawListUnitBG(lastRect.x + 1, lastRect.y + itemHeight, width + 30, itemHeight, apEditorUtil.UNIT_BG_STYLE.Main);

				curGUIStyle = _guiStyle_Selected;
			}
			else if(maskDataInfo._sharedType != SHARED_TYPE.None)
			{
				//Shared된 경우는 해시값을 이용해서 색상을 만들자
				Color sharedColor = SharedIDToColor(maskData._sharedRTID);
				apEditorUtil.DrawListUnitBG_CustomColor(lastRect.x + 1, lastRect.y + itemHeight, width + 30, itemHeight, sharedColor);
			}

			if(_guiContent_Item_Send == null)
			{
				_guiContent_Item_Send = new apGUIContentWrapper();
			}
			if(_guiContent_Item_Recv == null)
			{
				_guiContent_Item_Recv = new apGUIContentWrapper();
			}
			_guiContent_Item_Send.ClearAll();
			_guiContent_Item_Recv.ClearAll();

			//아이콘은
			//마스크 타입 - 공유 - 체인 - 이름 - 삭제
			
			//이름은 [전송 Mesh TF] (Shared면 추가 메시들) + > 대상 메시 TF 이름" + Shared 타입으로 결정
			//아이콘
			Texture2D icon_Type = GetRTShaderTypeIcon(maskData._rtShaderType);
			
			//유효한지 여부에 따라서 아이콘이 다르다
			//int nReceivedMasks = meshTF._linkedReceivedMasks != null ? meshTF._linkedReceivedMasks.Count : 0;
			//if (meshTF._isClipping_Child || nReceivedMasks > 0)
			//{
			//	//클리핑되거나 마스크를 받는 메시는 Send 주체가 되면 안된다.
			//	_guiContent_Item.SetImage(_icon_Data_Error);
			//}
			//else
			//{
			//	//유효한 메시. Shader Type에 다른 아이콘을 가져온다.
			//	_guiContent_Item.SetImage(GetRTShaderTypeIcon(maskData._rtShaderType));
			//}
				

			//전송하는 객체
			if (maskData._isRTShared)
			{
				//Shared인 경우 ID 추가
				_guiContent_Item_Send.AppendText("[ ", false);
				_guiContent_Item_Send.AppendText(maskData._sharedRTID.ToString(), false);
				_guiContent_Item_Send.AppendText(" ] ", false);
			}

			_guiContent_Item_Send.AppendText(meshTF._nickName, true);

			//대상 객체 이름들 표기하자
			List<apSendMaskData.TargetInfo> targetInfos = maskData.TargetInfos;
			int nTargetInfos = targetInfos != null ? targetInfos.Count : 0;
			if (nTargetInfos == 1)
			{
				_guiContent_Item_Recv.AppendText(targetInfos[0].Name, true);
			}
			else if (nTargetInfos > 1)
			{
				for (int iInfo = 0; iInfo < nTargetInfos; iInfo++)
				{
					if (iInfo >= nTargetInfos - 1)
					{
						//마지막 연결 정보
						_guiContent_Item_Recv.AppendText(targetInfos[iInfo].Name, true);
					}
					else
					{
						//리스트 중간
						_guiContent_Item_Recv.AppendText(targetInfos[iInfo].Name, false);

						//만약 더 출력 가능하다면 쉼표
						if (iInfo < 4)
						{
							_guiContent_Item_Recv.AppendText(", ", false);
						}
						else
						{
							//아직 남았지만 여기서 중단
							_guiContent_Item_Recv.AppendText("...", false);
							break;
						}
					}
				}

				_guiContent_Item_Recv.AppendText(" ( ", false);
				_guiContent_Item_Recv.AppendText(nTargetInfos.ToString(), false);
				_guiContent_Item_Recv.AppendText(" )", true);
			}
			else
			{
				_guiContent_Item_Recv.AppendText("", true);
			}

			// //Shader 타입
			// _guiContent_Item.AppendText(" [ ", false);
			// _guiContent_Item.AppendText(GetRTShaderTypeName(maskData._rtShaderType), false);
			// _guiContent_Item.AppendText(" ]", true);


			int iconSize = itemHeight;
			int width_Label_Send = Mathf.Max((int)(width * 0.25f), 100);
			int width_Label_Recv = width - (15 + 15 + itemHeight + 4 + (iconSize * 3) + 6 + 10 + width_Label_Send + 2);

			bool isClick = false;
			//항목을 보여주자
			EditorGUILayout.BeginHorizontal(GUILayout.Width(width - 20), GUILayout.Height(itemHeight));
			GUILayout.Space(15);

			//아이콘을 그리자
			//아이콘 1 : 타입
			if(GUILayout.Button(icon_Type, _guiStyle_ListIcon, GUILayout.Width(iconSize), GUILayout.Height(iconSize)))
			{
				isClick = true;
			}

			//아이콘 2 : 공유
			apGUIContentWrapper curGUIContent_Shared = _guiContent_Icon_None;
			switch (maskDataInfo._sharedType)
			{
				case SHARED_TYPE.Shared:
					curGUIContent_Shared = _guiContent_Icon_Shared;
					break;

				case SHARED_TYPE.Warning:
					curGUIContent_Shared = _guiContent_Icon_Shared_Warning;
					break;
			}
			if(GUILayout.Button(curGUIContent_Shared.Content, _guiStyle_ListIcon, GUILayout.Width(iconSize), GUILayout.Height(iconSize)))
			{
				isClick = true;
			}


			//아이콘 3 : 체인
			apGUIContentWrapper curGUIContent_Chained = _guiContent_Icon_None;
			switch (maskDataInfo._chainType)
			{
				case CHAIN_TYPE.Chained:
					curGUIContent_Chained = _guiContent_Icon_Chained;
					break;

				case CHAIN_TYPE.Warning:
					curGUIContent_Chained = _guiContent_Icon_Chained_Warning;
					break;
			}
			if(GUILayout.Button(curGUIContent_Chained.Content, _guiStyle_ListIcon, GUILayout.Width(iconSize), GUILayout.Height(iconSize)))
			{
				isClick = true;
			}

			GUILayout.Space(10);

			if (GUILayout.Button(_guiContent_Item_Send.Content, curGUIStyle, GUILayout.Width(width_Label_Send), GUILayout.Height(itemHeight)))
			{
				isClick = true;
			}

			if (GUILayout.Button(_guiContent_Item_Recv.Content, curGUIStyle, GUILayout.Width(width_Label_Recv), GUILayout.Height(itemHeight)))
			{
				isClick = true;
			}

			if(GUILayout.Button(_editor.ImageSet.Get(apImageSet.PRESET.Controller_RemoveRecordKey), _guiStyle_RemoveBtn, GUILayout.Width(itemHeight), GUILayout.Height(itemHeight - 4)))
			{
				isRemoveData = true;
			}
			EditorGUILayout.EndHorizontal();

			if(isClick)
			{
				_selectedInfo = maskDataInfo;
				_selectedClippingInfo = null;
			}
		}





		/// <summary>
		/// 아래-왼쪽의 UI : 기본 설정을 그린다.
		/// </summary>
		/// <param name="width"></param>
		private void DrawUI_LowerLeft_BasicOptions(int width, int height, Color listBGColor)
		{	
			GUILayout.Space(5);
			EditorGUILayout.LabelField(_editor.GetText(TEXT.SourceMesh), GUILayout.Height(22));
			GUILayout.Space(5);

			int width_Label = Mathf.Max((int)((float)width * 0.55f), 100);
			int width_Value1 = width - (width_Label + 4);
			int height_Item = 22;

			Color prevColor = GUI.backgroundColor;
			Color invalidColor = GUI.backgroundColor * 0.5f;


			apSendMaskData.RT_SHADER_TYPE prev_RTShaderType = apSendMaskData.RT_SHADER_TYPE.AlphaMask;
			Shader prev_CustomRTShaderAsset = null;

			apTransform_Mesh.RENDER_TEXTURE_SIZE prev_RTSize = apTransform_Mesh.RENDER_TEXTURE_SIZE.s_256;
			bool prev_IsRTSizeOptimized = true;

			apSendMaskData.RT_RENDER_ORDER prev_RTRenderOrder = apSendMaskData.RT_RENDER_ORDER.Phase1;
			int prev_ShaderPassIndex = 0;

			bool prev_IsSharedRT = false;
			int prev_SharedID = 0;



			apSendMaskData selectedMaskData = null;
			apTransform_Mesh selectedMeshTF = null;

			if (_selectedInfo != null)
			{
				selectedMaskData = _selectedInfo._sendMaskData;
				selectedMeshTF = _selectedInfo._meshTF;
			}

			if (selectedMaskData != null)
			{
				//데이터가 존재하는 경우
				prev_RTShaderType = selectedMaskData._rtShaderType;
				prev_CustomRTShaderAsset = selectedMaskData._customRTShaderAsset;
				prev_RTSize = selectedMaskData._renderTextureSize;
				prev_IsRTSizeOptimized = selectedMaskData._isRTSizeOptimized;
				prev_RTRenderOrder = selectedMaskData._rtRenderOrder;
				prev_ShaderPassIndex = selectedMaskData._shaderPassIndex;
				prev_IsSharedRT = selectedMaskData._isRTShared;
				prev_SharedID = selectedMaskData._sharedRTID;
			}
			else if(_selectedClippingInfo != null)
			{
				//마스크의 크기에 한해서는 여기서도 설정 가능하다.
				prev_RTSize = _selectedClippingInfo._maskParentMeshTF._renderTexSize;
			}

			//선택된 마스크 생성 메시를 먼저 보여주자
			if (selectedMaskData != null && selectedMeshTF != null)
			{
				//유효한지 체크해야한다.
				// [ 메시 이름을 출력한다. ]
				GUI.backgroundColor = new Color(0.4f, 1.4f, 1.2f, 1.0f);
				GUILayout.Box(selectedMeshTF._mesh._name, _guiStyle_CenterBox, GUILayout.Width(width - 2), GUILayout.Height(30));
			}
			else if(_selectedClippingInfo != null)
			{
				//클리핑 데이터를 클릭한 경우
				GUI.backgroundColor = new Color(1.2f, 0.5f, 0.3f, 1.0f);
				GUILayout.Box(_selectedClippingInfo._maskParentMeshTF._nickName, _guiStyle_CenterBox, GUILayout.Width(width - 2), GUILayout.Height(30));
			}
			else
			{
				GUI.backgroundColor = new Color(0.7f, 0.7f, 0.7f, 1.0f);
				GUILayout.Box(_editor.GetText(TEXT.DLG_NotSelected), _guiStyle_CenterBox, GUILayout.Width(width - 2), GUILayout.Height(30));
			}
			GUI.backgroundColor = prevColor;
				

			
			GUILayout.Space(10);

			EditorGUILayout.LabelField(_editor.GetText(TEXT.RenderTextureSettings), GUILayout.Height(22));//"Render Texture Options"
			GUILayout.Space(5);

			//1. 렌더 텍스쳐 설정들
			//1-1 : RT 쉐이더
			EditorGUILayout.BeginHorizontal(GUILayout.Width(width), GUILayout.Height(height_Item));
			EditorGUILayout.LabelField(_editor.GetText(TEXT.ShaderType), GUILayout.Width(width_Label));//"쉐이더 종류"

			if (selectedMaskData == null)
			{
				GUI.backgroundColor = invalidColor;
			}

			EditorGUI.BeginChangeCheck();
			apSendMaskData.RT_SHADER_TYPE next_RTShaderType = (apSendMaskData.RT_SHADER_TYPE)EditorGUILayout.EnumPopup(prev_RTShaderType, GUILayout.Width(width_Value1));

			if (EditorGUI.EndChangeCheck())
			{
				if (next_RTShaderType != prev_RTShaderType && selectedMaskData != null)
				{
					SetUndo();//-
					selectedMaskData._rtShaderType = next_RTShaderType;
					apEditorUtil.ReleaseGUIFocus();
				}
			}


			GUI.backgroundColor = prevColor;
			EditorGUILayout.EndHorizontal();


			//1-2 : 커스텀 쉐이더
			if (prev_RTShaderType == apSendMaskData.RT_SHADER_TYPE.CustomShader)
			{
				EditorGUILayout.BeginHorizontal(GUILayout.Width(width), GUILayout.Height(height_Item));
				EditorGUILayout.LabelField(_editor.GetText(TEXT.ShaderAsset), GUILayout.Width(width_Label));//"쉐이더 에셋"

				if (selectedMaskData == null)
				{
					GUI.backgroundColor = invalidColor;
				}

				EditorGUI.BeginChangeCheck();
				Shader next_CustomRTShaderAsset = EditorGUILayout.ObjectField(prev_CustomRTShaderAsset, typeof(Shader), false, GUILayout.Width(width_Value1)) as Shader;
				if (EditorGUI.EndChangeCheck())
				{
					if (next_CustomRTShaderAsset != prev_CustomRTShaderAsset && selectedMaskData != null)
					{
						SetUndo();//-
						selectedMaskData._customRTShaderAsset = next_CustomRTShaderAsset;
					}
				}
				GUI.backgroundColor = prevColor;
				EditorGUILayout.EndHorizontal();
			}

			GUILayout.Space(2);

			//쉐이더 패스 인덱스
			EditorGUILayout.BeginHorizontal(GUILayout.Width(width), GUILayout.Height(height_Item));
			EditorGUILayout.LabelField(_editor.GetText(TEXT.PassIndex), GUILayout.Width(width_Label));//"패스 인덱스"

			if (selectedMaskData == null)
			{
				GUI.backgroundColor = invalidColor;
			}

			EditorGUI.BeginChangeCheck();
			int nextShaderPassIndex = EditorGUILayout.DelayedIntField((int)prev_ShaderPassIndex, GUILayout.Width(width_Value1));

			if (EditorGUI.EndChangeCheck())
			{
				if (nextShaderPassIndex != prev_ShaderPassIndex && selectedMaskData != null)
				{
					SetUndo();//-
					selectedMaskData._shaderPassIndex = nextShaderPassIndex;
					apEditorUtil.ReleaseGUIFocus();
				}
			}

			GUI.backgroundColor = prevColor;
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(5);


			//마스크 생성시 메인 프로퍼티에서 복제될 추가 프로퍼티들
			EditorGUILayout.LabelField(_editor.GetText(TEXT.CopiedProperties), GUILayout.Height(22));//"프로퍼티 복제 정보"
			GUILayout.Space(2);

			int height_CopyProps = 100;

			Rect lastRect = GUILayoutUtility.GetLastRect();
			GUI.backgroundColor = listBGColor;
			GUI.Box(new Rect(lastRect.x, lastRect.y + 4, width, height_CopyProps), "");
			GUI.backgroundColor = prevColor;

			
			_scroll_MaskCopyProps = EditorGUILayout.BeginScrollView(_scroll_MaskCopyProps, false, true, GUILayout.Width(width), GUILayout.Height(height_CopyProps));

			int width_InScroll = width - 20;
			EditorGUILayout.BeginVertical(GUILayout.Width(width_InScroll));

			if(selectedMaskData != null)
			{
				apSendMaskData.CopiedPropertyInfo removeCopyProp = null;
				int nCopiedProps = selectedMaskData._copiedProperties != null ? selectedMaskData._copiedProperties.Count : 0;
				if(nCopiedProps > 0)
				{
					for (int iCProp = 0; iCProp < nCopiedProps; iCProp++)
					{
						apSendMaskData.CopiedPropertyInfo curCopyProp = selectedMaskData._copiedProperties[iCProp];
						bool isRemove = DrawCopiedProp(curCopyProp, width_InScroll);
						if(isRemove)
						{
							removeCopyProp = curCopyProp;
						}
					}
				}

				if(removeCopyProp != null)
				{
					bool result = EditorUtility.DisplayDialog(
													_editor.GetText(TEXT.DLG_RemoveProperty_Title),
													_editor.GetText(TEXT.DLG_RemoveProperty_Body),
													_editor.GetText(TEXT.Remove),
													_editor.GetText(TEXT.Cancel));

					if (result)
					{	
						if (selectedMaskData._copiedProperties != null)
						{
							SetUndo();//-
							selectedMaskData._copiedProperties.Remove(removeCopyProp);
						}
					}
					
				}
			}
			

			GUILayout.Space(height_CopyProps + 50);
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndScrollView();

			//int width_LowerBtn = ((width - 4) / 2) - 2;
			int width_CopyPropBtn = (int)((width - 4) * 0.4f) - 2;
			int width_CoptyPropBtnFromList = width - (4 + width_CopyPropBtn + 2);

			//마스크 생성을 위한 프로퍼티 값 복사 설정
			EditorGUILayout.BeginHorizontal(GUILayout.Width(width), GUILayout.Height(22));
			GUILayout.Space(2);

			string strAddProp = _editor.GetText(TEXT.AddProperty);
			if (apEditorUtil.ToggledButton_2Side(_icon_Add, strAddProp, strAddProp, false, _selectedInfo != null, width_CopyPropBtn, 22))
			{
				if(_selectedInfo != null && selectedMaskData != null)
				{
					SetUndo();//-
					if(selectedMaskData._copiedProperties == null)
					{
						selectedMaskData._copiedProperties = new List<apSendMaskData.CopiedPropertyInfo>();
					}

					//새로운 복제 프로퍼티 추가
					selectedMaskData._copiedProperties.Add(new apSendMaskData.CopiedPropertyInfo());
				}
				
			}

			string strAddPropFromList = _editor.GetText(TEXT.AddPropertyFromList);
			if (apEditorUtil.ToggledButton_2Side(_icon_Add, strAddPropFromList, strAddPropFromList, false, _selectedInfo != null, width_CoptyPropBtnFromList, 22))
			{
				if(_selectedInfo != null && selectedMaskData != null)
				{
					//리스트에서 프로퍼티를 가져오자
					AddCopyPropFromProps();
				}
				
			}
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(10);
			



			//렌더 순서
			EditorGUILayout.BeginHorizontal(GUILayout.Width(width), GUILayout.Height(height_Item));
			EditorGUILayout.LabelField(_editor.GetText(TEXT.RenderOrder), GUILayout.Width(width_Label));//"렌더 순서"
			if (selectedMaskData == null)
			{
				GUI.backgroundColor = invalidColor;
			}

			EditorGUI.BeginChangeCheck();
			apSendMaskData.RT_RENDER_ORDER next_RTRenderOrder = (apSendMaskData.RT_RENDER_ORDER)EditorGUILayout.EnumPopup(prev_RTRenderOrder, GUILayout.Width(width_Value1));
			if(EditorGUI.EndChangeCheck())
			{
				if(next_RTRenderOrder != prev_RTRenderOrder && selectedMaskData != null)
				{
					SetUndo();//-
					selectedMaskData._rtRenderOrder = next_RTRenderOrder;
					
					//리스트 갱신
					RefreshSendMaskDataList();

					apEditorUtil.ReleaseGUIFocus();
				}
			}
			GUI.backgroundColor = prevColor;
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(2);

			//1-3 : 렌더 텍스쳐 크기
			//이건 클리핑에서도 가능
			EditorGUILayout.BeginHorizontal(GUILayout.Width(width), GUILayout.Height(height_Item));
			EditorGUILayout.LabelField(_editor.GetText(TEXT.TextureSize), GUILayout.Width(width_Label));//"텍스쳐 크기"

			if (selectedMaskData == null && _selectedClippingInfo == null)
			{
				GUI.backgroundColor = invalidColor;
			}

			EditorGUI.BeginChangeCheck();
			apTransform_Mesh.RENDER_TEXTURE_SIZE next_RTSize = (apTransform_Mesh.RENDER_TEXTURE_SIZE)EditorGUILayout.Popup((int)prev_RTSize, _propLabel_RTTextureSize, GUILayout.Width(width_Value1));

			if (EditorGUI.EndChangeCheck())
			{
				if (next_RTSize != prev_RTSize)
				{
					SetUndo();//-

					if(selectedMaskData != null)
					{
						//마스크 데이터 변경
						selectedMaskData._renderTextureSize = next_RTSize;
					}
					else if(_selectedClippingInfo != null)
					{
						//클리핑 마스크의 크기 변경
						_selectedClippingInfo._maskParentMeshTF._renderTexSize = next_RTSize;
					}
						
					
					apEditorUtil.ReleaseGUIFocus();
				}
			}

			GUI.backgroundColor = prevColor;
			EditorGUILayout.EndHorizontal();

			//1-4 : 크기 최적화
			EditorGUILayout.BeginHorizontal(GUILayout.Width(width), GUILayout.Height(height_Item));
			EditorGUILayout.LabelField(_editor.GetText(TEXT.OptimizedRender), GUILayout.Width(width_Label));//"크기 최적화"

			if (selectedMaskData == null)
			{
				GUI.backgroundColor = invalidColor;
			}

			EditorGUI.BeginChangeCheck();

			bool next_IsRTSizeOptimized = EditorGUILayout.Toggle(prev_IsRTSizeOptimized);

			if (EditorGUI.EndChangeCheck())
			{
				if (next_IsRTSizeOptimized != prev_IsRTSizeOptimized && selectedMaskData != null)
				{
					SetUndo();//-
					selectedMaskData._isRTSizeOptimized = next_IsRTSizeOptimized;
					apEditorUtil.ReleaseGUIFocus();
				}
			}

			GUI.backgroundColor = prevColor;
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(2);

			//1-5 : 공유된 렌더 텍스쳐
			EditorGUILayout.BeginHorizontal(GUILayout.Width(width), GUILayout.Height(height_Item));
			EditorGUILayout.LabelField(_editor.GetText(TEXT.SharedTexture), GUILayout.Width(width_Label));//"공유 생성"

			if (selectedMaskData == null)
			{
				GUI.backgroundColor = invalidColor;
			}

			EditorGUI.BeginChangeCheck();
			bool next_IsShared = EditorGUILayout.Toggle(prev_IsSharedRT, GUILayout.Width(25));
			if (EditorGUI.EndChangeCheck())
			{
				if (next_IsShared != prev_IsSharedRT && selectedMaskData != null)
				{
					SetUndo();//-
					selectedMaskData._isRTShared = next_IsShared;

					//리스트 갱신
					RefreshSendMaskDataList();
					
					apEditorUtil.ReleaseGUIFocus();
				}
			}

			if (prev_IsSharedRT)
			{
				//공유 텍스쳐를 이용하는 경우 ID를 지정
				GUILayout.Space(4);
				EditorGUILayout.LabelField("ID", GUILayout.Width(25));//"ID"

				EditorGUI.BeginChangeCheck();
				int next_SharedID = EditorGUILayout.DelayedIntField(prev_SharedID, GUILayout.Width(width_Value1 - (27 + 34)));
				if (EditorGUI.EndChangeCheck())
				{
					if (next_SharedID != prev_SharedID && selectedMaskData != null)
					{
						SetUndo();//-
						selectedMaskData._sharedRTID = next_SharedID;

						//리스트 갱신
						RefreshSendMaskDataList();

						apEditorUtil.ReleaseGUIFocus();
					}
				}
			}

			GUI.backgroundColor = prevColor;

			EditorGUILayout.EndHorizontal();

			GUILayout.Space(2);

			if(prev_IsSharedRT && selectedMaskData != null)
			{
				//공유 텍스쳐를 이용하는 경우
				//설정을 복사하는 버튼이 나온다.
				if(GUILayout.Button(_editor.GetText(TEXT.SyncSharedTextureOptions), GUILayout.Height(25)))
				{
					int iBtn = EditorUtility.DisplayDialogComplex(	_editor.GetText(TEXT.DLG_SendMaskData_SyncOptions_Title),
																	_editor.GetText(TEXT.DLG_SendMaskData_SyncOptions_Body),
																	_editor.GetText(TEXT.SyncAllOptions),
																	_editor.GetText(TEXT.SyncExceptShader),
																	_editor.GetText(TEXT.Cancel)
																	);
																	
					bool isSyncOptions = false;
					bool syncShaderOption = false;
					if(iBtn == 0)
					{
						//SyncAllOptions
						isSyncOptions = true;
						syncShaderOption = true;
					}
					else if(iBtn == 1)
					{
						//SyncExceptShader
						isSyncOptions = true;
					}

					if(isSyncOptions)
					{
						int nMaskInfos = _infos != null ? _infos.Count : 0;
						//동일한 ID의 Shared RT를 가진 MaskData를 찾는다.
						if(nMaskInfos > 0)
						{
							SetUndo();
							SendMaskDataInfo curMaskInfo = null;
							apSendMaskData curMaskData = null;
							for (int i = 0; i < nMaskInfos; i++)
							{
								curMaskInfo = _infos[i];
								curMaskData = curMaskInfo._sendMaskData;
								if(curMaskData == selectedMaskData)
								{
									continue;
								}

								if(!curMaskData._isRTShared
									|| curMaskData._sharedRTID != selectedMaskData._sharedRTID)
								{
									//동일한 ID의 공유 텍스쳐 타입이 아니라면
									continue;
								}

								//Shader 속성 복사
								if(syncShaderOption)
								{
									curMaskData._rtShaderType = selectedMaskData._rtShaderType;
									curMaskData._customRTShaderAsset = selectedMaskData._customRTShaderAsset;
									curMaskData._shaderPassIndex = selectedMaskData._shaderPassIndex;
								}

								//일반 속성 복사
								curMaskData._rtRenderOrder = selectedMaskData._rtRenderOrder;
								curMaskData._renderTextureSize = selectedMaskData._renderTextureSize;
								curMaskData._isRTSizeOptimized = selectedMaskData._isRTSizeOptimized;
							}

							//리스트 갱신
							RefreshSendMaskDataList();

							apEditorUtil.ReleaseGUIFocus();
						}


						
					}
					
				}

			}
		}

		/// <summary>
		/// 마스크 생성시 프로퍼티 복사 정보. 삭제 요청시 true 리턴
		/// </summary>
		/// <param name="copyProp"></param>
		/// <param name="width"></param>
		/// <returns></returns>
		private bool DrawCopiedProp(apSendMaskData.CopiedPropertyInfo copyProp, int width)
		{
			if(copyProp == null)
			{
				return false;
			}

			int itemHeight = 22;
			bool isRemove = false;

			//UI는 이름 + 타입 + 삭제 버튼
			int width_Name = (int)((float)width * 0.5f);
			int width_RemoveBtn = 25;
			int width_Type = width - (8 + width_Name + width_RemoveBtn + 6);

			EditorGUILayout.BeginHorizontal(GUILayout.Width(width), GUILayout.Height(itemHeight));
			GUILayout.Space(4);

			EditorGUI.BeginChangeCheck();
			string nextName = EditorGUILayout.DelayedTextField(copyProp._propName, GUILayout.Width(width_Name));
			if(EditorGUI.EndChangeCheck())
			{
				if (!string.Equals(nextName, copyProp._propName))
				{
					SetUndo();//-
					copyProp._propName = nextName;
					apEditorUtil.ReleaseGUIFocus();
				}
			}

			EditorGUI.BeginChangeCheck();
			apSendMaskData.SHADER_PROP_REAL_TYPE nextType = (apSendMaskData.SHADER_PROP_REAL_TYPE)EditorGUILayout.EnumPopup(copyProp._propType, GUILayout.Width(width_Type), GUILayout.Height(itemHeight));
			if(EditorGUI.EndChangeCheck())
			{
				if (nextType != copyProp._propType)
				{
					SetUndo();//-
					copyProp._propType = nextType;
					apEditorUtil.ReleaseGUIFocus();
				}
			}

			GUILayout.Space(2);

			if (GUILayout.Button(_icon_Remove, GUILayout.Width(width_RemoveBtn), GUILayout.Height(itemHeight - 4)))
			{
				isRemove = true;
			}
			EditorGUILayout.EndHorizontal();

			return isRemove;
		}


		/// <summary>
		/// 마스크 생성시의 "복제 프로퍼티"를 재질의 프로퍼티 리스트로부터 가져오자
		/// </summary>
		private void AddCopyPropFromProps()
		{
			if(_selectedInfo == null)
			{
				return;
			}
			apTransform_Mesh sendMesh = _selectedInfo._meshTF;
			if(sendMesh == null)
			{
				return;
			}

			//다이얼로그에는 리스트 형태로 제공한다.
			List<apTransform_Mesh> meshTFs = new List<apTransform_Mesh>();
			meshTFs.Add(sendMesh);

			//기본 재질 세트도 가져오자
			apMaterialSet defaultMatSet = _portrait.GetDefaultMaterialSet();

			//다이얼로그를 열자
			_loadKey_SelectShaderProp = apDialog_SelectShaderProp.ShowDialog_OnMaskDialog(	_editor,
																							meshTFs,
																							defaultMatSet,
																							OnSelectShaderProps_CopyProp);
		}



		private void OnSelectShaderProps_CopyProp(bool isSuccess, object loadKey, List<apDialog_SelectShaderProp.PropInfo> resultProps, apMaterialSet calledMaterialSet)
		{
			if (!isSuccess
				|| loadKey == null
				|| _loadKey_SelectShaderProp != loadKey)
			{
				_loadKey_SelectShaderProp = null;
				return;
			}

			_loadKey_SelectShaderProp = null;

			int nResult = resultProps != null ? resultProps.Count : 0;
			if (nResult == 0)
			{
				return;
			}

			if(_selectedInfo == null)
			{
				return;
			}

			apSendMaskData selectedMaskData = _selectedInfo._sendMaskData;
			if(selectedMaskData == null)
			{
				return;
			}

			SetUndo();//-

			apDialog_SelectShaderProp.PropInfo info = null;
			for (int iResult = 0; iResult < nResult; iResult++)
			{
				info = resultProps[iResult];
				//키워드 타입은 제외한다.
				if (info._type == apMaterialSet.SHADER_PROP_TYPE.Keyword)
				{
					continue;
				}

				apSendMaskData.CopiedPropertyInfo newCopyProp = new apSendMaskData.CopiedPropertyInfo();

				newCopyProp._propName = info._name;

				switch (info._type)
				{
					case apMaterialSet.SHADER_PROP_TYPE.Float:
						newCopyProp._propType = apSendMaskData.SHADER_PROP_REAL_TYPE.Float;
						break;

					case apMaterialSet.SHADER_PROP_TYPE.Int:
						newCopyProp._propType = apSendMaskData.SHADER_PROP_REAL_TYPE.Int;
						break;

					case apMaterialSet.SHADER_PROP_TYPE.Vector:
						newCopyProp._propType = apSendMaskData.SHADER_PROP_REAL_TYPE.Vector;
						break;

					case apMaterialSet.SHADER_PROP_TYPE.Color:
						newCopyProp._propType = apSendMaskData.SHADER_PROP_REAL_TYPE.Color;
						break;

					case apMaterialSet.SHADER_PROP_TYPE.Texture:
						newCopyProp._propType = apSendMaskData.SHADER_PROP_REAL_TYPE.Texture;
						break;
				}

				if(selectedMaskData._copiedProperties == null)
				{
					selectedMaskData._copiedProperties = new List<apSendMaskData.CopiedPropertyInfo>();
				}
				selectedMaskData._copiedProperties.Add(newCopyProp);
			}
		}



		/// <summary>
		/// 아래-가운데의 UI : 마스크가 적용될 대상 MeshTF를 그린다.
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="listBGColor"></param>
		private void DrawUI_LowerCenter_TargetMeshTFs(int width, int height, Color listBGColor)
		{
			GUILayout.Space(5);
			EditorGUILayout.LabelField(_editor.GetText(TEXT.DestinationMeshes), GUILayout.Height(22));//"Target Meshes"
			GUILayout.Space(5);

			int width_Targets = width - 30;
			int height_TargetList = height - 75;

			Color prevColor = GUI.backgroundColor;

			Rect lastRect = GUILayoutUtility.GetLastRect();
			GUI.backgroundColor = listBGColor;
			GUI.Box(new Rect(lastRect.x, lastRect.y + 7, width, height_TargetList), "");
			GUI.backgroundColor = prevColor;


			_scroll_Targets = EditorGUILayout.BeginScrollView(_scroll_Targets, false, true, GUILayout.Width(width), GUILayout.Height(height_TargetList));
			EditorGUILayout.BeginVertical(GUILayout.Width(width_Targets));

			apSendMaskData selectedMaskData = _selectedInfo != null ? _selectedInfo._sendMaskData : null;
			apTransform_Mesh selectedMeshTF = _selectedInfo != null ? _selectedInfo._meshTF : null;

			//리스트에 MeshTF를 넣어서 중복 그리기 막기
			HashSet<apTransform_Mesh> shownMeshTFs = new HashSet<apTransform_Mesh>();

			//대상 Mesh TF 리스트를 출력하자. 삭제 가능
			if (selectedMaskData != null)
			{
				int nTargetInfos = selectedMaskData._targetInfos != null ? selectedMaskData._targetInfos.Count : 0;
				apSendMaskData.TargetInfo curTargetInfo = null;
				apSendMaskData.TargetInfo removeTargetInfo = null;
				if (nTargetInfos > 0)
				{
					for (int iInfo = 0; iInfo < nTargetInfos; iInfo++)
					{
						curTargetInfo = selectedMaskData._targetInfos[iInfo];
						
						bool isRemoveTarget = DrawTargetMeshTF(curTargetInfo, width_Targets);

						if (isRemoveTarget)
						{
							removeTargetInfo = curTargetInfo;
						}

						//중복 그리기 방지
						if(curTargetInfo._linkedMeshTF != null)
						{
							if(!shownMeshTFs.Contains(curTargetInfo._linkedMeshTF))
							{
								shownMeshTFs.Add(curTargetInfo._linkedMeshTF);
							}
						}
					}
				}

				//공유 텍스쳐인 경우
				//공유된 다른 타겟도 보여주자 (삭제 불가)
				if(_selectedInfo._sharedType != SHARED_TYPE.None)
				{
					int nLinkedInfos = _selectedInfo._linkedSharedInfo != null ? _selectedInfo._linkedSharedInfo.Count : 0;
					if(nLinkedInfos > 0)
					{
						SendMaskDataInfo linkedInfo = null;
						apSendMaskData linkedMaskData = null;
						apSendMaskData.TargetInfo linkedTarget = null;
						apTransform_Mesh linkedTargetMeshTF = null;
						for (int iLinked = 0; iLinked < nLinkedInfos; iLinked++)
						{
							linkedInfo = _selectedInfo._linkedSharedInfo[iLinked];
							if(linkedInfo == null)
							{
								continue;
							}

							linkedMaskData = linkedInfo._sendMaskData;
							if(linkedMaskData == null)
							{
								continue;
							}

							int nLinkedTargets = linkedMaskData._targetInfos != null ? linkedMaskData._targetInfos.Count : 0;
							if (nLinkedTargets == 0)
							{
								continue;
							}

							for (int iLinkedTarget = 0; iLinkedTarget < nLinkedTargets; iLinkedTarget++)
							{
								linkedTarget = linkedMaskData._targetInfos[iLinkedTarget];
								if(linkedTarget == null)
								{
									continue;
								}
								linkedTargetMeshTF = linkedTarget._linkedMeshTF;
								if(linkedTargetMeshTF == null)
								{
									continue;
								}

								//중복 체크를 하자
								if(shownMeshTFs.Contains(linkedTargetMeshTF))
								{
									continue;
								}


								shownMeshTFs.Add(linkedTargetMeshTF);								

								//출력을 하자
								DrawTargetMeshTF_Simple(linkedTargetMeshTF, width_Targets);
							}
						}
					}
				}

				if (removeTargetInfo != null)
				{
					//"대상 메시 제거", "마스크가 전달될 대상 메시를 리스트에서 제거할까요?", "제거", "취소"
					bool result = EditorUtility.DisplayDialog(	_editor.GetText(TEXT.DLG_RemoveDestination_Title),
																_editor.GetText(TEXT.DLG_RemoveDestination_Body),
																_editor.GetText(TEXT.Remove),
																_editor.GetText(TEXT.Cancel));
					if (result)
					{
						SetUndo();//>

						if (selectedMaskData._targetInfos != null)
						{
							selectedMaskData._targetInfos.Remove(removeTargetInfo);
						}

						//마스크 연결 정보 변경시 연결다시 하기
						_rootMeshGroup.LinkSendMaskData();

						//에디터 Hierarchy 갱신
						RefreshEditorHierarchy();
					}
				}
			}
			else if(_selectedClippingInfo != null)
			{
				//클리핑 데이터를 선택했을 경우에도 보여주자
				int nClipped = _selectedClippingInfo._clippedMeshTFs != null ? _selectedClippingInfo._clippedMeshTFs.Count : 0;
				if(nClipped > 0)
				{
					apTransform_Mesh clippedMeshTF = null;
					
					for (int iClipped = 0; iClipped < nClipped; iClipped++)
					{
						clippedMeshTF = _selectedClippingInfo._clippedMeshTFs[iClipped];
						if(clippedMeshTF == null)
						{
							continue;
						}

						DrawTargetMeshTF_Simple(clippedMeshTF, width_Targets);
					}
				}
			}



			GUILayout.Space(height_TargetList + 30);
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndScrollView();

			GUILayout.Space(5);

			//대상 추가 버튼

			//"전송 대상 추가"
			string strAddDestination = _editor.GetText(TEXT.AddDestinationMesh);
			if (apEditorUtil.ToggledButton_2Side(_icon_Add, strAddDestination, strAddDestination, false, selectedMaskData != null, width - 4, 25))
			{
				if (selectedMaskData != null)
				{
					_loadKey_SelectTFs = apDialog_SelectMultipleObjects.ShowDialog(_editor,
																				_rootMeshGroup,
																				apDialog_SelectMultipleObjects.REQUEST_TARGET.ChildMeshTransforms,
																				OnAddMultipleChildTransformDialogResult,
																				_editor.GetText(TEXT.DLG_Select),
																				selectedMaskData,//현재 선택된 마스크 정보
																				selectedMeshTF//제외 대상
																				);
				}
			}
		}

		private bool DrawTargetMeshTF(apSendMaskData.TargetInfo targetInfo, int width)
		{
			if (targetInfo == null)
			{
				return false;
			}

			int itemHeight = 22;
			bool isRemove = false;

			//이름 + 삭제 버튼
			if (_guiContent_TargetInfo == null)
			{
				_guiContent_TargetInfo = new apGUIContentWrapper();
				_guiContent_TargetInfo.ClearAll();
				_guiContent_TargetInfo.SetImage(_icon_Mesh);
			}

			_guiContent_TargetInfo.ClearText(false);
			_guiContent_TargetInfo.AppendSpaceText(1, false);
			_guiContent_TargetInfo.AppendText(targetInfo.Name, true);

			int width_RemoveBtn = 27;
			int width_Text = width - (15 + width_RemoveBtn + 2);

			EditorGUILayout.BeginHorizontal(GUILayout.Width(width), GUILayout.Height(itemHeight));
			GUILayout.Space(15);

			EditorGUILayout.LabelField(_guiContent_TargetInfo.Content, GUILayout.Width(width_Text), GUILayout.Height(itemHeight));

			//삭제 버튼 보여주기
			if (GUILayout.Button(_icon_Remove, GUILayout.Width(width_RemoveBtn), GUILayout.Height(itemHeight)))
			{
				isRemove = true;
			}
			

			EditorGUILayout.EndHorizontal();

			return isRemove;
		}

		private void DrawTargetMeshTF_Simple(apTransform_Mesh targetMeshTF, int width)
		{
			if (targetMeshTF == null)
			{
				return;
			}

			int itemHeight = 22;

			//이름만
			if (_guiContent_TargetInfo == null)
			{
				_guiContent_TargetInfo = new apGUIContentWrapper();
				_guiContent_TargetInfo.ClearAll();
				_guiContent_TargetInfo.SetImage(_icon_Mesh);
			}

			_guiContent_TargetInfo.ClearText(false);
			_guiContent_TargetInfo.AppendSpaceText(1, false);
			_guiContent_TargetInfo.AppendText(targetMeshTF._nickName, true);

			int width_Text = width - 15;

			EditorGUILayout.BeginHorizontal(GUILayout.Width(width), GUILayout.Height(itemHeight));
			GUILayout.Space(15);

			EditorGUILayout.LabelField(_guiContent_TargetInfo.Content, _guiStyle_None_Invalid,  GUILayout.Width(width_Text), GUILayout.Height(itemHeight));


			EditorGUILayout.EndHorizontal();
		}





		public void OnAddMultipleChildTransformDialogResult(bool isSuccess, object loadKey, List<object> selectedObjects, object savedObject)
		{
			if (!isSuccess
				|| _loadKey_SelectTFs != loadKey
				|| loadKey == null
				|| savedObject == null)
			{
				_loadKey_SelectTFs = null;
				return;
			}

			_loadKey_SelectTFs = null;
			apSendMaskData savedMaskData = savedObject as apSendMaskData;
			if(_selectedInfo == null)
			{
				return;
			}

			apSendMaskData selectedMaskData = _selectedInfo._sendMaskData;
			apTransform_Mesh selectedMeshTF = _selectedInfo._meshTF;

			if (selectedMaskData != savedMaskData
				|| selectedMaskData == null
				|| selectedMeshTF == null)
			{
				return;
			}

			int nSelectedObjs = selectedObjects != null ? selectedObjects.Count : 0;

			bool isUndo = false;
			for (int i = 0; i < nSelectedObjs; i++)
			{
				object curObj = selectedObjects[i];
				apTransform_Mesh meshTF = curObj as apTransform_Mesh;

				if (meshTF == selectedMeshTF)
				{
					//본인은 마스크의 대상이 될 수 없다.
					continue;
				}

				apSendMaskData.TargetInfo existInfo = selectedMaskData.GetTargetInfo(meshTF);
				if (existInfo != null)
				{
					//이미 등록되어 있다.
					continue;
				}

				//마스크를 전송할 대상을 추가하자
				if (!isUndo)
				{
					SetUndo();//>
					isUndo = true;
				}

				if (selectedMaskData._targetInfos == null)
				{
					selectedMaskData._targetInfos = new List<apSendMaskData.TargetInfo>();
				}
				apSendMaskData.TargetInfo newInfo = new apSendMaskData.TargetInfo();
				newInfo._meshTFID = meshTF._transformUniqueID;
				newInfo._linkedMeshTF = meshTF;

				selectedMaskData._targetInfos.Add(newInfo);
			}

			//마스크 연결 정보 변경시 연결 다시 하기
			_rootMeshGroup.LinkSendMaskData();


			//에디터 Hierarchy 갱신
			RefreshEditorHierarchy();
		}




		private void DrawUI_LowerRight(int width, int height, Color listBGColor)
		{
			GUILayout.Space(5);
			EditorGUILayout.LabelField(_editor.GetText(TEXT.Properties), GUILayout.Height(22));
			GUILayout.Space(5);

			int width_PropList = width - 30;
			int height_PropList = height - 75;

			Color prevColor = GUI.backgroundColor;

			Rect lastRect = GUILayoutUtility.GetLastRect();
			GUI.backgroundColor = listBGColor;
			GUI.Box(new Rect(lastRect.x, lastRect.y + 7, width, height_PropList), "");
			GUI.backgroundColor = prevColor;

			
			_scroll_Properties = EditorGUILayout.BeginScrollView(_scroll_Properties, false, true, GUILayout.Width(width), GUILayout.Height(height_PropList));
			EditorGUILayout.BeginVertical(GUILayout.Width(width_PropList));

			GUILayout.Space(5);

			//프로퍼티 리스트
			apSendMaskData selectedMaskData = _selectedInfo != null ? _selectedInfo._sendMaskData : null;

			if (selectedMaskData != null)
			{
				int nProps = selectedMaskData._propertySets != null ? selectedMaskData._propertySets.Count : 0;
				if (nProps > 0)
				{
					apSendMaskData.ReceivePropertySet curPropSet = null;
					apSendMaskData.ReceivePropertySet removePropSet = null;

					for (int iProp = 0; iProp < nProps; iProp++)
					{
						curPropSet = selectedMaskData._propertySets[iProp];
						bool isRemoveProp = false;

						DrawShaderProperty(iProp, curPropSet, width_PropList, out isRemoveProp);

						if (isRemoveProp)
						{
							removePropSet = curPropSet;
						}

						if (iProp < nProps - 1)
						{
							GUILayout.Space(10);
							apEditorUtil.GUI_DelimeterBoxH(width_PropList);
							GUILayout.Space(10);
						}
					}

					if (removePropSet != null)
					{
						//"프로퍼티 삭제", "프로퍼티를 삭제할까요?"
						bool result = EditorUtility.DisplayDialog(
													_editor.GetText(TEXT.DLG_RemoveProperty_Title),
													_editor.GetText(TEXT.DLG_RemoveProperty_Body),
													_editor.GetText(TEXT.Remove),
													_editor.GetText(TEXT.Cancel));

						if (result && selectedMaskData._propertySets != null)
						{
							SetUndo();//-
							selectedMaskData._propertySets.Remove(removePropSet);
						}
					}
				}
			}

			GUILayout.Space(height + 50);
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndScrollView();

			GUILayout.Space(5);

			int width_LowerBtn = ((width - 4) / 2) - 2;
			EditorGUILayout.BeginHorizontal(GUILayout.Width(width), GUILayout.Height(25));
			GUILayout.Space(2);

			string strAddProp = _editor.GetText(TEXT.AddProperty);
			if (apEditorUtil.ToggledButton_2Side(_icon_Add, strAddProp, strAddProp, false, _selectedInfo != null, width_LowerBtn, 25))
			{
				if(_selectedInfo != null && selectedMaskData != null)
				{
					SetUndo();//-

					if (selectedMaskData._propertySets == null)
					{
						selectedMaskData._propertySets = new List<apSendMaskData.ReceivePropertySet>();
					}

					apSendMaskData.ReceivePropertySet newProp = new apSendMaskData.ReceivePropertySet();
					newProp._preset = apSendMaskData.SHADER_PROP_PRESET.AlphaMaskPreset;
					newProp._reservedChannel = apSendMaskData.SHADER_PROP_RESERVED_CHANNEL.Channel_1;
					newProp._value_MaskOp = apSendMaskData.MASK_OPERATION.And;

					selectedMaskData._propertySets.Add(newProp);
				}
				
			}

			string strAddPropFromList = _editor.GetText(TEXT.AddPropertyFromList);
			if (apEditorUtil.ToggledButton_2Side(_icon_Add, strAddPropFromList, strAddPropFromList, false, _selectedInfo != null, width_LowerBtn, 25))
			{
				if(_selectedInfo != null && selectedMaskData != null)
				{
					//프리셋 리스트를 꺼내서 추가해야한다.
					AddPropertyFromShaderProps();
				}
				
			}
			EditorGUILayout.EndHorizontal();
		}


		// 쉐이더 프로퍼티 리스트들 (개별 아님)
		private void DrawShaderProperty(int index, apSendMaskData.ReceivePropertySet propSet, int width, out bool isRemove)
		{
			isRemove = false;

			if (propSet == null)
			{
				return;
			}

			Color prevColor = GUI.backgroundColor;

			//1 : 프리셋 타입 + 이름 (선택) + 삭제 버튼
			//2 : 값 타입 + 세부 값 옵션 (선택)
			//3 : 컨트롤 파라미터 연결 (선택)


			//1 : 번호 + 이름 + 타입 + 삭제 버튼
			//2-1 : 프리셋타입인 경우) Operation 또는 Interaction 중 하나
			//2-2 : 컨트롤 파라미터 타입인 경우) 컨트롤 파라미터 연결
			//2-3 : 커스텀 값인 경우) 해당 커스텀 값 UI
			int height_Item = 24;

			int width_Line1_Index = 30;
			int width_Line1_NameValue = (int)(width * 0.3f);
			int width_Line1_NameLabel = 55;
			int width_Line1_RemoveBtn = 23;
			int width_Line1_Preset = width - (width_Line1_Index + width_Line1_NameLabel + width_Line1_NameValue + width_Line1_RemoveBtn + 18);

			int width_Line2_Margin = 38;
			int width_Line2_Type = Mathf.Max(80, (int)(width * 0.3f));
			int width_Line2_Value = width - (width_Line2_Margin + width_Line2_Type + 8);


			// [ 첫번째 줄 ]
			//1. 이름 / 타입
			EditorGUILayout.BeginHorizontal(GUILayout.Width(width), GUILayout.Height(height_Item));
			GUILayout.Space(4);

			//인덱스
			if (_guiStyle_LabelCenter == null)
			{
				_guiStyle_LabelCenter = new GUIStyle(GUI.skin.label);
				_guiStyle_LabelCenter.alignment = TextAnchor.MiddleCenter;
			}

			EditorGUILayout.LabelField((index + 1).ToString(), _guiStyle_LabelCenter, GUILayout.Width(width_Line1_Index));

			//프리셋 타입
			EditorGUI.BeginChangeCheck();
			apSendMaskData.SHADER_PROP_PRESET nextPreset = (apSendMaskData.SHADER_PROP_PRESET)EditorGUILayout.EnumPopup(propSet._preset, GUILayout.Width(width_Line1_Preset));
			if (EditorGUI.EndChangeCheck())
			{
				if (propSet._preset != nextPreset)
				{
					SetUndo();//-
					//프리셋 변경
					propSet._preset = nextPreset;
				}

			}

			//첫줄 두번째 항목
			//- Custom 타입 : 프로퍼티 이름 (String)
			//- Alpha Mask 프리셋 : 채널 값
			//- See-Through 프리셋 : 여백

			GUILayout.Space(6);

			switch(propSet._preset)
			{
				case apSendMaskData.SHADER_PROP_PRESET.Custom:
					{
						// Custom 타입 : 프로퍼티 이름 (String)
						EditorGUILayout.LabelField(_editor.GetText(TEXT.DLG_Name), GUILayout.Width(width_Line1_NameLabel));
						EditorGUI.BeginChangeCheck();
						string nextName = EditorGUILayout.DelayedTextField(propSet._customName, GUILayout.Width(width_Line1_NameValue));
						if (EditorGUI.EndChangeCheck())
						{
							//이름 변경
							if (!string.Equals(nextName, propSet._customName))
							{
								SetUndo();//-
								propSet._customName = nextName;

								apEditorUtil.ReleaseGUIFocus();
							}
						}
					}
					break;

				case apSendMaskData.SHADER_PROP_PRESET.AlphaMaskPreset:
					{
						// Alpha Mask 프리셋 : 채널 값
						EditorGUILayout.LabelField(_editor.GetText(TEXT.Channel), GUILayout.Width(width_Line1_NameLabel));//"Channel"
						EditorGUI.BeginChangeCheck();
						apSendMaskData.SHADER_PROP_RESERVED_CHANNEL nextChannel = (apSendMaskData.SHADER_PROP_RESERVED_CHANNEL)EditorGUILayout.EnumPopup(propSet._reservedChannel, GUILayout.Width(width_Line1_NameValue));
						if (EditorGUI.EndChangeCheck())
						{
							//채널 변경
							if (propSet._reservedChannel != nextChannel)
							{
								SetUndo();//-
								propSet._reservedChannel = nextChannel;
							}
						}
					}
					break;

				case apSendMaskData.SHADER_PROP_PRESET.SeeThroughPreset:
					{
						// See-Through 프리셋 : 여백
						GUILayout.Space(width_Line1_NameLabel + width_Line1_NameValue + 6);
					}
					break;
			}

			GUILayout.Space(4);

			//첫줄 마지막 : 삭제 버튼
			if (GUILayout.Button(_icon_Remove, GUILayout.Width(width_Line1_RemoveBtn), GUILayout.Height(height_Item - 2)))
			{
				isRemove = true;
			}

			EditorGUILayout.EndHorizontal();



			// [ 두번째 줄 ]

			//- Custom 타입 : Value 타입 + 값
			//- Alpha Mask 프리셋 : MaskOp 타입 (고정) + Enum 값
			//- See-Through 프리셋 : Alpha (Float) + Float 값

			//주의
			//- Prop의 프리셋 타입이 Custom이나 MaskOp인 경우에 UI가 나타난다.
			//- MaskOp인 경우에는 UI가 조금 다르며 값의 타입을 조절할 수 없다.

			switch(propSet._preset)
			{
				case apSendMaskData.SHADER_PROP_PRESET.Custom:
					{
						// Custom 타입 : Value 타입 + 값
						EditorGUILayout.BeginHorizontal(GUILayout.Width(width), GUILayout.Height(height_Item));
						GUILayout.Space(width_Line2_Margin);

						//2-1 타입 (레이블 없는 UI)
						int curPropTypeIndex = PropTypeEnumToIndex(propSet._customPropType);

						EditorGUI.BeginChangeCheck();
						int nextPropTypeIndex = EditorGUILayout.Popup(curPropTypeIndex, _propLabel_ShaderPropType, GUILayout.Width(width_Line2_Type));
						if (EditorGUI.EndChangeCheck())
						{
							if (nextPropTypeIndex != curPropTypeIndex)
							{
								SetUndo();//-
								propSet._customPropType = IndexToPropTypeEnum(nextPropTypeIndex);
							}
						}

						GUILayout.Space(4);

						//2-2 값들
						switch (propSet._customPropType)
						{
							//1. 마스크 연산 방식
							case apSendMaskData.SHADER_PROP_VALUE_TYPE.MaskOp:
								{
									//마스크 연산 방식
									EditorGUI.BeginChangeCheck();
									apSendMaskData.MASK_OPERATION nextOp = (apSendMaskData.MASK_OPERATION)EditorGUILayout.EnumPopup(propSet._value_MaskOp, GUILayout.Width(width_Line2_Value));
									if (EditorGUI.EndChangeCheck())
									{
										if (nextOp != propSet._value_MaskOp)
										{
											SetUndo();//-
											propSet._value_MaskOp = nextOp;
										}
									}
								}
								break;

							case apSendMaskData.SHADER_PROP_VALUE_TYPE.Value_Float:
								{
									//커스텀 값 : Float
									EditorGUI.BeginChangeCheck();
									float nextFloatValue = EditorGUILayout.DelayedFloatField(propSet._value_Float, GUILayout.Width(width_Line2_Value));
									if (EditorGUI.EndChangeCheck())
									{
										SetUndo();//-
										propSet._value_Float = nextFloatValue;
									}
								}
								break;

							case apSendMaskData.SHADER_PROP_VALUE_TYPE.Value_Int:
								{
									//커스텀 값 : Int
									EditorGUI.BeginChangeCheck();
									int nextIntValue = EditorGUILayout.DelayedIntField(propSet._value_Int, GUILayout.Width(width_Line2_Value));
									if (EditorGUI.EndChangeCheck())
									{
										SetUndo();//-
										propSet._value_Int = nextIntValue;
									}
								}
								break;

							case apSendMaskData.SHADER_PROP_VALUE_TYPE.Value_Vector:
								{
									//커스텀 값 : Vector
									int width_Line2_Value4 = (width_Line2_Value / 4) - 2;
									EditorGUI.BeginChangeCheck();
									float nextVecValue_X = EditorGUILayout.DelayedFloatField(propSet._value_Vector.x, GUILayout.Width(width_Line2_Value4));
									if (EditorGUI.EndChangeCheck())
									{
										SetUndo();//-
										propSet._value_Vector.x = nextVecValue_X;
									}

									EditorGUI.BeginChangeCheck();
									float nextVecValue_Y = EditorGUILayout.DelayedFloatField(propSet._value_Vector.y, GUILayout.Width(width_Line2_Value4));
									if (EditorGUI.EndChangeCheck())
									{
										SetUndo();//-
										propSet._value_Vector.y = nextVecValue_Y;
									}

									EditorGUI.BeginChangeCheck();
									float nextVecValue_Z = EditorGUILayout.DelayedFloatField(propSet._value_Vector.z, GUILayout.Width(width_Line2_Value4));
									if (EditorGUI.EndChangeCheck())
									{
										SetUndo();//-
										propSet._value_Vector.z = nextVecValue_Z;
									}

									EditorGUI.BeginChangeCheck();
									float nextVecValue_W = EditorGUILayout.DelayedFloatField(propSet._value_Vector.w, GUILayout.Width(width_Line2_Value4));
									if (EditorGUI.EndChangeCheck())
									{
										SetUndo();//-
										propSet._value_Vector.w = nextVecValue_W;
									}
								}
								break;

							case apSendMaskData.SHADER_PROP_VALUE_TYPE.Value_Texture:
								{
									//커스텀 값 : Texture
									EditorGUI.BeginChangeCheck();
									Texture nextTex = EditorGUILayout.ObjectField(propSet._value_Texture, typeof(Texture), false, GUILayout.Width(width_Line2_Value)) as Texture;
									if (EditorGUI.EndChangeCheck())
									{
										SetUndo();//-
										propSet._value_Texture = nextTex;
									}
								}
								break;

							case apSendMaskData.SHADER_PROP_VALUE_TYPE.Value_Color:
								{
									//커스텀 값 : Color
									EditorGUI.BeginChangeCheck();
									Color nextColor = EditorGUILayout.ColorField(propSet._value_Color, GUILayout.Width(width_Line2_Value));
									if (EditorGUI.EndChangeCheck())
									{
										SetUndo();//-
										propSet._value_Color = nextColor;
									}
								}
								break;
						}

						EditorGUILayout.EndHorizontal();

					}
					break;

				case apSendMaskData.SHADER_PROP_PRESET.AlphaMaskPreset:
					{
						// Alpha Mask 프리셋 : MaskOp 타입 (고정) + Enum 값
						EditorGUILayout.BeginHorizontal(GUILayout.Width(width), GUILayout.Height(height_Item));
						GUILayout.Space(width_Line2_Margin);

						//Enum UI는 동일해보이지만 변경이 불가능하다 (붉은색 색상)
						int curPropTypeIndex = PropTypeEnumToIndex(apSendMaskData.SHADER_PROP_VALUE_TYPE.MaskOp);
						GUI.backgroundColor = new Color(1.0f, 0.7f, 0.7f, 1.0f);
						EditorGUILayout.Popup(curPropTypeIndex, _propLabel_ShaderPropType, GUILayout.Width(width_Line2_Type));
						GUI.backgroundColor = prevColor;
						GUILayout.Space(4);

						//마스크 연산 방식은 결정
						EditorGUI.BeginChangeCheck();
						apSendMaskData.MASK_OPERATION nextOp = (apSendMaskData.MASK_OPERATION)EditorGUILayout.EnumPopup(propSet._value_MaskOp, GUILayout.Width(width_Line2_Value));
						if (EditorGUI.EndChangeCheck())
						{
							if (nextOp != propSet._value_MaskOp)
							{
								SetUndo();//-
								propSet._value_MaskOp = nextOp;
							}
						}


						EditorGUILayout.EndHorizontal();
					}
					break;

				case apSendMaskData.SHADER_PROP_PRESET.SeeThroughPreset:
					{
						// See-Through 프리셋 : Alpha (Float) + Float 값
						// Custom 타입 : Value 타입 + 값
						EditorGUILayout.BeginHorizontal(GUILayout.Width(width), GUILayout.Height(height_Item));
						GUILayout.Space(width_Line2_Margin);

						//Enum UI는 동일해보이지만 변경이 불가능하다 (붉은색 색상)
						int curPropTypeIndex = PropTypeEnumToIndex(apSendMaskData.SHADER_PROP_VALUE_TYPE.Value_Float);

						GUI.backgroundColor = new Color(1.0f, 0.7f, 0.7f, 1.0f);
						EditorGUILayout.Popup(curPropTypeIndex, _propLabel_ShaderPropType, GUILayout.Width(width_Line2_Type));
						GUI.backgroundColor = prevColor;
						GUILayout.Space(4);

						// Float 타입의 Alpha 값 지정
						//커스텀 값 : Float
						EditorGUI.BeginChangeCheck();
						float nextFloatValue = EditorGUILayout.DelayedFloatField(propSet._value_Float, GUILayout.Width(width_Line2_Value));
						if (EditorGUI.EndChangeCheck())
						{
							SetUndo();//-
							propSet._value_Float = nextFloatValue;
						}
						EditorGUILayout.EndHorizontal();
					}
					break;
			}



			//[ 세번째 줄 ]
			//컨트롤 파라미터와 연결되는 타입은 추가로 UI가 필요하다.
			//컨트롤 파라미터가 필요한 경우
			//- Custom 타입 + Float, Int, Vector 값
			//- See-Through 프리셋 (Alpha 연산) - Float 타입만

			bool isControlParamUI = false;
			apSendMaskData.SHADER_PROP_REAL_TYPE cpRealType = apSendMaskData.SHADER_PROP_REAL_TYPE.Float;
			if (propSet._preset == apSendMaskData.SHADER_PROP_PRESET.Custom)
			{		
				switch(propSet._customPropType)
				{
					case apSendMaskData.SHADER_PROP_VALUE_TYPE.Value_Float:
						isControlParamUI = true;
						cpRealType = apSendMaskData.SHADER_PROP_REAL_TYPE.Float;
						break;

					case apSendMaskData.SHADER_PROP_VALUE_TYPE.Value_Int:
						isControlParamUI = true;
						cpRealType = apSendMaskData.SHADER_PROP_REAL_TYPE.Int;
						break;

					case apSendMaskData.SHADER_PROP_VALUE_TYPE.Value_Vector:
						isControlParamUI = true;
						cpRealType = apSendMaskData.SHADER_PROP_REAL_TYPE.Vector;
						break;
				}
			}
			else if(propSet._preset == apSendMaskData.SHADER_PROP_PRESET.SeeThroughPreset)
			{
				//See-Through 프리셋은 Alpha 값에 컨트롤 파라미터를 연결할 수 있다.
				isControlParamUI = true;
				cpRealType = apSendMaskData.SHADER_PROP_REAL_TYPE.Float;
			}

			if(isControlParamUI)
			{
				//컨트롤 파라미터 UI를 출력한다.
				EditorGUILayout.BeginHorizontal(GUILayout.Width(width), GUILayout.Height(height_Item));
				GUILayout.Space(width_Line2_Margin);

				//Toggle
				EditorGUI.BeginChangeCheck();
				bool nextUseControlParam = EditorGUILayout.Toggle(propSet._value_IsUseControlParam, GUILayout.Width(20));
				if(EditorGUI.EndChangeCheck())
				{
					SetUndo();//-
					propSet._value_IsUseControlParam = nextUseControlParam;
				}

				//Label (Control Param)
				EditorGUILayout.LabelField(_editor.GetUIWord(UIWORD.ControlParameter), GUILayout.Width(width_Line2_Type));

				//연결된 컨트롤 파라미터 이름
				if (_guiContent_ControlParam == null)
				{
					_guiContent_ControlParam = new apGUIContentWrapper();
					_guiContent_ControlParam.ClearAll();
					_guiContent_ControlParam.SetImage(_editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_Param_16px));
				}

				_guiContent_ControlParam.ClearText(false);
				_guiContent_ControlParam.AppendSpaceText(1, false);
				if (propSet._value_LinkedControlParam != null)
				{
					_guiContent_ControlParam.AppendText(propSet._value_LinkedControlParam._keyName, true);
				}
				else
				{
					_guiContent_ControlParam.AppendText(_editor.GetText(TEXT.None), true);
				}

				int width_Line2_SetBtn = 40;
				int width_Line2_ControlParam = width_Line2_Value - (width_Line2_SetBtn + 2 + 20);

				EditorGUILayout.LabelField(_guiContent_ControlParam.Content, GUILayout.Width(width_Line2_ControlParam));

				//버튼
				if (GUILayout.Button(_editor.GetUIWord(UIWORD.Set), GUILayout.Width(width_Line2_SetBtn), GUILayout.Height(20)))
				{
					//컨트롤 파라미터를 연결한다.
					apDialog_SelectControlParam.PARAM_TYPE paramFilter;
					switch(cpRealType)
					{
						case apSendMaskData.SHADER_PROP_REAL_TYPE.Float:
							paramFilter = apDialog_SelectControlParam.PARAM_TYPE.Float;
							break;

						case apSendMaskData.SHADER_PROP_REAL_TYPE.Int:
							paramFilter = apDialog_SelectControlParam.PARAM_TYPE.Int;
							break;

						case apSendMaskData.SHADER_PROP_REAL_TYPE.Vector:
							paramFilter = apDialog_SelectControlParam.PARAM_TYPE.Vector2;
							break;

						default:
							paramFilter = apDialog_SelectControlParam.PARAM_TYPE.Float;
							break;						
					}
					_loadKey_SelectControlParam = apDialog_SelectControlParam.ShowDialog(_editor, paramFilter, OnSelectControlParamResult, propSet);
				}

				EditorGUILayout.EndHorizontal();
			}

		}


		private void OnSelectControlParamResult(bool isSuccess, object loadKey, apControlParam resultControlParam, object savedObject)
		{
			if (!isSuccess
				|| _loadKey_SelectControlParam != loadKey
				|| savedObject == null
				|| _selectedInfo == null
				|| _rootMeshGroup == null)
			{
				_loadKey_SelectControlParam = null;
				return;
			}

			_loadKey_SelectControlParam = null;

			apSendMaskData.ReceivePropertySet savedPropSet = savedObject as apSendMaskData.ReceivePropertySet;
			if (savedPropSet == null)
			{
				return;
			}

			SetUndo();//-

			if (resultControlParam != null)
			{
				//컨트롤 파라미터 입력
				savedPropSet._value_ControlParamID = resultControlParam._uniqueID;
				savedPropSet._value_LinkedControlParam = resultControlParam;
			}
			else
			{
				//null 값을 입력
				savedPropSet._value_ControlParamID = -1;
				savedPropSet._value_LinkedControlParam = null;
			}

			//연결 다시
			_rootMeshGroup.LinkSendMaskData();

		}


		private void AddPropertyFromShaderProps()
		{
			//Default 재질을 가져오자
			apSendMaskData selectedMaskData = _selectedInfo != null ? _selectedInfo._sendMaskData : null;
			if (selectedMaskData == null)
			{
				return;
			}
			List<apTransform_Mesh> meshTFs = new List<apTransform_Mesh>();
			int nTargetInfos = selectedMaskData._targetInfos != null ? selectedMaskData._targetInfos.Count : 0;
			if (nTargetInfos > 0)
			{
				apSendMaskData.TargetInfo curInfo = null;
				for (int i = 0; i < nTargetInfos; i++)
				{
					curInfo = selectedMaskData._targetInfos[i];
					if (curInfo == null)
					{
						continue;
					}

					if (curInfo._linkedMeshTF == null)
					{
						continue;
					}

					if (!meshTFs.Contains(curInfo._linkedMeshTF))
					{
						meshTFs.Add(curInfo._linkedMeshTF);
					}
				}
			}

			//기본 재질 세트도 가져오자
			apMaterialSet defaultMatSet = _portrait.GetDefaultMaterialSet();

			_loadKey_SelectShaderProp = apDialog_SelectShaderProp.ShowDialog_OnMaskDialog(_editor,
																							meshTFs,
																							defaultMatSet,
																							OnSelectShaderProps);


		}


		private void OnSelectShaderProps(bool isSuccess, object loadKey, List<apDialog_SelectShaderProp.PropInfo> resultProps, apMaterialSet calledMaterialSet)
		{
			if (!isSuccess
				|| loadKey == null
				|| _loadKey_SelectShaderProp != loadKey)
			{
				_loadKey_SelectShaderProp = null;
				return;
			}

			_loadKey_SelectShaderProp = null;

			int nResult = resultProps != null ? resultProps.Count : 0;
			if (nResult == 0)
			{
				return;
			}

			//이제 하나씩 추가하자
			apSendMaskData selectedMaskData = _selectedInfo != null ? _selectedInfo._sendMaskData : null;
			if(selectedMaskData == null)
			{
				return;
			}

			SetUndo();//-

			if (selectedMaskData._propertySets == null)
			{
				selectedMaskData._propertySets = new List<apSendMaskData.ReceivePropertySet>();
			}

			apDialog_SelectShaderProp.PropInfo info = null;
			for (int iResult = 0; iResult < nResult; iResult++)
			{
				info = resultProps[iResult];
				//키워드 타입은 제외한다.
				if (info._type == apMaterialSet.SHADER_PROP_TYPE.Keyword)
				{
					continue;
				}

				apSendMaskData.ReceivePropertySet newPropSet = new apSendMaskData.ReceivePropertySet();

				newPropSet._customName = info._name;

				//기본 타입을 넣어주자
				switch (info._type)
				{
					case apMaterialSet.SHADER_PROP_TYPE.Float:
						newPropSet._customPropType = apSendMaskData.SHADER_PROP_VALUE_TYPE.Value_Float;
						break;

					case apMaterialSet.SHADER_PROP_TYPE.Int:
						newPropSet._customPropType = apSendMaskData.SHADER_PROP_VALUE_TYPE.Value_Int;
						break;

					case apMaterialSet.SHADER_PROP_TYPE.Vector:
						newPropSet._customPropType = apSendMaskData.SHADER_PROP_VALUE_TYPE.Value_Vector;
						break;

					case apMaterialSet.SHADER_PROP_TYPE.Texture:
						newPropSet._customPropType = apSendMaskData.SHADER_PROP_VALUE_TYPE.Value_Texture;
						break;

					case apMaterialSet.SHADER_PROP_TYPE.Color:
						newPropSet._customPropType = apSendMaskData.SHADER_PROP_VALUE_TYPE.Value_Color;
						break;

					case apMaterialSet.SHADER_PROP_TYPE.Keyword:
						newPropSet._customPropType = apSendMaskData.SHADER_PROP_VALUE_TYPE.Value_Float;//이건 원래 안됨
						break;
				}

				selectedMaskData._propertySets.Add(newPropSet);
			}

		}

		// 함수들
		//----------------------------------------------------------------
		/// <summary>
		/// Send Mask Data 리스트를 다시 가져온다.
		/// 기존의 리스트를 그대로 유지한 상태로 갱신해야 하므로, Find/Remove 등을 사용해서 순서를 유지하자
		/// </summary>
		private void RefreshSendMaskDataList()
		{
			//선택된 SendMaskData를 별도로 저장했다가
			apSendMaskData selectedMaskData = _selectedInfo != null ? _selectedInfo._sendMaskData : null;

			SendMaskDataInfo curInfo = null;

			//재활용위한 기존 리스트 복사
			Dictionary<apSendMaskData, SendMaskDataInfo> recycleList = new Dictionary<apSendMaskData, SendMaskDataInfo>();
			int nInfos = _infos != null ? _infos.Count : 0;
			if(nInfos > 0)
			{	
				for (int i = 0; i < nInfos; i++)
				{
					curInfo = _infos[i];
					curInfo.ClearMetaData();

					if(recycleList.ContainsKey(curInfo._sendMaskData))
					{
						continue;
					}
					recycleList.Add(curInfo._sendMaskData, curInfo);
				}
			}

			//리스트 전체 초기화
			if(_infos == null) { _infos = new List<SendMaskDataInfo>(); }
			_infos.Clear();

			if(_infos_Clipping == null) { _infos_Clipping = new List<ClippingDataInfo>(); }
			_infos_Clipping.Clear();

			CollectSendMaskDataRecursive(_rootMeshGroup, _rootMeshGroup, recycleList);

			//완성된 리스트를 바탕으로 Chain 여부도 체크해야한다.
			//Chain 조건 : Send Mask Data가 이전 Phase 또는 Clipping에서 마스크를 받는 역할을 하는 경우
			nInfos = _infos.Count;
			if(nInfos > 0)
			{	
				for (int i = 0; i < nInfos; i++)
				{
					curInfo = _infos[i];
					CheckChain(curInfo);//체인 여부를 갱신한다.
					CheckShared(curInfo);//공유 여부도 갱신한다.
				}
			}

			//Depth의 역순으로 보여줘야 Hierarchy와 유사하게 보여진다.
			_infos.Reverse();

			//이 상태에서 Shared에 맞게 다시 그룹을 만든다.
			List<SendMaskDataInfo> sortedList = new List<SendMaskDataInfo>();
			if (nInfos > 0)
			{
				for (int i = 0; i < nInfos; i++)
				{
					curInfo = _infos[i];

					if(sortedList.Contains(curInfo))
					{
						//이미 다른 정보에 의해 추가된 경우 생략
						continue;
					}


					//본인 넣기
					sortedList.Add(curInfo);

					if(curInfo._sharedType == SHARED_TYPE.None)
					{
						//일반 타입이면 패스
						continue;
					}

					//공유 타입이면, 연결된 다른 리스트를 근처로 묶이도록 같이 넣기
					int nLinked = curInfo._linkedSharedInfo != null ? curInfo._linkedSharedInfo.Count : 0;
					if(nLinked > 0)
					{
						SendMaskDataInfo linkedInfo = null;
						for (int iLinked = 0; iLinked < nLinked; iLinked++)
						{
							linkedInfo = curInfo._linkedSharedInfo[iLinked];
							if(sortedList.Contains(linkedInfo))
							{
								//이미 다른 정보에 의해 추가된 경우 생략
								continue;
							}

							sortedList.Add(linkedInfo);
						}
					}
				}
			}
			
			_infos = sortedList;//리스트 교체


			//선택된거 복원
			_selectedInfo = null;

			if(selectedMaskData != null)
			{
				_selectedInfo = _infos.Find(delegate (SendMaskDataInfo a)
				{
					return a._sendMaskData == selectedMaskData;
				});
			}
		}

		/// <summary>
		/// 전체 체인 조건을 다시 검사한다.
		/// </summary>
		private void RefreshSendMaskChained()
		{
			int nInfos = _infos.Count;
			if(nInfos > 0)
			{
				SendMaskDataInfo curInfo = null;
				for (int i = 0; i < nInfos; i++)
				{
					curInfo = _infos[i];
					CheckChain(curInfo);//체인 여부를 갱신한다.
				}
			}
		}

		//Mesh Group의 자식 MeshTF를 모두 체크하여 SendMaskData를 찾자.
		//재귀적으로 호출
		private void CollectSendMaskDataRecursive(apMeshGroup curMeshGroup, apMeshGroup rootMeshGroup, Dictionary<apSendMaskData, SendMaskDataInfo> recycleList)
		{
			if (curMeshGroup == null)
			{
				return;
			}

			int nMeshTFs = curMeshGroup._childMeshTransforms != null ? curMeshGroup._childMeshTransforms.Count : 0;
			if (nMeshTFs > 0)
			{
				for (int i = 0; i < nMeshTFs; i++)
				{
					apTransform_Mesh meshTF = curMeshGroup._childMeshTransforms[i];

					//Send Mask인 경우 수집
					int nSendMaskDataList = meshTF._sendMaskDataList != null ? meshTF._sendMaskDataList.Count : 0;
					if (nSendMaskDataList > 0)
					{
						//Send Mask Data가 있다.
						//리스트에 추가하자
						for (int iSendMask = 0; iSendMask < nSendMaskDataList; iSendMask++)
						{
							apSendMaskData sendMaskData = meshTF._sendMaskDataList[iSendMask];

							SendMaskDataInfo findInfo = _infos.Find(delegate (SendMaskDataInfo a)
							{
								return a._sendMaskData == sendMaskData;
							});

							if (findInfo != null)
							{
								continue;
							}

							SendMaskDataInfo newInfo = null;
							recycleList.TryGetValue(sendMaskData, out newInfo);

							if(newInfo != null)
							{
								//리사이클 리스트에 있는 경우 재활용
								newInfo.ClearMetaData();
							}
							else
							{
								//그렇지 않다면 새로 생성
								newInfo = new SendMaskDataInfo(meshTF, sendMaskData);
							}

							_infos.Add(newInfo);
						}
					}

					//Clipping도 수집
					if(meshTF._isClipping_Parent)
					{
						ClippingDataInfo newClipInfo = new ClippingDataInfo(meshTF);
						_infos_Clipping.Add(newClipInfo);
					}
				}
			}

			//자식 MeshGroup Transform을 찾아서 재귀적으로 더 호출하자
			int nChildMeshGroups = curMeshGroup._childMeshGroupTransforms != null ? curMeshGroup._childMeshGroupTransforms.Count : 0;
			if (nChildMeshGroups > 0)
			{
				for (int i = 0; i < nChildMeshGroups; i++)
				{
					apTransform_MeshGroup childMGTF = curMeshGroup._childMeshGroupTransforms[i];
					if (childMGTF == null)
					{
						continue;
					}
					if (childMGTF._meshGroup == null || childMGTF._meshGroup == rootMeshGroup)
					{
						continue;
					}
					CollectSendMaskDataRecursive(childMGTF._meshGroup, rootMeshGroup, recycleList);
				}
			}
		}

		private void CheckChain(SendMaskDataInfo info)
		{
			apSendMaskData curMaskData = info._sendMaskData;
			apTransform_Mesh curMesh = info._meshTF;

			int iRenderOrder = (int)curMaskData._rtRenderOrder;

			info._chainType = CHAIN_TYPE.None;

			bool isChained = false;//정상적으로 체인이 하나라도 되었다면 true
			bool isWarning = false;//Phase 에러가 하나라도 있다면 true

			//이 마스크가 다른 클리핑 마스크의 자식이라면, 이건 체인된 것이다.
			//근데 Phase 1이면 에러, 그 외에는 정상
			if(curMesh._isClipping_Child)
			{
				isChained = true;
				if(curMaskData._rtRenderOrder == apSendMaskData.RT_RENDER_ORDER.Phase1)
				{
					//Phase1에서는 클리핑 타이밍과 겹친다.
					isWarning = true;
				}
			}

			int nReceive = curMesh._linkedReceivedMasks != null ? curMesh._linkedReceivedMasks.Count : 0;
			if(nReceive > 0)
			{
				apMaskLinkInfo curReceiveInfo = null;
				for (int i = 0; i < nReceive; i++)
				{
					curReceiveInfo = curMesh._linkedReceivedMasks[i];

					//Receive가 이전 Phase인지 체크하자
					int iReceiveOrder = (int)(curReceiveInfo._parentMaskData._rtRenderOrder);
					if(iReceiveOrder < iRenderOrder)
					{
						//정상적으로 체인이 되었다.
						isChained = true;
					}
					else
					{
						//렌더 순서가 올바르지 않다.
						isWarning = true;
					}
				}
			}

			if(isWarning)
			{
				//일부 체인 경고 발생
				info._chainType = CHAIN_TYPE.Warning;
			}
			else if(isChained)
			{
				//정상적으로 체인됨
				info._chainType = CHAIN_TYPE.Chained;
			}
		}

		private void CheckShared(SendMaskDataInfo info)
		{
			info._sharedType = SHARED_TYPE.None;
			if(info._linkedSharedInfo == null)
			{
				info._linkedSharedInfo = new List<SendMaskDataInfo>();
			}
			info._linkedSharedInfo.Clear();

			apSendMaskData curMaskData = info._sendMaskData;
			//apTransform_Mesh curMesh = info._meshTF;

			bool isShared = false;
			bool isWarning = false;

			if(curMaskData._isRTShared)
			{
				//Shared라면
				//모든 Shared 데이터를 링크함과 동시에 Phase가 같아야 한다.
				isShared = true;

				int sharedID = curMaskData._sharedRTID;

				int nInfos = _infos != null ? _infos.Count : 0;
				SendMaskDataInfo otherInfo = null;
				apSendMaskData otherMaskData = null;

				for (int i = 0; i < nInfos; i++)
				{
					otherInfo = _infos[i];
					if(otherInfo == info)
					{
						continue;
					}

					otherMaskData = otherInfo._sendMaskData;

					if(!otherMaskData._isRTShared
						|| otherMaskData._sharedRTID != sharedID)
					{
						continue;
					}

					//일단 연결
					info.AddSharedInfo(otherInfo);
					otherInfo.AddSharedInfo(info);

					//렌더 순서가 다르면 경고
					if(curMaskData._rtRenderOrder != otherMaskData._rtRenderOrder)
					{
						isWarning = true;
					}
				}
			}

			//결과값 입력
			if(isWarning)
			{
				//일부 공유 타이밍이 맞지 않는 문제 발생
				info._sharedType = SHARED_TYPE.Warning;
			}
			else if(isShared)
			{
				info._sharedType = SHARED_TYPE.Shared;
			}
		}



		private object _loadKey_SelectTF = null;
		/// <summary>
		/// 새로운 마스크 정보를 생성한다.
		/// </summary>
		private void MakeNewSendMaskData()
		{
			if (_editor == null
				|| _portrait == null
				|| _rootMeshGroup == null)
			{
				return;
			}

			//MeshTF를 선택하는 창을 띄우자
			//초기에 선택할 객체를 받도록 하자 (_lastSelectedTF 이거 이용)
			_loadKey_SelectTF = apDialog_SelectMultipleObjects.ShowDialog(_editor,
																		_rootMeshGroup,
																		apDialog_SelectMultipleObjects.REQUEST_TARGET.ChildMeshTransforms,
																		OnNewSendMaskTransformSelected,
																		_editor.GetText(TEXT.DLG_Select),
																		null,
																		null,
																		_lastSelectedTF);
		}

		public void OnNewSendMaskTransformSelected(bool isSuccess,
															object loadKey,
															List<object> selectedObjects,
															object savedObject)
		{
			if(!isSuccess
				|| _loadKey_SelectTF != loadKey)
			{
				_loadKey_SelectTF = null;
				return;
			}

			_loadKey_SelectTF = null;
			//선택된 객체에서 apTransform_Mesh만 찾아서 리스트로 만들자
			List<apTransform_Mesh> selectedMeshTFs = new List<apTransform_Mesh>();
			int nSelected = selectedObjects != null ? selectedObjects.Count : 0;

			if(nSelected == 0)
			{
				return;
			}

			for (int i = 0; i < nSelected; i++)
			{
				apTransform_Mesh meshTF = selectedObjects[i] as apTransform_Mesh;
				if (meshTF != null)
				{
					selectedMeshTFs.Add(meshTF);
				}
			}

			int nMeshTFs = selectedMeshTFs.Count;
			if (nMeshTFs == 0)
			{
				return;
			}

			//해당 MeshTF마다 새로운 SendMaskData를 만들자
			SetUndo();//>

			//마지막으로 추가된 것에 해당하는 Info를 선택하도록 하자
			apSendMaskData lastSendMaskData = null;



			//하나씩 돌면서 MeshTF에 MaskSendData를 추가하자
			for (int i = 0; i < nMeshTFs; i++)
			{
				apTransform_Mesh meshTF = selectedMeshTFs[i];
				if (meshTF == null)
				{
					continue;
				}

				if(meshTF._sendMaskDataList == null)
				{
					meshTF._sendMaskDataList = new List<apSendMaskData>();
				}
				apSendMaskData newMaskData = new apSendMaskData();

				//미리 기본 양식을 지정하자
				newMaskData._rtShaderType = apSendMaskData.RT_SHADER_TYPE.AlphaMask;
				newMaskData._renderTextureSize = apTransform_Mesh.RENDER_TEXTURE_SIZE.s_256;
				newMaskData._isRTSizeOptimized = true;
				newMaskData._sharedRTID = 0;
				newMaskData._isRTShared = false;

				//첫 프로퍼티를 추가한다. > 이건 생략. Shared의 경우 불필요한 프로퍼티가 될 수 있는데 괜히 추가할 필요는 없다.
				if (newMaskData._propertySets == null)
				{
					newMaskData._propertySets = new List<apSendMaskData.ReceivePropertySet>();
				}
				//apSendMaskData.ReceivePropertySet newProp = new apSendMaskData.ReceivePropertySet();
				//newProp._preset = apSendMaskData.SHADER_PROP_PRESET.AlphaMaskPreset;
				//newProp._reservedChannel = apSendMaskData.SHADER_PROP_RESERVED_CHANNEL.Channel_1;
				//newProp._value_MaskOp = apSendMaskData.MASK_OPERATION.And;

				//newMaskData._propertySets.Add(newProp);

				//리스트에 추가
				meshTF._sendMaskDataList.Add(newMaskData);

				lastSendMaskData = newMaskData;
				_lastSelectedTF = meshTF;// 이걸 저장하여 다음에 선택시 활용하자

			}
			_rootMeshGroup.LinkSendMaskData();


			//리스트 갱신을 하자
			RefreshSendMaskDataList();

			//마지막 데이터에 해당하는 Info를 선택하자
			if (lastSendMaskData != null)
			{
				_selectedInfo = _infos.Find(delegate (SendMaskDataInfo a)
				{
					return a._sendMaskData == lastSendMaskData;
				});
			}

			//에디터 Hierarchy 갱신
			RefreshEditorHierarchy();
		}



		private void SetUndo()
		{
			if(_rootMeshGroup == null)
			{
				return;
			}

			//Root Mesh Group의 모든 Child Mesh TF 갱신
			apEditorUtil.SetRecord_MeshGroup(apUndoGroupData.ACTION.MeshGroup_MaskChanged, _editor, _rootMeshGroup, false, true, apEditorUtil.UNDO_STRUCT.ValueOnly);
		}

		private void RefreshEditorHierarchy()
		{
			if(_editor != null)
			{
				if(_editor.Hierarchy_MeshGroup != null)
				{
					_editor.Hierarchy_MeshGroup.RefreshUnits();
				}
			}
		}

		//쉐이더 타입 Enum 은 인덱스가 연속적이지 않으므로, UI에 사용하기 위해 연속적인 인덱스로 변경한다.
		private int PropTypeEnumToIndex(apSendMaskData.SHADER_PROP_VALUE_TYPE propType)
		{
			switch (propType)
			{
				case apSendMaskData.SHADER_PROP_VALUE_TYPE.RenderTexture: return 0;
				case apSendMaskData.SHADER_PROP_VALUE_TYPE.ScreenSpaceOffset: return 1;
			
				case apSendMaskData.SHADER_PROP_VALUE_TYPE.MaskOp: return 2;

				case apSendMaskData.SHADER_PROP_VALUE_TYPE.CalculatedColor: return 3;
			
				//커스텀 프로퍼티
				case apSendMaskData.SHADER_PROP_VALUE_TYPE.Value_Float: return 4;
				case apSendMaskData.SHADER_PROP_VALUE_TYPE.Value_Int: return 5;
				case apSendMaskData.SHADER_PROP_VALUE_TYPE.Value_Vector: return 6;
				case apSendMaskData.SHADER_PROP_VALUE_TYPE.Value_Texture: return 7;
				case apSendMaskData.SHADER_PROP_VALUE_TYPE.Value_Color: return 8;

				case apSendMaskData.SHADER_PROP_VALUE_TYPE.RenderTexture_VR_Left: return 9;
				case apSendMaskData.SHADER_PROP_VALUE_TYPE.RenderTexture_VR_Right: return 10;
			}
			return 0;
		}

		private apSendMaskData.SHADER_PROP_VALUE_TYPE IndexToPropTypeEnum(int index)
		{
			switch (index)
			{
				case 0: return apSendMaskData.SHADER_PROP_VALUE_TYPE.RenderTexture;
				case 1: return apSendMaskData.SHADER_PROP_VALUE_TYPE.ScreenSpaceOffset;

				case 2: return apSendMaskData.SHADER_PROP_VALUE_TYPE.MaskOp;

				case 3: return apSendMaskData.SHADER_PROP_VALUE_TYPE.CalculatedColor;

				//커스텀 프로퍼티
				case 4: return apSendMaskData.SHADER_PROP_VALUE_TYPE.Value_Float;
				case 5: return apSendMaskData.SHADER_PROP_VALUE_TYPE.Value_Int;
				case 6: return apSendMaskData.SHADER_PROP_VALUE_TYPE.Value_Vector;
				case 7: return apSendMaskData.SHADER_PROP_VALUE_TYPE.Value_Texture;
				case 8: return apSendMaskData.SHADER_PROP_VALUE_TYPE.Value_Color;

				case 9: return apSendMaskData.SHADER_PROP_VALUE_TYPE.RenderTexture_VR_Left;
				case 10: return apSendMaskData.SHADER_PROP_VALUE_TYPE.RenderTexture_VR_Right;
			}
			return apSendMaskData.SHADER_PROP_VALUE_TYPE.RenderTexture;
		}


		// Get
		//--------------------------------------------------------------------
		/// <summary>
		/// 대상 MeshTF의 마스크 정보들을 가져온다.
		/// </summary>
		private List<SendMaskDataInfo> GetMaskDataInfosOfMeshTF(apTransform_Mesh targetMeshTF)
		{
			int nInfos = _infos != null ? _infos.Count : 0;
			if(nInfos == 0)
			{
				return null;
			}

			return _infos.FindAll(delegate (SendMaskDataInfo a)
			{
				return a._meshTF == targetMeshTF;
			});
		}



		

		// GUI 보조 함수들
		//--------------------------------------------------------------------
		private string GetRTShaderTypeName(apSendMaskData.RT_SHADER_TYPE rtShaderType)
		{
			switch (rtShaderType)
			{
				case apSendMaskData.RT_SHADER_TYPE.AlphaMask:
					return "Alpha Mask";

				case apSendMaskData.RT_SHADER_TYPE.MainTextureWithColor:
					return "Main Texture With Color";

				case apSendMaskData.RT_SHADER_TYPE.MainTextureOnly:
					return "Main Texture Only";

				case apSendMaskData.RT_SHADER_TYPE.CustomShader:
					return "Custom Shader";
			}

			return "Undefined Type";
		}

		private Texture2D GetRTShaderTypeIcon(apSendMaskData.RT_SHADER_TYPE rtShaderType)
		{
			//쉐이더에 따라 다른 아이콘
			switch (rtShaderType)
			{
				case apSendMaskData.RT_SHADER_TYPE.AlphaMask:
					return _icon_Data_AlphaMask;

				case apSendMaskData.RT_SHADER_TYPE.MainTextureWithColor:
					return _icon_Data_MainTexWithColor;

				case apSendMaskData.RT_SHADER_TYPE.MainTextureOnly:
					return _icon_Data_MainTexOnly;

				case apSendMaskData.RT_SHADER_TYPE.CustomShader:
					return _icon_Data_Custom;
			}
			return _icon_Data_Custom;
		}


		private Color SharedIDToColor(int sharedID)
		{
			//f (EditorGUIUtility.isProSkin)
			//			{
			//				//서브 + Pro
			//				GUI.backgroundColor = new Color(0.6f, 0.3f, 0.3f, 1.0f);
			//			}
			//			else
			//			{
			//				//서브 + Normal
			//				GUI.backgroundColor = new Color(1.0f, 0.4f, 0.4f, 1.0f);
			//			}
			//float hue = Mathf.Clamp01((float)(((sharedID + 1) * 23 + (sharedID * 3) + (sharedID / 4)) % 32) / 32.0f);
			float hue = (((sharedID * 17) + (sharedID / 4) + 7) % 12) / 12.0f;
			float bright = EditorGUIUtility.isProSkin ? 0.3f : 0.8f;
			return Color.HSVToRGB(hue, 0.6f, bright);
		}

	}
}