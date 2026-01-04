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
using System;

using AnyPortrait;

namespace AnyPortrait
{
    /// <summary>
    /// Renderer들과 Receiver들을 가진 그룹.
    /// 루트 유닛 단위로 저장이 된다.
    /// </summary>
    public class apOptMaskProcess
    {
		// Members
		//------------------------------------------------
		public apOptRootUnit _rootUnit = null;

		////업데이트 시에는 Clipping/PerMesh는 구분이 필요없다.
		////Shared는 중복 체크때문에 필요함
				
		////공유 RT별 SendData
		////ID, ShaderType, (커스텀시 Shader Asset)에 따라 다르게 입력된다.
		//private List<apOptMaskRenderer> _renderers_Shared = null;
		//private int _nRenderers_Shared = 0;

		////전체 Renderers (List / Array)
		//private List<apOptMaskRenderer> _renderers_Total = null;
  //      private apOptMaskRenderer[] _renderers_Arr = null;
  //      private int _nRenderers_Total = 0;


  //      //Receiver도 저장을 한다.
  //      private List<apOptMaskReceiver> _receivers = null;
  //      private apOptMaskReceiver[] _receivers_Arr = null;
  //      private int _nReceivers = 0;

		private PhaseSubProcess[] _subProcess = null;
		private const int NUM_SUB_PROCESS = 3;
		private const int PHASE1 = 0;
		private const int PHASE2 = 1;
		private const int PHASE3 = 2;

		private bool _isAnyRendererAndReceivers = false;

		
		// Init
		//--------------------------------------------------
		public apOptMaskProcess(apOptRootUnit rootUnit)
        {
            _rootUnit = rootUnit;

            //if(_renderers_Shared == null) { _renderers_Shared = new List<apOptMaskRenderer>(); }
            //_renderers_Shared.Clear();
            //_nRenderers_Shared = 0;

            //if(_renderers_Total == null) { _renderers_Total = new List<apOptMaskRenderer>(); }
            //_renderers_Total.Clear();
            //_renderers_Arr = null;
            //_nRenderers_Total = 0;

            //if(_receivers == null) { _receivers = new List<apOptMaskReceiver>(); }
            //_receivers.Clear();
            //_receivers_Arr = null;
            //_nReceivers = 0;

			_subProcess = new PhaseSubProcess[NUM_SUB_PROCESS];
			_subProcess[PHASE1] = new PhaseSubProcess();
			_subProcess[PHASE2] = new PhaseSubProcess();
			_subProcess[PHASE3] = new PhaseSubProcess();
			_isAnyRendererAndReceivers = false;
		}

        // 마스크의 Parent/Child를 등록하기
        //---------------------------------------------------
        public void AddRenderer_ClippingParent(apOptMaskRenderer newRenderer)
        {
			if(newRenderer == null)
			{
				return;
			}

			////Total에도 저장
			//if(_renderers_Total == null)
			//{
			//    _renderers_Total = new List<apOptMaskRenderer>();                
			//}
			//_renderers_Total.Add(newRenderer);
			//_renderers_Arr = _renderers_Total.ToArray();//배열 갱신
			//_nRenderers_Total = _renderers_Total.Count;

			//Phase를 이용하여 SubProcess에 추가한다.
			//Clipping은 Phase1에만 들어간다.
			_subProcess[PHASE1].AddRenderer_ClippingParent(newRenderer);
			_isAnyRendererAndReceivers = true;
		}


        public void AddRenderer_SendDataPerMesh(apOptMaskRenderer newRenderer)
        {
			if(newRenderer == null)
			{
				return;
			}

			////Total에도 저장
			//         if(_renderers_Total == null)
			//         {
			//             _renderers_Total = new List<apOptMaskRenderer>();                
			//         }
			//         _renderers_Total.Add(newRenderer);
			//         _renderers_Arr = _renderers_Total.ToArray();//배열 갱신
			//         _nRenderers_Total = _renderers_Total.Count;

			//Phase를 이용하여 SubProcess에 추가한다.
			int iPhase = PHASE1;
			switch (newRenderer.RenderOrder)
			{
				case apSendMaskData.RT_RENDER_ORDER.Phase1:
					iPhase = PHASE1;
					break;

				case apSendMaskData.RT_RENDER_ORDER.Phase2:
					iPhase = PHASE2;
					break;

				case apSendMaskData.RT_RENDER_ORDER.Phase3:
					iPhase = PHASE3;
					break;
			}

			_subProcess[iPhase].AddRenderer_SendDataPerMesh(newRenderer);
			_isAnyRendererAndReceivers = true;
		}



		// Shared는 바로 Add하지 않고, 동일한 ID의 렌더러가 있으면, 거기에 더 추가를 한다.
		public apOptMaskRenderer GetSharedRenderer(int sharedID)
        {
			//if(_renderers_Shared == null || _nRenderers_Shared == 0)
			//{
			//    return null;
			//}

			////조건에 맞는걸 찾아서 리턴한다.
			//apOptMaskRenderer curRenderer = null;
			//for (int i = 0; i < _nRenderers_Shared; i++)
			//{
			//    curRenderer = _renderers_Shared[i];

			//    //ID가 같은가
			//    if(curRenderer.SharedID != sharedID)
			//    {
			//        continue;
			//    }

			//    //조건에 맞는 공유 RT를 가진 Renderer를 찾았다.
			//    //여기에서 같이 렌더링을 하자
			//    return curRenderer;
			//}

			//return null;

			//SubProcess를 이용하여 Shared Renderer를 찾는다.
			apOptMaskRenderer existSharedRenderer = null;
			for (int i = 0; i < NUM_SUB_PROCESS; i++)
			{
				existSharedRenderer = _subProcess[i].GetSharedRenderer(sharedID);
				if(existSharedRenderer != null)
				{
					return existSharedRenderer;
				}
			}
			return null;
		}


		//새로운 Shared RT에 대한 렌더러를 추가한다.
		//중복 체크는 GetSharedRenderer 호출 후 외부에서 미리 하자
		public void AddRenderer_SendDataShared(apOptMaskRenderer newRenderer)
        {
			if(newRenderer == null)
			{
				return;
			}
            //if (_renderers_Shared == null)
            //{
            //    _renderers_Shared = new List<apOptMaskRenderer>();
            //}
            //_renderers_Shared.Add(newRenderer);
            //_nRenderers_Shared = _renderers_Shared.Count;

            ////Total에도 저장
            //if(_renderers_Total == null)
            //{
            //    _renderers_Total = new List<apOptMaskRenderer>();                
            //}
            //_renderers_Total.Add(newRenderer);
            //_renderers_Arr = _renderers_Total.ToArray();//배열 갱신
            //_nRenderers_Total = _renderers_Total.Count;

			
			//Phase를 이용하여 SubProcess에 추가한다.
			int iPhase = PHASE1;
			switch (newRenderer.RenderOrder)
			{
				case apSendMaskData.RT_RENDER_ORDER.Phase1:
					iPhase = PHASE1;
					break;

				case apSendMaskData.RT_RENDER_ORDER.Phase2:
					iPhase = PHASE2;
					break;

				case apSendMaskData.RT_RENDER_ORDER.Phase3:
					iPhase = PHASE3;
					break;
			}

			_subProcess[iPhase].AddRenderer_SendDataShared(newRenderer);

			_isAnyRendererAndReceivers = true;
        }

        public void AddReceiver(apOptMaskReceiver receiver)
        {
			if(receiver == null)
			{
				return;
			}
			
			apOptMaskRenderer linkedRenderer = receiver.LinkedRenderer;
			if (linkedRenderer == null)
			{
				return;
			}

			//if (_receivers == null)
			//         {
			//             _receivers = new List<apOptMaskReceiver>();
			//         }
			//         _receivers.Add(receiver);
			//         _nReceivers = _receivers.Count;

			//         //배열에도 추가
			//         _receivers_Arr = _receivers.ToArray();

			//Receiver도 렌더러의 순서에 맞게 처리되어야 한다.
			//중첩된 마스크 전송하기 순서를 맞춰야 하기 때문이다.
			int iPhase = PHASE1;
			switch (linkedRenderer.RenderOrder)
			{
				case apSendMaskData.RT_RENDER_ORDER.Phase1:
					iPhase = PHASE1;
					break;

				case apSendMaskData.RT_RENDER_ORDER.Phase2:
					iPhase = PHASE2;
					break;

				case apSendMaskData.RT_RENDER_ORDER.Phase3:
					iPhase = PHASE3;
					break;
			}

			_subProcess[iPhase].AddReceiver(receiver);

			_isAnyRendererAndReceivers = true;

        }
        
		// 링크 완료시
		//--------------------------------------------------
		public void OnInitCompleted()
		{
			////Renderer들을 renderOrder에 맞추어서 정렬하고 배열로 변환하여 저장한다.
			//if(_nRenderers_Total > 0)
			//{
			//	//Renderer들의 _renderOrder가 Phase1이 앞으로 오고 Phase 3이 뒤로 가도록 정렬한다.
			//	//Phase1 -> Phase2 -> Phase3 순으로 정렬한다.
			//	//Phase를 int로 변환하여 오름차순이 되도록 정렬하자
			//	_renderers_Total.Sort(delegate (apOptMaskRenderer a, apOptMaskRenderer b)
			//	{
			//		return (int)a.RenderOrder - (int)b.RenderOrder;
			//	});

			//	//배열로 저장한다.
			//	_renderers_Arr = _renderers_Total.ToArray();

			//	////디버그
			//	//Debug.Log("Sort 디버그");
			//	//for (int i = 0; i < _nRenderers_Total; i++)
			//	//{
			//	//	Debug.Log(_renderers_Arr[i].RenderOrder);
			//	//}
			//}
			if(!_isAnyRendererAndReceivers)
			{
				return;
			}

			//SubProcess의 빠른 업데이트를 위해서 배열로 변환하고 체인을 검토한다.
			for (int i = 0; i < NUM_SUB_PROCESS; i++)
			{
				_subProcess[i].OnInitCompleted();
			}

			//체인 검토
			//앞 페이즈의 Receiver 리스트를 다음 페이즈에 입력하여 동일한 메시의 렌더러가 있는지 찾는다.
			//Phase 1 > Phase 2, 3
			//Phase 2 > Phase 3 으로 연결될 수 있다.
			apOptMaskReceiver[] receivers_Phase1 = _subProcess[PHASE1].Receivers;
			apOptMaskReceiver[] receivers_Phase2 = _subProcess[PHASE2].Receivers;
			_subProcess[PHASE2].CheckChain(receivers_Phase1);//Phase 2에서 Phase 1의 리시버를 검토하여 체이닝
			
			_subProcess[PHASE3].CheckChain(receivers_Phase1);//Phase 3에서 Phase 1의 리시버를 검토하여 체이닝
			_subProcess[PHASE3].CheckChain(receivers_Phase2);//Phase 3에서 Phase 2의 리시버를 검토하여 체이닝
		}


        // 초기화 (RT)
        //---------------------------------------------------
        /// <summary>
        /// [Show / Init시 호출]
        /// 마스크를 생성한다. 카메라 동기화 후 호출한다.
        /// </summary>
        public void MakeMaskRTs()
        {
			if(!_isAnyRendererAndReceivers)
			{
				return;
			}

			//if(_nRenderers_Total == 0)
			//{
			//    return;
			//}

			//for (int i = 0; i < _nRenderers_Total; i++)
			//{
			//    _renderers_Arr[i].MakeMaskRTs();
			//}

			//서브 프로세스별로 호출
			for (int i = 0; i < NUM_SUB_PROCESS; i++)
			{
				_subProcess[i].MakeMaskRTs();
			}
        }

        /// <summary>
        /// [Hide시 호출]
        /// 생성된 마스크 RT를 모두 제거한다.
        /// </summary>
        public void RemoveMaskRTs()
        {
			if(!_isAnyRendererAndReceivers)
			{
				return;
			}

            //if(_nRenderers_Total == 0)
            //{
            //    return;
            //}
            
            //for (int i = 0; i < _nRenderers_Total; i++)
            //{
            //    _renderers_Arr[i].ReleaseMaskRTs();
            //}

			//서브 프로세스별로 호출
			for (int i = 0; i < NUM_SUB_PROCESS; i++)
			{
				_subProcess[i].RemoveMaskRTs();
			}
        }


        //카메라 동기화
        //---------------------------------------------------
        /// <summary>
        /// 현재 카메라와 동기화를 한다.
        /// 모든 렌더러에 동기화를 요청한다.
        /// </summary>
        /// <param name="cameraUnits"></param>
        public void SyncCamera(List<apOptMaskRenderCameraUnit> cameraUnits)
        {
			if(!_isAnyRendererAndReceivers)
			{
				return;
			}

			//서브 프로세스별로 호출
			for (int i = 0; i < NUM_SUB_PROCESS; i++)
			{
				_subProcess[i].SyncCamera(cameraUnits);
			}
		}


		// 카메라에 커맨드 버퍼를 등록
		//---------------------------------------------------
		/// <summary>
		/// 동기화된 카메라에 커맨드 버퍼를 등록한다.
		/// 이 함수를 호출하기 전에는 카메라 동기화를 먼저 수행해야한다.
		/// </summary>
		public void AddCommandBufferToCameras()
        {
			if(!_isAnyRendererAndReceivers)
			{
				return;
			}

			//서브 프로세스별로 호출
			for (int i = 0; i < NUM_SUB_PROCESS; i++)
			{
				_subProcess[i].AddCommandBufferToCameras();
			}
		}


		// 업데이트
		//---------------------------------------------------

		//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
		// 단일+일반 카메라에서의 Parent/Child 업데이트 (Built-In / SRP 각각)
		//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
		public void Update_Basic_BuiltIn()
		{
            
            //Debug.Log("마스크 업데이트 - Basic Built-In (Parent : " + _nRenderers_Total + " / Child : " + _nReceivers + ")");
            // Parent 업데이트
			if(!_isAnyRendererAndReceivers)
			{
				return;
			}

			//재질의 프로퍼티 복사 먼저
			for (int i = 0; i < NUM_SUB_PROCESS; i++)
			{
				_subProcess[i].PreUpdate_CopyProperties();
			}

			//Phase별로 호출 (순서가 중요)
			for (int i = 0; i < NUM_SUB_PROCESS; i++)
			{
				_subProcess[i].Update_Basic_BuiltIn();
			}
		}



#if UNITY_2019_1_OR_NEWER
		/// <summary>
		/// 단일 카메라에서의 마스크 업데이트 (SRP)
		/// </summary>
		public void Update_Basic_SRP(ref ScriptableRenderContext context)
		{
			if(!_isAnyRendererAndReceivers)
			{
				return;
			}

			//재질의 프로퍼티 복사 먼저
			for (int i = 0; i < NUM_SUB_PROCESS; i++)
			{
				_subProcess[i].PreUpdate_CopyProperties();
			}

			//Phase별로 호출 (순서가 중요)
			for (int i = 0; i < NUM_SUB_PROCESS; i++)
			{
				_subProcess[i].Update_Basic_SRP(ref context);
			}

		}
#endif


		//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
		// 다중 카메라에서의 Parent/Child 업데이트 (Built-In / SRP 각각)
		//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
		/// <summary>
		/// 다중 카메라에서의 마스크 업데이트 (Build-In)
		/// </summary>
		public void Update_MultiCam_BuiltIn(Camera cam, bool isMaskAreaOptimizable)
		{
     //       // Parent 업데이트
     //       if(_nRenderers_Total > 0)
     //       {
     //           for (int i = 0; i < _nRenderers_Total; i++)
     //           {
     //               _renderers_Arr[i].Update_MultiCam_BuiltIn(cam, isMaskAreaOptimizable);
     //           }
     //       }

     //       // Child 업데이트
     //       if(_nReceivers > 0)
     //       {
     //           for (int i = 0; i < _nReceivers; i++)
     //           {
     //               //_receivers_Arr[i].Update_MultipleCam(cam);
					////Receiver는 Multi-Cam 로직이 Basic과 동일하다.
					////Renderer 업데이트 직후에 저장된 RT를 가져다 쓰면 되므로 카메라 구분이 필요 없어서 Basic과 로직이 같다.
					//_receivers_Arr[i].Update_Basic();
     //           }
     //       }

			if(!_isAnyRendererAndReceivers)
			{
				return;
			}

			//재질의 프로퍼티 복사 먼저
			for (int i = 0; i < NUM_SUB_PROCESS; i++)
			{
				_subProcess[i].PreUpdate_CopyProperties();
			}

			//Phase별로 호출 (순서가 중요)
			for (int i = 0; i < NUM_SUB_PROCESS; i++)
			{
				_subProcess[i].Update_MultiCam_BuiltIn(cam, isMaskAreaOptimizable);
			}
		}


#if UNITY_2019_1_OR_NEWER
        /// <summary>
		/// 다중 카메라에서의 마스크 업데이트 (SRP)
		/// </summary>
		public void Update_MultiCam_SRP(ref ScriptableRenderContext context,
										apOptMaskRenderCameraUnit camUnit,
										bool isMaskAreaOptimizable)
		{
    //        // Parent 업데이트
    //        if(_nRenderers_Total > 0)
    //        {
    //            for (int i = 0; i < _nRenderers_Total; i++)
    //            {
    //                _renderers_Arr[i].Update_MultiCam_SRP(ref context, camUnit, isMaskAreaOptimizable);
    //            }

				////여기서 커맨드 버퍼 일괄 제출
				//context.Submit();
    //        }

    //        // Child 업데이트
    //        if(_nReceivers > 0)
    //        {
    //            for (int i = 0; i < _nReceivers; i++)
    //            {
    //                //_receivers_Arr[i].Update_MultipleCam(camUnit.Camera);
				//	_receivers_Arr[i].Update_Basic();//Basic 로직 재활용
    //            }
    //        }

			if(!_isAnyRendererAndReceivers)
			{
				return;
			}

			//재질의 프로퍼티 복사 먼저
			for (int i = 0; i < NUM_SUB_PROCESS; i++)
			{
				_subProcess[i].PreUpdate_CopyProperties();
			}

			//Phase별로 호출 (순서가 중요)
			for (int i = 0; i < NUM_SUB_PROCESS; i++)
			{
				_subProcess[i].Update_MultiCam_SRP(ref context, camUnit, isMaskAreaOptimizable);
			}
		}
#endif


        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        // VR 카메라에서의 Parent/Child 업데이트 (Built-In / SRP 각각)
        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        public void Update_SingleVR_BuiltIn(Camera cam)
		{
            ////Debug.Log("마스크 업데이트 - SingleVR Built-In (Parent : " + _nRenderers_Total + " / Child : " + _nReceivers + ")");

            //// Parent 업데이트
            //if(_nRenderers_Total > 0)
            //{
            //    for (int i = 0; i < _nRenderers_Total; i++)
            //    {
            //        _renderers_Arr[i].Update_SingleVR_BuiltIn();
            //    }
            //}
            
            //// Child 업데이트
            //if(_nReceivers > 0)
            //{
            //    for (int i = 0; i < _nReceivers; i++)
            //    {
            //        _receivers_Arr[i].Update_SingleVR();
            //    }
            //}

			if(!_isAnyRendererAndReceivers)
			{
				return;
			}

			//재질의 프로퍼티 복사 먼저
			for (int i = 0; i < NUM_SUB_PROCESS; i++)
			{
				_subProcess[i].PreUpdate_CopyProperties();
			}

			//Phase별로 호출 (순서가 중요)
			for (int i = 0; i < NUM_SUB_PROCESS; i++)
			{
				_subProcess[i].Update_SingleVR_BuiltIn(cam);
			}
		}

#if UNITY_2019_1_OR_NEWER
        public void Update_SingleVR_SRP(ref ScriptableRenderContext context, apOptMaskRenderCameraUnit camUnit)
		{
    //        // Parent 업데이트
    //        if(_nRenderers_Total > 0)
    //        {
    //            for (int i = 0; i < _nRenderers_Total; i++)
    //            {
    //                _renderers_Arr[i].Update_SingleVR_SRP(ref context);
    //            }

				////여기서 커맨드 버퍼 일괄 제출
				//context.Submit();
    //        }

    //        // Child 업데이트
    //        if(_nReceivers > 0)
    //        {
    //            for (int i = 0; i < _nReceivers; i++)
    //            {
    //                _receivers_Arr[i].Update_SingleVR();
    //            }
    //        }

			if(!_isAnyRendererAndReceivers)
			{
				return;
			}

			//재질의 프로퍼티 복사 먼저
			for (int i = 0; i < NUM_SUB_PROCESS; i++)
			{
				_subProcess[i].PreUpdate_CopyProperties();
			}

			//Phase별로 호출 (순서가 중요)
			for (int i = 0; i < NUM_SUB_PROCESS; i++)
			{
				_subProcess[i].Update_SingleVR_SRP(ref context, camUnit);
			}
		}
#endif



		// Sub Class
		//------------------------------------------------
		//Renderer / Receiver를 렌더 순서의 Phase 별로 만든다.
		public class PhaseSubProcess
		{
			//Shared
			private List<apOptMaskRenderer> _renderers_Shared = null;
			private int _nRenderers_Shared = 0;

			//전체 Renderers (List / Array)
			private List<apOptMaskRenderer> _renderers_Total = null;
			private apOptMaskRenderer[] _renderers_Arr = null;
			private int _nRenderers_Total = 0;


			//Receiver도 저장을 한다.
			private List<apOptMaskReceiver> _receivers = null;
			private apOptMaskReceiver[] _receivers_Arr = null;
			private int _nReceivers = 0;

			// Init
			//--------------------------------------------------
			public PhaseSubProcess()
			{
				if (_renderers_Shared == null) { _renderers_Shared = new List<apOptMaskRenderer>(); }
				_renderers_Shared.Clear();
				_nRenderers_Shared = 0;

				if (_renderers_Total == null) { _renderers_Total = new List<apOptMaskRenderer>(); }
				_renderers_Total.Clear();
				_renderers_Arr = null;
				_nRenderers_Total = 0;

				if (_receivers == null) { _receivers = new List<apOptMaskReceiver>(); }
				_receivers.Clear();
				_receivers_Arr = null;
				_nReceivers = 0;
			}

			// Add Renderer
			//-----------------------------------------------------------
			public void AddRenderer_ClippingParent(apOptMaskRenderer newRenderer)
			{
				//Total에도 저장
				if(_renderers_Total == null)
				{
					_renderers_Total = new List<apOptMaskRenderer>();                
				}
				_renderers_Total.Add(newRenderer);
				//_renderers_Arr = _renderers_Total.ToArray();//배열 갱신
				_nRenderers_Total = _renderers_Total.Count;
			}

			public void AddRenderer_SendDataPerMesh(apOptMaskRenderer newRenderer)
			{
				//Total에도 저장
				if(_renderers_Total == null)
				{
					_renderers_Total = new List<apOptMaskRenderer>();                
				}
				_renderers_Total.Add(newRenderer);
				//_renderers_Arr = _renderers_Total.ToArray();//배열 갱신
				_nRenderers_Total = _renderers_Total.Count;
			}


			public apOptMaskRenderer GetSharedRenderer(int sharedID)
			{
				if(_renderers_Shared == null || _nRenderers_Shared == 0)
				{
					return null;
				}

				//조건에 맞는걸 찾아서 리턴한다.
				apOptMaskRenderer curRenderer = null;
				for (int i = 0; i < _nRenderers_Shared; i++)
				{
					curRenderer = _renderers_Shared[i];
                
					//ID가 같은가
					if(curRenderer.SharedID != sharedID)
					{
						continue;
					}

					//조건에 맞는 공유 RT를 가진 Renderer를 찾았다.
					//여기에서 같이 렌더링을 하자
					return curRenderer;
				}
            
				return null;
			}

			public void AddRenderer_SendDataShared(apOptMaskRenderer newRenderer)
			{
				if (_renderers_Shared == null)
				{
					_renderers_Shared = new List<apOptMaskRenderer>();
				}
				_renderers_Shared.Add(newRenderer);
				_nRenderers_Shared = _renderers_Shared.Count;

				//Total에도 저장
				if(_renderers_Total == null)
				{
					_renderers_Total = new List<apOptMaskRenderer>();                
				}
				_renderers_Total.Add(newRenderer);
				//_renderers_Arr = _renderers_Total.ToArray();//배열 갱신
				_nRenderers_Total = _renderers_Total.Count;
			}

			// Add Receiver
			//------------------------------------------------------------------
			public void AddReceiver(apOptMaskReceiver receiver)
			{
				if(_receivers == null)
				{
					_receivers = new List<apOptMaskReceiver>();
				}
				_receivers.Add(receiver);
				_nReceivers = _receivers.Count;

				//배열에도 추가
				//_receivers_Arr = _receivers.ToArray();

			}


			// List > Array
			//------------------------------------------------------------------
			/// <summary>
			/// 빠른 업데이트를 위해서 리스트를 배열로 만들자
			/// </summary>
			public void OnInitCompleted()
			{
				//개수 한번 더 갱신하고
				_nRenderers_Total = _renderers_Total != null ? _renderers_Total.Count : 0;
				if(_nRenderers_Total > 0)
				{
					_renderers_Arr = _renderers_Total.ToArray();
				}
				else
				{
					_renderers_Arr = null;
				}


				_nReceivers = _receivers != null ? _receivers.Count : 0;
				if(_nReceivers > 0)
				{
					_receivers_Arr = _receivers.ToArray();
				}
				else
				{
					_receivers_Arr = null;
				}
			}

			/// <summary>
			/// 이전 버전의 Receiver 리스트를 받아서 체인 여부를 검토한다.
			/// </summary>
			/// <param name="prevPhaseReceivers"></param>
			public void CheckChain(apOptMaskReceiver[] prevPhaseReceivers)
			{
				int nPrevReceivers = prevPhaseReceivers != null ? prevPhaseReceivers.Length : 0;
				if(nPrevReceivers == 0)
				{
					//입력한 리시버가 없다.
					return;
				}

				if(_nRenderers_Total == 0)
				{
					//이 페이즈엔 체인의 대상이 될만한 렌더러가 없다.
					return;
				}

				//이전 페이즈의 리시버의 메시를 공유하는 렌더러가 있다면 체인을 하자
				apOptMaskReceiver prevRecv = null;
				apOptMesh recvMesh = null;

				apOptMaskRenderer curRender = null;
				for (int iRecv = 0; iRecv < nPrevReceivers; iRecv++)
				{
					prevRecv = prevPhaseReceivers[iRecv];
					recvMesh = prevRecv.ReceivedMesh;
					if (recvMesh == null)
					{
						continue;
					}

					//이 메시를 가진 Renderer를 찾자
					//- Clipping과 PerMesh타입은 메시 하나만 비교하면 된다.
					//- Shared의 경우는 여러개의 메시들이 렌더링되므로, 내부 데이터를 비교해야 한다.
					for (int iRender = 0; iRender < _nRenderers_Total; iRender++)
					{
						curRender = _renderers_Arr[iRender];
						curRender.CheckChainFromPrevReceiver(prevRecv);
					}
				}
			}


			public apOptMaskReceiver[] Receivers { get { return _receivers_Arr; } }


			// Make Rt
			//------------------------------------------------------------------
			/// <summary>
			/// [Show / Init시 호출]
			/// 마스크를 생성한다. 카메라 동기화 후 호출한다.
			/// </summary>
			public void MakeMaskRTs()
			{
				if(_nRenderers_Total == 0)
				{
					return;
				}
            
				for (int i = 0; i < _nRenderers_Total; i++)
				{
					_renderers_Arr[i].MakeMaskRTs();
				}
			}

			/// <summary>
			/// [Hide시 호출]
			/// 생성된 마스크 RT를 모두 제거한다.
			/// </summary>
			public void RemoveMaskRTs()
			{
				if(_nRenderers_Total == 0)
				{
					return;
				}
            
				for (int i = 0; i < _nRenderers_Total; i++)
				{
					_renderers_Arr[i].ReleaseMaskRTs();
				}
			}

			//카메라 동기화
			//---------------------------------------------------
			/// <summary>
			/// 현재 카메라와 동기화를 한다.
			/// 모든 렌더러에 동기화를 요청한다.
			/// </summary>
			/// <param name="cameraUnits"></param>
			public void SyncCamera(List<apOptMaskRenderCameraUnit> cameraUnits)
			{
				if(_nRenderers_Total == 0)
				{
					return;
				}
            
				for (int i = 0; i < _nRenderers_Total; i++)
				{
					_renderers_Arr[i].SyncCamera(cameraUnits);
				}
			}

			// 카메라에 커맨드 버퍼를 등록
			//---------------------------------------------------
			/// <summary>
			/// 동기화된 카메라에 커맨드 버퍼를 등록한다.
			/// 이 함수를 호출하기 전에는 카메라 동기화를 먼저 수행해야한다.
			/// </summary>
			public void AddCommandBufferToCameras()
			{
				if(_nRenderers_Total == 0)
				{
					return;
				}

				for (int i = 0; i < _nRenderers_Total; i++)
				{
					_renderers_Arr[i].AddCommandBufferToCameras();
				}
			}




			// [ 업데이트 ]
			//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
			// 업데이트에 앞서서, 원본 재질로부터 재질 속성 복사
			//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
			public void PreUpdate_CopyProperties()
			{
				// Parent 업데이트
				if(_nRenderers_Total > 0)
				{
					for (int i = 0; i < _nRenderers_Total; i++)
					{
						_renderers_Arr[i].PreUpdate_CopyProps();
					}
				}
			}

			

			//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
			// 단일+일반 카메라에서의 Parent/Child 업데이트 (Built-In / SRP 각각)
			//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
			public void Update_Basic_BuiltIn()
			{
				
				// Parent 업데이트
				if(_nRenderers_Total > 0)
				{
					for (int i = 0; i < _nRenderers_Total; i++)
					{
						_renderers_Arr[i].Update_Basic_BuiltIn();
					}
				}

				// Child 업데이트
				if(_nReceivers > 0)
				{
					for (int i = 0; i < _nReceivers; i++)
					{
						_receivers_Arr[i].Update_Basic();
					}
				}
			}

#if UNITY_2019_1_OR_NEWER
			/// <summary>
			/// 단일 카메라에서의 마스크 업데이트 (SRP)
			/// </summary>
			public void Update_Basic_SRP(ref ScriptableRenderContext context)
			{
				//Debug.Log("마스크 업데이트 - Basic SRP (Parent : " + _nRenderers_Total + " / Child : " + _nReceivers + ")");
				// Parent 업데이트
				if(_nRenderers_Total > 0)
				{
					for (int i = 0; i < _nRenderers_Total; i++)
					{
						_renderers_Arr[i].Update_Basic_SRP(ref context);
					}

					//갱신된 커맨드 버퍼를 제출한다.
					context.Submit();
				}

				// Child 업데이트
				if(_nReceivers > 0)
				{
					for (int i = 0; i < _nReceivers; i++)
					{
						_receivers_Arr[i].Update_Basic();
					}
				}
            
			}
	#endif

			//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
			// 다중 카메라에서의 Parent/Child 업데이트 (Built-In / SRP 각각)
			//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
			/// <summary>
			/// 다중 카메라에서의 마스크 업데이트 (Build-In)
			/// </summary>
			public void Update_MultiCam_BuiltIn(Camera cam, bool isMaskAreaOptimizable)
			{
				// Parent 업데이트
				if(_nRenderers_Total > 0)
				{
					for (int i = 0; i < _nRenderers_Total; i++)
					{
						_renderers_Arr[i].Update_MultiCam_BuiltIn(cam, isMaskAreaOptimizable);
					}
				}

				// Child 업데이트
				if(_nReceivers > 0)
				{
					for (int i = 0; i < _nReceivers; i++)
					{
						//_receivers_Arr[i].Update_MultipleCam(cam);
						//Receiver는 Multi-Cam 로직이 Basic과 동일하다.
						//Renderer 업데이트 직후에 저장된 RT를 가져다 쓰면 되므로 카메라 구분이 필요 없어서 Basic과 로직이 같다.
						_receivers_Arr[i].Update_Basic();
					}
				}
			}


	#if UNITY_2019_1_OR_NEWER
			/// <summary>
			/// 다중 카메라에서의 마스크 업데이트 (SRP)
			/// </summary>
			public void Update_MultiCam_SRP(ref ScriptableRenderContext context,
											apOptMaskRenderCameraUnit camUnit,
											bool isMaskAreaOptimizable)
			{
				// Parent 업데이트
				if(_nRenderers_Total > 0)
				{
					for (int i = 0; i < _nRenderers_Total; i++)
					{
						_renderers_Arr[i].Update_MultiCam_SRP(ref context, camUnit, isMaskAreaOptimizable);
					}

					//여기서 커맨드 버퍼 일괄 제출
					context.Submit();
				}

				// Child 업데이트
				if(_nReceivers > 0)
				{
					for (int i = 0; i < _nReceivers; i++)
					{
						//_receivers_Arr[i].Update_MultipleCam(camUnit.Camera);
						_receivers_Arr[i].Update_Basic();//Basic 로직 재활용
					}
				}
			}
	#endif


			//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
			// VR 카메라에서의 Parent/Child 업데이트 (Built-In / SRP 각각)
			//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
			public void Update_SingleVR_BuiltIn(Camera cam)
			{
				//Debug.Log("마스크 업데이트 - SingleVR Built-In (Parent : " + _nRenderers_Total + " / Child : " + _nReceivers + ")");

				// Parent 업데이트
				if(_nRenderers_Total > 0)
				{
					for (int i = 0; i < _nRenderers_Total; i++)
					{
						_renderers_Arr[i].Update_SingleVR_BuiltIn();
					}
				}
            
				// Child 업데이트
				if(_nReceivers > 0)
				{
					for (int i = 0; i < _nReceivers; i++)
					{
						_receivers_Arr[i].Update_SingleVR();
					}
				}
			}

	#if UNITY_2019_1_OR_NEWER
			public void Update_SingleVR_SRP(ref ScriptableRenderContext context, apOptMaskRenderCameraUnit camUnit)
			{
				// Parent 업데이트
				if(_nRenderers_Total > 0)
				{
					for (int i = 0; i < _nRenderers_Total; i++)
					{
						_renderers_Arr[i].Update_SingleVR_SRP(ref context);
					}

					//여기서 커맨드 버퍼 일괄 제출
					context.Submit();
				}

				// Child 업데이트
				if(_nReceivers > 0)
				{
					for (int i = 0; i < _nReceivers; i++)
					{
						_receivers_Arr[i].Update_SingleVR();
					}
				}
			}
	#endif
		}

    }
}