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

namespace AnyPortrait
{
    /// <summary>
    /// Mask Render Camera에 속한 객체로서, Mask를 받아서 렌더링을 하는 Child Mesh에 대한 정보이다.
	/// ReceiveLinkInfo를 등록하여 이벤트 콜백을 받기 위함이다.
	/// Renderer도 같이 참조한다.
	/// 코드는 MaskChild에 준한다.
    /// </summary>
    public class apOptMaskReceiver
    {
        // Members
		//---------------------------------------------------------------
		private apOptMesh _receivedMesh = null;//마스크를 받는 대상 메시
		private apOptMaskLinkInfo _linkInfo = null;

		//연결된 Renderer
		private apOptMaskRenderer _linkedRenderer = null;

		//SendData의 프로퍼티 Info의 래퍼.
		public class PropertySetInfo
		{
			public apOptSendMaskData.ReceivePropertySet _linkedPropSet = null;

			//프로퍼티 ID가 1개 혹은 여러개이다.
			//- Preset 타입은 비율, 텍스쳐, 연산 방식에 의해 ID가 3개이다. (나중에 Visible 관련 파라미터도 추가해야..)
			//- Custom 타입은 1개

			public apSendMaskData.SHADER_PROP_PRESET _presetType = apSendMaskData.SHADER_PROP_PRESET.Custom;

			//Alpha Mask 프리셋 타입인 경우
			public int _propID_AMPreset_Ratio = -1;//마스크 적용 비율 (첫 1회만 적용)
			public int _propID_AMPreset_Texture = -1;//마스크 텍스쳐
			public int _propID_AMPreset_Texture_L = -1;//마스크 텍스쳐 (VR의 L)
			public int _propID_AMPreset_Texture_R = -1;//마스크 텍스쳐 (VR의 R)
			public int _propID_AMPreset_ScreenSpaceOffset = -1;//마스크 영역의 화면 오프셋
			public int _propID_AMPreset_Op = -1;//연산자

			//See-Through 프리셋 타입인 경우
			public int _propID_STPreset_Ratio = -1;//마스크 적용 비율 (첫 1회만 적용)
			public int _propID_STPreset_Texture = -1;//마스크 텍스쳐
			public int _propID_STPreset_Texture_L = -1;//마스크 텍스쳐 (VR의 L)
			public int _propID_STPreset_Texture_R = -1;//마스크 텍스쳐 (VR의 R)
			public int _propID_STPreset_ScreenSpaceOffset = -1;//마스크 영역의 화면 오프셋
			public int _propID_STPreset_Alpha = -1;//마스크 텍스쳐

			//커스텀 타입인 경우
			public int _propID_Custom = -1;//커스텀

			//일부는 값을 미리 계산해두자
			public float _value_AMPreset_Op = 0.0f;//연산 옵션 (Preset인 경우에만)
			public float _value_STPreset_Alpha = 0.0f;

			//커스텀 값들
			public float _value_Custom_Float = 0.0f;
			public int _value_Custom_Int = 0;
			public Vector4 _value_Custom_Vector = Vector4.zero;
			public Texture _value_Custom_Texture = null;
			public Color _value_Custom_Color = Color.clear;
			
			public PropertySetInfo(apOptSendMaskData.ReceivePropertySet srcPropSet)
			{
				_linkedPropSet = srcPropSet;

				_propID_AMPreset_Ratio = -1;
				_propID_AMPreset_Texture = -1;
				_propID_AMPreset_Texture_L = -1;
				_propID_AMPreset_Texture_R = -1;
				_propID_AMPreset_ScreenSpaceOffset = -1;
				_propID_AMPreset_Op = -1;

				_propID_Custom = -1;

				_presetType = _linkedPropSet._preset;

				switch(_presetType)
				{
					case apSendMaskData.SHADER_PROP_PRESET.AlphaMaskPreset:
						{
							// [ Alpha Mask ]
							switch (_linkedPropSet._reservedChannel)
							{
								case apSendMaskData.SHADER_PROP_RESERVED_CHANNEL.Channel_1:
									{
										_propID_AMPreset_Ratio = Shader.PropertyToID("_MaskRatio_1");
										_propID_AMPreset_Texture = Shader.PropertyToID("_MaskTex_1");
										_propID_AMPreset_Texture_L = Shader.PropertyToID("_MaskTex_1_L");
										_propID_AMPreset_Texture_R = Shader.PropertyToID("_MaskTex_1_R");
										_propID_AMPreset_ScreenSpaceOffset = Shader.PropertyToID("_MaskScreenSpaceOffset_1");
										_propID_AMPreset_Op = Shader.PropertyToID("_MaskOp_1");
									}
									break;

								case apSendMaskData.SHADER_PROP_RESERVED_CHANNEL.Channel_2:
									{
										_propID_AMPreset_Ratio = Shader.PropertyToID("_MaskRatio_2");
										_propID_AMPreset_Texture = Shader.PropertyToID("_MaskTex_2");
										_propID_AMPreset_Texture_L = Shader.PropertyToID("_MaskTex_2_L");
										_propID_AMPreset_Texture_R = Shader.PropertyToID("_MaskTex_2_R");
										_propID_AMPreset_ScreenSpaceOffset = Shader.PropertyToID("_MaskScreenSpaceOffset_2");
										_propID_AMPreset_Op = Shader.PropertyToID("_MaskOp_2");
									}
									break;

								case apSendMaskData.SHADER_PROP_RESERVED_CHANNEL.Channel_3:
									{
										_propID_AMPreset_Ratio = Shader.PropertyToID("_MaskRatio_3");
										_propID_AMPreset_Texture = Shader.PropertyToID("_MaskTex_3");
										_propID_AMPreset_Texture_L = Shader.PropertyToID("_MaskTex_3_L");
										_propID_AMPreset_Texture_R = Shader.PropertyToID("_MaskTex_3_R");
										_propID_AMPreset_ScreenSpaceOffset = Shader.PropertyToID("_MaskScreenSpaceOffset_3");
										_propID_AMPreset_Op = Shader.PropertyToID("_MaskOp_3");
									}
									break;

								case apSendMaskData.SHADER_PROP_RESERVED_CHANNEL.Channel_4:
									{
										_propID_AMPreset_Ratio = Shader.PropertyToID("_MaskRatio_4");
										_propID_AMPreset_Texture = Shader.PropertyToID("_MaskTex_4");
										_propID_AMPreset_Texture_L = Shader.PropertyToID("_MaskTex_4_L");
										_propID_AMPreset_Texture_R = Shader.PropertyToID("_MaskTex_4_R");
										_propID_AMPreset_ScreenSpaceOffset = Shader.PropertyToID("_MaskScreenSpaceOffset_4");
										_propID_AMPreset_Op = Shader.PropertyToID("_MaskOp_4");
									}
									break;
							}

							//연산 옵션 값은 미리 저장
							_value_AMPreset_Op = apSendMaskData.MaskOperationToFloatValue(_linkedPropSet._value_MaskOp);
						}
						
						break;

					case apSendMaskData.SHADER_PROP_PRESET.SeeThroughPreset:
						{
							// [ See-Through ]
							_propID_STPreset_Ratio = Shader.PropertyToID("_SeeThroughRatio");
							_propID_STPreset_Texture = Shader.PropertyToID("_SeeThroughTex");
							_propID_STPreset_Texture_L = Shader.PropertyToID("_SeeThroughTex_L");
							_propID_STPreset_Texture_R = Shader.PropertyToID("_SeeThroughTex_R");
							_propID_STPreset_ScreenSpaceOffset = Shader.PropertyToID("_SeeThroughScreenSpaceOffset");
							_propID_STPreset_Alpha = Shader.PropertyToID("_SeeThroughAlpha");

							//기본 Alpha 값을 미리 지정
							_value_STPreset_Alpha = Mathf.Clamp01(_linkedPropSet._value_Float);
						}
						break;

					case apSendMaskData.SHADER_PROP_PRESET.Custom:
					default:
						{
							// [ Custom ]
							_propID_Custom = Shader.PropertyToID(_linkedPropSet._customName);

							switch (_linkedPropSet._customPropType)
							{
								case apSendMaskData.SHADER_PROP_VALUE_TYPE.RenderTexture:
								case apSendMaskData.SHADER_PROP_VALUE_TYPE.ScreenSpaceOffset:
									// 이 타입들은 값을 미리 지정하지 않는다.
									break;

								case apSendMaskData.SHADER_PROP_VALUE_TYPE.MaskOp:
									{
										//연산 값은 프리셋과 동일. 다만 저장은 Float에 한다.
										switch (_linkedPropSet._value_MaskOp)
										{
											case apSendMaskData.MASK_OPERATION.And: _value_Custom_Float = 0.0f; break;
											case apSendMaskData.MASK_OPERATION.Or: _value_Custom_Float = 1.0f; break;
											case apSendMaskData.MASK_OPERATION.InverseAnd: _value_Custom_Float = 2.0f; break;
											case apSendMaskData.MASK_OPERATION.InverseOr: _value_Custom_Float = 3.0f; break;
										}
									}
									break;

								case apSendMaskData.SHADER_PROP_VALUE_TYPE.CalculatedColor:
									//계산된 색상은 미리 지정하지 않는다.
									break;

								//임의의 값. 일부 타입은 컨트롤 파라미터와 연결할 수 있디만 "기본값"으로서 일단 저장해둔다.
								case apSendMaskData.SHADER_PROP_VALUE_TYPE.Value_Float:
									_value_Custom_Float = _linkedPropSet._value_Float;
									break;

								case apSendMaskData.SHADER_PROP_VALUE_TYPE.Value_Int:
									_value_Custom_Int = _linkedPropSet._value_Int;
									break;

								case apSendMaskData.SHADER_PROP_VALUE_TYPE.Value_Vector:
									_value_Custom_Vector = _linkedPropSet._value_Vector;
									break;

								case apSendMaskData.SHADER_PROP_VALUE_TYPE.Value_Texture:
									_value_Custom_Texture = _linkedPropSet._value_Texture;
									break;

								case apSendMaskData.SHADER_PROP_VALUE_TYPE.Value_Color:
									_value_Custom_Color = _linkedPropSet._value_Color;
									break;

								case apSendMaskData.SHADER_PROP_VALUE_TYPE.RenderTexture_VR_Left:
								case apSendMaskData.SHADER_PROP_VALUE_TYPE.RenderTexture_VR_Right:
									//이 값들도 자동 생성하여 전송한다.
									break;
							}
						}
						break;
				}
			}
		}
		private PropertySetInfo[] _props = null;
		private int _nProps = 0;


		//Receiver > Renderer 체인
		//동일한 메시에 대해서 이 Receiver보다 나중에 실행되는 Renderer는 이 Receiver의 영향을 받게 된다.
		//일반적으로는 Material 값을 그대로 복사하면 되지만, RT의 해상도를 활용하는 Mask Screen Space Offset 불일치 문제가 발생한다.
		//- Receiver가 받는 MSSO는 화면 해상도를 기준으로 계산된다.
		//- 체인된 다음 Phase의 Renderer는 1:1 비율의 RT를 기준으로 계산되므로 현재 Receiver가 받은 MSSO를 활용할 수 없다.
		//따라서 체인 여부를 한번 확인한 후, 연결된 렌더러 (_linkedRenderer)에 1:1 비율의 RT를 추가로 생성할지를 요청하자
		//그리고 그 값을 체인된 다음 렌더러에 전달하자.

		private class ChainInfo
		{
			private apOptMaskRenderer _chainedRenderer = null;
			private Material _chainedMaterial = null;//렌더러가 Shared인 경우는 여러개의 메시들이 한번에 렌더링되므로, 아예 해당 메시를 렌더링하는 Material로 받아오자.

			//private HashSet<int> _validPropSets = new HashSet<int>();
			private class PropValidationInfo
			{
				public bool _isValid = false;
				public PropValidationInfo(bool isValid)
				{
					_isValid = isValid;
				}
			}
			private Dictionary<int, PropValidationInfo> _propSetValidation = new Dictionary<int, PropValidationInfo>();

			
			public apOptMaskRenderer ChainedRenderer { get { return _chainedRenderer; } }

			public ChainInfo(apOptMaskRenderer chainedRenderer, Material chainedMaterial)
			{
				_chainedRenderer = chainedRenderer;
				_chainedMaterial = chainedMaterial;
				//if(_validPropSets == null)
				//{
				//	_validPropSets = new HashSet<int>();
				//}
				//_validPropSets.Clear();

				if(_propSetValidation == null)
				{
					_propSetValidation = new Dictionary<int, PropValidationInfo>();
				}
				_propSetValidation.Clear();

				if(_chainedMaterial != null)
				{
					//Reserved 프로퍼티 몇개를 적용할 수 있는지 검토하자
					ValidateProp("_MainTex");
					ValidateProp("_Color");
					ValidateProp("_MaskTex");
					ValidateProp("_MaskScreenSpaceOffset");
					ValidateProp("_MaskTex_L");
					ValidateProp("_MaskTex_R");
					ValidateProp("_MaskRatio");
				}
			}

			private void ValidateProp(string propName)
			{
				int propID = Shader.PropertyToID(propName);

				//if(_validPropSets.Contains(propID))
				//{
				//	//이미 체크함 (유효함)
				//	return;
				//}

				PropValidationInfo valid = null;
				_propSetValidation.TryGetValue(propID, out valid);

				if (valid != null)
				{
					//이미 유효성을 체크함
					return;
				}

				bool isValid = _chainedMaterial.HasProperty(propID);//유효성을 체크한다.

				//유효성 결과를 넣자
				_propSetValidation.Add(propID, new PropValidationInfo(isValid));
			}

			//private void ValidateProp(int propID)
			//{
			//	if (_validPropSets.Contains(propID))
			//	{
			//		//이미 체크함 (유효함)
			//		return;
			//	}
			//	if (_chainedMaterial.HasProperty(propID))
			//	{
			//		//이건 유효한 프로퍼티다.
			//		_validPropSets.Add(propID);
			//	}
			//}

			private bool IsValidProp(int propID)
			{
				PropValidationInfo valid = null;
				_propSetValidation.TryGetValue(propID, out valid);

				if(valid != null)
				{
					//유효성 결과가 저장되어 있다면
					return valid._isValid;
				}

				//유효성을 검사한 후 리스트에 넣은 뒤 리턴
				bool isValid = _chainedMaterial.HasProperty(propID);
				_propSetValidation.Add(propID, new PropValidationInfo(isValid));
				return isValid;
			}

			////연결된 렌더러로 어떤 프로퍼티를 전송할 수 있을지 판단하자
			//public void AddValidPropSet(PropertySetInfo propSetInfo)
			//{
			//	switch (propSetInfo._presetType)
			//	{
			//		case apSendMaskData.SHADER_PROP_PRESET.AlphaMaskPreset:
			//			{
			//				//Alpha Mask에서의 프로퍼티들의 유효성을 체크한다.
			//				ValidateProp(propSetInfo._propID_AMPreset_Ratio);
			//				ValidateProp(propSetInfo._propID_AMPreset_Texture);
			//				ValidateProp(propSetInfo._propID_AMPreset_Texture_L);
			//				ValidateProp(propSetInfo._propID_AMPreset_Texture_R);
			//				ValidateProp(propSetInfo._propID_AMPreset_ScreenSpaceOffset);
			//				ValidateProp(propSetInfo._propID_AMPreset_Op);
			//			}
			//			break;

			//		case apSendMaskData.SHADER_PROP_PRESET.SeeThroughPreset:
			//			{
			//				//See-Through에서의 프로퍼티들의 유효성을 체크한다.
			//				ValidateProp(propSetInfo._propID_STPreset_Ratio);
			//				ValidateProp(propSetInfo._propID_STPreset_Texture);
			//				ValidateProp(propSetInfo._propID_STPreset_Texture_L);
			//				ValidateProp(propSetInfo._propID_STPreset_Texture_R);
			//				ValidateProp(propSetInfo._propID_STPreset_ScreenSpaceOffset);
			//				ValidateProp(propSetInfo._propID_STPreset_Alpha);
			//			}
			//			break;

			//		case apSendMaskData.SHADER_PROP_PRESET.Custom:
			//			{
			//				//커스텀인 경우
			//				ValidateProp(propSetInfo._propID_Custom);
			//			}
			//			break;
			//	}
			//}

			//Receiver의 프로퍼티 전송
			public void SetProp_Float(int propID, float propValue)
			{
				if(!IsValidProp(propID))
				{
					return;
				}
				_chainedMaterial.SetFloat(propID, propValue);
			}

			public void SetProp_Vector4(int propID, Vector4 propValue)
			{
				if(!IsValidProp(propID))
				{
					return;
				}
				_chainedMaterial.SetVector(propID, propValue);
			}

			public void SetProp_Vector2(int propID, Vector2 propValue)
			{
				if(!IsValidProp(propID))
				{
					return;
				}
				_chainedMaterial.SetVector(propID, new Vector4(propValue.x, propValue.y, 0.0f, 0.0f));
			}

			public void SetProp_Texture(int propID, Texture propValue)
			{
				if(!IsValidProp(propID))
				{
					return;
				}
				_chainedMaterial.SetTexture(propID, propValue);
			}

			public void SetProp_Color(int propID, Color propValue)
			{
				if(!IsValidProp(propID))
				{
					return;
				}
				_chainedMaterial.SetColor(propID, propValue);
			}

			public void SetProp_Int(int propID, int propValue)
			{
				if(!IsValidProp(propID))
				{
					return;
				}

#if UNITY_2021_1_OR_NEWER
				_chainedMaterial.SetInteger(propID, propValue);
				
#else
				_chainedMaterial.SetInt(propID, propValue);
#endif
			}

			
		}

		private bool _isChained = false;

		private List<ChainInfo> _chainedInfo = null;
		private int _nChaindInfos = 0;

		//일부 프로퍼티 ID는 미리 가지고 있자
		private int _reservedPropID_MaskTex = -1;
		private int _reservedPropID_MaskTex_L = -1;
		private int _reservedPropID_MaskTex_R = -1;
		private int _reservedPropID_MaskRatio = -1;
		private int _reservedPropID_MaskScreenSpaceOffset = -1;




		// Init
		//------------------------------------------------------------
		public apOptMaskReceiver(	apOptMesh receiveMesh,
									apOptMaskLinkInfo linkInfo,
									apOptMaskRenderer linkedRenderer)
		{
			_receivedMesh = receiveMesh;
			_linkInfo = linkInfo;

			_props = null;
			_nProps = 0;

			_isChained = false;
			_chainedInfo = null;
			_nChaindInfos = 0;

			if (_linkInfo.LinkType == apOptMaskLinkInfo.LINK_TYPE.SendData
				&& _linkInfo.ReceivedSendData != null)
			{
				List<apOptSendMaskData.ReceivePropertySet> srcProps = _linkInfo.ReceivedSendData._propertySets;
				int nSrcProps = srcProps != null ? srcProps.Count : 0;
				if(nSrcProps > 0)
				{
					_nProps = nSrcProps;
					_props = new PropertySetInfo[_nProps];
					for (int i = 0; i < _nProps; i++)
					{
						_props[i] = new PropertySetInfo(srcProps[i]);
					}
				}
			}

			_linkedRenderer = linkedRenderer;

			//일부 PropID는 별도로 저장한다.
			_reservedPropID_MaskTex = Shader.PropertyToID("_MaskTex");
			_reservedPropID_MaskTex_L = Shader.PropertyToID("_MaskTex_L");
			_reservedPropID_MaskTex_R = Shader.PropertyToID("_MaskTex_R");
			_reservedPropID_MaskRatio = Shader.PropertyToID("_MaskRatio");
			_reservedPropID_MaskScreenSpaceOffset = Shader.PropertyToID("_MaskScreenSpaceOffset");
		}


		// Chained Renderers
		//------------------------------------------------------------
		public void SetChainedRenderer(apOptMaskRenderer nextPhaseChainedRenderer, Material chainedMaterial)
		{
			if(nextPhaseChainedRenderer == null || nextPhaseChainedRenderer == _linkedRenderer)
			{
				//잘못된 요청
				return;
			}

			_isChained = true;
			if(_chainedInfo == null)
			{
				_chainedInfo = new List<ChainInfo>();
			}

			ChainInfo newChain = new ChainInfo(nextPhaseChainedRenderer, chainedMaterial);

			//프로퍼티를 입력
			


			//리스트에 추가
			_chainedInfo.Add(newChain);
			_nChaindInfos = _chainedInfo.Count;

			//하나라도 체인되었다면
			//이 리시버의 Parent 렌더러에도 체인용 MaskScreenSpaceOffset을 생성해달라고 요청하자
			if (_linkedRenderer != null)
			{
				_linkedRenderer.SetChainedRenderer_Prev();
			}

			if(nextPhaseChainedRenderer != null)
			{
				nextPhaseChainedRenderer.SetChainedRenderer_Next();
			}

			//Debug.Log("Set Chained Renderer (Receiver에서 호출) : " + _receivedMesh.gameObject.name);
		}


		// Update
		//------------------------------------------------------------
		// 단일 카메라에서의 업데이트 (MultiCam에서도 활용 가능하다.)
		public void Update_Basic()
		{
			Update(false);//false : Stereo 카메라가 아니다.
		}

		// VR 카메라에서의 업데이트
		public void Update_SingleVR()
		{
			Update(true);//true : Stereo 카메라이다.
		}


		private void Update(bool isStereoCamera)
		{

			//연결 타입에 따라서 데이터를 전달하는게 다르다.
			switch (_linkInfo.LinkType)
			{
				case apOptMaskLinkInfo.LINK_TYPE.Clipping:
					{
						// [ Clipping ]
						//- 고정적으로 MaskRT와 MaskSpaceScreenOffset을 전달한다.
						if(isStereoCamera)
						{
							//스테레오 카메라 (VR)인 경우
							RenderTexture resultRT_L = GetLastRenderTexture_L();
							RenderTexture resultRT_R = GetLastRenderTexture_R();

							//클리핑 연산 결과를 전달하자
							_receivedMesh.ReceiveMask_Clipped_VR(	_linkedRenderer.IsVisible,
																	resultRT_L, resultRT_R,
																	_linkedRenderer.LastMaskScreenSpaceOffset);
						}
						else
						{
							//싱글 카메라의 경우
							RenderTexture resultRT = GetLastRenderTexture_Single();

							//클리핑 연산 결과를 전달하자
							_receivedMesh.ReceiveMask_Clipped(	_linkedRenderer.IsVisible,
																resultRT,
																_linkedRenderer.LastMaskScreenSpaceOffset);

							//if(_isChained)
							//{
							//	int nChained = _chainedInfo != null ? _chainedInfo.Count : 0;
							//	for (int i = 0; i < nChained; i++)
							//	{	
							//		_chainedInfo[i].SetProp_Vector4("_MaskScreenSpaceOffset", _linkedRenderer.LastMaskScreenSpaceOffset);
							//		_chainedInfo[i].SetProp_Float("_MaskRatio", 1.0f);
							//		_chainedInfo[i].SetProp_Texture("_MaskTex", _linkedRenderer.LastMaskRT.RenderTexture_Single);
									
							//	}
							//	//Debug.Log("체인 : 기존 MSSO : " + _linkedRenderer.LastMaskScreenSpaceOffset + " > 체인용 MSSO : " + _linkedRenderer.LastMaskScreenSpaceOffsetChained);

							//}
						}

						if(_isChained)
						{
							//체인된 경우 클리핑값을 다음 렌더러 재질에 전달
							CopyPropToNextChain_Clipping(isStereoCamera);
						}
					}
					break;

				case apOptMaskLinkInfo.LINK_TYPE.SendData:
					{
						// [ Send Data ]
						//- SendData의 PropertyList로부터 받은 정보를 바탕으로 값을 전달한다.
						if(_nProps == 0)
						{
							break;
						}

						PropertySetInfo curProp = null;
						apOptSendMaskData.ReceivePropertySet linkedPropSet = null;
						for (int i = 0; i < _nProps; i++)
						{
							curProp = _props[i];
							linkedPropSet = curProp._linkedPropSet;

							switch(curProp._presetType)
							{
								case apSendMaskData.SHADER_PROP_PRESET.AlphaMaskPreset:
									{
										// [ Alpha Mask ]
										//- 다음의 값을 한번에 전송한다.
										//1. 적용 여부에 따른 Ratio = 1
										//2. 마스크 텍스쳐
										//3. 연산자
										if(isStereoCamera)
										{
											//스테레오 카메라 (VR)인 경우
											RenderTexture resultRT_L = GetLastRenderTexture_L();
											RenderTexture resultRT_R = GetLastRenderTexture_R();

											_receivedMesh.ReceiveMask_SendData_AlphaMaskPreset_VR(
																		curProp._propID_AMPreset_Ratio, 1.0f,
																		curProp._propID_AMPreset_Texture_L, resultRT_L,
																		curProp._propID_AMPreset_Texture_R, resultRT_R,
																		curProp._propID_AMPreset_ScreenSpaceOffset, _linkedRenderer.LastMaskScreenSpaceOffset,
																		curProp._propID_AMPreset_Op, curProp._value_AMPreset_Op);
										}
										else
										{
											RenderTexture resultRT = GetLastRenderTexture_Single();

											_receivedMesh.ReceiveMask_SendData_AlphaMaskPreset(
																		curProp._propID_AMPreset_Ratio, 1.0f,
																		curProp._propID_AMPreset_Texture, resultRT,
																		curProp._propID_AMPreset_ScreenSpaceOffset, _linkedRenderer.LastMaskScreenSpaceOffset,
																		curProp._propID_AMPreset_Op, curProp._value_AMPreset_Op);
										}
										
										if(_isChained)
										{
											//체인된 경우 클리핑값을 다음 렌더러 재질에 전달
											CopyPropToNextChain_AlphaMask(curProp, isStereoCamera);
										}
									}
									break;

								case apSendMaskData.SHADER_PROP_PRESET.SeeThroughPreset:
									{
										// [ See-Through ]
										//- 다음의 값을 한번에 전송한다.
										//1. 적용 여부에 따른 Ratio = 1
										//2. 마스크 텍스쳐
										//3. Alpha값 (컨트롤 파라미터일 수 있음)

										//Alpha값을 먼저 계산
										float stAlpha = Mathf.Clamp01(curProp._value_STPreset_Alpha);
										if(linkedPropSet._value_IsUseControlParam && linkedPropSet._value_LinkedControlParam != null)
										{
											apControlParam cp = linkedPropSet._value_LinkedControlParam;
											if(cp._valueType == apControlParam.TYPE.Float)
											{
												stAlpha = Mathf.Clamp01(cp.FloatValue);
											}
										}

										if(isStereoCamera)
										{
											//스테레오 (VR) 카메라인 경우
											RenderTexture resultRT_L = GetLastRenderTexture_L();
											RenderTexture resultRT_R = GetLastRenderTexture_R();

											_receivedMesh.ReceiveMask_SendData_SeeThroughPreset_VR(	curProp._propID_STPreset_Ratio, 1.0f,
																									curProp._propID_STPreset_Texture_L, resultRT_L,
																									curProp._propID_STPreset_Texture_R, resultRT_R,
																									curProp._propID_STPreset_ScreenSpaceOffset, _linkedRenderer.LastMaskScreenSpaceOffset,
																									curProp._propID_STPreset_Alpha, stAlpha);
										}
										else
										{
											//일반 카메라인 경우
											RenderTexture resultRT = GetLastRenderTexture_Single();

											_receivedMesh.ReceiveMask_SendData_SeeThroughPreset(	curProp._propID_STPreset_Ratio, 1.0f,
																									curProp._propID_STPreset_Texture, resultRT,
																									curProp._propID_STPreset_ScreenSpaceOffset, _linkedRenderer.LastMaskScreenSpaceOffset,
																									curProp._propID_STPreset_Alpha, stAlpha);
										}

										if(_isChained)
										{
											//체인된 경우 클리핑값을 다음 렌더러 재질에 전달
											CopyPropToNextChain_SeeThrough(curProp, stAlpha, isStereoCamera);
										}
									}
									break;

								case apSendMaskData.SHADER_PROP_PRESET.Custom:
								default:
									{
										// [ Custom ]
										//- 타입 상태에 따라 값을 전달한다.
										switch (linkedPropSet._customPropType)
										{
											case apSendMaskData.SHADER_PROP_VALUE_TYPE.RenderTexture:
												{
													//자동 생성된 마스크 RT (Single)
													RenderTexture resultRenderTexture = GetLastRenderTexture_Single();
													_receivedMesh.ReceiveMask_SendData_RenderTexture(curProp._propID_Custom, resultRenderTexture);

													if(_isChained)
													{
														//체인된 경우 클리핑값을 다음 렌더러 재질에 전달
														CopyPropToNextChain_Custom_SetProp_Texture(curProp, resultRenderTexture);
													}
												}
												break;
											case apSendMaskData.SHADER_PROP_VALUE_TYPE.ScreenSpaceOffset:
												{
													//자동 생성된 마스크의 영역 최적화 정보
													_receivedMesh.ReceiveMask_SendData_Vector4(curProp._propID_Custom, _linkedRenderer.LastMaskScreenSpaceOffset);

													if(_isChained)
													{
														//체인된 경우 클리핑값을 다음 렌더러 재질에 전달
														CopyPropToNextChain_Custom_SetProp_Vector4(curProp, _linkedRenderer.LastMaskScreenSpaceOffset);
													}
												}
												break;

											case apSendMaskData.SHADER_PROP_VALUE_TYPE.MaskOp:
												{
													//마스크 연산 방식. (미리 저장된 값 이용)
													_receivedMesh.ReceiveMask_SendData_Float(curProp._propID_Custom, curProp._value_Custom_Float);

													if(_isChained)
													{
														//체인된 경우 클리핑값을 다음 렌더러 재질에 전달
														CopyPropToNextChain_Custom_SetProp_Float(curProp, curProp._value_Custom_Float);
													}
												}
												break;

											case apSendMaskData.SHADER_PROP_VALUE_TYPE.CalculatedColor:
												{
													//계산된 색상. Send한 Mesh의 색상을 전달한다.
													if(curProp._linkedPropSet.ParentOptMesh != null)
													{
														Color calculatedColor = curProp._linkedPropSet.ParentOptMesh.MeshColor;
														_receivedMesh.ReceiveMask_SendData_Color(curProp._propID_Custom, calculatedColor);

														if(_isChained)
														{
															//체인된 경우 클리핑값을 다음 렌더러 재질에 전달
															CopyPropToNextChain_Custom_SetProp_Color(curProp, calculatedColor);
														}
													}
												}
												break;

											case apSendMaskData.SHADER_PROP_VALUE_TYPE.Value_Float:
												{
													//컨트롤 파라미터 또는 지정했던 Float 값을 전달
													float floatValue = curProp._value_Custom_Float;

													if (linkedPropSet._value_IsUseControlParam && linkedPropSet._value_LinkedControlParam != null)
													{
														apControlParam cp = linkedPropSet._value_LinkedControlParam;
														if(cp._valueType == apControlParam.TYPE.Float)
														{
															floatValue = cp.FloatValue;
														}
													}

													_receivedMesh.ReceiveMask_SendData_Float(curProp._propID_Custom, floatValue);		
													
													if(_isChained)
													{
														//체인된 경우 클리핑값을 다음 렌더러 재질에 전달
														CopyPropToNextChain_Custom_SetProp_Float(curProp, floatValue);
													}
												}
												break;

											case apSendMaskData.SHADER_PROP_VALUE_TYPE.Value_Int:
												{
													//컨트롤 파라미터 또는 지정했던 Int 값을 전달
													int intValue = curProp._value_Custom_Int;

													if(linkedPropSet._value_IsUseControlParam && linkedPropSet._value_LinkedControlParam != null)
													{
														apControlParam cp = linkedPropSet._value_LinkedControlParam;
														if(cp._valueType == apControlParam.TYPE.Int)
														{
															intValue = cp.IntValue;
														}
													}

													_receivedMesh.ReceiveMask_SendData_Int(curProp._propID_Custom, intValue);

													if(_isChained)
													{
														//체인된 경우 클리핑값을 다음 렌더러 재질에 전달
														CopyPropToNextChain_Custom_SetProp_Int(curProp, intValue);
													}
												}
												break;

											case apSendMaskData.SHADER_PROP_VALUE_TYPE.Value_Vector:
												{
													//컨트롤 파라미터 또는 지정했던 Vector 값을 전달
													Vector4 vec4Value = curProp._value_Custom_Vector;
													
													if(linkedPropSet._value_IsUseControlParam && linkedPropSet._value_LinkedControlParam != null)
													{
														apControlParam cp = linkedPropSet._value_LinkedControlParam;
														if(cp._valueType == apControlParam.TYPE.Vector2)
														{
															vec4Value.x = cp.Vector2Value.x;
															vec4Value.y = cp.Vector2Value.y;
															vec4Value.z = 0.0f;
															vec4Value.w = 0.0f;
														}
													}

													_receivedMesh.ReceiveMask_SendData_Vector4(curProp._propID_Custom, vec4Value);

													if(_isChained)
													{
														//체인된 경우 클리핑값을 다음 렌더러 재질에 전달
														CopyPropToNextChain_Custom_SetProp_Vector4(curProp, vec4Value);
													}
												}
												break;

											case apSendMaskData.SHADER_PROP_VALUE_TYPE.Value_Texture:
												{
													//지정했던 Texture 값을 전달
													_receivedMesh.ReceiveMask_SendData_Texture(curProp._propID_Custom, curProp._value_Custom_Texture);

													if(_isChained)
													{
														//체인된 경우 클리핑값을 다음 렌더러 재질에 전달
														CopyPropToNextChain_Custom_SetProp_Texture(curProp, curProp._value_Custom_Texture);
													}
												}
												break;

											case apSendMaskData.SHADER_PROP_VALUE_TYPE.Value_Color:
												{
													//지정했던 Color 값을 전달
													_receivedMesh.ReceiveMask_SendData_Color(curProp._propID_Custom, curProp._value_Custom_Color);

													if(_isChained)
													{
														//체인된 경우 클리핑값을 다음 렌더러 재질에 전달
														CopyPropToNextChain_Custom_SetProp_Color(curProp, curProp._value_Custom_Color);
													}
												}
												break;
											case apSendMaskData.SHADER_PROP_VALUE_TYPE.RenderTexture_VR_Left:
												{
													//자동 생성된 VR용 마스크 RT 중 Left
													RenderTexture resultRenderTextureL = GetLastRenderTexture_L();
													_receivedMesh.ReceiveMask_SendData_RenderTexture(curProp._propID_Custom, resultRenderTextureL);

													if(_isChained)
													{
														//체인된 경우 클리핑값을 다음 렌더러 재질에 전달
														CopyPropToNextChain_Custom_SetProp_Texture(curProp, resultRenderTextureL);
													}
												}
												break;

											case apSendMaskData.SHADER_PROP_VALUE_TYPE.RenderTexture_VR_Right:
												{
													//자동 생성된 VR용 마스크 RT 중 Right
													RenderTexture resultRenderTextureR = GetLastRenderTexture_R();
													_receivedMesh.ReceiveMask_SendData_RenderTexture(curProp._propID_Custom, resultRenderTextureR);

													if(_isChained)
													{
														//체인된 경우 클리핑값을 다음 렌더러 재질에 전달
														CopyPropToNextChain_Custom_SetProp_Texture(curProp, resultRenderTextureR);
													}
												}
												break;
										}
									}
									break;
							}
						}
					}
					break;
			}			
		}



		private RenderTexture GetLastRenderTexture_Single()
		{
			if(_linkedRenderer.LastMaskRT != null)
			{
				return _linkedRenderer.LastMaskRT.RenderTexture_Single;
			}

			return null;
		}

		private RenderTexture GetLastRenderTexture_L()
		{
			if(_linkedRenderer.LastMaskRT != null)
			{
				return _linkedRenderer.LastMaskRT.RenderTexture_L;
			}
			return null;
		}

		private RenderTexture GetLastRenderTexture_R()
		{
			if(_linkedRenderer.LastMaskRT != null)
			{
				return _linkedRenderer.LastMaskRT.RenderTexture_R;
			}
			return null;
		}
		

		//체인에 프로퍼티 적용
		//------------------------------------------------------------------------
		private void CopyPropToNextChain_Clipping(bool isStereoCamera)
		{
			if (_nChaindInfos == 0)
			{
				return;
			}

			//체인된 렌더러에 프로퍼티를 복사하자
			RenderTexture resultRT = null;
			RenderTexture resultRT_L = null;
			RenderTexture resultRT_R = null;
			if (isStereoCamera)
			{
				resultRT_L = GetLastRenderTexture_L();
				resultRT_R = GetLastRenderTexture_R();
			}
			else
			{
				resultRT = GetLastRenderTexture_Single();
			}
			ChainInfo curChain = null;
			for (int i = 0; i < _nChaindInfos; i++)
			{
				curChain = _chainedInfo[i];
				if (curChain == null)
				{
					continue;
				}
				
				if (isStereoCamera)
				{
					//스테레오 카메라 (VR)인 경우
					curChain.SetProp_Float(_reservedPropID_MaskRatio, 1.0f);
					curChain.SetProp_Texture(_reservedPropID_MaskTex_L, resultRT_L);
					curChain.SetProp_Texture(_reservedPropID_MaskTex_R, resultRT_R);
					curChain.SetProp_Vector4(_reservedPropID_MaskScreenSpaceOffset, _linkedRenderer.LastMaskScreenSpaceOffset);
				}
				else
				{
					curChain.SetProp_Float(_reservedPropID_MaskRatio, 1.0f);
					curChain.SetProp_Texture(_reservedPropID_MaskTex, resultRT);
					curChain.SetProp_Vector4(_reservedPropID_MaskScreenSpaceOffset, _linkedRenderer.LastMaskScreenSpaceOffset);
				}
			}
		}


		private void CopyPropToNextChain_AlphaMask(PropertySetInfo propSet, bool isStereoCamera)
		{
			if(_nChaindInfos == 0)
			{
				return;
			}
			
			//체인된 렌더러에 프로퍼티를 복사하자
			RenderTexture resultRT = null;
			RenderTexture resultRT_L = null;
			RenderTexture resultRT_R = null;
			if(isStereoCamera)
			{
				resultRT_L = GetLastRenderTexture_L();
				resultRT_R = GetLastRenderTexture_R();
			}
			else
			{
				resultRT = GetLastRenderTexture_Single();
			}

			ChainInfo curChain = null;
			for (int i = 0; i < _nChaindInfos; i++)
			{
				curChain = _chainedInfo[i];
				if (curChain == null)
				{
					continue;
				}
				if (isStereoCamera)
				{
					//스테레오 카메라 (VR)인 경우
					
					curChain.SetProp_Float(propSet._propID_AMPreset_Ratio, 1.0f);
					curChain.SetProp_Texture(propSet._propID_AMPreset_Texture_L, resultRT_L);
					curChain.SetProp_Texture(propSet._propID_AMPreset_Texture_R, resultRT_R);
					curChain.SetProp_Vector4(propSet._propID_AMPreset_ScreenSpaceOffset, _linkedRenderer.LastMaskScreenSpaceOffset);
					curChain.SetProp_Float(propSet._propID_AMPreset_Op, propSet._value_AMPreset_Op);
				}
				else
				{
					curChain.SetProp_Float(propSet._propID_AMPreset_Ratio, 1.0f);
					curChain.SetProp_Texture(propSet._propID_AMPreset_Texture, resultRT);
					curChain.SetProp_Vector4(propSet._propID_AMPreset_ScreenSpaceOffset, _linkedRenderer.LastMaskScreenSpaceOffset);
					curChain.SetProp_Float(propSet._propID_AMPreset_Op, propSet._value_AMPreset_Op);
				}
			}
		}

		private void CopyPropToNextChain_SeeThrough(PropertySetInfo propSet, float stAlpha, bool isStereoCamera)
		{
			if (_nChaindInfos == 0)
			{
				return;
			}
			//체인된 렌더러에 프로퍼티를 복사하자
			RenderTexture resultRT = null;
			RenderTexture resultRT_L = null;
			RenderTexture resultRT_R = null;
			if(isStereoCamera)
			{
				resultRT_L = GetLastRenderTexture_L();
				resultRT_R = GetLastRenderTexture_R();
			}
			else
			{
				resultRT = GetLastRenderTexture_Single();
			}

			ChainInfo curChain = null;
			for (int i = 0; i < _nChaindInfos; i++)
			{
				curChain = _chainedInfo[i];
				if (curChain == null)
				{
					continue;
				}
				if (isStereoCamera)
				{
					//스테레오 카메라 (VR)인 경우
					curChain.SetProp_Float(propSet._propID_STPreset_Ratio, 1.0f);
					curChain.SetProp_Texture(propSet._propID_STPreset_Texture_L, resultRT_L);
					curChain.SetProp_Texture(propSet._propID_STPreset_Texture_R, resultRT_R);
					curChain.SetProp_Vector4(propSet._propID_STPreset_ScreenSpaceOffset, _linkedRenderer.LastMaskScreenSpaceOffset);
					curChain.SetProp_Float(propSet._propID_STPreset_Alpha, stAlpha);
				}
				else
				{
					curChain.SetProp_Float(propSet._propID_STPreset_Ratio, 1.0f);
					curChain.SetProp_Texture(propSet._propID_STPreset_Texture, resultRT);
					curChain.SetProp_Vector4(propSet._propID_STPreset_ScreenSpaceOffset, _linkedRenderer.LastMaskScreenSpaceOffset);
					curChain.SetProp_Float(propSet._propID_STPreset_Alpha, stAlpha);
				}
			}
		}

		private void CopyPropToNextChain_Custom_SetProp_Float(PropertySetInfo propSet, float propValue)
		{
			if (_nChaindInfos == 0)
			{
				return;
			}

			ChainInfo curChain = null;
			for (int i = 0; i < _nChaindInfos; i++)
			{
				curChain = _chainedInfo[i];
				if (curChain == null) { continue; }

				curChain.SetProp_Float(propSet._propID_Custom, propValue);
			}
		}

		private void CopyPropToNextChain_Custom_SetProp_Vector4(PropertySetInfo propSet, Vector4 propValue)
		{
			if (_nChaindInfos == 0)
			{
				return;
			}
			ChainInfo curChain = null;
			for (int i = 0; i < _nChaindInfos; i++)
			{
				curChain = _chainedInfo[i];
				if (curChain == null)
				{ continue; }
				curChain.SetProp_Vector4(propSet._propID_Custom, propValue);
			}
		}

		private void CopyPropToNextChain_Custom_SetProp_Vector2(PropertySetInfo propSet, Vector2 propValue)
		{
			if (_nChaindInfos == 0)
			{
				return;
			}
			ChainInfo curChain = null;
			for (int i = 0; i < _nChaindInfos; i++)
			{
				curChain = _chainedInfo[i];
				if (curChain == null)
				{ continue; }
				curChain.SetProp_Vector2(propSet._propID_Custom, propValue);
			}
		}

		private void CopyPropToNextChain_Custom_SetProp_Texture(PropertySetInfo propSet, Texture propValue)
		{
			if (_nChaindInfos == 0)
			{
				return;
			}
			ChainInfo curChain = null;
			for (int i = 0; i < _nChaindInfos; i++)
			{
				curChain = _chainedInfo[i];
				if (curChain == null)
				{ continue; }
				curChain.SetProp_Texture(propSet._propID_Custom, propValue);
			}
		}

		private void CopyPropToNextChain_Custom_SetProp_Color(PropertySetInfo propSet, Color propValue)
		{
			if (_nChaindInfos == 0)
			{
				return;
			}
			ChainInfo curChain = null;
			for (int i = 0; i < _nChaindInfos; i++)
			{
				curChain = _chainedInfo[i];
				if (curChain == null)
				{ continue; }
				curChain.SetProp_Color(propSet._propID_Custom, propValue);
			}
		}

		private void CopyPropToNextChain_Custom_SetProp_Int(PropertySetInfo propSet, int propValue)
		{
			if (_nChaindInfos == 0)
			{
				return;
			}
			ChainInfo curChain = null;
			for (int i = 0; i < _nChaindInfos; i++)
			{
				curChain = _chainedInfo[i];
				if (curChain == null)
				{ continue; }
				curChain.SetProp_Int(propSet._propID_Custom, propValue);
			}
		}






		// Get
		//-------------------------------------------------------------------------
		public apOptMaskRenderer LinkedRenderer { get { return _linkedRenderer; } }
		public apOptMesh ReceivedMesh { get { return _receivedMesh; } }

	}
}