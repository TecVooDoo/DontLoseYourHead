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
	/// <summary>
	/// 에디터에 속한 클래스로서, 렌더링시 사용되는 RenderTexture들을 모두 관리한다.
	/// </summary>
	public class apEditorRT
	{
		// Members
		//------------------------------------------------------
		// [ 개별 마스크 ]
		//- MeshTF를 키값으로 저장한다.
		//- 카테고리는 클리핑/SendMaskData의 Custom을 제외한 타입별로 저장

		// [ 공통 마스크 ]
		//- SendMaskData의 Custom을 제외한 타입 + Channel을 키값으로 삼는다.

		//생성을 한 후에는 참조할 수 있도록 object 키값을 제공한다.

		//생성된 전체 RT
		private List<apMaskRT> _allRTs = null;

		//키-RT 매핑들
		
		//1. 개별 RT
		//- 클리핑 마스크의 RT
		private Dictionary<apTransform_Mesh, apMaskRT> _clippingParent2RT = null;

		//- 개별 타입의 SendMaskData
		private Dictionary<apTransform_Mesh, Dictionary<apSendMaskData.RT_SHADER_TYPE, apMaskRT>> _meshTF2RT = null;

		//2. 공통 RT
		private Dictionary<int, apMaskRT> _shared2RT = null;//다른 Shader들도 한번에 렌더링 가능.


		//마지막 Window의 Width/Height
		private int _lastWindowWidth = -1;
		private int _lastWindowHeight = -1;


		// Init
		//-------------------------------------
		public apEditorRT()
		{
			ReleaseAll();
		}

		/// <summary>
		/// 모든 RT를 해제하고, 변수를 모두 초기화한다. Portrait를 변경할 때 사용된다.
		/// </summary>
		public void ReleaseAll()
		{
			int nRTs = _allRTs != null ? _allRTs.Count : 0;
			if(nRTs > 0)
			{
				apMaskRT curRT = null;
				for (int i = 0; i < nRTs; i++)
				{
					curRT = _allRTs[i];
					curRT.Release();
				}
			}

			if(_allRTs == null) { _allRTs = new List<apMaskRT>(); }
			_allRTs.Clear();

			if(_clippingParent2RT == null) { _clippingParent2RT = new Dictionary<apTransform_Mesh, apMaskRT>(); }
			_clippingParent2RT.Clear();
			

			if(_meshTF2RT == null) { _meshTF2RT = new Dictionary<apTransform_Mesh, Dictionary<apSendMaskData.RT_SHADER_TYPE, apMaskRT>>(); }
			_meshTF2RT.Clear();

			if(_shared2RT == null) { _shared2RT = new Dictionary<int, apMaskRT>(); }
			_shared2RT.Clear();

			_lastWindowWidth = 1;
			_lastWindowHeight = 1;

			//Debug.Log("RT Release");
		}



		// Function (Render)
		//-------------------------------------
		/// <summary>
		/// 렌더링하기 위해서 일괄 준비를 한다. Buffer Clear 플래그를 초기화한다.
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		public void ReadyToRender(int width, int height)
		{
			int nRTs = _allRTs != null ? _allRTs.Count : 0;
			if(nRTs > 0)
			{
				apMaskRT curRT = null;

				for (int i = 0; i < nRTs; i++)
				{
					curRT = _allRTs[i];
					curRT.ReadyToRender(width, height);
				}
			}

			_lastWindowWidth = width > 1 ? width : 1;
			_lastWindowHeight = height > 1 ? height : 1;
		}


		// 참조하거나 생성을 하자
		//1. 개별-Clipping Parent
		/// <summary>
		/// Clipping Parent의 개별 마스크용 RT를 가져온다. 생성되지 않았다면 자동으로 생성도 한다.
		/// </summary>
		public apMaskRT GetRT_ClippingParent(apTransform_Mesh meshTF)
		{
			if(_clippingParent2RT == null) { _clippingParent2RT = new Dictionary<apTransform_Mesh, apMaskRT>(); }
			if(_allRTs == null) { _allRTs = new List<apMaskRT>(); }

			apMaskRT resultRT = null;
			_clippingParent2RT.TryGetValue(meshTF, out resultRT);
			if(resultRT == null)
			{
				//RT가 없다면 새로 생성
				resultRT = new apMaskRT();

				//새로 생성된 RT는 마지막 Window 크기로 바로 Ready To Render를 하자
				resultRT.ReadyToRender(_lastWindowWidth, _lastWindowHeight);

				//리스트에 넣자
				
				_allRTs.Add(resultRT);

				_clippingParent2RT.Add(meshTF, resultRT);
			}

			return resultRT;
		}

		//2. 개별-MaskData
		/// <summary>
		/// Send Mask Data에서 지정된 개별 마스크용 RT를 가져온다. 생성되지 않았다면 자동으로 생성도 한다.
		/// </summary>
		public apMaskRT GetRT_PerMeshTF(apTransform_Mesh meshTF, apSendMaskData.RT_SHADER_TYPE shaderType)
		{
			if(shaderType == apSendMaskData.RT_SHADER_TYPE.CustomShader)
			{
				//커스텀은 지원되지 않는다.
				return null;
			}

			if(_meshTF2RT == null) { _meshTF2RT = new Dictionary<apTransform_Mesh, Dictionary<apSendMaskData.RT_SHADER_TYPE, apMaskRT>>(); }
			if(_allRTs == null) { _allRTs = new List<apMaskRT>(); }

			apMaskRT resultRT = null;
			Dictionary<apSendMaskData.RT_SHADER_TYPE, apMaskRT> shader2RT = null;
			_meshTF2RT.TryGetValue(meshTF, out shader2RT);

			//Shader 타입별 리스트를 체크
			if(shader2RT == null)
			{
				//MeshTF별 RT 리스트가 없다면 생성
				shader2RT = new Dictionary<apSendMaskData.RT_SHADER_TYPE, apMaskRT>();

				//RT도 바로 생성한다.
				resultRT = new apMaskRT();
				resultRT.ReadyToRender(_lastWindowWidth, _lastWindowHeight);//바로 Ready To Render

				//매핑 리스트에 추가
				shader2RT.Add(shaderType, resultRT);

				//리스트에 추가
				_meshTF2RT.Add(meshTF, shader2RT);
				_allRTs.Add(resultRT);
			}
			else
			{
				//일단 MeshTF별 리스트는 발견했다면
				shader2RT.TryGetValue(shaderType, out resultRT);

				if(resultRT == null)
				{
					//RT도 바로 생성한다.
					resultRT = new apMaskRT();
					resultRT.ReadyToRender(_lastWindowWidth, _lastWindowHeight);//바로 Ready To Render

					//매핑 리스트에 추가
					shader2RT.Add(shaderType, resultRT);
					
					//리스트에 추가
					_allRTs.Add(resultRT);
				}
			}

			return resultRT;
		}


		//3. 공유 텍스쳐
		/// <summary>
		/// 공유된 렌더 텍스쳐를 가져온다. 없다면 생성한다.
		/// </summary>
		public apMaskRT GetRT_Shared(apSendMaskData.RT_SHADER_TYPE shaderType, int ID)
		{
			if(shaderType == apSendMaskData.RT_SHADER_TYPE.CustomShader)
			{
				//커스텀은 지원되지 않는다.
				return null;
			}

			//private Dictionary<apSendMaskData.RT_SHADER_TYPE, Dictionary<apSendMaskData.SHADER_PROP_RESERVED_CHANNEL, apMaskRT>> _shared2RT = null;
			if(_shared2RT == null)
			{
				_shared2RT = new Dictionary<int, apMaskRT>();
			}
			if(_allRTs == null) { _allRTs = new List<apMaskRT>(); }

			apMaskRT resultRT = null;
			_shared2RT.TryGetValue(ID, out resultRT);

			if(resultRT == null)
			{
				//RT도 바로 생성한다.
				resultRT = new apMaskRT();
				resultRT.ReadyToRender(_lastWindowWidth, _lastWindowHeight);//바로 Ready To Render

				//매핑 리스트에 추가
				_shared2RT.Add(ID, resultRT);
					
				//리스트에 추가
				_allRTs.Add(resultRT);
			}

			return resultRT;
		}
	}
}