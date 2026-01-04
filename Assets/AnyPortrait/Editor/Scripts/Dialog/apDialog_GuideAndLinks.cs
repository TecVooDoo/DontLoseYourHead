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
	// 홈페이지의 가이드와 링크를 보여주는 다이얼로그
	// 버전에 따라서는 내용이 다르다.
	public class apDialog_GuideAndLinks : EditorWindow
	{
		// Members
		//------------------------------------------------------------------
		private static apDialog_GuideAndLinks s_window = null;


		//메뉴 구성
		// (1) 가이드			|			(2) 링크들
		// - Getting Started				- 홈페이지
		// - Manual							- 버그 제보
		// - Scripting						- 질문
		// - Video Tutorial (글로벌 버전)	- 포럼
		//									- 에셋 스토어 (버전에 따라 다름 - 일단 중국어에서는 제외)

		private Texture2D _img_Icon_GettingStarted = null;
		private Texture2D _img_Icon_Manual = null;
		private Texture2D _img_Icon_Scripting = null;//<필요
		private Texture2D _img_Icon_VideoTutorial = null;

		private Texture2D _img_Icon_Homepage = null;//필요
		private Texture2D _img_Icon_BugReport = null;//필요
		private Texture2D _img_Icon_QnA = null;//필요
		private Texture2D _img_Icon_Forum = null;
		private Texture2D _img_Icon_AssetStore = null;//필요

		private apEditor.LANGUAGE _language = apEditor.LANGUAGE.English;

		private string _text_Tutorials = "";
		private string _text_Support = "";

		private string _text_GettingStarted = "";
		private string _text_Manual = "";
		private string _text_Scripting = "";
		private string _text_VideoTutorial = "";
		private string _text_Homepage = "";
		private string _text_BugReport = "";
		private string _text_QnA = "";
		private string _text_Forum = "";
		private string _text_AssetStore = "";

		private bool _isResourceLoaded = false;

		private GUIStyle _guiStyle_Button = null;

		private const int WIDTH_WINDOW = 600;
		//private const int WIDTH_HEIGHT = 250;

		private apGUIContentWrapper _guiContent = null;


		// Show Windows
		//---------------------------------------------------------------------
		[MenuItem("Window/AnyPortrait/Tutorials and Support", false, 781)]
		public static void ShowDialog()
		{
			CloseDialog();

			EditorWindow curWindow = EditorWindow.GetWindow(typeof(apDialog_GuideAndLinks), true, "Tutorials and Support", true);
			apDialog_GuideAndLinks curTool = curWindow as apDialog_GuideAndLinks;

			//object loadKey = new object();
			if (curTool != null && curTool != s_window)
			{
				int width = WIDTH_WINDOW;
				//int height = WIDTH_HEIGHT;
				int height = 250;

				//if(apVersion.I.IsCNStore)
				//{
				//	height = 220;
				//}

				s_window = curTool;
				s_window.position = new Rect(300, 300, width, height);
				s_window.Init();
			}
		}


		public static void CloseDialog()
		{
			if (s_window != null)
			{
				try
				{
					s_window.Close();
				}
				catch (Exception ex)
				{
					Debug.LogError("Close Exception : " + ex);
				}
				s_window = null;
			}
		}

		// Init
		//------------------------------------------------------------------
		public void Init()
		{
			apEditor.LANGUAGE defaultLanguage = apEditor.LANGUAGE.English;
			if(apVersion.I.IsCNStore)
			{
				defaultLanguage = apEditor.LANGUAGE.Chinese_Simplified;
			}

			_language = (apEditor.LANGUAGE)EditorPrefs.GetInt(apVersion.I.LanguagePrefKey, (int)defaultLanguage);//AnyPortrait_Language
		}


		void OnGUI()
		{
			int width = (int)position.width;
			int height = (int)position.height;

			if (!_isResourceLoaded)
			{
				LoadResources();
			}


			if(_guiContent == null)
			{
				_guiContent = new apGUIContentWrapper();
			}

			if(_guiStyle_Button == null)
			{
				_guiStyle_Button = new GUIStyle(GUI.skin.button);
				_guiStyle_Button.alignment = TextAnchor.MiddleLeft;				
			}

			GUILayout.Space(5);

			//절반씩 나눈다.
			int width_Half = (width - 15) / 2;
			int height_List = height - 10;
			EditorGUILayout.BeginHorizontal(GUILayout.Width(width), GUILayout.Height(height_List));
			GUILayout.Space(5);

			EditorGUILayout.BeginVertical(GUILayout.Width(width_Half), GUILayout.Height(height_List));
			DrawUI_Guides(width_Half, height_List);
			EditorGUILayout.EndVertical();

			GUILayout.Space(5);
			EditorGUILayout.BeginVertical(GUILayout.Width(width_Half), GUILayout.Height(height_List));
			DrawUI_Links(width_Half, height - 10);
			EditorGUILayout.EndVertical();

			EditorGUILayout.EndHorizontal();


		}

		private void DrawUI_Guides(int width, int height)
		{
			EditorGUILayout.Foldout(true, _text_Tutorials);
			GUILayout.Space(5);

			// (1) 가이드
			// - Getting Started
			// - Manual
			// - Scripting
			// - Video Tutorial (글로벌 버전)

			// - Getting Started
			if(DrawButton(_img_Icon_GettingStarted, _text_GettingStarted))
			{
				string url = "";

				if(_language == apEditor.LANGUAGE.Korean)
				{	
					url = "https://rainyrizzle.github.io/kr/GettingStarted.html";
				}
				else if(_language == apEditor.LANGUAGE.Japanese)
				{
					url = "https://rainyrizzle.github.io/jp/GettingStarted.html";
				}
				else
				{
					url = "https://rainyrizzle.github.io/en/GettingStarted.html";
				}
				Application.OpenURL(url);
			}


			// - Manual
			if(DrawButton(_img_Icon_Manual, _text_Manual))
			{
				string url = "";

				if(_language == apEditor.LANGUAGE.Korean)
				{	
					url = "https://rainyrizzle.github.io/kr/AdManual.html";
				}
				else if(_language == apEditor.LANGUAGE.Japanese)
				{	
					url = "https://rainyrizzle.github.io/jp/AdManual.html";
				}
				else
				{
					url = "https://rainyrizzle.github.io/en/AdManual.html";
				}
				Application.OpenURL(url);
			}

			// - Scripting
			if(DrawButton(_img_Icon_Scripting, _text_Scripting))
			{
				string url = "";

				if(_language == apEditor.LANGUAGE.Korean)
				{
					url = "https://rainyrizzle.github.io/kr/Script.html";
				}
				else if(_language == apEditor.LANGUAGE.Japanese)
				{
					url = "https://rainyrizzle.github.io/jp/Script.html";
				}
				else
				{
					url = "https://rainyrizzle.github.io/en/Script.html";
				}
				Application.OpenURL(url);
			}

			// - Video Tutorial (글로벌 버전 한정)
			if(!apVersion.I.IsCNStore)
			{
				if(DrawButton(_img_Icon_VideoTutorial, _text_VideoTutorial))
				{
					string url = "";

					if(_language == apEditor.LANGUAGE.Korean)
					{
						url = "https://rainyrizzle.com/ko/ap-video-tutorials-ko/";//주소 변경 (v1.6.1)
					}
					else if(_language == apEditor.LANGUAGE.Japanese)
					{
						url = "https://rainyrizzle.com/ja/ap-video-tutorials-ja/";//주소 변경 (v1.6.1)
					}
					else
					{
						url = "https://rainyrizzle.com/en/ap-video-tutorials-en/";//주소 변경 (v1.6.1)
					}
					Application.OpenURL(url);
				}
			}
		}

		private void DrawUI_Links(int width, int height)
		{
			EditorGUILayout.Foldout(true, _text_Support);
			GUILayout.Space(5);
			// (2) 링크들
			// - 홈페이지
			// - 에셋 스토어 (버전에 따라 다름 - 일단 중국어에서는 제외)

			
			// - 버그 제보
			// - 질문
			// - 포럼
			
			

			// - 홈페이지
			if (DrawButton(_img_Icon_Homepage, _text_Homepage))
			{
				Application.OpenURL("https://www.rainyrizzle.com/");
			}

			// - 에셋 스토어 (버전에 따라 다름 - 일단 중국어에서는 제외)
			if (DrawButton(_img_Icon_AssetStore, _text_AssetStore))
			{
				apEditorUtil.OpenAssetStorePage();
			}

			GUILayout.Space(5);

			// - 버그 제보
			if (DrawButton(_img_Icon_BugReport, _text_BugReport))
			{
				string url = "";

				if(_language == apEditor.LANGUAGE.Korean)
				{
					url = "https://rainyrizzle.com/ko/report-ko/";//URP 변경 (v1.6.1)
				}
				else if(_language == apEditor.LANGUAGE.Japanese)
				{
					url = "https://rainyrizzle.com/ja/report-ja/";//URP 변경 (v1.6.1)
				}
				else
				{
					url = "https://rainyrizzle.com/en/report-en/";//URP 변경 (v1.6.1)
				}
				Application.OpenURL(url);
			}

			// - 질문
			if (DrawButton(_img_Icon_QnA, _text_QnA))
			{
				string url = "";
				if(_language == apEditor.LANGUAGE.Korean)
				{
					url = "https://rainyrizzle.com/ko/qa-ko/";//주소 변경 (v1.6.1)
				}
				else if(_language == apEditor.LANGUAGE.Japanese)
				{
					url = "https://rainyrizzle.com/ja/qa-ja/";//주소 변경 (v1.6.1)
				}
				else
				{
					url = "https://rainyrizzle.com/en/qa-en/";//주소 변경 (v1.6.1)
				}
				Application.OpenURL(url);
			}

			// - 포럼
			if (DrawButton(_img_Icon_Forum, _text_Forum))
			{
				string url = "";

				if(_language == apEditor.LANGUAGE.Korean)
				{
					url = "https://rainyrizzle.com/ko/forum-ko/";//URP 변경 (v1.6.1)
				}
				else if(_language == apEditor.LANGUAGE.Japanese)
				{
					url = "https://rainyrizzle.com/ja/forum-ja/";//URP 변경 (v1.6.1)
				}
				else
				{
					url = "https://rainyrizzle.com/en/forum-en/";//URP 변경 (v1.6.1)
				}

				Application.OpenURL(url);
			}
		}


		


		private void LoadResources()
		{
			apEditor.LANGUAGE defaultLanguage = apEditor.LANGUAGE.English;
			if(apVersion.I.IsCNStore)
			{
				defaultLanguage = apEditor.LANGUAGE.Chinese_Simplified;
			}

			_language = (apEditor.LANGUAGE)EditorPrefs.GetInt(apVersion.I.LanguagePrefKey, (int)defaultLanguage);//AnyPortrait_Language

			_img_Icon_GettingStarted = LoadImage("StartPage_GettingStarted");
			_img_Icon_Manual = LoadImage("StartPage_Manual");
			_img_Icon_Scripting = LoadImage("StartPage_Scripting");
			_img_Icon_VideoTutorial = LoadImage("StartPage_VideoTutorial");

			_img_Icon_Homepage = LoadImage("StartPage_Homepage");
			_img_Icon_BugReport = LoadImage("StartPage_ReportBug");
			_img_Icon_QnA = LoadImage("StartPage_QnA");
			_img_Icon_Forum = LoadImage("StartPage_Forum");
			_img_Icon_AssetStore = LoadImage("StartPage_AssetStore");


			switch (_language)
			{
				case apEditor.LANGUAGE.English://영어
					_text_Tutorials = "Tutorials";
					_text_Support = "Support";

					_text_GettingStarted = "Getting Started";
					_text_Manual = "Manual";
					_text_Scripting = "Script";
					_text_VideoTutorial = "Video Tutorials";
					_text_Homepage = "Homepage";
					_text_BugReport = "Report a Bug";
					_text_QnA = "Ask a Question";
					_text_Forum = "Forum";
					_text_AssetStore = "Asset Store";
					break;
			
				case apEditor.LANGUAGE.Korean://한국어
					_text_Tutorials = "튜토리얼";
					_text_Support = "지원";

					_text_GettingStarted = "시작하기";
					_text_Manual = "메뉴얼";
					_text_Scripting = "스크립트";
					_text_VideoTutorial = "동영상 튜토리얼";
					_text_Homepage = "홈페이지";
					_text_BugReport = "버그 제보하기";
					_text_QnA = "질문하기";
					_text_Forum = "게시판";
					_text_AssetStore = "에셋 스토어";
					break;

				case apEditor.LANGUAGE.French://프랑스어
					_text_Tutorials = "Tutoriels";
					_text_Support = "Assistance";

					_text_GettingStarted = "Démarrage";
					_text_Manual = "Manuel";
					_text_Scripting = "Script";
					_text_VideoTutorial = "Tutoriels Vidéo";
					_text_Homepage = "Page d'accueil";
					_text_BugReport = "Signaler un Bug";
					_text_QnA = "Poser une Question";
					_text_Forum = "Forum";
					_text_AssetStore = "Asset Store";
					break;

				case apEditor.LANGUAGE.German://독일어
					_text_Tutorials = "Tutorials";
					_text_Support = "Support";

					_text_GettingStarted = "Erste Schritte";
					_text_Manual = "Handbuch";
					_text_Scripting = "Skript";
					_text_VideoTutorial = "Video-Tutorials";
					_text_Homepage = "Startseite";
					_text_BugReport = "Fehler melden";
					_text_QnA = "Frage stellen";
					_text_Forum = "Forum";
					_text_AssetStore = "Asset Store";
					break;

				case apEditor.LANGUAGE.Spanish://스페인어
					_text_Tutorials = "Tutoriales";
					_text_Support = "Soporte";

					_text_GettingStarted = "Tutorial para principiantes";
					_text_Manual = "Manual";
					_text_Scripting = "Script";
					_text_VideoTutorial = "Videotutoriales";
					_text_Homepage = "Página principal";
					_text_BugReport = "Informar de un error";
					_text_QnA = "Preguntar";
					_text_Forum = "Foro";
					_text_AssetStore = "Asset Store";
					break;

				case apEditor.LANGUAGE.Italian://이탈리아어
					_text_Tutorials = "Tutorial";
					_text_Support = "Supporto";

					_text_GettingStarted = "Guida introduttiva";
					_text_Manual = "Manuale";
					_text_Scripting = "Script";
					_text_VideoTutorial = "Tutorial video";
					_text_Homepage = "Homepage";
					_text_BugReport = "Segnala un bug";
					_text_QnA = "Fai una domanda";
					_text_Forum = "Forum";
					_text_AssetStore = "Asset Store";
					break;

				case apEditor.LANGUAGE.Danish://덴마크어
					_text_Tutorials = "Vejledninger";
					_text_Support = "Support";

					_text_GettingStarted = "Kom godt i gang";
					_text_Manual = "Manual";
					_text_Scripting = "Script";
					_text_VideoTutorial = "Videovejledninger";
					_text_Homepage = "Hjemmeside";
					_text_BugReport = "Rapporter en fejl";
					_text_QnA = "Stil et spørgsmål";
					_text_Forum = "Forum";
					_text_AssetStore = "Asset Store";
					break;

				case apEditor.LANGUAGE.Japanese://일본어
					_text_Tutorials = "チュートリアル";
					_text_Support = "サポート";

					_text_GettingStarted = "入門ガイド";
					_text_Manual = "マニュアル";
					_text_Scripting = "スクリプト";
					_text_VideoTutorial = "ビデオチュートリアル";
					_text_Homepage = "ホームページ";
					_text_BugReport = "バグを報告する";
					_text_QnA = "質問する";
					_text_Forum = "フォーラム";
					_text_AssetStore = "アセットストア";
					break;

				case apEditor.LANGUAGE.Chinese_Traditional://중국어-번체
					_text_Tutorials = "教學";
					_text_Support = "支援";

					_text_GettingStarted = "入門指南";
					_text_Manual = "手冊";
					_text_Scripting = "腳本";
					_text_VideoTutorial = "影片教學";
					_text_Homepage = "首頁";
					_text_BugReport = "報告錯誤";
					_text_QnA = "提問";
					_text_Forum = "論壇";
					_text_AssetStore = "資源商店";
					break;

				case apEditor.LANGUAGE.Chinese_Simplified://중국어-간체
					_text_Tutorials = "教学";
					_text_Support = "支持";

					_text_GettingStarted = "入门指南";
					_text_Manual = "手册";
					_text_Scripting = "脚本";
					_text_VideoTutorial = "影片教学";
					_text_Homepage = "首页";
					_text_BugReport = "报告错误";
					_text_QnA = "提问";
					_text_Forum = "论坛";
					_text_AssetStore = "资源商店";
					break;

				case apEditor.LANGUAGE.Polish://폴란드어
					_text_Tutorials = "Samouczki";
					_text_Support = "Wsparcie";

					_text_GettingStarted = "Pierwsze kroki";
					_text_Manual = "Instrukcja";
					_text_Scripting = "Skrypt";
					_text_VideoTutorial = "Samouczki wideo";
					_text_Homepage = "Strona główna";
					_text_BugReport = "Zgłoś błąd";
					_text_QnA = "Zadaj pytanie";
					_text_Forum = "Forum";
					_text_AssetStore = "Asset Store";
					break;
			}

			_isResourceLoaded = true;
		}

		private Texture2D LoadImage(string imageName)
		{
			return AssetDatabase.LoadAssetAtPath<Texture2D>(apEditorUtil.MakePath_Icon(imageName, false));
		}


		private bool DrawButton(Texture2D img, string text)
		{
			_guiContent.ClearAll();
			_guiContent.SetImage(img);
			_guiContent.SetText(3, text);
			
			if(GUILayout.Button(_guiContent.Content, _guiStyle_Button, GUILayout.Height(36)))
			{
				return true;
			}

			return false;
		}
	}
}