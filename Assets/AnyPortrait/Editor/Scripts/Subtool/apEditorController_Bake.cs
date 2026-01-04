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
using System.Text;

namespace AnyPortrait
{
	// Editor Controller 코드 중 "Bake"와 관련된 코드만 모았다.
	public partial class apEditorController
	{
		//-----------------------------------------------------------------
		// Bake
		//-----------------------------------------------------------------
		/// <summary>
		/// 현재 Portrait를 실행가능한 버전으로 Bake하자
		/// </summary>
		public apBakeResult Bake()
		{
			if (Editor._portrait == null)
			{
				return null;
			}

			apPortrait targetPortrait = Editor._portrait;

			//추가 20.11.7
			//이미지가 설정되지 않은 메시가 있다면 에러가 발생한다.
			//미리 안내를 하자
			if(!CheckIfAnyNoImageMesh(targetPortrait))
			{
				//에러가 발생해서 Bake 취소
				return null;
			}

			//v1.6.0 Undo
			apEditorUtil.EndRecordUndo();//기존의 Undo 기록이 있다면 여기서 한번 쉬어가자
			apEditorUtil.SetRecordBeforeCreateOrDestroyObject(targetPortrait, apUndoGroupData.ACTION.Portrait_Bake);//Undo를 시작한다.

			apBakeResult bakeResult = new apBakeResult();


			//추가 19.5.26 : v1.1.7의 용량 최적화가 적용되었는가 (=modMeshSet을 이용하도록 설정되었는가)
			bool isSizeOptimizedV117 = true;

			//bool isSizeOptimizedV117 = false;//<<테스트

			//추가 19.8.5
			//bool isUseSRP = Editor._isUseSRP;//이전
			bool isUseSRP = Editor.ProjectSettingData.Project_IsUseSRP;//변경 [v1.4.2]
			bool isBakeGammaColorSpace = Editor.ProjectSettingData.Project_IsColorSpaceGamma;//추가 [v1.4.2]

			//v1.6.0 : SRP를 Portrait에 저장하자 (기존은 OptMesh)
			if(isUseSRP)
			{
				targetPortrait._renderPipelineOption = apPortrait.RENDER_PIPELINE_OPTION.SRP;
			}
			else
			{
				targetPortrait._renderPipelineOption = apPortrait.RENDER_PIPELINE_OPTION.BuiltIn;
			}

			//추가 10.26 : Bake에서는 빌보드가 꺼져야 한다.
			//임시로 껐다가 마지막에 다시 복구
			apPortrait.BILLBOARD_TYPE billboardType = targetPortrait._billboardType;
			targetPortrait._billboardType = apPortrait.BILLBOARD_TYPE.None;//임시로 끄자




			//추가 21.3.11
			// Scale 이슈가 있다.
			// Bake 전에 이미 Scale이 음수인 경우, Bake 직후나 Link후 플레이시 메시가 거꾸로 보이게 된다.
			//따라서 portrait부터 시작해서 상위의 모든 GameObject의 Sca;e을 저장했다가 복원해야한다.
			Dictionary<Transform, Vector3> prevTransformScales = new Dictionary<Transform, Vector3>();
			Transform curScaleCheckTransform = targetPortrait.transform;
			while(true)
			{
				prevTransformScales.Add(curScaleCheckTransform, curScaleCheckTransform.localScale);
				curScaleCheckTransform.localScale = Vector3.one;//일단 기본으로 강제 적용
				if(curScaleCheckTransform.parent == null)
				{
					break;
				}
				curScaleCheckTransform = curScaleCheckTransform.parent;
			}
			


			//Bake 방식 변경
			//일단 숨겨진 GameObject를 제외한 모든 객체를 리스트로 저장한다.
			//LinkParam 형태로 저장을 한다.
			//LinkParam으로 저장하면서 <apOpt 객체>와 <그렇지 않은 객체>를 구분한다.
			//"apOpt 객체"는 나중에 (1)재활용 할지 (2) 삭제 할지 결정한다.
			//"그렇지 않은 GameObject"는 Hierarchy 정보를 가진채 (1) 링크를 유지할 지(재활용되는 경우) (2) Unlink Group에 넣을지 결정한다.
			//만약 재활용되지 않는 (apOpt GameObject)에서 알수 없는 Component가 발견된 경우 -> 이건 삭제 예외 대상에 넣는다.

			//분류를 위한 그룹
			//1. ReadyToRecycle
			// : 기존에 RootUnit과 그 하위에 있었던 GameObject들이다. 분류 전에 일단 여기로 들어간다.
			// : 분류 후에는 원칙적으로 하위에 어떤 객체도 남아선 안된다.

			//2. RemoveTargets
			// : apOpt를 가진 GameObject 그룹 중에서 사용되지 않았던 그룹이다. 
			// : 처리 후에는 이 GameObject를 통째로 삭제한다.

			//3. UnlinkedObjects
			// : apOpt를 가지지 않은 GameObject중에서 재활용되지 않은 객체들


			GameObject groupObj_1_ReadyToRecycle = new GameObject("__Baking_1_ReadyToRecycle");//<Undo
			GameObject groupObj_2_RemoveTargets = new GameObject("__Baking_2_RemoveTargets");//<Undo

			//v1.6.0 : Undo등록 (이후에 DestroyImmediate를 하더라도 일단 여기선 다 Undo에 등록한다.)
			apEditorUtil.SetRecordCreatedGameObject(groupObj_1_ReadyToRecycle);
			apEditorUtil.SetRecordCreatedGameObject(groupObj_2_RemoveTargets);

			GameObject groupObj_3_UnlinkedObjects = null;
			if (targetPortrait._bakeUnlinkedGroup == null)
			{
				groupObj_3_UnlinkedObjects = new GameObject("__UnlinkedObjects");//<Undo
				
				//v1.6.0 Undo 등록
				apEditorUtil.SetRecordCreatedGameObject(groupObj_3_UnlinkedObjects);

				targetPortrait._bakeUnlinkedGroup = groupObj_3_UnlinkedObjects;
			}
			else
			{
				groupObj_3_UnlinkedObjects = targetPortrait._bakeUnlinkedGroup;

				//v1.6.0 Undo 등록
				apEditorUtil.SetRecordAnyObjectComplete(groupObj_3_UnlinkedObjects);

				groupObj_3_UnlinkedObjects.name = "__UnlinkedObjects";
			}
			

			//이전
			// groupObj_1_ReadyToRecycle.transform.parent = targetPortrait.transform;//이전
			// groupObj_2_RemoveTargets.transform.parent = targetPortrait.transform;//이전
			// groupObj_3_UnlinkedObjects.transform.parent = targetPortrait.transform;//이전

			//변경 v1.6.0 : Undo API 이용
			apEditorUtil.SetParentWithRecord(groupObj_1_ReadyToRecycle.transform, targetPortrait.transform);
			apEditorUtil.SetParentWithRecord(groupObj_2_RemoveTargets.transform, targetPortrait.transform);
			apEditorUtil.SetParentWithRecord(groupObj_3_UnlinkedObjects.transform, targetPortrait.transform);

			groupObj_1_ReadyToRecycle.transform.localPosition = Vector3.zero;
			groupObj_2_RemoveTargets.transform.localPosition = Vector3.zero;
			groupObj_3_UnlinkedObjects.transform.localPosition = Vector3.zero;

			groupObj_1_ReadyToRecycle.transform.localRotation = Quaternion.identity;
			groupObj_2_RemoveTargets.transform.localRotation = Quaternion.identity;
			groupObj_3_UnlinkedObjects.transform.localRotation = Quaternion.identity;

			groupObj_1_ReadyToRecycle.transform.localScale = Vector3.one;
			groupObj_2_RemoveTargets.transform.localScale = Vector3.one;
			groupObj_3_UnlinkedObjects.transform.localScale = Vector3.one;


			//2. 기존 RootUnit을 Recycle로 옮긴다.
			//옮기면서 "Prev List"를 만들어야 한다. Recycle을 하기 위함
			List<apOptRootUnit> prevOptRootUnits = new List<apOptRootUnit>();
			if (targetPortrait._optRootUnitList != null)
			{
				for (int i = 0; i < targetPortrait._optRootUnitList.Count; i++)
				{
					apOptRootUnit optRootUnit = targetPortrait._optRootUnitList[i];
					if (optRootUnit != null)
					{
						//이전 
						//optRootUnit.transform.parent = groupObj_1_ReadyToRecycle.transform;

						//변경 v1.6.0 : Undo API 이용
						apEditorUtil.SetParentWithRecord(optRootUnit.transform, groupObj_1_ReadyToRecycle.transform);

						prevOptRootUnits.Add(optRootUnit);
					}
				}
			}

			//RootUnit 리스트를 초기화한다.
			if (targetPortrait._optRootUnitList == null)
			{
				targetPortrait._optRootUnitList = new List<apOptRootUnit>();
			}

			targetPortrait._optRootUnitList.Clear();
			targetPortrait._curPlayingOptRootUnit = null;

			if (targetPortrait._optTransforms == null) { targetPortrait._optTransforms = new List<apOptTransform>(); }
			if (targetPortrait._optMeshes == null) { targetPortrait._optMeshes = new List<apOptMesh>(); }
			if (targetPortrait._optTextureData == null) { targetPortrait._optTextureData = new List<apOptTextureData>(); }//<<텍스쳐 데이터 추가

			targetPortrait._optTransforms.Clear();
			targetPortrait._optMeshes.Clear();
			targetPortrait._optTextureData.Clear();

			//추가
			//Batched Matrial 관리 객체가 생겼다.
			if (targetPortrait._optBatchedMaterial == null)
			{
				targetPortrait._optBatchedMaterial = new apOptBatchedMaterial();
			}
			else
			{
				targetPortrait._optBatchedMaterial.Clear(true);//<<이미 생성되어 있다면 초기화
			}

			////추가 11.6 : LWRP Shader를 사용하는지 체크하고, 필요한 경우 생성해야한다.
			//CheckAndCreateLWRPShader();


			//3. 텍스쳐 데이터를 먼저 만들자.
			for (int i = 0; i < targetPortrait._textureData.Count; i++)
			{
				apTextureData textureData = targetPortrait._textureData[i];
				apOptTextureData newOptTexData = new apOptTextureData();

				newOptTexData.Bake(i, textureData);
				targetPortrait._optTextureData.Add(newOptTexData);
			}

			//추가 20.1.28 : Color Space가 동일하도록 묻고 변경
			CheckAndChangeTextureDataColorSpace(targetPortrait);



			//4. 추가 : Reset
			//TODO : 이 함수를 호출한 이후에, 현재 Mesh Group에 대해서 추가 처리 필요
			//이 함수를 호출하면 계층적인 MeshGroup 내부늬 Modifier 연결이 풀린다.
			//이 코드 두개가 포함되어야 한다.
			targetPortrait.LinkAndRefreshInEditor(false, apUtil.LinkRefresh.Set_AllObjects(null));


			//추가 : 사용되지 않는 Monobehaviour는 삭제해야한다.
			CheckAndRemoveUnusedMonobehaviours(targetPortrait);

			//이름을 갱신한다.
			CheckAndRefreshGameObjectNames(targetPortrait);


			//4. OptTransform을 만들자 (RootUnit부터)

			for (int i = 0; i < targetPortrait._rootUnits.Count; i++)
			{
				apRootUnit rootUnit = targetPortrait._rootUnits[i];

				//업데이트를 한번 해주자

				//추가 : 계층구조의 MeshGroup인 경우 이 코드가 추가되어야 한다.
				if (rootUnit._childMeshGroup != null)
				{
					rootUnit._childMeshGroup.SortRenderUnits(true, apMeshGroup.DEPTH_ASSIGN.OnlySort);//렌더 유닛의 Depth를 다시 계산해야한다. <<
					rootUnit._childMeshGroup.LinkModMeshRenderUnits(null);
					rootUnit._childMeshGroup.RefreshModifierLink(null);
				}

				rootUnit.Update(0.0f, false, false);


				apOptRootUnit optRootUnit = null;

				//1. Root Unit
				//재활용 가능한지 판단한다.


				bool isRecycledRootUnit = false;
				apOptRootUnit recycledOptRootUnit = GetRecycledRootUnit(rootUnit, prevOptRootUnits);

				if (recycledOptRootUnit != null)
				{

					//재활용이 된다.
					optRootUnit = recycledOptRootUnit;

					//Undo 등록
					apEditorUtil.SetRecordAnyObject(optRootUnit);

					//일부 값은 다시 리셋
					optRootUnit.name = "Root Unit " + i;
					optRootUnit._portrait = targetPortrait;
					optRootUnit._transform = optRootUnit.transform;

					//optRootUnit.transform.parent = targetPortrait.transform;//이전
					//변경 v1.6.0 : Undo API 이용
					apEditorUtil.SetParentWithRecord(optRootUnit.transform, targetPortrait.transform);


					optRootUnit.transform.localPosition = Vector3.zero;
					optRootUnit.transform.localRotation = Quaternion.identity;
					optRootUnit.transform.localScale = Vector3.one;

					//재활용에 성공했으니 OptUnit은 제외한다.
					prevOptRootUnits.Remove(recycledOptRootUnit);
					isRecycledRootUnit = true;

					//Count+1 : Recycled Opt
					bakeResult.AddCount_RecycledOptGameObject();
				}
				else
				{
					//새로운 RootUnit이다.
					optRootUnit = AddGameObject<apOptRootUnit>("Root Unit " + i, targetPortrait.transform);

					optRootUnit._portrait = targetPortrait;
					optRootUnit._rootOptTransform = null;
					optRootUnit._transform = optRootUnit.transform;

					//Count+1 : New Opt
					bakeResult.AddCount_NewOptGameObject();
				}

				optRootUnit.ClearChildLinks();//Child Link를 초기화한다.

				//추가 12.6 : SortedRenderBuffer에 관련한 Bake 코드 <<
				optRootUnit.BakeSortedRenderBuffer(targetPortrait, rootUnit);


				targetPortrait._optRootUnitList.Add(optRootUnit);



				//재활용에 성공했다면
				//기존의 GameObject + Bake 여부를 재귀적 리스트로 작성한다.
				apBakeLinkManager bakeLinkManager = null;
				if (isRecycledRootUnit)
				{
					bakeLinkManager = new apBakeLinkManager();

					//파싱하자.
					bakeLinkManager.Parse(optRootUnit._rootOptTransform.gameObject, recycledOptRootUnit.gameObject);
				}

				apMeshGroup childMainMeshGroup = rootUnit._childMeshGroup;

				//v1.6.0 추가 : Mask를 받는 메시들을 따로 받자
				HashSet<apTransform_Mesh> maskReceivedMeshTFs = new HashSet<apTransform_Mesh>();

				//0. 추가
				//일부 Modified Mesh를 갱신해야한다.
				if (childMainMeshGroup != null && rootUnit._childMeshGroupTransform != null)
				{
					//Refresh를 한번 해주자
					childMainMeshGroup.RefreshForce();

					List<apModifierBase> modifiers = childMainMeshGroup._modifierStack._modifiers;
					for (int iMod = 0; iMod < modifiers.Count; iMod++)
					{
						apModifierBase mod = modifiers[iMod];
						if (mod._paramSetGroup_controller != null)
						{
							for (int iPSG = 0; iPSG < mod._paramSetGroup_controller.Count; iPSG++)
							{
								apModifierParamSetGroup psg = mod._paramSetGroup_controller[iPSG];
								for (int iPS = 0; iPS < psg._paramSetList.Count; iPS++)
								{
									apModifierParamSet ps = psg._paramSetList[iPS];
									ps.UpdateBeforeBake(targetPortrait, childMainMeshGroup, rootUnit._childMeshGroupTransform);
								}
							}
						}
					}

					//v1.6.0 : Mask를 받는 MeshTransform을 찾자
					CollectMaskReceivedMeshes(	rootUnit._childMeshGroupTransform,
												rootUnit._childMeshGroupTransform,
												maskReceivedMeshTFs);
				}

				//1. 1차 Bake : GameObject 만들기
				if (childMainMeshGroup != null && rootUnit._childMeshGroupTransform != null)
				{
					//정렬 한번 해주고
					childMainMeshGroup.SortRenderUnits(true, apMeshGroup.DEPTH_ASSIGN.OnlySort);

					apRenderUnit rootRenderUnit = childMainMeshGroup._rootRenderUnit;
					//apRenderUnit rootRenderUnit = targetPortrait._rootUnit._renderUnit;
					if (rootRenderUnit != null)
					{
						//apTransform_MeshGroup meshGroupTransform = targetPortrait._rootUnit._childMeshGroupTransform;
						apTransform_MeshGroup meshGroupTransform = rootRenderUnit._meshGroupTransform;

						if (meshGroupTransform == null)
						{
							Debug.LogError("Bake Error : MeshGroupTransform Not Found [" + childMainMeshGroup._name + "]");
						}
						else
						{
							MakeMeshGroupToOptTransform(	rootRenderUnit,
															meshGroupTransform, optRootUnit.transform,
															null,
															optRootUnit,
															bakeLinkManager, bakeResult,
															targetPortrait._bakeZSize,

															//<<감마 색상 공간으로 Bake할 것인가
															//Editor._isBakeColorSpaceToGamma,//<<감마 색상 공간으로 Bake할 것인가
															isBakeGammaColorSpace,//로컬 변수로 변경 v1.4.2

															//Editor._isUseSRP,//LWRP Shader를 사용할 것인가 > 삭제 (SRP로 변경)
															targetPortrait,
															childMainMeshGroup,
															isSizeOptimizedV117,
															isUseSRP,
															maskReceivedMeshTFs//v1.6.0
															);
							//MakeMeshGroupToOptTransform(null, meshGroupTransform, targetPortrait._optRootUnit.transform, null);
						}
					}
					else
					{
						Debug.LogError("Bake Error : RootMeshGroup Not Found [" + childMainMeshGroup._name + "]");
					}
				}



				//optRootUnit.transform.localScale = Vector3.one * 0.01f;
				optRootUnit.transform.localScale = Vector3.one * targetPortrait._bakeScale;


				// 이전에 Bake 했던 정보에서 가져왔다면
				//만약 "재활용되지 않은 GameObject"를 찾아서 별도의 처리를 해야한다.
				if (isRecycledRootUnit && bakeLinkManager != null)
				{
					bakeLinkManager.SetHierarchyNotRecycledObjects(groupObj_1_ReadyToRecycle, groupObj_2_RemoveTargets, groupObj_3_UnlinkedObjects, bakeResult);
				}

				//추가 v1.4.8 : 루트 모션 설정을 입력하자
				optRootUnit._rootMotionBoneID = -1;
				if(childMainMeshGroup != null)
				{
					//루트 모션용 본이 존재하는지 확인하자
					apBone rootMotionBone = childMainMeshGroup.GetBone(childMainMeshGroup._rootMotionBoneID);
					if(rootMotionBone != null)
					{
						//루트 모션 본이 존재한다면 ID를 할당한다.
						optRootUnit._rootMotionBoneID = childMainMeshGroup._rootMotionBoneID;
					}
				}


				//추가 12.6 : Bake 함수 추가 <<
				optRootUnit.BakeComplete();

			}



			if (prevOptRootUnits.Count > 0)
			{
				//이 유닛들은 Remove Target으로 이동해야 한다.
				apOptRootUnit curPrevoptRootUnit = null;
				for (int i = 0; i < prevOptRootUnits.Count; i++)
				{
					curPrevoptRootUnit = prevOptRootUnits[i];//변경 1.4.5

					//[v1.4.5] 오류 검출
					if(curPrevoptRootUnit == null
						|| curPrevoptRootUnit.transform == null)
					{
						Debug.LogWarning("AnyPortrait : Bake warning. Since the previous root unit is null, some objects may not be created or deleted properly.");
						continue;
					}

					//curPrevoptRootUnit.transform.parent = groupObj_2_RemoveTargets.transform;//이전
					//변경 v1.6.0 : Undo API 이용
					apEditorUtil.SetParentWithRecord(curPrevoptRootUnit.transform, groupObj_2_RemoveTargets.transform);

					//[v1.4.5] 연결이 해제된 상태에서 Bake를 다시 실행할 때 Null 체크
					if (curPrevoptRootUnit._rootOptTransform == null)
					{	
						Debug.LogWarning("AnyPortrait : Bake warning. Some subobjects of the unused Root Unit have already been deleted, so moving them to the Unlinked group for preservation failed.");
						continue;
					}


					//만약 여기서 알수없는 GameObject나 Compnent에 대해서는 Remove가 아니라 Unlink로 옮겨야 한다.
					apBakeLinkManager prevBakeManager = new apBakeLinkManager();
					prevBakeManager.Parse(curPrevoptRootUnit._rootOptTransform.gameObject, null);

					prevBakeManager.SetHierarchyToUnlink(groupObj_3_UnlinkedObjects, bakeResult);
				}
			}


			//TODO: 이제 그룹을 삭제하던가 경고 다이얼로그를 띄워주던가 하자
			// UnityEngine.Object.DestroyImmediate(groupObj_1_ReadyToRecycle);
			// UnityEngine.Object.DestroyImmediate(groupObj_2_RemoveTargets);
			//Undo API로 변경
			apEditorUtil.SetRecordDestroyGameObject(groupObj_1_ReadyToRecycle);
			apEditorUtil.SetRecordDestroyGameObject(groupObj_2_RemoveTargets);

			if (groupObj_3_UnlinkedObjects.transform.childCount == 0)
			{
				//UnityEngine.Object.DestroyImmediate(groupObj_3_UnlinkedObjects);
				//Undo API로 변경
				apEditorUtil.SetRecordDestroyGameObject(groupObj_3_UnlinkedObjects);

				targetPortrait._bakeUnlinkedGroup = null;
			}


			//1-2. Masked Mesh 연결해주기
			for (int i = 0; i < targetPortrait._optMeshes.Count; i++)
			{
				apOptMesh optMesh = targetPortrait._optMeshes[i];

				//변경 v1.6.0
				//마스크 상태에 따른 링크를 한번에 처리
				optMesh.LinkOtherMeshesOnBake();

			}

			//2. 2차 Bake : Modifier 만들기
			List<apOptTransform> optTransforms = targetPortrait._optTransforms;
			for (int i = 0; i < optTransforms.Count; i++)
			{
				apOptTransform optTransform = optTransforms[i];

				apMeshGroup srcMeshGroup = targetPortrait.GetMeshGroup(optTransform._meshGroupUniqueID);
				optTransform.BakeModifier(targetPortrait, srcMeshGroup, isSizeOptimizedV117);
			}


			//3. 3차 Bake : ControlParam/KeyFrame ~~> Modifier <- [Calculated Param] -> OptTrasform + Mesh
			targetPortrait.SetFirstInitializeAfterBake();//이게 호출되어야 Initialize가 제대로 동작한다.
			targetPortrait.Initialize();

			//추가 20.8.10 [Flipped Scale 문제]
			//3.1 : 리깅 본 정보를 Initialize 직후에 Bake한다. (다만 옵션이 설정된 경우에 한해서)
			//Debug.LogError("Flipped Option : " + targetPortrait._flippedMeshOption);
			if (targetPortrait._flippedMeshOption == apPortrait.FLIPPED_MESH_CHECK.All)
			{
				for (int i = 0; i < optTransforms.Count; i++)
				{
					apOptTransform optTransform = optTransforms[i];

					//리깅이 된 optTransform은 연결된 본들을 입력해주자
					if (optTransform._childMesh != null && optTransform._isIgnoreParentModWorldMatrixByRigging)
					{
						SetRiggingOptBonesToOptTransform(optTransform);
					}
				}
			}



			//4. 첫번째 OptRoot만 보여주도록 하자
			if (targetPortrait._optRootUnitList.Count > 0)
			{
				targetPortrait.ShowRootUnitWhenBake(targetPortrait._optRootUnitList[0]);
			}

			//5. AnimClip의 데이터를 받아서 AnimPlay 데이터로 만들자
			if (targetPortrait._animPlayManager == null)
			{
				targetPortrait._animPlayManager = new apAnimPlayManager();
			}

			targetPortrait._animPlayManager.InitAndLink();
			targetPortrait._animPlayManager._animPlayDataList.Clear();

			for (int i = 0; i < targetPortrait._animClips.Count; i++)
			{
				apAnimClip animClip = targetPortrait._animClips[i];
				int animClipID = animClip._uniqueID;
				string animClipName = animClip._name;
				int targetMeshGroupID = animClip._targetMeshGroupID;

				apAnimPlayData animPlayData = new apAnimPlayData(animClipID, targetMeshGroupID, animClipName);
				targetPortrait._animPlayManager._animPlayDataList.Add(animPlayData);

			}

			//6. 한번 업데이트를 하자 (소켓들이 갱신된다)
			if (targetPortrait._optRootUnitList.Count > 0)
			{
				apOptRootUnit optRootUnit = null;
				for (int i = 0; i < targetPortrait._optRootUnitList.Count; i++)
				{
					//이전 : 함수가 너무 반복되어 래핑되었다. 함수를 제거한닷
					//targetPortrait._optRootUnitList[i].RemoveAllCalculateResultParams();

					//변경
					optRootUnit = targetPortrait._optRootUnitList[i];
					if (optRootUnit._rootOptTransform != null)
					{
						optRootUnit._rootOptTransform.ClearResultParams(true);
						optRootUnit._rootOptTransform.ResetCalculateStackForBake(true);
					}
					else
					{
						Debug.LogError("AnyPortrait : No Root Opt Transform on RootUnit");
					}
				}

				for (int i = 0; i < targetPortrait._optRootUnitList.Count; i++)
				{
					//업데이트
					targetPortrait._optRootUnitList[i].UpdateTransforms(0.0f, true, null);
					
				}
			}



			//6. Mask 메시 한번 더 갱신
			//if(targetPortrait._optMaskedMeshes.Count > 0)
			//{
			//	for (int i = 0; i < targetPortrait._optMaskedMeshes.Count; i++)
			//	{
			//		targetPortrait._optMaskedMeshes[i].RefreshMaskedMesh();
			//	}
			//}
			//> 변경 : Child 위주로 변경
			//if (targetPortrait._optClippedMeshes.Count > 0)
			//{
			//	for (int i = 0; i < targetPortrait._optClippedMeshes.Count; i++)
			//	{
			//		targetPortrait._optClippedMeshes[i].RefreshClippedMesh();
			//	}
			//}


			//추가 3.22 
			//6-2. LayerOrder 갱신하자
			string sortingLayerName = "";
			bool isValidSortingLayer = false;
			if (SortingLayer.IsValid(targetPortrait._sortingLayerID))
			{
				sortingLayerName = SortingLayer.IDToName(targetPortrait._sortingLayerID);
				isValidSortingLayer = true;
			}
			else
			{
				if (SortingLayer.layers.Length > 0)
				{
					sortingLayerName = SortingLayer.layers[0].name;
					isValidSortingLayer = true;
				}
				else
				{
					isValidSortingLayer = false;
				}
			}
			if (isValidSortingLayer)
			{
				targetPortrait.SetSortingLayer(sortingLayerName);
			}
			//변경 19.8.19 : 옵션이 적용되는 경우에 한해서
			if (targetPortrait._sortingOrderOption == apPortrait.SORTING_ORDER_OPTION.SetOrder)
			{
				targetPortrait.SetSortingOrder(targetPortrait._sortingOrder);
			}


			//추가 19.5.26
			//6-3. 최적화 옵션으로 Bake 되었는지 체크
			targetPortrait._isSizeOptimizedV117 = isSizeOptimizedV117;



			//7. 기본 GameObject 타입 (Mesh, MeshGroup, Modifier) 중에서 사용되지 않는 객체는 삭제해주자
			List<apMesh> usingMeshes = new List<apMesh>();
			List<apMeshGroup> usingMeshGroups = new List<apMeshGroup>();
			List<apModifierBase> usingModifiers = new List<apModifierBase>();

			for (int i = 0; i < targetPortrait._meshes.Count; i++)
			{
				targetPortrait._meshes[i].gameObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;

				usingMeshes.Add(targetPortrait._meshes[i]);
			}

			for (int i = 0; i < targetPortrait._meshGroups.Count; i++)
			{
				apMeshGroup meshGroup = targetPortrait._meshGroups[i];
				meshGroup.gameObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;

				usingMeshGroups.Add(meshGroup);

				for (int iMod = 0; iMod < meshGroup._modifierStack._modifiers.Count; iMod++)
				{
					meshGroup._modifierStack._modifiers[iMod].gameObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;

					usingModifiers.Add(meshGroup._modifierStack._modifiers[iMod]);
				}
			}

			CheckAndMakeObjectGroup();

			//각 서브 오브젝트 하위의 그룹들을 체크하여 유효하지 않는게 있는지 체크한다.

			List<GameObject> unusedMeshGameObjects = new List<GameObject>();
			List<GameObject> unusedMeshGroupGameObjects = new List<GameObject>();
			List<GameObject> unusedModifierGameObjects = new List<GameObject>();

			for (int iMesh = 0; iMesh < targetPortrait._subObjectGroup_Mesh.transform.childCount; iMesh++)
			{
				Transform meshTF = targetPortrait._subObjectGroup_Mesh.transform.GetChild(iMesh);
				apMesh targetMesh = meshTF.GetComponent<apMesh>();

				if (targetMesh == null)
				{
					//Mesh가 없는 GameObject 발견
					Debug.Log("No Mesh GameObject : " + meshTF.gameObject.name);

					unusedMeshGameObjects.Add(meshTF.gameObject);
				}
				else if (!usingMeshes.Contains(targetMesh))
				{
					//사용되지 않는 Mesh 발견
					Debug.Log("Unused Mesh Found : " + targetMesh._name);

					unusedMeshGameObjects.Add(meshTF.gameObject);
				}
			}

			for (int iMeshGroup = 0; iMeshGroup < targetPortrait._subObjectGroup_MeshGroup.transform.childCount; iMeshGroup++)
			{
				Transform meshGroupTF = targetPortrait._subObjectGroup_MeshGroup.transform.GetChild(iMeshGroup);
				apMeshGroup targetMeshGroup = meshGroupTF.GetComponent<apMeshGroup>();

				if (targetMeshGroup == null)
				{
					//MeshGroup이 없는 GameObject 발견
					//Debug.Log("No MeshGroup GameObject : " + meshGroupTF.gameObject.name);

					unusedMeshGroupGameObjects.Add(meshGroupTF.gameObject);
				}
				else if (!usingMeshGroups.Contains(targetMeshGroup))
				{
					//사용되지 않는 MeshGroup 발견
					//Debug.Log("Unused MeshGroup Found : " + targetMeshGroup._name);

					unusedMeshGroupGameObjects.Add(meshGroupTF.gameObject);
				}
			}

			for (int iMod = 0; iMod < targetPortrait._subObjectGroup_Modifier.transform.childCount; iMod++)
			{
				Transform modTF = targetPortrait._subObjectGroup_Modifier.transform.GetChild(iMod);
				apModifierBase targetMod = modTF.GetComponent<apModifierBase>();

				if (targetMod == null)
				{
					//Modifier가 없는 GameObject 발견
					//Debug.Log("No Modifier GameObject : " + modTF.gameObject.name);

					unusedModifierGameObjects.Add(modTF.gameObject);
				}
				else if (!usingModifiers.Contains(targetMod))
				{
					//사용되지 않는 Modifier 발견
					//Debug.Log("Unused Modifier Found : " + targetMod.DisplayName);

					unusedModifierGameObjects.Add(modTF.gameObject);
				}
			}

			//참조되지 않은건 삭제하자
			for (int i = 0; i < unusedMeshGameObjects.Count; i++)
			{
				//UnityEngine.Object.DestroyImmediate(unusedMeshGameObjects[i]);
				apEditorUtil.SetRecordDestroyGameObject(unusedMeshGameObjects[i]);
			}
			for (int i = 0; i < unusedMeshGroupGameObjects.Count; i++)
			{
				//UnityEngine.Object.DestroyImmediate(unusedMeshGroupGameObjects[i]);
				apEditorUtil.SetRecordDestroyGameObject(unusedMeshGroupGameObjects[i]);
			}
			for (int i = 0; i < unusedModifierGameObjects.Count; i++)
			{
				//UnityEngine.Object.DestroyImmediate(unusedModifierGameObjects[i]);
				apEditorUtil.SetRecordDestroyGameObject(unusedModifierGameObjects[i]);
			}

			//여기서 Opt 업뎃을 하나 할까..
			//targetPortrait.Hide();
			//targetPortrait.Show();
			//targetPortrait.UpdateForce();

			//추가3.22
			//Portrait가 Prefab이라면
			//Bake와 동시에 Apply를 해야한다.
			//if(apEditorUtil.IsPrefab(targetPortrait.gameObject))
			//{
			//	apEditorUtil.ApplyPrefab(targetPortrait.gameObject, true);
			//	//그리고 다시 Apply를 해제
			//	apEditorUtil.DisconnectPrefab(targetPortrait);
			//}

			//메카님 옵션이 켜져 있다면
			//1. Animation Clip들을 리소스로 생성한다.
			//2. Animator 컴포넌트를 추가한다.


			if (targetPortrait._isUsingMecanim)
			{
				//추가 3.22 : animClip 경로가 절대 경로인 경우, 여러 작업자가 공유해서 쓸 수 없다.
				//상대 경로로 바꾸는 작업을 해야한다.
				CheckAnimationsBasePathForV116(targetPortrait);

				CreateAnimationsWithMecanim(targetPortrait, targetPortrait._mecanimAnimClipResourcePath);
			}


			//추가 21.9.25 : 유니티 이벤트 (UnityEvent)를 사용한다면 Bake를 하자
			if(targetPortrait._unityEventWrapper == null)
			{
				targetPortrait._unityEventWrapper = new apUnityEventWrapper();
			}
			targetPortrait._unityEventWrapper.Bake(targetPortrait);


			apEditorUtil.SetDirty(_editor);

			//추가. Bake 후 처리
			ProcessAfterBake();

			//추가 19.10.26 : 빌보드 설정을 다시 복구
			targetPortrait._billboardType = billboardType;


			//추가 21.3.11
			// Scale 이슈가 있어서 저장된 값의 Scale로 복원
			foreach (KeyValuePair<Transform, Vector3> transform2Scale in prevTransformScales)
			{
				if(transform2Scale.Key != null)
				{
					transform2Scale.Key.localScale = transform2Scale.Value;
				}
			}



			//버그 수정 : 첫번째 루트 유닛만 보여야 하는데 그렇지 않은 경우 문제 해결
			//추가 22.1.9 : 루트 유닛이 여러개 있는 경우엔 첫번째 루트 유닛을 출력하자
			int nOptRootUnits = targetPortrait._optRootUnitList != null ? targetPortrait._optRootUnitList.Count : 0;
			if (nOptRootUnits > 1)
			{
				targetPortrait.ShowRootUnitWhenBake(targetPortrait._optRootUnitList[0]);
			}


			//Bake 후에는 Initialize를 하지 않은 상태로 되돌린다. (v1.4.3)
			targetPortrait.SetFirstInitializeAfterBake();


			//v1.6.0 Undo 통합
			apEditorUtil.EndRecordUndo();

			return bakeResult;
		}


		//-----------------------------------------------------------------
		// Optimized Bake
		//-----------------------------------------------------------------
		/// <summary>
		/// 현재 Portrait를 실행가능한 버전으로 Bake하자
		/// </summary>
		public apBakeResult Bake_Optimized(apPortrait srcPortrait, apPortrait targetOptPortrait)
		{
			if (srcPortrait == null)
			{
				return null;
			}


			//추가 20.11.7
			//이미지가 설정되지 않은 메시가 있다면 에러가 발생한다.
			//미리 안내를 하자
			if(!CheckIfAnyNoImageMesh(srcPortrait))
			{
				//에러가 발생해서 Bake 취소
				return null;
			}


			//추가 19.5.26 : v1.1.7에 추가된 "용량 최적화 옵션"이 적용되어 Bake를 하는가?
			bool isSizeOptimizedV117 = true;
			//bool isSizeOptimizedV117 = false;//테스트

			//추가 19.8.5
			//bool isUseSRP = Editor._isUseSRP;//이전
			bool isUseSRP = Editor.ProjectSettingData.Project_IsUseSRP;//변경 [v1.4.2]
			bool isBakeGammaColorSpace = Editor.ProjectSettingData.Project_IsColorSpaceGamma;//추가 [v1.4.2]


			//v1.6.0 : Undo
			apEditorUtil.EndRecordUndo();//기존의 Undo 기록이 있다면 여기서 한번 쉬어가자
			//원본 Portrait을 먼저 기록한다.
			apEditorUtil.SetRecordBeforeCreateOrDestroyObject(srcPortrait, apUndoGroupData.ACTION.Portrait_Bake);//Undo를 시작한다.

			//apEditorUtil.SetEditorDirty();
			//EditorUtility.SetDirty(srcPortrait);

			apBakeResult bakeResult = new apBakeResult();

			//Optimized에서 타겟이 되는 Portrait가 없다면 새로 만들어준다.
			if (targetOptPortrait == null)
			{
				GameObject dstPortraitGameObj = new GameObject(srcPortrait.gameObject.name + " (Optimized)");
				//Undo 등록
				apEditorUtil.SetRecordCreatedGameObject(dstPortraitGameObj);

				//dstPortraitGameObj.transform.parent = srcPortrait.transform.parent;//이전
				//v1.6.0 : Undo API 이용
				apEditorUtil.SetParentWithRecord(dstPortraitGameObj.transform, srcPortrait.transform.parent);

				dstPortraitGameObj.transform.localPosition = srcPortrait.transform.localPosition;
				dstPortraitGameObj.transform.localRotation = srcPortrait.transform.localRotation;
				dstPortraitGameObj.transform.localScale = srcPortrait.transform.localScale;

				dstPortraitGameObj.layer = srcPortrait.gameObject.layer;

				//targetOptPortrait = dstPortraitGameObj.AddComponent<apPortrait>();//이전
				targetOptPortrait = apEditorUtil.AddComponentWithRecord<apPortrait>(dstPortraitGameObj);//추가 v1.6.0 : Undo 등록
			}
			else
			{
				//타겟이 있다면
				//Undo 등록
				apEditorUtil.SetRecordAnyObjectComplete(targetOptPortrait);//스냅샷 방식으로 등록
			}

			//추가 20.9.14 : 만약 targetOptPortrait가 Prefab으로 만들어진 상태라면, 연결을 끊어야 한다. (안그러면 에러가 난다.)
			//갱신 > 조회 > 안내 > Disconnect 순서
			apEditorUtil.CheckAndRefreshPrefabInfo(targetOptPortrait);

			if (apEditorUtil.IsPrefabConnected(targetOptPortrait.gameObject))
			{
				//Prefab 해제 안내
				if (EditorUtility.DisplayDialog(	Editor.GetText(TEXT.DLG_PrefabDisconn_Title),
													Editor.GetText(TEXT.DLG_PrefabDisconn_Body),
													Editor.GetText(TEXT.Okay)))
				{	
					apEditorUtil.DisconnectPrefab(targetOptPortrait);
				}
			}



			//< Optimized Bake와 일반 Bake의 차이 >
			//- 순서는 일반 Bake와 동일하게 처리된다. (참조 에러를 막기 위해 Instantiate 등의 방법을 제외한다)
			//- 생성/제거되는 GameObject는 모두 taretOptPortrait에 속한다.
			//- 데이터는 srcPortrait에서 가져온다.
			//- 이 코드내에 Editor._portrait는 한번도 등장해선 안된다.

			//< 일단 Bake 했으니 초기 정보를 연결해준다. >
			//0. Bake 했다는 기본 정보 복사
			targetOptPortrait._isOptimizedPortrait = true;
			targetOptPortrait._bakeSrcEditablePortrait = srcPortrait;

			srcPortrait._bakeTargetOptPortrait = targetOptPortrait;

			//Editable GameObject로 저장되는 정보를 제외하고 모두 복사한다.
			//1. Controller 복사
			targetOptPortrait._controller._portrait = targetOptPortrait;
			targetOptPortrait._controller._controlParams.Clear();

			for (int iCP = 0; iCP < srcPortrait._controller._controlParams.Count; iCP++)
			{
				apControlParam srcParam = srcPortrait._controller._controlParams[iCP];

				apControlParam newParam = new apControlParam();
				newParam._portrait = targetOptPortrait;
				newParam.CopyFromControlParam(srcParam);//<<복사하자

				//리스트에 추가
				targetOptPortrait._controller._controlParams.Add(newParam);
			}

			//2. AnimClip 복사 (링크정보에 관한건 제외하고)
			// (AnimPlayManager는 나중에 Link하면 자동으로 연결됨)

			//추가 10.5 : 기존에 생성되었던 Animation Clip Asset은 없어지면 안된다.
			Dictionary<int, AnimationClip> animID2AnimAssets = new Dictionary<int, AnimationClip>();
			if (targetOptPortrait._animClips != null && targetOptPortrait._animClips.Count > 0)
			{
				for (int i = 0; i < targetOptPortrait._animClips.Count; i++)
				{
					apAnimClip beforeAnimClip = targetOptPortrait._animClips[i];
					if (beforeAnimClip != null && beforeAnimClip._animationClipForMecanim != null)
					{
						if (!animID2AnimAssets.ContainsKey(beforeAnimClip._uniqueID))
						{
							animID2AnimAssets.Add(beforeAnimClip._uniqueID, beforeAnimClip._animationClipForMecanim);
						}
					}
				}
			}
			targetOptPortrait._animClips.Clear();

			for (int iAnim = 0; iAnim < srcPortrait._animClips.Count; iAnim++)
			{
				apAnimClip srcAnimClip = srcPortrait._animClips[iAnim];

				//AnimClip을 Src로 부터 복사해서 넣자
				apAnimClip newAnimClip = new apAnimClip();
				newAnimClip.CopyFromAnimClip(srcAnimClip);

				if (animID2AnimAssets.ContainsKey(newAnimClip._uniqueID))
				{
					//추가 : Mecanim에 사용된 AnimAsset을 재활용해야한다.
					newAnimClip._animationClipForMecanim = animID2AnimAssets[newAnimClip._uniqueID];
				}

				targetOptPortrait._animClips.Add(newAnimClip);
			}

			//3. MainMeshGroup ID 복사
			targetOptPortrait._mainMeshGroupIDList.Clear();
			for (int iMainMG = 0; iMainMG < srcPortrait._mainMeshGroupIDList.Count; iMainMG++)
			{
				//ID(int) 복사
				targetOptPortrait._mainMeshGroupIDList.Add(srcPortrait._mainMeshGroupIDList[iMainMG]);
			}

			//4. 다른 정보들 복사
			targetOptPortrait._FPS = srcPortrait._FPS;

			targetOptPortrait._bakeScale = srcPortrait._bakeScale;
			targetOptPortrait._bakeZSize = srcPortrait._bakeZSize;

			targetOptPortrait._imageFilePath_Thumbnail = srcPortrait._imageFilePath_Thumbnail;

			targetOptPortrait._isImportant = srcPortrait._isImportant;
			targetOptPortrait._autoPlayAnimClipID = srcPortrait._autoPlayAnimClipID;

			targetOptPortrait._sortingLayerID = srcPortrait._sortingLayerID;
			targetOptPortrait._sortingOrder = srcPortrait._sortingOrder;

			targetOptPortrait._isUsingMecanim = srcPortrait._isUsingMecanim;
			targetOptPortrait._mecanimAnimClipResourcePath = srcPortrait._mecanimAnimClipResourcePath;

			targetOptPortrait._billboardType = srcPortrait._billboardType;
			targetOptPortrait._meshShadowCastingMode = srcPortrait._meshShadowCastingMode;
			targetOptPortrait._meshReceiveShadow = srcPortrait._meshReceiveShadow;

			//[v1.5.0 추가]
			targetOptPortrait._billboardParentRotation = srcPortrait._billboardParentRotation;

			//[v1.6.0 추가] 이건 복제 말고 프로젝트 옵션을 통해서 저장
			if(isUseSRP)
			{
				targetOptPortrait._renderPipelineOption = apPortrait.RENDER_PIPELINE_OPTION.SRP;
			}
			else
			{
				targetOptPortrait._renderPipelineOption = apPortrait.RENDER_PIPELINE_OPTION.BuiltIn;
			}

			//[v1.5.0 추가]
			targetOptPortrait._meshLightProbeUsage = srcPortrait._meshLightProbeUsage;
			targetOptPortrait._meshReflectionProbeUsage = srcPortrait._meshReflectionProbeUsage;

			targetOptPortrait._vrRenderTextureSize = srcPortrait._vrRenderTextureSize;
			targetOptPortrait._vrSupportMode = srcPortrait._vrSupportMode;
			targetOptPortrait._flippedMeshOption = srcPortrait._flippedMeshOption;
			targetOptPortrait._rootBoneScaleMethod = srcPortrait._rootBoneScaleMethod;//<<이것도 추가

			//추가 [v1.4.0]
			targetOptPortrait._isTeleportCorrectionOption = srcPortrait._isTeleportCorrectionOption;
			targetOptPortrait._teleportMovementDist = srcPortrait._teleportMovementDist;

			//추가 [v1.5.0] : 텔레포트 옵션 추가
			targetOptPortrait._teleportRotationOffset = srcPortrait._teleportRotationOffset;
			targetOptPortrait._teleportScaleOffset = srcPortrait._teleportScaleOffset;
			targetOptPortrait._teleportPositionEnabled = srcPortrait._teleportPositionEnabled;
			targetOptPortrait._teleportRotationEnabled = srcPortrait._teleportRotationEnabled;
			targetOptPortrait._teleportScaleEnabled = srcPortrait._teleportScaleEnabled;

			targetOptPortrait._unspecifiedAnimControlParamOption = srcPortrait._unspecifiedAnimControlParamOption;


			//추가 [v1.4.8] 옵션 복사
			targetOptPortrait._meshRefreshRateOption = srcPortrait._meshRefreshRateOption;
			targetOptPortrait._meshRefreshRateFPS = srcPortrait._meshRefreshRateFPS;
			targetOptPortrait._mainProcessEvent = srcPortrait._mainProcessEvent;

			//추가 [v1.4.9]
			targetOptPortrait._meshRefreshFPSScaleOption = srcPortrait._meshRefreshFPSScaleOption;

			//추가 [v1.4.8]
			targetOptPortrait._rootMotionModeOption = srcPortrait._rootMotionModeOption;			
			targetOptPortrait._rootMotionAxisOption_X = srcPortrait._rootMotionAxisOption_X;
			targetOptPortrait._rootMotionAxisOption_Y = srcPortrait._rootMotionAxisOption_Y;
			targetOptPortrait._rootMotionTargetTransformType = srcPortrait._rootMotionTargetTransformType;

			//주의 : 루트 모션 중 "지정된 Parent Transform 객체"는 복사하면 안된다.
			//targetOptPortrait._rootMotionSpecifiedParentTransform = srcPortrait._rootMotionSpecifiedParentTransform;//<<이거 주석 풀지 말것.

			//추가 [v1.5.1]
			targetOptPortrait._invisibleMeshUpdate = srcPortrait._invisibleMeshUpdate;
			targetOptPortrait._clippingMeshUpdate = srcPortrait._clippingMeshUpdate;


			//4-2. Material Set 복사
			targetOptPortrait._materialSets.Clear();
			for (int i = 0; i < srcPortrait._materialSets.Count; i++)
			{
				apMaterialSet srcMatSet = srcPortrait._materialSets[i];
				apMaterialSet copiedMatSet = new apMaterialSet();
				copiedMatSet.CopyFromSrc(srcMatSet, srcMatSet._uniqueID, false, false, srcMatSet._isDefault);
				targetOptPortrait._materialSets.Add(copiedMatSet);
			}


			//추가 10.26 : Bake에서는 빌보드가 꺼져야 한다.
			//임시로 껐다가 마지막에 다시 복구
			apPortrait.BILLBOARD_TYPE billboardType = targetOptPortrait._billboardType;
			targetOptPortrait._billboardType = apPortrait.BILLBOARD_TYPE.None;//임시로 끄자

			//추가 21.3.11
			// Scale 이슈가 있다.
			// Bake 전에 이미 Scale이 음수인 경우, Bake 직후나 Link후 플레이시 메시가 거꾸로 보이게 된다.
			//따라서 portrait부터 시작해서 상위의 모든 GameObject의 Sca;e을 저장했다가 복원해야한다.
			Dictionary<Transform, Vector3> prevTransformScales = new Dictionary<Transform, Vector3>();
			Transform curScaleCheckTransform = targetOptPortrait.transform;
			while(true)
			{
				prevTransformScales.Add(curScaleCheckTransform, curScaleCheckTransform.localScale);
				curScaleCheckTransform.localScale = Vector3.one;//일단 기본으로 강제 적용
				if(curScaleCheckTransform.parent == null)
				{
					break;
				}
				curScaleCheckTransform = curScaleCheckTransform.parent;
			}

			// 지금부터는 일반 Bake처럼 진행이 된다.
			// 1. Editor._portrait대신 targetOptPortrait를 사용한다.
			// 2. 데이터는 Mesh, MeshGroup, Modifier 정보는 srcPortrait 정보를 사용한다.


			//Bake 방식 변경
			//일단 숨겨진 GameObject를 제외한 모든 객체를 리스트로 저장한다.
			//LinkParam 형태로 저장을 한다.
			//LinkParam으로 저장하면서 <apOpt 객체>와 <그렇지 않은 객체>를 구분한다.
			//"apOpt 객체"는 나중에 (1)재활용 할지 (2) 삭제 할지 결정한다.
			//"그렇지 않은 GameObject"는 Hierarchy 정보를 가진채 (1) 링크를 유지할 지(재활용되는 경우) (2) Unlink Group에 넣을지 결정한다.
			//만약 재활용되지 않는 (apOpt GameObject)에서 알수 없는 Component가 발견된 경우 -> 이건 삭제 예외 대상에 넣는다.

			//분류를 위한 그룹
			//1. ReadyToRecycle
			// : 기존에 RootUnit과 그 하위에 있었던 GameObject들이다. 분류 전에 일단 여기로 들어간다.
			// : 분류 후에는 원칙적으로 하위에 어떤 객체도 남아선 안된다.

			//2. RemoveTargets
			// : apOpt를 가진 GameObject 그룹 중에서 사용되지 않았던 그룹이다. 
			// : 처리 후에는 이 GameObject를 통째로 삭제한다.

			//3. UnlinkedObjects
			// : apOpt를 가지지 않은 GameObject중에서 재활용되지 않은 객체들


			GameObject groupObj_1_ReadyToRecycle = new GameObject("__Baking_1_ReadyToRecycle");//<Undo
			GameObject groupObj_2_RemoveTargets = new GameObject("__Baking_2_RemoveTargets");//<Undo

			//v1.6.0 : Undo API로 변경
			apEditorUtil.SetRecordCreatedGameObject(groupObj_1_ReadyToRecycle);
			apEditorUtil.SetRecordCreatedGameObject(groupObj_2_RemoveTargets);

			GameObject groupObj_3_UnlinkedObjects = null;//이건 실제로 삭제되지 않고 남을 수도 있다.
			if (targetOptPortrait._bakeUnlinkedGroup == null)
			{
				groupObj_3_UnlinkedObjects = new GameObject("__UnlinkedObjects");//<Undo
				
				//v1.6.0 Undo 등록
				apEditorUtil.SetRecordCreatedGameObject(groupObj_3_UnlinkedObjects);

				targetOptPortrait._bakeUnlinkedGroup = groupObj_3_UnlinkedObjects;
			}
			else
			{
				groupObj_3_UnlinkedObjects = targetOptPortrait._bakeUnlinkedGroup;

				//v1.6.0 Undo 등록
				apEditorUtil.SetRecordAnyObjectComplete(groupObj_3_UnlinkedObjects);

				groupObj_3_UnlinkedObjects.name = "__UnlinkedObjects";
			}

			//이전
			// groupObj_1_ReadyToRecycle.transform.parent = targetOptPortrait.transform;//이전
			// groupObj_2_RemoveTargets.transform.parent = targetOptPortrait.transform;//이전
			// groupObj_3_UnlinkedObjects.transform.parent = targetOptPortrait.transform;//이전

			//변경 v1.6.0 : Undo API 이용
			apEditorUtil.SetParentWithRecord(groupObj_1_ReadyToRecycle.transform, targetOptPortrait.transform);
			apEditorUtil.SetParentWithRecord(groupObj_2_RemoveTargets.transform, targetOptPortrait.transform);
			apEditorUtil.SetParentWithRecord(groupObj_3_UnlinkedObjects.transform, targetOptPortrait.transform);

			//Undo.SetTransformParent()

			groupObj_1_ReadyToRecycle.transform.localPosition = Vector3.zero;
			groupObj_2_RemoveTargets.transform.localPosition = Vector3.zero;
			groupObj_3_UnlinkedObjects.transform.localPosition = Vector3.zero;

			groupObj_1_ReadyToRecycle.transform.localRotation = Quaternion.identity;
			groupObj_2_RemoveTargets.transform.localRotation = Quaternion.identity;
			groupObj_3_UnlinkedObjects.transform.localRotation = Quaternion.identity;

			groupObj_1_ReadyToRecycle.transform.localScale = Vector3.one;
			groupObj_2_RemoveTargets.transform.localScale = Vector3.one;
			groupObj_3_UnlinkedObjects.transform.localScale = Vector3.one;


			//2. 기존 RootUnit을 Recycle로 옮긴다.
			//옮기면서 "Prev List"를 만들어야 한다. Recycle을 하기 위함
			List<apOptRootUnit> prevOptRootUnits = new List<apOptRootUnit>();
			if (targetOptPortrait._optRootUnitList != null)
			{
				for (int i = 0; i < targetOptPortrait._optRootUnitList.Count; i++)
				{
					apOptRootUnit optRootUnit = targetOptPortrait._optRootUnitList[i];
					if (optRootUnit != null)
					{
						//optRootUnit.transform.parent = groupObj_1_ReadyToRecycle.transform;

						//변경 v1.6.0 : Undo API 이용
						apEditorUtil.SetParentWithRecord(optRootUnit.transform, groupObj_1_ReadyToRecycle.transform);

						prevOptRootUnits.Add(optRootUnit);
					}
				}
			}


			//RootUnit 리스트를 초기화한다.
			if (targetOptPortrait._optRootUnitList == null)
			{
				targetOptPortrait._optRootUnitList = new List<apOptRootUnit>();
			}

			targetOptPortrait._optRootUnitList.Clear();
			targetOptPortrait._curPlayingOptRootUnit = null;

			if (targetOptPortrait._optTransforms == null) { targetOptPortrait._optTransforms = new List<apOptTransform>(); }
			if (targetOptPortrait._optMeshes == null) { targetOptPortrait._optMeshes = new List<apOptMesh>(); }
			if (targetOptPortrait._optTextureData == null) { targetOptPortrait._optTextureData = new List<apOptTextureData>(); }//<<텍스쳐 데이터 추가

			targetOptPortrait._optTransforms.Clear();
			targetOptPortrait._optMeshes.Clear();
			targetOptPortrait._optTextureData.Clear();

			//추가
			//Batched Matrial 관리 객체가 생겼다.
			if (targetOptPortrait._optBatchedMaterial == null)
			{
				targetOptPortrait._optBatchedMaterial = new apOptBatchedMaterial();
			}
			else
			{
				targetOptPortrait._optBatchedMaterial.Clear(true);//<<이미 생성되어 있다면 초기화
			}

			////추가 11.6 : LWRP Shader를 사용하는지 체크하고, 필요한 경우 생성해야한다.
			//CheckAndCreateLWRPShader();


			// srcPortrait로 부터 가져온 데이터는 앞에 src를 붙인다.

			//3. 텍스쳐 데이터를 먼저 만들자.
			// Src -> Target
			for (int i = 0; i < srcPortrait._textureData.Count; i++)
			{
				apTextureData srcTextureData = srcPortrait._textureData[i];
				apOptTextureData newOptTexData = new apOptTextureData();

				newOptTexData.Bake(i, srcTextureData);
				targetOptPortrait._optTextureData.Add(newOptTexData);
			}


			//추가 20.1.28 : Color Space가 동일하도록 묻고 변경
			CheckAndChangeTextureDataColorSpace(srcPortrait);

			//4. 추가 : Reset
			srcPortrait.LinkAndRefreshInEditor(false, apUtil.LinkRefresh.Set_AllObjects(null)); // Source를 먼저 준비


			//4. OptTransform을 만들자 (RootUnit부터)
			// Src -> Taret
			for (int i = 0; i < srcPortrait._rootUnits.Count; i++)
			{
				apRootUnit srcRootUnit = srcPortrait._rootUnits[i];

				//추가 : 계층구조의 MeshGroup인 경우 이 코드가 추가되어야 한다.
				if (srcRootUnit._childMeshGroup != null)
				{
					srcRootUnit._childMeshGroup.SortRenderUnits(true, apMeshGroup.DEPTH_ASSIGN.OnlySort);//렌더 유닛의 Depth를 다시 계산해야한다. <<
					srcRootUnit._childMeshGroup.LinkModMeshRenderUnits(null);
					srcRootUnit._childMeshGroup.RefreshModifierLink(null);
				}

				//업데이트를 한번 해주자
				srcRootUnit.Update(0.0f, false, false);

				apOptRootUnit optRootUnit = null;

				//1. Root Unit
				//재활용 가능한지 판단한다.
				bool isRecycledRootUnit = false;
				apOptRootUnit recycledOptRootUnit = GetRecycledRootUnit(srcRootUnit, prevOptRootUnits);

				if (recycledOptRootUnit != null)
				{

					//재활용이 된다.
					optRootUnit = recycledOptRootUnit;

					//Undo 등록
					apEditorUtil.SetRecordAnyObject(optRootUnit);

					//일부 값은 다시 리셋
					optRootUnit.name = "Root Portrait " + i;
					optRootUnit._portrait = targetOptPortrait;
					optRootUnit._transform = optRootUnit.transform;

					//optRootUnit.transform.parent = targetOptPortrait.transform;
					//변경 v1.6.0 : Undo API 이용
					apEditorUtil.SetParentWithRecord(optRootUnit.transform, targetOptPortrait.transform);


					optRootUnit.transform.localPosition = Vector3.zero;
					optRootUnit.transform.localRotation = Quaternion.identity;
					optRootUnit.transform.localScale = Vector3.one;

					//재활용에 성공했으니 OptUnit은 제외한다.
					prevOptRootUnits.Remove(recycledOptRootUnit);
					isRecycledRootUnit = true;

					//Count+1 : Recycled Opt
					bakeResult.AddCount_RecycledOptGameObject();
				}
				else
				{
					//새로운 RootUnit이다.
					optRootUnit = AddGameObject<apOptRootUnit>("Root Portrait " + i, targetOptPortrait.transform);

					optRootUnit._portrait = targetOptPortrait;
					optRootUnit._rootOptTransform = null;
					optRootUnit._transform = optRootUnit.transform;

					//Count+1 : New Opt
					bakeResult.AddCount_NewOptGameObject();
				}

				optRootUnit.ClearChildLinks();//Child Link를 초기화한다.

				//추가 12.6 : SortedRenderBuffer에 관련한 Bake 코드 <<
				optRootUnit.BakeSortedRenderBuffer(targetOptPortrait, srcRootUnit);

				targetOptPortrait._optRootUnitList.Add(optRootUnit);



				//재활용에 성공했다면
				//기존의 GameObject + Bake 여부를 재귀적 리스트로 작성한다.
				apBakeLinkManager bakeLinkManager = null;
				if (isRecycledRootUnit)
				{
					bakeLinkManager = new apBakeLinkManager();

					//파싱하자.
					bakeLinkManager.Parse(optRootUnit._rootOptTransform.gameObject, recycledOptRootUnit.gameObject);
				}

				apMeshGroup srcChildMainMeshGroup = srcRootUnit._childMeshGroup;

				//v1.6.0 추가 : Mask를 받는 메시들을 따로 받자
				HashSet<apTransform_Mesh> maskReceivedMeshTFs = new HashSet<apTransform_Mesh>();

				//0. 추가
				//일부 Modified Mesh를 갱신해야한다.
				if (srcChildMainMeshGroup != null && srcRootUnit._childMeshGroupTransform != null)
				{
					//Refresh를 한번 해주자
					srcChildMainMeshGroup.RefreshForce();

					List<apModifierBase> srcModifiers = srcChildMainMeshGroup._modifierStack._modifiers;
					for (int iMod = 0; iMod < srcModifiers.Count; iMod++)
					{
						apModifierBase mod = srcModifiers[iMod];
						if (mod._paramSetGroup_controller != null)
						{
							for (int iPSG = 0; iPSG < mod._paramSetGroup_controller.Count; iPSG++)
							{
								apModifierParamSetGroup psg = mod._paramSetGroup_controller[iPSG];
								for (int iPS = 0; iPS < psg._paramSetList.Count; iPS++)
								{
									apModifierParamSet ps = psg._paramSetList[iPS];
									ps.UpdateBeforeBake(srcPortrait, srcChildMainMeshGroup, srcRootUnit._childMeshGroupTransform);
								}
							}
						}
					}

					//v1.6.0 : Mask를 받는 MeshTransform을 찾자
					CollectMaskReceivedMeshes(	srcRootUnit._childMeshGroupTransform,
												srcRootUnit._childMeshGroupTransform,
												maskReceivedMeshTFs);
				}

				//1. 1차 Bake : GameObject 만들기
				if (srcChildMainMeshGroup != null && srcRootUnit._childMeshGroupTransform != null)
				{
					//정렬 한번 해주고
					srcChildMainMeshGroup.SortRenderUnits(true, apMeshGroup.DEPTH_ASSIGN.OnlySort);

					apRenderUnit srcRootRenderUnit = srcChildMainMeshGroup._rootRenderUnit;
					//apRenderUnit rootRenderUnit = Editor._portrait._rootUnit._renderUnit;
					if (srcRootRenderUnit != null)
					{
						//apTransform_MeshGroup meshGroupTransform = Editor._portrait._rootUnit._childMeshGroupTransform;
						apTransform_MeshGroup srcMeshGroupTransform = srcRootRenderUnit._meshGroupTransform;

						if (srcMeshGroupTransform == null)
						{
							Debug.LogError("Bake Error : MeshGroupTransform Not Found [" + srcChildMainMeshGroup._name + "]");
						}
						else
						{
							MakeMeshGroupToOptTransform(srcRootRenderUnit,
															srcMeshGroupTransform,
															optRootUnit.transform,
															null,
															optRootUnit,
															bakeLinkManager,
															bakeResult,
															targetOptPortrait._bakeZSize,
															
															//Editor._isBakeColorSpaceToGamma,//<<감마 색상 공간으로 Bake할 것인가
															isBakeGammaColorSpace,//로컬 변수로 변경 v1.4.2
															
															//삭제
															//Editor._isUseSRP,//LWRP Shader를 사용할 것인가

															targetOptPortrait,
															srcChildMainMeshGroup,
															isSizeOptimizedV117,
															isUseSRP,
															maskReceivedMeshTFs//v1.6.0
															);
							//MakeMeshGroupToOptTransform(null, meshGroupTransform, Editor._portrait._optRootUnit.transform, null);
						}
					}
					else
					{
						Debug.LogError("Bake Error : RootMeshGroup Not Found [" + srcChildMainMeshGroup._name + "]");
					}
				}



				//optRootUnit.transform.localScale = Vector3.one * 0.01f;
				optRootUnit.transform.localScale = Vector3.one * targetOptPortrait._bakeScale;


				// 이전에 Bake 했던 정보에서 가져왔다면
				//만약 "재활용되지 않은 GameObject"를 찾아서 별도의 처리를 해야한다.
				if (isRecycledRootUnit && bakeLinkManager != null)
				{
					bakeLinkManager.SetHierarchyNotRecycledObjects(groupObj_1_ReadyToRecycle, groupObj_2_RemoveTargets, groupObj_3_UnlinkedObjects, bakeResult);

				}


				//추가 v1.4.8 : 루트 모션 설정을 입력하자
				optRootUnit._rootMotionBoneID = -1;
				if(srcChildMainMeshGroup != null)
				{
					//루트 모션용 본이 존재하는지 확인하자
					apBone rootMotionBone = srcChildMainMeshGroup.GetBone(srcChildMainMeshGroup._rootMotionBoneID);
					if(rootMotionBone != null)
					{
						//루트 모션 본이 존재한다면 ID를 할당한다.
						optRootUnit._rootMotionBoneID = srcChildMainMeshGroup._rootMotionBoneID;
					}
				}

				//추가 12.6 : Bake 함수 추가 <<
				optRootUnit.BakeComplete();


			}


			if (prevOptRootUnits.Count > 0)
			{
				//이 유닛들은 Remove Target으로 이동해야 한다.
				apOptRootUnit curPrevoptRootUnit = null;
				for (int i = 0; i < prevOptRootUnits.Count; i++)
				{
					curPrevoptRootUnit = prevOptRootUnits[i];//변경 1.4.5

					//[v1.4.5] 오류 검출
					if(curPrevoptRootUnit == null
						|| curPrevoptRootUnit.transform == null)
					{
						Debug.LogWarning("AnyPortrait : Bake warning. Since the previous root unit is null, some objects may not be created or deleted properly.");
						continue;
					}

					//curPrevoptRootUnit.transform.parent = groupObj_2_RemoveTargets.transform;//이전
					//변경 v1.6.0 : Undo API 이용
					apEditorUtil.SetParentWithRecord(curPrevoptRootUnit.transform, groupObj_2_RemoveTargets.transform);

					//[v1.4.5] 연결이 해제된 상태에서 Bake를 다시 실행할 때 Null 체크
					if (curPrevoptRootUnit._rootOptTransform == null)
					{	
						Debug.LogWarning("AnyPortrait : Bake warning. Some subobjects of the unused Root Unit have already been deleted, so moving them to the Unlinked group for preservation failed.");
						continue;
					}

					//만약 여기서 알수없는 GameObject나 Compnent에 대해서는 Remove가 아니라 Unlink로 옮겨야 한다.
					apBakeLinkManager prevBakeManager = new apBakeLinkManager();
					prevBakeManager.Parse(curPrevoptRootUnit._rootOptTransform.gameObject, null);

					prevBakeManager.SetHierarchyToUnlink(groupObj_3_UnlinkedObjects, bakeResult);

				}
			}


			//TODO: 이제 그룹을 삭제하던가 경고 다이얼로그를 띄워주던가 하자
			// UnityEngine.Object.DestroyImmediate(groupObj_1_ReadyToRecycle);
			// UnityEngine.Object.DestroyImmediate(groupObj_2_RemoveTargets);
			// Undo API로 변경
			apEditorUtil.SetRecordDestroyGameObject(groupObj_1_ReadyToRecycle);
			apEditorUtil.SetRecordDestroyGameObject(groupObj_2_RemoveTargets);

			if (groupObj_3_UnlinkedObjects.transform.childCount == 0)
			{
				//UnityEngine.Object.DestroyImmediate(groupObj_3_UnlinkedObjects);
				//Undo API로 변경
				apEditorUtil.SetRecordDestroyGameObject(groupObj_3_UnlinkedObjects);

				targetOptPortrait._bakeUnlinkedGroup = null;
			}


			for (int i = 0; i < targetOptPortrait._optMeshes.Count; i++)
			{
				apOptMesh optMesh = targetOptPortrait._optMeshes[i];
				
				//변경 v1.6.0
				//마스크 상태에 따른 링크를 한번에 처리
				optMesh.LinkOtherMeshesOnBake();
			}

			//2. 2차 Bake : Modifier 만들기
			List<apOptTransform> optTransforms = targetOptPortrait._optTransforms;
			for (int i = 0; i < optTransforms.Count; i++)
			{
				apOptTransform optTransform = optTransforms[i];

				apMeshGroup srcMeshGroup = srcPortrait.GetMeshGroup(optTransform._meshGroupUniqueID);
				optTransform.BakeModifier(targetOptPortrait, srcMeshGroup, isSizeOptimizedV117);
			}


			//3. 3차 Bake : ControlParam/KeyFrame ~~> Modifier <- [Calculated Param] -> OptTrasform + Mesh
			targetOptPortrait.SetFirstInitializeAfterBake();
			targetOptPortrait.Initialize();

			//추가 20.8.10 [Flipped Scale 문제]
			//3.1 : 리깅 본 정보를 Initialize 직후에 Bake한다.
			if (targetOptPortrait._flippedMeshOption == apPortrait.FLIPPED_MESH_CHECK.All)
			{
				for (int i = 0; i < optTransforms.Count; i++)
				{
					apOptTransform optTransform = optTransforms[i];

					//리깅이 된 optTransform은 연결된 본들을 입력해주자
					if (optTransform._childMesh != null && optTransform._isIgnoreParentModWorldMatrixByRigging)
					{
						SetRiggingOptBonesToOptTransform(optTransform);
					}
				}
			}



			//4. 첫번째 OptRoot만 보여주도록 하자
			if (targetOptPortrait._optRootUnitList.Count > 0)
			{
				targetOptPortrait.ShowRootUnitWhenBake(targetOptPortrait._optRootUnitList[0]);
			}


			//5. AnimClip의 데이터를 받아서 AnimPlay 데이터로 만들자
			if (targetOptPortrait._animPlayManager == null)
			{
				targetOptPortrait._animPlayManager = new apAnimPlayManager();
			}

			targetOptPortrait._animPlayManager.InitAndLink();
			targetOptPortrait._animPlayManager._animPlayDataList.Clear();

			for (int i = 0; i < targetOptPortrait._animClips.Count; i++)
			{
				apAnimClip animClip = targetOptPortrait._animClips[i];
				int animClipID = animClip._uniqueID;
				string animClipName = animClip._name;
				int targetMeshGroupID = animClip._targetMeshGroupID;

				apAnimPlayData animPlayData = new apAnimPlayData(animClipID, targetMeshGroupID, animClipName);
				targetOptPortrait._animPlayManager._animPlayDataList.Add(animPlayData);

			}


			//6. 한번 업데이트를 하자 (소켓들이 갱신된다)
			if (targetOptPortrait._optRootUnitList.Count > 0)
			{
				apOptRootUnit optRootUnit = null;
				for (int i = 0; i < targetOptPortrait._optRootUnitList.Count; i++)
				{
					//이전
					//taretOptPortrait._optRootUnitList[i].RemoveAllCalculateResultParams();

					//변경
					optRootUnit = targetOptPortrait._optRootUnitList[i];
					if (optRootUnit._rootOptTransform != null)
					{
						optRootUnit._rootOptTransform.ClearResultParams(true);
						optRootUnit._rootOptTransform.ResetCalculateStackForBake(true);
					}
					else
					{
						Debug.LogError("AnyPortrait : No Root Opt Transform on RootUnit (OptBake)");
					}
				}

				//추가 3.22 : Bake후 메시가 변경되었을 경우에 다시 리셋할 필요가 있다.
				//for (int i = 0; i < taretOptPortrait._optRootUnitList.Count; i++)
				//{
				//	taretOptPortrait._optRootUnitList[i].ResetCalculateStackForBake();
				//}

				for (int i = 0; i < targetOptPortrait._optRootUnitList.Count; i++)
				{
					targetOptPortrait._optRootUnitList[i].UpdateTransforms(0.0f, true, null);
				}
			}
			//taretOptPortrait.ResetMeshesCommandBuffers(false);

			//taretOptPortrait.UpdateForce();

			// 원래는 "사용하지 않는 Mesh, MeshGroup 등을 삭제하는 코드"가 있는데,
			// Opt에서는 필요가 없다.
			//추가 3.22 
			//6-2. LayerOrder 갱신하자
			string sortingLayerName = "";
			bool isValidSortingLayer = false;
			if (SortingLayer.IsValid(Editor._portrait._sortingLayerID))
			{
				sortingLayerName = SortingLayer.IDToName(Editor._portrait._sortingLayerID);
				isValidSortingLayer = true;
			}
			else
			{
				if (SortingLayer.layers.Length > 0)
				{
					sortingLayerName = SortingLayer.layers[0].name;
					isValidSortingLayer = true;
				}
				else
				{
					isValidSortingLayer = false;
				}
			}
			if (isValidSortingLayer)
			{
				targetOptPortrait.SetSortingLayer(sortingLayerName);
			}
			//변경 19.8.19 : 옵션이 적용되는 경우에 한해서
			if (Editor._portrait._sortingOrderOption == apPortrait.SORTING_ORDER_OPTION.SetOrder)
			{
				targetOptPortrait.SetSortingOrder(Editor._portrait._sortingOrder);
			}


			//추가 19.5.26
			//6-3. 최적화 옵션으로 Bake 되었는지 체크
			targetOptPortrait._isSizeOptimizedV117 = isSizeOptimizedV117;



			//추가3.22
			//Portrait가 Prefab이라면
			//Bake와 동시에 Apply를 해야한다.
			//if(apEditorUtil.IsPrefab(taretOptPortrait.gameObject))
			//{
			//	apEditorUtil.ApplyPrefab(taretOptPortrait.gameObject);
			//}

			//추가 4.26
			//메카님 옵션이 켜져 있다면
			//1. Animation Clip들을 리소스로 생성한다.
			//2. Animator 컴포넌트를 추가한다.
			//TODO : > Optimized에서도
			if (targetOptPortrait._isUsingMecanim)
			{
				//추가 3.22 : animClip 경로가 절대 경로인 경우, 여러 작업자가 공유해서 쓸 수 없다.
				//상대 경로로 바꾸는 작업을 해야한다.
				CheckAnimationsBasePathForV116(targetOptPortrait);

				CreateAnimationsWithMecanim(targetOptPortrait, targetOptPortrait._mecanimAnimClipResourcePath);
				targetOptPortrait.SetFirstInitializeAfterBake();
				targetOptPortrait.Initialize();
			}


			//추가 21.9.25 : 유니티 이벤트 (UnityEvent)를 사용한다면 Bake를 하자
			if(targetOptPortrait._unityEventWrapper == null)
			{
				targetOptPortrait._unityEventWrapper = new apUnityEventWrapper();
			}
			targetOptPortrait._unityEventWrapper.Bake(targetOptPortrait);

			EditorUtility.SetDirty(targetOptPortrait);


			//추가. Bake 후 처리
			ProcessAfterBake();

			//추가 19.10.26 : 빌보드 설정을 다시 복구
			targetOptPortrait._billboardType = billboardType;

			//추가 21.3.11
			// Scale 이슈가 있어서 저장된 값의 Scale로 복원
			foreach (KeyValuePair<Transform, Vector3> transform2Scale in prevTransformScales)
			{
				if(transform2Scale.Key != null)
				{
					transform2Scale.Key.localScale = transform2Scale.Value;
				}
			}

			//버그 수정 : 첫번째 루트 유닛만 보여야 하는데 그렇지 않은 경우 문제 해결
			//추가 22.1.9 : 루트 유닛이 여러개 있는 경우엔 첫번째 루트 유닛을 출력하자
			int nOptRootUnits = targetOptPortrait._optRootUnitList != null ? targetOptPortrait._optRootUnitList.Count : 0;
			if (nOptRootUnits > 1)
			{
				targetOptPortrait.ShowRootUnitWhenBake(targetOptPortrait._optRootUnitList[0]);
			}



			//Bake 후에는 Initialize를 하지 않은 상태로 되돌린다. (v1.4.3)
			targetOptPortrait.SetFirstInitializeAfterBake();

			//v1.6.0 Undo 통합
			apEditorUtil.EndRecordUndo();

			return bakeResult;
		}



		

		//----------------------------------------------------------------------------
		// 하위 객체 생성 함수들 (주로 재귀 호출)
		//----------------------------------------------------------------------------
		private T AddGameObject<T>(string name, Transform parent) where T : MonoBehaviour
		{
			GameObject newGameObject = new GameObject(name);//<Undo
			//Undo 추가 (v1.6.0)
			apEditorUtil.SetRecordCreatedGameObject(newGameObject);

			//newGameObject.transform.parent = parent;
			//Undo API로 변경
			apEditorUtil.SetParentWithRecord(newGameObject.transform, parent);


			newGameObject.transform.localPosition = Vector3.zero;
			newGameObject.transform.localRotation = Quaternion.identity;
			newGameObject.transform.localScale = Vector3.one;

			//return newGameObject.AddComponent<T>();//이전
			return apEditorUtil.AddComponentWithRecord<T>(newGameObject);//Undo API로 변경
		}

		/// <summary>
		/// MeshGroup을 OptTransform으로 Bake한다.
		/// 이 함수 내에서 OptMesh도 이어서 Bake 한다.
		/// </summary>
		private void MakeMeshGroupToOptTransform(apRenderUnit renderUnit,
													apTransform_MeshGroup meshGroupTransform,
													Transform parent, apOptTransform parentTransform,
													apOptRootUnit targetOptRootUnit,
													apBakeLinkManager bakeLinkManager,
													apBakeResult bakeResult,
													float bakeZScale,
													bool isGammaColorSpace,
													//bool isLWRPShader,//삭제
													apPortrait targetOptPortrait,
													apMeshGroup rootMeshGroup,
													bool isSizeOptimizedV117,
													bool isUseSRP,
													HashSet<apTransform_Mesh> maskReceivedMeshTFs//v1.6.0
													)
		{
			string objectName = meshGroupTransform._nickName;
			int meshGroupUniqueID = -1;
			if (meshGroupTransform._meshGroup != null)
			{
				objectName = meshGroupTransform._meshGroup._name;
				meshGroupUniqueID = meshGroupTransform._meshGroup._uniqueID;
			}

			apMeshGroup meshGroup = meshGroupTransform._meshGroup;

			//if(meshGroupTransform._nickName.Length == 0)
			//{
			//	Debug.LogWarning("Empy Name : " + meshGroupTransform._meshGroup._name);
			//}

			apOptTransform optTransform = null;
			if (bakeLinkManager != null)
			{
				optTransform = bakeLinkManager.FindOptTransform(null, meshGroupTransform);
				if (optTransform != null)
				{
					//재활용에 성공했다.

					//Undo 등록
					apEditorUtil.SetRecordAnyObject(optTransform);

					optTransform.gameObject.name = objectName;
					
					//이전
					//optTransform.transform.parent = parent;

					//변경 v1.6.0 : Undo API 이용
					apEditorUtil.SetParentWithRecord(optTransform.transform, parent);

					optTransform.transform.localPosition = Vector3.zero;
					optTransform.transform.localRotation = Quaternion.identity;
					optTransform.transform.localScale = Vector3.one;

					//Count+1 : Recycled Opt
					bakeResult.AddCount_RecycledOptGameObject();
				}
			}

			if (optTransform == null)
			{
				//재활용에 실패했다면 생성
				optTransform = AddGameObject<apOptTransform>(objectName, parent);

				//Count+1 : New Opt
				bakeResult.AddCount_NewOptGameObject();
			}

			//OptTransform을 설정하자
			#region [미사용 코드] SetBasicSetting 함수로 대체
			//optTransform._transformID = meshGroupTransform._transformUniqueID;
			//optTransform._transform = optTransform.transform;

			//optTransform._depth = meshGroupTransform._depth;
			//optTransform._defaultMatrix = new apMatrix(meshGroupTransform._matrix);

			////optTransform._transform.localPosition = optTransform._defaultMatrix.Pos3 - new Vector3(0.0f, 0.0f, (float)optTransform._depth * 0.1f);
			//optTransform._transform.localPosition = optTransform._defaultMatrix.Pos3 - new Vector3(0.0f, 0.0f, (float)optTransform._depth);
			//optTransform._transform.localRotation = Quaternion.Euler(0.0f, 0.0f, optTransform._defaultMatrix._angleDeg);
			//optTransform._transform.localScale = optTransform._defaultMatrix._scale; 
			#endregion

			int renderUnitLevel = -1;
			if (renderUnit != null)
			{
				renderUnitLevel = renderUnit._level;
			}
			optTransform.Bake(targetOptPortrait,//meshGroup, 
								parentTransform,
								targetOptRootUnit,
								meshGroupTransform._nickName,
								meshGroupTransform._transformUniqueID,
								meshGroupUniqueID,
								meshGroupTransform._matrix,
								false,
								renderUnitLevel, meshGroupTransform._depth,
								meshGroupTransform._isVisible_Default,
								meshGroupTransform._meshColor2X_Default,
								bakeZScale,
								isSizeOptimizedV117,
								false,//리깅 옵션. MeshGroupTF는 리깅이 적용되지 않는다.
								false
								);


			//첫 초기화 Matrix(No-Mod)를 만들어주자 - Mesh Bake에서 사용된다.
			if (optTransform._matrix_TF_ToParent == null) { optTransform._matrix_TF_ToParent = new apMatrix(); }
			if (optTransform._matrix_TF_ParentWorld_NonModified == null) { optTransform._matrix_TF_ParentWorld_NonModified = new apMatrix(); }
			if (optTransform._matrix_TFResult_WorldWithoutMod == null) { optTransform._matrix_TFResult_WorldWithoutMod = new apMatrix(); }

			optTransform._matrix_TF_ToParent.SetMatrix(optTransform._defaultMatrix, true);
			optTransform._matrix_TF_ParentWorld_NonModified.SetIdentity();
			if (parentTransform != null)
			{
				optTransform._matrix_TF_ParentWorld_NonModified.SetMatrix(parentTransform._matrix_TFResult_WorldWithoutMod, true);
			}
			optTransform._matrix_TFResult_WorldWithoutMod.SetIdentity();

			//추가 20.8.6. [RMultiply Scale 이슈]
			optTransform._matrix_TFResult_WorldWithoutMod.OnBeforeRMultiply();


			optTransform._matrix_TFResult_WorldWithoutMod.RMultiply(optTransform._matrix_TF_ToParent, false);
			optTransform._matrix_TFResult_WorldWithoutMod.RMultiply(optTransform._matrix_TF_ParentWorld_NonModified, true);


			//RootUnit에 등록하자
			targetOptRootUnit.AddChildTransform(optTransform, rootMeshGroup.SortedBuffer.GetBufferData(renderUnit));


			//apBone을 추가해주자
			if (meshGroup._boneList_All.Count > 0)
			{
				MakeOptBone(meshGroup, optTransform, targetOptRootUnit, bakeLinkManager, bakeResult);
			}
			else
			{
				optTransform._boneList_All = null;
				optTransform._boneList_Root = null;
				optTransform._isBoneUpdatable = false;
			}


			//추가
			//소켓을 붙이자
			if (meshGroupTransform._isSocket)
			{
				apOptNode socketNode = null;
				if (bakeLinkManager != null)
				{
					socketNode = bakeLinkManager.FindOptTransformSocket(optTransform);
					if (socketNode != null)
					{
						apEditorUtil.SetRecordAnyObject(socketNode);

						socketNode.gameObject.name = meshGroupTransform._nickName + " Socket";
						
						//이전
						//socketNode.transform.parent = optTransform.transform;

						//Undo API로 변경
						apEditorUtil.SetParentWithRecord(socketNode.transform, optTransform.transform);

						socketNode.transform.localPosition = Vector3.zero;
						socketNode.transform.localRotation = Quaternion.identity;
						socketNode.transform.localScale = Vector3.one;

						//Count+1 : Recycled Opt
						bakeResult.AddCount_RecycledOptGameObject();
					}
				}

				if (socketNode == null)
				{
					socketNode = AddGameObject<apOptNode>(meshGroupTransform._nickName + " Socket", optTransform.transform);

					//Count+1 : New Opt
					bakeResult.AddCount_NewOptGameObject();
				}
				optTransform._socketTransform = socketNode.transform;
			}
			else
			{
				optTransform._socketTransform = null;
			}

			if (parentTransform != null)
			{
				parentTransform.AddChildTransforms(optTransform);
			}

			//만약 Root라면 ->
			if (parentTransform == null)
			{
				targetOptRootUnit._rootOptTransform = optTransform;
			}
			targetOptPortrait._optTransforms.Add(optTransform);


			if (renderUnit != null)
			{
				for (int i = 0; i < renderUnit._childRenderUnits.Count; i++)
				{
					apRenderUnit childRenderUnit = renderUnit._childRenderUnits[i];

					apTransform_MeshGroup childTransform_MeshGroup = childRenderUnit._meshGroupTransform;
					apTransform_Mesh childTransform_Mesh = childRenderUnit._meshTransform;

					if (childTransform_MeshGroup != null)
					{
						MakeMeshGroupToOptTransform(	childRenderUnit, childTransform_MeshGroup, optTransform.transform, optTransform, targetOptRootUnit, 
														bakeLinkManager, bakeResult, bakeZScale, 
														isGammaColorSpace,
														//isLWRPShader, //삭제
														targetOptPortrait, rootMeshGroup, 
														isSizeOptimizedV117, isUseSRP,
														maskReceivedMeshTFs);
					}
					else if (childTransform_Mesh != null)
					{
						MakeMeshToOptTransform(	childRenderUnit, childTransform_Mesh, meshGroup, optTransform.transform, optTransform, targetOptRootUnit, 
												bakeLinkManager, bakeResult, bakeZScale, 
												isGammaColorSpace,
												//isLWRPShader, //삭제
												targetOptPortrait, rootMeshGroup, 
												isSizeOptimizedV117, isUseSRP,
												maskReceivedMeshTFs);
					}
					else
					{
						Debug.LogError("Empty Render Unit");
					}
				}
			}
			else
			{
				Debug.LogError("No RenderUnit");
			}
		}

		/// <summary>
		/// OptMesh를 생성한다.
		/// </summary>
		private void MakeMeshToOptTransform(apRenderUnit renderUnit,
												apTransform_Mesh meshTransform,
												apMeshGroup parentMeshGroup,
												Transform parent,
												apOptTransform parentTransform,
												apOptRootUnit targetOptRootUnit,
												apBakeLinkManager bakeLinkManager,
												apBakeResult bakeResult,
												float bakeZScale,
												bool isGammaColorSpace,
												//bool isLWRPShader,//삭제
												apPortrait targetOptPortrait,
												apMeshGroup rootMeshGroup,
												bool isSizeOptimizedV117,
												bool isUseSRP,
												HashSet<apTransform_Mesh> maskReceivedMeshTFs//v1.6.0
												)
		{
			apOptTransform optTransform = null;
			if (bakeLinkManager != null)
			{
				optTransform = bakeLinkManager.FindOptTransform(meshTransform, null);
				if (optTransform != null)
				{
					//재활용에 성공했다.
					apEditorUtil.SetRecordAnyObject(optTransform);

					optTransform.gameObject.name = meshTransform._nickName;
					
					//이전
					//optTransform.transform.parent = parent;

					//Undo API로 변경
					apEditorUtil.SetParentWithRecord(optTransform.transform, parent);

					optTransform.transform.localPosition = Vector3.zero;
					optTransform.transform.localRotation = Quaternion.identity;
					optTransform.transform.localScale = Vector3.one;

					//Count+1 : Recycled Opt
					bakeResult.AddCount_RecycledOptGameObject();
				}

			}

			if (optTransform == null)
			{
				//재활용에 실패했다면 생성
				optTransform = AddGameObject<apOptTransform>(meshTransform._nickName, parent);

				//Count+1 : New Opt
				bakeResult.AddCount_NewOptGameObject();
			}


			//OptTransform을 설정하자

			optTransform.Bake(targetOptPortrait, //null, 
								parentTransform,
								targetOptRootUnit,
								meshTransform._nickName,
								meshTransform._transformUniqueID,
								-1,
								meshTransform._matrix,
								true,
								renderUnit._level, meshTransform._depth,
								meshTransform._isVisible_Default,
								meshTransform._meshColor2X_Default,
								bakeZScale,
								isSizeOptimizedV117,
								renderUnit._calculatedStack.IsRigging,//추가 20.8.10 : 리깅 여부를 Bake에 미리 넣는다.
								(targetOptPortrait._flippedMeshOption == apPortrait.FLIPPED_MESH_CHECK.All)//Flip 여부를 리깅본으로 부터 체크할지를 portrait 옵션에서 확인
								);


			//Debug.Log("Mesh OptTransform Bake [" + optTransform.name + "] Pivot : " + meshTransform._matrix._pos);
			//첫 초기화 Matrix(No-Mod)를 만들어주자 - Mesh Bake에서 사용된다.
			if (optTransform._matrix_TF_ToParent == null) { optTransform._matrix_TF_ToParent = new apMatrix(); }
			if (optTransform._matrix_TF_ParentWorld_NonModified == null) { optTransform._matrix_TF_ParentWorld_NonModified = new apMatrix(); }
			if (optTransform._matrix_TFResult_WorldWithoutMod == null) { optTransform._matrix_TFResult_WorldWithoutMod = new apMatrix(); }

			optTransform._matrix_TF_ToParent.SetMatrix(optTransform._defaultMatrix, true);
			optTransform._matrix_TF_ParentWorld_NonModified.SetIdentity();
			if (parentTransform != null)
			{
				optTransform._matrix_TF_ParentWorld_NonModified.SetMatrix(parentTransform._matrix_TFResult_WorldWithoutMod, true);
			}
			optTransform._matrix_TFResult_WorldWithoutMod.SetIdentity();

			//추가 20.8.6. [RMultiply Scale 이슈]
			optTransform._matrix_TFResult_WorldWithoutMod.OnBeforeRMultiply();

			optTransform._matrix_TFResult_WorldWithoutMod.RMultiply(optTransform._matrix_TF_ToParent, false);
			optTransform._matrix_TFResult_WorldWithoutMod.RMultiply(optTransform._matrix_TF_ParentWorld_NonModified, true);


			//추가
			//소켓을 붙이자
			if (meshTransform._isSocket)
			{
				apOptNode socketNode = null;
				if (bakeLinkManager != null)
				{

					socketNode = bakeLinkManager.FindOptTransformSocket(optTransform);
					if (socketNode != null)
					{
						//Undo 등록
						apEditorUtil.SetRecordAnyObject(socketNode);

						socketNode.gameObject.name = meshTransform._nickName + " Socket";
						
						//이전
						//socketNode.transform.parent = optTransform.transform;

						//Undo API로 변경
						apEditorUtil.SetParentWithRecord(socketNode.transform, optTransform.transform);

						socketNode.transform.localPosition = Vector3.zero;
						socketNode.transform.localRotation = Quaternion.identity;
						socketNode.transform.localScale = Vector3.one;

						//Count+1 : Recycled Opt
						bakeResult.AddCount_RecycledOptGameObject();
					}

				}

				if (socketNode == null)
				{
					socketNode = AddGameObject<apOptNode>(meshTransform._nickName + " Socket", optTransform.transform);

					//Count+1 : New Opt
					bakeResult.AddCount_NewOptGameObject();
				}
				optTransform._socketTransform = socketNode.transform;
			}
			else
			{
				optTransform._socketTransform = null;
			}

			if (parentTransform != null)
			{
				parentTransform.AddChildTransforms(optTransform);
			}

			targetOptPortrait._optTransforms.Add(optTransform);

			//RootUnit에 등록하자
			targetOptRootUnit.AddChildTransform(optTransform, rootMeshGroup.SortedBuffer.GetBufferData(renderUnit));


			//하위에 OptMesh를 만들자
			apMesh mesh = meshTransform._mesh;
			if (mesh != null)
			{
				apOptMesh optMesh = null;

				if (bakeLinkManager != null)
				{
					optMesh = bakeLinkManager.FindOptMesh(optTransform);
					if (optMesh != null)
					{
						//재활용에 성공했다.
						//Undo 등록
						apEditorUtil.SetRecordAnyObject(optMesh);

						optMesh.gameObject.name = meshTransform._nickName + "_Mesh";
						
						//optMesh.transform.parent = optTransform.transform;
						//Undo API로 변경
						apEditorUtil.SetParentWithRecord(optMesh.transform, optTransform.transform);

						optMesh.transform.localPosition = Vector3.zero;
						optMesh.transform.localRotation = Quaternion.identity;
						optMesh.transform.localScale = Vector3.one;

						//필수 컴포넌트가 비었는지도 확인
						if (optMesh.GetComponent<MeshFilter>() == null)
						{
							//optMesh.gameObject.AddComponent<MeshFilter>();//이전
							apEditorUtil.AddComponentWithRecord<MeshFilter>(optMesh.gameObject);//Undo
						}
						if (optMesh.GetComponent<MeshRenderer>() == null)
						{
							//optMesh.gameObject.AddComponent<MeshRenderer>();//이전
							apEditorUtil.AddComponentWithRecord<MeshRenderer>(optMesh.gameObject);//Undo
						}

						//Count+1 : Recycled Opt
						bakeResult.AddCount_RecycledOptGameObject();

					}
				}
				if (optMesh == null)
				{
					//재활용이 안되었으니 직접 만들자
					optMesh = AddGameObject<apOptMesh>(meshTransform._nickName + "_Mesh", optTransform.transform);
					// optMesh.gameObject.AddComponent<MeshFilter>();//이전
					// optMesh.gameObject.AddComponent<MeshRenderer>();//이전
					apEditorUtil.AddComponentWithRecord<MeshFilter>(optMesh.gameObject);//Undo API
					apEditorUtil.AddComponentWithRecord<MeshRenderer>(optMesh.gameObject);//Undo API

					//Count+1 : New Opt
					bakeResult.AddCount_NewOptGameObject();
				}


				List<apVertex> verts = mesh._vertexData;

				List<Vector3> posList = new List<Vector3>();
				List<Vector2> UVList = new List<Vector2>();
				List<int> IDList = new List<int>();
				List<int> triList = new List<int>();
				List<float> zDepthList = new List<float>();

				apVertex vert = null;
				for (int i = 0; i < verts.Count; i++)
				{
					vert = verts[i];
					posList.Add(vert._pos);
					UVList.Add(vert._uv);
					IDList.Add(vert._uniqueID);
					zDepthList.Add(vert._zDepth);
				}

				for (int i = 0; i < mesh._indexBuffer.Count; i++)
				{
					triList.Add(mesh._indexBuffer[i]);
				}

				Texture2D texture = null;
				apOptTextureData optTextureData = null;//<<연결될 OptTextureData

				//변경 코드 4.1
				if (mesh.LinkedTextureData != null)
				{
					texture = mesh.LinkedTextureData._image;
					optTextureData = targetOptPortrait._optTextureData.Find(delegate (apOptTextureData a)
					{
						return a._srcUniqueID == mesh.LinkedTextureData._uniqueID;
					});
				}

				//Mesh Bake를 하자
				optMesh._portrait = targetOptPortrait;
				optMesh._uniqueID = meshTransform._transformUniqueID;

				//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

				//이전 : MaterialSet / Material Library를 사용하지 않는 경우
				////Shader 설정
				//Shader shaderNormal = GetOptMeshShader(meshTransform._shaderType, false, isGammaColorSpace, isLWRPShader);
				//Shader shaderMask = GetOptMeshShader(meshTransform._shaderType, true, isGammaColorSpace, isLWRPShader);
				//Shader shaderAlphaMask = GetOptAlphaMaskShader(isLWRPShader);
				//if (meshTransform._isCustomShader && meshTransform._customShader != null)
				//{
				//	shaderNormal = meshTransform._customShader;
				//	shaderMask = meshTransform._customShader;
				//}


				////통합 재질을 찾자
				//int batchedMatID = -1;
				//if (texture != null && optTextureData != null && !meshTransform._isClipping_Child)
				//{
				//	apOptBatchedMaterial.MaterialUnit batchedMatUnit = targetOptPortrait._optBatchedMaterial.MakeBatchedMaterial_Prev(texture, optTextureData._textureID, shaderNormal);
				//	if (batchedMatUnit != null)
				//	{
				//		batchedMatID = batchedMatUnit._uniqueID;
				//	}
				//}

				//변경 19.6.15 : Material Set / Material Library를 사용하는 경우
				//Mat Info 만들기 전에 다시 Mat Set 다시 설정
				if(meshTransform._isUseDefaultMaterialSet)
				{
					meshTransform._linkedMaterialSet = targetOptPortrait.GetDefaultMaterialSet();
					if(meshTransform._linkedMaterialSet != null)
					{
						meshTransform._materialSetID = meshTransform._linkedMaterialSet._uniqueID;
					}
				}
				else
				{
					if(meshTransform._materialSetID >= 0)
					{
						meshTransform._linkedMaterialSet = targetOptPortrait.GetMaterialSet(meshTransform._materialSetID);
						if (meshTransform._linkedMaterialSet == null)
						{
							//연결될 MatSet이 없다면.. > 기본값
							meshTransform._linkedMaterialSet = targetOptPortrait.GetDefaultMaterialSet();
							if (meshTransform._linkedMaterialSet != null)
							{
								meshTransform._materialSetID = meshTransform._linkedMaterialSet._uniqueID;
							}
							else
							{
								meshTransform._materialSetID = -1;
							}
						}
					}
					else
					{
						meshTransform._linkedMaterialSet = null;
					}
				}


				apOptMaterialInfo matInfo = new apOptMaterialInfo();
				int textureDataID = -1;
				int linkedSrcTextureDataID = -1;
				if(meshTransform._mesh != null)
				{
					//기존 방식 (SrcUniqueID : 에디터용을 사용했다.)
					textureDataID = meshTransform._mesh.LinkedTextureDataID;
					linkedSrcTextureDataID = meshTransform._mesh.LinkedTextureDataID;

					apOptTextureData optTexData = targetOptPortrait._optTextureData.Find(delegate(apOptTextureData a)
					{
						return a._srcUniqueID == meshTransform._mesh.LinkedTextureDataID;
					});
					if(optTexData != null)
					{
						//Debug.Log("optTexData를 MatInfo로 저장 : " + optTexData._name + "(" + optTexData._srcUniqueID + ") : " + optTexData._textureID);
						textureDataID = optTexData._textureID;
					}
					else
					{
						//Debug.LogError("실패 : optTexData를 찾지 못했다. : " + meshTransform._mesh.LinkedTextureDataID);
						textureDataID = meshTransform._mesh.LinkedTextureDataID;
					}
					
				}

				//v1.6.0 추가 : 마스크 데이터를 받는지 여부
				bool isAnyMaskReceived = false;
				if(maskReceivedMeshTFs != null)
				{
					if(maskReceivedMeshTFs.Contains(meshTransform))
					{
						isAnyMaskReceived = true;
					}
				}


				matInfo.Bake(	meshTransform,
								targetOptPortrait,
								!isGammaColorSpace,
								textureDataID,
								linkedSrcTextureDataID,
								isAnyMaskReceived,
								Editor.MaterialLibrary);

				Shader shader_AlphaMask = null;
				if(meshTransform._linkedMaterialSet != null)
				{
					shader_AlphaMask = meshTransform._linkedMaterialSet._shader_AlphaMask;
				}
				else
				{
					shader_AlphaMask = targetOptPortrait.GetDefaultMaterialSet()._shader_AlphaMask;
				}

				//Debug.LogError("Bake Mesh : " + meshTransform._nickName);

				//Material Info를 이용하여 BatchedMatID를 찾자
				int batchedMatID = -1;
				if (texture != null && optTextureData != null && !meshTransform._isClipping_Child)
				{
					apOptBatchedMaterial.MaterialUnit batchedMatUnit = targetOptPortrait._optBatchedMaterial.MakeBatchedMaterial_MatInfo(matInfo);
					if (batchedMatUnit != null)
					{
						batchedMatID = batchedMatUnit._uniqueID;
					}
				}
				//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

				//Render Texture 크기
				int maskRenderTextureSize = 0;
				switch (meshTransform._renderTexSize)
				{
					case apTransform_Mesh.RENDER_TEXTURE_SIZE.s_64:		maskRenderTextureSize = 64;		break;
					case apTransform_Mesh.RENDER_TEXTURE_SIZE.s_128:	maskRenderTextureSize = 128;	break;
					case apTransform_Mesh.RENDER_TEXTURE_SIZE.s_256:	maskRenderTextureSize = 256;	break;
					case apTransform_Mesh.RENDER_TEXTURE_SIZE.s_512:	maskRenderTextureSize = 512;	break;
					case apTransform_Mesh.RENDER_TEXTURE_SIZE.s_1024:	maskRenderTextureSize = 1024;	break;
					
					case apTransform_Mesh.RENDER_TEXTURE_SIZE.FullScreen:	maskRenderTextureSize = -1; break;
					case apTransform_Mesh.RENDER_TEXTURE_SIZE.HalfScreen:	maskRenderTextureSize = -2; break;
					case apTransform_Mesh.RENDER_TEXTURE_SIZE.QuarterScreen:	maskRenderTextureSize = -3; break;
					case apTransform_Mesh.RENDER_TEXTURE_SIZE.MaxFHD:	maskRenderTextureSize = -4; break;
					case apTransform_Mesh.RENDER_TEXTURE_SIZE.MaxHD:	maskRenderTextureSize = -5; break;
					default:
						maskRenderTextureSize = 64;
						Debug.LogError("Unknown RenderTexture Size [" + meshTransform._renderTexSize + "]");
						break;
				}

				bool isVisibleDefault = true;

				if (!meshTransform._isVisible_Default)
				{
					isVisibleDefault = false;
				}
				else
				{
					//Parent로 올라가면서 VisibleDefault가 하나라도 false이면 false
					apRenderUnit curRenderUnit = renderUnit;
					while (true)
					{
						if (curRenderUnit == null) { break; }

						if (curRenderUnit._unitType == apRenderUnit.UNIT_TYPE.Mesh)
						{
							if (curRenderUnit._meshTransform != null)
							{
								if (!curRenderUnit._meshTransform._isVisible_Default)
								{
									isVisibleDefault = false;
									break;
								}
							}
							else
							{
								break;
							}
						}
						else if (curRenderUnit._unitType == apRenderUnit.UNIT_TYPE.GroupNode)
						{
							if (curRenderUnit._meshGroupTransform != null)
							{
								if (!curRenderUnit._meshGroupTransform._isVisible_Default)
								{
									isVisibleDefault = false;
									break;
								}
							}
							else
							{
								break;
							}
						}
						//위로 이동
						curRenderUnit = curRenderUnit._parentRenderUnit;
					}
				}

				//추가 : 그림자 설정
				apPortrait.SHADOW_CASTING_MODE shadowCastMode = targetOptPortrait._meshShadowCastingMode;
				bool receiveShadow = targetOptPortrait._meshReceiveShadow;
				if (!meshTransform._isUsePortraitShadowOption)
				{
					shadowCastMode = meshTransform._shadowCastingMode;
					receiveShadow = meshTransform._receiveShadow;
				}

				//추가 v1.5.0 : Light Probe 설정
				apPortrait.LIGHT_PROBE_USAGE lightProbeUsage = targetOptPortrait._meshLightProbeUsage;
				apPortrait.REFLECTION_PROBE_USAGE reflectionProbeUsage = targetOptPortrait._meshReflectionProbeUsage;

				//변경 21.5.27 : 여기로 이동
				//Parent Transform에 등록하자
				optTransform.SetChildMesh(optMesh);

				apOptMesh.MeshBakeRequest bakeRequest = new apOptMesh.MeshBakeRequest(optTransform);
				bakeRequest.SetOption1_Vertices(	posList.ToArray(),
													UVList.ToArray(),
													IDList.ToArray(),
													triList.ToArray(),
													zDepthList.ToArray(),
													mesh._offsetPos);

				bakeRequest.SetOption2_Material(	texture,
													//텍스쳐 ID가 들어가야 한다.
													(optTextureData != null ? optTextureData._textureID : -1),
													meshTransform._shaderType,

													//변경 : 19.6.15 : Material Info 이용
													matInfo,

													batchedMatID,

													shader_AlphaMask,
													maskRenderTextureSize,
													isVisibleDefault);

				bakeRequest.SetOption3_Mask(	meshTransform._isClipping_Parent,
												meshTransform._isClipping_Child,
												meshTransform._sendMaskDataList,//v1.6.0
												meshTransform._isMaskOnlyMesh
												);

				bakeRequest.SetOption4_Render(	meshTransform._isAlways2Side,
												shadowCastMode,
												receiveShadow,
												lightProbeUsage,
												reflectionProbeUsage,
												isUseSRP);

				optMesh.BakeMesh(bakeRequest);

				//역으로 OptTextureData에도 OptMesh를 등록
				if (optTextureData != null)
				{
					optTextureData.AddLinkOptMesh(optMesh);
				}

				//Clipping의 기본 정보를 넣고, 나중에 연결하자
				if (meshTransform._isClipping_Parent)
				{
					optMesh.SetMaskBasicSetting_Parent();
				}
				else if (meshTransform._isClipping_Child)
				{
					optMesh.SetMaskBasicSetting_Child(meshTransform._clipParentMeshTransform._transformUniqueID);
				}

				targetOptPortrait._optMeshes.Add(optMesh);
			}
		}


		/// <summary>
		/// Opt Bone 생성
		/// </summary>
		private void MakeOptBone(apMeshGroup srcMeshGroup,
									apOptTransform targetOptTransform,
									apOptRootUnit targetOptRootUnit,
									apBakeLinkManager bakeLinkManager,
									apBakeResult bakeResult)
		{
			//1. Bone Group을 만들고
			//2. Bone을 계층적으로 추가하자 (재귀 함수 필요)

			apOptNode boneGroupNode = null;
			if (bakeLinkManager != null)
			{
				boneGroupNode = bakeLinkManager.FindOptBoneGroupNode();
				if (boneGroupNode != null)
				{
					//재활용에 성공했다.
					//Undo 등록
					apEditorUtil.SetRecordAnyObject(boneGroupNode);

					boneGroupNode.gameObject.name = "__Bone Group";
					
					//boneGroupNode.transform.parent = targetOptTransform.transform;
					//Undo API로 변경
					apEditorUtil.SetParentWithRecord(boneGroupNode.transform, targetOptTransform.transform);

					boneGroupNode.transform.localPosition = Vector3.zero;
					boneGroupNode.transform.localRotation = Quaternion.identity;
					boneGroupNode.transform.localScale = Vector3.one;

					boneGroupNode._param = 100;

					//Count+1 : Recycled Opt
					bakeResult.AddCount_RecycledOptGameObject();

				}
			}
			if (boneGroupNode == null)
			{
				boneGroupNode = AddGameObject<apOptNode>("__Bone Group", targetOptTransform.transform);
				boneGroupNode._param = 100;//<<Bone Group의 Param은 100이다.

				//Count+1 : New Opt
				bakeResult.AddCount_NewOptGameObject();
			}


			targetOptTransform._boneGroup = boneGroupNode.transform;
			targetOptTransform._boneList_All = null;
			targetOptTransform._boneList_Root = null;
			targetOptTransform._isBoneUpdatable = true;

			List<apBone> rootBones = srcMeshGroup._boneList_Root;
			List<apOptBone> totalOptBones = new List<apOptBone>();
			for (int i = 0; i < rootBones.Count; i++)
			{
				apOptBone newRootBone = MakeOptBoneRecursive(	srcMeshGroup, rootBones[i], null, 
																targetOptTransform, targetOptRootUnit, totalOptBones, 
																bakeLinkManager, bakeResult);
				targetOptTransform._boneList_Root = apEditorUtil.AddItemToArray<apOptBone>(newRootBone, targetOptTransform._boneList_Root);
			}

			targetOptTransform._boneList_All = totalOptBones.ToArray();



			int nBones = totalOptBones.Count;
			//이제 전체 Bone을 돌면서 링크를 해주자
			for (int i = 0; i < totalOptBones.Count; i++)
			{
				totalOptBones[i].LinkOnBake(targetOptTransform);
			}
			//Root에서부터 LinkChaining을 실행하자
			for (int i = 0; i < targetOptTransform._boneList_Root.Length; i++)
			{
				targetOptTransform._boneList_Root[i].LinkBoneChaining();
			}
		}

		/// <summary>
		/// Opt Bone을 생성하는 재귀 함수
		/// </summary>
		private apOptBone MakeOptBoneRecursive(apMeshGroup srcMeshGroup,
												apBone srcBone,
												apOptBone parentOptBone,
												apOptTransform targetOptTransform,
												apOptRootUnit targetOptRootUnit,
												List<apOptBone> resultOptBones,
												apBakeLinkManager bakeLinkManager,
												apBakeResult bakeResult)
		{
			Transform parentTransform = targetOptTransform._boneGroup;
			if (parentOptBone != null)
			{
				parentTransform = parentOptBone.transform;
			}
			apOptBone newBone = null;

			if (bakeLinkManager != null)
			{
				newBone = bakeLinkManager.FindOptBone(srcBone);
				if (newBone != null)
				{
					//재활용에 성공했다.
					//Undo 등록
					apEditorUtil.SetRecordAnyObject(newBone);

					newBone.gameObject.name = srcBone._name;
					
					//newBone.transform.parent = parentTransform;
					//Undo API로 변경
					apEditorUtil.SetParentWithRecord(newBone.transform, parentTransform);

					newBone.transform.localPosition = Vector3.zero;
					newBone.transform.localRotation = Quaternion.identity;
					newBone.transform.localScale = Vector3.one;

					//Count+1 : Recycled Opt
					bakeResult.AddCount_RecycledOptGameObject();
				}

			}
			if (newBone == null)
			{
				newBone = AddGameObject<apOptBone>(srcBone._name, parentTransform);

				//Count+1 : New Opt
				bakeResult.AddCount_NewOptGameObject();
			}

			srcBone.GUIUpdate(false);

			//Link를 제외한 Bake를 먼저 하자.
			//Link는 ID를 이용하여 일괄적으로 처리
			newBone.Bake(srcBone);

			
			//RootUnit에 등록하자
			targetOptRootUnit.AddChildBone(newBone);


			if (srcBone._isSocketEnabled)
			{
				//소켓을 붙여주자
				apOptNode socketNode = null;
				if (bakeLinkManager != null)
				{
					socketNode = bakeLinkManager.FindOptBoneSocket(newBone);
					if (socketNode != null)
					{
						//Undo 등록
						apEditorUtil.SetRecordAnyObject(socketNode);

						socketNode.gameObject.name = srcBone._name + " Socket";
						
						//socketNode.transform.parent = newBone.transform;
						//Undo API로 변경
						apEditorUtil.SetParentWithRecord(socketNode.transform, newBone.transform);

						socketNode.transform.localPosition = Vector3.zero;
						socketNode.transform.localRotation = Quaternion.identity;
						socketNode.transform.localScale = Vector3.one;

						//Count+1 : Recycled Opt
						bakeResult.AddCount_RecycledOptGameObject();
					}

				}

				if (socketNode == null)
				{
					socketNode = AddGameObject<apOptNode>(srcBone._name + " Socket", newBone.transform);

					//Count+1 : New Opt
					bakeResult.AddCount_NewOptGameObject();
				}
				newBone._socketTransform = socketNode.transform;
			}

			if (parentOptBone != null)
			{
				newBone._parentBone = parentOptBone;
				parentOptBone._childBones = apEditorUtil.AddItemToArray<apOptBone>(newBone, parentOptBone._childBones);
			}
			else
			{
				//[v1.4.2 버그] TODO : 만약에 Root Bone이 없다면 여기서 해제하는 것이 중요하다.
				//null로 만드는 코드가 없다면 편집하여 부모 본을 해제했을 때 반영이 안되서 에러가 발생한다.
				//if(newBone._parentBone != null)
				//{
				//	Debug.Log("버그 확인");
				//}
				newBone._parentBone = null;//<<중요
			}

			resultOptBones.Add(newBone);
			//하위 Child Bone에 대해서도 반복

			for (int i = 0; i < srcBone._childBones.Count; i++)
			{
				MakeOptBoneRecursive(srcMeshGroup,
										srcBone._childBones[i],
										newBone,
										targetOptTransform,
										targetOptRootUnit,
										resultOptBones,
										bakeLinkManager,
										bakeResult);
			}


			return newBone;
		}


		
		//추가 20.8.11
		//리깅된 메시 Transform은 미리 모든 버텍스의 리깅 정보를 참고하여
		//리깅된 본들을 리스트>배열로 가진다.
		private void SetRiggingOptBonesToOptTransform(apOptTransform targetOptTransform)
		{
			//Debug.Log("Set RiggingOptBones [" + targetOptTransform.gameObject.name + "]");
			targetOptTransform._riggingBones = null;
			if(targetOptTransform.CalculatedStack == null)
			{
				//Debug.LogError("No CalculateStack");
				return;
			}

			//TODO : 이 코드가 종종 null을 리턴한다..

			List<apOptBone> riggingBones = targetOptTransform.CalculatedStack.GetRiggingBonesForBake();
			if(riggingBones == null || riggingBones.Count == 0)
			{
				//Debug.LogError("No RiggingBones");
				return;
			}

			//리깅된 본을 저장하자
			targetOptTransform.BakeRiggingBones(riggingBones);
		}



		//----------------------------------------------------------------------------
		// 보조 함수들
		//----------------------------------------------------------------------------

		// 생성된 오브젝트 재활용 관련 함수들 

		private apOptRootUnit GetRecycledRootUnit(apRootUnit srcRootUnit, List<apOptRootUnit> prevObjects)
		{
			//Debug.Log("RootUnit 재활용 찾기");
			if (srcRootUnit._childMeshGroup != null && srcRootUnit._childMeshGroup._rootRenderUnit != null && srcRootUnit._childMeshGroup._rootRenderUnit._meshGroupTransform != null)
			{
				apTransform_MeshGroup rootMGTransform = srcRootUnit._childMeshGroup._rootRenderUnit._meshGroupTransform;

				apOptRootUnit prevOptRootUnit = null;
				for (int i = 0; i < prevObjects.Count; i++)
				{
					prevOptRootUnit = prevObjects[i];


					if (prevOptRootUnit._rootOptTransform != null)
					{

						//동일한 OptTransform을 가진다면 복사 가능함
						if (IsOptTransformRecyclable(prevOptRootUnit._rootOptTransform, null, rootMGTransform))
						{
							return prevOptRootUnit;
						}
					}
				}
			}

			return null;
		}

		private bool IsOptTransformRecyclable(apOptTransform prevOptTransform, apTransform_Mesh meshTransform, apTransform_MeshGroup meshGroupTransform)
		{
			if (meshTransform != null)
			{
				if (prevOptTransform._unitType == apOptTransform.UNIT_TYPE.Mesh)
				{
					return prevOptTransform._transformID == meshTransform._transformUniqueID;
				}
			}
			else if (meshGroupTransform != null)
			{
				if (prevOptTransform._unitType == apOptTransform.UNIT_TYPE.Group)
				{
					return prevOptTransform._transformID == meshGroupTransform._transformUniqueID;
				}
			}

			return false;
		}

		
		/// <summary>
		/// 만약 사용하지 않는 Monobehaviour 객체가 있는 경우 삭제를 해야한다.
		/// </summary>
		/// <param name="portrait"></param>
		public void CheckAndRemoveUnusedMonobehaviours(apPortrait portrait)
		{
			if (portrait == null)
			{
				return;
			}
			//Monobehaiour는 Mesh, MeshGroup, Modifier이다.
			if (portrait._subObjectGroup_Mesh == null ||
				portrait._subObjectGroup_MeshGroup == null ||
				portrait._subObjectGroup_Modifier == null)
			{
				return;
			}
			//실제로 존재하는 데이터를 정리한다.
			List<GameObject> meshObjects = new List<GameObject>();
			List<GameObject> meshGroupObjects = new List<GameObject>();
			List<GameObject> modifierObjects = new List<GameObject>();

			apMesh mesh = null;
			apMeshGroup meshGroup = null;
			apModifierBase modifier = null;

			for (int i = 0; i < portrait._meshes.Count; i++)
			{
				mesh = portrait._meshes[i];
				if (mesh == null) { continue; }

				meshObjects.Add(mesh.gameObject);
			}

			for (int i = 0; i < portrait._meshGroups.Count; i++)
			{
				meshGroup = portrait._meshGroups[i];
				if (meshGroup == null) { continue; }

				meshGroupObjects.Add(meshGroup.gameObject);

				for (int iMod = 0; iMod < meshGroup._modifierStack._modifiers.Count; iMod++)
				{
					modifier = meshGroup._modifierStack._modifiers[iMod];
					if (modifier == null) { continue; }

					modifierObjects.Add(modifier.gameObject);
				}
			}

			//이제 Child GameObject를 확인하자
			int nChild_Mesh = portrait._subObjectGroup_Mesh.transform.childCount;
			int nChild_MeshGroup = portrait._subObjectGroup_MeshGroup.transform.childCount;
			int nChild_Modifier = portrait._subObjectGroup_Modifier.transform.childCount;
			List<GameObject> unusedGameObjects = new List<GameObject>();

			GameObject curGameObject = null;

			//1. Mesh
			for (int i = 0; i < nChild_Mesh; i++)
			{
				curGameObject = portrait._subObjectGroup_Mesh.transform.GetChild(i).gameObject;
				if (!meshObjects.Contains(curGameObject))
				{
					//안쓰는게 나왔다.
					unusedGameObjects.Add(curGameObject);
				}
			}

			//2. MeshGroup
			for (int i = 0; i < nChild_MeshGroup; i++)
			{
				curGameObject = portrait._subObjectGroup_MeshGroup.transform.GetChild(i).gameObject;
				if (!meshGroupObjects.Contains(curGameObject))
				{
					//안쓰는게 나왔다.
					unusedGameObjects.Add(curGameObject);
				}
			}

			//3. Modifier
			for (int i = 0; i < nChild_Modifier; i++)
			{
				curGameObject = portrait._subObjectGroup_Modifier.transform.GetChild(i).gameObject;
				if (!modifierObjects.Contains(curGameObject))
				{
					//안쓰는게 나왔다.
					unusedGameObjects.Add(curGameObject);
				}
			}

			if (unusedGameObjects.Count > 0)
			{
				//Debug.LogError("삭제되어야 하는 게임 오브젝트가 나왔다.");
				for (int i = 0; i < unusedGameObjects.Count; i++)
				{
					//Debug.LogError("[" + i + "] " + unusedGameObjects[i].name);
					//Undo.DestroyObjectImmediate(unusedGameObjects[i]);
					apEditorUtil.SetRecordDestroyGameObject(unusedGameObjects[i]);
				}
			}
		}


		/// <summary>
		/// GameObject들의 이름을 갱신하자
		/// Mesh, MeshGroup이 그 대상
		/// </summary>
		/// <param name="portrait"></param>
		public void CheckAndRefreshGameObjectNames(apPortrait portrait)
		{
			//숨어있는 GameObject들의 이름을 갱신한다.
			if (portrait == null)
			{
				return;
			}
			if (portrait._subObjectGroup_Mesh == null ||
				portrait._subObjectGroup_MeshGroup == null ||
				portrait._subObjectGroup_Modifier == null)
			{
				return;
			}
			apMesh mesh = null;
			apMeshGroup meshGroup = null;

			for (int i = 0; i < portrait._meshes.Count; i++)
			{
				mesh = portrait._meshes[i];
				if (mesh == null) { continue; }

				mesh.gameObject.name = mesh._name;
			}

			for (int i = 0; i < portrait._meshGroups.Count; i++)
			{
				meshGroup = portrait._meshGroups[i];
				if (meshGroup == null) { continue; }

				meshGroup.gameObject.name = meshGroup._name;
			}
		}


		//애니메이션 데이터 저장 관련 함수들
		
		//버전 1.1.6에서 애니메이션 경로가 "절대 경로"에서 "상대 경로"로 바뀌었다.
		//절대 경로인지 확인하여 상대 경로로 전환한다.
		private void CheckAnimationsBasePathForV116(apPortrait targetPortrait)
		{
			string basePath = apUtil.ConvertEscapeToPlainText(targetPortrait._mecanimAnimClipResourcePath);//변경 21.7.3 (Escape (%20)과 같은 문자)

			bool isUndoRecorded = false;
			if(!string.Equals(basePath, targetPortrait._mecanimAnimClipResourcePath))
			{
				//Debug.Log("Escape 문자 발견 [" + targetPortrait._mecanimAnimClipResourcePath + "]");

				isUndoRecorded = true;
				apEditorUtil.SetRecord_Portrait(	apUndoGroupData.ACTION.Portrait_BakeOptionChanged, 
																			_editor, 
																			targetPortrait, 
																			//targetPortrait, 
																			false,
																			apEditorUtil.UNDO_STRUCT.ValueOnly);

				targetPortrait._mecanimAnimClipResourcePath = basePath;//Escape 문자 삭제
			}

			if (!string.IsNullOrEmpty(basePath))
			{
				//경로를 체크하자
				apEditorUtil.PATH_INFO_TYPE pathInfo = apEditorUtil.GetPathInfo(basePath);
				switch (pathInfo)
				{
					case apEditorUtil.PATH_INFO_TYPE.Absolute_InAssetFolder:
						{
							//Asset 안의 절대 경로 >> 메시지 없이 바로 상대 경로로 바꾼다.
							if (!isUndoRecorded)//위에서 Undo Record가 없었다면
							{
								apEditorUtil.SetRecord_Portrait(apUndoGroupData.ACTION.Portrait_BakeOptionChanged,
																	_editor,
																	targetPortrait,
																	//targetPortrait, 
																	false,
																	apEditorUtil.UNDO_STRUCT.ValueOnly);
							}

							targetPortrait._mecanimAnimClipResourcePath = apEditorUtil.AbsolutePath2RelativePath(basePath);
						}
						break;

					case apEditorUtil.PATH_INFO_TYPE.Absolute_OutAssetFolder:
					case apEditorUtil.PATH_INFO_TYPE.NotValid:
					case apEditorUtil.PATH_INFO_TYPE.Relative_OutAssetFolder:
						{
							//잘못된 경로이므로 다시 지정하라고 안내
							//1. 일단 안내 메시지를 띄운다 > 
							//2. Okay인 경우 > Save Panel 을 띄운다.
							//3. Save Panel에서 유효한 Path를 리턴 받은 경우 검사
							//4. 유효한 경로라면 저장, 아니라면 다시 경고 메시지 (이때는 저장 불가)
							bool isReset = EditorUtility.DisplayDialog(_editor.GetText(TEXT.DLG_AnimClipSavePathValidationError_Title),
								_editor.GetText(TEXT.DLG_AnimClipSavePathValidationError_Body),
								_editor.GetText(TEXT.Okay),
								_editor.GetText(TEXT.Cancel));

							if (isReset)
							{
								string nextPath = EditorUtility.SaveFolderPanel("Select to export animation clips", "", "");

								if (!string.IsNullOrEmpty(nextPath))
								{
									//이스케이프 삭제
									nextPath = apUtil.ConvertEscapeToPlainText(nextPath);

									if (apEditorUtil.IsInAssetsFolder(nextPath))
									{
										//유효한 폴더인 경우
										//중요 : 경로가 절대 경로로 찍힌다.
										//상대 경로로 바꾸자
										apEditorUtil.PATH_INFO_TYPE pathInfoType = apEditorUtil.GetPathInfo(nextPath);
										if (pathInfoType == apEditorUtil.PATH_INFO_TYPE.Absolute_InAssetFolder)
										{
											//절대 경로 + Asset 폴더 안쪽이라면
											nextPath = apEditorUtil.AbsolutePath2RelativePath(nextPath);

										}

										if (!isUndoRecorded)//위에서 Undo Record가 없었다면
										{
											apEditorUtil.SetRecord_Portrait(apUndoGroupData.ACTION.Portrait_BakeOptionChanged,
																				_editor,
																				targetPortrait,
																				//targetPortrait, 
																				false,
																				apEditorUtil.UNDO_STRUCT.ValueOnly);
										}

										targetPortrait._mecanimAnimClipResourcePath = nextPath;
									}
									else
									{
										//유효한 폴더가 아닌 경우
										EditorUtility.DisplayDialog(
													_editor.GetText(TEXT.DLG_AnimClipSavePathValidationError_Title),
													_editor.GetText(TEXT.DLG_AnimClipSavePathResetError_Body),
													_editor.GetText(TEXT.Close));
									}
								}
							}
						}
						break;

					case apEditorUtil.PATH_INFO_TYPE.Relative_InAssetFolder:
						//Asset 안의 상대 경로 >> 그대로 둔다. >> 근데 %20이 포함되어 있다면?
						if(basePath.Contains("%20"))
						{
							string nextPath = apEditorUtil.DecodeURLEmptyWord(basePath);

							if (!isUndoRecorded)//위에서 Undo Record가 없었다면
							{
								apEditorUtil.SetRecord_Portrait(apUndoGroupData.ACTION.Portrait_BakeOptionChanged,
																	_editor,
																	targetPortrait,
																	//targetPortrait, 
																	false,
																	apEditorUtil.UNDO_STRUCT.ValueOnly);
							}

							targetPortrait._mecanimAnimClipResourcePath = nextPath;
						}

						break;
				}
			}
		}


		private bool CreateAnimationsWithMecanim(apPortrait targetPortrait, string basePath)
		{
			if (targetPortrait == null)
			{
				return false;
			}
			if (!targetPortrait._isUsingMecanim)
			{
				return false;
			}

			if(string.IsNullOrEmpty(basePath))
			{
				Debug.LogError("AnyPortrait : The path where animation clip assets are saved is not specified.");
				return false;
			}
			//Debug.Log("Base Path : " + basePath);

			//1. 경로 체크
			if (!basePath.EndsWith("/"))
			{
				basePath += "/";
			}

			basePath = basePath.Replace("\\", "/");

			//추가 21.7.3 : 경로 문제 수정
			basePath = apUtil.ConvertEscapeToPlainText(basePath);


			System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(basePath);

			//변경 3.24 : basePath가 절대 경로에서 상대 경로(Assets로 시작되는..)로 바뀌었다.
			//보통은 그래도 경로가 인식이 되는데, 경로 인식이 안된다면 Asset 폴더의 절대 경로를 한번 더 붙여주자
			if (!di.Exists)
			{
				if (basePath.StartsWith("Assets"))
				{
					//상대 경로로서 충분하다면
					string projectRootPath = Application.dataPath;
					//뒤의 Assets을 빼자 (6글자 빼자)
					projectRootPath = projectRootPath.Substring(0, projectRootPath.Length - 6);

					//루트 + / 로 되어 있을 것
					string absPath = projectRootPath + basePath;

					di = new System.IO.DirectoryInfo(absPath);
				}
			}

			if (!di.Exists)
			{
				Debug.LogError("AnyPortrait : Wrong Animation Clip Destination Folder [" + basePath + "]");
				return false;
			}

			string fullPath = di.FullName;

			//AssetDataBase는 Assets 부터 시작해야한다.
			string projectPath = Application.dataPath + "/";

			//Debug.Log("DataPath : " + projectPath);
			//Debug.Log("BasePath : " + basePath);

			System.Uri uri_dataPath = new Uri(projectPath);
			//System.Uri uri_basePath = new Uri(basePath);
			System.Uri uri_basePath = new Uri(fullPath);

			if (!apEditorUtil.IsInAssetsFolder(fullPath))
			{
				Debug.LogError("AnyPortrait : Wrong Animation Clip Destination Folder [" + fullPath + "]");
				return false;
			}

			//string relativePath = "Assets/" + uri_dataPath.MakeRelativeUri(uri_basePath).ToString();
			string relativePath = apUtil.ConvertEscapeToPlainText(uri_dataPath.MakeRelativeUri(uri_basePath).ToString());//변경 21.7.11 : 이스케이프 문자 삭제

			if (!relativePath.StartsWith("Assets/"))
			{
				relativePath = "Assets/" + relativePath;
			}
			if (!relativePath.EndsWith("/"))
			{
				relativePath += "/";
			}
			//Debug.Log("AnimClip Result Path : " + relativePath);

			//2. Animator 체크
			if (targetPortrait._animator == null)
			{
				targetPortrait._animator = targetPortrait.gameObject.AddComponent<Animator>();

				//추가 v1.4.8
				//Root Motion 옵션을 켜서 오히려 위치가 (0, 0, 0)으로 강제되는 것을 막자
				//원래는 false여야 위치가 강제되지 않는데, 여기서는 FBX가 아닌 애니메이션 키값에 원점 위치가 있어서 그걸 무시하기 위해서 true를 입력
				targetPortrait._animator.applyRootMotion = true;
			}

			//3. AnimatorController 있는지 체크 > 없다면 만든다. 다만 있을 경우엔 더이상 수정하지 않는다.
			UnityEditor.Animations.AnimatorController newAnimController = null;
			UnityEditor.Animations.AnimatorController runtimeAnimController = null;
			if (targetPortrait._animator.runtimeAnimatorController == null)
			{
				//AnimatorController는 파일 덮어쓰기는 아예 안되고, 새로 만드는 것만 가능
				//새로 만들자

				string animControllerPath = relativePath + targetPortrait.name + "-AnimController";
				string animControllerExp = "controller";
				string animControllerFullPath = GetNewUniqueAssetName(animControllerPath, animControllerExp, typeof(UnityEditor.Animations.AnimatorController));

				newAnimController = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(animControllerFullPath);
				targetPortrait._animator.runtimeAnimatorController = newAnimController;
			}
			else
			{
				runtimeAnimController = targetPortrait._animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
			}

			//4. 애니메이션 클립 체크
			//이미 AnimationClip이 있다면 덮어씌운다.
			//없다면 새로 생성한다. 이때 에셋 이름은 "충돌되지 않게" 만든다.

			for (int iAnim = 0; iAnim < targetPortrait._animClips.Count; iAnim++)
			{
				AnimationClip createdAnimClipAsset = CreateAnimationClipAsset(targetPortrait, targetPortrait._animClips[iAnim], relativePath);
				if (createdAnimClipAsset != null)
				{
					//데이터를 저장하자
					//targetPortrait._animClipAssetPairs.Add(new apAnimMecanimData_AssetPair(targetPortrait._animClips[iAnim]._uniqueID, createdAnimClipAsset));
					targetPortrait._animClips[iAnim]._animationClipForMecanim = createdAnimClipAsset;

					if (newAnimController != null)
					{
						//자동으로 생성된 AnimController가 있는 경우
						if (newAnimController.layers.Length > 0)
						{
							//animController.layers[0].stateMachine.AddStateMachineBehaviour()
							UnityEditor.Animations.AnimatorState newMotionState = newAnimController.AddMotion(createdAnimClipAsset, 0);
							newMotionState.motion = createdAnimClipAsset;
							newMotionState.name = targetPortrait._animClips[iAnim]._name;

						}
					}
				}

				EditorUtility.SetDirty(createdAnimClipAsset);
			}

			//추가 : "비어있는 애니메이션 클립"을 만든다.
			AnimationClip emptyAnimClipAsset = CreateEmptyAnimationClipAsset(targetPortrait, relativePath);
			targetPortrait._emptyAnimClipForMecanim = emptyAnimClipAsset;

			if (newAnimController != null)
			{
				//자동으로 생성된 AnimController가 있는 경우
				if (newAnimController.layers.Length > 0)
				{
					//animController.layers[0].stateMachine.AddStateMachineBehaviour()
					UnityEditor.Animations.AnimatorState newMotionState = newAnimController.AddMotion(emptyAnimClipAsset, 0);
					newMotionState.motion = emptyAnimClipAsset;
					newMotionState.name = "Empty";
				}
			}

			EditorUtility.SetDirty(emptyAnimClipAsset);



			//4. 1차적으로 레이어 Refresh
			//이름으로 비교하여 없으면 추가, 있으면 넣기 방식으로 갱신한다.
			List<apAnimMecanimData_Layer> mecanimLayers = new List<apAnimMecanimData_Layer>();

			if (newAnimController != null || runtimeAnimController != null)
			{
				UnityEditor.Animations.AnimatorController curAnimController = null;
				if (newAnimController != null)
				{
					curAnimController = newAnimController;
				}
				else
				{
					curAnimController = runtimeAnimController;
				}

				if (curAnimController.layers != null && curAnimController.layers.Length > 0)
				{
					for (int iLayer = 0; iLayer < curAnimController.layers.Length; iLayer++)
					{
						apAnimMecanimData_Layer newLayerData = new apAnimMecanimData_Layer();
						newLayerData._layerIndex = iLayer;
						newLayerData._layerName = curAnimController.layers[iLayer].name;
						newLayerData._blendType = apAnimMecanimData_Layer.MecanimLayerBlendType.Unknown;
						switch (curAnimController.layers[iLayer].blendingMode)
						{
							case UnityEditor.Animations.AnimatorLayerBlendingMode.Override:
								newLayerData._blendType = apAnimMecanimData_Layer.MecanimLayerBlendType.Override;
								break;

							case UnityEditor.Animations.AnimatorLayerBlendingMode.Additive:
								newLayerData._blendType = apAnimMecanimData_Layer.MecanimLayerBlendType.Additive;
								break;
						}
						mecanimLayers.Add(newLayerData);
					}
				}

				targetPortrait._animatorLayerBakedData.Clear();
				for (int i = 0; i < mecanimLayers.Count; i++)
				{
					targetPortrait._animatorLayerBakedData.Add(new apAnimMecanimData_Layer(mecanimLayers[i]));
				}
			}



			apEditorUtil.SetDirty(_editor);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			return true;
		}

		//Animation Clip 만들기
		private AnimationClip CreateAnimationClipAsset(apPortrait targetPortrait, apAnimClip targetAnimClip, string basePath)
		{
			//이미 AnimationClip이 있다면 덮어씌운다.
			//없다면 새로 생성한다. 이때 에셋 이름은 "충돌되지 않게" 만든다.

			float timeLength = targetAnimClip.TimeLength;

			AnimationClip resultAnimClip = null;



			string animClipAssetPath = "";
			bool isCreate = false;
			if (targetAnimClip._animationClipForMecanim != null)
			{
				//1. 이미 저장된 AnimationClip이 있는 경우
				//> 저장된 에셋 Path와 이름을 공유한다. 
				//> 해당 에셋을 덮어씌운다.
				//수정 : 덮어씌우지 말고 이걸 그냥 수정할 순 없을까
				resultAnimClip = targetAnimClip._animationClipForMecanim;
				animClipAssetPath = AssetDatabase.GetAssetPath(targetAnimClip._animationClipForMecanim);
				isCreate = false;
			}
			else
			{
				isCreate = true;
			}

			if (string.IsNullOrEmpty(animClipAssetPath))
			{
				//2. 새로 만들어야 하는 경우 or Asset 경로를 찾지 못했을 경우
				//> "겹치지 않는 이름"으로 생성한다.
				resultAnimClip = new AnimationClip();
				resultAnimClip.name = targetPortrait.name + "-" + targetAnimClip._name;
				animClipAssetPath = GetNewUniqueAssetName(basePath + resultAnimClip.name, "anim", typeof(AnimationClip));
				isCreate = true;
			}

			resultAnimClip.legacy = false;
			if (targetAnimClip.IsLoop)
			{
				resultAnimClip.wrapMode = WrapMode.Loop;
			}
			else
			{
				resultAnimClip.wrapMode = WrapMode.Once;
			}
			AnimationUtility.SetEditorCurve(resultAnimClip, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalPosition.x"), AnimationCurve.Linear(0.0f, 0.0f, timeLength, 0.0f));
			AnimationUtility.SetEditorCurve(resultAnimClip, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalPosition.y"), AnimationCurve.Linear(0.0f, 0.0f, timeLength, 0.0f));
			AnimationUtility.SetEditorCurve(resultAnimClip, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalPosition.z"), AnimationCurve.Linear(0.0f, 0.0f, timeLength, 0.0f));

			if (isCreate)
			{
				AssetDatabase.CreateAsset(resultAnimClip, animClipAssetPath);
			}


			return AssetDatabase.LoadAssetAtPath<AnimationClip>(animClipAssetPath);

		}



		private string GetNewUniqueAssetName(string assetPathWOExtension, string extension, System.Type type)
		{
			if (AssetDatabase.LoadAssetAtPath(assetPathWOExtension + "." + extension, type) != null)
			{
				//에셋이 이미 존재한다.
				//이름을 바꾼다.
				int newNameIndex = 1;
				string newName = "";
				while (true)
				{
					newName = assetPathWOExtension + " (" + newNameIndex + ")." + extension;

					if (AssetDatabase.LoadAssetAtPath(newName, type) == null)
					{
						//겹치는게 없다.
						return newName;//새로운 이름을 찾았다.
					}

					newNameIndex++;
				}
			}
			else
			{
				//에셋이 없다. 그대로 사용하자
				return assetPathWOExtension + "." + extension;
			}
		}



		private AnimationClip CreateEmptyAnimationClipAsset(apPortrait targetPortrait, string basePath)
		{
			//이미 AnimationClip이 있다면 덮어씌운다.
			//없다면 새로 생성한다. 이때 에셋 이름은 "충돌되지 않게" 만든다.

			float timeLength = 1.0f;

			if (targetPortrait._emptyAnimClipForMecanim != null)
			{
				return targetPortrait._emptyAnimClipForMecanim;
			}
			AnimationClip resultAnimClip = new AnimationClip();
			resultAnimClip.name = targetPortrait.name + "-Empty";
			string animClipAssetPath = GetNewUniqueAssetName(basePath + resultAnimClip.name, "anim", typeof(AnimationClip));

			resultAnimClip.legacy = false;
			resultAnimClip.wrapMode = WrapMode.Loop;

			AnimationUtility.SetEditorCurve(resultAnimClip, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalPosition.x"), AnimationCurve.Linear(0.0f, 0.0f, timeLength, 0.0f));
			AnimationUtility.SetEditorCurve(resultAnimClip, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalPosition.y"), AnimationCurve.Linear(0.0f, 0.0f, timeLength, 0.0f));
			AnimationUtility.SetEditorCurve(resultAnimClip, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalPosition.z"), AnimationCurve.Linear(0.0f, 0.0f, timeLength, 0.0f));

			AssetDatabase.CreateAsset(resultAnimClip, animClipAssetPath);
			return AssetDatabase.LoadAssetAtPath<AnimationClip>(animClipAssetPath);

		}



		// 색상 공간 관련 함수들
		//추가 20.1.28 : Bake시 Color Space에 맞추어서 모든 TextureData를 확인하여 일괄 변환할지 물어보고 처리하자
		private void CheckAndChangeTextureDataColorSpace(apPortrait targetPortrait)
		{
			if(targetPortrait == null)
			{
				return;
			}


			//True이면 Gamma, False면 Linear
			//bool isGammaSpace = Editor._isBakeColorSpaceToGamma;//이전
			bool isGammaSpace = Editor.ProjectSettingData.Project_IsColorSpaceGamma;//변경 22.12.19 [v1.4.2]


			int nTextureData = targetPortrait._textureData == null ? 0 : targetPortrait._textureData.Count;
			
			bool isAllSameColorSpace = true;//모두 같은 ColorSpace인가
			apTextureData curTexData = null;
			TextureImporter textureImporter = null;
			
			for (int iTex = 0; iTex < nTextureData; iTex++)
			{
				curTexData = targetPortrait._textureData[iTex];
				if(curTexData._image == null)
				{
					continue;
				}
				string path = AssetDatabase.GetAssetPath(curTexData._image);
				textureImporter = (TextureImporter)TextureImporter.GetAtPath(path);

				if(textureImporter != null)
				{
					bool isGammaTexture = textureImporter.sRGBTexture;
					textureImporter = null;

					if(isGammaSpace != isGammaTexture)
					{
						//하나라도 다르다면
						isAllSameColorSpace = false;
						break;
					}
				}
			}

			if(isAllSameColorSpace)
			{
				//모두 Color Space가 같네염
				return;
			}

			//물어봅시다.
			//apStringFactory.I.Gamma
			//"Color Space Correction"
			//"The value of the Color Space of some Images differs from the current setting.\nDo you want to change the Color Space of all the Images to [] to be the same as the current setting?"
			bool result = EditorUtility.DisplayDialog(
				Editor.GetText(TEXT.DLG_CorrectionImageColorSpace_Title), 
				Editor.GetTextFormat(TEXT.DLG_CorrectionImageColorSpace_Body, isGammaSpace ? apStringFactory.I.Gamma : apStringFactory.I.Linear),
				Editor.GetText(TEXT.Okay), 
				Editor.GetText(TEXT.Cancel));

			if(!result)
			{
				return;
			}

			//모든 텍스쳐의 Color Space
			for (int iTex = 0; iTex < nTextureData; iTex++)
			{
				curTexData = targetPortrait._textureData[iTex];
				if (curTexData._image == null)
				{
					continue;
				}
				string path = AssetDatabase.GetAssetPath(curTexData._image);
				textureImporter = (TextureImporter)TextureImporter.GetAtPath(path);
				if(textureImporter != null)
				{
					bool isGammaTexture = textureImporter.sRGBTexture;

					if(isGammaSpace != isGammaTexture)
					{
						textureImporter.sRGBTexture = isGammaSpace;
						textureImporter.SaveAndReimport();
					}

					textureImporter = null;
				}
			}

			AssetDatabase.Refresh();
		}


		// Image가 없는 메시 Bake 방지

		
		/// <summary>
		/// 추가 20.11.7
		/// 이미지가 지정되지 않은 메시가 있다면 Bake를 할 수 없다.
		/// </summary>
		/// <returns>문제가 되는 메시가 없어서 Bake를 할 수 있는 상황이면 True, 없으면 False</returns>
		private bool CheckIfAnyNoImageMesh(apPortrait targetPortrait)
		{
			int nMeshes = targetPortrait._meshes.Count;
			apMesh curMesh = null;
			
			List<apMesh> wrongMeshes = new List<apMesh>();
			bool isAnyNoImageMesh = false;

			for (int i = 0; i < nMeshes; i++)
			{
				curMesh = targetPortrait._meshes[i];

				//이미지가 없는 메시가 있다.
				if(curMesh._textureData_Linked == null)
				{
					wrongMeshes.Add(curMesh);
					isAnyNoImageMesh = true;
				}
				else if(curMesh._textureData_Linked._image == null)
				{
					//Image는 연결되었지만 텍스쳐 에셋이 없다..
					wrongMeshes.Add(curMesh);
					isAnyNoImageMesh = true;
				}
			}
			
			if(isAnyNoImageMesh)
			{
				//문제가 있는 메시가 있다.
				//메시지를 보여주자
				apStringWrapper strMeshes = new apStringWrapper(128);
				if(wrongMeshes.Count == 1)
				{
					strMeshes.Append(wrongMeshes[0]._name, false);
				}
				else
				{
					for (int i = 0; i < wrongMeshes.Count; i++)
					{
						if(i > 3)
						{
							//개수가 너무 많다.
							strMeshes.Append(apStringFactory.I.Dot3, false);
							strMeshes.Append(apStringFactory.I.Return, false);
							break;
						}

						strMeshes.Append(wrongMeshes[i]._name, false);
						if(i < wrongMeshes.Count - 1)
						{
							strMeshes.Append(apStringFactory.I.Return, false);
						}
					}
				}
				strMeshes.MakeString();

				bool result = EditorUtility.DisplayDialog(	Editor.GetText(TEXT.DLG_NoImageMesh_Title),
															Editor.GetTextFormat(TEXT.DLG_NoImageMesh_Body, strMeshes.ToString()),
															Editor.GetText(TEXT.Okay),
															Editor.GetText(TEXT.Ignore));

				if(result)
				{
					//에러가 발생했고 Bake는 중지
					return false;
				}
				else
				{
					//에러가 발생했지만 무시
					return true;
				}				
			}
			
			//에러가 없다.
			return true;
		}



		// MaskData를 수신하는 Mesh Transform 수집
		
		/// <summary>
		/// v1.6.0 : MaskMesh를 받는 Mesh Transform을 수집한다. 재귀적으로 동작한다.
		/// </summary>
		private void CollectMaskReceivedMeshes(apTransform_MeshGroup curMeshGroupTF, apTransform_MeshGroup rootMeshGroupTF, HashSet<apTransform_Mesh> resultList)
		{
			if(curMeshGroupTF == null || rootMeshGroupTF == null)
			{
				return;
			}

			if(curMeshGroupTF._meshGroup == null)
			{
				return;
			}

			apMeshGroup curMeshGroup = curMeshGroupTF._meshGroup;


			int nMeshTFs = curMeshGroup._childMeshTransforms != null ? curMeshGroup._childMeshTransforms.Count : 0;
			if(nMeshTFs > 0)
			{
				apTransform_Mesh meshTF = null;
				apSendMaskData sendData = null;
				apSendMaskData.TargetInfo targetInfo = null;
				for (int iMeshTF = 0; iMeshTF < nMeshTFs; iMeshTF++)
				{
					meshTF = curMeshGroup._childMeshTransforms[iMeshTF];
					if(meshTF == null)
					{
						continue;
					}

					int nSendMaskData = meshTF._sendMaskDataList != null ? meshTF._sendMaskDataList.Count : 0;
					if(nSendMaskData == 0)
					{
						continue;
					}

					for (int iData = 0; iData < nSendMaskData; iData++)
					{
						sendData = meshTF._sendMaskDataList[iData];
						if(sendData == null)
						{
							continue;
						}

						int nTargets = sendData._targetInfos != null ? sendData._targetInfos.Count : 0;
						if (nTargets == 0)
						{
							continue;
						}

						for (int iTarget = 0; iTarget < nTargets; iTarget++)
						{
							targetInfo = sendData._targetInfos[iTarget];
							if (targetInfo == null)
							{
								continue;
							}

							apTransform_Mesh targetMeshTF = targetInfo._linkedMeshTF;
							if(targetMeshTF == null)
							{
								//Link가 안되었다면 ID로 직접 가져오자
								if(rootMeshGroupTF._meshGroup != null)
								{
									targetMeshTF = rootMeshGroupTF._meshGroup.GetMeshTransformRecursive(targetInfo._meshTFID);
								}
							}

							if(targetMeshTF == null)
							{
								continue;
							}

							//데이터를 저장
							if(resultList == null)
							{
								resultList = new HashSet<apTransform_Mesh>();
							}
							if(!resultList.Contains(targetMeshTF))
							{
								resultList.Add(targetMeshTF);
							}
						}
					}
				}
			}

			//자식 MeshGroupTransform도 찾아서 재귀적으로 호출한다.
			int nChildMeshGroups = curMeshGroup._childMeshGroupTransforms != null ? curMeshGroup._childMeshGroupTransforms.Count : 0;
			if (nChildMeshGroups > 0)
			{
				apTransform_MeshGroup childMeshGroupTF = null;
				for (int iChild = 0; iChild < nChildMeshGroups; iChild++)
				{
					childMeshGroupTF = curMeshGroup._childMeshGroupTransforms[iChild];
					if (childMeshGroupTF == null
						|| childMeshGroupTF == curMeshGroupTF
						|| childMeshGroupTF == rootMeshGroupTF)
					{
						continue;
					}
					CollectMaskReceivedMeshes(childMeshGroupTF, rootMeshGroupTF, resultList);
				}
			}
		}


		// Bake 후 호출되는 함수

		/// <summary>
		/// Bake / OptimizedBake 이후에 호출해야하는 함수.
		/// 현재 편집되는 것에 따라서 Link를 다시 해야한다.
		/// </summary>
		private void ProcessAfterBake()
		{
			apPortrait portrait = Editor.Select.Portrait;
			if (portrait == null)
			{
				return;
			}
			apMeshGroup meshGroup = null;
			switch (Editor.Select.SelectionType)
			{
				case apSelection.SELECTION_TYPE.Overall:
					if (Editor.Select.RootUnit != null)
					{
						meshGroup = Editor.Select.RootUnit._childMeshGroup;
					}
					break;


				case apSelection.SELECTION_TYPE.MeshGroup:
					meshGroup = Editor.Select.MeshGroup;
					break;

				case apSelection.SELECTION_TYPE.Animation:
					if (Editor.Select.AnimClip != null)
					{
						meshGroup = Editor.Select.AnimClip._targetMeshGroup;
					}

					break;
			}
			if (meshGroup != null)
			{
				//현재 작업 중인 MeshGroup을 찾아서 Link를 다시 한다.
				portrait.LinkAndRefreshInEditor(false, apUtil.LinkRefresh.Set_MeshGroup_AllModifiers(meshGroup));
			}

		}



		// Bake용 Undo 함수
		//---------------------------------------------------------------
		
		
	}
}