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
	// apGL 중에서 "메시 메뉴"에서의 렌더링 함수들만 모았다.
	public static partial class apGL
	{
		public static void DrawMesh(	apMesh mesh, 
										apMatrix3x3 matrix, 
										Color color2X, 
										
										//RENDER_TYPE renderType, 이전
										RenderTypeRequest renderRequest,//변경 22.3.3

										apVertexController vertexController, 
										apEditor editor, 
										Vector2 mousePosition,
										bool isPSDAreaEditing)
		{
			try
			{
				//0. 메시, 텍스쳐가 없을 때
				//if (mesh == null || mesh._textureData == null || mesh._textureData._image == null)//이전 코드
				if (mesh == null || mesh.LinkedTextureData == null || mesh.LinkedTextureData._image == null)//변경 코드
				{
					DrawBox(Vector2.zero, 512, 512, Color.red, true);
					DrawText("No Image", Vector2.zero, 80, Color.cyan);
					return;
				}



				//1. 모든 메시를 보여줄때 (또는 클리핑된 메시가 없을 때) => 
				bool isShowAllTexture = false;
				Color textureColor = _textureColor_Gray;
				if (
					//(renderType & RENDER_TYPE.ShadeAllMesh) != 0	//이전
					renderRequest.ShadeAllMesh						//변경 22.3.3 (v1.4.0)
					|| mesh._indexBuffer.Count < 3)
				{
					isShowAllTexture = true;
					textureColor = _textureColor_Shade;
				}
				else if (
					//(renderType & RENDER_TYPE.AllMesh) != 0	//이전
					renderRequest.AllMesh						//변경 22.3.3
					)
				{
					isShowAllTexture = true;
				}

				matrix *= mesh.Matrix_VertToLocal;

				if (isShowAllTexture)
				{
					//DrawTexture(mesh._textureData._image, matrix, mesh._textureData._width, mesh._textureData._height, textureColor, -10);
					DrawTexture(mesh.LinkedTextureData._image, matrix, mesh.LinkedTextureData._width, mesh.LinkedTextureData._height, textureColor, -10);
				}

				apVertex selectedVertex = null;
				List<apVertex> selectedVertices = null;
				apVertex nextSelectedVertex = null;
				apBone selectedBone = null;
				apMeshPolygon selectedPolygon = null;

				//핀
				apMeshPin selectedPin = editor.Select.MeshPin;
				List<apMeshPin> selectedPins = editor.Select.MeshPins;


				//메시의 버텍스/인덱스 리스트
				List<apVertex> meshVerts = mesh._vertexData;
				int nVerts = meshVerts != null ? meshVerts.Count : 0;

				List<int> meshIndexBuffers = mesh._indexBuffer;
				int nIndexBuffers = meshIndexBuffers != null ? meshIndexBuffers.Count : 0;

				List<apMeshEdge> meshEdges = mesh._edges;
				int nEdges = meshEdges != null ? meshEdges.Count : 0;

				List<apMeshPolygon> meshPolygons = mesh._polygons;
				int nPolygons = meshPolygons != null ? meshPolygons.Count : 0;
				


				if (vertexController != null)
				{
					selectedVertex = vertexController.Vertex;
					selectedVertices = vertexController.Vertices;
					nextSelectedVertex = vertexController.LinkedNextVertex;
					selectedBone = vertexController.Bone;
					selectedPolygon = vertexController.Polygon;
				}

				//삭제 v1.4.4 : 사용하지 않음
				//Vector2 pos2_0 = Vector2.zero;
				//Vector2 pos2_1 = Vector2.zero;
				//Vector2 pos2_2 = Vector2.zero;

				Vector3 pos_0 = Vector3.zero;
				Vector3 pos_1 = Vector3.zero;
				Vector3 pos_2 = Vector3.zero;

				Vector2 uv_0 = Vector2.zero;
				Vector2 uv_1 = Vector2.zero;
				Vector2 uv_2 = Vector2.zero;

				//2. 메시를 렌더링하자
				if (nIndexBuffers >= 3)
				{
					//------------------------------------------
					// Drawcall Batch를 했을때
					// <참고> Weight를 출력하고 싶다면 Normal 대신 VColor를 넣고, VertexColor를 넣어주자
					
					//if ((renderType & RENDER_TYPE.VolumeWeightColor) != 0)	//이전
					if (renderRequest.VolumeWeightColor)						//변경 22.3.3 (v1.4.0)
					{
						_matBatch.BeginPass_Texture_VColor(GL.TRIANGLES, _textureColor_Gray, mesh.LinkedTextureData._image, 1.0f, apPortrait.SHADER_TYPE.AlphaBlend, false, Vector4.zero);
					}
					else
					{
						_matBatch.BeginPass_Texture_Normal(GL.TRIANGLES, color2X, mesh.LinkedTextureData._image, apPortrait.SHADER_TYPE.AlphaBlend);

					}
					
					//삭제
					//_matBatch.SetClippingSize(_glScreenClippingSize);
					//GL.Begin(GL.TRIANGLES);


					//------------------------------------------
					apVertex vert0, vert1, vert2;
					
					int iVert_0 = 0;
					int iVert_1 = 0;
					int iVert_2 = 0;

					
					if (renderRequest.TestPinWeight)
					{
						//[핀 테스트모드]에서의 메시 렌더링
						
						//버텍스 색상이 white로 공통
						GL.Color(Color.white);

						for (int i = 0; i < nIndexBuffers; i += 3)
						{
							if (i + 2 >= nIndexBuffers) { break; }

							iVert_0 = meshIndexBuffers[i + 0];
							iVert_1 = meshIndexBuffers[i + 1];
							iVert_2 = meshIndexBuffers[i + 2];

							if (iVert_0 >= nVerts ||
								iVert_1 >= nVerts ||
								iVert_2 >= nVerts)
							{
								break;
							}

							vert0 = meshVerts[iVert_0];
							vert1 = meshVerts[iVert_1];
							vert2 = meshVerts[iVert_2];

						
							//이전
							//pos2_0 = World2GL(matrix.MultiplyPoint(vert0._pos_PinTest));
							//pos2_1 = World2GL(matrix.MultiplyPoint(vert1._pos_PinTest));
							//pos2_2 = World2GL(matrix.MultiplyPoint(vert2._pos_PinTest));

							//pos_0 = new Vector3(pos2_0.x, pos2_0.y, vert0._zDepth * 0.1f);
							//pos_1 = new Vector3(pos2_1.x, pos2_1.y, vert1._zDepth * 0.5f);
							//pos_2 = new Vector3(pos2_2.x, pos2_2.y, vert2._zDepth * 0.5f);//<<Z값이 반영되었다.

							//변경 v1.4.4 : Ref를 이용한 빠른 변환
							Local2GL(ref pos_0, ref matrix, ref vert0._pos_PinTest, vert0._zDepth * 0.1f);
							Local2GL(ref pos_1, ref matrix, ref vert1._pos_PinTest, vert1._zDepth * 0.5f);
							Local2GL(ref pos_2, ref matrix, ref vert2._pos_PinTest, vert2._zDepth * 0.5f);

							uv_0 = vert0._uv;
							uv_1 = vert1._uv;
							uv_2 = vert2._uv;

							GL.TexCoord(uv_0); GL.Vertex(pos_0); // 0
							GL.TexCoord(uv_1); GL.Vertex(pos_1); // 1
							GL.TexCoord(uv_2); GL.Vertex(pos_2); // 2

							//Back Side
							GL.TexCoord(uv_2); GL.Vertex(pos_2); // 2
							GL.TexCoord(uv_1); GL.Vertex(pos_1); // 1
							GL.TexCoord(uv_0); GL.Vertex(pos_0); // 0
						}
					}
					else if (renderRequest.VolumeWeightColor)
					{
						//[Depth Weight 렌더링]에서의 메시 렌더링

						Color color0 = Color.white;
						Color color1 = Color.white;
						Color color2 = Color.white;

						for (int i = 0; i < nIndexBuffers; i += 3)
						{
							if (i + 2 >= nIndexBuffers) { break; }

							iVert_0 = meshIndexBuffers[i + 0];
							iVert_1 = meshIndexBuffers[i + 1];
							iVert_2 = meshIndexBuffers[i + 2];

							if (iVert_0 >= nVerts ||
								iVert_1 >= nVerts ||
								iVert_2 >= nVerts)
							{
								break;
							}

							vert0 = meshVerts[iVert_0];
							vert1 = meshVerts[iVert_1];
							vert2 = meshVerts[iVert_2];

						
							//이전
							//pos2_0 = World2GL(matrix.MultiplyPoint(vert0._pos));
							//pos2_1 = World2GL(matrix.MultiplyPoint(vert1._pos));
							//pos2_2 = World2GL(matrix.MultiplyPoint(vert2._pos));

							//pos_0 = new Vector3(pos2_0.x, pos2_0.y, vert0._zDepth * 0.1f);
							//pos_1 = new Vector3(pos2_1.x, pos2_1.y, vert1._zDepth * 0.5f);
							//pos_2 = new Vector3(pos2_2.x, pos2_2.y, vert2._zDepth * 0.5f);//<<Z값이 반영되었다.

							//변경 v1.4.4 : Ref를 이용한 빠른 변환
							Local2GL(ref pos_0, ref matrix, ref vert0._pos, vert0._zDepth * 0.1f);
							Local2GL(ref pos_1, ref matrix, ref vert1._pos, vert1._zDepth * 0.5f);
							Local2GL(ref pos_2, ref matrix, ref vert2._pos, vert2._zDepth * 0.5f);

							uv_0 = vert0._uv;
							uv_1 = vert1._uv;
							uv_2 = vert2._uv;

							//VolumeWeightColor
							color0 = GetWeightGrayscale(vert0._zDepth);
							color1 = GetWeightGrayscale(vert1._zDepth);
							color2 = GetWeightGrayscale(vert2._zDepth);

							////------------------------------------------

							GL.Color(color0); GL.TexCoord(uv_0); GL.Vertex(pos_0); // 0
							GL.Color(color1); GL.TexCoord(uv_1); GL.Vertex(pos_1); // 1
							GL.Color(color2); GL.TexCoord(uv_2); GL.Vertex(pos_2); // 2

							//Back Side
							GL.Color(color2); GL.TexCoord(uv_2); GL.Vertex(pos_2); // 2
							GL.Color(color1); GL.TexCoord(uv_1); GL.Vertex(pos_1); // 1
							GL.Color(color0); GL.TexCoord(uv_0); GL.Vertex(pos_0); // 0

							////------------------------------------------
						}
					}
					else
					{
						//[일반 모드]에서의 메시 렌더링

						//버텍스 색상이 white로 공통
						GL.Color(Color.white);

						for (int i = 0; i < nIndexBuffers; i += 3)
						{
							if (i + 2 >= nIndexBuffers) { break; }

							iVert_0 = meshIndexBuffers[i + 0];
							iVert_1 = meshIndexBuffers[i + 1];
							iVert_2 = meshIndexBuffers[i + 2];

							if (iVert_0 >= nVerts ||
								iVert_1 >= nVerts ||
								iVert_2 >= nVerts)
							{
								break;
							}

							vert0 = meshVerts[iVert_0];
							vert1 = meshVerts[iVert_1];
							vert2 = meshVerts[iVert_2];

							//이전
							//pos2_0 = World2GL(matrix.MultiplyPoint(vert0._pos));
							//pos2_1 = World2GL(matrix.MultiplyPoint(vert1._pos));
							//pos2_2 = World2GL(matrix.MultiplyPoint(vert2._pos));

							//pos_0 = new Vector3(pos2_0.x, pos2_0.y, vert0._zDepth * 0.1f);
							//pos_1 = new Vector3(pos2_1.x, pos2_1.y, vert1._zDepth * 0.5f);
							//pos_2 = new Vector3(pos2_2.x, pos2_2.y, vert2._zDepth * 0.5f);//<<Z값이 반영되었다.

							//변경 v1.4.4 : Ref 이용
							Local2GL(ref pos_0, ref matrix, ref vert0._pos, vert0._zDepth * 0.1f);
							Local2GL(ref pos_1, ref matrix, ref vert1._pos, vert1._zDepth * 0.5f);
							Local2GL(ref pos_2, ref matrix, ref vert2._pos, vert2._zDepth * 0.5f);

							uv_0 = vert0._uv;
							uv_1 = vert1._uv;
							uv_2 = vert2._uv;

							GL.TexCoord(uv_0); GL.Vertex(pos_0); // 0
							GL.TexCoord(uv_1); GL.Vertex(pos_1); // 1
							GL.TexCoord(uv_2); GL.Vertex(pos_2); // 2

							//Back Side
							GL.TexCoord(uv_2); GL.Vertex(pos_2); // 2
							GL.TexCoord(uv_1); GL.Vertex(pos_1); // 1
							GL.TexCoord(uv_0); GL.Vertex(pos_0); // 0

							////------------------------------------------
						}
					}
					

					//삭제 21.5.18
					//GL.End();//<전환 완료>
					//GL.Flush();
					EndPass();

				}

				//미러 모드
				//--------------------------------------------------------------
				if(editor._meshEditMode == apEditor.MESH_EDIT_MODE.MakeMesh &&
					editor._meshEditMirrorMode == apEditor.MESH_EDIT_MIRROR_MODE.Mirror)
				{
					DrawMeshMirror(mesh);
				}


				// Atlas 외곽선
				//--------------------------------------------------------------
				if (mesh._isPSDParsed)
				{
					Vector2 pos_LT = matrix.MultiplyPoint(new Vector2(mesh._atlasFromPSD_LT.x, mesh._atlasFromPSD_LT.y));
					Vector2 pos_RT = matrix.MultiplyPoint(new Vector2(mesh._atlasFromPSD_RB.x, mesh._atlasFromPSD_LT.y));
					Vector2 pos_LB = matrix.MultiplyPoint(new Vector2(mesh._atlasFromPSD_LT.x, mesh._atlasFromPSD_RB.y));
					Vector2 pos_RB = matrix.MultiplyPoint(new Vector2(mesh._atlasFromPSD_RB.x, mesh._atlasFromPSD_RB.y));

					_matBatch.BeginPass_Color(GL.LINES);
					
					if(!isPSDAreaEditing)
					{
						DrawLine(pos_LT, pos_RT, editor._colorOption_AtlasBorder, false);
						DrawLine(pos_RT, pos_RB, editor._colorOption_AtlasBorder, false);
						DrawLine(pos_RB, pos_LB, editor._colorOption_AtlasBorder, false);
						DrawLine(pos_LB, pos_LT, editor._colorOption_AtlasBorder, false);
					}
					else
					{
						DrawAnimatedLine(pos_LT, pos_RT, editor._colorOption_AtlasBorder, false);
						DrawAnimatedLine(pos_RT, pos_RB, editor._colorOption_AtlasBorder, false);
						DrawAnimatedLine(pos_RB, pos_LB, editor._colorOption_AtlasBorder, false);
						DrawAnimatedLine(pos_LB, pos_LT, editor._colorOption_AtlasBorder, false);
					}
					
					//삭제 21.5.18
					//GL.End();//<전환 완료>
					//GL.Flush();
					
				}

				//외곽선을 그려주자
				//float imageWidthHalf = mesh._textureData._width * 0.5f;
				//float imageHeightHalf = mesh._textureData._height * 0.5f;

				float imageWidthHalf = mesh.LinkedTextureData._width * 0.5f;
				float imageHeightHalf = mesh.LinkedTextureData._height * 0.5f;

				Vector2 pos_TexOutline_LT = matrix.MultiplyPoint(new Vector2(-imageWidthHalf, -imageHeightHalf));
				Vector2 pos_TexOutline_RT = matrix.MultiplyPoint(new Vector2(imageWidthHalf, -imageHeightHalf));
				Vector2 pos_TexOutline_LB = matrix.MultiplyPoint(new Vector2(-imageWidthHalf, imageHeightHalf));
				Vector2 pos_TexOutline_RB = matrix.MultiplyPoint(new Vector2(imageWidthHalf, imageHeightHalf));

				//변경 21.5.18
				_matBatch.BeginPass_Color(GL.LINES);
				//_matBatch.SetClippingSize(_glScreenClippingSize);
				//GL.Begin(GL.LINES);



				DrawLine(pos_TexOutline_LT, pos_TexOutline_RT, editor._colorOption_AtlasBorder, false);
				DrawLine(pos_TexOutline_RT, pos_TexOutline_RB, editor._colorOption_AtlasBorder, false);
				DrawLine(pos_TexOutline_RB, pos_TexOutline_LB, editor._colorOption_AtlasBorder, false);
				DrawLine(pos_TexOutline_LB, pos_TexOutline_LT, editor._colorOption_AtlasBorder, false);

				//삭제 21.5.18
				//GL.End();//<전환 완료>
				//GL.Flush();
				EndPass();

				
				//3. Edge를 렌더링하자 (전체 / Ouline)
				if (nEdges > 0
					&& (renderRequest.AllEdges || renderRequest.TransparentEdges)										//변경 22.3.3 (v1.4.0)
					)
				{
					Color edgeColor = editor._colorOption_MeshEdge;
					
					//if((renderType & RENDER_TYPE.TransparentEdges) != 0)	//이전
					if(renderRequest.TransparentEdges)						//변경 22.3.3
					{
						edgeColor.a *= 0.5f;//반투명인 경우
					}
					Vector2 pos0 = Vector2.zero, pos1 = Vector2.zero;

					apMeshEdge curEdge = null;
					apMeshPolygon curPolygon = null;

					List<apMeshEdge> curHiddenEdges = null;
					int nCurHiddenEdges = 0;

					apMeshEdge curHiddenEdge = null;

					//변경 21.5.18
					_matBatch.BeginPass_Color(GL.LINES);
						
					if (renderRequest.TestPinWeight)
					{
						//[핀 테스트 모드]에서의 렌더링
						for (int i = 0; i < nEdges; i++)
						{
							curEdge = mesh._edges[i];

							pos0 = matrix.MultiplyPoint(curEdge._vert1._pos_PinTest);
							pos1 = matrix.MultiplyPoint(curEdge._vert2._pos_PinTest);

							DrawLine(pos0, pos1, edgeColor, false);
						}

						if (renderRequest.AllEdges && nPolygons > 0) //변경 22.3.3
						{
							for (int iPoly = 0; iPoly < nPolygons; iPoly++)
							{
								curPolygon = mesh._polygons[iPoly];
								curHiddenEdges = curPolygon._hidddenEdges;
								nCurHiddenEdges = curHiddenEdges != null ? curHiddenEdges.Count : 0;

								if (nCurHiddenEdges > 0)
								{
									for (int iHE = 0; iHE < nCurHiddenEdges; iHE++)
									{
										curHiddenEdge = curHiddenEdges[iHE];

										pos0 = matrix.MultiplyPoint(curHiddenEdge._vert1._pos_PinTest);
										pos1 = matrix.MultiplyPoint(curHiddenEdge._vert2._pos_PinTest);

										DrawLine(pos0, pos1, editor._colorOption_MeshHiddenEdge, false);
									}
								}
							}
						}
					}
					else
					{
						//[일반 모드]에서의 렌더링
						for (int i = 0; i < nEdges; i++)
						{
							curEdge = mesh._edges[i];

							pos0 = matrix.MultiplyPoint(curEdge._vert1._pos);
							pos1 = matrix.MultiplyPoint(curEdge._vert2._pos);

							DrawLine(pos0, pos1, edgeColor, false);
						}

						if (renderRequest.AllEdges && nPolygons > 0)                     //변경 22.3.3
						{
							for (int iPoly = 0; iPoly < nPolygons; iPoly++)
							{
								curPolygon = mesh._polygons[iPoly];
								curHiddenEdges = curPolygon._hidddenEdges;
								nCurHiddenEdges = curHiddenEdges != null ? curHiddenEdges.Count : 0;

								if (nCurHiddenEdges > 0)
								{
									for (int iHE = 0; iHE < nCurHiddenEdges; iHE++)
									{
										curHiddenEdge = curHiddenEdges[iHE];

										pos0 = matrix.MultiplyPoint(curHiddenEdge._vert1._pos);
										pos1 = matrix.MultiplyPoint(curHiddenEdge._vert2._pos);

										DrawLine(pos0, pos1, editor._colorOption_MeshHiddenEdge, false);
									}
								}
							}
						}
					}
				}
				
				//if ((renderType & RENDER_TYPE.Outlines) != 0)		//이전
				if (renderRequest.Outlines && nEdges > 0)							//변경 22.3.3
				{
					Vector2 pos0 = Vector2.zero, pos1 = Vector2.zero;
					
					_matBatch.BeginPass_Color(GL.TRIANGLES);
						
					apMeshEdge curEdge = null;

					for (int i = 0; i < nEdges; i++)
					{
						curEdge = mesh._edges[i];
							
						if (!curEdge._isOutline)
						{
							continue;
						}

						pos0 = matrix.MultiplyPoint(curEdge._vert1._pos);
						pos1 = matrix.MultiplyPoint(curEdge._vert2._pos);

						DrawBoldLine(pos0, pos1, 6.0f, editor._colorOption_Outline, false);
					}
				}

				//if ((renderType & RENDER_TYPE.PolygonOutline) != 0)	//이전
				if (renderRequest.PolygonOutline
					&& selectedPolygon != null
					&& nPolygons > 0)						//변경 22.3.3
				{
					Vector2 pos0 = Vector2.zero, pos1 = Vector2.zero;

					//변경 21.5.18
					_matBatch.BeginPass_Color(GL.TRIANGLES);
					
					List<apMeshEdge> selectedPolyEdges = selectedPolygon._edges;
					int nSelectedPolyEdges = selectedPolyEdges != null ? selectedPolyEdges.Count : 0;

					if (nSelectedPolyEdges > 0)
					{
						apMeshEdge curSelectedPolyEdge = null;

						for (int i = 0; i < nSelectedPolyEdges; i++)
						{
							curSelectedPolyEdge = selectedPolygon._edges[i];
							pos0 = matrix.MultiplyPoint(curSelectedPolyEdge._vert1._pos);
							pos1 = matrix.MultiplyPoint(curSelectedPolyEdge._vert2._pos);

							DrawBoldLine(pos0, pos1, 6.0f, editor._colorOption_Outline, false);
						}
					}
					
				}

				//3. 버텍스를 렌더링하자
				//if ((renderType & RENDER_TYPE.Vertex) != 0)	//이전
				if (renderRequest.Vertex != RenderTypeRequest.VISIBILITY.Hidden && nVerts > 0) //변경 22.3.3
				{
					//변경 22.4.12 [v1.4.0]
					_matBatch.BeginPass_VertexAndPin(GL.TRIANGLES);


					//이전 : 텍스쳐 없는 World 기준 사이즈
					//float pointSize = 10.0f / _zoom;

					//변경 22.4.12 : 텍스쳐로 그려지는 GL 기준 사이즈
					//float halfPointSize = VERTEX_RENDER_SIZE * 0.5f; // 삭제 v1.4.2 : 옵션에 따른 변수값을 바로 사용
					

					Vector2 posGL = Vector2.zero;

					//버텍스 투명도 설정
					float vertAlphaRatio = renderRequest.Vertex == RenderTypeRequest.VISIBILITY.Transparent ? 0.5f : 1.0f;//메시는 메시 그룹때보단 버텍스 반투명도가 조금 높다. Weight를 보기 위함

					Color vColor = Color.black;
					apVertex curVert = null;

					if (renderRequest.TestPinWeight)
					{
						//Pin 편집 모드일때 - 버텍스 Test 위치 + Ratio에 따른 가중치 색상 출력
						for (int i = 0; i < nVerts; i++)
						{
							curVert = meshVerts[i];

							vColor = GetWeightColor4_Vert(curVert._pinWeightRatio);
							vColor.a *= vertAlphaRatio;

							posGL = World2GL(matrix.MultiplyPoint(curVert._pos_PinTest));
							
							DrawVertex(ref posGL, _vertexRenderSizeHalf, ref vColor);
						}
					}
					else if(renderRequest.PinVertWeight)
					{
						//Pin 편집 모드 중 테스트 모드가 아닌 그 외의 모드일 때 : 버텍스 기본 위치 + 가중치 표시
						
						for (int i = 0; i < nVerts; i++)
						{
							curVert = meshVerts[i];
							
							vColor = GetWeightColor4_Vert(curVert._pinWeightRatio);
							vColor.a *= vertAlphaRatio;

							posGL = World2GL(matrix.MultiplyPoint(curVert._pos));
							
							DrawVertex(ref posGL, _vertexRenderSizeHalf, ref vColor);
						}
					}
					else
					{
						//나머지 일반 모드일때 - 버텍스 기본 위치 출력
						for (int i = 0; i < nVerts; i++)
						{
							curVert = meshVerts[i];
							
							vColor = editor._colorOption_VertColor_NotSelected;

							if (curVert == selectedVertex)
							{
								vColor = editor._colorOption_VertColor_Selected;
							}
							else if (curVert == nextSelectedVertex)
							{
								vColor = _vertColor_NextSelected;
							}
							else if (selectedVertices != null)
							{
								if (selectedVertices.Contains(curVert))
								{
									vColor = editor._colorOption_VertColor_Selected;
								}
							}
							vColor.a *= vertAlphaRatio;

							posGL = World2GL(matrix.MultiplyPoint(curVert._pos));
							
							//이전
							//DrawBox(posGL, pointSize, pointSize, vColor, isWireFramePoint, false);

							//변경 22.4.12
							DrawVertex(ref posGL, _vertexRenderSizeHalf, ref vColor);

							AddCursorRect(mousePosition, posGL, 10, 10, MouseCursor.MoveArrow);
						}
					}

					//삭제 21.5.18
					//GL.End();//<전환 완료>  (맨밑에)
					//GL.Flush();
				}

				EndPass();


				//추가 22.3.4 (v1.4.0)
				//4. 핀을 렌더링하자
				if(renderRequest.Pin != RenderTypeRequest.VISIBILITY.Hidden)
				{
					int nPins = 0;
					List<apMeshPin> meshPins = null;
					if(mesh._pinGroup != null)
					{
						meshPins = mesh._pinGroup._pins_All;
						nPins = meshPins != null ? meshPins.Count : 0;
					}
					if (nPins > 0)
					{
						apMeshPin curPin = null;
						apMeshPinCurve cur2NextCurve = null;

						

						//4-1. 핀 라인을 렌더링하자 : Transparent일 때는 패스
						if (renderRequest.Pin == RenderTypeRequest.VISIBILITY.Shown)
						{
							_matBatch.BeginPass_Color(GL.TRIANGLES);

							Vector2 posLineA = Vector2.zero;
							Vector2 posLineB = Vector2.zero;
							int nCurvePoints = 20;
							Color curveLineColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
							Color curveLineSelected = new Color(1.0f, 0.7f, 0.0f, 1.0f);

							Color curCurveColor = Color.black;

							if (renderRequest.TestPinWeight)
							{
								//[테스트 모드]
								for (int iPin = 0; iPin < nPins; iPin++)
								{
									curPin = meshPins[iPin];

									//Next로만 연결
									cur2NextCurve = curPin._nextCurve;

									if (cur2NextCurve == null)
									{
										continue;
									}

									bool isSelected = false;
									if (selectedPins != null)
									{
										if (selectedPins.Contains(cur2NextCurve._prevPin) || selectedPins.Contains(cur2NextCurve._nextPin))
										{
											isSelected = true;
										}
									}
									curCurveColor = isSelected ? curveLineSelected : curveLineColor;

									if (cur2NextCurve.IsLinear())
									{
										//두개의 핀 사이가 직선이라면
										posLineA = matrix.MultiplyPoint(cur2NextCurve.GetCurvePos_Test(apMeshPin.TMP_VAR_TYPE.MeshTest, 0.0f));
										posLineB = matrix.MultiplyPoint(cur2NextCurve.GetCurvePos_Test(apMeshPin.TMP_VAR_TYPE.MeshTest, 1.0f));

										DrawBoldLine(posLineA, posLineB, _pinLineThickness, curCurveColor, false);
									}
									else
									{
										//두개의 핀 사이가 커브라면
										for (int iLerp = 0; iLerp < nCurvePoints; iLerp++)
										{
											float lerpA = (float)iLerp / (float)nCurvePoints;
											float lerpB = (float)(iLerp + 1) / (float)nCurvePoints;

											posLineA = matrix.MultiplyPoint(cur2NextCurve.GetCurvePos_Test(apMeshPin.TMP_VAR_TYPE.MeshTest, lerpA));
											posLineB = matrix.MultiplyPoint(cur2NextCurve.GetCurvePos_Test(apMeshPin.TMP_VAR_TYPE.MeshTest, lerpB));
											DrawBoldLine(posLineA, posLineB, _pinLineThickness, curCurveColor, false);
										}
									}
								}
							}
							else
							{
								//[일반 모드]
								for (int iPin = 0; iPin < nPins; iPin++)
								{
									curPin = meshPins[iPin];
									
									//Next로만 연결
									cur2NextCurve = curPin._nextCurve;
									
									if (cur2NextCurve == null)
									{
										continue;
									}

									bool isSelected = false;
									if (selectedPins != null)
									{
										if (selectedPins.Contains(cur2NextCurve._prevPin) || selectedPins.Contains(cur2NextCurve._nextPin))
										{
											isSelected = true;
										}
									}

									curCurveColor = isSelected ? curveLineSelected : curveLineColor;

									if (cur2NextCurve.IsLinear())
									{
										//두개의 핀 사이가 직선이라면
										posLineA = matrix.MultiplyPoint(cur2NextCurve.GetCurvePos_Default(0.0f));
										posLineB = matrix.MultiplyPoint(cur2NextCurve.GetCurvePos_Default(1.0f));

										DrawBoldLine(posLineA, posLineB, _pinLineThickness, curCurveColor, false);
									}
									else
									{
										//두개의 핀 사이가 커브라면
										for (int iLerp = 0; iLerp < nCurvePoints; iLerp++)
										{
											float lerpA = (float)iLerp / (float)nCurvePoints;
											float lerpB = (float)(iLerp + 1) / (float)nCurvePoints;

											posLineA = matrix.MultiplyPoint(cur2NextCurve.GetCurvePos_Default(lerpA));
											posLineB = matrix.MultiplyPoint(cur2NextCurve.GetCurvePos_Default(lerpB));
											DrawBoldLine(posLineA, posLineB, _pinLineThickness, curCurveColor, false);
										}
									}
								}
							}
						}

						//4-2. 선택한 Pin들의 Range를 보여주자
						if (renderRequest.PinRange)
						{
							if (selectedPin != null && selectedPins != null)
							{
								Color color_RangeInner = new Color(0.0f, 1.0f, 0.0f, 0.7f);
								Color color_RangeOuter = new Color(1.0f, 1.0f, 0.0f, 0.5f);

								_matBatch.BeginPass_Color(GL.TRIANGLES);

								int nSelectedMeshPins = selectedPins != null ? selectedPins.Count : 0;

								for (int iPin = 0; iPin < nSelectedMeshPins; iPin++)
								{
									curPin = selectedPins[iPin];
									Vector2 pinPos = renderRequest.TestPinWeight ? curPin.TmpPos_MeshTest : curPin._defaultPos;
									pinPos = matrix.MultiplyPoint(pinPos);

									float weightRange = Mathf.Max((float)curPin._range, 0.0f);
									float weightFadeRange = weightRange + Mathf.Max((float)curPin._fade, 0.0f);

									DrawBoldCircleGL(World2GL(pinPos), weightRange, 1.5f, color_RangeInner, false);
									DrawBoldCircleGL(World2GL(pinPos), weightFadeRange, 1.0f, color_RangeOuter, false);
								}

							}
						}

						//4-3. 핀을 렌더링하자
						Color pinColor_None = new Color(1.0f, 1.0f, 0.0f, 1.0f);
						Color pinColor_Selected = new Color(1.0f, 0.15f, 0.5f, 1.0f);
						if (renderRequest.Pin == RenderTypeRequest.VISIBILITY.Transparent)
						{
							//투명도를 더 줄이자 (기존 0.4, 0.6 > 변경 0.3, 0.5 v1.4.2)
							pinColor_None.a = 0.3f;
							pinColor_Selected.a = 0.5f;
						}

						_matBatch.BeginPass_VertexAndPin(GL.TRIANGLES);

						//float halfPointSizeGL = PIN_RENDER_SIZE * 0.5f;//삭제 v1.4.2 : 옵션에 따른 변수를 직접 사용

						Vector2 posGL = Vector2.zero;
						Color vColor = Color.black;

						Vector2 cpPoint_Prev = Vector2.zero;
						Vector2 cpPoint_Next = Vector2.zero;

						if (renderRequest.TestPinWeight)
						{
							//Test의 위치를 출력하자
							for (int iPin = 0; iPin < nPins; iPin++)
							{
								curPin = meshPins[iPin];

								if (selectedPins != null && selectedPins.Contains(curPin))
								{
									vColor = pinColor_Selected;
								}
								else
								{
									vColor = pinColor_None;
								}

								posGL = World2GL(matrix.MultiplyPoint(curPin.TmpPos_MeshTest));
								cpPoint_Prev = matrix.MultiplyPoint(curPin.TmpControlPos_Prev_MeshTest);
								cpPoint_Next = matrix.MultiplyPoint(curPin.TmpControlPos_Next_MeshTest);

								DrawPin(ref posGL, _pinRenderSizeHalf, ref vColor);

								AddCursorRect(mousePosition, posGL, 14, 14, MouseCursor.MoveArrow);//이건 옵션 켤때만
							}
						}
						else
						{
							//Default의 위치를 출력하자
							for (int iPin = 0; iPin < nPins; iPin++)
							{
								curPin = meshPins[iPin];

								if (selectedPins != null && selectedPins.Contains(curPin))
								{
									vColor = pinColor_Selected;
								}
								else
								{
									vColor = pinColor_None;
								}

								posGL = World2GL(matrix.MultiplyPoint(curPin._defaultPos));
								cpPoint_Prev = World2GL(matrix.MultiplyPoint(curPin._controlPointPos_Def_Prev));
								cpPoint_Next = World2GL(matrix.MultiplyPoint(curPin._controlPointPos_Def_Next));

								DrawPin(ref posGL, _pinRenderSizeHalf, ref vColor);

								AddCursorRect(mousePosition, posGL, 14, 14, MouseCursor.MoveArrow);//이건 옵션 켤때만
							}
						}

						//EndPass();

						
					}
				}

				EndPass();

			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
		}


		public static void DrawMeshAreaEditing(	apMesh mesh, 
												apMatrix3x3 matrix, 
												apEditor editor,
												Vector2 mousePosition)
		{
			
			try
			{
				if (mesh == null || mesh.LinkedTextureData == null || mesh.LinkedTextureData._image == null)//변경 코드
				{
					return;
				}
				if(!mesh._isPSDParsed)
				{
					return;
				}
				matrix *= mesh.Matrix_VertToLocal;

				Texture2D imgControlPoint = editor.ImageSet.Get(apImageSet.PRESET.TransformControlPoint);
				
				//크기는 26
				float imgSize = 26.0f / apGL.Zoom;

				Vector2 pos_LT = matrix.MultiplyPoint(new Vector2(mesh._atlasFromPSD_LT.x, mesh._atlasFromPSD_LT.y));
				Vector2 pos_RT = matrix.MultiplyPoint(new Vector2(mesh._atlasFromPSD_RB.x, mesh._atlasFromPSD_LT.y));
				Vector2 pos_LB = matrix.MultiplyPoint(new Vector2(mesh._atlasFromPSD_LT.x, mesh._atlasFromPSD_RB.y));
				Vector2 pos_RB = matrix.MultiplyPoint(new Vector2(mesh._atlasFromPSD_RB.x, mesh._atlasFromPSD_RB.y));
				
				AddCursorRect(mousePosition, World2GL(pos_LT), 20, 20, MouseCursor.MoveArrow);
				AddCursorRect(mousePosition, World2GL(pos_RT), 20, 20, MouseCursor.MoveArrow);
				AddCursorRect(mousePosition, World2GL(pos_LB), 20, 20, MouseCursor.MoveArrow);
				AddCursorRect(mousePosition, World2GL(pos_RB), 20, 20, MouseCursor.MoveArrow);

				//변경 21.5.18
				_matBatch.BeginPass_Texture_VColor(GL.TRIANGLES, _textureColor_Gray, imgControlPoint, 1.0f, apPortrait.SHADER_TYPE.AlphaBlend, false, Vector4.zero);
				//_matBatch.SetClippingSize(_glScreenClippingSize);
				//GL.Begin(GL.TRIANGLES);

				//4개의 점을 만든다.
				


				DrawTextureGLWithVColor(imgControlPoint, World2GL(pos_LT), imgSize, imgSize, (editor.Select._meshAreaPointEditType == apSelection.MESH_AREA_POINT_EDIT.LT ? Color.red : Color.white), 1.0f);
				DrawTextureGLWithVColor(imgControlPoint, World2GL(pos_RT), imgSize, imgSize, (editor.Select._meshAreaPointEditType == apSelection.MESH_AREA_POINT_EDIT.RT ? Color.red : Color.white), 1.0f);
				DrawTextureGLWithVColor(imgControlPoint, World2GL(pos_LB), imgSize, imgSize, (editor.Select._meshAreaPointEditType == apSelection.MESH_AREA_POINT_EDIT.LB ? Color.red : Color.white), 1.0f);
				DrawTextureGLWithVColor(imgControlPoint, World2GL(pos_RB), imgSize, imgSize, (editor.Select._meshAreaPointEditType == apSelection.MESH_AREA_POINT_EDIT.RB ? Color.red : Color.white), 1.0f);

				//삭제 21.5.18
				//GL.End();//<전환 완료>
				//GL.Flush();

				EndPass();
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
		}



		//------------------------------------------------------------------------------------------------
		// Mesh 모드에서 Mirror 라인 긋기
		//------------------------------------------------------------------------------------------------
		public static void DrawMeshMirror(apMesh mesh)
		{
			if (mesh == null || mesh.LinkedTextureData == null || mesh.LinkedTextureData._image == null)
			{
				return;
			}

			//Vector2 imageHalfOffset = new Vector2(mesh.LinkedTextureData._width * 0.5f, mesh.LinkedTextureData._height * 0.5f);
			
			Vector2 posW = Vector2.zero;
			Vector2 posA_GL = Vector2.zero;
			Vector2 posB_GL = Vector2.zero;
			if(mesh._isMirrorX)
			{
				//세로 줄을 긋는다.
				//posW.x = mesh._mirrorAxis.x - (mesh._offsetPos.x + imageHalfOffset.x);
				posW.x = mesh._mirrorAxis.x - (mesh._offsetPos.x);
				posA_GL = World2GL(posW);
				posB_GL = posA_GL;

				posA_GL.y = -500;
				posB_GL.y = _windowHeight + 500;
			}
			else
			{
				//가로 줄을 긋는다.
				//posW.y = mesh._mirrorAxis.y - (mesh._offsetPos.y + imageHalfOffset.y);
				posW.y = mesh._mirrorAxis.y - (mesh._offsetPos.y);
				posA_GL = World2GL(posW);
				posB_GL = posA_GL;

				posA_GL.x = -500;
				posB_GL.x = _windowWidth + 500;
			}
			
			DrawBoldLineGL(posA_GL, posB_GL, 3, new Color(0.0f, 1.0f, 0.5f, 0.4f), true);

			
		}

		//------------------------------------------------------------------------------------------------
		// Draw Mesh의 Edge Wire
		//------------------------------------------------------------------------------------------------
		public static void DrawMeshWorkSnapNextVertex(apMesh mesh, apVertexController vertexController)
		{
			try
			{
				//0. 메시, 텍스쳐가 없을 때
				//if (mesh == null || mesh._textureData == null || mesh._textureData._image == null)
				if (mesh == null || mesh.LinkedTextureData == null || mesh.LinkedTextureData._image == null)
				{
					return;
				}

				if (vertexController.LinkedNextVertex != null && vertexController.LinkedNextVertex != vertexController.Vertex)
				{
					Vector2 linkedVertPosW = vertexController.LinkedNextVertex._pos - mesh._offsetPos;
					
					//float size = 24.0f / _zoom;					
					float size = (_vertexRenderSizeHalf * 2.4f) / _zoom;//변경 v1.4.2 옵션에 의한 크기 보다 조금 더 큰 정도

					DrawBox(linkedVertPosW, size, size, Color.green, true);
				}
			}
			catch (Exception ex)
			{
				Debug.LogError("AnyPortrait : DrawMeshWorkSnapNextVertex() Exception : " + ex);
			}
		}
		public static void DrawMeshWorkEdgeSnap(apMesh mesh, apVertexController vertexController)
		{
			try
			{
				//0. 메시, 텍스쳐가 없을 때
				//if (mesh == null || mesh._textureData == null || mesh._textureData._image == null)
				if (mesh == null || mesh.LinkedTextureData == null || mesh.LinkedTextureData._image == null)
				{
					return;
				}

				//if (vertexController.IsTmpSnapToEdge && vertexController.Vertex == null)
				if (vertexController.IsTmpSnapToEdge)
				{
					//float size = 20.0f / _zoom;//이전
					float size = (_vertexRenderSizeHalf * 2.0f) / _zoom;//변경 v1.4.2


					DrawBox(vertexController.TmpSnapToEdgePos - mesh._offsetPos, size, size, new Color(0.0f, 1.0f, 1.0f, 1.0f), true);
				}
			}
			catch (Exception ex)
			{
				Debug.LogError("AnyPortrait : DrawMeshWorkEdgeSnap() Exception : " + ex);
			}
		}
		public static void DrawMeshWorkEdgeWire(apMesh mesh, apMatrix3x3 matrix, apVertexController vertexController, bool isCross, bool isCrossMultiple)
		{
			try
			{
				//0. 메시, 텍스쳐가 없을 때
				//if (mesh == null || mesh._textureData == null || mesh._textureData._image == null)
				if (mesh == null || mesh.LinkedTextureData == null || mesh.LinkedTextureData._image == null)
				{
					return;
				}

				if (vertexController.Vertex == null)
				{
					return;
				}

				matrix *= mesh.Matrix_VertToLocal;

				Vector2 mouseW = GL2World(vertexController.TmpEdgeWirePos);
				Vector2 vertPosW = matrix.MultiplyPoint(vertexController.Vertex._pos);

				Color lineColor = Color.green;
				if (isCross)
				{
					lineColor = Color.red;
				}
				else if (isCrossMultiple)
				{
					lineColor = new Color(0.2f, 0.8f, 1.0f, 1.0f);
				}

				//DrawLine(vertPosW, mouseW, lineColor, true);
				DrawAnimatedLine(vertPosW, mouseW, lineColor, true);

				//if (vertexController.LinkedNextVertex != null && vertexController.LinkedNextVertex != vertexController.Vertex)
				//{
				//	Vector2 linkedVertPosW = matrix.MultiplyPoint(vertexController.LinkedNextVertex._pos);
				//	float size = 20.0f / _zoom;
				//	DrawBox(linkedVertPosW, size, size, lineColor, true);
				//}

				//float size = 20.0f / _zoom;//이전
				float size = (_vertexRenderSizeHalf * 2.0f) / _zoom;//변경 v1.4.2

				if (isCross)
				{
					Vector2 crossPointW = matrix.MultiplyPoint(vertexController.EdgeWireCrossPoint());
					
					DrawBox(crossPointW, size, size, Color.cyan, true);
				}
				else if (isCrossMultiple)
				{
					List<Vector2> crossVerts = vertexController.EdgeWireMultipleCrossPoints();

					for (int i = 0; i < crossVerts.Count; i++)
					{
						Vector2 crossPointW = matrix.MultiplyPoint(crossVerts[i]);
						DrawBox(crossPointW, size, size, Color.yellow, true);
					}
				}
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
		}


		public static void DrawMeshWorkMirrorEdgeWire(apMesh mesh, apMirrorVertexSet mirrorSet)
		{
			try
			{
				if (mesh == null)
				{
					return;
				}

				//DrawBox(-mesh._offsetPos, 50, 50, Color.yellow, false);

				Color lineColor = new Color(1, 1, 0, 0.5f);
				Color nearVertColor = new Color(1, 1, 0, 0.5f);

				//둘다 유효하다면 => 선을 긋는다.
				//하나만 유효하다면 => 점 또는 Vertex 외곽선을 만든다.
				bool isPrevEnabled = mirrorSet._meshWork_TypePrev != apMirrorVertexSet.MIRROR_MESH_WORK_TYPE.None;
				bool isNextEnabled = mirrorSet._meshWork_TypeNext != apMirrorVertexSet.MIRROR_MESH_WORK_TYPE.None;
				bool isPrevNearVert = false;
				bool isNextNearVert = false;
				Vector2 posPrev = Vector2.zero;
				Vector2 posNext = Vector2.zero;

				//float pointSize = 10.0f / _zoom;//<<원래 크기
				//float pointSize_Wire = 18.0f / _zoom;

				//변경 v1.4.2
				float pointSize = (_vertexRenderSizeHalf * 1.7f) / _zoom;
				float pointSize_Wire = (_vertexRenderSizeHalf * 2.0f) / _zoom;

				if(isPrevEnabled)
				{
					posPrev = mirrorSet._meshWork_PosPrev - mesh._offsetPos;
					if(mirrorSet._meshWork_VertPrev != null)
					{
						//버텍스 위치로 이동
						isPrevNearVert = true;
						posPrev = mirrorSet._meshWork_VertPrev._pos - mesh._offsetPos;
					}
				}
				if(isNextEnabled)
				{
					posNext = mirrorSet._meshWork_PosNext - mesh._offsetPos;
					if(mirrorSet._meshWork_VertNext != null)
					{
						//버텍스 위치로 이동
						isNextNearVert = true;
						posNext = mirrorSet._meshWork_VertNext._pos - mesh._offsetPos;
					}
				}
				if(isPrevEnabled && isNextEnabled)
				{
					//선을 긋자
					DrawAnimatedLine(posPrev, posNext, lineColor, true);
				}

				//점을 찍자
				if(isPrevEnabled)
				{
					if(isPrevNearVert)
					{
						DrawBox(posPrev, pointSize_Wire, pointSize_Wire, nearVertColor, true);
					}
					else
					{
						DrawBox(posPrev, pointSize, pointSize, nearVertColor, true);
					}
				}
				if(isNextEnabled)
				{
					if(isNextNearVert)
					{
						DrawBox(posNext, pointSize_Wire, pointSize_Wire, nearVertColor, true);
					}
					else
					{
						DrawBox(posNext, pointSize, pointSize, nearVertColor, true);
					}
				}

				//만약 Snap이 되는 상황이면, Snap되는 위치를 찍자
				if(mirrorSet._meshWork_SnapToAxis)
				{
					DrawBox(mirrorSet._meshWork_PosNextSnapped - mesh._offsetPos, pointSize_Wire, pointSize_Wire, new Color(0, 1, 1, 0.8f), true);
				}
			}

			catch(Exception ex)
			{
				Debug.LogError("AnyPortrait : DrawMeshWorkMirrorEdgeWire Exception : " + ex);
			}
		}

		//------------------------------------------------------------------------------------------------
		// Draw Mirror Mesh PreviewLines
		//------------------------------------------------------------------------------------------------
		public static void DrawMirrorMeshPreview(apMesh mesh, apMirrorVertexSet mirrorSet, apEditor editor, apVertexController vertexController)
		{
			try
			{
				if (mesh == null || mesh.LinkedTextureData == null || mesh.LinkedTextureData._image == null)//변경 코드
				{
					return;
				}

				if(mirrorSet._cloneVerts.Count == 0)
				{
					return;
				}

				Vector2 offsetPos = mesh._offsetPos;
				apMirrorVertexSet.CloneEdge cloneEdge = null;
				apMirrorVertexSet.CloneVertex cloneVert = null;
				Vector2 pos1 = Vector2.zero;
				Vector2 pos2 = Vector2.zero;

				//Edge 렌더
				if (mirrorSet._cloneEdges.Count > 0)
				{
					Color edgeColor = editor._colorOption_MeshHiddenEdge;
					edgeColor.a *= 0.5f;

					//변경 21.5.18
					_matBatch.BeginPass_Color(GL.LINES);
					//_matBatch.SetClippingSize(_glScreenClippingSize);
					//GL.Begin(GL.LINES);

					for (int iEdge = 0; iEdge < mirrorSet._cloneEdges.Count; iEdge++)
					{
						cloneEdge = mirrorSet._cloneEdges[iEdge];
						pos1 = cloneEdge._cloneVert1._pos - offsetPos;
						pos2 = cloneEdge._cloneVert2._pos - offsetPos;
						DrawDotLine(pos1, pos2, edgeColor, false);
					}

					//삭제 21.5.18
					//GL.End();//<전환 완료>
					_matBatch.EndPass();
				}

				//Vertex 렌더 (Clone / Cross)
				Color vertColor_Mirror = new Color(0.0f, 1.0f, 0.5f, 0.5f);
				Color vertColor_Cross = new Color(1.0f, 1.0f, 0.0f, 0.5f);
				float pointSize = 10.0f / _zoom;
				float pointSize_Wire = 18.0f / _zoom;

				//1) Mirror
				
				//변경 21.5.18
				_matBatch.BeginPass_Color(GL.TRIANGLES);
				//_matBatch.SetClippingSize(_glScreenClippingSize);
				//GL.Begin(GL.TRIANGLES);

				for (int iVert = 0; iVert < mirrorSet._cloneVerts.Count; iVert++)
				{
					cloneVert = mirrorSet._cloneVerts[iVert];
					pos1 = cloneVert._pos - offsetPos;
					if (!cloneVert._isOnAxis)
					{
						DrawBox(pos1, pointSize, pointSize, vertColor_Mirror, false, false);
					}
				}
				
				//삭제 21.5.18
				//GL.End();//<전환 완료>
				_matBatch.EndPass();


				//2) On Axis
				//변경 21.5.18
				_matBatch.BeginPass_Color(GL.LINES);
				//_matBatch.SetClippingSize(_glScreenClippingSize);
				//GL.Begin(GL.LINES);

				for (int iVert = 0; iVert < mirrorSet._cloneVerts.Count; iVert++)
				{
					cloneVert = mirrorSet._cloneVerts[iVert];
					pos1 = cloneVert._pos - offsetPos;
					if (cloneVert._isOnAxis)
					{
						DrawBox(pos1, pointSize_Wire, pointSize_Wire, vertColor_Cross, true, false);
					}
				}
				
				//삭제 21.5.18
				//GL.End();//<전환 완료>
				_matBatch.EndPass();

				//3) Cross
				//변경 21.5.18
				_matBatch.BeginPass_Color(GL.TRIANGLES);
				//_matBatch.SetClippingSize(_glScreenClippingSize);
				//GL.Begin(GL.TRIANGLES);

				for (int iVert = 0; iVert < mirrorSet._crossVerts.Count; iVert++)
				{
					cloneVert = mirrorSet._crossVerts[iVert];
					pos1 = cloneVert._pos - offsetPos;
					DrawBox(pos1, pointSize, pointSize, vertColor_Cross, false, false);
				}

				//삭제 21.5.18
				//GL.End();//<전환 완료>
				_matBatch.EndPass();
			}
			catch(Exception ex)
			{
				Debug.LogError("AnyPortrait : DrawMirrorMeshPreview Exception : " + ex);
			}
		}



		public static void DrawMeshWorkSnapNextPin(apMesh mesh, apMeshPin nextPin)
		{
			//0. 메시, 텍스쳐가 없을 때
			if (mesh == null || mesh.LinkedTextureData == null || mesh.LinkedTextureData._image == null)
			{
				return;
			}

			Vector2 linkedPinPosW = nextPin._defaultPos - mesh._offsetPos;
			float size = 20.0f / _zoom;
			DrawCircle(linkedPinPosW, size, Color.green, true);
		}


		public static void DrawMeshWorkPinWire(apMesh mesh, apMeshPin srcPin, Vector2 mousePosW, apMeshPin snapedPin)
		{
			//0. 메시, 텍스쳐가 없을 때
			if (mesh == null || mesh.LinkedTextureData == null || mesh.LinkedTextureData._image == null)
			{
				return;
			}

			if(srcPin == null)
			{
				return;
			}

			if(snapedPin != null)
			{
				//스냅시에는 스냅 핀으로 붙이자
				Vector2 linkedPinPosW = snapedPin._defaultPos - mesh._offsetPos;
				DrawAnimatedLine(srcPin._defaultPos - mesh._offsetPos, linkedPinPosW, Color.green, true);
			}
			else
			{
				DrawAnimatedLine(srcPin._defaultPos - mesh._offsetPos, mousePosW - mesh._offsetPos, Color.green, true);
			}

			
		}
	}
}
