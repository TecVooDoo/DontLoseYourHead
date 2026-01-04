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
	/// 마스크 연결 정보. 이 값은 마스크를 받는 Child Mesh에서 생성된다. (Mesh TF용)
	/// Link 과정에서 생성되며 Serialize되지 않는다.
	/// </summary>
	public class apMaskLinkInfo
	{
		public apTransform_Mesh _parentMaskMeshTF = null;
		public apSendMaskData _parentMaskData = null;

		public apMaskLinkInfo()
		{
			_parentMaskMeshTF = null;
			_parentMaskData = null;
		}

		public void Link(	apTransform_Mesh parentMaskMeshTF,
							apSendMaskData parentMaskData)
		{
			_parentMaskMeshTF = parentMaskMeshTF;
			_parentMaskData = parentMaskData;
		}
	}
}