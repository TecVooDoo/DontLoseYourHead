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

	

	// AnimPlay에서 재생하기 위해 AnimClip (string 이름), MeshGroup + RootUnit의 세트를 저장한다.
	// 직렬화가 되는 데이터와 그렇지 않은 데이터가 있어서 링크 작업 필요
	// 플레이 참조시 사용된다. (일일이 검색하지 않도록 만듬)
	// 이 데이터를 참조로 AnimPlayUnit이 생성된다.
	
	/// <summary>
	/// The data class that stores the "AnimClip" and the RootUnit to play the animation
	/// </summary>
	[Serializable]
	public class apAnimPlayData
	{
		// Members
		//----------------------------------------------
		/// <summary>Target Animation Clip ID</summary>
		[SerializeField]
		public int _animClipID = -1;

		/// <summary>Target Animataion Clip</summary>
		[NonSerialized]
		public apAnimClip _linkedAnimClip = null;

		/// <summary>Target Animation Clip Name</summary>
		[SerializeField]
		public string _animClipName = "";

		/// <summary>Linked Mesh Group ID</summary>
		[SerializeField]
		public int _meshGroupID = -1;

		/// <summary>Linked Opt Root Unit</summary>
		[NonSerialized]
		public apOptRootUnit _linkedOptRootUnit = null;


		/// <summary>Is Valid Data</summary>
		[NonSerialized]
		public bool _isValid = false;

		
		public enum AnimationPlaybackStatus
		{
			/// <summary>The animation is not playing.</summary>
			None,
			/// <summary>The animation is being played.</summary>
			Playing,
			/// <summary>The animation is paused.</summary>
			Paused,
			/// <summary>The non-loop type animation has been executed and it has been played until the last frame.</summary>
			Ended,
		}



		// Init
		//----------------------------------------------
		/// <summary>
		/// 백업용 생성자
		/// </summary>
		public apAnimPlayData()
		{

		}

		public apAnimPlayData(int animClipID, int meshGroupID, string animClipName)
		{
			_animClipID = animClipID;

			_meshGroupID = meshGroupID;

			_animClipName = animClipName;
		}

		public void Link(apAnimClip animClip, apOptRootUnit optRootUnit)
		{
			_linkedAnimClip = animClip;
			_linkedOptRootUnit = optRootUnit;
			_isValid = true;

			if(_linkedAnimClip != null)
			{
				//상호 연결을 하자
				_linkedAnimClip.LinkPlayData(this);
			}
		}


		// Functions
		//----------------------------------------------
		/// <summary>Set the speed of the animation.</summary>
		/// <param name="speed"></param>
		public void SetSpeed(float speed)
		{
			if(_linkedAnimClip != null)
			{
				//_linkedAnimClip._speedRatio = speed;//이전
				_linkedAnimClip.SetSpeed(speed);
			}
		}

		/// <summary>
		/// Returns the playback state of the animation.
		/// </summary>
		public AnimationPlaybackStatus PlaybackStatus
		{
			get
			{
				if(_linkedAnimClip == null)
				{
					return AnimationPlaybackStatus.None;
				}

				if(!_linkedAnimClip.IsHasValidPlayUnit)
				{
					//적절한 PlayUnit이 없다.
					return AnimationPlaybackStatus.None;
				}

				if(_linkedAnimClip.IsPlaying_Opt)
				{
					//재생중 / 끝
					if(!_linkedAnimClip.IsLoop)
					{
						//Loop 타입이 아닐때 현재 프레임이 마지막 프레임인가
						if(_linkedAnimClip.CurFrame >= _linkedAnimClip.EndFrame)
						{
							return AnimationPlaybackStatus.Ended;
						}
					}
					return AnimationPlaybackStatus.Playing;
				}
				else
				{
					//일시 정지
					return AnimationPlaybackStatus.Paused;
				}
			}
		}

		public string Name
		{
			get
			{
				return _animClipName;
			}
		}

		/// <summary>
		/// Return the current frame at which the animation is played.
		/// (The returned value is of type int, but internally, it is operated with data of type float.)
		/// </summary>
		public int CurrentFrame
		{
			get
			{
				if (_linkedAnimClip == null) { return -1; }
				return _linkedAnimClip.CurFrame;
			}
			
		}
		public int StartFrame
		{
			get
			{
				if (_linkedAnimClip == null) { return -1; }
				return _linkedAnimClip.StartFrame;
			}
		}


		public int EndFrame
		{
			get
			{
				if (_linkedAnimClip == null) { return -1; }
				return _linkedAnimClip.EndFrame;
			}
		}

		/// <summary>
		/// Return the playing time as a value between 0 and 1.
		/// Return -1 if there is no target animation clip.
		/// </summary>
		public float NormalizedTime
		{
			get
			{
				if (_linkedAnimClip == null) { return -1.0f; }
				float fFrame = _linkedAnimClip.CurFrameFloat;
				int length = _linkedAnimClip.EndFrame - _linkedAnimClip.StartFrame;

				if(length <= 0)
				{
					return 0.0f;
				}

				return Mathf.Clamp01(fFrame / (float)length);

			}
		}


		//v1.5.2
		/// <summary>
		/// Returns the total length of the animation in seconds.
		/// This value is not affected by playback speed.
		/// Return -1 if there is no target animation clip.
		/// </summary>
		public float TimeLength
		{
			get
			{
				if (_linkedAnimClip == null)
				{
					return -1.0f;
				}

				return _linkedAnimClip.TimeLength;
			}
		}

		/// <summary>
		/// Returns the total length of the animation in seconds.
		/// This value changes depending on the playback speed.
		/// If the target animation clip does not exist or its playback speed is 0, -1 is returned.
		/// </summary>
		public float Duration
		{
			get
			{
				if(_linkedAnimClip == null)
				{
					return -1.0f;
				}

				float speedRatio = Mathf.Abs(_linkedAnimClip.SpeedRatio);
				if(speedRatio > 0.0f)
				{
					return _linkedAnimClip.TimeLength / speedRatio;
				}

				return -1.0f;
			}

		}

		/// <summary>
		/// Returns whether the animation is looping.
		/// </summary>
		public bool IsLoop
		{
			get
			{
				if(_linkedAnimClip == null)
				{
					return false;
				}

				return _linkedAnimClip.IsLoop;
			}
		}
	}

}