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
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;
using System;

using AnyPortrait;

namespace AnyPortrait
{
	/// <summary>
	/// apMaskRT의 Opt용 래핑 클래스 중 하위 클래스
	/// RenderTexture와 몇가지 정보들을 래핑한다.
	/// 듀얼 채널인 경우, L, R용으로도 생성한다.
	/// 이 단위는 "카메라"당 생성된다.
	/// 렌더 요청 전체에 대한 클래스는 apOptMaskRTSet이다.
	/// </summary>
	public class apOptMaskRT
	{
		// Members
		//--------------------------------------
		// 싱글 방식인지 여부
		private bool _isDualRT = false;

		// 크기
		private int _size = 0;//크기값. 이게 음수면 자동 크기이다.
#if UNITY_2019_1_OR_NEWER
		private bool _isEyeTextureSize = false;
#endif

		// Single이라면
		private RenderTexture _renderTexture;
		private RenderTargetIdentifier _RTID;
		

		// Dual이라면
		private RenderTexture _renderTexture_L;
		private RenderTexture _renderTexture_R;
		private RenderTargetIdentifier _RTID_L;
		private RenderTargetIdentifier _RTID_R;
		
		private const int AUTO_SIZE__FULL_SCREEN = -1;
		private const int AUTO_SIZE__HALF_SCREEN = -2;
		private const int AUTO_SIZE__QUARTER_SCREEN = -3;
		private const int AUTO_SIZE__MAX_FHD = -4;
		private const int AUTO_SIZE__MAX_HD = -5;

		private bool _isCreated = false;

		// Init
		//--------------------------------------
		public apOptMaskRT(	int size,
							bool isDualRT
							#if UNITY_2019_1_OR_NEWER
							, bool isEyeTextureSize
							#endif
							)
		{
			

			_isDualRT = isDualRT;
			_size = size;
#if UNITY_2019_1_OR_NEWER
			_isEyeTextureSize = isEyeTextureSize;
#endif

			_renderTexture = null;
			_renderTexture_L = null;
			_renderTexture_R = null;

			_isCreated = false;
		}

		public void Make()
		{
			if(_isCreated)
			{
				return;
			}

			if(_isDualRT)
			{
				//L/R 텍스쳐를 생성한다. VR용
				_renderTexture_L = MakeRenderTexture();
				_renderTexture_R = MakeRenderTexture();
				_RTID_L = new RenderTargetIdentifier(_renderTexture_L);
				_RTID_R = new RenderTargetIdentifier(_renderTexture_R);
			}
			else
			{
				//단일 텍스쳐를 생성한다. 일반용
				_renderTexture = MakeRenderTexture();
				_RTID = new RenderTargetIdentifier(_renderTexture);
			}

			_isCreated = true;
		}

		public void Release()
		{
			if(_renderTexture != null)
			{
				RenderTexture.ReleaseTemporary(_renderTexture);
				_renderTexture = null;
			}

			if(_renderTexture_L != null)
			{
				RenderTexture.ReleaseTemporary(_renderTexture_L);
				_renderTexture_L = null;
			}

			if(_renderTexture_R != null)
			{
				RenderTexture.ReleaseTemporary(_renderTexture_R);
				_renderTexture_R = null;
			}

			_isCreated = false;
		}


		private RenderTexture MakeRenderTexture()
		{
			//Render Texture의 크기를 계산하다.
			//보통은 고정값이지만, 옵션에 다라서는 이 시점의 Screen 크기를 고려한 값이 될 수 있다.
			int textureWidth = 0;
			int textureHeight = 0;
			CalculateRTSize(out textureWidth, out textureHeight);

			if(textureWidth < 10)
			{
				textureWidth = 10;
			}
			if(textureHeight < 10)
			{
				textureHeight = 10;
			}

#if UNITY_2019_1_OR_NEWER
			if(UnityEngine.XR.XRSettings.enabled)
			{
				if(_isEyeTextureSize)
				{
					//완전히 EyeTexture와 동일하게
					//return RenderTexture.GetTemporary(UnityEngine.XR.XRSettings.eyeTextureDesc);//<<이건 정상적으로 동작하지 않는다.
					return RenderTexture.GetTemporary(	UnityEngine.XR.XRSettings.eyeTextureDesc.width, 
														UnityEngine.XR.XRSettings.eyeTextureDesc.height, 
														UnityEngine.XR.XRSettings.eyeTextureDesc.depthBufferBits, 
														UnityEngine.XR.XRSettings.eyeTextureDesc.colorFormat);
				}
				else
				{
					//크기는 설정대로, 포맷만 EyeTexture
					return RenderTexture.GetTemporary(	textureWidth, textureHeight, 
														UnityEngine.XR.XRSettings.eyeTextureDesc.depthBufferBits, 
														UnityEngine.XR.XRSettings.eyeTextureDesc.colorFormat);
				}
			}
			else
			{
				return RenderTexture.GetTemporary(textureWidth, textureHeight, 24, RenderTextureFormat.Default);
			}
#else
			return RenderTexture.GetTemporary(textureWidth, textureHeight, 24, RenderTextureFormat.Default);
#endif
		}


		private void CalculateRTSize(out int width, out int height)
		{
			if(_size > 0)
			{
				width = Mathf.Max(_size, 10);
				height = Mathf.Max(_size, 10);
				return;
			}

			// 자동 크기인 경우
			int screenWidth = Mathf.Max(Screen.width, 100);
			int screenHeight = Mathf.Max(Screen.height, 100);

			int shortestSize = Mathf.Min(screenWidth, screenHeight);
			
			switch(_size)
			{
				case AUTO_SIZE__FULL_SCREEN:
					width = screenWidth;
					height = screenHeight;
					break;

				case AUTO_SIZE__HALF_SCREEN:
					width = screenWidth / 2;
					height = screenHeight / 2;
					break;

				case AUTO_SIZE__QUARTER_SCREEN:
					width = screenWidth / 4;
					height = screenHeight / 4;
					break;

				case AUTO_SIZE__MAX_FHD:
					{
						width = screenWidth;
						height = screenHeight;
						if(shortestSize > 1080)//짧은 축의 길이가 1080보다 크다면
						{
							float rescale = 1080.0f / (float)shortestSize;
							width = Mathf.Max((int)(screenWidth * rescale), 10);
							height = Mathf.Max((int)(screenHeight * rescale), 10);
						}
					}
					break;

				case AUTO_SIZE__MAX_HD:
					{
						width = screenWidth;
						height = screenHeight;
						if(shortestSize > 720)//짧은 축의 길이가 720보다 크다면
						{
							float rescale = 720.0f / (float)shortestSize;
							width = Mathf.Max((int)(screenWidth * rescale), 10);
							height = Mathf.Max((int)(screenHeight * rescale), 10);
						}						
					}					
					break;

				default:
					width = Mathf.Max(_size, 10);
					height = Mathf.Max(_size, 10);
					break;
			}
		}


		// Get
		//----------------------------------------------------------------
		public bool IsCreated() { return _isCreated; }


		public RenderTexture RenderTexture_Single { get { return _renderTexture; } }
		public RenderTexture RenderTexture_L { get { return _renderTexture_L; } }
		public RenderTexture RenderTexture_R { get { return _renderTexture_R; } }

		public RenderTargetIdentifier RenderTargetID { get { return _RTID; } }
		public RenderTargetIdentifier RenderTargetID_L { get { return _RTID_L; } }
		public RenderTargetIdentifier RenderTargetID_R { get { return _RTID_R; } }

		


	}
}