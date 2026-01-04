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
	/// MeshTF에서 사용되는 마스크용 RenderTexture의 래핑 클래스.
	/// 이건 RT의 래퍼이며, 생성은 에디터에서 수행한다.
	/// 윈도우 크기에 따라 재생성이 있을 수 있다.
	/// MeshTF나 MaskData에서 참조할 수 있다.
	/// </summary>
	public class apMaskRT
	{
		// Members
		//--------------------------------------------------
		// 생성된 RT와 생성 여부 정보 등
		private RenderTexture _renderTexture = null;
		
		private int _width = -1;
		private int _height = -1;

		//Clear 여부. 이건 매 프레임마다 첫 렌더 요청시 시행되어야 한다.
		//매 렌더 시작시 이 값은 false가 된다.
		private bool _isCleared = false;

		
		// Init
		//--------------------------------------------------
		public apMaskRT()
		{
			_renderTexture = null;
			_width = -1;
			_height = -1;

			_isCleared = false;
		}

		/// <summary>
		/// 생성된 RT를 강제로 해체한다.
		/// 에디터 초기화시 중요
		/// </summary>
		public void Release()
		{
			if(_renderTexture != null)
			{
				RenderTexture.ReleaseTemporary(_renderTexture);
				_renderTexture = null;
			}
		}

		// Functions
		//--------------------------------------------------
		/// <summary>
		/// 생성 여부를 체크하고 렌더링을 준비한다.
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		public void ReadyToRender(int width, int height)
		{
			if(width < 1) { width = 1; }
			if (height < 1) { height = 1; }

			//생성된 상태라면 크기를 먼저 체크한다.
			//크기가 맞지 않다면 바로 해제한다.
			if(_renderTexture != null)
			{
				if(_width != width || _height != height)
				{
					//크기가 다르다면 해제
					RenderTexture.ReleaseTemporary(_renderTexture);
					_renderTexture = null;
					_width = -1;
					_height = -1;
				}
			}

			if(_renderTexture == null)
			{
				//생성되지 않았다면 생성하자
				_width = width;
				_height = height;
				_renderTexture = RenderTexture.GetTemporary(_width, _height, 8, RenderTextureFormat.ARGB32);
				_renderTexture.wrapMode = TextureWrapMode.Clamp;
				_renderTexture.isPowerOfTwo = false;
				_renderTexture.filterMode = FilterMode.Bilinear;
			}

			//색상 초기화 플래그를 초기화하자
			_isCleared = false;

		}

		// Get / Set
		//--------------------------------------------------
		public RenderTexture GetRenderTexture()
		{
			return _renderTexture;
		}

		public bool IsBufferCleared
		{
			get { return _isCleared; }
		}

		public void SetBufferCleared()
		{
			_isCleared = true;
		}


	}
}