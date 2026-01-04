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


namespace AnyPortrait
{
	/// <summary>
	/// 임의의 마스크 전달 정보를 가진 데이터.
	/// apSendMaskData의 Opt 버전이다.
	/// </summary>
	[Serializable]
	public class apOptSendMaskData
	{
		// Sub Class
		//----------------------------------------------------------------
		/// <summary>
		/// 데이터가 도착하는 쉐이더의 프로퍼티
		/// </summary>
		[Serializable]
		public class ReceivePropertySet
		{
			//프로퍼티의 타입 (프리셋 사용 여부)
			[SerializeField] public apSendMaskData.SHADER_PROP_PRESET _preset = apSendMaskData.SHADER_PROP_PRESET.Custom;
			[SerializeField] public apSendMaskData.SHADER_PROP_RESERVED_CHANNEL _reservedChannel = apSendMaskData.SHADER_PROP_RESERVED_CHANNEL.Channel_1;

			//이름과 타입
			[SerializeField] public string _customName = "";
			[SerializeField] public apSendMaskData.SHADER_PROP_VALUE_TYPE _customPropType = apSendMaskData.SHADER_PROP_VALUE_TYPE.RenderTexture;

			//값
			//1. 프리셋 지원 타입 (이건 커스텀/프리셋 공용이다)
			[SerializeField] public apSendMaskData.MASK_OPERATION _value_MaskOp = apSendMaskData.MASK_OPERATION.And;

			//2. 컨트롤 파라미터 연동 타입 (이건 하나만 있으면 된다)
			[SerializeField] public bool _value_IsUseControlParam = false;
			[SerializeField] public int _value_ControlParamID = -1;
			[NonSerialized] public apControlParam _value_LinkedControlParam = null;

			//3. 커스텀 값
			[SerializeField] public float _value_Float = 0.0f;
			[SerializeField] public int _value_Int = 0;
			[SerializeField] public Vector4 _value_Vector = Vector4.zero;
			[SerializeField] public Texture _value_Texture = null;
			[SerializeField] public Color _value_Color = new Color(0, 0, 0, 1);

			//커스텀의 경우 Shader Prop ID (고정값이 아니므로 참조시 필요)
			[NonSerialized] private int _customShaderPropID = -1;


			// 이 PropSet을 가진 Parent Send Data와 OptMesh
			[NonSerialized] private apOptSendMaskData _parentSendData = null;
			[NonSerialized] private apOptMesh _parentOptMesh = null;


			public ReceivePropertySet()
			{
				_value_LinkedControlParam = null;
				_parentSendData = null;
				_parentOptMesh = null;
			}

			public void Bake(apSendMaskData.ReceivePropertySet srcPropSet)
			{
				_preset = srcPropSet._preset;
				_reservedChannel = srcPropSet._reservedChannel;

				_customName = srcPropSet._customName;
				_customPropType = srcPropSet._customPropType;

				_value_MaskOp = srcPropSet._value_MaskOp;

				_value_IsUseControlParam = srcPropSet._value_IsUseControlParam;
				_value_ControlParamID = srcPropSet._value_ControlParamID;
				_value_LinkedControlParam = null;

				_value_Float = srcPropSet._value_Float;
				_value_Int = srcPropSet._value_Int;
				_value_Vector = srcPropSet._value_Vector;
				_value_Texture = srcPropSet._value_Texture;
				_value_Color = srcPropSet._value_Color;
			}

			
			public void Link(apPortrait portrait, apOptSendMaskData parentSendData, apOptMesh parentMesh)
			{
				if(_value_ControlParamID >= 0 && _value_IsUseControlParam)
				{
					_value_LinkedControlParam = portrait.GetControlParam(_value_ControlParamID);
				}

				//여기서 커스텀 프로퍼티의 ID를 만들자
				_customShaderPropID = Shader.PropertyToID(_customName);

				//부모 객체들
				_parentSendData = parentSendData;
				_parentOptMesh = parentMesh;
			}

			///// <summary>
			///// Mask Operation의 Float 값을 리턴한다. Shader에 입력할 용도
			///// </summary>
			///// <returns></returns>
			//public float GetMaskOpFloatValue()
			//{
			//	switch (_value_MaskOp)
			//	{
			//		case apSendMaskData.MASK_OPERATION.And:			return 0.0f;
			//		case apSendMaskData.MASK_OPERATION.Or:			return 1.0f;
			//		case apSendMaskData.MASK_OPERATION.InverseAnd:	return 2.0f;
			//		case apSendMaskData.MASK_OPERATION.InverseOr:	return 3.0f;
			//	}
			//	return 0.0f;
			//}

			public int CustomShaderPropID { get { return _customShaderPropID; } }

			public apOptSendMaskData ParentSendData { get { return _parentSendData; } }
			public apOptMesh ParentOptMesh { get { return _parentOptMesh; } }


		}

		[Serializable]
		public class TargetInfo
		{
			[SerializeField] public int _meshTFID = -1;

			//Monobehaviour이므로 Serialize 가능
			[SerializeField] public apOptTransform _linkedTF = null;
			[SerializeField] public apOptMesh _linkedMesh = null;

			public TargetInfo()
			{	
			}

			//1차로 ID만 연결한다.
			public void BakeID(int meshTFID)
			{
				_meshTFID = meshTFID;				
			}

			public void BakeTargetMeshTF(apOptTransform targetTF, apOptMesh targetMesh)
			{
				_linkedTF = targetTF;
				_linkedMesh = targetMesh;
			}
		}

		//마스크 생성할 때, 메시의 기본 재질로부터 프로퍼티를 일부 복제해올 수 있다.
		[Serializable]
		public class CopiedPropertyInfo
		{
			[SerializeField] public string _propName = "";
			[SerializeField] public apSendMaskData.SHADER_PROP_REAL_TYPE _propType = apSendMaskData.SHADER_PROP_REAL_TYPE.Float;

			[NonSerialized] public int _propID = -1;

			public CopiedPropertyInfo()
			{

			}

			public void Link()
			{
				_propID = Shader.PropertyToID(_propName);
			}

		}

		// Members
		//-------------------------------------------------------------------------
		// [ RT 생성하기 ]
		//쉐이더 종류
		[SerializeField] public apSendMaskData.RT_SHADER_TYPE _rtShaderType = apSendMaskData.RT_SHADER_TYPE.AlphaMask;
		[SerializeField] public Shader _customRTShaderAsset = null;//커스텀 쉐이더 타입인 경우


		//원본 재질로부터 프로퍼티 복제하기
		[SerializeField] public List<CopiedPropertyInfo> _copiedProperties = null;

		//RT 렌더 순서
		[SerializeField] public apSendMaskData.RT_RENDER_ORDER _rtRenderOrder = apSendMaskData.RT_RENDER_ORDER.Phase1;
		

		//RT의 크기와 최적화 여부
		[SerializeField] public apTransform_Mesh.RENDER_TEXTURE_SIZE _renderTextureSize = apTransform_Mesh.RENDER_TEXTURE_SIZE.s_256;
		[SerializeField] public bool _isRTSizeOptimized = true;

		//쉐이더 패스 인덱스
		[SerializeField] public int _shaderPassIndex = 0;


		//RT 공유 여부
		//- 같은 RT_SHADER_TYPE과 같은 Group ID를 가진 경우, 공유된 Render Texture에 렌더링을 할 수 있다.
		[SerializeField] public bool _isRTShared = false;
		[SerializeField] public int _sharedRTID = 0;

		// [ RT를 전송하기 ]
		//연결된 대상 Child Transform 연결 정보		
		[SerializeField] public List<TargetInfo> _targetInfos = null;

		//프로퍼티 정보들
		[SerializeField] public List<ReceivePropertySet> _propertySets = null;

		


		//연결된 마스크 렌더러 (실시간 생성)
		[NonSerialized] public apOptMaskRenderer _linkedMaskRenderer = null;

		//이 SendData를 가진 부모 Mesh
		[NonSerialized] public apOptMesh _parentMesh = null;


		// Init
		//------------------------------------------------------------------------
		public apOptSendMaskData()
		{

		}


		// Bake
		//------------------------------------------------------------------------
		public void Bake(apSendMaskData src)
		{
			_rtShaderType = src._rtShaderType;
			_customRTShaderAsset = src._customRTShaderAsset;

			_copiedProperties = new List<CopiedPropertyInfo>();
			int nSrcCopiedProps = src._copiedProperties != null ? src._copiedProperties.Count : 0;
			if(nSrcCopiedProps > 0)
			{
				apSendMaskData.CopiedPropertyInfo srcCopyProp = null;
				for (int iSrcProp = 0; iSrcProp < nSrcCopiedProps; iSrcProp++)
				{
					srcCopyProp = src._copiedProperties[iSrcProp];
					if(string.IsNullOrEmpty(srcCopyProp._propName))
					{
						continue;
					}

					CopiedPropertyInfo newDstInfo = new CopiedPropertyInfo();
					newDstInfo._propName = srcCopyProp._propName;
					newDstInfo._propType = srcCopyProp._propType;
					_copiedProperties.Add(newDstInfo);
				}
			}

			//RT 렌더 순서
			_rtRenderOrder = src._rtRenderOrder;

			//RT의 크기와 최적화 여부
			_renderTextureSize = src._renderTextureSize;
			_isRTSizeOptimized = src._isRTSizeOptimized;

			_shaderPassIndex = src._shaderPassIndex;

			//RT 공유 여부
			//- 같은 RT_SHADER_TYPE과 같은 Group ID를 가진 경우, 공유된 Render Texture에 렌더링을 할 수 있다.
			_isRTShared = src._isRTShared;
			_sharedRTID = src._sharedRTID;

			// [ RT를 전송하기 ]
			//연결된 대상 Child Transform 연결 정보		
			_targetInfos = new List<TargetInfo>();
			int nSrcTargets = src._targetInfos != null ? src._targetInfos.Count : 0;
			if(nSrcTargets > 0)
			{
				apSendMaskData.TargetInfo srcTarget = null;
				for (int i = 0; i < nSrcTargets; i++)
				{
					srcTarget = src._targetInfos[i];
					TargetInfo newInfo = new TargetInfo();
					newInfo.BakeID(srcTarget._meshTFID);//1차로 ID만 연결한다.

					_targetInfos.Add(newInfo);
				}
			}

			//프로퍼티 정보들
			_propertySets = new List<ReceivePropertySet>();
			int nSrcProps = src._propertySets != null ? src._propertySets.Count : 0;
			if(nSrcProps > 0)
			{
				apSendMaskData.ReceivePropertySet srcProp = null;
				for (int i = 0; i < nSrcProps; i++)
				{
					srcProp = src._propertySets[i];

					//유효하지 않는건 제외한다.
					if(srcProp._preset == apSendMaskData.SHADER_PROP_PRESET.Custom)
					{
						//커스텀인데 이름이 없는것 > 유효하지 않다.
						//if(string.IsNullOrWhiteSpace(srcProp._customName))
						if(string.IsNullOrEmpty(srcProp._customName))
						{
							continue;
						}
					}
					

					ReceivePropertySet newProp = new ReceivePropertySet();
					newProp.Bake(srcProp);

					_propertySets.Add(newProp);
				}
			}
		}

		/// <summary>
		/// Bake 후반부에 호출되는 함수. TargetInfo 내의 "대상 OptMesh"를 연결한다.
		/// 참조를 위해 apPortrait를 인자로 받는다.
		/// </summary>
		/// <param name="portrait"></param>
		public void LinkTargetMeshOnBake(apPortrait portrait)
		{
			int nTargetInfos = _targetInfos != null ? _targetInfos.Count : 0;
			if(nTargetInfos == 0)
			{
				return;
			}

			TargetInfo targetInfo = null;
			for (int i = 0; i < nTargetInfos; i++)
			{
				targetInfo = _targetInfos[i];
				apOptTransform targetTF = portrait.GetOptTransform(targetInfo._meshTFID);
				if(targetTF == null)
				{
					continue;
				}

				apOptMesh targetMesh = targetTF._childMesh;
				if(targetMesh == null)
				{
					continue;
				}

				targetInfo.BakeTargetMeshTF(targetTF, targetMesh);
			}
		}



		// 초기화
		//--------------------------------------------------------------------
		public void Link(apPortrait portrait, apOptMesh parentMesh)
		{
			//데이터 Link를 하자.
			//- 프로퍼티 데이터들 중 일부는 컨트롤 파라미터를 연결해야한다.
			int nProps = _propertySets != null ? _propertySets.Count : 0;
			if(nProps > 0)
			{
				ReceivePropertySet propSet = null;
				for (int i = 0; i < nProps; i++)
				{
					propSet = _propertySets[i];
					propSet.Link(portrait, this, parentMesh);
				}
			}

			//복사 프로퍼티로 링크하자
			int nCopyProps = _copiedProperties != null ? _copiedProperties.Count : 0;
			if(nCopyProps > 0)
			{
				CopiedPropertyInfo copyProp = null;
				for (int i = 0; i < nCopyProps; i++)
				{
					copyProp = _copiedProperties[i];
					copyProp.Link();
				}
			}


			_parentMesh = parentMesh;
		}

		// Get
		//---------------------------------------------------------------------
		public apOptMaskRenderer GetLinkedMaskRenderer()
		{
			return _linkedMaskRenderer;
		}

		public apOptMesh ParentMesh
		{
			get
			{
				return _parentMesh;
			}
		}
	}
}