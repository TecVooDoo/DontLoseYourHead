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
//using UnityEngine.Profiling;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;
using System;

using AnyPortrait;
//using UnityEditor.PackageManager;
//using System.Linq;

namespace AnyPortrait
{
	/// <summary>
	/// VR등의 이유로 카메라가 여러개 있는 경우에 한해서 생성되는 스크립트
	/// 이 스크립트는 Clipped optMesh (Child)에서 카메라와 연결하는 과정에서 실시간으로 생성된다.
	/// OnPreRender 이벤트를 optMesh로 보내주기 위함
	/// 
	/// </summary>
	public class apOptMultiCameraController : MonoBehaviour
	{
		// Members
		//------------------------------------------------
		private Camera _camera = null;

		public delegate void FUNC_MESH_PRE_RENDERED(Camera camera);
		
		// 이전에는 OptMesh를 키값으로 삼아서 생성/삭제를 했다.
		
		// 빠른 업데이트를 위해서 배열로 만들고, 래퍼 클래스를 만들자
		//private Dictionary<apOptMesh, FUNC_MESH_PRE_RENDERED> _meshPreRenderedEvents = new Dictionary<apOptMesh, FUNC_MESH_PRE_RENDERED>();

		//LoadKey + 이벤트 래퍼
		public class EventSet
		{
			public object _loadKey = null;
			public FUNC_MESH_PRE_RENDERED _funcEvent = null;

			public EventSet(object loadKey, FUNC_MESH_PRE_RENDERED funcEvent)
			{
				_loadKey = loadKey;
				_funcEvent = funcEvent;
			}

		}

		private Dictionary<object, EventSet> _key2Event = null;
		private EventSet[] _renderEvents = null;
		private int _nEvent = 0;
		
		private bool _isInit = false;
		private bool _isDestroyed = false;//삭제 중이라면..

		// Init
		//------------------------------------------------
		void Start()
		{
			if(!_isInit)
			{
				Init();
			}
		}

		public void Init()
		{
			if(_isInit)
			{
				return;
			}

			if(_camera == null)
			{
				_camera = gameObject.GetComponent<Camera>();
			}

			//추가 v1.6.0 : MultiCameraController는 인게임 중에 생성되며 저장되지 않아야 한다.
			hideFlags = HideFlags.DontSave;

			_key2Event = null;
			_renderEvents = null;

			_nEvent = 0;
			_isDestroyed = false;
			_isInit = true;
		}

		public bool IsInit()
		{
			return _isInit;
		}


		// Functions
		//------------------------------------------------
		//변경 : OptMesh가 아니라 범용적으로 이벤트를 등록한다.
		public object AddPreRenderEvent(object prevLoadKey, FUNC_MESH_PRE_RENDERED preRenderEvent)
		{
			if(preRenderEvent == null)
			{
				//유효하지 않은 이벤트
				return null;
			}

			if(_camera == null)
			{
				Debug.LogError("[" + name + "] Camera is null. Please check the camera.", gameObject);
			}
			
			//이미 등록되었는지 확인하자
			if(prevLoadKey != null && _key2Event != null)
			{
				EventSet existSet = null;
				_key2Event.TryGetValue(prevLoadKey, out existSet);
				if(existSet != null)
				{
					//이미 등록이 되었다면 리턴
					return existSet._loadKey;
				}
			}

			//새로 데이터를 입력하자
			object newKey = prevLoadKey;
			if(newKey == null)
			{
				newKey = new object();
			}

			if(_key2Event == null)
			{
				_key2Event = new Dictionary<object, EventSet>();
			}

			EventSet newSet = new EventSet(newKey, preRenderEvent);
			_key2Event.Add(newKey, newSet);

			//배열 갱신
			//_renderEvents = _key2Event.Values.ToArray();
			_nEvent = _key2Event.Count;
			if(_nEvent == 0)
			{
				_renderEvents = null;
			}
			else if(_nEvent > 0)
			{
				_renderEvents = new EventSet[_nEvent];
				int iEvent = 0;
				foreach (KeyValuePair<object, EventSet> eventPair in _key2Event)
				{
					//Debug.Log("> Add Render Event : " + _camera.gameObject.name);
					_renderEvents[iEvent] = eventPair.Value;
					iEvent++;
				}
			}
				
			

			return newKey;
		}


		//변경 : LoadKey를 기준으로 삭제 요청
		/// <summary>
		/// 저장된 PreRender 이벤트 삭제
		/// </summary>
		public void RemovePreRenderEvent(object loadKey)
		{
			if(loadKey == null
				|| _nEvent == 0)
			{
				return;
			}
			
			if(_key2Event == null)
			{
				_key2Event = new Dictionary<object, EventSet>();
			}

			EventSet targetEventSet = null;
			if(_key2Event != null)
			{
				_key2Event.TryGetValue(loadKey, out targetEventSet);
			}

			if(targetEventSet != null)
			{
				//등록되었다면 삭제하자
				_key2Event.Remove(loadKey);
			}

			_nEvent = _key2Event.Count;
			

			//배열 갱신
			if (_nEvent == 0)
			{
				_renderEvents = null;
			}
			else if(_nEvent > 0)
			{
				_renderEvents = new EventSet[_nEvent];
				int iEvent = 0;
				foreach (KeyValuePair<object, EventSet> eventPair in _key2Event)
				{
					_renderEvents[iEvent] = eventPair.Value;
					iEvent++;
				}
			}
			
			if(_nEvent == 0)
			{
				//모든 이벤트가 삭제되었다면
				//Debug.LogError("[" + name + "] Event is 0");
				_isDestroyed = true;
				Destroy(this);
			}
		}

		//Pre Render Event
		private void OnPreRender()
		{
			if (_nEvent == 0)
			{
				return;
			}

			//이전
			// apOptMesh optMesh = null;
			// FUNC_MESH_PRE_RENDERED funcMeshPreRendered = null;

			// foreach (KeyValuePair<apOptMesh, FUNC_MESH_PRE_RENDERED> pair in _meshPreRenderedEvents)
			// {
			// 	optMesh = pair.Key;
			// 	funcMeshPreRendered = pair.Value;

			// 	if(optMesh == null || funcMeshPreRendered == null)
			// 	{
			// 		//메시가 없다면 리스트를 다시 봐야 한다.
			// 		continue;
			// 	}

			// 	funcMeshPreRendered(_camera);
			// }

			//변경 v1.6.0
			EventSet curEventSet = null;
			for (int i = 0; i < _nEvent; i++)
			{
				curEventSet = _renderEvents[i];
				
				if(curEventSet._loadKey == null || curEventSet._funcEvent == null)
				{
					continue;
				}

				curEventSet._funcEvent(_camera);
			}
		}


		// Events
		//------------------------------------------------
		private void OnDisable()
		{
			if(!_isDestroyed)
			{
				//Debug.LogError("[" + name + "] On Disable");
				_isDestroyed = true;
				Destroy(this);
			}
		}


		// Get / Set
		//-------------------------------------------------
		// public Dictionary<apOptMesh, FUNC_MESH_PRE_RENDERED> GetPreRenderedEvents()
		// {
		// 	return _meshPreRenderedEvents;
		// }

		public bool IsDestroying()
		{
			return _isDestroyed;
		}

		

	}
}