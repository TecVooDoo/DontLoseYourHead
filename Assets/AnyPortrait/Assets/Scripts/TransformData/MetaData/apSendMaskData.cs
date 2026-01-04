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
	//v1.6.0 추가
	/// <summary>
	/// 마스크 또는 메타맵을 다른 TransformMesh로 전달할 수 있다.
	/// 기존의 클리핑 마스크를 응용한 데이터이다. (클리핑 마스크의 ClipMeshSet 데이터와 유사하다)
	/// </summary>
	[Serializable]
	public class apSendMaskData
	{
		// RT를 작성하는 Shader 타입
		public enum RT_SHADER_TYPE : int
		{
			/// <summary>투명도를 Red 채널에 넣는 마스크 쉐이더. 클리핑 레이어와 동일. 이건 에디터 내장 쉐이더로도 지원한다.</summary>
			AlphaMask = 0,
			/// <summary>기본 AlphaBlend 쉐이더를 그대로 이용하여 마스크를 작성한다. 색상값이 포함</summary>
			MainTextureWithColor = 1,
			/// <summary>기본 AlphaBlend 쉐이더를 그대로 이용하되 Tint 색상은 기본값으로 둔다.</summary>
			MainTextureOnly = 2,
			/// <summary>커스텀 쉐이더로 마스크를 생성한다.</summary>
			CustomShader = 8,
		}

		// RT를 작성하는 렌더 순서 (페이즈 그룹으로 지정)
		public enum RT_RENDER_ORDER : int
		{
			/// <summary>Clipping 등에서 실행되는 가장 빠른 렌더 순서.</summary>
			Phase1 = 0,
			/// <summary>두번째 순서</summary>
			Phase2 = 1,
			/// <summary>마지막 순서. 클리핑 마스크를 받은 메시가 마스크를 생성하고자 한다면 이 단계를 활용하자</summary>
			Phase3 = 2,
		}



		//빌트인 프로퍼티를 이용할지 커스텀 프로퍼티인지 결정
		public enum SHADER_PROP_PRESET : int
		{
			/// <summary>일반적인 커스텀 타입. 직접 지정해줘야 한다.</summary>
			Custom = 0,

			// 프리셋화된 프로퍼티 타입 (여러개의 프로퍼티들이 패키지로 묶여있다.)
			/// <summary>Alpha Mask 방식의 프로퍼티의 세트</summary>
			AlphaMaskPreset = 1,

			/// <summary>다른 메시가 투과되어 보이는 프로퍼티 세트</summary>
			SeeThroughPreset = 2,
		}
		public enum SHADER_PROP_RESERVED_CHANNEL : int
		{
			Channel_1 = 0,
			Channel_2 = 1,
			Channel_3 = 2,
			Channel_4 = 3,
		}


		public enum SHADER_PROP_VALUE_TYPE : int
		{
			//자동 생성 값 
			RenderTexture = 0,//생성된 RenderTexture
			ScreenSpaceOffset = 1,//최적화된 RT 크기용 프로퍼티

			//프리셋 지원 값
			MaskOp = 2,//마스크 부울 연산(Float 타입이며 0:마스크없음 / 1:AND 연산 / 2:OR 연산)

			//자동 생성값 추가
			CalculatedColor = 5,

			//임의의 값. 일부 타입은 컨트롤 파라미터로 링크할 수 있다.
			Value_Float = 10,
			Value_Int = 11,
			Value_Vector = 12,
			Value_Texture = 13,
			Value_Color = 14,

			//VR용 특수값
			RenderTexture_VR_Left = 30,
			RenderTexture_VR_Right = 31,
		}

		/// <summary>
		/// SHADER_PROP_VALUE_TYPE 값의 실제 타입 (Shader Type에 해당)
		/// </summary>
		public enum SHADER_PROP_REAL_TYPE : int
		{
			Texture = 0,
			Float = 1,
			Int = 2,
			Vector = 3,
			Color = 4
		}

		//마스크 연산 규칙
		//1. 교집함 / 합집함
		// - And (교집합) : 앞 채널 마스크와 겹치는 부분만 마스크가 된다. (첫 채널에서는 해당되지 않음)
		// - Or (합집합) : 앞 채널 마스크에 더해서 더 넓은 부분이 마스크가 된다. (첫채널에서는 해당되지 않음)

		//2. 반전 여부
		// - Inverse : 알파 영역을 반전한 값을 적용한다.

		//3. 공통 규칙
		// - 연산 순서를 고려하며, 첫 채널에서는 마스크 연산이 적용되지 않는다. 클리핑 레이어가 있다면 그게 첫 채널이다.

		public enum MASK_OPERATION : int
		{
			And = 0,//교집합 (Multiply)
			Or = 1,//합집합 (Max)
			InverseAnd = 2,//반전 교집합
			InverseOr = 3,//반전 합집합
		}


		/// <summary>
		/// 데이터가 도착하는 쉐이더의 프로퍼티
		/// </summary>
		[Serializable]
		public class ReceivePropertySet
		{
			//프로퍼티의 타입 (프리셋 사용 여부)
			[SerializeField] public SHADER_PROP_PRESET _preset = SHADER_PROP_PRESET.Custom;
			[SerializeField] public SHADER_PROP_RESERVED_CHANNEL _reservedChannel = SHADER_PROP_RESERVED_CHANNEL.Channel_1;


			//이름과 타입
			[SerializeField] public string _customName = "";
			[SerializeField] public SHADER_PROP_VALUE_TYPE _customPropType = SHADER_PROP_VALUE_TYPE.RenderTexture;

			//값
			//1. 프리셋 지원 타입 (이건 커스텀/프리셋 공용이다)
			[SerializeField] public MASK_OPERATION _value_MaskOp = MASK_OPERATION.And;
			//[SerializeField] public MASK_INTERACTION _propValue_MaskInteraction = MASK_INTERACTION.VisibleUnderMask;

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

			public ReceivePropertySet()
			{
				Clear();
			}

			public void Clear()
			{
				_preset = SHADER_PROP_PRESET.Custom;
				_reservedChannel = SHADER_PROP_RESERVED_CHANNEL.Channel_1;

				_customName = "";
				_customPropType = SHADER_PROP_VALUE_TYPE.RenderTexture;

				_value_MaskOp = MASK_OPERATION.And;

				_value_IsUseControlParam = false;
				_value_ControlParamID = -1;
				_value_LinkedControlParam = null;

				_value_Float = 0.0f;
				_value_Int = 0;
				_value_Vector = Vector4.zero;
				_value_Texture = null;
				_value_Color = new Color(0, 0, 0, 1);
			}

			/// <summary>
			/// Mask Operation의 Float 값을 리턴한다. Shader에 입력할 용도
			/// </summary>
			/// <returns></returns>
			public float GetMaskOpFloatValue()
			{
				switch (_value_MaskOp)
				{
					case MASK_OPERATION.And:		return 0.0f;
					case MASK_OPERATION.Or:			return 1.0f;
					case MASK_OPERATION.InverseAnd:	return 2.0f;
					case MASK_OPERATION.InverseOr:	return 3.0f;
				}
				return 0.0f;
			}

			//일부 값은 컨트롤 파라미터와 연결되었을 수 있다.
			/// <summary>
			/// Float 값을 리턴한다. 컨트롤 파라미터와 연결되어 있다면 그 값을 리턴한다.
			/// </summary>
			public float GetValue_Float()
			{
				if(_value_IsUseControlParam && _value_LinkedControlParam != null)
				{
					if(_value_LinkedControlParam._valueType == apControlParam.TYPE.Float)
					{
						return _value_LinkedControlParam._float_Cur;
					}
				}
				return _value_Float;
			}

			/// <summary>
			/// Int 값을 리턴한다. 컨트롤 파라미터와 연결되어 있다면 그 값을 리턴한다.
			/// </summary>
			/// <returns></returns>
			public int GetValue_Int()
			{
				if (_value_IsUseControlParam && _value_LinkedControlParam != null)
				{
					if (_value_LinkedControlParam._valueType == apControlParam.TYPE.Int)
					{
						return _value_LinkedControlParam._int_Cur;
					}
				}
				return _value_Int;
			}

			/// <summary>
			/// Vector 값을 리턴한다. 컨트롤 파라미터(Vector2)와 연결되어 있다면 그 값을 리턴한다.
			/// </summary>
			/// <returns></returns>
			public Vector4 GetValue_Vector()
			{
				if (_value_IsUseControlParam && _value_LinkedControlParam != null)
				{
					if (_value_LinkedControlParam._valueType == apControlParam.TYPE.Vector2)
					{
						Vector2 vec2Value = _value_LinkedControlParam._vec2_Cur;
						return new Vector4(vec2Value.x, vec2Value.y, 0.0f, 0.0f);
					}
				}
				return _value_Vector;
			}

			public Texture GetValue_Texture()
			{
				return _value_Texture;
			}

			public Color GetValue_Color()
			{
				return _value_Color;
			}
		}

		[Serializable]
		public class TargetInfo
		{
			[SerializeField] public int _meshTFID = -1;

			[NonSerialized] public apTransform_Mesh _linkedMeshTF = null;

			public TargetInfo()
			{
				_meshTFID = -1;
				_linkedMeshTF = null;
			}

			public string Name
			{
				get
				{
					return _linkedMeshTF != null ? _linkedMeshTF._nickName : "(Unknown)";
				}
			}

		}


		//마스크 생성시 원래의 재질의 일부 프로퍼티를 복사받도록 하자
		[Serializable]
		public class CopiedPropertyInfo
		{
			[SerializeField] public string _propName = "";
			[SerializeField] public SHADER_PROP_REAL_TYPE _propType = SHADER_PROP_REAL_TYPE.Float;

			public CopiedPropertyInfo()
			{

			}
		}

		// Members
		//--------------------------------------------------------

		//마스크 데이터 속성들
		//  [ RT 생성하기 ]
		// - 쉐이더 종류, 커스텀 쉐이더 에셋
		// - RT 크기
		// - 크기 최적화 여부 (크기 최적화를 할 경우 별도의 프로퍼티가 필요하다)

		// [ RT를 전송하기 ]
		// - 대상 MeshTF
		// - 프로퍼티 이름 + 값
		//   > RT가 적용되는 텍스쳐 프로퍼티 이름 (필수)
		//   > (크기 최적화시) 마스크 크기 최적화 프로퍼티 이름 (기존의 _MaskScreenSpaceOffset)
		//   > 부수적인 프로퍼티 이름과 값
		
		// - 프리셋으로 정해서 RT 속성과 프로퍼티들을 빠르게 설정 가능
		//   > "마스크" : AlphaMask 쉐이더 + RT + 크기 최적화 프로퍼티 + 채널 + 마스크 연산(OR, AND)
		//   > "투과" : BasicWithColor 쉐이더 + RT + 크기 최적화 프로퍼티 + 채널 (투명도는 대상 MeshTF의 Shader에서 일괄 설정)

		// [ RT 생성하기 ]
		//쉐이더 종류
		[SerializeField] public RT_SHADER_TYPE _rtShaderType = RT_SHADER_TYPE.AlphaMask;
		[SerializeField] public Shader _customRTShaderAsset = null;//커스텀 쉐이더 타입인 경우

		// 프로퍼티 복사받기
		[SerializeField] public List<CopiedPropertyInfo> _copiedProperties = null;

		//렌더 순서
		[SerializeField] public RT_RENDER_ORDER _rtRenderOrder = RT_RENDER_ORDER.Phase1;

		//RT의 크기와 최적화 여부
		[SerializeField] public apTransform_Mesh.RENDER_TEXTURE_SIZE _renderTextureSize = apTransform_Mesh.RENDER_TEXTURE_SIZE.s_256;
		[SerializeField] public bool _isRTSizeOptimized = true;

		//쉐이더의 Pass Index
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


		// Init
		//--------------------------------------------------------
		public apSendMaskData()
		{

		}




		// Get / Set
		//--------------------------------------------------------
		public List<TargetInfo> TargetInfos
		{
			get { return _targetInfos; }
		}


		/// <summary>
		/// 해당 MeshTransform을 대상으로 하는
		/// </summary>
		/// <param name="meshTF"></param>
		/// <returns></returns>
		public TargetInfo GetTargetInfo(apTransform_Mesh meshTF)
		{
			if(meshTF == null)
			{
				return null;
			}
			int nInfos = _targetInfos != null ? _targetInfos.Count : 0;
			if(nInfos == 0)
			{
				return null;
			}

			return _targetInfos.Find(delegate(TargetInfo a)
			{
				return a._meshTFID == meshTF._transformUniqueID;
			});
		}


		///// <summary>
		///// 연결된 MeshTF들을 입력합니다. null 입력시 모두 초기화
		///// </summary>
		///// <param name="srcTFList"></param>
		//public void SetLinkedMeshTF(List<apTransform_Mesh> srcTFList)
		//{
		//	if(_linkedMeshTFs == null)
		//	{
		//		_linkedMeshTFs = new List<apTransform_Mesh>();
		//	}
		//	_linkedMeshTFs.Clear();

		//	int nSrc = srcTFList != null ? srcTFList.Count : 0;
		//	if(nSrc == 0)
		//	{
		//		return;
		//	}

		//	for (int i = 0; i < nSrc; i++)
		//	{
		//		_linkedMeshTFs.Add(srcTFList[i]);
		//	}
		//}

		// Static Functions
		//--------------------------------------------------------------
		public static float MaskOperationToFloatValue(MASK_OPERATION maskOp)
		{
			switch (maskOp)
			{
				case MASK_OPERATION.And: return 0.0f;
				case MASK_OPERATION.Or: return 1.0f;
				case MASK_OPERATION.InverseAnd: return 2.0f;
				case MASK_OPERATION.InverseOr: return 3.0f;
			}
			return 0.0f;
		}


		public static SHADER_PROP_REAL_TYPE PropValueTypeToRealType(SHADER_PROP_VALUE_TYPE propType)
		{
			switch (propType)
			{
				case SHADER_PROP_VALUE_TYPE.RenderTexture:		return SHADER_PROP_REAL_TYPE.Texture;
				case SHADER_PROP_VALUE_TYPE.ScreenSpaceOffset:	return SHADER_PROP_REAL_TYPE.Vector;
				case SHADER_PROP_VALUE_TYPE.MaskOp:				return SHADER_PROP_REAL_TYPE.Float;

				case SHADER_PROP_VALUE_TYPE.CalculatedColor:	return SHADER_PROP_REAL_TYPE.Color;

				case SHADER_PROP_VALUE_TYPE.Value_Float:		return SHADER_PROP_REAL_TYPE.Float;
				case SHADER_PROP_VALUE_TYPE.Value_Int:			return SHADER_PROP_REAL_TYPE.Int;
				case SHADER_PROP_VALUE_TYPE.Value_Vector:		return SHADER_PROP_REAL_TYPE.Vector;
				case SHADER_PROP_VALUE_TYPE.Value_Texture:		return SHADER_PROP_REAL_TYPE.Texture;
				case SHADER_PROP_VALUE_TYPE.Value_Color:		return SHADER_PROP_REAL_TYPE.Color;

				case SHADER_PROP_VALUE_TYPE.RenderTexture_VR_Left:	return SHADER_PROP_REAL_TYPE.Texture;
				case SHADER_PROP_VALUE_TYPE.RenderTexture_VR_Right:	return SHADER_PROP_REAL_TYPE.Texture;
			}
			return SHADER_PROP_REAL_TYPE.Float;
		}
	}
}
