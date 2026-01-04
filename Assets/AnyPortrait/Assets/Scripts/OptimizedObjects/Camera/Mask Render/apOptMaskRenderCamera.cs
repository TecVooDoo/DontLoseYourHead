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

using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using AnyPortrait;

namespace AnyPortrait
{
    /// <summary>
    /// 기존의 apOptMeshRenderCamera의 역할을 하는 카메라 클래스를 apPortrait에 넣었다.
    /// 기존에는 Clipping Mask용으로만 동작하는 것을 더 포괄적으로 작동하기 위해 변경.
    /// 특히 "공유 Mask"를 위해서 더 유연하게 수정했다.
    /// 카메라별 데이터를 가지고 있다.
    /// </summary>
    public class apOptMaskRenderCamera
    {
        // Members
        //------------------------------------
        private apPortrait _portrait;
		private bool _isSRP = false;

		//카메라들
		//렌더링 이벤트 등록 등의 역할을 한다.
		private List<apOptMaskRenderCameraUnit> _camUnits = null;
		private Dictionary<Camera, apOptMaskRenderCameraUnit> _cam2Unit = null;
		private int _nCamUnits = 0;
		
		
		//상태 변경 모니터링
		public enum CAMERA_TYPE
		{
			None, Single_NoVR, Single_VR, Multiple
		}
		private CAMERA_TYPE _cameraType_Cur = CAMERA_TYPE.None;
		private CAMERA_TYPE _cameraType_Prev = CAMERA_TYPE.None;
		private int _refreshKey = -1;//메인 카메라에서 변동이 생기면 Key값이 바뀐다. 이걸로 변경 체크 가능

		private bool _isAllSameForward = true;
		private bool _isAllCameraOrthographic = true;
		private apPortrait.VR_SUPPORT_MODE _vrSupportMode = apPortrait.VR_SUPPORT_MODE.None;

		//카메라 상태 / RT 생성 상태 / 이벤트 등록 상태를 모두 저장한다.
		// [ 카메라 상태 ] : 현재 씬의 카메라와 한번이라도 동기화를 했는지 여부. 초기화 이후엔 항상 참조된 상태다
		// [ RT 생성 상태 ] : 카메라 동기화 이후엔 RT 동기화 요청을 해야한다. 캐릭터 전체가 Hide된 상태에서는 RT를 해제한다.
		// [ 이벤트 등록 상태 ] : 카메라 동기화 이후에 렌더 이벤트 등록을 해야한다. 캐릭터 전체가 Hide된 상태에서는 이벤트 등록을 해제한다.

		public enum CAMERA_SYNC { NoCam, Refreshed }
		public enum RT_SYNC { NoRT, Created }
		public enum RENDER_EVENT_SYNC { NoEvent, Added }

		private CAMERA_SYNC _cameraSync = CAMERA_SYNC.NoCam;
		private RT_SYNC _RTSync = RT_SYNC.NoRT;
		private RENDER_EVENT_SYNC _renderEventSync = RENDER_EVENT_SYNC.NoEvent;

		//렌더러(+리시버) 데이터는 RootUnit을 키값으로 동작한다.
		private Dictionary<apOptRootUnit, apOptMaskProcess> _rootUnit2Process = null;
		private bool _isAnyMaskProcess = false;


		//업데이트 호출/연산 방식
		//- 호출 방식과 연산 방식은 카메라 상태와 Render Pipeline에 따라 각각 결정된다.
		/// <summary>
		/// 업데이트 연산 함수의 호출 방식
		/// </summary>
		public enum FUNC_CALL_TYPE
		{
			None,
			/// <summary>portrait 스크립트에서 업데이트 함수 호출 (커맨드 버퍼 등록)</summary>
			CalculateCall,
			/// <summary>MultiCamController를 통한 PreRender 이벤트로부터 업데이트 함수 호출 (커맨드 버퍼 등록)</summary>
			MultiCamPreRenderEvent,
			/// <summary>SRP의 BeginCameraRendering 이벤트로부터 업데이트 함수 호출 (커맨드 버퍼 등록 안함)</summary>
			SRPEvent,
		}
		private FUNC_CALL_TYPE _funcCallType = FUNC_CALL_TYPE.None;

		/// <summary>
		/// 카메라에 따라 업데이트 방식이 구분된다.
		/// </summary>
		public enum FUNC_CAMERA_TYPE
		{
			None,
			/// <summary>단일 카메라 + 일반 방식</summary>
			Basic,
			/// <summary>단일 VR 카메라</summary>
			SingleVR,
			/// <summary>다중 카메라</summary>
			MultiCamera
		}
		private FUNC_CAMERA_TYPE _funcCameraType = FUNC_CAMERA_TYPE.None;

		//연산을 위한 접근
		private apOptRootUnit _lastCheckRootUnit = null;
		private apOptMaskProcess _lastRunProcess = null;


        
        // Init
        //------------------------------------
        public apOptMaskRenderCamera(apPortrait portrait)
        {
            _portrait = portrait;
			switch(_portrait._renderPipelineOption)
			{
				//Bake를 안한 경우
				case apPortrait.RENDER_PIPELINE_OPTION.Unknown:
					{
						//그래픽스 설정으로 직접 판단
						_isSRP = false;

#if UNITY_6000_0_OR_NEWER						
						if (UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline != null)
						{
							_isSRP = true;
						}
#elif UNITY_2020_1_OR_NEWER
						if (UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset != null)
						{
							_isSRP = true;
						}
#endif
					}
					break;

				case apPortrait.RENDER_PIPELINE_OPTION.BuiltIn:
					_isSRP = false;
					break;

				case apPortrait.RENDER_PIPELINE_OPTION.SRP:
					_isSRP = true;
					break;
			}


			//렌더러/리시버 정보를 모두 초기화한다.
			if (_rootUnit2Process == null)
			{
				_rootUnit2Process = new Dictionary<apOptRootUnit, apOptMaskProcess>();
			}
			_rootUnit2Process.Clear();
			_isAnyMaskProcess = false;

			
			//카메라도 초기화
			if(_camUnits == null) { _camUnits = new List<apOptMaskRenderCameraUnit>(); }
			_camUnits.Clear();
			
			if(_cam2Unit == null) { _cam2Unit = new Dictionary<Camera, apOptMaskRenderCameraUnit>(); }
			_cam2Unit.Clear();
			
			_nCamUnits = 0;			

			_cameraSync = CAMERA_SYNC.NoCam;
			_RTSync = RT_SYNC.NoRT;
			_renderEventSync = RENDER_EVENT_SYNC.NoEvent;

			_funcCallType = FUNC_CALL_TYPE.None;
			_funcCameraType = FUNC_CAMERA_TYPE.None;

			_lastCheckRootUnit = null;
			_lastRunProcess = null;
			
			Clear();
			
        }

		/// <summary>
		/// 카메라와 관련된 모든 데이터를 삭제한다.
		/// (카메라 등록 정보, 커맨드버퍼, 마스크 RT)
		/// 이 함수 이후에는 다시 처음부터 생성해야한다.
		/// </summary>
		public void Clear()
		{
			//카메라 대상 : 카메라 삭제, 이벤트 등록 해제
			ClearCamerasAndEvents();

			//렌더러 대상 : 생성된 RT 삭제
			ClearMaskRTs();

			//카메라 동기화 정보 해제
			_cameraType_Cur = CAMERA_TYPE.None;
			_cameraType_Prev = _cameraType_Cur;
			_refreshKey = -1;

			_isAllSameForward = true;
			_isAllCameraOrthographic = true;
			_vrSupportMode = apPortrait.VR_SUPPORT_MODE.None;
		}


		/// <summary>
		/// 카메라를 초기화한다.
		/// 이 함수는 렌더링 이벤트도 같이 초기화한다.
		/// </summary>
		private void ClearCamerasAndEvents()
		{
			//이벤트를 먼저 삭제
			ClearEvents();

			//리스트 초기화
			if(_camUnits == null) { _camUnits = new List<apOptMaskRenderCameraUnit>(); }
			_camUnits.Clear();

			if(_cam2Unit == null) { _cam2Unit = new Dictionary<Camera, apOptMaskRenderCameraUnit>(); }
			_cam2Unit.Clear();
			_nCamUnits = 0;

			//상태 갱신
			_cameraSync = CAMERA_SYNC.NoCam;
		}

		/// <summary>
		/// [Hide 시 호출]
		/// 렌더링 이벤트만 삭제한다. 카메라 자체는 그대로 둔다.
		/// </summary>
		private void ClearEvents()
		{
			//카메라에 등록된 이벤트 삭제
			if(_nCamUnits > 0)
			{
				apOptMaskRenderCameraUnit curCam = null;
				for (int i = 0; i < _nCamUnits; i++)
				{
					curCam = _camUnits[i];

					//커맨드 버퍼 등록 해제 (있다면)
					curCam.RemoveAllCommandBuffers(_isSRP);

					//다중 카메라 이벤트 삭제 (있다면)
					curCam.RemoveMultiCamRenderEvent();
				}				
			}

			//렌더링 이벤트도 삭제
#if UNITY_2019_1_OR_NEWER
			if(_isSRP)
			{
				//SRP에서의 이벤트도 삭제
				RenderPipelineManager.beginCameraRendering -= RenderEvent_SRP;				
			}
#endif
			_renderEventSync = RENDER_EVENT_SYNC.NoEvent;
		}


		/// <summary>
		/// [Hide 시 호출]
		/// Mask RT들을 일단 삭제한다.
		/// </summary>
		private void ClearMaskRTs()
		{
			foreach (KeyValuePair<apOptRootUnit, apOptMaskProcess> rendererPair in _rootUnit2Process)
			{
				rendererPair.Value.RemoveMaskRTs();
			}
			_RTSync = RT_SYNC.NoRT;
		}


		

		//---------------------------------------------------------------------------------
		// 렌더러 / 리시버 추가 (초기화시 호출)
		//---------------------------------------------------------------------------------
		/// <summary>
		/// Clipping Parent Mask Mesh를 Renderer로서 이 클래스에 등록한다. (중복 체크 없음)
		/// 나중에 Child가 참조하기 좋도록 생성된 렌더러를 리턴한다. 꼭 저장하자
		/// </summary>
		public apOptMaskRenderer AddRenderer_ClippingParent(apOptMesh parentMaskMesh)
		{
			if(parentMaskMesh == null)
			{
				return null;
			}

			apOptRootUnit rootUnit = parentMaskMesh._parentTransform._rootUnit;

			//Root Unit에 따라서 렌더러 추가
			apOptMaskProcess targetProcess = null;
			_rootUnit2Process.TryGetValue(rootUnit, out targetProcess);
			if(targetProcess == null)
			{
				targetProcess = new apOptMaskProcess(rootUnit);
				_rootUnit2Process.Add(rootUnit, targetProcess);
			}

			//렌더러 리스트에 이 Clipping Parent 메시에 대한 Renderer를 생성하여 추가한다.
			apOptMaskRenderer maskRenderer = new apOptMaskRenderer(_portrait, rootUnit);
			maskRenderer.SetClipping(parentMaskMesh);

			targetProcess.AddRenderer_ClippingParent(maskRenderer);
			_isAnyMaskProcess = true;
			

			return maskRenderer;
		}



		/// <summary>
		/// 마스크를 생성하는 SendData를 Renderer로서 이 클래스에 등록한다. (중복 체크 없음)
		/// Shared 여부에 따라 자동으로 구분하여 별도로 등록한다.
		/// 결과값을 리턴하니 SendData에 저장해두자 (Child에서 참조용)
		/// </summary>
		/// <param name="sendMaskMesh"></param>
		/// <param name="sendData"></param>
		public apOptMaskRenderer AddRenderer_SendData(apOptMesh sendMaskMesh, apOptSendMaskData sendData)
		{
			if(sendMaskMesh == null || sendData == null)
			{
				return null;
			}

			apOptRootUnit rootUnit = sendMaskMesh._parentTransform._rootUnit;

			//Root Unit에 따라서 렌더러 추가
			apOptMaskProcess targetProcess = null;
			_rootUnit2Process.TryGetValue(rootUnit, out targetProcess);
			if(targetProcess == null)
			{
				targetProcess = new apOptMaskProcess(rootUnit);
				_rootUnit2Process.Add(rootUnit, targetProcess);
			}

			_isAnyMaskProcess = true;

			if(sendData._isRTShared)
			{
				//공유 RT 타입이다.
				//이미 생성된 Renderer가 있다면, 거기에 추가하자
				int sharedID = sendData._sharedRTID;
				//apSendMaskData.RT_SHADER_TYPE RTShaderType = sendData._rtShaderType;
				//Shader customShaderAsset = sendData._customRTShaderAsset;

				//Shared ID + Shader Type + 커스텀 Shader 에셋을 키값으로 하여 동일한 렌더러에 추가하여 같이 렌더링을 하자
				apOptMaskRenderer sharedRenderer = targetProcess.GetSharedRenderer(	sharedID);

				if(sharedRenderer == null)
				{
					//이 조건으로 등록된 렌더러가 없다. 새로 생성
					sharedRenderer = new apOptMaskRenderer(_portrait, rootUnit);
					sharedRenderer.SetSendData_Shared(sharedID);

					//초기값을 입력하자
					sharedRenderer.AddSendData_Shared(sendMaskMesh, sendData, true);//true : 초기 데이터

					//프로세스에 넣자
					targetProcess.AddRenderer_SendDataShared(sharedRenderer);
				}
				else
				{
					//이 렌더러에 이 마스크 메시 정보를 추가한다.
					sharedRenderer.AddSendData_Shared(sendMaskMesh, sendData, false);//false : 추가 데이터
				}	
				return sharedRenderer;
			}


			//일반 메시별 RT 타입
			//렌더러를 생성한다.
			apOptMaskRenderer newRenderer = new apOptMaskRenderer(_portrait, rootUnit);
			newRenderer.SetSendData_PerMesh(sendMaskMesh, sendData);
			targetProcess.AddRenderer_SendDataPerMesh(newRenderer);

			return newRenderer;
		}


		/// <summary>
		/// Mask Child (Clipped나 Send Mask Data 수신)라면 콜백을 받기 위해 Receiver를 등록하자
		/// </summary>
		/// <param name="receiveMesh"></param>
		/// <param name="receiveLinkInfo"></param>
		/// <returns></returns>
		public apOptMaskReceiver AddReceiver(apOptMesh receiveMesh, apOptMaskLinkInfo receiveLinkInfo)
		{
			if(receiveMesh == null || receiveLinkInfo == null)
			{
				return null;
			}

			apOptRootUnit rootUnit = receiveMesh._parentTransform._rootUnit;

			//Root Unit에 따라서 렌더러 그룹이 다름
			apOptMaskProcess targetProcess = null;
			_rootUnit2Process.TryGetValue(rootUnit, out targetProcess);
			if(targetProcess == null)
			{
				targetProcess = new apOptMaskProcess(rootUnit);
				_rootUnit2Process.Add(rootUnit, targetProcess);
			}

			//여기서 타입에 따라 Renderer를 찾자
			apOptMaskRenderer linkedRenderer = null;
			if(receiveLinkInfo.LinkType == apOptMaskLinkInfo.LINK_TYPE.Clipping)
			{
				//Clipping 타입이라면 Parent의 Renderer에서 찾을 수 있다.
				if(receiveLinkInfo.MaskMesh != null)
				{
					linkedRenderer = receiveLinkInfo.MaskMesh.LinkedClippingParentRenderer;
				}
			}
			else
			{
				//Send Data 타입이라면
				if(receiveLinkInfo.ReceivedSendData != null)
				{
					linkedRenderer = receiveLinkInfo.ReceivedSendData._linkedMaskRenderer;
				}
			}

			apOptMaskReceiver newReceiver = new apOptMaskReceiver(	receiveMesh,
																	receiveLinkInfo,
																	linkedRenderer);

			targetProcess.AddReceiver(newReceiver);

			return newReceiver;
		}


		//추가 후에는 정렬을 하자 (커맨드 버퍼의 렌더 순서 정렬)
		public void SortRenderers()
		{
			if(_rootUnit2Process == null)
			{
				return;
			}

			foreach (KeyValuePair<apOptRootUnit, apOptMaskProcess> processPair in _rootUnit2Process)
			{
				processPair.Value.OnInitCompleted();
			}
		}



		// Reset Render
		//---------------------------------------------------------
		/// <summary>
		/// 마스크 프로세스를 활성화한다.
		/// 완벽히 초기화하는 건 아니고, 상태를 검토하여 꼭 필요한 상태일 때만 카메라, RT, 이벤트 등을 다시 생성하고 등록한다.
		/// 초기화때도 호출되고, 임의로도 호출된다.
		/// 활성화된 상태에서 호출하면 상태를 봐서 갱신하는 효과도 있다.
		/// OptMesh의 Initialize_MaskParent / Initialize_MaskChiild의 역할을 한다.
		/// </summary>
		public void EnableRender()
		{
			if(!_isAnyMaskProcess)
			{
				//프로세스가 없다면 Enable되지 않는다.
				return;
			}

			// 만약 등록된게 없으면 
			//1. 카메라 업데이트를 한다. > 카메라는 체크하지 말자. 이 함수를 호출하는 경로가 이미 카메라 변경에 의한 것
			bool isAnyCameraChanged = IsAnyCameraChanged();

			if(	_cameraSync == CAMERA_SYNC.Refreshed
				&& _renderEventSync == RENDER_EVENT_SYNC.Added
				&& _RTSync == RT_SYNC.Created
				&& !isAnyCameraChanged
				)
			{
				//카메라 변동사항이 없고
				//이미 나머지가 동기화가 다 끝난 상태라면
				//더 처리하지 않아도 된다.
				return;
			}

			//일단 기존의 커맨드 버퍼와 이벤트는 해제한다. (중복 방지)
			ClearEvents();

			//카메라 갱신을 하자
			bool isCameraChanged = SyncCameras(true);//여기서는 갱신이 강제다.
			

			_funcCallType = FUNC_CALL_TYPE.None;
			_funcCameraType = FUNC_CAMERA_TYPE.None;

			//2. 모든 프로세스에 대해서 카메라 동기화를 하고, Mask RT를 생성한다.
			apOptMaskProcess curProcess = null;
			foreach (KeyValuePair<apOptRootUnit, apOptMaskProcess> processPair in _rootUnit2Process)
			{
				curProcess = processPair.Value;
				if(isCameraChanged)
				{
					//프로세스의 카메라도 변경하자
					curProcess.SyncCamera(_camUnits);
				}

				//RT가 생성되어 있지않다면 생성해두자
				curProcess.MakeMaskRTs();
			}

			_RTSync = RT_SYNC.Created;//RT 생성


			if(_cameraType_Cur == CAMERA_TYPE.None)
			{
				//카메라 타입이 없어서 이벤트를 등록할 수 없다.
				return;
			}

			//카메라와 SRP에 따라서
			//"호출 방식", "업데이트 방식 및 대리자"를 결정하자
			//3. 이벤트와 커맨드 버퍼를 등록하자
			switch(_cameraType_Cur)
			{
				case CAMERA_TYPE.Single_NoVR:
					{
						//일반적인 단일 카메라
						_funcCameraType = FUNC_CAMERA_TYPE.Basic;

#if UNITY_2019_1_OR_NEWER
						if(_isSRP)
						{
							//SRP는 무조건 SRP 이벤트에서 체크
							_funcCallType = FUNC_CALL_TYPE.SRPEvent;
						}
						else
						{
							//단일의 경우는 별도의 업데이트에서 실행
							_funcCallType = FUNC_CALL_TYPE.CalculateCall;
						}
						
#else
						_funcCallType = FUNC_CALL_TYPE.CalculateCall;
#endif
					}
					break;

				case CAMERA_TYPE.Multiple:
					{
						//다중 카메라
						_funcCameraType = FUNC_CAMERA_TYPE.MultiCamera;
#if UNITY_2019_1_OR_NEWER
						if(_isSRP)
						{
							//SRP는 무조건 SRP 이벤트에서 체크
							_funcCallType = FUNC_CALL_TYPE.SRPEvent;
						}
						else
						{
							//다중 카메라는 MultiCamController로부터 이벤트를 받자
							_funcCallType = FUNC_CALL_TYPE.MultiCamPreRenderEvent;
						}
						
#else
						_funcCallType = FUNC_CALL_TYPE.MultiCamPreRenderEvent;
#endif
					}
					break;

				case CAMERA_TYPE.Single_VR:
					{
						//단일 VR 타입이라면
						_funcCameraType = FUNC_CAMERA_TYPE.SingleVR;
#if UNITY_2019_1_OR_NEWER
						if(_isSRP)
						{
							//SRP는 무조건 SRP 이벤트에서 체크
							_funcCallType = FUNC_CALL_TYPE.SRPEvent;
						}
						else
						{
							//VR 카메라도 PreRender 이벤트를 별도로 받자
							_funcCallType = FUNC_CALL_TYPE.MultiCamPreRenderEvent;
						}
						
#else
						_funcCallType = FUNC_CALL_TYPE.MultiCamPreRenderEvent;
#endif
					}
					break;
			}

			//이제 업데이트 함수가 호출되도록 렌더 이벤트를 등록하고,
			//타입에 따라선 커맨드 버퍼를 지금 등록하자
#if UNITY_2019_1_OR_NEWER
			if(_funcCallType == FUNC_CALL_TYPE.SRPEvent)
			{
				//이벤트 추가 전에 삭제를 한번 한다.
				RenderPipelineManager.beginCameraRendering -= RenderEvent_SRP;
				RenderPipelineManager.beginCameraRendering += RenderEvent_SRP;
			}
#endif

			if(_funcCallType == FUNC_CALL_TYPE.CalculateCall
				|| _funcCallType == FUNC_CALL_TYPE.MultiCamPreRenderEvent)
			{
				//업데이트내에서의 연산 또는 MultiCam Event에서는 
				//커맨드 버퍼를 카메라에 등록해야한다.
				AddCommandBufferToCameras();
			}

			if(_funcCallType == FUNC_CALL_TYPE.MultiCamPreRenderEvent)
			{
				//SRP외의 다중 카메라 또는 VR에서는 MultiCam으로 부터 이벤트를 받아서 업데이트하자
				SetPreRenderEventToMultiCameraController();
			}

			//이벤트 등록됨
			_renderEventSync = RENDER_EVENT_SYNC.Added;
		}


		/// <summary>
		/// 커맨드 버퍼들을 카메라에 입력하자
		/// </summary>
		private void AddCommandBufferToCameras()
		{
			apOptMaskProcess curProcess = null;
			foreach (KeyValuePair<apOptRootUnit, apOptMaskProcess> processPair in _rootUnit2Process)
			{
				curProcess = processPair.Value;
				curProcess.AddCommandBufferToCameras();
			}
		}

		/// <summary>
		/// 다중 카메라의 경우, PreRender 이벤트 콜백을 수신할 수 있도록 하자
		/// </summary>
		private void SetPreRenderEventToMultiCameraController()
		{
			if(_nCamUnits > 0)
			{
				apOptMaskRenderCameraUnit curCam = null;
				for (int i = 0; i < _nCamUnits; i++)
				{
					curCam = _camUnits[i];
					curCam.AddMultiCamRenderEvent(RenderEvent_MultiCam);
				}				
			}
		}




		/// <summary>
		/// [ Hide 시 호출 ]
		/// 마스크 렌더링을 중단한다.
		/// </summary>
		public void DisableRender()
		{
			if (!_isAnyMaskProcess)
			{
				//프로세스가 없다면 Enable되지 않는다.
				return;
			}

			//이벤트를 삭제한다.
			ClearEvents();

			//마스크 RT도 삭제한다.
			ClearMaskRTs();

			//카메라까지는 삭제하지 않는다.

			_funcCallType = FUNC_CALL_TYPE.None;
			_funcCameraType = FUNC_CAMERA_TYPE.None;
		}

		//---------------------------------------------------------
		// Update 함수가 호출되는 Render Event / Calculate Event
		//---------------------------------------------------------

		//1. Calculate Call 방식일 때 (Built-In)
		public void RenderEvent_CalculateCall()
		{
			if(!_isAnyMaskProcess
				|| _cameraSync == CAMERA_SYNC.NoCam
				|| _RTSync == RT_SYNC.NoRT
				|| _renderEventSync == RENDER_EVENT_SYNC.NoEvent)
			{
				//업데이트할 준비가 안되었다.
				return;
			}
			
			if(_funcCallType != FUNC_CALL_TYPE.CalculateCall)
			{
				//호출 방식이 맞지 않는다.
				return;
			}

			//이 방식은 Built-In + 일반 카메라(Basic)에서만 호출된다.
			if(_funcCameraType != FUNC_CAMERA_TYPE.Basic)
			{
				return;
			}

			//현재 유니티 씬의 카메라를 체크한다. (필요하면 갱신)
			CheckCamerasOnUpdate();

			// 현재 활성화된 RootUnit을 체크하여
			// 업데이트되어야 하는 프로세서 (_lastRunProcess)를 갱신한다.
			CheckAndSelectProcess();

			if(_lastRunProcess == null)
			{
				//현재 업데이트할 프로세스가 없다.
				return;
			}

			//프로세스를 업데이트한다. (Basic-BuiltIn)
			_lastRunProcess.Update_Basic_BuiltIn();
		}




		//2. SRP 일때
#if UNITY_2019_1_OR_NEWER
		/// <summary>
		/// SRP에서의 렌더 이벤트로서 여기서 업데이트를 하자
		/// </summary>
		private void RenderEvent_SRP(ScriptableRenderContext context, Camera cam)
		{
			if(!_isAnyMaskProcess
				|| _cameraSync == CAMERA_SYNC.NoCam
				|| _RTSync == RT_SYNC.NoRT
				|| _renderEventSync == RENDER_EVENT_SYNC.NoEvent)
			{
				//업데이트할 준비가 안되었다.
				return;
			}

			if(_funcCallType != FUNC_CALL_TYPE.SRPEvent)
			{
				//호출 방식이 맞지 않는다.
				return;
			}

			//현재 유니티 씬의 카메라를 체크한다. (필요하면 갱신)
			CheckCamerasOnUpdate();

			// 현재 활성화된 RootUnit을 체크하여
			// 업데이트되어야 하는 프로세서 (_lastRunProcess)를 갱신한다.
			CheckAndSelectProcess();

			if(_lastRunProcess == null)
			{
				//현재 업데이트할 프로세스가 없다.
				return;
			}

			//SRP 함수 내에서는 모든 타입의 카메라에 대한 업데이트를 실행한다.
			switch(_funcCameraType)
			{
				case FUNC_CAMERA_TYPE.Basic:
					_lastRunProcess.Update_Basic_SRP(ref context);
					break;

				case FUNC_CAMERA_TYPE.MultiCamera:
					{
						_cam2Unit.TryGetValue(cam, out apOptMaskRenderCameraUnit camUnit);
						if(camUnit != null)
						{
							//SRP에서의 Mask 최적화는 "모두가 Orthographic"일 때만 가능하다.							
							_lastRunProcess.Update_MultiCam_SRP(ref context, camUnit, _isAllCameraOrthographic);
						}
					}
					break;

				case FUNC_CAMERA_TYPE.SingleVR:
					{
						_cam2Unit.TryGetValue(cam, out apOptMaskRenderCameraUnit camUnit);
						if(camUnit != null)
						{
							_lastRunProcess.Update_SingleVR_SRP(ref context, camUnit);
						}
						
					}
					
					break;
			}
		}
#endif

		//3. Multi Cam PreRenderEvent 방식일 때
		/// <summary>
		/// MultiCamController의 렌더 이벤트로서, 여기서 업데이트를 하자
		/// </summary>
		/// <param name="cam"></param>
		private void RenderEvent_MultiCam(Camera cam)
		{
			if(!_isAnyMaskProcess
				|| _cameraSync == CAMERA_SYNC.NoCam
				|| _RTSync == RT_SYNC.NoRT
				|| _renderEventSync == RENDER_EVENT_SYNC.NoEvent)
			{
				//업데이트할 준비가 안되었다.
				return;
			}

			if(_funcCallType != FUNC_CALL_TYPE.MultiCamPreRenderEvent)
			{
				//호출 방식이 맞지 않는다.
				return;
			}

			//현재 유니티 씬의 카메라를 체크한다. (필요하면 갱신)
			CheckCamerasOnUpdate();

			// 현재 활성화된 RootUnit을 체크하여
			// 업데이트되어야 하는 프로세서 (_lastRunProcess)를 갱신한다.
			CheckAndSelectProcess();

			if(_lastRunProcess == null)
			{
				//현재 업데이트할 프로세스가 없다.
				return;
			}

			//다중 카메라의 경우 조건이 맞아야만 마스크 영역 최적화가 가능하다.			
			bool isMaskAreaOptimizable = false;
			if(_isAllCameraOrthographic)
			{
				//참고 : 이 조건은 SRP에서도 가능하다.
				isMaskAreaOptimizable = true;
			}
			else if(_isAllSameForward
				&& _portrait._billboardType != apPortrait.BILLBOARD_TYPE.None)
			{
				//참고 : 이 조건은 SRP에서는 무시한다.
				isMaskAreaOptimizable = true;
			}	
			

			//이 이벤트에서는 Basic을 제외한 업데이트가 수행된다.
			//(Basic은 CalculateCall에서 수행됨)
			switch(_funcCameraType)
			{
				case FUNC_CAMERA_TYPE.MultiCamera:
					_lastRunProcess.Update_MultiCam_BuiltIn(cam, isMaskAreaOptimizable);
					break;

				case FUNC_CAMERA_TYPE.SingleVR:
					_lastRunProcess.Update_SingleVR_BuiltIn(cam);
					break;
			}
		}


		
		/// <summary>
		/// 현재 재생중인 루트 유닛을 체크하여 적절한 프로세스를 가져온다.
		/// 가능하면 매번 체크하는게 아니라, _lastCheckRootUnit/_lastRunProcess 체크하고 여기에 저장한다.
		/// </summary>
		private void CheckAndSelectProcess()
		{
			apOptRootUnit curRootUnit = _portrait._curPlayingOptRootUnit;
			if(curRootUnit == null)
			{
				//현재 동작하는 루트 유닛이 없다.
				_lastCheckRootUnit = null;
				_lastRunProcess = null;
				return;
			}

			if(_lastCheckRootUnit != curRootUnit)
			{
				_lastCheckRootUnit = curRootUnit;				
				_lastRunProcess = null;
				_rootUnit2Process.TryGetValue(curRootUnit, out _lastRunProcess);//프로세서 변경
			}
		}



		/// <summary>
		/// 업데이트 중에도 카메라 동기화를 매번 한다.
		/// 단, 이미 RT/이벤트가 생성되었다는 가정하에 변동 사항이 있을때만 재동기화를 한다.
		/// </summary>
		private void CheckCamerasOnUpdate()
		{
			//카메라 갱신을 하자.
			//강제 갱신은 아니다.
			bool isCameraChanged = SyncCameras(false);//false : 강제 갱신 아님

			if(!isCameraChanged)
			{
				//카메라 변동 내역이 없다면 바로 종료
				return;
			}

			//변동 내역이 있다.
			//초기화를 해야한다.
			ClearEvents();

			_funcCallType = FUNC_CALL_TYPE.None;
			_funcCameraType = FUNC_CAMERA_TYPE.None;

			//2. 모든 프로세스에 대해서 카메라 동기화를 하고, Mask RT를 생성한다.
			apOptMaskProcess curProcess = null;
			foreach (KeyValuePair<apOptRootUnit, apOptMaskProcess> processPair in _rootUnit2Process)
			{
				curProcess = processPair.Value;
				if(isCameraChanged)
				{
					//프로세스의 카메라도 변경하자
					curProcess.SyncCamera(_camUnits);
				}

				//RT가 생성되어 있지않다면 생성해두자
				curProcess.MakeMaskRTs();
			}

			_RTSync = RT_SYNC.Created;//RT 생성


			if(_cameraType_Cur == CAMERA_TYPE.None)
			{
				//카메라 타입이 없어서 이벤트를 등록할 수 없다.
				return;
			}

			if(_cameraType_Cur == CAMERA_TYPE.None)
			{
				//카메라 타입이 없어서 이벤트를 등록할 수 없다.
				return;
			}

			//카메라와 SRP에 따라서
			//"호출 방식", "업데이트 방식 및 대리자"를 결정하자
			//3. 이벤트와 커맨드 버퍼를 등록하자
			switch(_cameraType_Cur)
			{
				case CAMERA_TYPE.Single_NoVR:
					{
						//일반적인 단일 카메라
						_funcCameraType = FUNC_CAMERA_TYPE.Basic;

#if UNITY_2019_1_OR_NEWER
						if(_isSRP)
						{
							//SRP는 무조건 SRP 이벤트에서 체크
							_funcCallType = FUNC_CALL_TYPE.SRPEvent;
						}
						else
						{
							//단일의 경우는 별도의 업데이트에서 실행
							_funcCallType = FUNC_CALL_TYPE.CalculateCall;
						}
						
#else
						_funcCallType = FUNC_CALL_TYPE.CalculateCall;
#endif
					}
					break;

				case CAMERA_TYPE.Multiple:
					{
						//다중 카메라
						_funcCameraType = FUNC_CAMERA_TYPE.MultiCamera;
#if UNITY_2019_1_OR_NEWER
						if(_isSRP)
						{
							//SRP는 무조건 SRP 이벤트에서 체크
							_funcCallType = FUNC_CALL_TYPE.SRPEvent;
						}
						else
						{
							//다중 카메라는 MultiCamController로부터 이벤트를 받자
							_funcCallType = FUNC_CALL_TYPE.MultiCamPreRenderEvent;
						}
						
#else
						_funcCallType = FUNC_CALL_TYPE.MultiCamPreRenderEvent;
#endif
					}
					break;

				case CAMERA_TYPE.Single_VR:
					{
						//단일 VR 타입이라면
						_funcCameraType = FUNC_CAMERA_TYPE.SingleVR;
#if UNITY_2019_1_OR_NEWER
						if(_isSRP)
						{
							//SRP는 무조건 SRP 이벤트에서 체크
							_funcCallType = FUNC_CALL_TYPE.SRPEvent;
						}
						else
						{
							//VR 카메라도 PreRender 이벤트를 별도로 받자
							_funcCallType = FUNC_CALL_TYPE.MultiCamPreRenderEvent;
						}
						
#else
						_funcCallType = FUNC_CALL_TYPE.MultiCamPreRenderEvent;
#endif
					}
					break;
			}

			//이제 업데이트 함수가 호출되도록 렌더 이벤트를 등록하고,
			//타입에 따라선 커맨드 버퍼를 지금 등록하자
#if UNITY_2019_1_OR_NEWER
			if(_funcCallType == FUNC_CALL_TYPE.SRPEvent)
			{
				//이벤트 추가 전에 삭제를 한번 한다.
				RenderPipelineManager.beginCameraRendering -= RenderEvent_SRP;
				RenderPipelineManager.beginCameraRendering += RenderEvent_SRP;
			}
#endif

			if(_funcCallType == FUNC_CALL_TYPE.CalculateCall
				|| _funcCallType == FUNC_CALL_TYPE.MultiCamPreRenderEvent)
			{
				//업데이트내에서의 연산 또는 MultiCam Event에서는 
				//커맨드 버퍼를 카메라에 등록해야한다.
				AddCommandBufferToCameras();
			}

			if(_funcCallType == FUNC_CALL_TYPE.MultiCamPreRenderEvent)
			{
				//SRP외의 다중 카메라 또는 VR에서는 MultiCam으로 부터 이벤트를 받아서 업데이트하자
				SetPreRenderEventToMultiCameraController();
			}

			//이벤트 등록됨
			_renderEventSync = RENDER_EVENT_SYNC.Added;
		}


		// Refresh
		//---------------------------------------------------------
		/// <summary>
		/// 카메라에 무슨 변경 사항이 있는지 체크
		/// </summary>
		/// <returns></returns>
		private bool IsAnyCameraChanged()
		{
			apOptMainCamera mainCam = _portrait.GetMainCamera();
			if(mainCam == null)
			{
				return false;
			}
			return _refreshKey != mainCam._refreshKey;
		}


		/// <summary>
		/// 씬의 카메라들과 멤버의 카메라 정보가 동일하도록 동기화를 한다.
		/// 변동사항이 있다면 true를 리턴한다.
		/// </summary>
		/// <param name="isRefreshForce"></param>
		public bool SyncCameras(bool isRefreshForce)
		{
			apOptMainCamera mainCam = _portrait.GetMainCamera();
			if(mainCam == null)
			{
				Clear();
				return true;
			}

			//카메라 상태를 갱신하자
			_vrSupportMode = mainCam.VRSupportMode;
			apOptMainCamera.NumberOfCamera numberOfCamera = mainCam.GetNumberOfCamera();

			switch (numberOfCamera)
			{
				case apOptMainCamera.NumberOfCamera.None:
					_cameraType_Cur = CAMERA_TYPE.None;
					break;

				case apOptMainCamera.NumberOfCamera.Single:
					{
						if (_vrSupportMode == apPortrait.VR_SUPPORT_MODE.SingleCamera)
						{
							//VR이 되는 단일 카메라
							_cameraType_Cur = CAMERA_TYPE.Single_VR;
						}
						else
						{
							//일반 단일 카메라
							_cameraType_Cur = CAMERA_TYPE.Single_NoVR;
						}
					}
					break;

				case apOptMainCamera.NumberOfCamera.Multiple:
					_cameraType_Cur = CAMERA_TYPE.Multiple;
					break;
			}

			//영역 최적화 가능 여부를 갱신하자
			_isAllSameForward = mainCam.IsAllSameForward;
			_isAllCameraOrthographic = mainCam.IsAllOrthographic;

			//갱신 필요 여부를 체크하자
			if(!isRefreshForce
				&& mainCam._refreshKey == _refreshKey
				&& _cameraSync == CAMERA_SYNC.Refreshed
				&& _cameraType_Cur == _cameraType_Prev)
			{
				//카메라 상태가 동일하여 갱신할 필요가 없다.
				return false;
			}

			//카메라를 갱신해야한다.
			if(_camUnits == null) { _camUnits = new List<apOptMaskRenderCameraUnit>(); }
			if(_cam2Unit == null) { _cam2Unit = new Dictionary<Camera, apOptMaskRenderCameraUnit>(); }

			

			//일단 업데이트를 준비하자
			apOptMaskRenderCameraUnit curCamUnit = null;
			for (int i = 0; i < _nCamUnits; i++)
			{
				curCamUnit = _camUnits[i];
				curCamUnit.ReadyToCheck();
			}

			//MainCamera에서 동기화했던 "실제 카메라"들을 가져온다.
			List<apOptMainCamera.CameraData> mainRenderCameraDataList = mainCam.RenderCameraDataList;
			int nMainCamData = mainRenderCameraDataList != null ? mainRenderCameraDataList.Count : 0;

			bool isAnyAddedOrRemoved = false;

			//MainCamera에서의 동기화 여부(RefreshKey의 값이 양수)와 동기화된 카메라 데이터를 받아서 갱신을 하자
			if(mainCam._refreshKey > 0 && nMainCamData > 0)
			{
				apOptMainCamera.CameraData curMainCamData = null;
				Camera targetCamera = null;
				for (int iMainCam = 0; iMainCam < nMainCamData; iMainCam++)
				{
					curMainCamData = mainRenderCameraDataList[iMainCam];
					targetCamera = curMainCamData._camera;
					if(targetCamera == null)
					{
						continue;
					}
					curCamUnit = null;
					_cam2Unit.TryGetValue(targetCamera, out curCamUnit);

					//이미 등록된 카메라
					if(curCamUnit != null)
					{						
						//업데이트할 때마다 카메라의 Target Texture 값을 받아서 저장
						curCamUnit.RefreshTargetTexture();

						//이건 삭제 대상이 아님을 체크
						curCamUnit.SetCheck();
					}
					else
					{
						// < 등록되지 않은 카메라 발견! >
						//카메라를 생성한다.
						//(이 상태에서는 커맨드 버퍼등이 추가되지 않은 빈 카메라다.)
						apOptMaskRenderCameraUnit newCamUnit = new apOptMaskRenderCameraUnit(targetCamera);

						//새 카메라 유닛이 삭제되지 않도록 체크
						newCamUnit.SetCheck();

						//리스트에 추가
						_camUnits.Add(newCamUnit);

						//중복 방지로 임시로 Dictionary도 갱신
						_cam2Unit.Add(targetCamera, newCamUnit);

						//변동 사항이 발생했다.
						isAnyAddedOrRemoved = true;
					}
				}
			}

			//삭제될 유닛들을 찾자
			_nCamUnits = _camUnits.Count;
			List<apOptMaskRenderCameraUnit> removeUnits = null;

			for (int i = 0; i < _nCamUnits; i++)
			{
				curCamUnit = _camUnits[i];

				if(curCamUnit.IsChecked())
				{
					continue;
				}

				//이건 존재하지 않는 카메라에 대한 정보다. 삭제하자
				if(removeUnits == null)
				{
					removeUnits = new List<apOptMaskRenderCameraUnit>();
				}
				removeUnits.Add(curCamUnit);
				isAnyAddedOrRemoved = true;//삭제되는 카메라 정보가 있다.
			}

			int nRemoved = removeUnits != null ? removeUnits.Count : 0;
			if(nRemoved > 0)
			{
				//유효하지 않은 카메라 정보를 삭제하자
				for (int i = 0; i < nRemoved; i++)
				{
					curCamUnit = removeUnits[i];

					//Built-In의 경우) 등록된 커맨드 버퍼의 등록을 해제
					curCamUnit.RemoveAllCommandBuffers(_isSRP);

					//다중 카메라인 경우) 등록된 PreRender 이벤트의 등록을 해제
					curCamUnit.RemoveMultiCamRenderEvent();

					_camUnits.Remove(curCamUnit);
				}
			}

			_nCamUnits = _camUnits.Count;

			if(isAnyAddedOrRemoved)
			{
				//카메라 유닛이 추가/삭제된 경우엔 Dictionay도 갱신해야한다.
				_cam2Unit.Clear();

				for (int i = 0; i < _nCamUnits; i++)
				{
					curCamUnit = _camUnits[i];
					_cam2Unit.Add(curCamUnit.Camera, curCamUnit);
				}
			}

			//동기화가 끝났음을 기록하자
			_refreshKey = mainCam._refreshKey;
			_cameraSync = CAMERA_SYNC.Refreshed;
			_cameraType_Prev = _cameraType_Cur;

			return true;//변동 사항이 있다 > true
			
		}




		//---------------------------------------------------



    }
}