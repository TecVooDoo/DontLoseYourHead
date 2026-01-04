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
    /// 마스크 연결 정보. 마스크를 받는 Child Mesh에서 생성된다. (OptMesh용)
    /// Link 과정에서 생성되며 Serialize 되지 않는다.
    /// 에디터와 달리, 클리핑 마스크도 이 데이터를 이용하여 마스크를 전달받는다.
    /// </summary>
    public class apOptMaskLinkInfo
    {
        // 연결 타입
        public enum LINK_TYPE
        {
            Clipping, SendData
        }

        // Members
        //------------------------------
        private apOptMesh _maskMesh = null;
        private LINK_TYPE _linkType = LINK_TYPE.Clipping;

        //Send Data의 경우
        private apOptSendMaskData _parentSendData = null;

		//렌더 카메라에 등록된 Receiver
		private apOptMaskReceiver _linkedReceiver = null;

        //참고. Shared Mask를 받더라도, 데이터 수신은 Mesh > Mesh 관계이다.
        //Link Info를 바탕으로 Shader 프로퍼티 입력을 해야한다.
        //이 클래스의 역할은 Parent Mask Mesh로부터 데이터를 전달받아서 Shader 프로퍼티에 입력을 하도록 만드는 것이다.

        // Init
        //-------------------------------------------------------------
        public static apOptMaskLinkInfo MakeLink_Clipping(apOptMesh clippingParentMesh)
        {
            apOptMaskLinkInfo newLink = new apOptMaskLinkInfo();
            newLink.SetClipping(clippingParentMesh);
            return newLink;
        }

        public static apOptMaskLinkInfo MakeLink_SendData(apOptMesh sendMaskMesh, apOptSendMaskData sendData)
        {
            apOptMaskLinkInfo newLink = new apOptMaskLinkInfo();
            newLink.SetSendData(sendMaskMesh, sendData);
            return newLink;
        }


        private apOptMaskLinkInfo()
		{
			_maskMesh = null;
			_parentSendData = null;
			_linkedReceiver = null;
		}

        private void SetClipping(apOptMesh clippingParentMesh)
        {
            _maskMesh = clippingParentMesh;
            _linkType = LINK_TYPE.Clipping;
            _parentSendData = null;
        }

        public void SetSendData(apOptMesh sendMaskMesh, apOptSendMaskData sendData)
        {
            _maskMesh = sendMaskMesh;
            _linkType = LINK_TYPE.SendData;
            _parentSendData = sendData;
        }

		public void SetReceiver(apOptMaskReceiver receiver)
		{
			_linkedReceiver = receiver;
		}

        // Get        
        //-------------------------------------------------------------
        public apOptMesh MaskMesh { get { return _maskMesh; } }
        public LINK_TYPE LinkType { get { return _linkType; } }

        //Send Data의 경우
        public apOptSendMaskData ReceivedSendData { get { return _parentSendData; } }
		
		public apOptMaskReceiver LinkedReceiver { get { return _linkedReceiver; } }//사용하진 않지만 일단 만들어두면 어딘가 쓰지 않을까
    }
}