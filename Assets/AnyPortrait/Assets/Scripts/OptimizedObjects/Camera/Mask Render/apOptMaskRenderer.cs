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
using System.Collections;
using System.Collections.Generic;
using System;

using AnyPortrait;
using UnityEngine.Rendering;
using System.Text;



namespace AnyPortrait
{
    /// <summary>
    /// Mask Render Camera에 속한 객체로서, SendMaskData를 받아서 렌더링을 실제로 수행한다.
    /// Send Mask Data ~ Camera의 중간에 속한 업데이트 객체이다.
    /// (개념상) Mask RT 세트당 하나의 객체가 생성되기 때문에, Shared 타입은 여러개의 Send Mask Data를 받는다.
    /// Mask Child의 경우 이 객체를 경유하여 필요한 데이터를 받아간다.
    /// 기존 로직의 MeshRenderCamera 중 "업데이트" 부분에 해당한다.
    /// </summary>
    public class apOptMaskRenderer
    {
        // Members
        //------------------------------------        
        // [ 생성 요청에 따른 멤버 변수들 ]
        
        public enum RENDERER_TYPE
        {
            /// <summary>기존의 클리핑 마스크 Parent의 경우엔 이 타입이다. Send Data를 갖지 않고 바로 Parent Mesh를 참조한다.</summary>
            Clipping,
            /// <summary>1개의 메시마다 마스크를 생성하는 타입이다. 1개의 Send Mask Data를 갖는다.</summary>
            PerMesh,
            /// <summary>n개의 메시들이 동일한 마스크에 입력되는 타입이다. n개의 Send Mask Data가 있으며, Sorting하여 렌더링을 해야한다.</summary>
            Shared
        }



        //타입에 따라 다른 연결 정보를 갖는다.
        private RENDERER_TYPE _rendererType = RENDERER_TYPE.Clipping;

		//렌더링 순서에 따라 커맨드 버퍼가 렌더링시 실행되는 순서가 정해진다.
		private apSendMaskData.RT_RENDER_ORDER _renderOrder = apSendMaskData.RT_RENDER_ORDER.Phase1;

		//Shared를 제외한 경우(Clipping / PerMesh)에 사용되는 재질 관련 값들		
		private apSendMaskData.RT_SHADER_TYPE _RTShaderType = apSendMaskData.RT_SHADER_TYPE.AlphaMask;

        //렌더링 Shader는 Mesh로 부터 받자
		private Shader _shader = null;
        private Material _material = null;


        private Request_Clipping _request_Clipping = null;
        private Request_PerMesh _request_PerMesh = null;

        private List<Request_Shared> _requests_Shared = null;
        private int _nRequestsShared = 0;
        private int _sharedID = -1;


        // [ 카메라에 따른 데이터 ]
        private List<CameraRTCmdUnit> _RTUnits = null;
        private Dictionary<apOptMaskRenderCameraUnit, CameraRTCmdUnit> _cam2RTUnit = null;
        private int _nRTUnits = 0;
		private CameraRTCmdUnit _mainRTUnit = null;//1개의 카메라에 의한 RT가 있는 경우

        //갱신 여부
		private bool _isVisible = false;//메시가 숨겨진 상태면 false가 된다. 조건이 충족되지 않아도 false이다.

		//매 업데이트마다 다음의 값이 바뀌며, 같은 루틴 내의 곧 실행될 Child 연산에서 참조한다.
		private apOptMaskRT _lastUpdatedMaskRT = null;
		private Vector4 _lastMaskScreenSpaceOffset = Vector4.zero;

		//체인 여부.
		//이 렌더러를 받는 Receiver가 다른 렌더러와 체인된 상태라면,
		//MaskScreenSpaceOffset을 계산할 때, 화면 비율이 아닌 다음 RT 생성용 1:1 MSSO도 계산해둬야 한다.
		private bool _isChained_AsPrev = false;
		private bool _isChained_AsNext = false;
		//private Vector4 _lastMaskScreenSpaceOffsetForChained = Vector4.zero;


		// 공통된 마스크 생성 정보
		// Portrait에게 받아서 설정한다.
		private apPortrait _portrait;

        private int _textureSize = -1;
        private bool _isDualRT = false;

#if UNITY_2019_1_OR_NEWER
        private bool _isEyeTextureSize = false;
#endif


        // RT 영역 최적화 옵션
        public enum RT_OPTIMIZE_OPTION
        {
            /// <summary>상황에 따라 가능하면 RT 렌더링을 최적화한다.</summary>
            OptimizeWhenPossible,
            /// <summary>RT 렌더링을 최적화하지 않는다.</summary>
            None,
        }
        private RT_OPTIMIZE_OPTION _RTOptimizeOption = RT_OPTIMIZE_OPTION.OptimizeWhenPossible;

        //공통적으로 RootUnit을 갖는다.
        //RootUnit이 활성화되지 않으면 처리가 되지 않기 때문
        private apOptRootUnit _rootUnit = null;


		//마스크를 작성하는 주요 Shader Prop ID
		//+ 프로퍼티 존재 여부도
		private int _propID_MainTex = -1;
		private int _propID_Color = -1;
		private bool _isHasProp_MainTex = false;
		private bool _isHasProp_Color = false;

		private Color _defaultGrayColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);

		//VR용 Shader Property
		private int _propID_unity_StereoEyeIndex = -1;

        //Visible 상태
        //이전에 Show/Hide 상태에 따라서 커맨드 버퍼를 갱신하지 않아도 될 때가 있다. (Hide > Hide 연속인 경우)
        private enum VISIBLE_STATUS
        {
            Unknown, Shown, Hidden
        }

        private VISIBLE_STATUS _visibleStatus = VISIBLE_STATUS.Unknown;




        // Init
        //---------------------------------------------------
        public apOptMaskRenderer(   apPortrait portrait,
                                    apOptRootUnit rootUnit
                                )
        {
            
            _portrait = portrait;
            _rootUnit = rootUnit;

			//기본 크기 옵션
			//텍스쳐 크기는 방식에 따라 다르다.
            _textureSize = 64;//일단 작은 크기

			if(_portrait._vrSupportMode == apPortrait.VR_SUPPORT_MODE.SingleCamera)
			{
				//단일 카메라 VR인 경우 (스테레오 RT)
				_isDualRT = true;
			}
			else
			{
				//그 외는 카메라당 1개의 RT
				_isDualRT = false;
			}

			

#if UNITY_2019_1_OR_NEWER

			_isEyeTextureSize = false;

			if(_portrait._vrRenderTextureSize == apPortrait.VR_RT_SIZE.ByEyeTextureSize)
			{
				//단일 카메라 VR에 명시된 EyeTexture 크기로 RT 크기를 강제한다.
				_isEyeTextureSize = true;
			}
#endif

            _RTOptimizeOption = RT_OPTIMIZE_OPTION.OptimizeWhenPossible;//기본값

            //기본값 초기화
            _rendererType = RENDERER_TYPE.Clipping;
            _shader = null;
            _material = null;

            _request_Clipping = null;
            _request_PerMesh = null;

            _requests_Shared = null;
            _nRequestsShared = 0;
            _sharedID = -1;

            if(_RTUnits == null)
            {
                _RTUnits = new List<CameraRTCmdUnit>();
            }
            _RTUnits.Clear();

            if(_cam2RTUnit == null)
            {
                _cam2RTUnit = new Dictionary<apOptMaskRenderCameraUnit, CameraRTCmdUnit>();
            }
            _cam2RTUnit.Clear();
            _nRTUnits = 0;
			_mainRTUnit = null;

			_isVisible = false;
			_lastUpdatedMaskRT = null;
			_lastMaskScreenSpaceOffset = Vector4.zero;
			_isChained_AsPrev = false;
			_isChained_AsNext = false;

			_propID_MainTex = Shader.PropertyToID("_MainTex");
			_propID_Color = Shader.PropertyToID("_Color");
			_isHasProp_MainTex = false;
			_isHasProp_Color = false;

			_propID_unity_StereoEyeIndex = Shader.PropertyToID("unity_StereoEyeIndex");

            _visibleStatus = VISIBLE_STATUS.Unknown;

			_renderOrder = apSendMaskData.RT_RENDER_ORDER.Phase1;
        }

        /// <summary>
        /// Clipping Mask 타입의 Renderer이다.
        /// </summary>
        /// <param name="parentMaskMesh"></param>
        public void SetClipping(apOptMesh parentMaskMesh)
        {
            _rendererType = RENDERER_TYPE.Clipping;
            _request_Clipping = new Request_Clipping(parentMaskMesh);

            //Clipping에서는 항상 RT 최적화 로직을 켠다.
            _RTOptimizeOption = RT_OPTIMIZE_OPTION.OptimizeWhenPossible;

			//Clipping Mask를 생성하는 것은 항상 가장 빠른 렌더 순서를 가진다.
			_renderOrder = apSendMaskData.RT_RENDER_ORDER.Phase1;

			//텍스쳐 크기는 Mesh의 옵션에 의해 정해진다.
			_textureSize = parentMaskMesh._clippingRenderTextureSize;

			//음수인 경우 자동 크기이다.
			// if(_textureSize < 64)
			// {
			// 	_textureSize = 64;
			// }

			//클리핑은 렌더링 쉐이더가 AlphaMask로 정해져있다.
			_RTShaderType = apSendMaskData.RT_SHADER_TYPE.AlphaMask;
			_shader = parentMaskMesh._shader_AlphaMask;//AlphaMask 쉐이더를 이용한다.
            _material = new Material(_shader);

			//마스크를 작성하기 위한 프로퍼티를 가지고 있는가
			_isHasProp_MainTex = _material.HasProperty(_propID_MainTex);
			_isHasProp_Color = _material.HasProperty(_propID_Color);
        }


        /// <summary>
        /// 메시당 1개씩 RT를 생성하는 SendData 타입의 렌더러이다.
        /// </summary>
        /// <param name="sendMesh"></param>
        /// <param name="sendData"></param>
        public void SetSendData_PerMesh(apOptMesh sendMesh, apOptSendMaskData sendData)
        {
            _rendererType = RENDERER_TYPE.PerMesh;
            _request_PerMesh = new Request_PerMesh(sendMesh, sendData);

			//옵션에 따라 RT 최적화 로직이 켜진다.
			if (sendData._isRTSizeOptimized)
			{
				_RTOptimizeOption = RT_OPTIMIZE_OPTION.OptimizeWhenPossible;       
			}
			else
			{
				_RTOptimizeOption = RT_OPTIMIZE_OPTION.None;
			}

			//Clipping Mask를 생성하는 것은 항상 가장 빠른 렌더 순서를 가진다.
			_renderOrder = sendData._rtRenderOrder;

			//텍스쳐 크기는 Mesh의 옵션에 의해 정해진다.
			_textureSize = 64;
			switch (sendData._renderTextureSize)
			{
				case apTransform_Mesh.RENDER_TEXTURE_SIZE.s_64:		_textureSize = 64; break;
				case apTransform_Mesh.RENDER_TEXTURE_SIZE.s_128:	_textureSize = 128; break;
				case apTransform_Mesh.RENDER_TEXTURE_SIZE.s_256:	_textureSize = 256; break;
				case apTransform_Mesh.RENDER_TEXTURE_SIZE.s_512:	_textureSize = 512; break;
				case apTransform_Mesh.RENDER_TEXTURE_SIZE.s_1024:	_textureSize = 1024; break;
				//음수값 : 자동으로 정해지는 크기
				case apTransform_Mesh.RENDER_TEXTURE_SIZE.FullScreen:	_textureSize = -1; break;
				case apTransform_Mesh.RENDER_TEXTURE_SIZE.HalfScreen:	_textureSize = -2; break;
				case apTransform_Mesh.RENDER_TEXTURE_SIZE.QuarterScreen:	_textureSize = -3; break;
				case apTransform_Mesh.RENDER_TEXTURE_SIZE.MaxFHD:	_textureSize = -4; break;
				case apTransform_Mesh.RENDER_TEXTURE_SIZE.MaxHD:	_textureSize = -5; break;
			}

			//쉐이더 방식
			_RTShaderType = sendData._rtShaderType;

            //Shader 타입에 따라 Shader 에셋 결정
            switch(_RTShaderType)
            {
                case apSendMaskData.RT_SHADER_TYPE.AlphaMask:
                    _shader = sendMesh._shader_AlphaMask;
                    break;

                case apSendMaskData.RT_SHADER_TYPE.MainTextureOnly:
                case apSendMaskData.RT_SHADER_TYPE.MainTextureWithColor:
					{
						//_shader = sendMesh._shaderNormal;
						apOptMaterialInfo matInfo = sendMesh.MaterialInfo;
						if(matInfo != null)
						{
							_shader = matInfo._shader;
						}
						else
						{
							_shader = sendMesh._shaderNormal;//아주 예전 방식 (현재 사용 안함)
						}
						break;
					}

                case apSendMaskData.RT_SHADER_TYPE.CustomShader:
                    _shader = sendData._customRTShaderAsset;
                    break;
            }

			if(_shader == null)
			{
				Debug.LogError("AnyPortrait : No Shader on Send Mask Data [Mesh : " + sendMesh.gameObject.name + " / " + _RTShaderType + "]");
			}
            _material = new Material(_shader);

			//마스크를 작성하기 위한 프로퍼티를 가지고 있는가
			_isHasProp_MainTex = _material.HasProperty(_propID_MainTex);
			_isHasProp_Color = _material.HasProperty(_propID_Color);
        }


        /// <summary>
        /// 메시들이 특정 ID의 RT를 공유하는 SendData 타입의 렌더러이다.
        /// 이 함수 이후 추가를 하자.
        /// </summary>
        /// <param name="sharedID"></param>
        public void SetSendData_Shared(int sharedID)
        {
            _rendererType = RENDERER_TYPE.Shared;
            _sharedID = sharedID;

            if(_requests_Shared == null)
            {
                _requests_Shared = new List<Request_Shared>();
            }
            _requests_Shared.Clear();
            _nRequestsShared = 0;

			//텍스쳐 최적화는 일단 None 부터 시작하고 하나라도 최적화를 하면 최적화가 된다.
			_RTOptimizeOption = RT_OPTIMIZE_OPTION.None;

			//렌더 순서는 가장 느린 타이밍에 시작하여 옵션 중 가장 빠른 값을 사용한다.
			_renderOrder = apSendMaskData.RT_RENDER_ORDER.Phase3;

			//텍스쳐 크기는 작은 값으로 시작하여 최대 값으로 적용한다.
			_textureSize = 64;

			//단일 속성값은 사용되지 않는다.
            _shader = null;
            _material = null;
			_RTShaderType = apSendMaskData.RT_SHADER_TYPE.AlphaMask;
		}


		/// <summary>
		/// Shared에 SendData를 추가한다.
		/// </summary>
		/// <param name="maskMesh"></param>
		/// <param name="sendData"></param>
		public void AddSendData_Shared(apOptMesh maskMesh, apOptSendMaskData sendData, bool isInitData)
		{
			Request_Shared request = new Request_Shared(maskMesh, sendData);

			//Request 별로 Shader와 Material을 만들어야 한다.
			Shader curShader = null;
			switch (sendData._rtShaderType)
			{
				case apSendMaskData.RT_SHADER_TYPE.AlphaMask:
					curShader = maskMesh._shader_AlphaMask;
					break;

				case apSendMaskData.RT_SHADER_TYPE.MainTextureOnly:
				case apSendMaskData.RT_SHADER_TYPE.MainTextureWithColor:
					{
						apOptMaterialInfo matInfo = maskMesh.MaterialInfo;
						if(matInfo != null)
						{
							curShader = matInfo._shader;
						}
						else
						{
							curShader = maskMesh._shaderNormal;//아주 예전 방식 (현재 사용 안함)
						}
					}
					break;

				case apSendMaskData.RT_SHADER_TYPE.CustomShader:
					curShader = sendData._customRTShaderAsset;
					break;
			}

			if (curShader != null)
			{
				Material newMat = new Material(curShader);
				bool isHasProp_MainTex = newMat.HasProperty(_propID_MainTex);
				bool isHasProp_Color = newMat.HasProperty(_propID_Color);
				request.SetMaterial(newMat, isHasProp_MainTex, isHasProp_Color);
			}
			else
			{
				Debug.LogError("AnyPortrait : No Shader corresponding to the RT Shader property. [" + sendData._rtShaderType + "]");
			}

			//리스트에 Request 추가
			_requests_Shared.Add(request);
			_nRequestsShared = _requests_Shared.Count;

			//Shared 데이터를 설정하기 (첫 요청의 값을 적용)
			if(isInitData)
			{
				//[ 초기 설정 ]
				// 대부분 첫 값을 그대로 사용한다.
				if (sendData._isRTSizeOptimized)
				{
					_RTOptimizeOption = RT_OPTIMIZE_OPTION.OptimizeWhenPossible;
				}

				//텍스쳐 크기는 최대값을 적용
				//만약 자동 크기가 하나라도 있다면, 그 중에서 가장 큰 값을 이용한다.
				//자동 크기의 경우 > MaxHD > MaxFHD > QuarterScreen > HalfScreen > FullScreen 순으로 크기가 정해진다.
				int nextTextureSize = 64;
				switch (sendData._renderTextureSize)
				{
					case apTransform_Mesh.RENDER_TEXTURE_SIZE.s_64: nextTextureSize = 64; break;
					case apTransform_Mesh.RENDER_TEXTURE_SIZE.s_128: nextTextureSize = 128; break;
					case apTransform_Mesh.RENDER_TEXTURE_SIZE.s_256: nextTextureSize = 256; break;
					case apTransform_Mesh.RENDER_TEXTURE_SIZE.s_512: nextTextureSize = 512; break;
					case apTransform_Mesh.RENDER_TEXTURE_SIZE.s_1024: nextTextureSize = 1024; break;

					case apTransform_Mesh.RENDER_TEXTURE_SIZE.FullScreen:	nextTextureSize = -1; break;
					case apTransform_Mesh.RENDER_TEXTURE_SIZE.HalfScreen:	nextTextureSize = -2; break;
					case apTransform_Mesh.RENDER_TEXTURE_SIZE.QuarterScreen:	nextTextureSize = -3; break;
					case apTransform_Mesh.RENDER_TEXTURE_SIZE.MaxFHD:	nextTextureSize = -4; break;
					case apTransform_Mesh.RENDER_TEXTURE_SIZE.MaxHD:	nextTextureSize = -5; break;
				}

				_textureSize = nextTextureSize;

				//렌더 순서는 가장 빠른 값 (int형으로 더 작은값)으로 적용
				_renderOrder = sendData._rtRenderOrder;
			}
		}




		// Functions
		//------------------------------------------------------------------
		/// <summary>
		/// 생성해두지 않은 마스크 RT를 생성한다.
		/// </summary>
		public void MakeMaskRTs()
        {
            if(_nRTUnits == 0)
            {
                return;
            }

            CameraRTCmdUnit curRTUnit = null;            
            for (int i = 0; i < _nRTUnits; i++)
            {
                curRTUnit = _RTUnits[i];
                curRTUnit.MakeRT();
            }
        }

        /// <summary>모든 마스크 RT를 해제한다.</summary>
        public void ReleaseMaskRTs()
        {
            if(_nRTUnits == 0)
            {
                return;
            }

            CameraRTCmdUnit curRTUnit = null;            
            for (int i = 0; i < _nRTUnits; i++)
            {
                curRTUnit = _RTUnits[i];
                curRTUnit.ReleaseRT();
            }
        }


        /// <summary>
        /// 현재 씬의 카메라에 대해서 동기화를 하자.
        /// 이 단계에서는 카메라 객체만 만든다.
        /// </summary>
        /// <param name="cameraUnits"></param>
        public void SyncCamera(List<apOptMaskRenderCameraUnit> srcCameraUnits)
        {
            if(_RTUnits == null)
            {
                _RTUnits = new List<CameraRTCmdUnit>();
            }
            if(_cam2RTUnit == null)
            {
                _cam2RTUnit = new Dictionary<apOptMaskRenderCameraUnit, CameraRTCmdUnit>();
            }

            CameraRTCmdUnit curRTUnit = null;
            if(_nRTUnits > 0)
            {
                for (int i = 0; i < _nRTUnits; i++)
                {
                    curRTUnit = _RTUnits[i];
                    curRTUnit.ReadyToCheck();//생성/삭제를 위한 플래그 초기화
                }
            }
            
            bool isAnyAddedOrRemoved = false;
            //입력된 카메라에 해당하는 RT 유닛이 있는지 체크한다.
            int nSrc = srcCameraUnits != null ? srcCameraUnits.Count : 0;
            if(nSrc > 0)
            {
                apOptMaskRenderCameraUnit srcUnit = null;
                for (int i = 0; i < nSrc; i++)
                {
                    srcUnit = srcCameraUnits[i];

                    curRTUnit = null;
                    _cam2RTUnit.TryGetValue(srcUnit, out curRTUnit);

                    if(curRTUnit != null)
                    {
                        //이미 이 카메라에 대해 등록되어 있다면 유효하다고 체크
                        curRTUnit.SetCheck();
                    }
                    else
                    {
                        //이 카메라에 대한 RT 유닛이 생성되어 있지 않았다.
                        //새로 생성하자
                        CameraRTCmdUnit newUnit = new CameraRTCmdUnit(  this,
                                                                        srcUnit,
                                                                        MakeCommandBufferName()//커맨드 버퍼 이름
                                                                        );

                        newUnit.SetCheck();//삭제되지 않도록 체크

                        _RTUnits.Add(newUnit);
                        _cam2RTUnit.Add(srcUnit, newUnit);//중복 방지용
                        isAnyAddedOrRemoved = true;//추가된게 있다.
                    }
                }
            }

            //이제 삭제할게 있는지 체크
            _nRTUnits = _RTUnits.Count;

            List<CameraRTCmdUnit> removeUnits = null;

            if(_nRTUnits > 0)
            {
                for (int i = 0; i < _nRTUnits; i++)
                {
                    curRTUnit = _RTUnits[i];
                    if(!curRTUnit.IsChecked())
                    {
                        //체크된게 아니라면
                        if(removeUnits == null)
                        {
                            removeUnits = new List<CameraRTCmdUnit>();
                        }
                        removeUnits.Add(curRTUnit);
                        isAnyAddedOrRemoved = true;//삭제된게 있다.
                    }
                }
            }

            //동기화되지 않은 유닛들을 삭제한다.
            int nRemoved = removeUnits != null ? removeUnits.Count : 0;
            if(nRemoved > 0)
            {
                CameraRTCmdUnit removeUnit = null;
                for (int i = 0; i < nRemoved; i++)
                {
                    removeUnit = removeUnits[i];

                    //RT를 먼저 삭제한다.
                    removeUnit.ReleaseRT();
                    _RTUnits.Remove(removeUnit);
                }
            }

            _nRTUnits = _RTUnits.Count;

            //추가, 삭제된게 있다면 Dictionary 다시 갱신
            if(isAnyAddedOrRemoved)
            {
                _cam2RTUnit.Clear();

                if(_nRTUnits > 0)
                {
                    for (int i = 0; i < _nRTUnits; i++)
                    {
                        curRTUnit = _RTUnits[i];
                        _cam2RTUnit.Add(curRTUnit.LinkedCameraUnit, curRTUnit);
                    }
                }
            }

			_mainRTUnit = null;
			if(_nRTUnits > 0)
			{
				_mainRTUnit = _RTUnits[0];
			}
        }

        private string MakeCommandBufferName()
        {
            StringBuilder strBuilder = new StringBuilder(50);
            //strBuilder.Clear();
            strBuilder.Append("[AP] ");
            strBuilder.Append(_rootUnit.gameObject.name);
            strBuilder.Append(" ");
            switch(_rendererType)
            {
                case RENDERER_TYPE.Clipping:
                    {
                        if(_request_Clipping != null
                            && _request_Clipping.Mesh != null)
                        {
                            strBuilder.Append(_request_Clipping.Mesh.gameObject.name);
                        }
                        strBuilder.Append(" Clip");
                    }
                    break;

                case RENDERER_TYPE.PerMesh:
                    {
                        if(_request_PerMesh != null
                            && _request_PerMesh.Mesh != null)
                        {
                            strBuilder.Append(_request_PerMesh.Mesh.gameObject.name);
                        }
                        strBuilder.Append(" SendData");
                    }
                    break;

                case RENDERER_TYPE.Shared:
                    {
                        strBuilder.Append(_sharedID);
                        strBuilder.Append(" SendData (Shared)");
                    }
                    break;
            }

            return strBuilder.ToString();
        }



		/// <summary>
		/// 커맨드 버퍼를 연결된 카메라에 등록한다.
		/// </summary>
		public void AddCommandBufferToCameras()
		{
			if(_nRTUnits == 0)
			{
				return;
			}

#if UNITY_EDITOR
			if(!Application.isPlaying)
			{
				//Debug.Log("게임 실행 중이 아니면 커맨드 버퍼 생성 안함");
				return;
			}
#endif

			CameraRTCmdUnit curRTUnit = null;
			for (int i = 0; i < _nRTUnits; i++)
			{
				curRTUnit = _RTUnits[i];

				//생성된 커맨드 버퍼를 연결된 카메라에 등록한다.
				//렌더링 순서를 지정해야한다.
				curRTUnit.AddCommandToLinkedCamera(_renderOrder);
			}
		}






		//------------------------------------------------------------------
		// 업데이트 직전 로직
		//------------------------------------------------------------------
		public void PreUpdate_CopyProps()
		{
			//렌더러 타입별로 업데이트 처리
			//클리핑은 처리안함
			switch (_rendererType)
            {
                case RENDERER_TYPE.Clipping:
                    return;

                case RENDERER_TYPE.PerMesh:
					PreUpdate_CopyProps_PerMesh();
					break;

                case RENDERER_TYPE.Shared:
					PreUpdate_CopyProps_Shared();
					break;
            }
		}

		private void PreUpdate_CopyProps_PerMesh()
		{
			if(_request_PerMesh == null || _material == null)
			{
				return;
			}

			//복사 정보를 체크하자
			apOptSendMaskData sendData = _request_PerMesh.SendData;

			int nCopyProps = sendData._copiedProperties != null ? sendData._copiedProperties.Count : 0;
			if(nCopyProps == 0)
			{
				return;
			}

			apOptMesh mesh = _request_PerMesh.Mesh;

			if(!mesh.IsVisible())
			{
				//보여지지 않는 경우는 생략
				return;
			}

			//값을 받아와서 복사해서 넣는다.
			apOptSendMaskData.CopiedPropertyInfo propInfo = null;
			for (int i = 0; i < nCopyProps; i++)
			{
				propInfo = sendData._copiedProperties[i];

				switch (propInfo._propType)
				{
					case apSendMaskData.SHADER_PROP_REAL_TYPE.Texture:
						mesh.CopyPropertyToTarget_Texture(_material, propInfo._propID);
						break;

					case apSendMaskData.SHADER_PROP_REAL_TYPE.Float:
						mesh.CopyPropertyToTarget_Float(_material, propInfo._propID);
						break;

					case apSendMaskData.SHADER_PROP_REAL_TYPE.Int:
						mesh.CopyPropertyToTarget_Int(_material, propInfo._propID);
						break;

					case apSendMaskData.SHADER_PROP_REAL_TYPE.Vector:
						mesh.CopyPropertyToTarget_Vector4(_material, propInfo._propID);
						break;

					case apSendMaskData.SHADER_PROP_REAL_TYPE.Color:
						mesh.CopyPropertyToTarget_Color(_material, propInfo._propID);
						break;
				}
			}
		}

		private void PreUpdate_CopyProps_Shared()
		{
			if(_nRequestsShared == 0)
			{
				return;
			}

			Request_Shared curReq = null;
			apOptMesh curMesh = null;
			apOptSendMaskData curSendData = null;
			Material curMat = null;

			apOptSendMaskData.CopiedPropertyInfo propInfo = null;

			for (int iReq = 0; iReq < _nRequestsShared; iReq++)
			{
				curReq = _requests_Shared[iReq];

				if(curReq == null)
				{
					continue;
				}
				curMesh = curReq.Mesh;
				curMat = curReq.Material;
				curSendData = curReq.SendData;

				if(curMesh == null
					|| curMat == null
					|| curSendData == null
					|| !curMesh.IsVisible())
				{
					continue;
				}
				
				int nCopyProps = curSendData._copiedProperties != null ? curSendData._copiedProperties.Count : 0;
				if(nCopyProps == 0)
				{
					continue;
				}

				//마스크 렌더링 전 지정된 프로퍼티 값들을 복사해오자
				for (int iProp = 0; iProp < nCopyProps; iProp++)
				{
					propInfo = curSendData._copiedProperties[iProp];

					switch(propInfo._propType)
					{
						case apSendMaskData.SHADER_PROP_REAL_TYPE.Texture:
						curMesh.CopyPropertyToTarget_Texture(curMat, propInfo._propID);
						break;

					case apSendMaskData.SHADER_PROP_REAL_TYPE.Float:
						curMesh.CopyPropertyToTarget_Float(curMat, propInfo._propID);
						break;

					case apSendMaskData.SHADER_PROP_REAL_TYPE.Int:
						curMesh.CopyPropertyToTarget_Int(curMat, propInfo._propID);
						break;

					case apSendMaskData.SHADER_PROP_REAL_TYPE.Vector:
						curMesh.CopyPropertyToTarget_Vector4(curMat, propInfo._propID);
						break;

					case apSendMaskData.SHADER_PROP_REAL_TYPE.Color:
						curMesh.CopyPropertyToTarget_Color(curMat, propInfo._propID);
						break;
					}
				}
			}
		}


		//------------------------------------------------------------------
		// 중요 : 업데이트 로직
		// 이 코드는 OptMesh의 CommandBuffer 갱신 로직을 이전했다.
		//------------------------------------------------------------------

		//코드 구성
		//- 마스크 업데이트 시작 : 카메라+RP에 따른 함수 호출. 렌더러 타입에 따른 분기를 가지고 있다.
		//- 커맨드 버퍼 업데이트 : "마스크 업데이트 시작"에서 호출된다. 렌더러 타입 + 카메라에 따라 결정된다.

		//------------------------------------------------------------------
		// 단일 카메라에서의 마스크 업데이트 시작 (Built-In / SRP)
		//------------------------------------------------------------------
		/// <summary>
		/// 단일 카메라에서의 마스크 업데이트 (BuiltIn)
		/// </summary>
		public void Update_Basic_BuiltIn()
		{
			//시작시엔 false
			_isVisible = false;
			_lastUpdatedMaskRT = null;

            //여기서는 카메라 체크 안하고 메인 RT (메인 카메라) 한개만 이용한다.
			
            if(_mainRTUnit == null
            || _mainRTUnit.LinkedCameraUnit == null)
			{
				return;
			}

			Camera linkedCam = _mainRTUnit.LinkedCameraUnit.Camera;
			if(linkedCam == null)
			{
				return;
			}


			//영역 최적화 여부는 여기서 체크하자.
			//커맨드 버퍼 타입에 관계없이 공통이며, 카메라 상태/RP 상태에 따라 다르므로
			bool isOptimizeArea = false;
			if (_RTOptimizeOption == RT_OPTIMIZE_OPTION.OptimizeWhenPossible)
			{
				//옵션이 켜져 있어야 한다.
				if(_nRTUnits == 1)
				{
					//<조건 1> RT가 1개인 경우
					if(_portrait._billboardType != apPortrait.BILLBOARD_TYPE.None)
					{
						//<조건 2-1> 빌보드가 켜지면 카메라를 똑바로 바라보기 때문에 가능하다. (단 SRP에선 불가)
                        isOptimizeArea = true;
					}
					else if(linkedCam.orthographic)
					{
						//<조건 2-2> 빌보드가 아니더라도 Orthographic 타입의 카메라면 영역 최적화가 가능하다.
						isOptimizeArea = true;
					}
				}
			}

			//만약 체인된 상태라면 영역 최적화를 할 수 없다.
			if(_isChained_AsPrev || _isChained_AsNext)
			{
				isOptimizeArea = false;
			}

			//렌더러 타입별로 업데이트 처리
			switch (_rendererType)
            {
                case RENDERER_TYPE.Clipping:
                    UpdateCommandBuffer_Basic_Clipping(_mainRTUnit, isOptimizeArea);
                    break;

                case RENDERER_TYPE.PerMesh:
					UpdateCommandBuffer_Basic_PerMesh(_mainRTUnit, isOptimizeArea);
					break;

                case RENDERER_TYPE.Shared:
					UpdateCommandBuffer_Basic_Shared(_mainRTUnit, isOptimizeArea);
					break;
            }
		}


#if UNITY_2019_1_OR_NEWER
		/// <summary>
		/// 단일 카메라에서의 마스크 업데이트 (SRP)
		/// </summary>
		public void Update_Basic_SRP(ref ScriptableRenderContext context)
		{
			//시작시엔 false
			_isVisible = false;
			_lastUpdatedMaskRT = null;

			//여기서는 카메라 체크 안하고 메인 RT (메인 카메라) 한개만 이용한다.
            if(_mainRTUnit == null
            || _mainRTUnit.LinkedCameraUnit == null)
			{
				return;
			}

			Camera linkedCam = _mainRTUnit.LinkedCameraUnit.Camera;
			if(linkedCam == null)
			{
				return;
			}

			//영역 최적화 여부는 여기서 체크하자.
			//커맨드 버퍼 타입에 관계없이 공통이며, 카메라 상태/RP 상태에 따라 다르므로
			bool isOptimizeArea = false;
			if (_RTOptimizeOption == RT_OPTIMIZE_OPTION.OptimizeWhenPossible)
			{
				//옵션이 켜져 있어야 한다.
				if(_nRTUnits == 1)
				{
					//<조건 1> RT가 1개인 경우
					if(_portrait._billboardType == apPortrait.BILLBOARD_TYPE.None
						&& linkedCam.orthographic)
					{
						//<조건 2> 빌보드가 아니고, Orthographic 타입의 카메라면 영역 최적화가 가능하다.
                        isOptimizeArea = true;
					}
				}
			}

			//만약 체인된 상태라면 영역 최적화를 할 수 없다.
			if(_isChained_AsPrev || _isChained_AsNext)
			{
				isOptimizeArea = false;
			}
			
            switch(_rendererType)
            {
                case RENDERER_TYPE.Clipping:
                    UpdateCommandBuffer_Basic_Clipping(_mainRTUnit, isOptimizeArea);
                    break;

                case RENDERER_TYPE.PerMesh:
					UpdateCommandBuffer_Basic_PerMesh(_mainRTUnit, isOptimizeArea);
					break;

                case RENDERER_TYPE.Shared:
					UpdateCommandBuffer_Basic_Shared(_mainRTUnit, isOptimizeArea);
					break;
            }

			//SRP의 경우는 context에서 실행 요청을 해야한다.
			context.ExecuteCommandBuffer(_mainRTUnit.CommandBuffer);
			//context.Submit();//밖에서 일괄 제출
		}
#endif
		


		//------------------------------------------------------------------
		// 다중 카메라에서의 마스크 업데이트 시작 (Built-In / SRP)
		//------------------------------------------------------------------
		/// <summary>
		/// 다중 카메라에서의 마스크 업데이트 (Built-In)
		/// </summary>
		public void Update_MultiCam_BuiltIn(Camera cam, bool isMaskAreaOptimizable)
		{
			//시작시엔 false
			_isVisible = false;
			_lastUpdatedMaskRT = null;
			
			//모든 카메라를 체크한다.
			if(_nRTUnits == 0)
			{
				return;
			}

			//영역 최적화 여부 체크
			//다중 카메라의 경우는 외부에서 다중 카메라의 성격 (모두 같은 방향 바라보는지 등)을 체크해야 하므로
			//그 결과값인 isMaskAreaOptimizable을 받아서 활용해야한다.
			bool isOptimizeArea = false;
			if (_RTOptimizeOption == RT_OPTIMIZE_OPTION.OptimizeWhenPossible
				&& isMaskAreaOptimizable)
			{
				isOptimizeArea = true;
			}

			//만약 체인된 상태라면 영역 최적화를 할 수 없다.
			if(_isChained_AsPrev || _isChained_AsNext)
			{
				isOptimizeArea = false;
			}

			CameraRTCmdUnit curRTUnit = null;
			Camera linkedCam = null;
			for (int iRT = 0; iRT < _nRTUnits; iRT++)
			{
				curRTUnit = _RTUnits[iRT];
				if(curRTUnit == null
					|| curRTUnit.LinkedCameraUnit == null)
				{
					continue;
				}

				linkedCam = curRTUnit.LinkedCameraUnit.Camera;
				if(linkedCam == null || linkedCam != cam)
				{
					//이 카메라가 아니다.
					continue;
				}

				//Debug.Log("> Parent Render : " + _rendererType + " / " + curRTUnit.LinkedCameraUnit.Camera.gameObject.name);
				
				//렌더러 타입별로 커맨드 버퍼 업데이트
				//Basic 방식을 재활용할 수 있다.
				switch(_rendererType)
				{
					case RENDERER_TYPE.Clipping:
						UpdateCommandBuffer_Basic_Clipping(curRTUnit, isOptimizeArea);
						break;

					case RENDERER_TYPE.PerMesh:
						UpdateCommandBuffer_Basic_PerMesh(curRTUnit, isOptimizeArea);
						break;

					case RENDERER_TYPE.Shared:
						UpdateCommandBuffer_Basic_Shared(curRTUnit, isOptimizeArea);
						break;
				}

			}
		}


#if UNITY_2019_1_OR_NEWER
        /// <summary>
		/// 다중 카메라에서의 마스크 업데이트 (SRP)
		/// </summary>
		public void Update_MultiCam_SRP(	ref ScriptableRenderContext context,
											apOptMaskRenderCameraUnit camUnit,
											bool isMaskAreaOptimizable)
		{
			//시작시엔 false
			_isVisible = false;
			_lastUpdatedMaskRT = null;

			//모든 카메라를 체크한다.
			if(_nRTUnits == 0 || camUnit == null)
			{
				return;
			}

			//대상이 되는 카메라만 렌더링을 한다.
			CameraRTCmdUnit curRTUnit = null;
			_cam2RTUnit.TryGetValue(camUnit, out curRTUnit);

			if(curRTUnit == null
				|| curRTUnit.LinkedCameraUnit == null)
			{
				return;
			}

			Camera linkedCam = curRTUnit.LinkedCameraUnit.Camera;

			if(linkedCam == null)
			{
				return;
			}

			//영역 최적화 여부 체크
			//다중 카메라의 경우는 외부에서 다중 카메라의 성격 (모두 같은 방향 바라보는지 등)을 체크해야 하므로
			//그 결과값인 isMaskAreaOptimizable을 받아서 활용해야한다.
			bool isOptimizeArea = false;
			if (_RTOptimizeOption == RT_OPTIMIZE_OPTION.OptimizeWhenPossible
				&& isMaskAreaOptimizable)
			{
				isOptimizeArea = true;
			}

			//만약 체인된 상태라면 영역 최적화를 할 수 없다.
			if(_isChained_AsPrev || _isChained_AsNext)
			{
				isOptimizeArea = false;
			}

			//렌더러 타입별로 커맨드 버퍼 업데이트
			//Basic 방식을 재활용할 수 있다.
			switch(_rendererType)
			{
				case RENDERER_TYPE.Clipping:
					UpdateCommandBuffer_Basic_Clipping(curRTUnit, isOptimizeArea);
					break;

				case RENDERER_TYPE.PerMesh:
					UpdateCommandBuffer_Basic_PerMesh(curRTUnit, isOptimizeArea);
					break;

				case RENDERER_TYPE.Shared:
					UpdateCommandBuffer_Basic_Shared(curRTUnit, isOptimizeArea);
					break;
			}

			//SRP의 경우는 context에서 실행 요청을 해야한다.
			context.ExecuteCommandBuffer(curRTUnit.CommandBuffer);
			//context.Submit();
		}
#endif

		//------------------------------------------------------------------
		// Single VR 카메라에서의 마스크 업데이트 시작 (Built-In / SRP)
		//------------------------------------------------------------------
        /// <summary>
        /// VR 카메라에서의 마스크 업데이트 (Built-In)
        /// </summary>
        /// <param name="cam"></param>
		public void Update_SingleVR_BuiltIn()
		{
			//시작시엔 false
			_isVisible = false;
			_lastUpdatedMaskRT = null;

			//여기서는 카메라 체크 안하고 메인 RT (메인 카메라) 한개만 이용한다.
            if(_mainRTUnit == null
            || _mainRTUnit.LinkedCameraUnit == null
            || _mainRTUnit.LinkedCameraUnit.Camera == null)
			{
				return;
			}

			//Single VR은 최적화 영역이 없다.

			//렌더러 타입별로 업데이트 처리
            switch(_rendererType)
            {
                case RENDERER_TYPE.Clipping:
                    UpdateCommandBuffer_SingleVR_Clipping(_mainRTUnit);
                    break;

                case RENDERER_TYPE.PerMesh:
					UpdateCommandBuffer_SingleVR_PerMesh(_mainRTUnit);
					break;

                case RENDERER_TYPE.Shared:
					UpdateCommandBuffer_SingleVR_Shared(_mainRTUnit);
					break;
            }
		}

#if UNITY_2019_1_OR_NEWER
        /// <summary>
        /// VR 카메라에서의 마스크 업데이트 (SRP)
        /// </summary>
        /// <param name="cam"></param>
		public void Update_SingleVR_SRP(ref ScriptableRenderContext context)
		{
			//시작시엔 false
			_isVisible = false;
			_lastUpdatedMaskRT = null;

			//여기서는 카메라 체크 안하고 메인 RT (메인 카메라) 한개만 이용한다.
            if(_mainRTUnit == null
            || _mainRTUnit.LinkedCameraUnit == null
            || _mainRTUnit.LinkedCameraUnit.Camera == null)
			{
				return;
			}

			//Single VR은 최적화 영역이 없다.

			//렌더러 타입별로 업데이트 처리
            switch(_rendererType)
            {
                case RENDERER_TYPE.Clipping:
                    UpdateCommandBuffer_SingleVR_Clipping(_mainRTUnit);
                    break;

                case RENDERER_TYPE.PerMesh:
					UpdateCommandBuffer_SingleVR_PerMesh(_mainRTUnit);
					break;

                case RENDERER_TYPE.Shared:
					UpdateCommandBuffer_SingleVR_Shared(_mainRTUnit);
					break;
            }

			//SRP의 경우는 context에서 실행 요청을 해야한다.
			context.ExecuteCommandBuffer(_mainRTUnit.CommandBuffer);
			//context.Submit();
		}
#endif

		//------------------------------------------------------------------------
		// 커맨드 버퍼 업데이트
		//------------------------------------------------------------------------

		//------------------------------------------------------------------------
		// 단일 카메라/다중 카메라에서의 커맨드 버퍼 업데이트 (렌더러 타입별)
		//------------------------------------------------------------------------
		
		/// <summary>
		/// [커맨드 버퍼 업데이트]
		/// Basic 방식 + Clipping Request
		/// </summary>
		/// <param name="RTUnit"></param>
		private void UpdateCommandBuffer_Basic_Clipping(CameraRTCmdUnit RTUnit, bool isOptimizeArea)
        {			
            if(_request_Clipping == null || _material == null)
            {
                return;
            }

            if(RTUnit == null || RTUnit.LinkedCameraUnit == null)
            {
                return;
            }
			
            Camera mainCam = RTUnit.LinkedCameraUnit.Camera;
			Transform mainCamTransform = RTUnit.LinkedCameraUnit.CamTransform;
            CommandBuffer cmdBuffer = RTUnit.CommandBuffer;
			apOptMaskRT maskRT = RTUnit.MaskRT;

            apOptMesh mesh = _request_Clipping.Mesh;
            Transform meshTransform = _request_Clipping.Mesh._transform;

            if(mainCam == null)
            {
                return;
            }

			if(!maskRT.IsCreated())
			{
				//마스크가 생성되지 않았다.
				return;
			}


			//"마지막으로 업데이트된 마스크 RT"를 저장하자
			_lastUpdatedMaskRT = maskRT;

			if(!mesh.IsVisible())
			{
				//메시가 보여지지 않는 상태라면 마스크를 클리어만 한다.
                if(_visibleStatus != VISIBLE_STATUS.Hidden)
                {
                    //이전에도 Hidden 상태였다면 Skip
                    cmdBuffer.Clear();
                    cmdBuffer.SetRenderTarget(maskRT.RenderTargetID, 0);
                    cmdBuffer.ClearRenderTarget(true, true, Color.clear);

#if UNITY_2019_1_OR_NEWER
					cmdBuffer.SetViewMatrix(mainCam.worldToCameraMatrix);
					cmdBuffer.SetProjectionMatrix(mainCam.projectionMatrix);
#endif
                    _visibleStatus = VISIBLE_STATUS.Hidden;
                }

				return;
			}


			

            //렌더링을 위한 설정값 받아오기
			Texture curTexture = null;
            Color curMeshColor = Color.black;
            float vertRange_XMin = 0.0f;
            float vertRange_XMax = 0.0f;
			float vertRange_YMin = 0.0f;
			float vertRange_YMax = 0.0f;
			Vector3 vertPosCenter = Vector3.zero;

			//복사부터 하자
			//mesh.CopyMaterialToTarget(_material);

            mesh.GetMaskRenderInfo( out curTexture,
                                    out curMeshColor,
                                    out vertRange_XMin,
                                    out vertRange_XMax,
                                    out vertRange_YMin,
                                    out vertRange_YMax,
                                    out vertPosCenter);

            //재질 설정 (Clipping에서는 MainTex와 Color를 지정한다.)
			if(_isHasProp_MainTex)
			{
				_material.SetTexture(_propID_MainTex, curTexture);
			}
            if(_isHasProp_Color)
			{
				_material.SetColor(_propID_Color, curMeshColor);
			}

            if(isOptimizeArea)
            {
				// [ 영역 최적화 ]

                Vector3 vertPosL_LT = new Vector3(vertRange_XMin, vertRange_YMax, 0.0f);
                Vector3 vertPosL_RB = new Vector3(vertRange_XMax, vertRange_YMin, 0.0f);
                
                Vector3 vertPosW_LT = meshTransform.TransformPoint(vertPosL_LT);
                Vector3 vertPosW_RB = meshTransform.TransformPoint(vertPosL_RB);
                Vector3 vertPosW_Center = meshTransform.TransformPoint(vertPosCenter);

                //현재 카메라에 대해서만 업데이트
                //<< 이게 의미가 없는 것 같은데??
                // if(mainCam.orthographic)
                // {
                //     vertPosCenter.z = 0.0f;                    
                // }

                Vector3 vertPosS_Center = mainCam.WorldToScreenPoint(vertPosW_Center);
                Vector3 vertPosS_LT = mainCam.WorldToScreenPoint(vertPosW_LT);
                Vector3 vertPosS_RB = mainCam.WorldToScreenPoint(vertPosW_RB);

                float screenWidth = Mathf.Max((float)Screen.width * mainCam.rect.width, 0.001f);
                float screenHeight = Mathf.Max((float)Screen.height * mainCam.rect.height, 0.001f);
                Vector3 screenCenterOffset = new Vector3((float)Screen.width * mainCam.rect.x, (float)Screen.height * mainCam.rect.y, 0.0f);

                //해당 카메라가 TargetTexture를 갖고 있다면
                if(mainCam.targetTexture != null)
                {
                    screenWidth = mainCam.targetTexture.width;
                    screenHeight = mainCam.targetTexture.height;
                    screenCenterOffset.x = 0.0f;
                    screenCenterOffset.y = 0.0f;

                    //주의 : 카메라의 rect (Viewport Rect)가 기본값인 (0, 0, 1, 1)이 아닌 경우
                    //크기와 화면 비율에 따라서 값이 바뀌지만, 이것까지 제어하진 말자
                    //-ㅅ- =3
                }

				#region [미사용 코드] 기본 수식이며 함수로 래핑
				//            if (!mainCam.orthographic)
				//            {
				//                Vector3 centerSceenPos = vertPosS_LT * 0.5f + vertPosS_RB * 0.5f;

				//                //영역 대각선의 길이의 절반
				//                float distLT2RB_Half = 0.5f * Mathf.Sqrt(
				//                    (vertPosS_LT.x - vertPosS_RB.x) * (vertPosS_LT.x - vertPosS_RB.x)
				//                    + (vertPosS_LT.y - vertPosS_RB.y) * (vertPosS_LT.y - vertPosS_RB.y));
				//                distLT2RB_Half *= 1.6f;

				//                vertPosS_LT.x = centerSceenPos.x - distLT2RB_Half;
				//                vertPosS_LT.y = centerSceenPos.y - distLT2RB_Half;

				//                vertPosS_RB.x = centerSceenPos.x + distLT2RB_Half;
				//                vertPosS_RB.y = centerSceenPos.y + distLT2RB_Half;
				//            }

				//            //Zoom 계산
				//            //모든 버텍스가 화면안에 들어온다면 Sceen 좌표계 Scale이 0~1의 값을 가진다.
				//            float prevSizeWidth = Mathf.Max(Mathf.Abs(vertPosS_LT.x - vertPosS_RB.x) / screenWidth, 0.001f);
				//            float prevSizeHeight = Mathf.Max(Mathf.Abs(vertPosS_LT.y - vertPosS_RB.y) / screenHeight, 0.001f);

				//            //화면에 가득 찰 수 있도록 확대하는 비율은 W, H 중에서 "덜 확대하는 비율 (Min 함수)"로 진행한다.
				//            float zoomScale = Mathf.Min(1.0f / prevSizeWidth, 1.0f / prevSizeHeight);

				//            //메시 자체를 평행이동하여 화면 중앙에 위치시켜야 한다.
				//            float aspectRatio = screenWidth / screenHeight;

				////변형된 Ortho 크기
				//float newOrthoSizeY = 0.0f;
				//            if(mainCam.orthographic)
				//            {
				//	newOrthoSizeY = mainCam.orthographicSize / zoomScale;
				//            }
				//else
				//{
				//	//메시의 중심까지의 거리
				//	float zDepth = Mathf.Abs(mainCam.worldToCameraMatrix.MultiplyPoint3x4(vertPosW_Center).z);
				//	newOrthoSizeY = zDepth * Mathf.Tan(mainCam.fieldOfView * 0.5f * Mathf.Deg2Rad) / zoomScale;
				//}
				//float newOrthoSizeX = aspectRatio * newOrthoSizeY;

				////다음 카메라 위치는 메시의 Center로부터 카메라가 바라보는 방향의 "역방향"으로 Ray를 쏠 때,
				////Ray*Dist 만큼의 위치
				//float distCenterToCamera = Vector3.Distance(vertPosW_Center, mainCamTransform.position);
				//Vector3 nextCameraPos = vertPosW_Center - (mainCamTransform.forward * distCenterToCamera);

				//            Matrix4x4 customWorldToCam = Matrix4x4.TRS(nextCameraPos, mainCamTransform.rotation, Vector3.one).inverse;
				//customWorldToCam.m20 *= -1f;
				//customWorldToCam.m21 *= -1f;
				//customWorldToCam.m22 *= -1f;
				//customWorldToCam.m23 *= -1f;

				//Matrix4x4 customCullingMatrix = Matrix4x4.Ortho(	-newOrthoSizeX,		//Left
				//													newOrthoSizeX,		//Right
				//													-newOrthoSizeY,		//Bottom
				//													newOrthoSizeY,		//Top
				//													distCenterToCamera - 10,	//Near
				//													distCenterToCamera + 50		//Far
				//												)
				//								* customWorldToCam;

				////오브젝트의 World Matrix를 다시 계산하자
				////다중 메시 연산시 이 부분만 건들면 될 듯
				//Matrix4x4 newLocalToProj = customCullingMatrix * meshTransform.localToWorldMatrix;//Local > Proj를 만들고
				//Matrix4x4 newLocalToWorld = mainCam.cullingMatrix.inverse * newLocalToProj;//Proj에 Culling-inv를 곱하여 World 역계산 
				#endregion

				Matrix4x4 resultLocalToWorld = Matrix4x4.identity;

				CalculateMaskScreenSpaceOffset(	mainCam, mainCamTransform,
												meshTransform,
												vertPosW_Center,
												vertPosS_LT, vertPosS_RB, vertPosS_Center,
												screenWidth, screenHeight, screenCenterOffset,
												out resultLocalToWorld,
												out _lastMaskScreenSpaceOffset);

				//커맨드 버퍼 작성
				cmdBuffer.Clear();
				cmdBuffer.SetRenderTarget(maskRT.RenderTargetID, 0);
				cmdBuffer.ClearRenderTarget(true, true, Color.clear);

#if UNITY_2019_1_OR_NEWER
				cmdBuffer.SetViewMatrix(mainCam.worldToCameraMatrix);
				cmdBuffer.SetProjectionMatrix(mainCam.projectionMatrix);
#endif
				//메시를 그린다.
				//cmdBuffer.DrawMesh(mesh._mesh, newLocalToWorld, _material, 0, 0);//Pass 0 강제 사용
				cmdBuffer.DrawMesh(mesh._mesh, resultLocalToWorld, _material, 0, 0);//Pass 0 강제 사용

				#region [미사용 코드] 기본 수식이며 함수로 래핑
				////ScreenSpace가 얼마나 바뀌었는가를 저장하여 Child에 알려주자
				//Vector3 screenPosOffset = new Vector3((screenWidth / 2), (screenHeight / 2), 0) - vertPosS_Center;

				//_lastMaskScreenSpaceOffset.x = (screenPosOffset.x + screenCenterOffset.x) / screenWidth;
				//_lastMaskScreenSpaceOffset.y = (screenPosOffset.y + screenCenterOffset.y) / screenHeight;
				//_lastMaskScreenSpaceOffset.z = zoomScale;
				//_lastMaskScreenSpaceOffset.w = zoomScale; 
				#endregion


				//if (_isAnyChainedReceiver)
				//{
				//	//체인 리시버가 있다면, 리시버용 MaskScreenSpaceOffset을 계산하자
				//	//ScreenWidth / Height가 RT 크기를 기반으로 한다.
				//	screenWidth = Mathf.Max((float)_textureSize * mainCam.rect.width, 0.001f);
				//	screenHeight = Mathf.Max((float)_textureSize * mainCam.rect.height, 0.001f);
				//	screenCenterOffset = new Vector3((float)_textureSize * mainCam.rect.x, (float)_textureSize * mainCam.rect.y, 0.0f);

				//	resultLocalToWorld = Matrix4x4.identity;

				//	CalculateMaskScreenSpaceOffset(	mainCam, mainCamTransform,
				//									meshTransform,
				//									vertPosW_Center,
				//									vertPosS_LT, vertPosS_RB, vertPosS_Center,
				//									screenWidth, screenHeight, screenCenterOffset,
				//									out resultLocalToWorld,
				//									out _lastMaskScreenSpaceOffsetForChained);

				//}
			}
            else
            {
				// [ 최적화 없이 전체 화면 렌더링 ]
				cmdBuffer.Clear();
				cmdBuffer.SetRenderTarget(maskRT.RenderTargetID, 0);
				cmdBuffer.ClearRenderTarget(true, true, Color.clear);

#if UNITY_2019_1_OR_NEWER
				cmdBuffer.SetViewMatrix(mainCam.worldToCameraMatrix);
				cmdBuffer.SetProjectionMatrix(mainCam.projectionMatrix);
#endif

				//메시를 그린다.
				cmdBuffer.DrawMesh(mesh._mesh, meshTransform.localToWorldMatrix, _material, 0, 0);//일반 Clipping은 Pass 0을 강제로 사용한다.

				//최적화 하지 않은 MaskScreenSpaceOffset을 저장 (렌더 포커스 이동 없음(0, 0) + Zoom 비율 1)
				_lastMaskScreenSpaceOffset.x = 0.0f;
				_lastMaskScreenSpaceOffset.y = 0.0f;
				_lastMaskScreenSpaceOffset.z = 1.0f;
				_lastMaskScreenSpaceOffset.w = 1.0f;

				//if (_isAnyChainedReceiver)
				//{
				//	_lastMaskScreenSpaceOffsetForChained = _lastMaskScreenSpaceOffset;
				//}
            }

			//마스크 작성을 완료하면 true로 변환
			_isVisible = true;

			// Shown 상태로 만들자
            _visibleStatus = VISIBLE_STATUS.Shown;
        }



		private void UpdateCommandBuffer_Basic_PerMesh(CameraRTCmdUnit RTUnit, bool isOptimizeArea)
		{
			if(_request_PerMesh == null || _material == null)
			{
				return;
			}

			if(RTUnit == null || RTUnit.LinkedCameraUnit == null)
            {
                return;
            }

			Camera linkedCam = RTUnit.LinkedCameraUnit.Camera;
			Transform linkedCamTransform = RTUnit.LinkedCameraUnit.CamTransform;
            CommandBuffer cmdBuffer = RTUnit.CommandBuffer;
			apOptMaskRT maskRT = RTUnit.MaskRT;

			apOptMesh mesh = _request_PerMesh.Mesh;
			Transform meshTransform = _request_PerMesh.Mesh._transform;
			

			if (linkedCam == null)
            {
                return;
            }

			if(!maskRT.IsCreated())
			{
				//마스크가 생성되지 않았다.
				return;
			}

			
			//"마지막으로 업데이트된 마스크 RT"를 저장하자
			_lastUpdatedMaskRT = maskRT;


            //클리핑 마스크와 달리, 메시가 보여지지 않아도 마스크는 작성해야한다. (렌더링만 안할 뿐)
			if(!mesh.IsVisible())
			{
                //메시가 보여지지 않는 상태라면 마스크를 클리어만 한다.
                if(_visibleStatus != VISIBLE_STATUS.Hidden)
                {
                    //이전에도 Hidden 상태였다면 Skip
                    cmdBuffer.Clear();
                    cmdBuffer.SetRenderTarget(maskRT.RenderTargetID, 0);
                    cmdBuffer.ClearRenderTarget(true, true, Color.clear);
                    _visibleStatus = VISIBLE_STATUS.Hidden;
                }

				return;
			}			

			//렌더링을 위한 설정값 받아오기
			Texture curTexture = null;
            Color curMeshColor = Color.black;
            float vertRange_XMin = 0.0f;
            float vertRange_XMax = 0.0f;
            float vertRange_YMin = 0.0f;
            float vertRange_YMax = 0.0f;
            Vector3 vertPosCenter = Vector3.zero;

			//복사부터 하자
			//mesh.CopyMaterialToTarget(_material);

            mesh.GetMaskRenderInfo( out curTexture,
                                    out curMeshColor,
                                    out vertRange_XMin,
                                    out vertRange_XMax,
                                    out vertRange_YMin,
                                    out vertRange_YMax,
                                    out vertPosCenter);

			apOptSendMaskData sendData = _request_PerMesh.SendData;			

			//재질 설정
			//옵션에 따라 다르다.
			if(sendData._rtShaderType == apSendMaskData.RT_SHADER_TYPE.MainTextureOnly)
			{
				//MainTex만 사용하는 경우는, Color는 기본값은 Gray를 전달한다.
				if(_isHasProp_MainTex)
				{
					_material.SetTexture(_propID_MainTex, curTexture);
				}
				if(_isHasProp_Color)
				{
					_material.SetColor(_propID_Color, _defaultGrayColor);
				}
			}
			else
			{
				//Custom Shader를 포함한 대부분에서는 MainTex + Color를 전달한다.
				if(_isHasProp_MainTex)
				{
					_material.SetTexture(_propID_MainTex, curTexture);
				}
				if(_isHasProp_Color)
				{
					_material.SetColor(_propID_Color, curMeshColor);
				}
			}

			//쉐이더 패스도 지정
			int shaderPassIndex = sendData._shaderPassIndex;

			if(isOptimizeArea)
            {
				// [ 영역 최적화 ]

                Vector3 vertPosL_LT = new Vector3(vertRange_XMin, vertRange_YMax, 0.0f);
                Vector3 vertPosL_RB = new Vector3(vertRange_XMax, vertRange_YMin, 0.0f);
                
                Vector3 vertPosW_LT = meshTransform.TransformPoint(vertPosL_LT);
                Vector3 vertPosW_RB = meshTransform.TransformPoint(vertPosL_RB);
                Vector3 vertPosW_Center = meshTransform.TransformPoint(vertPosCenter);

                //현재 카메라에 대해서만 업데이트
                // if(mainCam.orthographic)
                // {
                //     vertPosCenter.z = 0.0f;                    
                // }

                Vector3 vertPosS_Center = linkedCam.WorldToScreenPoint(vertPosW_Center);
                Vector3 vertPosS_LT = linkedCam.WorldToScreenPoint(vertPosW_LT);
                Vector3 vertPosS_RB = linkedCam.WorldToScreenPoint(vertPosW_RB);

                float screenWidth = Mathf.Max((float)Screen.width * linkedCam.rect.width, 0.001f);
                float screenHeight = Mathf.Max((float)Screen.height * linkedCam.rect.height, 0.001f);
                Vector3 screenCenterOffset = new Vector3((float)Screen.width * linkedCam.rect.x, (float)Screen.height * linkedCam.rect.y, 0.0f);

                //해당 카메라가 TargetTexture를 갖고 있다면
                if(linkedCam.targetTexture != null)
                {
                    screenWidth = linkedCam.targetTexture.width;
                    screenHeight = linkedCam.targetTexture.height;
                    screenCenterOffset.x = 0.0f;
                    screenCenterOffset.y = 0.0f;

                    //주의 : 카메라의 rect (Viewport Rect)가 기본값인 (0, 0, 1, 1)이 아닌 경우
                    //크기와 화면 비율에 따라서 값이 바뀌지만, 이것까지 제어하진 말자
                    //-ㅅ- =3
                }

                if (!linkedCam.orthographic)
                {
                    Vector3 centerSceenPos = vertPosS_LT * 0.5f + vertPosS_RB * 0.5f;

                    //영역 대각선의 길이의 절반
                    float distLT2RB_Half = 0.5f * Mathf.Sqrt(
                        (vertPosS_LT.x - vertPosS_RB.x) * (vertPosS_LT.x - vertPosS_RB.x)
                        + (vertPosS_LT.y - vertPosS_RB.y) * (vertPosS_LT.y - vertPosS_RB.y));
                    distLT2RB_Half *= 1.6f;

                    vertPosS_LT.x = centerSceenPos.x - distLT2RB_Half;
                    vertPosS_LT.y = centerSceenPos.y - distLT2RB_Half;

                    vertPosS_RB.x = centerSceenPos.x + distLT2RB_Half;
                    vertPosS_RB.y = centerSceenPos.y + distLT2RB_Half;
                }

                //Zoom 계산
                //모든 버텍스가 화면안에 들어온다면 Sceen 좌표계 Scale이 0~1의 값을 가진다.
                float prevSizeWidth = Mathf.Max(Mathf.Abs(vertPosS_LT.x - vertPosS_RB.x) / screenWidth, 0.001f);
                float prevSizeHeight = Mathf.Max(Mathf.Abs(vertPosS_LT.y - vertPosS_RB.y) / screenHeight, 0.001f);

                //화면에 가득 찰 수 있도록 확대하는 비율은 W, H 중에서 "덜 확대하는 비율 (Min 함수)"로 진행한다.
                float zoomScale = Mathf.Min(1.0f / prevSizeWidth, 1.0f / prevSizeHeight);

                //메시 자체를 평행이동하여 화면 중앙에 위치시켜야 한다.
                float aspectRatio = screenWidth / screenHeight;

				//변형된 Ortho 크기
				float newOrthoSizeY = 0.0f;
                if(linkedCam.orthographic)
                {
					newOrthoSizeY = linkedCam.orthographicSize / zoomScale;
                }
				else
				{
					//메시의 중심까지의 거리
					float zDepth = Mathf.Abs(linkedCam.worldToCameraMatrix.MultiplyPoint3x4(vertPosW_Center).z);
					newOrthoSizeY = zDepth * Mathf.Tan(linkedCam.fieldOfView * 0.5f * Mathf.Deg2Rad) / zoomScale;
				}
				float newOrthoSizeX = aspectRatio * newOrthoSizeY;

				//다음 카메라 위치는 메시의 Center로부터 카메라가 바라보는 방향의 "역방향"으로 Ray를 쏠 때,
				//Ray*Dist 만큼의 위치
				float distCenterToCamera = Vector3.Distance(vertPosW_Center, linkedCamTransform.position);
				Vector3 nextCameraPos = vertPosW_Center - (linkedCamTransform.forward * distCenterToCamera);
				
                Matrix4x4 customWorldToCam = Matrix4x4.TRS(nextCameraPos, linkedCamTransform.rotation, Vector3.one).inverse;
				customWorldToCam.m20 *= -1f;
				customWorldToCam.m21 *= -1f;
				customWorldToCam.m22 *= -1f;
				customWorldToCam.m23 *= -1f;
				
				Matrix4x4 customCullingMatrix = Matrix4x4.Ortho(	-newOrthoSizeX,		//Left
																	newOrthoSizeX,		//Right
																	-newOrthoSizeY,		//Bottom
																	newOrthoSizeY,		//Top
																	distCenterToCamera - 10,	//Near
																	distCenterToCamera + 50		//Far
																)
												* customWorldToCam;

				//오브젝트의 World Matrix를 다시 계산하자
				//다중 메시 연산시 이 부분만 건들면 될 듯
				Matrix4x4 newLocalToProj = customCullingMatrix * meshTransform.localToWorldMatrix;//Local > Proj를 만들고
				Matrix4x4 newLocalToWorld = linkedCam.cullingMatrix.inverse * newLocalToProj;//Proj에 Culling-inv를 곱하여 World 역계산


				//커맨드 버퍼 작성
				cmdBuffer.Clear();
				cmdBuffer.SetRenderTarget(maskRT.RenderTargetID, 0);
				cmdBuffer.ClearRenderTarget(true, true, Color.clear);

#if UNITY_2019_1_OR_NEWER
				cmdBuffer.SetViewMatrix(linkedCam.worldToCameraMatrix);
				cmdBuffer.SetProjectionMatrix(linkedCam.projectionMatrix);
#endif
				//메시를 그린다.
				cmdBuffer.DrawMesh(mesh._mesh, newLocalToWorld, _material, 0, shaderPassIndex);

				//ScreenSpace가 얼마나 바뀌었는가를 저장하여 Child에 알려주자
				Vector3 screenPosOffset = new Vector3((screenWidth / 2), (screenHeight / 2), 0) - vertPosS_Center;
				
				_lastMaskScreenSpaceOffset.x = (screenPosOffset.x + screenCenterOffset.x) / screenWidth;
				_lastMaskScreenSpaceOffset.y = (screenPosOffset.y + screenCenterOffset.y) / screenHeight;
				_lastMaskScreenSpaceOffset.z = zoomScale;
				_lastMaskScreenSpaceOffset.w = zoomScale;
            }
            else
            {
				// [ 최적화 없이 전체 화면 렌더링 ]
				cmdBuffer.Clear();
				cmdBuffer.SetRenderTarget(maskRT.RenderTargetID, 0);
				cmdBuffer.ClearRenderTarget(true, true, Color.clear);

#if UNITY_2019_1_OR_NEWER
				cmdBuffer.SetViewMatrix(linkedCam.worldToCameraMatrix);
				cmdBuffer.SetProjectionMatrix(linkedCam.projectionMatrix);
#endif

				//메시를 그린다.
				cmdBuffer.DrawMesh(mesh._mesh, meshTransform.localToWorldMatrix, _material, 0, shaderPassIndex);

				//최적화 하지 않은 MaskScreenSpaceOffset을 저장 (렌더 포커스 이동 없음(0, 0) + Zoom 비율 1)
				_lastMaskScreenSpaceOffset.x = 0.0f;
				_lastMaskScreenSpaceOffset.y = 0.0f;
				_lastMaskScreenSpaceOffset.z = 1.0f;
				_lastMaskScreenSpaceOffset.w = 1.0f;
            }

			//마스크 작성을 완료하면 true로 변환
			_isVisible = true;

			_visibleStatus = VISIBLE_STATUS.Shown;
				
		}


		private void UpdateCommandBuffer_Basic_Shared(CameraRTCmdUnit RTUnit, bool isOptimizeArea)
		{
			if(_nRequestsShared == 0)
			{
				//Debug.LogError("Request 없음");
				return;
			}
			if(RTUnit == null || RTUnit.LinkedCameraUnit == null)
			{
				//Debug.LogError("RT Unit 없음 / 카메라 없음");
				return;
			}

			Camera linkedCam = RTUnit.LinkedCameraUnit.Camera;
			Transform linkedCamTF = RTUnit.LinkedCameraUnit.CamTransform;
            CommandBuffer cmdBuffer = RTUnit.CommandBuffer;
			apOptMaskRT maskRT = RTUnit.MaskRT;

			if(linkedCam == null)
			{
				//Debug.LogError("실제 카메라 없음");
				return;
			}

			if(!maskRT.IsCreated())
			{
				//마스크가 생성되지 않았다.
				//Debug.LogError("RT 생성 안됨");
				return;
			}

			//"마지막으로 업데이트된 마스크 RT"를 저장하자
			_lastUpdatedMaskRT = maskRT;

			//메시들을 Sorting하여 렌더링 순서를 결정하자
			if(_portrait._sortingOrderOption == apPortrait.SORTING_ORDER_OPTION.DepthToOrder
				|| _portrait._sortingOrderOption == apPortrait.SORTING_ORDER_OPTION.ReverseDepthToOrder)
			{
				//Sorting Order를 이용하여 Sorting (Order 순서로 오름차순)
				_requests_Shared.Sort(delegate(Request_Shared a, Request_Shared b)
				{
					return a.GetSortingValue_SortingOrder() - b.GetSortingValue_SortingOrder();
				});
			}
			else
			{
				//Z 위치를 기준으로 Sorting (Z 내림차순. Z가 클 수록 뒤에 있다.)
				_requests_Shared.Sort(delegate (Request_Shared a, Request_Shared b)
				{
					//Z값이 큰게 먼저 렌더링되어야 한다.
					return (int)((b.GetSortingValue_LocalZ() - a.GetSortingValue_LocalZ()) * 1000.0f);
				});
			}


			//순서가 조금 다르다
			//1. 먼저 영역 최적화가 있을 수 있으니, 메시들의 Vert Range (Min-Max)와 Vert Center를 저장해둔다.
			//2. 전체 영역 최적화를 기준으로 렌더링을 한다.
			//3. 렌더링 직전에 재질에 값을 넣는다.
			if (isOptimizeArea)
			{
				// [ 영역 최적화 ]
				
                ////World Min-Max를 저장
                //버텍스 Center의 World는 World Min-Max의 중점으로 구하고 Screen으로 변경                
                //Screen Min-Max으로 변환
                float curVertPosL_MinX = 0.0f;
                float curVertPosL_MaxX = 0.0f;
                float curVertPosL_MinY = 0.0f;
                float curVertPosL_MaxY = 0.0f;
                Vector3 curVertCenterL = Vector3.zero;

                Vector3 curVertPosL_LT = Vector3.zero;
                Vector3 curVertPosL_RB = Vector3.zero;

                Vector3 curVertPosW_LT = Vector3.zero;
                Vector3 curVertPosW_RB = Vector3.zero;

                //전체 범위 (Min-Max)
                Vector3 vertRangeW_Min = Vector3.zero;
                Vector3 vertRangeW_Max = Vector3.zero;
                
				int nVisibleMeshes = 0;

				Request_Shared curReq = null;
				apOptMesh curMesh = null;
				Transform curMeshTransform = null;
				
				int iValid = 0;
				for (int iReq = 0; iReq < _nRequestsShared; iReq++)
				{
					curReq = _requests_Shared[iReq];
					curMesh = curReq.Mesh;
					curMeshTransform = curMesh._transform;

					curReq._cal_IsVisible = curMesh.IsVisible() && curReq.Material != null;

					if (!curReq._cal_IsVisible)
					{
						//안보인다면 일단 패스
						continue;
					}

					//메시의 재질을 그대로 1차로 복사한다.
					//curReq.CopyMeshProperties(curMesh);

					//값을 받아와서 바로 Request에 저장을 해둔다.
					curMesh.GetMaskRenderInfo(out curReq._cal_Texture,
												out curReq._cal_MeshColor,
												out curVertPosL_MinX,
												out curVertPosL_MaxX,
												out curVertPosL_MinY,
												out curVertPosL_MaxY,
												out curVertCenterL);

					//Request의 재질을 설정한다.
					if(curReq.RTShaderType == apSendMaskData.RT_SHADER_TYPE.MainTextureOnly)
					{
						//MainTex만 사용하는 경우는 Color에는 기본값인 Gray를 전달한다.
						curReq.SetMainTex(_propID_MainTex, curReq._cal_Texture);
						curReq.SetColor(_propID_Color, _defaultGrayColor);
					}
					else
					{
						//Custom Shader를 포함한 대부분에서는 MainTex+Color를 전달한다.
						curReq.SetMainTex(_propID_MainTex, curReq._cal_Texture);
						curReq.SetColor(_propID_Color, curReq._cal_MeshColor);
					}

					curVertPosL_LT = new Vector3(curVertPosL_MinX, curVertPosL_MaxY, 0.0f);
                    curVertPosL_RB = new Vector3(curVertPosL_MaxX, curVertPosL_MinY, 0.0f);

                    curVertPosW_LT = curMeshTransform.TransformPoint(curVertPosL_LT);
                    curVertPosW_RB = curMeshTransform.TransformPoint(curVertPosL_RB);

					
					//영역을 결정한다.
					if(iValid == 0)
					{
						vertRangeW_Min.x = Mathf.Min(curVertPosW_LT.x, curVertPosW_RB.x);
                        vertRangeW_Min.y = Mathf.Min(curVertPosW_LT.y, curVertPosW_RB.y);
                        vertRangeW_Min.z = Mathf.Min(curVertPosW_LT.z, curVertPosW_RB.z);//Center는 Z값도 가져야 하므로

                        vertRangeW_Max.x = Mathf.Max(curVertPosW_LT.x, curVertPosW_RB.x);
                        vertRangeW_Max.y = Mathf.Max(curVertPosW_LT.y, curVertPosW_RB.y);
                        vertRangeW_Max.z = Mathf.Max(curVertPosW_LT.z, curVertPosW_RB.z);
					}
					else
					{
						vertRangeW_Min.x = Mathf.Min(vertRangeW_Min.x, curVertPosW_LT.x, curVertPosW_RB.x);
                        vertRangeW_Min.y = Mathf.Min(vertRangeW_Min.y, curVertPosW_LT.y, curVertPosW_RB.y);
                        vertRangeW_Min.z = Mathf.Min(vertRangeW_Min.z, curVertPosW_LT.z, curVertPosW_RB.z);

                        vertRangeW_Max.x = Mathf.Max(vertRangeW_Max.x, curVertPosW_LT.x, curVertPosW_RB.x);
                        vertRangeW_Max.y = Mathf.Max(vertRangeW_Max.y, curVertPosW_LT.y, curVertPosW_RB.y);
                        vertRangeW_Max.z = Mathf.Max(vertRangeW_Max.z, curVertPosW_LT.z, curVertPosW_RB.z);
					}

					nVisibleMeshes += 1;
					iValid += 1;					
				}

				//Debug.Log("Range : " + vertRangeW_Min + " > " + vertRangeW_Max);

				//계산이 끝났다면 다시 돌면서 업데이트를 하자
                if(nVisibleMeshes == 0)
                {
                    //만약 보여지는 메시가 없다면
                    //커맨드 버퍼를 클리어하기만 한다.
                    if(_visibleStatus != VISIBLE_STATUS.Hidden)
                    {
                        //이전에도 Hidden 상태였다면 Skip
                        cmdBuffer.Clear();
                        cmdBuffer.SetRenderTarget(maskRT.RenderTargetID, 0);
                        cmdBuffer.ClearRenderTarget(true, true, Color.clear);
                        _visibleStatus = VISIBLE_STATUS.Hidden;
                    }

					//Debug.LogError("Shared : 보여지는 메시 없음 (" + _nRequestsShared + ")");
                    return;
                }

                //최대-최소 범위는 찾았으니 평균 위치를 계산하자
                Vector3 vertPosCenterW = (vertRangeW_Min * 0.5f) + (vertRangeW_Max * 0.5f);
				Vector3 vertPosCenterS = linkedCam.WorldToScreenPoint(vertPosCenterW);
                Vector3 vertRengeS_Min = linkedCam.WorldToScreenPoint(vertRangeW_Min);
                Vector3 vertRengeS_Max = linkedCam.WorldToScreenPoint(vertRangeW_Max);
                
                float screenWidth = Mathf.Max((float)Screen.width * linkedCam.rect.width, 0.001f);
                float screenHeight = Mathf.Max((float)Screen.height * linkedCam.rect.height, 0.001f);
                Vector3 screenCenterOffset = new Vector3((float)Screen.width * linkedCam.rect.x, (float)Screen.height * linkedCam.rect.y, 0.0f);

                //해당 카메라가 TargetTexture를 갖고 있다면
                if(linkedCam.targetTexture != null)
                {
                    screenWidth = linkedCam.targetTexture.width;
                    screenHeight = linkedCam.targetTexture.height;
                    screenCenterOffset.x = 0.0f;
                    screenCenterOffset.y = 0.0f;
                }

                if (!linkedCam.orthographic)
                {
                    Vector3 centerSceenPos = (vertRengeS_Min * 0.5f) + (vertRengeS_Max * 0.5f);

                    //영역 대각선의 길이의 절반
                    float distLT2RB_Half = 0.5f * Mathf.Sqrt(
                                            (vertRengeS_Min.x - vertRengeS_Max.x) * (vertRengeS_Min.x - vertRengeS_Max.x)
                                            + (vertRengeS_Min.y - vertRengeS_Max.y) * (vertRengeS_Min.y - vertRengeS_Max.y));
                    distLT2RB_Half *= 1.6f;

                    vertRengeS_Min.x = centerSceenPos.x - distLT2RB_Half;
                    vertRengeS_Min.y = centerSceenPos.y - distLT2RB_Half;

                    vertRengeS_Max.x = centerSceenPos.x + distLT2RB_Half;
                    vertRengeS_Max.y = centerSceenPos.y + distLT2RB_Half;
                }

                //Zoom 계산
                //모든 버텍스가 화면안에 들어온다면 Sceen 좌표계 Scale이 0~1의 값을 가진다.
                float prevSizeWidth = Mathf.Max(Mathf.Abs(vertRengeS_Max.x - vertRengeS_Min.x) / screenWidth, 0.001f);
                float prevSizeHeight = Mathf.Max(Mathf.Abs(vertRengeS_Max.y - vertRengeS_Min.y) / screenHeight, 0.001f);

                //화면에 가득 찰 수 있도록 확대하는 비율은 W, H 중에서 "덜 확대하는 비율 (Min 함수)"로 진행한다.
                float zoomScale = Mathf.Min(1.0f / prevSizeWidth, 1.0f / prevSizeHeight);
				
                //메시 자체를 평행이동하여 화면 중앙에 위치시켜야 한다.
                float aspectRatio = screenWidth / screenHeight;

				//변형된 Ortho 크기
				float newOrthoSizeY = 0.0f;
                if(linkedCam.orthographic)
                {
					newOrthoSizeY = linkedCam.orthographicSize / zoomScale;
                }
				else
				{
					//메시의 중심까지의 거리
					float zDepth = Mathf.Abs(linkedCam.worldToCameraMatrix.MultiplyPoint3x4(vertPosCenterW).z);
					newOrthoSizeY = zDepth * Mathf.Tan(linkedCam.fieldOfView * 0.5f * Mathf.Deg2Rad) / zoomScale;
				}
				float newOrthoSizeX = aspectRatio * newOrthoSizeY;

				//다음 카메라 위치는 메시의 Center로부터 카메라가 바라보는 방향의 "역방향"으로 Ray를 쏠 때,
				//Ray*Dist 만큼의 위치
				float distCenterToCamera = Vector3.Distance(vertPosCenterW, linkedCamTF.position);
				Vector3 nextCameraPos = vertPosCenterW - (linkedCamTF.forward * distCenterToCamera);
				
                Matrix4x4 customWorldToCam = Matrix4x4.TRS(nextCameraPos, linkedCamTF.rotation, Vector3.one).inverse;
				customWorldToCam.m20 *= -1f;
				customWorldToCam.m21 *= -1f;
				customWorldToCam.m22 *= -1f;
				customWorldToCam.m23 *= -1f;

				
				Matrix4x4 customCullingMatrix = Matrix4x4.Ortho(	-newOrthoSizeX,		//Left
																	newOrthoSizeX,		//Right
																	-newOrthoSizeY,		//Bottom
																	newOrthoSizeY,		//Top
																	distCenterToCamera - 10,	//Near
																	distCenterToCamera + 50		//Far
																)
												* customWorldToCam;

				//커맨드 버퍼 작성
                cmdBuffer.Clear();
				cmdBuffer.SetRenderTarget(maskRT.RenderTargetID, 0);
				cmdBuffer.ClearRenderTarget(true, true, Color.clear);

#if UNITY_2019_1_OR_NEWER
				cmdBuffer.SetViewMatrix(linkedCam.worldToCameraMatrix);
				cmdBuffer.SetProjectionMatrix(linkedCam.projectionMatrix);
#endif

				Matrix4x4 invCullingMatrix = linkedCam.cullingMatrix.inverse;

				//이제 메시들을 돌면서 하나씩 렌더링을 하자
				//Material도 갱신해야한다.				
				for (int iReq = 0; iReq < _nRequestsShared; iReq++)
				{
					curReq = _requests_Shared[iReq];
					curMesh = curReq.Mesh;
					curMeshTransform = curMesh._transform;

					if (!curReq._cal_IsVisible)
					{
						//안보인다면 패스
						continue;
					}

					//Matrix를 계산한다.
					Matrix4x4 newLocalToProj = customCullingMatrix * curMeshTransform.localToWorldMatrix;//Local > Proj를 만들고
				    Matrix4x4 newLocalToWorld = invCullingMatrix * newLocalToProj;//Proj에 Culling-inv를 곱하여 World 역계산

					//메시를 그린다.
					cmdBuffer.DrawMesh(curMesh._mesh, newLocalToWorld, curReq.Material, 0, curReq.SendData._shaderPassIndex);
                }

                //ScreenSpace가 얼마나 바뀌었는가를 저장하여 Child에 알려주자
				Vector3 screenPosOffset = new Vector3((screenWidth / 2), (screenHeight / 2), 0) - vertPosCenterS;
				
				_lastMaskScreenSpaceOffset.x = (screenPosOffset.x + screenCenterOffset.x) / screenWidth;
				_lastMaskScreenSpaceOffset.y = (screenPosOffset.y + screenCenterOffset.y) / screenHeight;
				_lastMaskScreenSpaceOffset.z = zoomScale;
				_lastMaskScreenSpaceOffset.w = zoomScale;


			}
			else
			{
				// [ 최적화 없이 전체 화면 렌더링 ]
                Request_Shared curReq = null;
				apOptMesh curMesh = null;
				Transform curMeshTransform = null;
                int nVisibleMeshes = 0;

				for (int iReq = 0; iReq < _nRequestsShared; iReq++)
				{
					curReq = _requests_Shared[iReq];
					curMesh = curReq.Mesh;
					curMeshTransform = curMesh._transform;

					curReq._cal_IsVisible = curMesh.IsVisible() && curReq.Material != null;
					if(!curReq._cal_IsVisible)
					{
						//안보인다면 일단 패스
						continue;
					}

					//메시의 재질을 그대로 1차로 복사한다.
					//curReq.CopyMeshProperties(curMesh);

                    //여기서는 텍스쳐와 색상 값만 받아온다.
                    curMesh.GetMaskRenderInfo(  out curReq._cal_Texture,
                                                out curReq._cal_MeshColor);

					//Request의 재질을 설정한다.
					if(curReq.RTShaderType == apSendMaskData.RT_SHADER_TYPE.MainTextureOnly)
					{
						//MainTex만 사용하는 경우는 Color에는 기본값인 Gray를 전달한다.
						curReq.SetMainTex(_propID_MainTex, curReq._cal_Texture);
						curReq.SetColor(_propID_Color, _defaultGrayColor);
					}
					else
					{
						//Custom Shader를 포함한 대부분에서는 MainTex+Color를 전달한다.
						curReq.SetMainTex(_propID_MainTex, curReq._cal_Texture);
						curReq.SetColor(_propID_Color, curReq._cal_MeshColor);
					}

                    nVisibleMeshes += 1;
                }

                if(nVisibleMeshes == 0)
                {
                    //만약 보여지는 메시가 없다면
                    //커맨드 버퍼를 클리어하기만 한다.
                    if(_visibleStatus != VISIBLE_STATUS.Hidden)
                    {
                        //이전에도 Hidden 상태였다면 Skip
                        cmdBuffer.Clear();
                        cmdBuffer.SetRenderTarget(maskRT.RenderTargetID, 0);
                        cmdBuffer.ClearRenderTarget(true, true, Color.clear);
                        _visibleStatus = VISIBLE_STATUS.Hidden;
                    }

					//Debug.LogError("보여지는 메시 없음");
                    return;
                }

                cmdBuffer.Clear();
				cmdBuffer.SetRenderTarget(maskRT.RenderTargetID, 0);
				cmdBuffer.ClearRenderTarget(true, true, Color.clear);

#if UNITY_2019_1_OR_NEWER
				cmdBuffer.SetViewMatrix(linkedCam.worldToCameraMatrix);
				cmdBuffer.SetProjectionMatrix(linkedCam.projectionMatrix);
#endif

                //이제 메시들을 돌면서 하나씩 렌더링을 하자
                //Material도 갱신해야한다.
                for (int iReq = 0; iReq < _nRequestsShared; iReq++)
                {
                    curReq = _requests_Shared[iReq];
                    curMesh = curReq.Mesh;
                    curMeshTransform = curMesh._transform;

                    if(!curReq._cal_IsVisible)
                    {
                        //안보인다면 패스
                        continue;
                    }

					//메시를 그린다.
                    cmdBuffer.DrawMesh(curMesh._mesh, curMeshTransform.localToWorldMatrix, curReq.Material, 0, curReq.SendData._shaderPassIndex);
                }

                //최적화 하지 않은 MaskScreenSpaceOffset을 저장 (렌더 포커스 이동 없음(0, 0) + Zoom 비율 1)
				_lastMaskScreenSpaceOffset.x = 0.0f;
				_lastMaskScreenSpaceOffset.y = 0.0f;
				_lastMaskScreenSpaceOffset.z = 1.0f;
				_lastMaskScreenSpaceOffset.w = 1.0f;
			}

            _isVisible = true;
            _visibleStatus = VISIBLE_STATUS.Shown;

			//Debug.Log("> Mast RT 렌더링됨");
		}



		//------------------------------------------------------------------------
		// Single VR 카메라에서의 커맨드 버퍼 업데이트 (렌더러 타입별)
		//------------------------------------------------------------------------

		private void UpdateCommandBuffer_SingleVR_Clipping(CameraRTCmdUnit RTUnit)
		{
			if(_request_Clipping == null || _material == null)
            {
                return;
            }

            if(RTUnit == null || RTUnit.LinkedCameraUnit == null)
            {
                return;
            }
			
            Camera mainCam = RTUnit.LinkedCameraUnit.Camera;
			Transform mainCamTransform = RTUnit.LinkedCameraUnit.CamTransform;
            CommandBuffer cmdBuffer = RTUnit.CommandBuffer;
			apOptMaskRT maskRT = RTUnit.MaskRT;

            apOptMesh mesh = _request_Clipping.Mesh;
            Transform meshTransform = _request_Clipping.Mesh._transform;

            if(mainCam == null)
            {
                return;
            }

			if(!maskRT.IsCreated())
			{
				//마스크가 생성되지 않았다.
				return;
			}

			//"마지막으로 업데이트된 마스크 RT"를 저장하자
			_lastUpdatedMaskRT = maskRT;

			if(!mesh.IsVisible())
			{
				//메시가 보여지지 않는 상태라면 마스크를 클리어만 한다.
                if(_visibleStatus != VISIBLE_STATUS.Hidden)
                {
                    //이전에도 Hidden 상태였다면 Skip
                    cmdBuffer.Clear();
                    cmdBuffer.SetRenderTarget(maskRT.RenderTargetID, 0);
                    cmdBuffer.ClearRenderTarget(true, true, Color.clear);
                    _visibleStatus = VISIBLE_STATUS.Hidden;
                }
				return;
			}

			//렌더링을 위한 설정값 받아오기
			Texture curTexture = null;
            Color curMeshColor = Color.black;
            float vertRange_XMin = 0.0f;
            float vertRange_XMax = 0.0f;
            float vertRange_YMin = 0.0f;
            float vertRange_YMax = 0.0f;
            Vector3 vertPosCenter = Vector3.zero;

			
			//복사부터 하자
			//mesh.CopyMaterialToTarget(_material);

            mesh.GetMaskRenderInfo( out curTexture,
                                    out curMeshColor,
                                    out vertRange_XMin,
                                    out vertRange_XMax,
                                    out vertRange_YMin,
                                    out vertRange_YMax,
                                    out vertPosCenter);

            //재질 설정 (Clipping에서는 MainTex와 Color를 지정한다.)
			if(_isHasProp_MainTex)
			{
				_material.SetTexture(_propID_MainTex, curTexture);
			}
            if(_isHasProp_Color)
			{
				_material.SetColor(_propID_Color, curMeshColor);
			}

			// [ 최적화 없이 전체 화면 렌더링 ]
			//Single VR은 최적화가 없다.
			cmdBuffer.Clear();

			//듀얼 렌더링
			//1. Left Eye
			cmdBuffer.SetRenderTarget(maskRT.RenderTargetID_L, 0);
			cmdBuffer.ClearRenderTarget(true, true, Color.clear);

			// Left Eye에선 0
			cmdBuffer.SetGlobalFloat(_propID_unity_StereoEyeIndex, 0);

#if UNITY_2019_1_OR_NEWER
			cmdBuffer.SetViewMatrix(mainCam.GetStereoViewMatrix(Camera.StereoscopicEye.Left));
			cmdBuffer.SetProjectionMatrix(mainCam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left));
#endif

			cmdBuffer.DrawMesh(mesh._mesh, meshTransform.localToWorldMatrix, _material, 0, 0);//비 최적화 영역

			//2. Right Eye
			cmdBuffer.SetRenderTarget(maskRT.RenderTargetID_R, 0);
			cmdBuffer.ClearRenderTarget(true, true, Color.clear);

			// Right Eye에선 1
			cmdBuffer.SetGlobalFloat(_propID_unity_StereoEyeIndex, 1);

#if UNITY_2019_1_OR_NEWER
			cmdBuffer.SetViewMatrix(mainCam.GetStereoViewMatrix(Camera.StereoscopicEye.Right));
			cmdBuffer.SetProjectionMatrix(mainCam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right));
#endif
			cmdBuffer.DrawMesh(mesh._mesh, meshTransform.localToWorldMatrix, _material, 0, 0);//비 최적화 영역

			//최적화 하지 않은 MaskScreenSpaceOffset을 저장 (렌더 포커스 이동 없음(0, 0) + Zoom 비율 1)
			_lastMaskScreenSpaceOffset.x = 0.0f;
			_lastMaskScreenSpaceOffset.y = 0.0f;
			_lastMaskScreenSpaceOffset.z = 1.0f;
			_lastMaskScreenSpaceOffset.w = 1.0f;

			_isVisible = true;
            _visibleStatus = VISIBLE_STATUS.Shown;
		}
		


		private void UpdateCommandBuffer_SingleVR_PerMesh(CameraRTCmdUnit RTUnit)
		{
			if(_request_PerMesh == null || _material == null)
			{
				return;
			}

			if(RTUnit == null || RTUnit.LinkedCameraUnit == null)
            {
                return;
            }
			
            Camera mainCam = RTUnit.LinkedCameraUnit.Camera;
			Transform mainCamTransform = RTUnit.LinkedCameraUnit.CamTransform;
            CommandBuffer cmdBuffer = RTUnit.CommandBuffer;
			apOptMaskRT maskRT = RTUnit.MaskRT;

            apOptMesh mesh = _request_PerMesh.Mesh;
            Transform meshTransform = _request_PerMesh.Mesh._transform;

            if(mainCam == null)
            {
                return;
            }

			if(!maskRT.IsCreated())
			{
				//마스크가 생성되지 않았다.
				return;
			}

			//"마지막으로 업데이트된 마스크 RT"를 저장하자
			_lastUpdatedMaskRT = maskRT;

			if(!mesh.IsVisible())
			{
				//메시가 보여지지 않는 상태라면 마스크를 클리어만 한다.
                if(_visibleStatus != VISIBLE_STATUS.Hidden)
                {
                    //이전에도 Hidden 상태였다면 Skip
                    cmdBuffer.Clear();
                    cmdBuffer.SetRenderTarget(maskRT.RenderTargetID, 0);
                    cmdBuffer.ClearRenderTarget(true, true, Color.clear);
                    _visibleStatus = VISIBLE_STATUS.Hidden;
                }
				return;
			}

			//렌더링을 위한 설정값 받아오기
			Texture curTexture = null;
            Color curMeshColor = Color.black;
            float vertRange_XMin = 0.0f;
            float vertRange_XMax = 0.0f;
            float vertRange_YMin = 0.0f;
            float vertRange_YMax = 0.0f;
            Vector3 vertPosCenter = Vector3.zero;

			//복사부터 하자
			//mesh.CopyMaterialToTarget(_material);

            mesh.GetMaskRenderInfo( out curTexture,
                                    out curMeshColor,
                                    out vertRange_XMin,
                                    out vertRange_XMax,
                                    out vertRange_YMin,
                                    out vertRange_YMax,
                                    out vertPosCenter);

            apOptSendMaskData sendData = _request_PerMesh.SendData;

			//재질 설정
			//옵션에 따라 다르다.
			if(sendData._rtShaderType == apSendMaskData.RT_SHADER_TYPE.MainTextureOnly)
			{
				//MainTex만 사용하는 경우는, Color는 기본값은 Gray를 전달한다.
				if(_isHasProp_MainTex)
				{
					_material.SetTexture(_propID_MainTex, curTexture);
				}
				if(_isHasProp_Color)
				{
					_material.SetColor(_propID_Color, _defaultGrayColor);
				}
			}
			else
			{
				//Custom Shader를 포함한 대부분에서는 MainTex + Color를 전달한다.
				if(_isHasProp_MainTex)
				{
					_material.SetTexture(_propID_MainTex, curTexture);
				}
				if(_isHasProp_Color)
				{
					_material.SetColor(_propID_Color, curMeshColor);
				}
			}

			int shaderPassIndex = sendData._shaderPassIndex;

			// [ 최적화 없이 전체 화면 렌더링 ]
			//Single VR은 최적화가 없다.
			cmdBuffer.Clear();

			//듀얼 렌더링
			//1. Left Eye
			cmdBuffer.SetRenderTarget(maskRT.RenderTargetID_L, 0);
			cmdBuffer.ClearRenderTarget(true, true, Color.clear);

			// Left Eye에선 0
			cmdBuffer.SetGlobalFloat(_propID_unity_StereoEyeIndex, 0);

#if UNITY_2019_1_OR_NEWER
			cmdBuffer.SetViewMatrix(mainCam.GetStereoViewMatrix(Camera.StereoscopicEye.Left));
			cmdBuffer.SetProjectionMatrix(mainCam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left));
#endif
			cmdBuffer.DrawMesh(mesh._mesh, meshTransform.localToWorldMatrix, _material, 0, shaderPassIndex);//비 최적화 영역

			//2. Right Eye
			cmdBuffer.SetRenderTarget(maskRT.RenderTargetID_R, 0);
			cmdBuffer.ClearRenderTarget(true, true, Color.clear);

			// Right Eye에선 1
			cmdBuffer.SetGlobalFloat(_propID_unity_StereoEyeIndex, 1);

#if UNITY_2019_1_OR_NEWER
			cmdBuffer.SetViewMatrix(mainCam.GetStereoViewMatrix(Camera.StereoscopicEye.Right));
			cmdBuffer.SetProjectionMatrix(mainCam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right));
#endif
			cmdBuffer.DrawMesh(mesh._mesh, meshTransform.localToWorldMatrix, _material, 0, shaderPassIndex);//비 최적화 영역

			//최적화 하지 않은 MaskScreenSpaceOffset을 저장 (렌더 포커스 이동 없음(0, 0) + Zoom 비율 1)
			_lastMaskScreenSpaceOffset.x = 0.0f;
			_lastMaskScreenSpaceOffset.y = 0.0f;
			_lastMaskScreenSpaceOffset.z = 1.0f;
			_lastMaskScreenSpaceOffset.w = 1.0f;

			_isVisible = true;
            _visibleStatus = VISIBLE_STATUS.Shown;
		}


		private void UpdateCommandBuffer_SingleVR_Shared(CameraRTCmdUnit RTUnit)
		{
			if(_nRequestsShared == 0)
			{
				return;
			}
			if(RTUnit == null || RTUnit.LinkedCameraUnit == null)
			{
				return;
			}

			Camera mainCam = RTUnit.LinkedCameraUnit.Camera;
			Transform mainCamTransform = RTUnit.LinkedCameraUnit.CamTransform;
            CommandBuffer cmdBuffer = RTUnit.CommandBuffer;
			apOptMaskRT maskRT = RTUnit.MaskRT;

			if(mainCam == null)
			{
				return;
			}

			if(!maskRT.IsCreated())
			{
				//마스크가 생성되지 않았다.
				return;
			}

			//"마지막으로 업데이트된 마스크 RT"를 저장하자
			_lastUpdatedMaskRT = maskRT;

			//메시들을 Sorting하여 렌더링 순서를 결정하자
			if(_portrait._sortingOrderOption == apPortrait.SORTING_ORDER_OPTION.DepthToOrder
				|| _portrait._sortingOrderOption == apPortrait.SORTING_ORDER_OPTION.ReverseDepthToOrder)
			{
				//Sorting Order를 이용하여 Sorting (Order 순서로 오름차순)
				_requests_Shared.Sort(delegate(Request_Shared a, Request_Shared b)
				{
					return a.GetSortingValue_SortingOrder() - b.GetSortingValue_SortingOrder();
				});
			}
			else
			{
				//Z 위치를 기준으로 Sorting (Z 내림차순. Z가 클 수록 뒤에 있다.)
				_requests_Shared.Sort(delegate (Request_Shared a, Request_Shared b)
				{
					//Z값이 큰게 먼저 렌더링되어야 한다.
					return (int)((b.GetSortingValue_LocalZ() - a.GetSortingValue_LocalZ()) * 1000.0f);
				});
			}


			// [ 최적화 없이 전체 화면 렌더링 ]
			//Single VR은 영역 최적화가 없다.
            Request_Shared curReq = null;
			apOptMesh curMesh = null;
			Transform curMeshTransform = null;
            int nVisibleMeshes = 0;

			for (int iReq = 0; iReq < _nRequestsShared; iReq++)
			{
				curReq = _requests_Shared[iReq];
				curMesh = curReq.Mesh;
				curMeshTransform = curMesh._transform;

				curReq._cal_IsVisible = curMesh.IsVisible() && curReq.Material != null;
				if(!curReq._cal_IsVisible)
				{
					//안보인다면 일단 패스
					continue;
				}

				//메시의 재질을 그대로 1차로 복사한다.
				//curReq.CopyMeshProperties(curMesh);

                //여기서는 텍스쳐와 색상 값만 받아온다.
                curMesh.GetMaskRenderInfo(  out curReq._cal_Texture,
                                            out curReq._cal_MeshColor);

				//Request의 재질을 설정한다.
				if(curReq.RTShaderType == apSendMaskData.RT_SHADER_TYPE.MainTextureOnly)
				{
					//MainTex만 사용하는 경우는 Color에는 기본값인 Gray를 전달한다.
					curReq.SetMainTex(_propID_MainTex, curReq._cal_Texture);
					curReq.SetColor(_propID_Color, _defaultGrayColor);
				}
				else
				{
					//Custom Shader를 포함한 대부분에서는 MainTex+Color를 전달한다.
					curReq.SetMainTex(_propID_MainTex, curReq._cal_Texture);
					curReq.SetColor(_propID_Color, curReq._cal_MeshColor);
				}

                nVisibleMeshes += 1;
            }

            if(nVisibleMeshes == 0)
            {
                //만약 보여지는 메시가 없다면
                //커맨드 버퍼를 클리어하기만 한다.
                if(_visibleStatus != VISIBLE_STATUS.Hidden)
                {
                    //이전에도 Hidden 상태였다면 Skip
                    cmdBuffer.Clear();
                    cmdBuffer.SetRenderTarget(maskRT.RenderTargetID_L, 0);
                    cmdBuffer.ClearRenderTarget(true, true, Color.clear);

					cmdBuffer.SetRenderTarget(maskRT.RenderTargetID_R, 0);
                    cmdBuffer.ClearRenderTarget(true, true, Color.clear);

                    _visibleStatus = VISIBLE_STATUS.Hidden;
                }
                return;
            }

			//L, R 두번씩 그려야 한다.
			cmdBuffer.Clear();
			cmdBuffer.SetRenderTarget(maskRT.RenderTargetID_L, 0);
			cmdBuffer.ClearRenderTarget(true, true, Color.clear);

			// Left Eye에선 0
			cmdBuffer.SetGlobalFloat(_propID_unity_StereoEyeIndex, 0);

#if UNITY_2019_1_OR_NEWER
			cmdBuffer.SetViewMatrix(mainCam.GetStereoViewMatrix(Camera.StereoscopicEye.Left));
			cmdBuffer.SetProjectionMatrix(mainCam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left));
#endif

			//이제 메시들을 돌면서 하나씩 렌더링을 하자
            //Material도 갱신해야한다.
            for (int iReq = 0; iReq < _nRequestsShared; iReq++)
            {
                curReq = _requests_Shared[iReq];
                curMesh = curReq.Mesh;
                curMeshTransform = curMesh._transform;

                if(!curReq._cal_IsVisible)
                {
                    //안보인다면 패스
                    continue;
                }

                //메시를 그린다.
                cmdBuffer.DrawMesh(curMesh._mesh, curMeshTransform.localToWorldMatrix, curReq.Material, 0, curReq.SendData._shaderPassIndex);
            }

			cmdBuffer.SetRenderTarget(maskRT.RenderTargetID_R, 0);
			cmdBuffer.ClearRenderTarget(true, true, Color.clear);

			// Right Eye에선 1
			cmdBuffer.SetGlobalFloat(_propID_unity_StereoEyeIndex, 1);

#if UNITY_2019_1_OR_NEWER
			cmdBuffer.SetViewMatrix(mainCam.GetStereoViewMatrix(Camera.StereoscopicEye.Right));
			cmdBuffer.SetProjectionMatrix(mainCam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right));
#endif

			//한번 더 그리자
			for (int iReq = 0; iReq < _nRequestsShared; iReq++)
            {
                curReq = _requests_Shared[iReq];
                curMesh = curReq.Mesh;
                curMeshTransform = curMesh._transform;

                if(!curReq._cal_IsVisible)
                {
                    //안보인다면 패스
                    continue;
                }

                //메시를 그린다.
                cmdBuffer.DrawMesh(curMesh._mesh, curMeshTransform.localToWorldMatrix, curReq.Material, 0, curReq.SendData._shaderPassIndex);
            }

            //최적화 하지 않은 MaskScreenSpaceOffset을 저장 (렌더 포커스 이동 없음(0, 0) + Zoom 비율 1)
			_lastMaskScreenSpaceOffset.x = 0.0f;
			_lastMaskScreenSpaceOffset.y = 0.0f;
			_lastMaskScreenSpaceOffset.z = 1.0f;
			_lastMaskScreenSpaceOffset.w = 1.0f;

			_isVisible = true;
            _visibleStatus = VISIBLE_STATUS.Shown;
		}


		// 체인 여부 설정
		//------------------------------------------------------------------
		/// <summary>
		/// 이 렌더러의 결과를 받은 Receiver의 정보가 다음 페이즈에서 렌더러로 역할을 하는 경우.
		/// 이건 체인 중 "이전 페이즈"의 렌더러이다.
		/// </summary>
		public void SetChainedRenderer_Prev()
		{
			_isChained_AsPrev = true;
			//_lastMaskScreenSpaceOffsetForChained = Vector4.zero;
		}

		/// <summary>
		/// 이 렌더러가 이전 페이즈와 연결된 경우.
		/// 체인 중 "다음 페이즈"의 렌더러이다.
		/// </summary>
		public void SetChainedRenderer_Next()
		{
			_isChained_AsNext = true;
		}


		/// <summary>
		/// 이전 페이즈의 리시버가 이 렌더러의 메시와 동일하다면 체인이 되는 것이다.
		/// 이 렌더러의 타입에 따라 비교 코드가 다르다.
		/// </summary>
		public void CheckChainFromPrevReceiver(apOptMaskReceiver receiver)
		{
			if(receiver == null || receiver.ReceivedMesh == null)
			{
				return;
			}

			apOptMesh receiverMesh = receiver.ReceivedMesh;

			switch (_rendererType)
			{
				case RENDERER_TYPE.Clipping:
					{
						if(_request_Clipping != null)
						{
							if(_request_Clipping.Mesh == receiverMesh)
							{
								Debug.Log("Clipping Chain : " + _renderOrder);
								//체인되었다. = 이 렌더러는 리시버로부터 데이터를 받아서 업데이트한다.
								receiver.SetChainedRenderer(this, _material);
							}
						}
					}
					break;

				case RENDERER_TYPE.PerMesh:
					{
						if(_request_PerMesh != null)
						{
							if(_request_PerMesh.Mesh == receiverMesh)
							{
								//체인되었다.
								receiver.SetChainedRenderer(this, _material);
							}
						}
					}
					break;

				case RENDERER_TYPE.Shared:
					{
						//Shared는 여러개의 메시들이 있다.
						if(_requests_Shared != null && _nRequestsShared > 0)
						{
							Request_Shared curShared = null;
							for (int iShared = 0; iShared < _nRequestsShared; iShared++)
							{
								curShared = _requests_Shared[iShared];
								if(curShared.Mesh == receiverMesh)
								{	
									//체인되었다.
									receiver.SetChainedRenderer(this, curShared.Material);
								}
							}
						}
					}
					break;
			}
		}


		// Sub Functions
		//------------------------------------------------------------------
		private void CalculateMaskScreenSpaceOffset(	Camera camera, Transform camTransform,
														Transform meshTransform,
														Vector3 vertPosW_Center,
														Vector3 vertPosS_LT, Vector3 vertPosS_RB, Vector3 vertPosS_Center,
														float screenWidth, float screenHeight, Vector3 screenCenterOffset,
														out Matrix4x4 resultLocalToWorld,
														out Vector4 resultMaskScreenSpaceOffset)
		{
			if (!camera.orthographic)
            {
                Vector3 centerSceenPos = vertPosS_LT * 0.5f + vertPosS_RB * 0.5f;

                //영역 대각선의 길이의 절반
                float distLT2RB_Half = 0.5f * Mathf.Sqrt(
                    (vertPosS_LT.x - vertPosS_RB.x) * (vertPosS_LT.x - vertPosS_RB.x)
                    + (vertPosS_LT.y - vertPosS_RB.y) * (vertPosS_LT.y - vertPosS_RB.y));
                distLT2RB_Half *= 1.6f;

                vertPosS_LT.x = centerSceenPos.x - distLT2RB_Half;
                vertPosS_LT.y = centerSceenPos.y - distLT2RB_Half;

                vertPosS_RB.x = centerSceenPos.x + distLT2RB_Half;
                vertPosS_RB.y = centerSceenPos.y + distLT2RB_Half;
            }

			//Zoom 계산
            //모든 버텍스가 화면안에 들어온다면 Sceen 좌표계 Scale이 0~1의 값을 가진다.
            float prevSizeWidth = Mathf.Max(Mathf.Abs(vertPosS_LT.x - vertPosS_RB.x) / screenWidth, 0.001f);
            float prevSizeHeight = Mathf.Max(Mathf.Abs(vertPosS_LT.y - vertPosS_RB.y) / screenHeight, 0.001f);

            //화면에 가득 찰 수 있도록 확대하는 비율은 W, H 중에서 "덜 확대하는 비율 (Min 함수)"로 진행한다.
            float zoomScale = Mathf.Min(1.0f / prevSizeWidth, 1.0f / prevSizeHeight);

            //메시 자체를 평행이동하여 화면 중앙에 위치시켜야 한다.
            float aspectRatio = screenWidth / screenHeight;

			//변형된 Ortho 크기
			float newOrthoSizeY = 0.0f;
            if(camera.orthographic)
            {
				newOrthoSizeY = camera.orthographicSize / zoomScale;
            }
			else
			{
				//메시의 중심까지의 거리
				float zDepth = Mathf.Abs(camera.worldToCameraMatrix.MultiplyPoint3x4(vertPosW_Center).z);
				newOrthoSizeY = zDepth * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad) / zoomScale;
			}
			float newOrthoSizeX = aspectRatio * newOrthoSizeY;

			//다음 카메라 위치는 메시의 Center로부터 카메라가 바라보는 방향의 "역방향"으로 Ray를 쏠 때,
			//Ray*Dist 만큼의 위치
			float distCenterToCamera = Vector3.Distance(vertPosW_Center, camTransform.position);
			Vector3 nextCameraPos = vertPosW_Center - (camTransform.forward * distCenterToCamera);
				
            Matrix4x4 customWorldToCam = Matrix4x4.TRS(nextCameraPos, camTransform.rotation, Vector3.one).inverse;
			customWorldToCam.m20 *= -1f;
			customWorldToCam.m21 *= -1f;
			customWorldToCam.m22 *= -1f;
			customWorldToCam.m23 *= -1f;
				
			Matrix4x4 customCullingMatrix = Matrix4x4.Ortho(	-newOrthoSizeX,		//Left
																newOrthoSizeX,		//Right
																-newOrthoSizeY,		//Bottom
																newOrthoSizeY,		//Top
																distCenterToCamera - 10,	//Near
																distCenterToCamera + 50		//Far
															)
											* customWorldToCam;

			//오브젝트의 World Matrix를 다시 계산하자
			//다중 메시 연산시 이 부분만 건들면 될 듯
			Matrix4x4 newLocalToProj = customCullingMatrix * meshTransform.localToWorldMatrix;//Local > Proj를 만들고
			resultLocalToWorld = camera.cullingMatrix.inverse * newLocalToProj;//Proj에 Culling-inv를 곱하여 World 역계산

			//ScreenSpace가 얼마나 바뀌었는가를 저장하여 Child에 알려주자
			Vector3 screenPosOffset = new Vector3((screenWidth / 2), (screenHeight / 2), 0) - vertPosS_Center;

			resultMaskScreenSpaceOffset = new Vector4(	(screenPosOffset.x + screenCenterOffset.x) / screenWidth,
														(screenPosOffset.y + screenCenterOffset.y) / screenHeight,
														zoomScale, zoomScale);
		}



		// Get
		//------------------------------------------------------------------
		public RENDERER_TYPE RendererType { get { return _rendererType; } }

		public apSendMaskData.RT_RENDER_ORDER RenderOrder { get { return _renderOrder; } }

		//렌더 쉐이더 타입과 커스텀 에셋
		public apSendMaskData.RT_SHADER_TYPE ShaderType { get { return _RTShaderType; } }
		public Shader ShaderAsset { get { return _shader; } }

		//Shared RT의 ID
		public int SharedID {  get { return _sharedID; } }

        //RT의 크기 변수
        public int RenderTextureSize { get { return _textureSize; } }
        public bool IsDualRT { get { return _isDualRT; } }

        public bool IsUseEyeTextureSize()
        {
#if UNITY_2019_1_OR_NEWER
            return _isEyeTextureSize;
#else
            return false;
#endif
        }

		public bool IsVisible { get { return _isVisible; } }
		public apOptMaskRT LastMaskRT { get { return _lastUpdatedMaskRT; } }
		public Vector4 LastMaskScreenSpaceOffset { get { return _lastMaskScreenSpaceOffset; } }

		//체인된 경우
		//public Vector4 LastMaskScreenSpaceOffsetChained { get { return _lastMaskScreenSpaceOffsetForChained; } }
		


        // Sub-Class
        //------------------------------------
        //Info 클래스
        //타입에 따라 다른 Info 클래스를 갖는다.
        //경우에 따라 n개일 수도 있다.

        /// <summary>
        /// Clipping Parent에 의한 렌더링시 정보
        /// </summary>
        public class Request_Clipping
        {
            private apOptMesh _mesh = null;
            public Request_Clipping(apOptMesh targetMesh)
            {
                _mesh = targetMesh;
            }

            public apOptMesh Mesh { get { return _mesh; } }
        }

        /// <summary>
        /// 메시당 1개의 Mask 생성 요청시의 정보
        /// </summary>
        public class Request_PerMesh
        {
            private apOptMesh _mesh = null;
            private apOptSendMaskData _sendData = null;

            public Request_PerMesh(    apOptMesh targetMesh,
                                        apOptSendMaskData sendData)
            {
                _mesh = targetMesh;
                _sendData = sendData;
            }

            public apOptMesh Mesh { get { return _mesh; } }
            public apOptSendMaskData SendData { get { return _sendData; } }
        }

        /// <summary>
        /// 여러개의 메시가 Mask 생성 요청시의 연결 정보. 이 값은 List 타입으로 저장된다.
        /// </summary>
        public class Request_Shared
        {
            private apOptMesh _mesh = null;
            private apOptTransform _parentTransform = null;
            private apOptSendMaskData _sendData = null;

			//계산용 데이터
			//Optimized Area에서의 연산시 메시의 VertRange를 별도로 저장해야한다.
			//그 외에도 Visible, Texture, Color도 저장해두자
			public bool _cal_IsVisible = false;
			public Texture _cal_Texture = null;
			public Color _cal_MeshColor = Color.clear;

			//Shared의 경우는 Request가 각각의 Material을 가진다.
			private Material _cal_Material = null;
			private bool _isHasProp_MainTex = false;
			private bool _isHasProp_Color = false;
			private apSendMaskData.RT_SHADER_TYPE _RTShaderType = apSendMaskData.RT_SHADER_TYPE.CustomShader;

			public Request_Shared(  apOptMesh targetMesh,
                                    apOptSendMaskData sendData)
            {
                _mesh = targetMesh;
                _parentTransform = _mesh._parentTransform;
                _sendData = sendData;

				//초기 값은 null
				_cal_Material = null;
				_isHasProp_MainTex = false;
				_isHasProp_Color = false;
				_RTShaderType = sendData._rtShaderType;
            }

			public void SetMaterial(Material material, bool isHasProp_MainTex, bool isHasProperty_Color)
			{
				_cal_Material = material;
				_isHasProp_MainTex = isHasProp_MainTex;
				_isHasProp_Color = isHasProperty_Color;
			}

			public void SetMainTex(int propID_MainTex, Texture texture)
			{
				if (_isHasProp_MainTex && _cal_Material != null)
				{
					_cal_Material.SetTexture(propID_MainTex, texture);
				}
			}

			public void SetColor(int propID_Color, Color color)
			{
				if (_isHasProp_Color && _cal_Material != null)
				{
					_cal_Material.SetColor(propID_Color, color);
				}
			}

			public void CopyMeshProperties(apOptMesh srcMesh)
			{
				if(_cal_Material != null && srcMesh != null)
				{
					srcMesh.CopyMaterialToTarget(_cal_Material);
				}
			}


			public apOptMesh Mesh { get { return _mesh; } }
            public apOptSendMaskData SendData { get { return _sendData; } }

			public Material Material { get { return _cal_Material; } }
			public bool IsHasProp_MainTex { get { return _isHasProp_MainTex; } }
			public bool IsHasProp_Color { get { return _isHasProp_Color; } }

			public apSendMaskData.RT_SHADER_TYPE RTShaderType { get { return _RTShaderType; } }

			//렌더링을 위한 Sorting 값
			//Z는 내림차순, Sorting Order는 오름차순으로 렌더링을 하자
			public float GetSortingValue_LocalZ()
            {
                return _parentTransform._transform.localPosition.z;
            }
            
            public int GetSortingValue_SortingOrder()
            {
                return _mesh.GetSortingOrder();
            }
        }

        
        /// <summary>
        /// 렌더러에 속한 카메라 정보.
        /// 카메라에 등록할 RT와 커맨드 버퍼를 가지고 있다.
        /// 여기서는 RT와 커맨드 버퍼를 제공하면 Process에서 업데이트를 한다.
        /// apOptMaskRenderCameraUnit에 동기화 되므로, 해당 유닛으로부터 생성된다.
        /// </summary>
        public class CameraRTCmdUnit
        {
            // Members
            //---------------------------------------------
            private apOptMaskRenderer _parentRenderer = null;
            private apOptMaskRenderCameraUnit _linkedCameraUnit = null;

            //생성된 Mask RT
            private apOptMaskRT _maskRT = null;

            //커맨드 버퍼
            private CommandBuffer _cmdBuffer = null;
            
            //생성/삭제시 체크하는 값
            private bool _isChecked = false;

            // Init
            //---------------------------------------------
            public CameraRTCmdUnit( apOptMaskRenderer parentRenderer,
                                    apOptMaskRenderCameraUnit cameraUnit,
                                    string cmdBufferName)
            {
                _parentRenderer = parentRenderer;
                _linkedCameraUnit = cameraUnit;

                // RT 객체 생성 (실제로 RT 할당은 Make를 해야한다.)
                _maskRT = new apOptMaskRT(  _parentRenderer.RenderTextureSize,
                                            _parentRenderer.IsDualRT
#if UNITY_2019_1_OR_NEWER
                                            , _parentRenderer.IsUseEyeTextureSize()
#endif
                                            );

                // 커맨드 버퍼는 항상 생성해둔다.
                _cmdBuffer = new CommandBuffer();
                _cmdBuffer.name = cmdBufferName;
            }

            /// <summary>
            /// 마스크 RT를 생성한다.
            /// </summary>
            public void MakeRT()
            {
                _maskRT.Make();
            }

            /// <summary>
            /// 마스크 RT를 제거한다. 이 함수 전에 미리 이벤트를 제거하자
            /// </summary>
            public void ReleaseRT()
            {
                _maskRT.Release();
            }


			/// <summary>
			/// 커맨드 버퍼를 등록한다.
			/// (해제는 등록한 곳에서 전체 해제한다)
			/// </summary>
			public void AddCommandToLinkedCamera(apSendMaskData.RT_RENDER_ORDER renderOrder)
			{
				if(_linkedCameraUnit == null || _cmdBuffer == null)
				{
					return;
				}
				_linkedCameraUnit.AddCommandBufferToCamera(_cmdBuffer, renderOrder);
			}



            //생성 / 삭제를 위한 플래그 함수
            public void ReadyToCheck()
            {
                _isChecked = false;
            }


            public void SetCheck()
            {
                _isChecked = true;
            }

            public bool IsChecked()
            {
                return _isChecked;
            }


            // Get
            //-------------------------------------
            public apOptMaskRenderCameraUnit LinkedCameraUnit { get { return _linkedCameraUnit; } }
            public apOptMaskRT MaskRT { get { return _maskRT; } }
            public bool IsMaskRTCreated() { return _maskRT.IsCreated(); }
            public CommandBuffer CommandBuffer { get { return _cmdBuffer; } }
			
        }

    }
}