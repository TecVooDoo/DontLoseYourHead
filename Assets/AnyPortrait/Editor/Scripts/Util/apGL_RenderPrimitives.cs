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
using System.Collections.Generic;
using System;

using AnyPortrait;

namespace AnyPortrait
{
	// apGL 중의 함수 중에서 기본 도형 렌더링 함수들만 모았다.
	public static partial class apGL
	{
		// Pass 시작 / 종료
		//---------------------------------------
		//삭제 21.5.18 : 이 함수는 사용하지 않는다. 직접 호출할 것 (?)
		public static void BeginBatch_ColoredPolygon()
		{
			_matBatch.BeginPass_Color(GL.TRIANGLES);
		}


		public static void BeginBatch_ColoredLine()
		{
			//변경 21.5.18
			_matBatch.BeginPass_Color(GL.LINES);
		}

		//남은 모든 패스를 종료한다.
		public static void EndPass()
		{
			_matBatch.EndPass();
		}

		public static void RefreshScreenSizeToBatch()
		{
			_matBatch.SetClippingSizeToAllMaterial(_glScreenClippingSize);
		}



		//-------------------------------------------------------------------------------
		// 직선
		//-------------------------------------------------------------------------------
		public static void DrawLine(Vector2 pos1, Vector2 pos2, Color color, bool isNeedResetMat)
		{
			if (_matBatch.IsNotReady())
			{ return; }

			if (Vector2.Equals(pos1, pos2)) { return; }

			//이전
			//pos1 = World2GL(pos1);
			//pos2 = World2GL(pos2);

			//변경
			Vector3 pos3_1 = Vector3.zero;
			Vector3 pos3_2 = Vector3.zero;
			World2GL_Vec3(ref pos3_1, ref pos1);
			World2GL_Vec3(ref pos3_2, ref pos2);

			if (isNeedResetMat)
			{
				_matBatch.BeginPass_Color(GL.LINES);

				//삭제 21.5.18
				//_matBatch.SetClippingSize(_glScreenClippingSize);
				//GL.Begin(GL.LINES);
			}

			GL.Color(color);
			GL.Vertex(pos3_1);
			GL.Vertex(pos3_2);

			if (isNeedResetMat)
			{
				//GL.End();//<전환 완료>
				//GL.Flush();
				_matBatch.EndPass();
			}
		}

		public static void DrawLineGL(Vector2 pos1_GL, Vector2 pos2_GL, Color color, bool isNeedResetMat)
		{
			if (_matBatch.IsNotReady())
			{ return; }

			if (Vector2.Equals(pos1_GL, pos2_GL))
			{ return; }


			if (isNeedResetMat)
			{
				_matBatch.BeginPass_Color(GL.LINES);

				//삭제 21.5.18
				//_matBatch.SetClippingSize(_glScreenClippingSize);
				//GL.Begin(GL.LINES);
			}

			GL.Color(color);
			GL.Vertex(new Vector3(pos1_GL.x, pos1_GL.y, 0.0f));
			GL.Vertex(new Vector3(pos2_GL.x, pos2_GL.y, 0.0f));


			//삭제 21.5.18
			if (isNeedResetMat)
			{
				//GL.End();//<전환 완료>
				//GL.Flush();
				_matBatch.EndPass();
			}
		}




		//추가 : 애니메이션되는 라인
		public static void DrawAnimatedLine(Vector2 pos1, Vector2 pos2, Color color, bool isNeedResetMat)
		{
			if (_matBatch.IsNotReady())
			{ return; }

			if (Vector2.Equals(pos1, pos2))
			{ return; }

			pos1 = World2GL(pos1);
			pos2 = World2GL(pos2);

			if (isNeedResetMat)
			{
				//변경 21.5.18
				_matBatch.BeginPass_Color(GL.LINES);
			}

			GL.Color(color);

			Vector2 vLine = (pos2 - pos1);
			float remainedLength = vLine.magnitude;
			vLine.Normalize();
			float startOffset = _animationTimeRatio * (ANIMATED_LINE_UNIT_LENGTH + ANIMATED_LINE_SPACE_LENGTH);
			Vector2 curPos = pos1 + vLine * startOffset;
			remainedLength -= startOffset;

			if(startOffset - ANIMATED_LINE_SPACE_LENGTH > 0)
			{
				GL.Vertex(new Vector3(pos1.x, pos1.y, 0.0f));
				GL.Vertex(new Vector3(	pos1.x + vLine.x * (startOffset - ANIMATED_LINE_SPACE_LENGTH), 
										pos1.y + vLine.y * (startOffset - ANIMATED_LINE_SPACE_LENGTH), 0.0f));
			}
			
			//움직이는 점선라인을 그리자
			while(true)
			{
				if(remainedLength < 0.0f)
				{
					break;
				}

				GL.Vertex(new Vector3(curPos.x, curPos.y, 0.0f));
				if(remainedLength > ANIMATED_LINE_UNIT_LENGTH)
				{
					GL.Vertex(new Vector3(	curPos.x + vLine.x * ANIMATED_LINE_UNIT_LENGTH, 
											curPos.y + vLine.y * ANIMATED_LINE_UNIT_LENGTH, 
											0.0f));
				}
				else
				{
					GL.Vertex(new Vector3(	curPos.x + vLine.x * remainedLength, 
											curPos.y + vLine.y * remainedLength, 
											0.0f));
					break;
				}
				//이동
				curPos += vLine * (ANIMATED_LINE_UNIT_LENGTH + ANIMATED_LINE_SPACE_LENGTH);
				remainedLength -= ANIMATED_LINE_UNIT_LENGTH + ANIMATED_LINE_SPACE_LENGTH;
			}

			//삭제 21.5.18
			if (isNeedResetMat)
			{
				//GL.End();//<전환 완료>
				//GL.Flush();
				_matBatch.EndPass();
			}
		}

		public static void DrawAnimatedLineGL(Vector2 pos1_GL, Vector2 pos2_GL, Color color, bool isNeedResetMat)
		{
			if (_matBatch.IsNotReady())
			{ return; }

			if (Vector2.Equals(pos1_GL, pos2_GL))
			{ return; }


			if (isNeedResetMat)
			{
				//변경 21.5.18
				_matBatch.BeginPass_Color(GL.LINES);

				//_matBatch.SetClippingSize(_glScreenClippingSize);
				//GL.Begin(GL.LINES);
			}

			GL.Color(color);

			Vector2 vLine = (pos2_GL - pos1_GL);
			float remainedLength = vLine.magnitude;
			vLine.Normalize();
			float startOffset = _animationTimeRatio * (ANIMATED_LINE_UNIT_LENGTH + ANIMATED_LINE_SPACE_LENGTH);
			Vector2 curPos = pos1_GL + vLine * startOffset;
			remainedLength -= startOffset;

			if(startOffset - ANIMATED_LINE_SPACE_LENGTH > 0)
			{
				GL.Vertex(new Vector3(pos1_GL.x, pos1_GL.y, 0.0f));
				GL.Vertex(new Vector3(	pos1_GL.x + vLine.x * (startOffset - ANIMATED_LINE_SPACE_LENGTH), 
										pos1_GL.y + vLine.y * (startOffset - ANIMATED_LINE_SPACE_LENGTH), 0.0f));
			}

			//움직이는 점선라인을 그리자
			while(true)
			{
				if(remainedLength < 0.0f)
				{
					break;
				}

				GL.Vertex(new Vector3(curPos.x, curPos.y, 0.0f));
				if(remainedLength > ANIMATED_LINE_UNIT_LENGTH)
				{
					GL.Vertex(new Vector3(	curPos.x + vLine.x * ANIMATED_LINE_UNIT_LENGTH, 
											curPos.y + vLine.y * ANIMATED_LINE_UNIT_LENGTH, 
											0.0f));
				}
				else
				{
					GL.Vertex(new Vector3(	curPos.x + vLine.x * remainedLength, 
											curPos.y + vLine.y * remainedLength, 
											0.0f));
					break;
				}
				//이동
				curPos += vLine * (ANIMATED_LINE_UNIT_LENGTH + ANIMATED_LINE_SPACE_LENGTH);
				remainedLength -= ANIMATED_LINE_UNIT_LENGTH + ANIMATED_LINE_SPACE_LENGTH;
			}

			//삭제 21.5.18
			if (isNeedResetMat)
			{
				//GL.End();//<전환 완료>
				//GL.Flush();
				_matBatch.EndPass();
			}
		}




		public static void DrawDotLine(Vector2 pos1, Vector2 pos2, Color color, bool isNeedResetMat)
		{
			if (_matBatch.IsNotReady())
			{ return; }

			if (Vector2.Equals(pos1, pos2))
			{ return; }

			pos1 = World2GL(pos1);
			pos2 = World2GL(pos2);

			if (isNeedResetMat)
			{
				//변경 21.5.18
				_matBatch.BeginPass_Color(GL.LINES);

				//_matBatch.SetClippingSize(_glScreenClippingSize);
				//GL.Begin(GL.LINES);
			}

			GL.Color(color);

			Vector2 vLine = (pos2 - pos1);
			float remainedLength = vLine.magnitude;
			vLine.Normalize();
			//float startOffset = _animationTimeRatio * (ANIMATED_LINE_UNIT_LENGTH + ANIMATED_LINE_SPACE_LENGTH);
			//Vector2 curPos = pos1 + vLine * startOffset;
			Vector2 curPos = pos1 + vLine;
			//remainedLength -= startOffset;

			//if(startOffset - ANIMATED_LINE_SPACE_LENGTH > 0)
			//{
			//	GL.Vertex(new Vector3(pos1.x, pos1.y, 0.0f));
			//	GL.Vertex(new Vector3(	pos1.x + vLine.x * (startOffset - ANIMATED_LINE_SPACE_LENGTH), 
			//							pos1.y + vLine.y * (startOffset - ANIMATED_LINE_SPACE_LENGTH), 0.0f));
			//}
			
			//움직이는 점선라인을 그리자
			while(true)
			{
				if(remainedLength < 0.0f)
				{
					break;
				}

				GL.Vertex(new Vector3(curPos.x, curPos.y, 0.0f));
				if(remainedLength > ANIMATED_LINE_UNIT_LENGTH)
				{
					GL.Vertex(new Vector3(	curPos.x + vLine.x * ANIMATED_LINE_UNIT_LENGTH, 
											curPos.y + vLine.y * ANIMATED_LINE_UNIT_LENGTH, 
											0.0f));
				}
				else
				{
					GL.Vertex(new Vector3(	curPos.x + vLine.x * remainedLength, 
											curPos.y + vLine.y * remainedLength, 
											0.0f));
					break;
				}
				//이동
				curPos += vLine * (ANIMATED_LINE_UNIT_LENGTH + ANIMATED_LINE_SPACE_LENGTH);
				remainedLength -= ANIMATED_LINE_UNIT_LENGTH + ANIMATED_LINE_SPACE_LENGTH;
			}


			//삭제 21.5.18
			if (isNeedResetMat)
			{
				//GL.End();//<전환 완료>
				//GL.Flush();
				_matBatch.EndPass();
			}
		}



		//-------------------------------------------------------------------------------
		// 사각형 (Box)
		//-------------------------------------------------------------------------------
		public static void DrawBox(Vector2 pos, float width, float height, Color color, bool isWireframe)
		{
			DrawBox(pos, width, height, color, isWireframe, true);
		}


		public static void DrawBox(Vector2 pos, float width, float height, Color color, bool isWireframe, bool isNeedResetMat)
		{
			if (_matBatch.IsNotReady())
			{ return; }

			pos = World2GL(pos);

			float halfWidth = width * 0.5f * _zoom;
			float halfHeight = height * 0.5f * _zoom;

			//CW
			// -------->
			// | 0(--) 1
			// | 		
			// | 3   2 (++)
			Vector2 pos_0 = new Vector2(pos.x - halfWidth, pos.y - halfHeight);
			Vector2 pos_1 = new Vector2(pos.x + halfWidth, pos.y - halfHeight);
			Vector2 pos_2 = new Vector2(pos.x + halfWidth, pos.y + halfHeight);
			Vector2 pos_3 = new Vector2(pos.x - halfWidth, pos.y + halfHeight);

			if (isWireframe)
			{
				if (isNeedResetMat)
				{
					//변경 21.5.18
					_matBatch.BeginPass_Color(GL.LINES);

					//_matBatch.SetClippingSize(_glScreenClippingSize);
					//GL.Begin(GL.LINES);
				}

				GL.Color(color);
				GL.Vertex(pos_0);
				GL.Vertex(pos_1);

				GL.Vertex(pos_1);
				GL.Vertex(pos_2);

				GL.Vertex(pos_2);
				GL.Vertex(pos_3);

				GL.Vertex(pos_3);
				GL.Vertex(pos_0);


				//삭제 21.5.18
				if (isNeedResetMat)
				{
					//GL.End();//<전환 완료>
					//GL.Flush();
					_matBatch.EndPass();
				}
			}
			else
			{
				//CW
				// -------->
				// | 0   1
				// | 		
				// | 3   2
				if (isNeedResetMat)
				{
					//변경 21.5.18
					_matBatch.BeginPass_Color(GL.TRIANGLES);

					//_matBatch.SetClippingSize(_glScreenClippingSize);
					//GL.Begin(GL.TRIANGLES);
				}
				GL.Color(color);
				GL.Vertex(pos_0); // 0
				GL.Vertex(pos_1); // 1
				GL.Vertex(pos_2); // 2

				GL.Vertex(pos_2); // 2
				GL.Vertex(pos_3); // 3
				GL.Vertex(pos_0); // 0

				//삭제 21.5.18
				if (isNeedResetMat)
				{
					//GL.End();//<전환 완료>
					//GL.Flush();
					_matBatch.EndPass();
				}
			}

			//GL.Flush();
		}


		public static void DrawBoxGL(Vector2 pos, float width, float height, Color color, bool isWireframe, bool isNeedResetMat)
		{
			if (_matBatch.IsNotReady())
			{ return; }

			float halfWidth = width * 0.5f;
			float halfHeight = height * 0.5f;

			//CW
			// -------->
			// | 0(--) 1
			// | 		
			// | 3   2 (++)
			Vector2 pos_0 = new Vector2(pos.x - halfWidth, pos.y - halfHeight);
			Vector2 pos_1 = new Vector2(pos.x + halfWidth, pos.y - halfHeight);
			Vector2 pos_2 = new Vector2(pos.x + halfWidth, pos.y + halfHeight);
			Vector2 pos_3 = new Vector2(pos.x - halfWidth, pos.y + halfHeight);

			if (isWireframe)
			{
				if (isNeedResetMat)
				{
					//변경 21.5.18
					_matBatch.BeginPass_Color(GL.LINES);

					//_matBatch.SetClippingSize(_glScreenClippingSize);
					//GL.Begin(GL.LINES);
				}

				GL.Color(color);
				GL.Vertex(pos_0);
				GL.Vertex(pos_1);
				GL.Vertex(pos_1);
				GL.Vertex(pos_2);
				GL.Vertex(pos_2);
				GL.Vertex(pos_3);
				GL.Vertex(pos_3);
				GL.Vertex(pos_0);

				//삭제 21.5.18
				if (isNeedResetMat)
				{
					//GL.End();//<전환 완료>
					//GL.Flush();
					_matBatch.EndPass();
				}
			}
			else
			{
				//CW
				// -------->
				// | 0   1
				// | 		
				// | 3   2
				if (isNeedResetMat)
				{
					//변경 21.5.18
					_matBatch.BeginPass_Color(GL.TRIANGLES);

					//_matBatch.SetClippingSize(_glScreenClippingSize);
					//GL.Begin(GL.TRIANGLES);
				}
				GL.Color(color);
				// 0 - 1 - 2
				GL.Vertex(pos_0);
				GL.Vertex(pos_1);
				GL.Vertex(pos_2);

				// 2 - 3 - 0
				GL.Vertex(pos_2);
				GL.Vertex(pos_3);
				GL.Vertex(pos_0);

				//삭제 21.5.18
				if (isNeedResetMat)
				{
					//GL.End();//<전환 완료>
					//GL.Flush();
					_matBatch.EndPass();
				}
			}

			//GL.Flush();
		}


		public static void DrawBoxGL_PixelPerfect(int posX_Left, int posY_Bottom, int width, int height, Color color, bool isWireframe, bool isNeedResetMat)
		{
			if (_matBatch.IsNotReady()) { return; }

			//float halfWidth = width * 0.5f * _zoom;
			//float halfHeight = height * 0.5f * _zoom;
			//float halfWidth = width * 0.5f;
			//float halfHeight = height * 0.5f;

			//CW
			// -------->
			// | 0(--) 1
			// | 		
			// | 3   2 (++)
			//Vector2 pos_0 = new Vector2(pos.x - halfWidth, pos.y - halfHeight);
			//Vector2 pos_1 = new Vector2(pos.x + halfWidth, pos.y - halfHeight);
			//Vector2 pos_2 = new Vector2(pos.x + halfWidth, pos.y + halfHeight);
			//Vector2 pos_3 = new Vector2(pos.x - halfWidth, pos.y + halfHeight);

			//ConvertPixelPerfectPos2(ref pos_0);
			//ConvertPixelPerfectPos2(ref pos_1);
			//ConvertPixelPerfectPos2(ref pos_2);
			//ConvertPixelPerfectPos2(ref pos_3);

			Vector2 pos_0 = new Vector2(posX_Left, posY_Bottom);
			Vector2 pos_1 = new Vector2(posX_Left + width, posY_Bottom);
			Vector2 pos_2 = new Vector2(posX_Left + width, posY_Bottom + height);
			Vector2 pos_3 = new Vector2(posX_Left, posY_Bottom + height);

			if (isWireframe)
			{
				if (isNeedResetMat)
				{
					//변경 21.5.18
					_matBatch.BeginPass_Color(GL.LINES);

					//_matBatch.SetClippingSize(_glScreenClippingSize);
					//GL.Begin(GL.LINES);
				}

				GL.Color(color);
				GL.Vertex(pos_0);	GL.Vertex(pos_1);
				GL.Vertex(pos_1);	GL.Vertex(pos_2);
				GL.Vertex(pos_2);	GL.Vertex(pos_3);
				GL.Vertex(pos_3);	GL.Vertex(pos_0);

				//삭제 21.5.18
				if (isNeedResetMat)
				{
					//GL.End();//<전환 완료>
					//GL.Flush();
					_matBatch.EndPass();
				}
			}
			else
			{
				//CW
				// -------->
				// | 0   1
				// | 		
				// | 3   2
				if (isNeedResetMat)
				{
					//변경 21.5.18
					_matBatch.BeginPass_Color(GL.TRIANGLES);

					//_matBatch.SetClippingSize(_glScreenClippingSize);
					//GL.Begin(GL.TRIANGLES);
				}
				GL.Color(color);
				// 0 - 1 - 2
				GL.Vertex(pos_0);
				GL.Vertex(pos_1);
				GL.Vertex(pos_2);

				// 2 - 3 - 0
				GL.Vertex(pos_2);
				GL.Vertex(pos_3);
				GL.Vertex(pos_0);

				//삭제 21.5.18
				if (isNeedResetMat)
				{
					//GL.End();//<전환 완료>
					//GL.Flush();
					_matBatch.EndPass();
				}
			}

			//GL.Flush();
		}

		//-------------------------------------------------------------------------------
		// 원형 (Circle)
		//-------------------------------------------------------------------------------
		public static void DrawCircle(Vector2 pos, float radius, Color color, bool isNeedResetMat)
		{
			if (_matBatch.IsNotReady())
			{
				return;
			}

			pos = World2GL(pos);

			//CW
			// -------->
			// | 0(--) 1
			// | 		
			// | 3   2 (++)

			if (isNeedResetMat)
			{
				//변경 21.5.18
				_matBatch.BeginPass_Color(GL.LINES);

				//_matBatch.SetClippingSize(_glScreenClippingSize);
				//GL.Begin(GL.LINES);
			}

			float radiusGL = radius * _zoom;
			GL.Color(color);
			for (int i = 0; i < 36; i++)
			{
				float angleRad_0 = (i / 36.0f) * Mathf.PI * 2.0f;
				float angleRad_1 = ((i + 1) / 36.0f) * Mathf.PI * 2.0f;

				Vector2 pos0 = pos + new Vector2(Mathf.Cos(angleRad_0) * radiusGL, Mathf.Sin(angleRad_0) * radiusGL);
				Vector2 pos1 = pos + new Vector2(Mathf.Cos(angleRad_1) * radiusGL, Mathf.Sin(angleRad_1) * radiusGL);

				GL.Vertex(pos0);
				GL.Vertex(pos1);
			}


			//삭제 21.5.18
			if (isNeedResetMat)
			{
				//GL.End();//<전환 완료>
				//GL.Flush();
				_matBatch.EndPass();
			}


			//GL.Flush();
		}



		public static void DrawBoldCircleGL(Vector2 posGL, float radius, float lineWidth, Color color, bool isNeedResetMat)
		{
			if (_matBatch.IsNotReady())
			{
				return;
			}

			//CW
			// -------->
			// | 0(--) 1
			// | 		
			// | 3   2 (++)

			if (isNeedResetMat)
			{
				//변경 21.5.18
				_matBatch.BeginPass_Color(GL.TRIANGLES);

				//_matBatch.SetClippingSize(_glScreenClippingSize);
				//GL.Begin(GL.TRIANGLES);
			}

			float radiusGL = radius * _zoom;
			//float radiusGL = radius;
			GL.Color(color);
			for (int i = 0; i < 36; i++)
			{
				float angleRad_0 = (i / 36.0f) * Mathf.PI * 2.0f;
				float angleRad_1 = ((i + 1) / 36.0f) * Mathf.PI * 2.0f;

				Vector2 pos0 = posGL + new Vector2(Mathf.Cos(angleRad_0) * radiusGL, Mathf.Sin(angleRad_0) * radiusGL);
				Vector2 pos1 = posGL + new Vector2(Mathf.Cos(angleRad_1) * radiusGL, Mathf.Sin(angleRad_1) * radiusGL);

				//GL.Vertex(pos0);
				//GL.Vertex(pos1);

				DrawBoldLineGL(pos0, pos1, lineWidth, color, false);
			}

			//삭제 21.5.18
			if (isNeedResetMat)
			{
				//GL.End();//<전환 완료>
				//GL.Flush();
				_matBatch.EndPass();
			}


			//GL.Flush();
		}


		//-------------------------------------------------------------------------------
		// 두께가 있는 선 (Bold Line)
		//-------------------------------------------------------------------------------
		public static void DrawBoldLine(Vector2 pos1, Vector2 pos2, float width, Color color, bool isNeedResetMat)
		{
			if (_matBatch.IsNotReady())
			{
				return;
			}

			pos1 = World2GL(pos1);
			pos2 = World2GL(pos2);

			if (pos1 == pos2)
			{
				return;
			}

			//float halfWidth = width * 0.5f / _zoom;
			float halfWidth = width * 0.5f;

			//CW
			// -------->
			// | 0(--) 1
			// | 		
			// | 3   2 (++)

			// -------->
			// |    1
			// | 0     2
			// | 
			// | 
			// | 
			// | 5     3
			// |    4

			Vector2 dir = (pos1 - pos2).normalized;
			Vector2 dirRev = new Vector2(-dir.y, dir.x);

			Vector2 pos_0 = pos1 - dirRev * halfWidth;
			Vector2 pos_1 = pos1 + dir * halfWidth;
			//Vector2 pos_1 = pos1;
			Vector2 pos_2 = pos1 + dirRev * halfWidth;

			Vector2 pos_3 = pos2 + dirRev * halfWidth;
			Vector2 pos_4 = pos2 - dir * halfWidth;
			//Vector2 pos_4 = pos2;
			Vector2 pos_5 = pos2 - dirRev * halfWidth;

			if (isNeedResetMat)
			{
				//변경 21.5.18
				_matBatch.BeginPass_Color(GL.TRIANGLES);

				//_matBatch.SetClippingSize(_glScreenClippingSize);
				//GL.Begin(GL.TRIANGLES);
			}
			GL.Color(color);
			// 0 - 1 - 2
			GL.Vertex(pos_0);	GL.Vertex(pos_1);	GL.Vertex(pos_2);
			GL.Vertex(pos_2);	GL.Vertex(pos_1);	GL.Vertex(pos_0);

			// 0 - 2 - 3
			GL.Vertex(pos_0);	GL.Vertex(pos_2);	GL.Vertex(pos_3);
			GL.Vertex(pos_3);	GL.Vertex(pos_2);	GL.Vertex(pos_0);

			// 3 - 5 - 0
			GL.Vertex(pos_3);	GL.Vertex(pos_5);	GL.Vertex(pos_0);
			GL.Vertex(pos_0);	GL.Vertex(pos_5);	GL.Vertex(pos_3);

			// 3 - 4 - 5
			GL.Vertex(pos_3);	GL.Vertex(pos_4);	GL.Vertex(pos_5);
			GL.Vertex(pos_5);	GL.Vertex(pos_4);	GL.Vertex(pos_3);

			//삭제 21.5.18
			if (isNeedResetMat)
			{
				//GL.End();//<전환 완료>
				//GL.Flush();
				_matBatch.EndPass();
			}
		}


		public static void DrawBoldLineGL(Vector2 pos1, Vector2 pos2, float width, Color color, bool isNeedResetMat)
		{
			if (_matBatch.IsNotReady())
			{
				return;
			}

			if (pos1 == pos2)
			{ return; }

			//float halfWidth = width * 0.5f / _zoom;
			float halfWidth = width * 0.5f;

			//CW
			// -------->
			// | 0(--) 1
			// | 		
			// | 3   2 (++)

			// -------->
			// |    1
			// | 0     2
			// | 
			// | 
			// | 
			// | 5     3
			// |    4

			Vector2 dir = (pos1 - pos2).normalized;
			Vector2 dirRev = new Vector2(-dir.y, dir.x);

			Vector2 pos_0 = pos1 - dirRev * halfWidth;
			Vector2 pos_1 = pos1 + dir * halfWidth;
			//Vector2 pos_1 = pos1;
			Vector2 pos_2 = pos1 + dirRev * halfWidth;

			Vector2 pos_3 = pos2 + dirRev * halfWidth;
			Vector2 pos_4 = pos2 - dir * halfWidth;
			//Vector2 pos_4 = pos2;
			Vector2 pos_5 = pos2 - dirRev * halfWidth;

			if (isNeedResetMat)
			{
				//변경 21.5.18
				_matBatch.BeginPass_Color(GL.TRIANGLES);

				//_matBatch.SetClippingSize(_glScreenClippingSize);
				//GL.Begin(GL.TRIANGLES);
			}
			GL.Color(color);
			// 0 - 1 - 2
			GL.Vertex(pos_0);	GL.Vertex(pos_1);	GL.Vertex(pos_2);
			GL.Vertex(pos_2);	GL.Vertex(pos_1);	GL.Vertex(pos_0);

			// 0 - 2 - 3
			GL.Vertex(pos_0);	GL.Vertex(pos_2);	GL.Vertex(pos_3);
			GL.Vertex(pos_3);	GL.Vertex(pos_2);	GL.Vertex(pos_0);

			// 3 - 5 - 0
			GL.Vertex(pos_3);	GL.Vertex(pos_5);	GL.Vertex(pos_0);
			GL.Vertex(pos_0);	GL.Vertex(pos_5);	GL.Vertex(pos_3);

			// 3 - 4 - 5
			GL.Vertex(pos_3);	GL.Vertex(pos_4);	GL.Vertex(pos_5);
			GL.Vertex(pos_5);	GL.Vertex(pos_4);	GL.Vertex(pos_3);

			//삭제 21.5.18
			if (isNeedResetMat)
			{
				//GL.End();//<전환 완료>
				//GL.Flush();
				EndPass();
			}
		}



		//-------------------------------------------------------------------------------
		// 텍스트
		//-------------------------------------------------------------------------------
		public static void DrawText(string text, Vector2 pos, float width, Color color)
		{
			//if(_mat_Color == null || _mat_Texture == null)
			//{
			//	return;
			//}
			if (_matBatch.IsNotReady())
			{
				return;
			}

			pos = World2GL(pos);

			if (IsVertexClipped(pos))
			{
				return;
			}

			if (IsVertexClipped(pos + new Vector2(width * _zoom, 15)))
			{
				return;
			}
			_textStyle.normal.textColor = color;


			GUI.Label(new Rect(pos.x, pos.y, 100.0f, 30.0f), text, _textStyle);
		}


		public static void DrawTextGL(string text, Vector2 pos, float width, Color color)
		{
			if (_matBatch.IsNotReady())
			{
				return;
			}

			if (IsVertexClipped(pos))
			{
				return;
			}

			if (IsVertexClipped(pos + new Vector2(width, 15)))
			{
				return;
			}
			_textStyle.normal.textColor = color;


			GUI.Label(new Rect(pos.x, pos.y, width + 50, 30.0f), text, _textStyle);
		}

		public static void DrawTextGL_IgnoreRightClipping(string text, Vector2 pos, float width, Color color)
		{
			if (_matBatch.IsNotReady())
			{
				return;
			}

			if (IsVertexClipped(pos))
			{
				return;
			}

			//if (IsVertexClipped(pos + new Vector2(width, 15)))
			//{
			//	return;
			//}
			_textStyle.normal.textColor = color;

			GUI.Label(new Rect(pos.x, pos.y, width + 50, 30.0f), text, _textStyle);
		}


		//-------------------------------------------------------------------------------
		// 텍스쳐 단순 렌더링
		//-------------------------------------------------------------------------------
		public static void DrawTexture(Texture2D image, Vector2 pos, float width, float height, Color color2X)
		{
			DrawTexture(image, pos, width, height, color2X, 0.0f);
		}

		public static void DrawTexture(Texture2D image, Vector2 pos, float width, float height, Color color2X, float depth)
		{
			if (_matBatch.IsNotReady())
			{
				return;
			}

			pos = World2GL(pos);


			float realWidth = width * _zoom;
			float realHeight = height * _zoom;

			float realWidth_Half = realWidth * 0.5f;
			float realHeight_Half = realHeight * 0.5f;

			//CW
			// -------->
			// | 0(--) 1
			// | 		
			// | 3   2 (++)
			Vector2 pos_0 = new Vector2(pos.x - realWidth_Half, pos.y - realHeight_Half);
			Vector2 pos_1 = new Vector2(pos.x + realWidth_Half, pos.y - realHeight_Half);
			Vector2 pos_2 = new Vector2(pos.x + realWidth_Half, pos.y + realHeight_Half);
			Vector2 pos_3 = new Vector2(pos.x - realWidth_Half, pos.y + realHeight_Half);


			float widthResize = (pos_1.x - pos_0.x);
			float heightResize = (pos_3.y - pos_0.y);

			if (widthResize < 1.0f || heightResize < 1.0f)
			{
				return;
			}


			float u_left = 0.0f;
			float u_right = 1.0f;

			float v_top = 0.0f;
			float v_bottom = 1.0f;

			Vector3 uv_0 = new Vector3(u_left, v_bottom, 0.0f);
			Vector3 uv_1 = new Vector3(u_right, v_bottom, 0.0f);
			Vector3 uv_2 = new Vector3(u_right, v_top, 0.0f);
			Vector3 uv_3 = new Vector3(u_left, v_top, 0.0f);

			//CW
			// -------->
			// | 0   1
			// | 		
			// | 3   2
			//변경 21.5.18
			_matBatch.BeginPass_Texture_Normal(GL.TRIANGLES, color2X, image, apPortrait.SHADER_TYPE.AlphaBlend);

			//_matBatch.SetClippingSize(_glScreenClippingSize);
			//GL.Begin(GL.TRIANGLES);

			GL.TexCoord(uv_0);	GL.Vertex(new Vector3(pos_0.x, pos_0.y, depth)); // 0
			GL.TexCoord(uv_1);	GL.Vertex(new Vector3(pos_1.x, pos_1.y, depth)); // 1
			GL.TexCoord(uv_2);	GL.Vertex(new Vector3(pos_2.x, pos_2.y, depth)); // 2

			GL.TexCoord(uv_2);	GL.Vertex(new Vector3(pos_2.x, pos_2.y, depth)); // 2
			GL.TexCoord(uv_3);	GL.Vertex(new Vector3(pos_3.x, pos_3.y, depth)); // 3
			GL.TexCoord(uv_0);	GL.Vertex(new Vector3(pos_0.x, pos_0.y, depth)); // 0

			//삭제 21.5.18
			//GL.End();//<전환완료>
			//GL.Flush();
			EndPass();
		}



		private static Vector2 s_tmp_TexturePos_Local_0 = Vector2.zero;
		private static Vector2 s_tmp_TexturePos_Local_1 = Vector2.zero;
		private static Vector2 s_tmp_TexturePos_Local_2 = Vector2.zero;
		private static Vector2 s_tmp_TexturePos_Local_3 = Vector2.zero;
		private static Vector2 s_tmp_TexturePos_World_0 = Vector2.zero;
		private static Vector2 s_tmp_TexturePos_World_1 = Vector2.zero;
		private static Vector2 s_tmp_TexturePos_World_2 = Vector2.zero;
		private static Vector2 s_tmp_TexturePos_World_3 = Vector2.zero;
		private static Vector2 s_tmp_TexturePos_GL_0 = Vector2.zero;
		private static Vector2 s_tmp_TexturePos_GL_1 = Vector2.zero;
		private static Vector2 s_tmp_TexturePos_GL_2 = Vector2.zero;
		private static Vector2 s_tmp_TexturePos_GL_3 = Vector2.zero;


		public static void DrawTexture(Texture2D image, apMatrix3x3 matrix, float width, float height, Color color2X, float depth)
		{
			if (_matBatch.IsNotReady())
			{
				return;
			}

			float width_Half = width * 0.5f;
			float height_Half = height * 0.5f;

			//Zero 대신 mesh Pivot 위치로 삼자
			
			//이전
			//Vector2 pos_0 = World2GL(matrix.MultiplyPoint(new Vector2(-width_Half, +height_Half)));
			//Vector2 pos_1 = World2GL(matrix.MultiplyPoint(new Vector2(+width_Half, +height_Half)));
			//Vector2 pos_2 = World2GL(matrix.MultiplyPoint(new Vector2(+width_Half, -height_Half)));
			//Vector2 pos_3 = World2GL(matrix.MultiplyPoint(new Vector2(-width_Half, -height_Half)));

			//v1.4.4 : Ref를 이용한다.
			s_tmp_TexturePos_Local_0 = new Vector2(-width_Half, +height_Half);
			s_tmp_TexturePos_Local_1 = new Vector2(+width_Half, +height_Half);
			s_tmp_TexturePos_Local_2 = new Vector2(+width_Half, -height_Half);
			s_tmp_TexturePos_Local_3 = new Vector2(-width_Half, -height_Half);

			apMatrix3x3.MultiplyPoint(ref s_tmp_TexturePos_World_0, ref matrix, ref s_tmp_TexturePos_Local_0);
			apMatrix3x3.MultiplyPoint(ref s_tmp_TexturePos_World_1, ref matrix, ref s_tmp_TexturePos_Local_1);
			apMatrix3x3.MultiplyPoint(ref s_tmp_TexturePos_World_2, ref matrix, ref s_tmp_TexturePos_Local_2);
			apMatrix3x3.MultiplyPoint(ref s_tmp_TexturePos_World_3, ref matrix, ref s_tmp_TexturePos_Local_3);
			World2GL(ref s_tmp_TexturePos_GL_0, ref s_tmp_TexturePos_World_0);
			World2GL(ref s_tmp_TexturePos_GL_1, ref s_tmp_TexturePos_World_1);
			World2GL(ref s_tmp_TexturePos_GL_2, ref s_tmp_TexturePos_World_2);
			World2GL(ref s_tmp_TexturePos_GL_3, ref s_tmp_TexturePos_World_3);

			//CW
			// -------->
			// | 0(--) 1
			// | 		
			// | 3   2 (++)
			float u_left = 0.0f;
			float u_right = 1.0f;

			float v_top = 0.0f;
			float v_bottom = 1.0f;

			Vector3 uv_0 = new Vector3(u_left, v_bottom, 0.0f);
			Vector3 uv_1 = new Vector3(u_right, v_bottom, 0.0f);
			Vector3 uv_2 = new Vector3(u_right, v_top, 0.0f);
			Vector3 uv_3 = new Vector3(u_left, v_top, 0.0f);

			//CW
			// -------->
			// | 0   1
			// | 		
			// | 3   2
			//변경 21.5.18
			_matBatch.BeginPass_Texture_Normal(GL.TRIANGLES, color2X, image, apPortrait.SHADER_TYPE.AlphaBlend);

			//_matBatch.SetClippingSize(_glScreenClippingSize);
			//GL.Begin(GL.TRIANGLES);

			//GL.TexCoord(uv_0);	GL.Vertex(new Vector3(pos_0.x, pos_0.y, depth)); // 0
			//GL.TexCoord(uv_1);	GL.Vertex(new Vector3(pos_1.x, pos_1.y, depth)); // 1
			//GL.TexCoord(uv_2);	GL.Vertex(new Vector3(pos_2.x, pos_2.y, depth)); // 2

			//GL.TexCoord(uv_2);	GL.Vertex(new Vector3(pos_2.x, pos_2.y, depth)); // 2
			//GL.TexCoord(uv_3);	GL.Vertex(new Vector3(pos_3.x, pos_3.y, depth)); // 3
			//GL.TexCoord(uv_0);	GL.Vertex(new Vector3(pos_0.x, pos_0.y, depth)); // 0

			GL.TexCoord(uv_0);	GL.Vertex(s_tmp_TexturePos_GL_0); // 0
			GL.TexCoord(uv_1);	GL.Vertex(s_tmp_TexturePos_GL_1); // 1
			GL.TexCoord(uv_2);	GL.Vertex(s_tmp_TexturePos_GL_2); // 2

			GL.TexCoord(uv_2);	GL.Vertex(s_tmp_TexturePos_GL_2); // 2
			GL.TexCoord(uv_3);	GL.Vertex(s_tmp_TexturePos_GL_3); // 3
			GL.TexCoord(uv_0);	GL.Vertex(s_tmp_TexturePos_GL_0); // 0


			//삭제 21.5.18
			//GL.End();//<전환 완료>
			//GL.Flush();
			EndPass();
		}

		public static void DrawTextureGL(Texture2D image, Vector2 pos, float width, float height, Color color2X, float depth)
		{
			if (_matBatch.IsNotReady())
			{
				return;
			}

			float realWidth = width * _zoom;
			float realHeight = height * _zoom;

			float realWidth_Half = realWidth * 0.5f;
			float realHeight_Half = realHeight * 0.5f;

			//CW
			// -------->
			// | 0(--) 1
			// | 		
			// | 3   2 (++)
			Vector2 pos_0 = new Vector2(pos.x - realWidth_Half, pos.y - realHeight_Half);
			Vector2 pos_1 = new Vector2(pos.x + realWidth_Half, pos.y - realHeight_Half);
			Vector2 pos_2 = new Vector2(pos.x + realWidth_Half, pos.y + realHeight_Half);
			Vector2 pos_3 = new Vector2(pos.x - realWidth_Half, pos.y + realHeight_Half);


			float widthResize = (pos_1.x - pos_0.x);
			float heightResize = (pos_3.y - pos_0.y);

			if (widthResize < 1.0f || heightResize < 1.0f)
			{
				return;
			}


			float u_left = 0.0f;
			float u_right = 1.0f;

			float v_top = 0.0f;
			float v_bottom = 1.0f;

			Vector3 uv_0 = new Vector3(u_left, v_bottom, 0.0f);
			Vector3 uv_1 = new Vector3(u_right, v_bottom, 0.0f);
			Vector3 uv_2 = new Vector3(u_right, v_top, 0.0f);
			Vector3 uv_3 = new Vector3(u_left, v_top, 0.0f);

			//CW
			// -------->
			// | 0   1
			// | 		
			// | 3   2
			//변경 21.5.18
			_matBatch.BeginPass_Texture_Normal(GL.TRIANGLES, color2X, image, apPortrait.SHADER_TYPE.AlphaBlend);
			
			//_matBatch.SetClippingSize(_glScreenClippingSize);
			//GL.Begin(GL.TRIANGLES);


			GL.TexCoord(uv_0);	GL.Vertex(new Vector3(pos_0.x, pos_0.y, depth)); // 0
			GL.TexCoord(uv_1);	GL.Vertex(new Vector3(pos_1.x, pos_1.y, depth)); // 1
			GL.TexCoord(uv_2);	GL.Vertex(new Vector3(pos_2.x, pos_2.y, depth)); // 2

			GL.TexCoord(uv_2);	GL.Vertex(new Vector3(pos_2.x, pos_2.y, depth)); // 2
			GL.TexCoord(uv_3);	GL.Vertex(new Vector3(pos_3.x, pos_3.y, depth)); // 3
			GL.TexCoord(uv_0);	GL.Vertex(new Vector3(pos_0.x, pos_0.y, depth)); // 0


			//삭제 21.5.18
			//GL.End();//<전환 완료>
			//GL.Flush();
			EndPass();
		}




		public static void DrawTextureGL(Texture2D image, Vector2 pos, float width, float height, Color color2X, float depth, bool isReverseX, bool isReverseY)
		{
			if (_matBatch.IsNotReady())
			{
				return;
			}

			float realWidth = width * _zoom;
			float realHeight = height * _zoom;

			float realWidth_Half = realWidth * 0.5f;
			float realHeight_Half = realHeight * 0.5f;

			//CW
			// -------->
			// | 0(--) 1
			// | 		
			// | 3   2 (++)
			Vector2 pos_0 = new Vector2(pos.x - realWidth_Half, pos.y - realHeight_Half);
			Vector2 pos_1 = new Vector2(pos.x + realWidth_Half, pos.y - realHeight_Half);
			Vector2 pos_2 = new Vector2(pos.x + realWidth_Half, pos.y + realHeight_Half);
			Vector2 pos_3 = new Vector2(pos.x - realWidth_Half, pos.y + realHeight_Half);


			float widthResize = (pos_1.x - pos_0.x);
			float heightResize = (pos_3.y - pos_0.y);

			if (widthResize < 1.0f || heightResize < 1.0f)
			{
				return;
			}


			float u_left = 0.0f;
			float u_right = 1.0f;

			float v_top = 0.0f;
			float v_bottom = 1.0f;

			if(isReverseX)
			{
				//X축 UV를 뒤집기
				u_left = 1.0f;
				u_right = 0.0f;
			}

			if(isReverseY)
			{
				//Y축 UV를 뒤집기
				v_top = 1.0f;
				v_bottom = 0.0f;
			}

			Vector3 uv_0 = new Vector3(u_left, v_bottom, 0.0f);
			Vector3 uv_1 = new Vector3(u_right, v_bottom, 0.0f);
			Vector3 uv_2 = new Vector3(u_right, v_top, 0.0f);
			Vector3 uv_3 = new Vector3(u_left, v_top, 0.0f);

			//CW
			// -------->
			// | 0   1
			// | 		
			// | 3   2
			//변경 21.5.18
			_matBatch.BeginPass_Texture_Normal(GL.TRIANGLES, color2X, image, apPortrait.SHADER_TYPE.AlphaBlend);
			
			//_matBatch.SetClippingSize(_glScreenClippingSize);
			//GL.Begin(GL.TRIANGLES);


			GL.TexCoord(uv_0);	GL.Vertex(new Vector3(pos_0.x, pos_0.y, depth)); // 0
			GL.TexCoord(uv_1);	GL.Vertex(new Vector3(pos_1.x, pos_1.y, depth)); // 1
			GL.TexCoord(uv_2);	GL.Vertex(new Vector3(pos_2.x, pos_2.y, depth)); // 2

			GL.TexCoord(uv_2);	GL.Vertex(new Vector3(pos_2.x, pos_2.y, depth)); // 2
			GL.TexCoord(uv_3);	GL.Vertex(new Vector3(pos_3.x, pos_3.y, depth)); // 3
			GL.TexCoord(uv_0);	GL.Vertex(new Vector3(pos_0.x, pos_0.y, depth)); // 0


			//삭제 21.5.18
			//GL.End();//<전환 완료>
			//GL.Flush();
			EndPass();
		}



		public static void DrawTextureGL(Texture2D image, Vector2 pos, float width, float height, Color color2X, float depth, bool isNeedResetMat)
		{
			if (_matBatch.IsNotReady())
			{
				return;
			}

			float realWidth = width * _zoom;
			float realHeight = height * _zoom;

			float realWidth_Half = realWidth * 0.5f;
			float realHeight_Half = realHeight * 0.5f;

			//CW
			// -------->
			// | 0(--) 1
			// | 		
			// | 3   2 (++)
			Vector2 pos_0 = new Vector2(pos.x - realWidth_Half, pos.y - realHeight_Half);
			Vector2 pos_1 = new Vector2(pos.x + realWidth_Half, pos.y - realHeight_Half);
			Vector2 pos_2 = new Vector2(pos.x + realWidth_Half, pos.y + realHeight_Half);
			Vector2 pos_3 = new Vector2(pos.x - realWidth_Half, pos.y + realHeight_Half);


			float widthResize = (pos_1.x - pos_0.x);
			float heightResize = (pos_3.y - pos_0.y);

			if (widthResize < 1.0f || heightResize < 1.0f)
			{
				return;
			}


			float u_left = 0.0f;
			float u_right = 1.0f;

			float v_top = 0.0f;
			float v_bottom = 1.0f;

			Vector3 uv_0 = new Vector3(u_left, v_bottom, 0.0f);
			Vector3 uv_1 = new Vector3(u_right, v_bottom, 0.0f);
			Vector3 uv_2 = new Vector3(u_right, v_top, 0.0f);
			Vector3 uv_3 = new Vector3(u_left, v_top, 0.0f);

			//CW
			// -------->
			// | 0   1
			// | 		
			// | 3   2
			if (isNeedResetMat)
			{
				//변경 21.5.18
				_matBatch.BeginPass_Texture_Normal(GL.TRIANGLES, color2X, image, apPortrait.SHADER_TYPE.AlphaBlend);
				//_matBatch.SetClippingSize(_glScreenClippingSize);
				//GL.Begin(GL.TRIANGLES);
			}

			GL.TexCoord(uv_0);	GL.Vertex(new Vector3(pos_0.x, pos_0.y, depth)); // 0
			GL.TexCoord(uv_1);	GL.Vertex(new Vector3(pos_1.x, pos_1.y, depth)); // 1
			GL.TexCoord(uv_2);	GL.Vertex(new Vector3(pos_2.x, pos_2.y, depth)); // 2

			GL.TexCoord(uv_2);	GL.Vertex(new Vector3(pos_2.x, pos_2.y, depth)); // 2
			GL.TexCoord(uv_3);	GL.Vertex(new Vector3(pos_3.x, pos_3.y, depth)); // 3
			GL.TexCoord(uv_0);	GL.Vertex(new Vector3(pos_0.x, pos_0.y, depth)); // 0

			//삭제 21.5.18
			if (isNeedResetMat)
			{
				//GL.End();//<전환 완료>
				//GL.Flush();
				EndPass();
			}


			//GL.Flush();
		}


		/// <summary>
		/// 이미 VColor Texture Pass가 시작될 때 사용하는 텍스쳐
		/// </summary>
		/// <param name="image"></param>
		/// <param name="pos"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="VColor1x"></param>
		/// <param name="depth"></param>
		public static void DrawTextureGLWithVColor(Texture2D image, Vector2 pos, float width, float height, Color VColor1x, float depth)
		{
			if (_matBatch.IsNotReady())
			{
				return;
			}

			float realWidth = width * _zoom;
			float realHeight = height * _zoom;

			float realWidth_Half = realWidth * 0.5f;
			float realHeight_Half = realHeight * 0.5f;

			//CW
			// -------->
			// | 0(--) 1
			// | 		
			// | 3   2 (++)
			Vector2 pos_0 = new Vector2(pos.x - realWidth_Half, pos.y - realHeight_Half);
			Vector2 pos_1 = new Vector2(pos.x + realWidth_Half, pos.y - realHeight_Half);
			Vector2 pos_2 = new Vector2(pos.x + realWidth_Half, pos.y + realHeight_Half);
			Vector2 pos_3 = new Vector2(pos.x - realWidth_Half, pos.y + realHeight_Half);


			float widthResize = (pos_1.x - pos_0.x);
			float heightResize = (pos_3.y - pos_0.y);

			if (widthResize < 1.0f || heightResize < 1.0f)
			{
				return;
			}

			float u_left = 0.0f;
			float u_right = 1.0f;

			float v_top = 0.0f;
			float v_bottom = 1.0f;

			Vector3 uv_0 = new Vector3(u_left, v_bottom, 0.0f);
			Vector3 uv_1 = new Vector3(u_right, v_bottom, 0.0f);
			Vector3 uv_2 = new Vector3(u_right, v_top, 0.0f);
			Vector3 uv_3 = new Vector3(u_left, v_top, 0.0f);

			//CW
			// -------->
			// | 0   1
			// | 		
			// | 3   2
			GL.Color(VColor1x);
			GL.TexCoord(uv_0);	GL.Vertex(new Vector3(pos_0.x, pos_0.y, depth)); // 0
			GL.TexCoord(uv_1);	GL.Vertex(new Vector3(pos_1.x, pos_1.y, depth)); // 1
			GL.TexCoord(uv_2);	GL.Vertex(new Vector3(pos_2.x, pos_2.y, depth)); // 2

			GL.TexCoord(uv_2);	GL.Vertex(new Vector3(pos_2.x, pos_2.y, depth)); // 2
			GL.TexCoord(uv_3);	GL.Vertex(new Vector3(pos_3.x, pos_3.y, depth)); // 3
			GL.TexCoord(uv_0);	GL.Vertex(new Vector3(pos_0.x, pos_0.y, depth)); // 0
		}

		/// <summary>
		/// 이미 VColor Texture Pass가 시작될 때 사용하는 Box 그리기 함수
		/// </summary>
		/// <param name="pos"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="vColor"></param>
		private static void DrawBoxWithVColorAndUV(Vector2 pos, float width, float height, Color vColor)
		{
			float width_Half = width * 0.5f;
			float height_Half = height * 0.5f;

			Vector3 uv_0 = new Vector3(0, 1, 0.0f);
			Vector3 uv_1 = new Vector3(1, 1, 0.0f);
			Vector3 uv_2 = new Vector3(1, 0, 0.0f);
			Vector3 uv_3 = new Vector3(0, 0, 0.0f);

			GL.Color(vColor);

			GL.TexCoord(uv_0);	GL.Vertex(new Vector3(pos.x - width_Half, pos.y - height_Half, 0)); // 0
			GL.TexCoord(uv_1);	GL.Vertex(new Vector3(pos.x + width_Half, pos.y - height_Half, 0)); // 1
			GL.TexCoord(uv_2);	GL.Vertex(new Vector3(pos.x + width_Half, pos.y + height_Half, 0)); // 2

			GL.TexCoord(uv_2);	GL.Vertex(new Vector3(pos.x + width_Half, pos.y + height_Half, 0)); // 2
			GL.TexCoord(uv_3);	GL.Vertex(new Vector3(pos.x - width_Half, pos.y + height_Half, 0)); // 3
			GL.TexCoord(uv_0);	GL.Vertex(new Vector3(pos.x - width_Half, pos.y - height_Half, 0)); // 0
		}

		private static void DrawBoxWithVColorAndUV(Vector2 pos, float width, float height, Color vColor, Vector2 uvOffset)
		{
			float width_Half = width * 0.5f;
			float height_Half = height * 0.5f;

			Vector3 uv_0 = new Vector3(0 + uvOffset.x, 1 + uvOffset.y, 0.0f);
			Vector3 uv_1 = new Vector3(1 + uvOffset.x, 1 + uvOffset.y, 0.0f);
			Vector3 uv_2 = new Vector3(1 + uvOffset.x, 0 + uvOffset.y, 0.0f);
			Vector3 uv_3 = new Vector3(0 + uvOffset.x, 0 + uvOffset.y, 0.0f);

			GL.Color(vColor);

			GL.TexCoord(uv_0);	GL.Vertex(new Vector3(pos.x - width_Half, pos.y - height_Half, 0)); // 0
			GL.TexCoord(uv_1);	GL.Vertex(new Vector3(pos.x + width_Half, pos.y - height_Half, 0)); // 1
			GL.TexCoord(uv_2);	GL.Vertex(new Vector3(pos.x + width_Half, pos.y + height_Half, 0)); // 2

			GL.TexCoord(uv_2);	GL.Vertex(new Vector3(pos.x + width_Half, pos.y + height_Half, 0)); // 2
			GL.TexCoord(uv_3);	GL.Vertex(new Vector3(pos.x - width_Half, pos.y + height_Half, 0)); // 3
			GL.TexCoord(uv_0);	GL.Vertex(new Vector3(pos.x - width_Half, pos.y - height_Half, 0)); // 0
		}

		//--------------------------------------------------------------------------
		// 버텍스 / 핀
		//--------------------------------------------------------------------------
		private static Vector2 s_VertUV_LB = new Vector2(0.0f, 0.0f);
		private static Vector2 s_VertUV_RB = new Vector2(0.5f, 0.0f);
		private static Vector2 s_VertUV_LT = new Vector2(0.0f, 0.5f);
		private static Vector2 s_VertUV_RT = new Vector2(0.5f, 0.5f);

		private static Vector2 s_VertWhiteOutlineUV_LB = new Vector2(0.0f, 0.5f);
		private static Vector2 s_VertWhiteOutlineUV_RB = new Vector2(0.5f, 0.5f);
		private static Vector2 s_VertWhiteOutlineUV_LT = new Vector2(0.0f, 1.0f);
		private static Vector2 s_VertWhiteOutlineUV_RT = new Vector2(0.5f, 1.0f);

		private static Vector2 s_PinUV_LB = new Vector2(0.5f, 0.0f);
		private static Vector2 s_PinUV_RB = new Vector2(1.0f, 0.0f);
		private static Vector2 s_PinUV_LT = new Vector2(0.5f, 0.5f);
		private static Vector2 s_PinUV_RT = new Vector2(1.0f, 0.5f);

		public static void DrawVertex(ref Vector2 posGL, float halfSize, ref Color vColor)
		{
			//UV는 (0~0.5, 0.0~0.5)
			GL.Color(vColor);

			//2  -  3
			//0  -  1

			GL.TexCoord(s_VertUV_LB);	GL.Vertex(new Vector3(posGL.x - halfSize, posGL.y - halfSize, 0));
			GL.TexCoord(s_VertUV_RB);	GL.Vertex(new Vector3(posGL.x + halfSize, posGL.y - halfSize, 0));
			GL.TexCoord(s_VertUV_LT);	GL.Vertex(new Vector3(posGL.x - halfSize, posGL.y + halfSize, 0));

			GL.TexCoord(s_VertUV_RB);	GL.Vertex(new Vector3(posGL.x + halfSize, posGL.y - halfSize, 0));
			GL.TexCoord(s_VertUV_RT);	GL.Vertex(new Vector3(posGL.x + halfSize, posGL.y + halfSize, 0));
			GL.TexCoord(s_VertUV_LT);	GL.Vertex(new Vector3(posGL.x - halfSize, posGL.y + halfSize, 0));
		}

		public static void DrawVertex_WhiteOutline(ref Vector2 posGL, float halfSize, ref Color vColor)
		{
			//UV는 (0~0.5, 0.0~0.5)
			GL.Color(vColor);

			//2  -  3
			//0  -  1

			GL.TexCoord(s_VertWhiteOutlineUV_LB);	GL.Vertex(new Vector3(posGL.x - halfSize, posGL.y - halfSize, 0));
			GL.TexCoord(s_VertWhiteOutlineUV_RB);	GL.Vertex(new Vector3(posGL.x + halfSize, posGL.y - halfSize, 0));
			GL.TexCoord(s_VertWhiteOutlineUV_LT);	GL.Vertex(new Vector3(posGL.x - halfSize, posGL.y + halfSize, 0));

			GL.TexCoord(s_VertWhiteOutlineUV_RB);	GL.Vertex(new Vector3(posGL.x + halfSize, posGL.y - halfSize, 0));
			GL.TexCoord(s_VertWhiteOutlineUV_RT);	GL.Vertex(new Vector3(posGL.x + halfSize, posGL.y + halfSize, 0));
			GL.TexCoord(s_VertWhiteOutlineUV_LT);	GL.Vertex(new Vector3(posGL.x - halfSize, posGL.y + halfSize, 0));
		}

		public static void DrawPin(ref Vector2 posGL, float halfSize, ref Color vColor)
		{
			//UV는 (0.5~1.0, 0.0~0.5)
			GL.Color(vColor);

			//2  -  3
			//0  -  1

			GL.TexCoord(s_PinUV_LB);	GL.Vertex(new Vector3(posGL.x - halfSize, posGL.y - halfSize, 0));
			GL.TexCoord(s_PinUV_RB);	GL.Vertex(new Vector3(posGL.x + halfSize, posGL.y - halfSize, 0));
			GL.TexCoord(s_PinUV_LT);	GL.Vertex(new Vector3(posGL.x - halfSize, posGL.y + halfSize, 0));

			GL.TexCoord(s_PinUV_RB);	GL.Vertex(new Vector3(posGL.x + halfSize, posGL.y - halfSize, 0));
			GL.TexCoord(s_PinUV_RT);	GL.Vertex(new Vector3(posGL.x + halfSize, posGL.y + halfSize, 0));
			GL.TexCoord(s_PinUV_LT);	GL.Vertex(new Vector3(posGL.x - halfSize, posGL.y + halfSize, 0));
		}
    }
}
