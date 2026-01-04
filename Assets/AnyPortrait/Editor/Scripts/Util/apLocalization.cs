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

using UnityEditor;

using AnyPortrait;

namespace AnyPortrait
{
	/// <summary>
	/// 텍스트를 설정에 맞게 번역하는 클래스
	/// Editor의 멤버로 존재하며, Editor에서 Language 옵션을 넣어준다.
	/// </summary>
	public class apLocalization
	{
		// Member
		//------------------------------------------------
		//텍스트를 받는다.
		


		private bool _isLoaded = false;
		public bool IsLoaded { get { return _isLoaded; } }
		private apEditor.LANGUAGE _language = apEditor.LANGUAGE.English;
		public apEditor.LANGUAGE Language { get { return _language; } }

		

		///// <summary>
		///// UI에 들어가는 텍스트 데이터
		///// </summary>
		//private Dictionary<UIWORD, UIWordSet> _uiWordSets = new Dictionary<UIWORD, UIWordSet>();

		//변경 : 선택된 Language만 포함된다.
		private int _nData_TEXT = 0;
		private int _nData_UIWORD = 0;
		private int _nData_V3 = 0;

		private string[] _dataArr_TEXT = null;
		private string[] _dataArr_UIWORD = null;
		private string[] _dataArr_V3 = null;

		//확장자 없이 파일명만
		private const string ASSET_NAME__TEXT = "apLangPack";
		private const string ASSET_NAME__UIWORD = "apLangPack_UI";
		private const string ASSET_NAME__V3 = "apLangPack_V3";


		

		// Function
		//------------------------------------------------
		public apLocalization()
		{
			_isLoaded = false;

			_dataArr_TEXT = null;
			_dataArr_UIWORD = null;
			_dataArr_V3 = null;
		}

		/// <summary>
		/// 다시 로드해야하는지 체크한다.
		/// "한번도 로드를 안했거나" / "선택한 언어가 아니라면" True 리턴
		/// </summary>
		/// <param name="language"></param>
		/// <returns></returns>
		public bool CheckToReloadLanguage(apEditor.LANGUAGE language)
		{
			return (!_isLoaded || _language != language);
		}


		//public void SetTextAsset(apEditor.LANGUAGE language, TextAsset textAsset_Dialog, TextAsset textAsset_UI)
		public void LoadTextAsset(apEditor.LANGUAGE language, bool isForce = false)
		{
			if (_isLoaded && _language == language && !isForce)
			{
				return;
			}

			int iText = GetLanguageIndex(language);
			if(iText < 0)
			{
				Debug.LogError("알 수 없는 언어 : " + language);
				//영어로 변경
				iText = GetLanguageIndex(apEditor.LANGUAGE.English);
			}
			_language = language;
			_isLoaded = true;


			//배열로도 만들자 [v1.4.5]
			_nData_TEXT = apEditorUtil.GetEnumCount(typeof(TEXT));
			_nData_UIWORD = apEditorUtil.GetEnumCount(typeof(UIWORD));
			_nData_V3 = apEditorUtil.GetEnumCount(typeof(TEXTV3));

			_dataArr_TEXT = new string[_nData_TEXT];
			_dataArr_UIWORD = new string[_nData_UIWORD];
			_dataArr_V3 = new string[_nData_V3];


			////추가 v1.6.2 : 사용자의 번역 보정 기능
			//apLocalizationCorrection correction = new apLocalizationCorrection();
			//correction.Load();
			//correction.Validate(_language);//유효성 테스트도 해야한다.


			//변경 v1.6.1
			ParseTextAsset(ASSET_NAME__TEXT,	_dataArr_TEXT		/*correction, apLocalizationCorrection.TARGET_ENUM.TEXT*/);
			ParseTextAsset(ASSET_NAME__UIWORD,	_dataArr_UIWORD	/*correction, apLocalizationCorrection.TARGET_ENUM.UIWORD*/);
			ParseTextAsset(ASSET_NAME__V3,		_dataArr_V3		/*correction, apLocalizationCorrection.TARGET_ENUM.TEXTV3*/);


			#region [미사용 코드]
			//string[] strParseLines = textAsset_Dialog.text.Split(new string[] { "\n" }, StringSplitOptions.None);

			//string strCurParseLine = null;

			//for (int i = 1; i < strParseLines.Length; i++)
			//{
			//	//첫줄(index 0)은 빼고 읽는다.
			//	strCurParseLine = strParseLines[i].Replace("\r", "");
			//	string[] strSubParseLine = strCurParseLine.Split(new string[] { "," }, StringSplitOptions.None);
			//	//Parse 순서
			//	//0 : TEXT 타입 (string) - 파싱 안한다.
			//	//1 : TEXT 타입 (int)
			//	//2 : English (영어)
			//	//3 : Korean (한국어)
			//	//4 : French (프랑스어)
			//	//5 : German (독일어)
			//	//6 : Spanish (스페인어)
			//	//7 : Italian (이탈리아어)
			//	//8 : Danish (덴마크어)
			//	//9 : Japanese (일본어)
			//	//10 : Chinese_Traditional (중국어-번체)
			//	//11 : Chinese_Simplified (중국어-간체)
			//	if (strSubParseLine.Length < 13)
			//	{
			//		//Debug.LogError("인식할 수 없는 Text (" + i + " : " + strCurParseLine + ")");
			//		continue;
			//	}
			//	try
			//	{	
			//		TEXT textType = (TEXT)(int.Parse(strSubParseLine[1]));

			//		//이전 코드
			//		//TextSet newTextSet = new TextSet(textType);

			//		//newTextSet.SetText(apEditor.LANGUAGE.English, strSubParseLine[2]);
			//		//newTextSet.SetText(apEditor.LANGUAGE.Korean, strSubParseLine[3]);
			//		//newTextSet.SetText(apEditor.LANGUAGE.French, strSubParseLine[4]);
			//		//newTextSet.SetText(apEditor.LANGUAGE.German, strSubParseLine[5]);
			//		//newTextSet.SetText(apEditor.LANGUAGE.Spanish, strSubParseLine[6]);
			//		//newTextSet.SetText(apEditor.LANGUAGE.Italian, strSubParseLine[7]);
			//		//newTextSet.SetText(apEditor.LANGUAGE.Danish, strSubParseLine[8]);
			//		//newTextSet.SetText(apEditor.LANGUAGE.Japanese, strSubParseLine[9]);
			//		//newTextSet.SetText(apEditor.LANGUAGE.Chinese_Traditional, strSubParseLine[10]);
			//		//newTextSet.SetText(apEditor.LANGUAGE.Chinese_Simplified, strSubParseLine[11]);
			//		//newTextSet.SetText(apEditor.LANGUAGE.Polish, strSubParseLine[12]);

			//		//_textSets.Add(textType, newTextSet);

			//		//변경된 코드
			//		string strCurText = ConvertText(strSubParseLine[iText], _language);
			//		_texts.Add(textType, strCurText);

			//		//[v1.4.5]
			//		_dataArr_TEXT[(int)textType] = strCurText;
			//	}
			//	catch (Exception)
			//	{
			//		Debug.LogError("Parsing 실패 (" + i + " : " + strCurParseLine + ")");
			//	}


			//}


			////UI 단어도 열자
			//strParseLines = textAsset_UI.text.Split(new string[] { "\n" }, StringSplitOptions.None);

			//for (int i = 1; i < strParseLines.Length; i++)
			//{
			//	//첫줄(index 0)은 빼고 읽는다.
			//	strCurParseLine = strParseLines[i].Replace("\r", "");
			//	string[] strSubParseLine = strCurParseLine.Split(new string[] { "," }, StringSplitOptions.None);
			//	//Parse 순서
			//	//0 : TEXT 타입 (string) - 파싱 안한다.
			//	//1 : TEXT 타입 (int)
			//	//2 : English (영어)
			//	//3 : Korean (한국어)
			//	//4 : French (프랑스어)
			//	//5 : German (독일어)
			//	//6 : Spanish (스페인어)
			//	//7 : Italian (이탈리아어)
			//	//8 : Danish (덴마크어)
			//	//9 : Japanese (일본어)
			//	//10 : Chinese_Traditional (중국어-번체)
			//	//11 : Chinese_Simplified (중국어-간체)
			//	if (strSubParseLine.Length < 13)
			//	{
			//		//Debug.LogError("인식할 수 없는 Text (" + i + " : " + strCurParseLine + ")");
			//		continue;
			//	}
			//	try
			//	{
			//		UIWORD uiWordType = (UIWORD)(int.Parse(strSubParseLine[1]));

			//		//이전 코드
			//		//UIWordSet newUIWordSet = new UIWordSet(uiWordType);

			//		//newUIWordSet.SetUIWord(apEditor.LANGUAGE.English, strSubParseLine[2]);
			//		//newUIWordSet.SetUIWord(apEditor.LANGUAGE.Korean, strSubParseLine[3]);
			//		//newUIWordSet.SetUIWord(apEditor.LANGUAGE.French, strSubParseLine[4]);
			//		//newUIWordSet.SetUIWord(apEditor.LANGUAGE.German, strSubParseLine[5]);
			//		//newUIWordSet.SetUIWord(apEditor.LANGUAGE.Spanish, strSubParseLine[6]);
			//		//newUIWordSet.SetUIWord(apEditor.LANGUAGE.Italian, strSubParseLine[7]);
			//		//newUIWordSet.SetUIWord(apEditor.LANGUAGE.Danish, strSubParseLine[8]);
			//		//newUIWordSet.SetUIWord(apEditor.LANGUAGE.Japanese, strSubParseLine[9]);
			//		//newUIWordSet.SetUIWord(apEditor.LANGUAGE.Chinese_Traditional, strSubParseLine[10]);
			//		//newUIWordSet.SetUIWord(apEditor.LANGUAGE.Chinese_Simplified, strSubParseLine[11]);
			//		//newUIWordSet.SetUIWord(apEditor.LANGUAGE.Polish, strSubParseLine[12]);

			//		//_uiWordSets.Add(uiWordType, newUIWordSet);

			//		//변경된 코드
			//		string strCurText = ConvertText(strSubParseLine[iText]);
			//		_uiWords.Add(uiWordType, strCurText);

			//		//변경된 코드
			//		 //[v1.4.5]
			//		_dataArr_UIWORD[(int)uiWordType] = strCurText;
			//	}
			//	catch (Exception)
			//	{
			//		Debug.LogError("Parsing 실패 (" + i + " : " + strCurParseLine + ")");
			//	}


			//} 
			#endregion



			_isLoaded = true;
		}

		private void ParseTextAsset(	string textAssetName,
										string[] targetData
										////사용자의 번역 보정
										//apLocalizationCorrection correction,
										//apLocalizationCorrection.TARGET_ENUM correctionType
										)
		{
			TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(apEditorUtil.MakePath_Text(textAssetName + ".txt"));
			string[] strParseLines = textAsset.text.Split(new string[] { "\n" }, StringSplitOptions.None);

			string strCurParseLine = null;

			int iText = GetLanguageIndex(_language);//언어별 Column Index

			if(iText < 0)
			{
				//영어로 변경
				iText = GetLanguageIndex(apEditor.LANGUAGE.English);
			}

			//첫줄부터 읽는다.
			//이전에는 첫줄에 각 항목의 이름이 있었는데, 이젠 첫줄부터 값이다.
			//
			for (int i = 0; i < strParseLines.Length; i++)
			{
				
				strCurParseLine = strParseLines[i].Replace("\r", "");
				string[] strSubParseLine = strCurParseLine.Split(new string[] { "," }, StringSplitOptions.None);
				
				if (strSubParseLine.Length < 13)
				{
					//Debug.LogError("인식할 수 없는 Text (" + i + " : " + strCurParseLine + ")");
					continue;
				}
				try
				{	
					//TEXT textType = (TEXT)(int.Parse(strSubParseLine[1]));//기존 : 두번짜 Column인 "Index"의 값을 파싱하여 사용
					int iTextType = i;//변경 v1.6.1 : 그냥 순서대로 넣는다.
					//TEXT textType = (TEXT)(int.Parse(strSubParseLine[1]));
					
					//하나씩 파싱해서 배열에 넣기
					string strCurText = ConvertText(strSubParseLine[iText], _language);
					targetData[iTextType] = strCurText;
				}
				catch (Exception)
				{
					Debug.LogError("Parsing 실패 (" + i + " : " + strCurParseLine + ")");
				}
			}

			int nTargetData = targetData.Length;

			//일단 미사용
			////사용자의 번역 보정이 있다면 덮어씌우자
			//List<apLocalizationCorrection.FixInfo> fixList = correction.GetFixInfoList(correctionType, _language);
			//int nFix = fixList != null ? fixList.Count : 0;
			//if(nFix > 0)
			//{
			//	apLocalizationCorrection.FixInfo curFix = null;
			//	for (int i = 0; i < nFix; i++)
			//	{
			//		curFix = fixList[i];
			//		if(curFix._validation != apLocalizationCorrection.VALIDATION.Valid)
			//		{
			//			//유효하지 않다.
			//			continue;
			//		}

			//		if(curFix._enumIndex < 0 || curFix._enumIndex >= nTargetData)
			//		{
			//			//범위를 벗어났다.
			//			continue;
			//		}

			//		//사용자의 번역 보정을 적용한다.
			//		targetData[curFix._enumIndex] = ConvertText(curFix._correctedText, _language);
			//	}
			//}
		}

		public string GetText(TEXT textType)
		{
			return _dataArr_TEXT[(int)textType];
		}

		public string GetUIWord(UIWORD uiWordType)
		{
			return _dataArr_UIWORD[(int)uiWordType];
		}

		public string GetTextV3(TEXTV3 textType)
		{
			return _dataArr_V3[(int)textType];
		}


		private string ConvertText(string srcText, apEditor.LANGUAGE language)
		{
			srcText = srcText.Replace("\t", "");
			srcText = srcText.Replace("[]", "\n");

			//언어에 따라 일부 값들은 다르게 파싱된다.
			if(language == apEditor.LANGUAGE.Japanese)
			{
				//일본어 쉼표
				srcText = srcText.Replace("[c]", "、");
			}
			else if(language == apEditor.LANGUAGE.Chinese_Traditional
				|| language == apEditor.LANGUAGE.Chinese_Simplified)
			{
				//중국어 쉼표
				srcText = srcText.Replace("[c]", "，");
			}
			else
			{
				//나머지 글자들은 평범한 쉼표
				srcText = srcText.Replace("[c]", ",");
			}
				
			
			srcText = srcText.Replace("[u]", "\"");
			return srcText;
		}

		private int GetLanguageIndex(apEditor.LANGUAGE language)
		{
			switch (language)
			{
				case apEditor.LANGUAGE.English:					return 2;
				case apEditor.LANGUAGE.Korean:					return 3;
				case apEditor.LANGUAGE.French:					return 4;
				case apEditor.LANGUAGE.German:					return 5;
				case apEditor.LANGUAGE.Spanish:					return 6;
				case apEditor.LANGUAGE.Italian:					return 7;
				case apEditor.LANGUAGE.Danish:					return 8;
				case apEditor.LANGUAGE.Japanese:				return 9;
				case apEditor.LANGUAGE.Chinese_Traditional:		return 10;
				case apEditor.LANGUAGE.Chinese_Simplified:		return 11;
				case apEditor.LANGUAGE.Polish:					return 12;
					
			}
			return -1;
		}
	}

}