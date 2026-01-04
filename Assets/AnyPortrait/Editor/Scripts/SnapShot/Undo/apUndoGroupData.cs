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



	/// <summary>
	/// Unity의 Undo 기능을 사용할 때, 불필요한 호출을 막는 용도
	/// "연속된 동일한 요청"을 방지한다.
	/// 중복 체크만 하는 것이므로 1개의 값만 가진다.
	/// </summary>
	public class apUndoGroupData
	{
		// Singletone
		//---------------------------------------------------
		private static apUndoGroupData _instance = new apUndoGroupData();
		public static apUndoGroupData I { get { return _instance; } }

		// Members
		//--------------------------------------------------
		private ACTION _action = ACTION.None;

		[Flags]
		public enum SAVE_TARGET : int
		{
			None = 0,
			Portrait = 1,
			Mesh = 2,
			MeshGroup = 4,
			AllMeshGroups = 8,
			Modifier = 16,
			AllModifiers = 32,
			AllMeshes = 64
		}
		private SAVE_TARGET _saveTarget = SAVE_TARGET.None;
		private apPortrait _portrait = null;
		private apMesh _mesh = null;
		private apMeshGroup _meshGroup = null;
		private apModifierBase _modifier = null;


		//private object _keyObject = null;//키 오브젝트는 삭제. 사용하는 일이 거의 없다.
		private bool _isCallContinuous = false;//여러 항목을 연속으로 처리하는 Batch 액션 중인가

		private bool _lastUndoTimeRecorded = false;
		private DateTime _lastUndoTime = new DateTime();
		//private bool _isFirstAction = true;

		private const float CONT_SAVE_TIME = 5.0f;//이전 : 1초마다 Cont 작업을 분절해서 Undo > 5초로 변경 (21.7.17)

		public enum ACTION
		{
			None,
			Main_AddImage,
			Main_RemoveImage,
			Main_AddMesh,
			Main_RemoveMesh,
			Main_AddMeshGroup,
			Main_RemoveMeshGroup,
			Main_AddAnimation,
			Main_RemoveAnimation,
			Main_AddParam,
			Main_RemoveParam,

			Main_DuplicateMesh,
			Main_DuplicateMeshGroup,
			

			Portrait_SettingChanged,
			Portrait_BakeOptionChanged,
			Portrait_SetMeshGroup,
			Portrait_ReleaseMeshGroup,
			Portrait_InternalChanged,
			Portrait_Bake,

			Image_SettingChanged,

			Image_PSDImport,

			MeshEdit_AddVertex,
			MeshEdit_EditVertex,
			MeshEdit_EditVertexDepth,
			MeshEdit_RemoveVertex,
			MeshEdit_ResetVertices,
			MeshEdit_RemoveAllVertices,
			MeshEdit_AddEdge,
			MeshEdit_EditEdge,
			MeshEdit_RemoveEdge,
			MeshEdit_MakeEdges,
			MeshEdit_EditPolygons,
			MeshEdit_SetImage,
			MeshEdit_SetPivot,
			MeshEdit_SettingChanged,
			MeshEdit_AtlasChanged,
			MeshEdit_AutoGen,
			MeshEdit_VertexCopied,
			MeshEdit_VertexMoved,
			MeshEdit_FFDStart,
			MeshEdit_FFDAdapt,
			MeshEdit_FFDRevert,

			MeshEdit_AddPin,
			MeshEdit_MovePin,
			MeshEdit_MovePin_Rotate,
			MeshEdit_MovePin_Scale,
			MeshEdit_ChangePin,
			MeshEdit_CalculatePinWeight,
			MeshEdit_RemovePin,

			MeshGroup_AttachMesh,
			MeshGroup_AttachMeshGroup,
			MeshGroup_DetachMesh,
			MeshGroup_DetachMeshGroup,
			MeshGroup_ClippingChanged,
			MeshGroup_MaskChanged,
			MeshGroup_DepthChanged,
			MeshGroup_AddBone,
			MeshGroup_RemoveBone,
			MeshGroup_RemoveAllBones,
			MeshGroup_BoneSettingChanged,
			MeshGroup_BoneIKRangeChanged,
			MeshGroup_BoneJiggleRangeChanged,
			MeshGroup_BoneDefaultEdit,
			MeshGroup_AttachBoneToChild,
			MeshGroup_DetachBoneFromChild,
			MeshGroup_SetBoneAsParent,
			MeshGroup_SetBoneAsIKTarget,
			MeshGroup_AddBoneFromRetarget,
			MeshGroup_BoneIKControllerChanged,
			MeshGroup_BoneMirrorChanged,

			MeshGroup_DuplicateMeshTransform,
			MeshGroup_DuplicateMeshGroupTransform,
			MeshGroup_DuplicateBone,


			MeshGroup_Gizmo_MoveTransform,
			MeshGroup_Gizmo_RotateTransform,
			MeshGroup_Gizmo_ScaleTransform,
			MeshGroup_Gizmo_Color,

			MeshGroup_AddModifier,
			MeshGroup_RemoveModifier,
			MeshGroup_RemoveParamSet,
			MeshGroup_RemoveParamSetGroup,

			MeshGroup_DefaultSettingChanged,
			MeshGroup_MigrateMeshTransform,


			Modifier_LinkControlParam,
			Modifier_UnlinkControlParam,
			Modifier_AddStaticParamSetGroup,

			Modifier_LayerChanged,
			Modifier_SettingChanged,
			Modifier_SetBoneWeight,
			Modifier_RemoveBoneWeight,
			Modifier_RemoveBoneRigging,
			Modifier_RemovePhysics,
			Modifier_SetPhysicsWeight,
			Modifier_SetVolumeWeight,
			Modifier_SetPhysicsProperty,

			Modifier_ExtraOptionChanged,

			Modifier_Gizmo_MoveTransform,
			Modifier_Gizmo_RotateTransform,
			Modifier_Gizmo_ScaleTransform,
			Modifier_Gizmo_BoneIKTransform,
			Modifier_Gizmo_MoveVertex,
			Modifier_Gizmo_RotateVertex,
			Modifier_Gizmo_ScaleVertex,
			Modifier_Gizmo_FFDVertex,
			Modifier_Gizmo_Color,
			Modifier_Gizmo_BlurVertex,

			Modifier_Gizmo_MovePin,
			Modifier_Gizmo_RotatePin,
			Modifier_Gizmo_ScalePin,
			Modifier_Gizmo_FFDPin,
			Modifier_Gizmo_BlurPin,

			Modifier_ModMeshValuePaste,
			Modifier_ModMeshValueReset,
			Modifier_AddModMeshToParamSet,
			Modifier_RemoveModMeshFromParamSet,

			Modifier_RiggingWeightChanged,

			Modifier_FFDStart,
			Modifier_FFDAdapt,
			Modifier_FFDRevert,

			Anim_SetMeshGroup,
			Anim_DupAnimClip,
			Anim_ImportAnimClip,
			Anim_AddTimeline,
			Anim_RemoveTimeline,
			Anim_AddTimelineLayer,
			Anim_RemoveTimelineLayer,
			Anim_AddKeyframe,
			Anim_MoveKeyframe,
			Anim_CopyKeyframe,
			Anim_RemoveKeyframe,
			Anim_DupKeyframe,
			Anim_KeyframeValueChanged,
			Anim_AddEvent,
			Anim_RemoveEvent,
			Anim_SortEvents,
			Anim_EventChanged,

			Anim_Gizmo_MoveTransform,
			Anim_Gizmo_RotateTransform,
			Anim_Gizmo_ScaleTransform,
			Anim_Gizmo_BoneIKControllerTransform,

			Anim_Gizmo_MoveVertex,
			Anim_Gizmo_RotateVertex,
			Anim_Gizmo_ScaleVertex,
			Anim_Gizmo_FFDVertex,
			Anim_Gizmo_BlurVertex,

			Anim_Gizmo_Color,

			Anim_SettingChanged,

			ControlParam_SettingChanged,
			ControlParam_Duplicated,

			Retarget_ImportSinglePoseToMod,
			Retarget_ImportSinglePoseToAnim,

			PSDSet_AddNewPSDSet,

			MaterialSetAdded,
			MaterialSetRemoved,
			MaterialSetChanged,

			VisibilityChanged,
			GuidelineChanged,

			MonoChanged,
		}

		private Dictionary<ACTION, string> _undoLabels = null;


		public static string GetLabel(ACTION action)
		{
			return I._undoLabels[action];
		}



		//v1.4.2 : 해당 프레임에서의 Undo는 기능에 상관없이 구분하지 않고 처리한다.
		//또한 Continuous에 대해서도 다음과 같이 변경한다.		
		//- OnGUI 시작시 Undo 기록을 받기 위한 준비를 한다.
		//- Undo가 발생하면
		// > 이 프레임에서 Undo가 처음 발생했다면 Group ID를 새로 생성한다. 그 이후엔 해당 ID에 모두 병합된다.
		// > 이 프레임에서 Undo가 추가로 발생했다면, Group ID를 생성하지 않고 Undo만 기록한다.
		//   >> Object 리스트를 이용하여 중복 기록은 하지 않도록 막는다.
		//   >> 생성/삭제를 하고자 할 땐 Object 리스트에서 비교하지 않는다. 삭제시엔 Object 리스트에서 제거한다.
		// > Continuous라면 Undo 자체를 발생시키지 않는다. 다만, 이전/지금의 요청이 복수개의 Undo 요청이었다면, 이건 Continuous로 간주하지 않고 다 기록한다.
		private bool _isRecording = false;//하나라도 Undo 요청이 들어왔다면 True가 된다.
		//private bool _isContinuousRecording = false;//Undo가 Continuous로 시작되었는가

		private bool _isUndoIDAssigned = false;//ID를 할당받았는가
		private int _curUndoID = -1;

		
		//private List<UnityEngine.Object> _curRecordingObjects = null;
		private HashSet<UnityEngine.Object> _recordingObjects_Value = new HashSet<UnityEngine.Object>();
		private HashSet<UnityEngine.Object> _recordingObjects_Complete = new HashSet<UnityEngine.Object>();

		//기본 Value Only이며 더 강한 StructChanged가 하나라도 들어오면 그 값을 사용한다.
		private apEditorUtil.UNDO_STRUCT _curStructChangedType = apEditorUtil.UNDO_STRUCT.ValueOnly;
		private string _curGroupName = "";
		private const string EMPTY_NAME = "";

		//삭제 v1.6.0. 한 기능 단위의 처리 내에서는 Undo는 모두 같은 Group ID를 사용한다.
		////Undo 요청 타입. 완전히 다른 타입의 액션인 경우엔 무조건 Group ID가 분리되어 구분되어야 한다.
		//private enum UNDO_REQUEST_TYPE
		//{
		//	None,
		//	PropertyChanged,
		//	CreateOrDestroy
		//}
		//private UNDO_REQUEST_TYPE _curUndoRequestType = UNDO_REQUEST_TYPE.None;


		// Init
		//--------------------------------------------------
		private apUndoGroupData()
		{
			_lastUndoTimeRecorded = false;
			_lastUndoTime = DateTime.Now;
			//_isFirstAction = true;

			_undoLabels = new Dictionary<ACTION, string>();

			//중요 : 텍스트를 추가한다. (21.1.25)

			_undoLabels.Add(ACTION.None, "None");

			_undoLabels.Add(ACTION.Main_AddImage, "Add Image");
			_undoLabels.Add(ACTION.Main_RemoveImage, "Remove Image");
			_undoLabels.Add(ACTION.Main_AddMesh, "Add Mesh");
			_undoLabels.Add(ACTION.Main_RemoveMesh, "Remove Mesh");
			_undoLabels.Add(ACTION.Main_AddMeshGroup, "Add MeshGroup");
			_undoLabels.Add(ACTION.Main_RemoveMeshGroup, "Remove MeshGroup");
			_undoLabels.Add(ACTION.Main_AddAnimation, "Add Animation");
			_undoLabels.Add(ACTION.Main_RemoveAnimation, "Remove Animation");
			_undoLabels.Add(ACTION.Main_AddParam, "Add Parameter");
			_undoLabels.Add(ACTION.Main_RemoveParam, "Remove Parameter");

			_undoLabels.Add(ACTION.Main_DuplicateMesh, "Duplicate Mesh");
			_undoLabels.Add(ACTION.Main_DuplicateMeshGroup, "Duplicate MeshGroup");

			_undoLabels.Add(ACTION.Portrait_SettingChanged, "Portrait Setting Changed");
			_undoLabels.Add(ACTION.Portrait_BakeOptionChanged, "Bake Option Changed");
			_undoLabels.Add(ACTION.Portrait_SetMeshGroup, "Set Main MeshGroup");
			_undoLabels.Add(ACTION.Portrait_ReleaseMeshGroup, "Release Main MeshGroup");
			_undoLabels.Add(ACTION.Portrait_InternalChanged, "Portrait Internal Changed");
			_undoLabels.Add(ACTION.Portrait_Bake, "Bake");

			_undoLabels.Add(ACTION.Image_SettingChanged, "Set Image Property");
			_undoLabels.Add(ACTION.Image_PSDImport, "Import PSD");

			_undoLabels.Add(ACTION.MeshEdit_AddVertex, "Add Vertex");
			_undoLabels.Add(ACTION.MeshEdit_EditVertex, "Edit Vertex");
			_undoLabels.Add(ACTION.MeshEdit_EditVertexDepth, "Edit Vertex Settings");

			_undoLabels.Add(ACTION.MeshEdit_RemoveVertex, "Remove Vertex");
			_undoLabels.Add(ACTION.MeshEdit_ResetVertices, "Reset Vertices");
			_undoLabels.Add(ACTION.MeshEdit_RemoveAllVertices, "Remove All Vertices");
			_undoLabels.Add(ACTION.MeshEdit_AddEdge, "Add Edge");
			_undoLabels.Add(ACTION.MeshEdit_EditEdge, "Edit Edge");
			_undoLabels.Add(ACTION.MeshEdit_RemoveEdge, "Remove Edge");
			_undoLabels.Add(ACTION.MeshEdit_MakeEdges, "Make Edges");
			_undoLabels.Add(ACTION.MeshEdit_EditPolygons, "Edit Polygons");
			_undoLabels.Add(ACTION.MeshEdit_SetImage, "Set Image");
			_undoLabels.Add(ACTION.MeshEdit_SetPivot, "Set Mesh Pivot");
			_undoLabels.Add(ACTION.MeshEdit_SettingChanged, "Mesh Setting Changed");
			_undoLabels.Add(ACTION.MeshEdit_AtlasChanged, "Mesh Atals Changed");
			_undoLabels.Add(ACTION.MeshEdit_AutoGen, "Vertices Generated");
			_undoLabels.Add(ACTION.MeshEdit_VertexCopied, "Vertices Copied");
			_undoLabels.Add(ACTION.MeshEdit_VertexMoved, "Vertices Moved");
			_undoLabels.Add(ACTION.MeshEdit_FFDStart, "Edit FFD");
			_undoLabels.Add(ACTION.MeshEdit_FFDAdapt, "Adapt FFD");
			_undoLabels.Add(ACTION.MeshEdit_FFDRevert, "Revert FFD");
			

			_undoLabels.Add(ACTION.MeshEdit_AddPin, "Add Pin");
			_undoLabels.Add(ACTION.MeshEdit_MovePin, "Move Pin");
			_undoLabels.Add(ACTION.MeshEdit_MovePin_Rotate, "Move Pin (Rotate)");
			_undoLabels.Add(ACTION.MeshEdit_MovePin_Scale, "Move Pin (Scale)");
			_undoLabels.Add(ACTION.MeshEdit_ChangePin, "Change Pin Properties");
			_undoLabels.Add(ACTION.MeshEdit_CalculatePinWeight, "Calculate Weights");
			_undoLabels.Add(ACTION.MeshEdit_RemovePin, "Remove Pin");

			_undoLabels.Add(ACTION.MeshGroup_AttachMesh, "Attach Mesh");
			_undoLabels.Add(ACTION.MeshGroup_AttachMeshGroup, "Attach MeshGroup");
			_undoLabels.Add(ACTION.MeshGroup_DetachMesh, "Detach Mesh");
			_undoLabels.Add(ACTION.MeshGroup_DetachMeshGroup, "Detach MeshGroup");
			_undoLabels.Add(ACTION.MeshGroup_ClippingChanged, "Clipping Changed");
			_undoLabels.Add(ACTION.MeshGroup_MaskChanged, "Mask Changed");
			_undoLabels.Add(ACTION.MeshGroup_DepthChanged, "Depth Changed");

			_undoLabels.Add(ACTION.MeshGroup_AddBone, "Add Bone");
			_undoLabels.Add(ACTION.MeshGroup_RemoveBone, "Remove Bone");
			_undoLabels.Add(ACTION.MeshGroup_RemoveAllBones, "Remove All Bones");
			_undoLabels.Add(ACTION.MeshGroup_BoneSettingChanged, "Bone Setting Changed");
			_undoLabels.Add(ACTION.MeshGroup_BoneIKRangeChanged, "Bone IK Range Changed");
			_undoLabels.Add(ACTION.MeshGroup_BoneJiggleRangeChanged, "Jiggle Range Changed");
			_undoLabels.Add(ACTION.MeshGroup_BoneDefaultEdit, "Bone Edit");
			_undoLabels.Add(ACTION.MeshGroup_AttachBoneToChild, "Attach Bone to Child");
			_undoLabels.Add(ACTION.MeshGroup_DetachBoneFromChild, "Detach Bone from Child");
			_undoLabels.Add(ACTION.MeshGroup_SetBoneAsParent, "Set Bone as Parent");
			_undoLabels.Add(ACTION.MeshGroup_SetBoneAsIKTarget, "Set Bone as IK target");
			_undoLabels.Add(ACTION.MeshGroup_AddBoneFromRetarget, "Add Bones from File");
			_undoLabels.Add(ACTION.MeshGroup_BoneIKControllerChanged, "IK Controller Changed");
			_undoLabels.Add(ACTION.MeshGroup_BoneMirrorChanged, "Mirror Changed");

			_undoLabels.Add(ACTION.MeshGroup_DuplicateMeshTransform, "Duplicate Mesh Transform");
			_undoLabels.Add(ACTION.MeshGroup_DuplicateMeshGroupTransform, "Duplicate Mesh Group Transform");
			_undoLabels.Add(ACTION.MeshGroup_DuplicateBone, "Duplicate Bone");

			_undoLabels.Add(ACTION.MeshGroup_Gizmo_MoveTransform, "Default Position");
			_undoLabels.Add(ACTION.MeshGroup_Gizmo_RotateTransform, "Default Rotation");
			_undoLabels.Add(ACTION.MeshGroup_Gizmo_ScaleTransform, "Default Scaling");
			_undoLabels.Add(ACTION.MeshGroup_Gizmo_Color, "Default Color");

			_undoLabels.Add(ACTION.MeshGroup_AddModifier, "Add Modifier");
			_undoLabels.Add(ACTION.MeshGroup_RemoveModifier, "Remove Modifier");
			_undoLabels.Add(ACTION.MeshGroup_RemoveParamSet, "Remove Modified Key");
			_undoLabels.Add(ACTION.MeshGroup_RemoveParamSetGroup, "Remove Modified Set of Keys");

			_undoLabels.Add(ACTION.MeshGroup_DefaultSettingChanged, "Default Setting Changed");
			_undoLabels.Add(ACTION.MeshGroup_MigrateMeshTransform, "Migrate Mesh Transform");

			_undoLabels.Add(ACTION.Modifier_LinkControlParam, "Link Control Parameter");
			_undoLabels.Add(ACTION.Modifier_UnlinkControlParam, "Unlink Control Parameter");
			_undoLabels.Add(ACTION.Modifier_AddStaticParamSetGroup, "Add StaticPSG");

			_undoLabels.Add(ACTION.Modifier_LayerChanged, "Change Layer Order");
			_undoLabels.Add(ACTION.Modifier_SettingChanged, "Change Layer Setting");
			_undoLabels.Add(ACTION.Modifier_SetBoneWeight, "Set Bone Weight");
			_undoLabels.Add(ACTION.Modifier_RemoveBoneWeight, "Remove Bone Weight");
			_undoLabels.Add(ACTION.Modifier_RemoveBoneRigging, "Remove Bone Rigging");
			_undoLabels.Add(ACTION.Modifier_RemovePhysics, "Remove Physics");
			_undoLabels.Add(ACTION.Modifier_SetPhysicsWeight, "Set Physics Weight");
			_undoLabels.Add(ACTION.Modifier_SetVolumeWeight, "Set Volume Weight");
			_undoLabels.Add(ACTION.Modifier_SetPhysicsProperty, "Set Physics Property");

			_undoLabels.Add(ACTION.Modifier_ExtraOptionChanged, "Extra Option Changed");

			_undoLabels.Add(ACTION.Modifier_Gizmo_MoveTransform, "Move Transform");
			_undoLabels.Add(ACTION.Modifier_Gizmo_RotateTransform, "Rotate Transform");
			_undoLabels.Add(ACTION.Modifier_Gizmo_ScaleTransform, "Scale Transform");
			_undoLabels.Add(ACTION.Modifier_Gizmo_BoneIKTransform, "FK/IK Weight Changed");
			_undoLabels.Add(ACTION.Modifier_Gizmo_MoveVertex, "Move Vertex");
			_undoLabels.Add(ACTION.Modifier_Gizmo_RotateVertex, "Rotate Vertex");
			_undoLabels.Add(ACTION.Modifier_Gizmo_ScaleVertex, "Scale Vertex");
			_undoLabels.Add(ACTION.Modifier_Gizmo_FFDVertex, "Freeform Vertices");
			_undoLabels.Add(ACTION.Modifier_Gizmo_Color, "Set Color");
			_undoLabels.Add(ACTION.Modifier_Gizmo_BlurVertex, "Blur Vertices");

			_undoLabels.Add(ACTION.Modifier_Gizmo_MovePin, "Move Pin");
			_undoLabels.Add(ACTION.Modifier_Gizmo_RotatePin, "Rotate Pin");
			_undoLabels.Add(ACTION.Modifier_Gizmo_ScalePin, "Scale Pin");
			_undoLabels.Add(ACTION.Modifier_Gizmo_FFDPin, "Freeform Pins");
			_undoLabels.Add(ACTION.Modifier_Gizmo_BlurPin, "Blur Pins");

			_undoLabels.Add(ACTION.Modifier_ModMeshValuePaste, "Paste Modified Value");
			_undoLabels.Add(ACTION.Modifier_ModMeshValueReset, "Reset Modified Value");

			_undoLabels.Add(ACTION.Modifier_AddModMeshToParamSet, "Add To Key");
			_undoLabels.Add(ACTION.Modifier_RemoveModMeshFromParamSet, "Remove From Key");

			_undoLabels.Add(ACTION.Modifier_RiggingWeightChanged, "Weight Changed");

			_undoLabels.Add(ACTION.Modifier_FFDStart, "Edit FFD");
			_undoLabels.Add(ACTION.Modifier_FFDAdapt, "Adapt FFD");
			_undoLabels.Add(ACTION.Modifier_FFDRevert, "Revert FFD");

			_undoLabels.Add(ACTION.Anim_SetMeshGroup, "Set MeshGroup");
			_undoLabels.Add(ACTION.Anim_DupAnimClip, "Duplicate AnimClip");
			_undoLabels.Add(ACTION.Anim_ImportAnimClip, "Import AnimClip");
			_undoLabels.Add(ACTION.Anim_AddTimeline, "Add Timeline");
			_undoLabels.Add(ACTION.Anim_RemoveTimeline, "Remove Timeline");
			_undoLabels.Add(ACTION.Anim_AddTimelineLayer, "Add Timeline Layer");
			_undoLabels.Add(ACTION.Anim_RemoveTimelineLayer, "Remove Timeline Layer");

			_undoLabels.Add(ACTION.Anim_AddKeyframe, "Add Keyframe");
			_undoLabels.Add(ACTION.Anim_MoveKeyframe, "Move Keyframe");
			_undoLabels.Add(ACTION.Anim_CopyKeyframe, "Copy Keyframe");
			_undoLabels.Add(ACTION.Anim_RemoveKeyframe, "Remove Keyframe");
			_undoLabels.Add(ACTION.Anim_DupKeyframe, "Duplicate Keyframe");

			_undoLabels.Add(ACTION.Anim_KeyframeValueChanged, "Keyframe Value Changed");
			_undoLabels.Add(ACTION.Anim_AddEvent, "Event Added");
			_undoLabels.Add(ACTION.Anim_RemoveEvent, "Event Removed");
			_undoLabels.Add(ACTION.Anim_EventChanged, "Event Changed");
			_undoLabels.Add(ACTION.Anim_SortEvents, "Events Sorted");

			_undoLabels.Add(ACTION.Anim_Gizmo_MoveTransform, "Move Transform");
			_undoLabels.Add(ACTION.Anim_Gizmo_RotateTransform, "Rotate Transform");
			_undoLabels.Add(ACTION.Anim_Gizmo_ScaleTransform, "Scale Transform");
			_undoLabels.Add(ACTION.Anim_Gizmo_BoneIKControllerTransform, "FK/IK Weight Changed");

			_undoLabels.Add(ACTION.Anim_Gizmo_MoveVertex, "Move Vertex");
			_undoLabels.Add(ACTION.Anim_Gizmo_RotateVertex, "Rotate Vertex");
			_undoLabels.Add(ACTION.Anim_Gizmo_ScaleVertex, "Scale Vertex");
			_undoLabels.Add(ACTION.Anim_Gizmo_FFDVertex, "Freeform Vertices");
			_undoLabels.Add(ACTION.Anim_Gizmo_BlurVertex, "Blur Vertices");
			_undoLabels.Add(ACTION.Anim_Gizmo_Color, "Set Color");
			_undoLabels.Add(ACTION.Anim_SettingChanged, "Animation Setting Changed");

			_undoLabels.Add(ACTION.ControlParam_SettingChanged, "Control Param Setting");
			_undoLabels.Add(ACTION.ControlParam_Duplicated, "Control Param Duplicated");

			_undoLabels.Add(ACTION.Retarget_ImportSinglePoseToMod, "Import Pose");
			_undoLabels.Add(ACTION.Retarget_ImportSinglePoseToAnim, "Import Pose");

			_undoLabels.Add(ACTION.PSDSet_AddNewPSDSet, "New PSD Set");

			_undoLabels.Add(ACTION.MaterialSetAdded, "Material Set Added");
			_undoLabels.Add(ACTION.MaterialSetRemoved, "Material Set Removed");
			_undoLabels.Add(ACTION.MaterialSetChanged, "Material Set Changed");

			_undoLabels.Add(ACTION.VisibilityChanged, "Visibility Changed");
			_undoLabels.Add(ACTION.GuidelineChanged, "Guideline Option Changed");

			_undoLabels.Add(ACTION.MonoChanged, "Mono-Object Changed");



			_isRecording = false;
			
			//_isContinuousRecording = false;

			_isUndoIDAssigned = false;
			_curUndoID = -1;
			// if (_curRecordingObjects == null)
			// {
			// 	_curRecordingObjects = new List<UnityEngine.Object>();
			// }
			// _curRecordingObjects.Clear();

			if(_recordingObjects_Value == null)
			{
				_recordingObjects_Value = new HashSet<UnityEngine.Object>();
			}
			_recordingObjects_Value.Clear();

			if(_recordingObjects_Complete == null)
			{
				_recordingObjects_Complete = new HashSet<UnityEngine.Object>();
			}
			_recordingObjects_Complete.Clear();

			_curStructChangedType = apEditorUtil.UNDO_STRUCT.ValueOnly;
			_curGroupName = EMPTY_NAME;
			//_curUndoRequestType = UNDO_REQUEST_TYPE.None;

		}

		public void Clear()
		{
			_action = ACTION.None;
			_saveTarget = SAVE_TARGET.None;
			_portrait = null;
			_mesh = null;
			_meshGroup = null;
			_modifier = null;

			//_keyObject = null;
			_isCallContinuous = false;//여러 항목을 동시에 처리하는 Batch 액션 중인가

			_lastUndoTimeRecorded = false;
			_lastUndoTime = DateTime.Now;

			_isRecording = false;
			_isCallContinuous = false;

			_isUndoIDAssigned = false;
			_curUndoID = -1;

			// if (_curRecordingObjects == null)
			// {
			// 	_curRecordingObjects = new List<UnityEngine.Object>();
			// }
			// _curRecordingObjects.Clear();

			if(_recordingObjects_Value == null)
			{
				_recordingObjects_Value = new HashSet<UnityEngine.Object>();
			}
			_recordingObjects_Value.Clear();

			if(_recordingObjects_Complete == null)
			{
				_recordingObjects_Complete = new HashSet<UnityEngine.Object>();
			}
			_recordingObjects_Complete.Clear();

			_curStructChangedType = apEditorUtil.UNDO_STRUCT.ValueOnly;
			_curGroupName = EMPTY_NAME;
			//_curUndoRequestType = UNDO_REQUEST_TYPE.None;
		}




		// Functions
		//--------------------------------------------------
		public void ReadyToUndo()
		{
			_isRecording = false;
			//_isContinuousRecording = false;

			_isUndoIDAssigned = false;
			_curUndoID = -1;

			// if (_curRecordingObjects == null)
			// {
			// 	_curRecordingObjects = new List<UnityEngine.Object>();
			// }
			// _curRecordingObjects.Clear();

			if(_recordingObjects_Value == null) { _recordingObjects_Value = new HashSet<UnityEngine.Object>(); }
			_recordingObjects_Value.Clear();

			if(_recordingObjects_Complete == null) { _recordingObjects_Complete = new HashSet<UnityEngine.Object>(); }
			_recordingObjects_Complete.Clear();

			_curStructChangedType = apEditorUtil.UNDO_STRUCT.ValueOnly;
			_curGroupName = EMPTY_NAME;
			//_curUndoRequestType = UNDO_REQUEST_TYPE.None;
		}

		public void EndUndo()
		{
			if (!_isRecording)
			{
				//Undo 기록이 시작된게 아니라면
				return;
			}

			if(!_isUndoIDAssigned)
			{
				//ID가 Assign이 안되었다면 
				//= StartUndo는 호출되었지만 RecordObject는 호출되지 않았다.
				StoreUndoID();
			}

			//기록한 것을 Hitory에 남기자 < 중요 >
			//이 함수 안에 UndoID에 의한 Collapse가 포함되어있다.
			apUndoHistory.I.AddRecord(	_curUndoID,
										_curGroupName,
										_curStructChangedType == apEditorUtil.UNDO_STRUCT.StructChanged);

			//Collapse를 하자
			Undo.CollapseUndoOperations(_curUndoID);//이전 : v1.5.0

			//Debug.Log("Undo 기록 : " + _curUndoID + " / " + _curGroupName + " / " + _curStructChangedType);

			//Recording 상태를 초기화하자
			_isRecording = false;
			//_isContinuousRecording = false;

			_isUndoIDAssigned = false;
			_curUndoID = -1;
			//_curUndoRequestType = UNDO_REQUEST_TYPE.None;

			if(_recordingObjects_Value == null) { _recordingObjects_Value = new HashSet<UnityEngine.Object>(); }
			_recordingObjects_Value.Clear();

			if(_recordingObjects_Complete == null) { _recordingObjects_Complete = new HashSet<UnityEngine.Object>(); }
			_recordingObjects_Complete.Clear();
		}

		/// <summary>
		/// End를 강제로 호출한 후 다시 Record를 하자. Continuos도 해제한다.
		/// </summary>
		public void RestartUndo()
		{
			if(_isRecording)
			{
				//디버그
				//Debug.Log("(Restart Undo)");
				//DebugUndoRecord();

				if(!_isUndoIDAssigned)
				{
					//ID가 Assign이 안되었다면 
					//= StartUndo는 호출되었지만 RecordObject는 호출되지 않았다.
					StoreUndoID();
				}

				//Collapse를 하자.
				apUndoHistory.I.AddRecord(	_curUndoID, 
											_curGroupName,
											_curStructChangedType == apEditorUtil.UNDO_STRUCT.StructChanged);

				//Collapse를 하자
				Undo.CollapseUndoOperations(_curUndoID);//이전 : v1.5.0

				//Continuous를 리셋한다.
				ResetContinuous();
			}

			//값을 초기화하여 다시 Record를 할 준비를 하자
			ReadyToUndo();
		}



		// Undo 기록하기 < 단순 속성 변화 >
		public void StartRecording(	ACTION action,
									apPortrait portrait,
									apMesh mesh,
									apMeshGroup meshGroup,
									apModifierBase modifier,
									apEditorUtil.UNDO_STRUCT structChanged,
									bool isCallContinuous,
									SAVE_TARGET saveTarget,
									out bool isSkipResult//v1.6.0 : 이 값이 true면 Undo하지 않는다.
									)
		{
			//Record가 활성화된 상태에서 재시작을 해야하는 경우
			//- Undo 처리 타입이 Create/Destroy가 진행중이었던 경우
			//- Continuous 타입이었던 경우 (Continuous는 중첩된 Undo가 불가하다)

			//v1.6.0 : 변경
			//- Continuous에 따라서만 Undo 생략을 결정한다.
			//- 타입에 따라서 Undo를 나누지 않는다.
			//- 이러한 변경점으로 인하여 CheckNewAction은 삭제되었다.
			isSkipResult = false;

			//이미 다른 처리로 인하여 Recording 중이라면 연속 요청은 의미가 없다.
			//또는 구조가 변경된 경우에도 연속 요청은 무효하다.
			if(_isRecording || structChanged == apEditorUtil.UNDO_STRUCT.StructChanged)
			{
				isCallContinuous = false;
			}

			//알아서 Gizmo의 첫 입력시에만 들어온다. 이렇다면 굳이 Continuous에서 Skip을 처리할 필요가 없다.
			//if(isCallContinuous)
			//{
			//	//연속 액션 요청이라면 Undo를 스킵할 수도 있다.
			//	bool isContSkip = IsSkippableContinuousAction(action, portrait, mesh, meshGroup, modifier, saveTarget);
			//	if (isContSkip)
			//	{
			//		//이전 프레임과 연속된 Undo 요청이다. Undo하지 않는다.
			//		isSkipResult = true;
			//		Debug.Log(">> Skip Undo : " + action + " / " + _curGroupName);
			//		return;
			//	}
			//}

			//Undo 액션을 시작하자
			if(!_isRecording)
			{
				//첫 액션이라면
				_isRecording = true;
				_curStructChangedType = structChanged;
				_curGroupName = GetLabel(action);

				_isUndoIDAssigned = false;
				_curUndoID = -1;

				StoreUndoAction(action, portrait, mesh, meshGroup, modifier, isCallContinuous, saveTarget);
			}
			else
			{
				//만약 액션 중이었는데 요청이 추가되었다면 기존의 Continuous는 무조건 해제된다.
				ResetContinuous();
				
				//Struct 타입은 갱신될 수 있다 (Value > Struct Changed
				if(structChanged == apEditorUtil.UNDO_STRUCT.StructChanged)
				{
					_curStructChangedType = apEditorUtil.UNDO_STRUCT.StructChanged;
				}
			}


			//삭제 v1.6.0 : 쓸데없이 복잡함
			//if (_isRecording)
			//{
			//	if (_curUndoRequestType == UNDO_REQUEST_TYPE.CreateOrDestroy
			//		|| _isContinuousRecording)
			//	{
			//		//기존의 Undo를 종료하고 재시작한다.
			//		RestartUndo();
			//	}
			//}


			////현재 Undo 방식은 Property 변경 방식이다.
			//_curUndoRequestType = UNDO_REQUEST_TYPE.PropertyChanged;

			////Continuous는 StructChanged 요청인 경우엔 허용되지 않는다.
			//if(structChanged == apEditorUtil.UNDO_STRUCT.StructChanged)
			//{
			//	isCallContinuous = false;
			//}

			//if(!_isRecording)
			//{
			//	// < 아직 기록이 되지 않은 상태 >
			//	//새롭게 ID를 만드는 단계이다.
			//	//단 Continuous라면 ID 생성을 생략한다. (ID는 현재 상태의 값에 병합한다.)
			//	bool isNewAction = CheckNewAction(action, portrait, mesh, meshGroup, modifier, isCallContinuous, saveTarget);

			//	_isRecording = true;
				
			//	_curStructChangedType = structChanged;
			//	_curGroupName = GetLabel(action);

			//	if(isNewAction)
			//	{
			//		//분절된 새로운 액션이다.
			//		_isContinuousRecording = false;
			//		//Undo.IncrementCurrentGroup();//Group ID를 증가시킨다. > 삭제 v1.5.0
			//		Undo.SetCurrentGroupName(_curGroupName);

			//		//Debug.Log("+Undo ID : " + Undo.GetCurrentGroup());
			//	}
			//	else
			//	{
			//		_isContinuousRecording = true;//이 요청은 Continuous 타입이다.
			//	}
			//}
			//else
			//{
			//	// < 이전에 이미 기록이 된 상태 >
			//	//ID를 생성하지 않은 상태에서 일부 상태값을 갱신한다.

			//	//기록 추가시에는 Continuous는 무조건 해제된다.
			//	ResetContinuous();

			//	_isRecording = true;
			//	_isContinuousRecording = false;//<<Continuous는 해제

			//	//ValueOnly보다 더 처리 강도가 높은 Struct Changed로의 전환만 가능하다.
			//	if(structChanged == apEditorUtil.UNDO_STRUCT.StructChanged)
			//	{	
			//		_curStructChangedType = apEditorUtil.UNDO_STRUCT.StructChanged;
			//	}
			//}


			if(portrait != null)
			{
				EditorUtility.SetDirty(portrait);
			}
		}

		public void StoreUndoID()
		{
			//Debug.Log("Set Undo ID : " + undoID);
			if(!_isUndoIDAssigned)
			{
				//병합을 위해 ID를 할당받는다.
				_curUndoID = Undo.GetCurrentGroup();
				_isUndoIDAssigned = true;
			}
			
		}

		public int GetUndoID()
		{
			return _curUndoID;
		}

		public string GetUndoName()
		{
			return _curGroupName;
		}


		// 기록 함수들
		//----------------------------------------------------------------------------------
		/// <summary>
		/// 오브젝트를 Record하는 함수. 타입에 따라서는 GameObject와 컴포넌트를 같이 기록한다.
		/// isRecording이 True인 상태여야 한다.
		/// 이 함수 내에서 UndoID를 할당받는다.
		/// </summary>
		private void RecordObjectWithAllComponents(UnityEngine.Object targetObject, apEditorUtil.UNDO_STRUCT structChanged)
		{
			if(_recordingObjects_Value == null)
			{
				_recordingObjects_Value = new HashSet<UnityEngine.Object>();
			}

			if(_recordingObjects_Complete == null)
			{
				_recordingObjects_Complete = new HashSet<UnityEngine.Object>();
			}

			HashSet<UnityEngine.Object> recordingObjects = structChanged == apEditorUtil.UNDO_STRUCT.StructChanged ? _recordingObjects_Complete : _recordingObjects_Value;


			//일단 요청된 오브젝트를 저장한다.
			if(!recordingObjects.Contains(targetObject))
			{
				//if(structChanged == apEditorUtil.UNDO_STRUCT.StructChanged)
				//{
				//	Undo.RegisterCompleteObjectUndo(targetObject, _curGroupName);
				//}
				//else
				//{
				//	Undo.RecordObject(targetObject, _curGroupName);//구조가 복잡하니 변수 추적이 잘 안된다.
				//}

				//항상 스냅샷 저장
				Undo.RegisterCompleteObjectUndo(targetObject, _curGroupName);
				recordingObjects.Add(targetObject);

				//첫번째 Record 직후엔 ID를 갱신해서 병합을 대비하자
				StoreUndoID();
			}

			//만약 이 오브젝트가 GameObject나 Component라면
			//대상 외의 하위의 컴포넌트들도 같이 기록한다.
			GameObject targetGameObject = null;

			if(targetObject is UnityEngine.Component)
			{
				//이 오브젝트가 컴포넌트라면 GameObject를 찾고, 그 하위의 컴포넌트들을 모두 기록한다.
				UnityEngine.Component comp = targetObject as UnityEngine.Component;
				targetGameObject = comp.gameObject;
			}
			else if(targetObject is GameObject)
			{
				//이 오브젝트가 GameObject라면 그 하위의 컴포넌트들을 모두 기록한다.
				targetGameObject = targetObject as GameObject;
			}

			if(targetGameObject == null)
			{
				//요청된 대상이 Component난 GameObject가 아니므로 더 기록할 필요는 없다.
				return;
			}

			//1. GameObject를 기록한다. (본인이 아닌 경우)
			if(targetGameObject != targetObject
				&& !recordingObjects.Contains(targetGameObject))
			{
				//if(structChanged == apEditorUtil.UNDO_STRUCT.StructChanged)
				//{
				//	Undo.RegisterCompleteObjectUndo(targetGameObject, _curGroupName);
				//}
				//else
				//{
				//	Undo.RecordObject(targetGameObject, _curGroupName);//<<구조가 복잡하니 변수 추적이 잘 안된다.
				//}

				//항상 스냅샷 저장
				Undo.RegisterCompleteObjectUndo(targetGameObject, _curGroupName);
				recordingObjects.Add(targetGameObject);
			}

			//2. GameObject의 컴포넌트들을 기록한다.
			UnityEngine.Component[] components = targetGameObject.GetComponents<UnityEngine.Component>();
			int nComponents = components.Length;

			if(nComponents > 0)
			{
				UnityEngine.Component curComp = null;

				for (int i = 0; i < nComponents; i++)
				{
					curComp = components[i];

					if(curComp == null
					|| curComp == targetObject)
					{
						continue;
					}

					if(recordingObjects.Contains(curComp))
					{
						continue;
					}

					//if(structChanged == apEditorUtil.UNDO_STRUCT.StructChanged)
					//{
					//	Undo.RegisterCompleteObjectUndo(curComp, _curGroupName);
					//}
					//else
					//{
					//	Undo.RecordObject(curComp, _curGroupName);//<<이거 제대로 작동 안한다. 구조가 복잡해서 그런듯
					//}

					//그냥 항상 스냅샷 저장
					Undo.RegisterCompleteObjectUndo(curComp, _curGroupName);
					recordingObjects.Add(curComp);
				}
			}
		}


		public enum RECORD_WITH
		{
			OnlyTarget,
			GameObjectAllComponents,
		}

		/// <summary>
		/// 오브젝트를 기록한다. Undo.RegisterCompleteObjectUndo의 래퍼 함수
		/// </summary>
		/// <param name="targetObject"></param>
		public void RecordObject(UnityEngine.Object targetObject, apEditorUtil.UNDO_STRUCT structChanged, RECORD_WITH recordWith)
		{
			if(targetObject == null)
			{
				return;
			}

			if(recordWith == RECORD_WITH.GameObjectAllComponents)
			{
				//오브젝트들을 컴포넌트들과 함께 기록한다.
				RecordObjectWithAllComponents(targetObject, structChanged);
			}
			else
			{
				//이 객체만 저장하자
				if(_recordingObjects_Value == null)
				{
					_recordingObjects_Value = new HashSet<UnityEngine.Object>();
				}

				if(_recordingObjects_Complete == null)
				{
					_recordingObjects_Complete = new HashSet<UnityEngine.Object>();
				}

				HashSet<UnityEngine.Object> recordingObjects = structChanged == apEditorUtil.UNDO_STRUCT.StructChanged ? _recordingObjects_Complete : _recordingObjects_Value;


				//일단 요청된 오브젝트를 저장한다.
				if(!recordingObjects.Contains(targetObject))
				{
					//if(structChanged == apEditorUtil.UNDO_STRUCT.StructChanged)
					//{
					//	Undo.RegisterCompleteObjectUndo(targetObject, _curGroupName);
					//}
					//else
					//{
					//	Undo.RecordObject(targetObject, _curGroupName);//구조가 복잡해서 변수 추적이 잘 안된다.
					//}

					//항상 스냅샷 저장
					Undo.RegisterCompleteObjectUndo(targetObject, _curGroupName);
					recordingObjects.Add(targetObject);

					//첫번째 Record 직후엔 ID를 갱신해서 병합을 대비하자
					StoreUndoID();
				}
			}	
		}

		/// <summary>오브젝트를 기록한다. Undo.RegisterCompleteObjectUndo의 래퍼 함수</summary>
		public void RecordObjects<T>(List<T> targetObjects, apEditorUtil.UNDO_STRUCT structChanged, RECORD_WITH recordWith) where T : UnityEngine.Object
		{
			int nObjects = targetObjects != null ? targetObjects.Count : 0;
			if(nObjects == 0)
			{
				return;
			}

			// List<UnityEngine.Object> validObjs = new List<UnityEngine.Object>();
			// UnityEngine.Object curObj = null;
			// for (int i = 0; i < nObjects; i++)
			// {
			// 	curObj = targetObjects[i];

			// 	if(curObj == null)
			// 	{
			// 		continue;
			// 	}

			// 	if(_curRecordingObjects.Contains(curObj))
			// 	{
			// 		continue;
			// 	}

			// 	validObjs.Add(curObj);
			// 	_curRecordingObjects.Add(curObj);
			// }

			// if(validObjs.Count > 0)
			// {
			// 	if(structChanged == apEditorUtil.UNDO_STRUCT.StructChanged)
			// 	{
			// 		//구조 변경시에는 완벽하게 상태를 기록한다.
			// 		Undo.RegisterCompleteObjectUndo(validObjs.ToArray(), _curGroupName);
			// 	}
			// 	else
			// 	{
			// 		//변경된 속성만 추적하여 기록한다.
			// 		Undo.RecordObjects(validObjs.ToArray(), _curGroupName);
			// 	}
			// }

			// //첫번째 Record 직후엔 ID를 갱신해서 병합을 대비하자
			// StoreUndoID();

			if(recordWith == RECORD_WITH.GameObjectAllComponents)
			{
				//오브젝트들을 컴포넌트들과 함께 기록한다.
				//변경 v1.6.0 : Undo.RecordObjects 함수를 사용하는 대신, 일일이 Record 함수를 호출하자
				T curObj = null;
				for (int i = 0; i < nObjects; i++)
				{
					curObj = targetObjects[i];
					if(curObj == null)
					{
						continue;
					}
					RecordObjectWithAllComponents(curObj, structChanged);
				}
			}
			else
			{
				//입력된 대상만 저장하자
				//이 객체만 저장하자
				if(_recordingObjects_Value == null)
				{
					_recordingObjects_Value = new HashSet<UnityEngine.Object>();
				}

				if(_recordingObjects_Complete == null)
				{
					_recordingObjects_Complete = new HashSet<UnityEngine.Object>();
				}

				HashSet<UnityEngine.Object> recordingObjects = structChanged == apEditorUtil.UNDO_STRUCT.StructChanged ? _recordingObjects_Complete : _recordingObjects_Value;

				T curObj = null;
				for (int i = 0; i < nObjects; i++)
				{
					curObj = targetObjects[i];
					if (curObj == null)
					{
						continue;
					}

					if(recordingObjects.Contains(curObj))
					{
						continue;
					}

					//if(structChanged == apEditorUtil.UNDO_STRUCT.StructChanged)
					//{
					//	Undo.RegisterCompleteObjectUndo(curObj, _curGroupName);
					//}
					//else
					//{
					//	Undo.RecordObject(curObj, _curGroupName);//구조가 복잡해서 변수 추적이 잘 안된다.
					//}

					//항상 스냅샷 저장
					Undo.RegisterCompleteObjectUndo(curObj, _curGroupName);
					recordingObjects.Add(curObj);

					//첫번째 Record 직후엔 ID를 갱신해서 병합을 대비하자
					StoreUndoID();
				}
			}
			
		}

		/// <summary>
		/// GameObject를 포함하여 모든 컴포넌트와 모든 자식 GameObject를 기록한다.
		/// </summary>
		/// <param name="targetObject"></param>
		/// <param name="structChanged"></param>
		public void RecordGameObjectWithAllChildren(GameObject targetGameObject, apEditorUtil.UNDO_STRUCT structChanged)
		{
			if(targetGameObject == null)
			{
				return;
			}

			//재귀적으로 모든 GameObject를 기록한다.
			RecordGameObjectWithAllChildrenRecursive(targetGameObject, targetGameObject, structChanged);
		}

		private void RecordGameObjectWithAllChildrenRecursive(GameObject targetGameObject, GameObject rootGameObject, apEditorUtil.UNDO_STRUCT structChanged)
		{
			if(targetGameObject == null)
			{
				return;
			}

			//Undo로 컴포넌트를 포함해서 기록한다.
			RecordObjectWithAllComponents(targetGameObject, structChanged);

			int nChild = targetGameObject.transform.childCount;
			if(nChild == 0)
			{
				return;
			}

			for (int i = 0; i < nChild; i++)
			{
				GameObject childGameObject = targetGameObject.transform.GetChild(i).gameObject;
				if(childGameObject == null || childGameObject == rootGameObject)
				{
					continue;
				}
				RecordGameObjectWithAllChildrenRecursive(childGameObject, rootGameObject, structChanged);
			}
		}


		/// <summary>
		/// v1.6.0 : CheckNewAction이 삭제되고 IsSkippableContinuousAction으로 대체되었다.
		/// 연속된 액션인 경우엔 Undo의 예외가 된다.
		/// 연속된 액션이어서 생략 가능하다면 true를 리턴한다.
		/// 만약 이전과 연결된 연속 액션이 아니라면 false를 리턴한다.
		/// </summary>
		private bool IsSkippableContinuousAction(	ACTION action,
													apPortrait portrait,
													apMesh mesh,
													apMeshGroup meshGroup,
													apModifierBase modifier,
													SAVE_TARGET saveTarget)
		{
			//기본 조건이 되는가
			//이전의 동일한 Continuous 기록이 있는지 확인
			if(!_isCallContinuous 
				|| _action != action
				|| _saveTarget != saveTarget
				|| _portrait != portrait
				|| _mesh != mesh
				|| _meshGroup != meshGroup
				|| _modifier != modifier)
			{
				//이전 기록과 다르다. 연속된 입력이 아니므로 Undo 등록 가능하다.
				return false;
			}

			//이전과 연속적인 Undo 액션이 기록되는 중이었다.

			//시간을 보자
			if(!_lastUndoTimeRecorded)
			{
				//이전 액션의 시간이 기록되지 않았거나 시간이 초기화되었다.
				//연속된 액션으로 볼 수 없으므로 Undo 등록 가능하다.
				return false;
			}

			//시간을 보자
			float lastDeltaTime = (float)(DateTime.Now.Subtract(_lastUndoTime).TotalSeconds);
			if(lastDeltaTime > CONT_SAVE_TIME)
			{
				//시간이 지나서 연속된 액션이 아니다.
				return false;
			}

			//연속 액션 조건을 모두 만족했다.
			return true;
		}


		/// <summary>
		/// 추가 v1.6.0 : Undo 액션이 기록될 때 요청된 액션을 기록한다.
		/// 일반적으로 추가 액션은 기록하지 않아도 되지만,
		/// 연속된 기록 중 (isCallContinuous)인 도중에 다른 액션이 등록 되었다면 isCallContinuous를 false로 강제하여 이 함수를 다시 호출하자
		/// </summary>
		/// <param name="action"></param>
		/// <param name="portrait"></param>
		/// <param name="mesh"></param>
		/// <param name="meshGroup"></param>
		/// <param name="modifier"></param>
		/// <param name="isCallContinuous"></param>
		/// <param name="saveTarget"></param>
		private void StoreUndoAction(	ACTION action,
										apPortrait portrait,
										apMesh mesh,
										apMeshGroup meshGroup,
										apModifierBase modifier,
										bool isCallContinuous,
										SAVE_TARGET saveTarget)
		{
			//값을 모두 갱신한다.
			_action = action;
			
			_portrait = portrait;
			_mesh = mesh;
			_meshGroup = meshGroup;
			_modifier = modifier;

			_isCallContinuous = isCallContinuous;

			_saveTarget = saveTarget;

			_lastUndoTimeRecorded = true;
			_lastUndoTime = DateTime.Now;
		}




		///// <summary>
		///// Undo 전에 중복을 체크하기 위해 Action을 등록한다.
		///// 리턴값이 True이면 "새로운 Action"이므로 Undo 등록을 해야한다.
		///// 만약 Action 타입이 Add, New.. 계열이면 targetObject가 null일 수 있다. (parent는 null이 되어선 안된다)
		///// </summary>
		///// <returns>이어지지 않은 새로운 타입의 Undo Action이면 True</returns>
		//private bool CheckNewAction(	ACTION action,
		//								apPortrait portrait,
		//								apMesh mesh,
		//								apMeshGroup meshGroup,
		//								apModifierBase modifier,
		//								bool isCallContinuous,
		//								SAVE_TARGET saveTarget)
		//{	
		//	bool isTimeOver = false;
		//	double lastDeltaTime = DateTime.Now.Subtract(_lastUndoTime).TotalSeconds;
		//	if(lastDeltaTime > CONT_SAVE_TIME || _isFirstAction)
		//	{
		//		//Debug.Log("Undo Delta Time : " + lastDeltaTime + " > " + CONT_SAVE_TIME);

		//		//1초가 넘었다면 강제 Undo ID 증가
		//		isTimeOver = true;
		//		_lastUndoTime = DateTime.Now;
		//		_isFirstAction = false;
		//	}

		//	//특정 조건에서는 UndoID가 증가하지 않는다.
		//	//유효한 Action이고 시간이 지나지 않았다면
		//	//+CallContinuous 한정
		//	if(_action != ACTION.None && !isTimeOver && isCallContinuous)
		//	{
		//		//이전과 값이 같을 때에만 Multiple 처리가 된다.
		//		if(	action == _action &&
		//			saveTarget == _saveTarget &&
		//			portrait == _portrait &&
		//			mesh == _mesh &&
		//			meshGroup == _meshGroup &&
		//			modifier == _modifier && 
		//			isCallContinuous == _isCallContinuous
		//			)
		//		{
		//			//연속 호출이면 KeyObject가 달라도 Undo를 묶는다.
		//			//>KeyObject는 무시
		//			return false;
		//		}
		//	}

		//	_action = action;

		//	_saveTarget = saveTarget;
		//	_portrait = portrait;
		//	_mesh = mesh;
		//	_meshGroup = meshGroup;
		//	_modifier = modifier;

		//	_isCallContinuous = isCallContinuous;//여러 항목을 동시에 처리하는 Batch 액션 중인가

		//	return true;
		//}


		/// <summary>
		/// 추가 21.6.30 : 마우스 Up, 다른 객체 선택 (작은 단위까지)시 Undo의 연속성을 초기화한다.
		/// 이 함수가 제대로 작동하면 KeyObject를 사용하지 않아도 된다.
		/// </summary>
		public void ResetContinuous()
		{
			//_isFirstAction = true;
			_lastUndoTimeRecorded = false;
			_lastUndoTime = DateTime.Now;
			_isCallContinuous = false;
		}


		// Undo 기록하기 < 객체 생성/삭제 >
		//public void StartRecording_CreateOrDestroy(string label)
		//{
		//	//객체 생성/삭제시에는 무조건 ID를 증가시킨다. (같은 방식이어도 마찬가지)
			
		//	//만약 Property Changed 타입의 기록이 있었다면 기록 재시작을 해야한다.
		//	//이건 객체 생성/삭제 변경용이다.
		//	if(_isRecording)
		//	{
		//		//이전의 Undo는 모두 종료하고 다시 시작한다.
		//		RestartUndo();
		//	}


		//	// < 아직 기록이 되지 않은 상태 >
		//	//새롭게 ID를 만드는 단계이다.

		//	//Continuous를 중단
		//	ResetContinuous();

		//	_isRecording = true;
				
		//	_curStructChangedType = apEditorUtil.UNDO_STRUCT.StructChanged;
		//	_curGroupName = label;
		//	_isContinuousRecording = false;
			
		//	Undo.IncrementCurrentGroup();//무조건 Group ID를 증가시킨다.

		//	//Debug.Log("+Undo ID : " + Undo.GetCurrentGroup() + "[CorD]");

		//	//UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();//삭제

		//	_curUndoRequestType = UNDO_REQUEST_TYPE.CreateOrDestroy;//생성/삭제 타입
		//}

		//public bool IsRecordingAsCreateOrDestroy()
		//{
		//	return _isRecording && _curUndoRequestType == UNDO_REQUEST_TYPE.CreateOrDestroy;
		//}



		/// <summary>
		/// 새로운 GameObject를 생성했음을 기록한다.
		/// </summary>
		public void RecordCreatedGameObject(GameObject newGameObject)//Label은 삭제
		{
			if(newGameObject == null)
			{
				return;
			}
			//if(!_isRecording || _curUndoRequestType != UNDO_REQUEST_TYPE.CreateOrDestroy)
			//{
			//	//Recording이 진행중이 아니라면
			//	StartRecording_CreateOrDestroy(label);
			//}

			//조건 체크 후 임시로라도 기록 시작 처리
			if(!_isRecording)
			{
				_isRecording = true;
				_curStructChangedType = apEditorUtil.UNDO_STRUCT.StructChanged;
				_curGroupName = GetLabel(ACTION.MonoChanged);//임시 액션 할당

				_isUndoIDAssigned = false;
				_curUndoID = -1;

				StoreUndoAction(ACTION.MonoChanged, null, null, null, null, false,
					SAVE_TARGET.Portrait
					| SAVE_TARGET.AllMeshes
					| SAVE_TARGET.AllMeshGroups
					| SAVE_TARGET.AllModifiers);
			}
			else
			{
				//Continuous는 해제
				ResetContinuous();

				//구조 변경 값 갱신
				_curStructChangedType = apEditorUtil.UNDO_STRUCT.StructChanged;
			}

			Undo.RegisterCreatedObjectUndo(newGameObject, "Create GameObject");//<이건 임시 Undo 이름

			//Undo ID도 갱신. (병합 위함)
			StoreUndoID();

			//이 GameObject의 상태값도 바로 기록한다.
			RecordObjectWithAllComponents(newGameObject, apEditorUtil.UNDO_STRUCT.StructChanged);
		}

		/// <summary>
		/// 게임 오브젝트를 삭제하면서 Undo에 기록한다.
		/// </summary>
		public void RecordDestroyGameObject(GameObject destroyableGameObject)
		{
			if(destroyableGameObject == null)
			{
				return;
			}

			//if(!_isRecording || _curUndoRequestType != UNDO_REQUEST_TYPE.CreateOrDestroy)
			//{
			//	//Recording이 진행중이 아니라면
			//	StartRecording_CreateOrDestroy(label);
			//}

			//조건 체크 후 임시로라도 기록 시작 처리
			if(!_isRecording)
			{
				_isRecording = true;
				_curStructChangedType = apEditorUtil.UNDO_STRUCT.StructChanged;
				_curGroupName = GetLabel(ACTION.MonoChanged);//임시 액션 할당

				_isUndoIDAssigned = false;
				_curUndoID = -1;

				StoreUndoAction(ACTION.MonoChanged, null, null, null, null, false,
					SAVE_TARGET.Portrait
					| SAVE_TARGET.AllMeshes
					| SAVE_TARGET.AllMeshGroups
					| SAVE_TARGET.AllModifiers);
			}
			else
			{
				//Continuous는 해제
				ResetContinuous();

				//구조 변경 값 갱신
				_curStructChangedType = apEditorUtil.UNDO_STRUCT.StructChanged;
			}

			Undo.DestroyObjectImmediate(destroyableGameObject);

			//Undo ID도 갱신. (병합 위함)
			StoreUndoID();
		}

		/// <summary>
		/// Transform의 Parent를 변경할때 그냥 하지 말고 이 함수를 사용하자
		/// </summary>
		public void SetParentWithRecord(Transform childTF, Transform newParentTF)
		{
			if(childTF == null)
			{
				return;
			}

			//조건 체크 후 임시로라도 기록 시작 처리
			if(!_isRecording)
			{
				_isRecording = true;
				_curStructChangedType = apEditorUtil.UNDO_STRUCT.StructChanged;
				_curGroupName = GetLabel(ACTION.MonoChanged);//임시 액션 할당

				_isUndoIDAssigned = false;
				_curUndoID = -1;

				StoreUndoAction(ACTION.MonoChanged, null, null, null, null, false,
					SAVE_TARGET.Portrait
					| SAVE_TARGET.AllMeshes
					| SAVE_TARGET.AllMeshGroups
					| SAVE_TARGET.AllModifiers);
			}
			else
			{
				//Continuous는 해제
				ResetContinuous();

				//구조 변경 값 갱신
				_curStructChangedType = apEditorUtil.UNDO_STRUCT.StructChanged;
			}

			// if(newParentTF == null)
			// {
			// 	Debug.LogError("SetParentWithRecord : newParentTF is null");
			// }
			Undo.SetTransformParent(childTF, newParentTF, "Set Parent Transform");//<임시 Undo 이름			

			//Undo ID도 갱신. (병합 위함)
			StoreUndoID();

			//Undo.RegisterCompleteObjectUndo(childTF, "Set Parent Transform");//일반 Record도 하자 (스냅샷 저장)

			//Tranform의 상태값도 바로 기록한다.
			RecordObjectWithAllComponents(childTF, apEditorUtil.UNDO_STRUCT.StructChanged);
		}

		public T AddComponentWithRecord<T>(GameObject targetGameObject) where T : UnityEngine.Component
		{
			if(targetGameObject == null)
			{
				return null;
			}

			//조건 체크 후 임시로라도 기록 시작 처리
			if(!_isRecording)
			{
				_isRecording = true;
				_curStructChangedType = apEditorUtil.UNDO_STRUCT.StructChanged;
				_curGroupName = GetLabel(ACTION.MonoChanged);//임시 액션 할당

				_isUndoIDAssigned = false;
				_curUndoID = -1;

				StoreUndoAction(ACTION.MonoChanged, null, null, null, null, false,
								SAVE_TARGET.Portrait
								| SAVE_TARGET.AllMeshes
								| SAVE_TARGET.AllMeshGroups
								| SAVE_TARGET.AllModifiers);
			}
			else
			{
				//Continuous는 해제
				ResetContinuous();

				//구조 변경 값 갱신
				_curStructChangedType = apEditorUtil.UNDO_STRUCT.StructChanged;
			}

			T result = Undo.AddComponent<T>(targetGameObject);
			//Undo ID도 갱신. (병합 위함)
			StoreUndoID();


			if(result != null)
			{
				//Undo.RecordObject(result, "Add Component");//일반 Record도 하자
				//일반 Record도 하자
				RecordObjectWithAllComponents(result, apEditorUtil.UNDO_STRUCT.StructChanged);
			}

			return result;
		}

		public void SetRecordAnyObject(UnityEngine.Object targetObject, bool isRecordComplete)
		{
			if (targetObject == null)
			{
				return;
			}
			//조건 체크 후 임시로라도 기록 시작 처리
			if (!_isRecording)
			{
				_isRecording = true;
				_curStructChangedType = isRecordComplete ? apEditorUtil.UNDO_STRUCT.StructChanged : apEditorUtil.UNDO_STRUCT.ValueOnly;
				_curGroupName = GetLabel(ACTION.MonoChanged);//임시 액션 할당
				_isUndoIDAssigned = false;
				_curUndoID = -1;
				StoreUndoAction(ACTION.MonoChanged, null, null, null, null, false,
									SAVE_TARGET.Portrait
									| SAVE_TARGET.AllMeshes
									| SAVE_TARGET.AllMeshGroups
									| SAVE_TARGET.AllModifiers);
			}
			else
			{
				//Continuous는 해제
				ResetContinuous();

				if(isRecordComplete)
				{
					_curStructChangedType = apEditorUtil.UNDO_STRUCT.StructChanged;
				}
			}
			// if(isRecordComplete)
			// {
			// 	Undo.RegisterCompleteObjectUndo(targetObject, "Any Object Changed");
			// }
			// else
			// {
			// 	Undo.RecordObject(targetObject, "Any Object Changed");
			// }

			//기록을 하자
			RecordObjectWithAllComponents(targetObject, isRecordComplete ? apEditorUtil.UNDO_STRUCT.StructChanged : apEditorUtil.UNDO_STRUCT.ValueOnly);
			

			//Undo ID도 갱신. (병합 위함)
			StoreUndoID();

		}


		// /// <summary>
		// /// 여러개의 게임 오브젝트를 생성했음을 기록한다.
		// /// </summary>
		// public void RecordCreatedMonoObjects<T>(List<T> newMonoObjects) where T : MonoBehaviour
		// {
		// 	int nTargets = newMonoObjects != null ? newMonoObjects.Count : 0;
		// 	if(nTargets == 0)
		// 	{
		// 		return;
		// 	}

		// 	//if(!_isRecording || _curUndoRequestType != UNDO_REQUEST_TYPE.CreateOrDestroy)
		// 	//{
		// 	//	//Recording이 진행중이 아니라면
		// 	//	StartRecording_CreateOrDestroy(label);
		// 	//}

		// 	//조건 체크 후 임시로라도 기록 시작 처리
		// 	if(!_isRecording)
		// 	{
		// 		_isRecording = true;
		// 		_curStructChangedType = apEditorUtil.UNDO_STRUCT.StructChanged;
		// 		_curGroupName = GetLabel(ACTION.MonoChanged);//임시 액션 할당

		// 		_isUndoIDAssigned = false;
		// 		_curUndoID = -1;

		// 		StoreUndoAction(ACTION.MonoChanged, null, null, null, null, false,
		// 			SAVE_TARGET.Portrait
		// 			| SAVE_TARGET.AllMeshes
		// 			| SAVE_TARGET.AllMeshGroups
		// 			| SAVE_TARGET.AllModifiers);
		// 	}
		// 	else
		// 	{
		// 		//Continuous는 해제
		// 		ResetContinuous();

		// 		//구조 변경 값 갱신
		// 		_curStructChangedType = apEditorUtil.UNDO_STRUCT.StructChanged;
		// 	}

		// 	T curObj = null;
		// 	for (int i = 0; i < nTargets; i++)
		// 	{
		// 		curObj = newMonoObjects[i];
		// 		if(curObj == null)
		// 		{
		// 			continue;
		// 		}
		// 		Undo.RegisterCreatedObjectUndo(curObj.gameObject, "Create Mono-Object");//임시 Undo 이름
		// 		curObj = null;

		// 		//Undo ID도 갱신. (병합 위함)
		// 		StoreUndoID();
		// 	}
			
		// }



		public void RecordDestroyGameObjects(List<GameObject> destroyableGameObjects)
		{
			int nTargets = destroyableGameObjects != null ? destroyableGameObjects.Count : 0;
			if(nTargets == 0)
			{
				return;
			}

			//if(!_isRecording || _curUndoRequestType != UNDO_REQUEST_TYPE.CreateOrDestroy)
			//{
			//	//Recording이 진행중이 아니라면
			//	StartRecording_CreateOrDestroy(label);
			//}

			//조건 체크 후 임시로라도 기록 시작 처리
			if(!_isRecording)
			{
				_isRecording = true;
				_curStructChangedType = apEditorUtil.UNDO_STRUCT.StructChanged;
				_curGroupName = GetLabel(ACTION.MonoChanged);//임시 액션 할당

				_isUndoIDAssigned = false;
				_curUndoID = -1;

				StoreUndoAction(ACTION.MonoChanged, null, null, null, null, false,
					SAVE_TARGET.Portrait
					| SAVE_TARGET.AllMeshes
					| SAVE_TARGET.AllMeshGroups
					| SAVE_TARGET.AllModifiers);
			}
			else
			{
				//Continuous는 해제
				ResetContinuous();

				//구조 변경 값 갱신
				_curStructChangedType = apEditorUtil.UNDO_STRUCT.StructChanged;
			}

			GameObject curObj = null;
			for (int i = 0; i < nTargets; i++)
			{
				curObj = destroyableGameObjects[i];
				if(curObj != null)
				{
					Undo.DestroyObjectImmediate(curObj);
					
					//Undo ID도 갱신. (병합 위함)
					StoreUndoID();
				}
				curObj = null;
			}
		}

		// //디버그 함수
		// private void DebugUndoRecord()
		// {
		// 	if(!_isRecording)
		// 	{
		// 		Debug.Log("Undo가 기록되지 않음");
		// 		return;
		// 	}

		// 	int undoID = Undo.GetCurrentGroup();
		// 	string recordedUndoName = Undo.GetCurrentGroupName();

		// 	string strDebug = "Undo 기록 - ID : " + undoID + " | Name : " + _curGroupName + " (" + recordedUndoName + ")";
			
		// 	int nRecordedObjects = _curRecordingObjects != null ? _curRecordingObjects.Count : 0;

		// 	strDebug += " - " + nRecordedObjects + "개의 오브젝트";
		// 	if(nRecordedObjects > 0)
		// 	{
		// 		UnityEngine.Object curObj = null;
		// 		for (int i = 0; i < nRecordedObjects; i++)
		// 		{
		// 			curObj = _curRecordingObjects[i];
		// 			if(curObj == null)
		// 			{
		// 				continue;
		// 			}
		// 			if(curObj is apPortrait)
		// 			{
		// 				//apPortrait curPortrait = curObj as apPortrait;
		// 				strDebug += "\n- Portrait";
		// 			}
		// 			else if(curObj is apMesh)
		// 			{
		// 				apMesh curMesh = curObj as apMesh;
		// 				strDebug += "\n- Mesh : " + curMesh._name;
		// 			}
		// 			else if(curObj is apMeshGroup)
		// 			{
		// 				apMeshGroup curMeshGroup = curObj as apMeshGroup;
		// 				strDebug += "\n- MeshGroup : " + curMeshGroup._name;
		// 			}
		// 			else if(curObj is apModifierBase)
		// 			{
		// 				apModifierBase curMod = curObj as apModifierBase;
		// 				strDebug += "\n- Modifier : " + curMod.DisplayName;
		// 			}
		// 		}
		// 	}

		// 	Debug.Log(strDebug);
		// }
	}
}