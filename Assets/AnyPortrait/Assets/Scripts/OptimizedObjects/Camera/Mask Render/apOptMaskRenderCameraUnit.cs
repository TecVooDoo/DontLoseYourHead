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
    /// apOptMeshRenderCamera의 CameraRenderData에 해당하는 클래스 중 일부
    /// 씬에 존재하는 각 카메라를 나타낸다.
    /// 렌더 이벤트를 등록/해제하는 역할을 수행한다.
    /// OptMesh의 MeshRenderCamera는 카메라 메타 정보 + RT 생성 + 업데이트를 모두 담당했지만
    /// 여기서는 카메라 메타정보(+이벤트 등록)만 담당한다.
    /// (RT 생성은 Renderer내의 CamMetaUnit이, 업데이트는 부모 클래스인 MaskRenderCamera가 담당)
    /// </summary>
    public class apOptMaskRenderCameraUnit
    {
        // Members
        //------------------------------------------------
        private Camera _camera = null;
        private Transform _transform = null;

        //카메라의 기본 텍스쳐는 여기에서 입력하자
        private RenderTexture _camTargetTexture = null;
        

        private apOptMultiCameraController _multiCamController = null;//OnPreRender 이벤트를 받기 위해 생성되는 임시 스크립트
        private object _multiCamLoadKey = null;

		//추가/삭제 체크용
		private bool _isChecked = false;

        //등록된 커맨드 버퍼는 리스트에 넣어서 관리한다.
        //중복 체크와 일괄 삭제를 위해서
        private List<CommandBuffer> _addedCommandBuffers_Phase1 = null;
		private List<CommandBuffer> _addedCommandBuffers_Phase2 = null;
		private List<CommandBuffer> _addedCommandBuffers_Phase3 = null;


		// Init
		//------------------------------------------------------------
        public apOptMaskRenderCameraUnit(Camera camera)
        {
            _camera = camera;
            _transform = camera.transform;
            _camTargetTexture = _camera.targetTexture;

            _multiCamController = null;
            _multiCamLoadKey = null;
            
            _addedCommandBuffers_Phase1 = null;
			_addedCommandBuffers_Phase2 = null;
			_addedCommandBuffers_Phase3 = null;

            _isChecked = false;
        }


        
        
        // 생성된 커맨드 버퍼 등록/해제 (Built-In 전체)
        //------------------------------------------------------------
        //Built-In에서는 카메라 방식에 관계없이 커맨드 버퍼를 카메라에 등록해야한다.
        /// <summary>
        /// [Built-In 전체]
        /// 커맨드 버퍼를 카메라에 등록한다.
        /// </summary>
        /// <param name="cmdBuffer"></param>
        public void AddCommandBufferToCamera(CommandBuffer cmdBuffer, apSendMaskData.RT_RENDER_ORDER renderOrder)
        {
			//렌더 순서에 따라 입력되는 카메라 이벤트가 다르다.
			switch (renderOrder)
			{
				case apSendMaskData.RT_RENDER_ORDER.Phase1:
					{
						if (_addedCommandBuffers_Phase1 == null)
						{
							_addedCommandBuffers_Phase1 = new List<CommandBuffer>();
						}

						//이미 등록되었다면 생략
						if(_addedCommandBuffers_Phase1.Contains(cmdBuffer))
						{
							return;
						}

						//카메라에 입력을 하고
						//Phase1은 가장 빠른 Before Forward Opaque
						_camera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, cmdBuffer);

						//리스트에도 넣자
						_addedCommandBuffers_Phase1.Add(cmdBuffer);
					}
					break;

				case apSendMaskData.RT_RENDER_ORDER.Phase2:
					{
						if (_addedCommandBuffers_Phase2 == null)
						{
							_addedCommandBuffers_Phase2 = new List<CommandBuffer>();
						}

						//이미 등록되었다면 생략
						if(_addedCommandBuffers_Phase2.Contains(cmdBuffer))
						{
							return;
						}

						//카메라에 입력을 하고
						//Phase2은 After Forward Opaque
						_camera.AddCommandBuffer(CameraEvent.AfterForwardOpaque, cmdBuffer);

						//리스트에도 넣자
						_addedCommandBuffers_Phase2.Add(cmdBuffer);
					}
					break;

				case apSendMaskData.RT_RENDER_ORDER.Phase3:
					{
						if (_addedCommandBuffers_Phase3 == null)
						{
							_addedCommandBuffers_Phase3 = new List<CommandBuffer>();
						}

						//이미 등록되었다면 생략
						if(_addedCommandBuffers_Phase3.Contains(cmdBuffer))
						{
							return;
						}

						//카메라에 입력을 하고
						//Phase3는 메인 렌더링 직전인 Befor Forward Alpha
						_camera.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, cmdBuffer);

						//리스트에도 넣자
						_addedCommandBuffers_Phase3.Add(cmdBuffer);
					}
					break;
			}
        }

        /// <summary>
        /// [Built-In 전체]
        /// 등록된 커맨드 버퍼를 모두 해제하자.
        /// </summary>
        /// <param name="cmdBuffer"></param>
        public void RemoveAllCommandBuffers(bool isSRP)
        {
			//페이즈별로 호출한다.
            int nCmdBuffers_Phase1 = _addedCommandBuffers_Phase1 != null ? _addedCommandBuffers_Phase1.Count : 0;
			int nCmdBuffers_Phase2 = _addedCommandBuffers_Phase2 != null ? _addedCommandBuffers_Phase2.Count : 0;
			int nCmdBuffers_Phase3 = _addedCommandBuffers_Phase3 != null ? _addedCommandBuffers_Phase3.Count : 0;

			if(!isSRP)
			{
				if(_camera != null)
				{
					//페이즈별로 삭제 (삭제되는 이벤트가 다르다)
					if(nCmdBuffers_Phase1 > 0)
					{
						for (int i = 0; i < nCmdBuffers_Phase1; i++)
						{
							_camera.RemoveCommandBuffer(CameraEvent.BeforeForwardOpaque, _addedCommandBuffers_Phase1[i]);
						}
					}

					if(nCmdBuffers_Phase2 > 0)
					{
						for (int i = 0; i < nCmdBuffers_Phase2; i++)
						{
							_camera.RemoveCommandBuffer(CameraEvent.AfterForwardOpaque, _addedCommandBuffers_Phase2[i]);
						}
					}

					if(nCmdBuffers_Phase3 > 0)
					{
						for (int i = 0; i < nCmdBuffers_Phase3; i++)
						{
							_camera.RemoveCommandBuffer(CameraEvent.BeforeForwardAlpha, _addedCommandBuffers_Phase3[i]);
						}
					}
					
				}
			}
            
            
            //해당 카메라에 다른 스크립트에 의한 커맨드 버퍼가 있을 수 있으니
            //_camera.RemoveAllCommandBuffers를 호출하면 안된다.
            _addedCommandBuffers_Phase1 = null;
			_addedCommandBuffers_Phase2 = null;
			_addedCommandBuffers_Phase3 = null;
        }



        // 이벤트 등록/해제 (Built-In + Multi Cam)
        //------------------------------------------------------------
        /// <summary>
        /// [ Built-In + 다중 카메라 ]
        /// 다중 카메라의 경우엔 업데이트를 PreRender 이벤트에서 해야하므로, MultiCamController를 이용하여 이벤트를 등록하자
        /// </summary>
        public void AddMultiCamRenderEvent(apOptMultiCameraController.FUNC_MESH_PRE_RENDERED funcMeshPreRendered)
        {
			bool isValidControllerExist = false;
            _multiCamController = _camera.gameObject.GetComponent<apOptMultiCameraController>();
            //Debug.Log("AddMultiCamRenderEvent : " + _camera.gameObject.name + " / " + (_multiCamController != null ? "true" : "false"));

			if(_multiCamController != null)
			{
				//삭제 중이 아닌지도 체크해야한다.
				//간혹 스크립트가 존재하지만 Destroy가 이미 호출되어 삭제 직전의 상태인 경우도 있다.
				//예) 커맨드버퍼+카메라 동기화 리셋할때 ClearEvent 직후에 재연결을 하는 경우

				if(!_multiCamController.IsDestroying())
				{
					//삭제 중이 아닌 컨트롤러가 존재하다 = 유효함
					isValidControllerExist = true;
				}
			}
			
            if(!isValidControllerExist)
            {	
                _multiCamController = _camera.gameObject.AddComponent<apOptMultiCameraController>();
            }

            if(!_multiCamController.IsInit())
            {
                _multiCamController.Init();
            }

            //함수 연결
            _multiCamLoadKey = _multiCamController.AddPreRenderEvent(_multiCamLoadKey, funcMeshPreRendered);
        }

        /// <summary>
        /// [ Built-In + 다중 카메라 ]
        /// 다중 카메라에 등록된 PreRender 이벤트를 삭제한다.
        /// </summary>
        public void RemoveMultiCamRenderEvent()
        {
            if(_multiCamController != null)
            {
                _multiCamController.RemovePreRenderEvent(_multiCamLoadKey);
                _multiCamController = null;
            }
        }





        // 갱신/삭제용 플래그 및 업데이트로 TargetTexture 갱신
        //---------------------------------------------------------
        public void ReadyToCheck()
        {
            _isChecked = false;
        }

        public void SetCheck()
        {
            _isChecked = true;
        }

        public void RefreshTargetTexture()
        {
            _camTargetTexture = _camera.targetTexture;
        }

        public bool IsChecked()
        {
            return _isChecked;
        }


        // Get
        //---------------------------------------------------------
        /// <summary>카메라</summary>
        public Camera Camera { get { return _camera; } }

        /// <summary>카메라의 Transform</summary>
        public Transform CamTransform { get { return _transform; } }

        /// <summary>카메라의 원래 RenderTarget</summary>
        public RenderTexture CamTargetTexture { get { return _camTargetTexture; } }
        
    }
}