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
	// apGL 중의 함수 중에서 "메시 그룹/애니메이션" 메뉴에서의 렌더링 함수들을 모았다. 가장 핵심
	public static partial class apGL
	{
		private static List<apRenderVertex> _tmpSelectedVertices = new List<apRenderVertex>();
		private static List<apRenderVertex> _tmpSelectedVertices_Weighted = new List<apRenderVertex>();
		private static List<apRenderPin> _tmpSelectedPins = new List<apRenderPin>();
		private static List<apRenderPin> _tmpSelectedPins_Weighted = new List<apRenderPin>();
		//private static List<float> _tmpSelectedVertices_WeightedValue = new List<float>();

		public static void DrawRenderUnit(apRenderUnit renderUnit,
											
											//RENDER_TYPE renderType,			//이전
											RenderTypeRequest renderRequest,	//변경 22.3.3

											apVertexController vertexController,
											apSelection select,
											apEditor editor,
											Vector2 mousePos,
											
											bool isMainSelected = true)//선택된 경우, Main인지 여부 (20.5.28)
		{
			try
			{
				//0. 메시, 텍스쳐가 없을 때
				if (renderUnit == null || renderUnit._meshTransform == null || renderUnit._meshTransform._mesh == null)
				{
					return;
				}

				//이전
				//if (renderUnit._renderVerts.Count == 0) { return; }

				//변경 22.3.23 [v1.4.0]
				int nRenderVerts = renderUnit._renderVerts != null ? renderUnit._renderVerts.Length : 0;
				if(nRenderVerts == 0)
				{
					return;
				}

				Color textureColor = renderUnit._meshColor2X;

				apTransform_Mesh meshTF = renderUnit._meshTransform;

				
				apMesh mesh = renderUnit._meshTransform._mesh;
				bool isVisible = renderUnit._isVisible;

				//메시의 버텍스/인덱스/선분 리스트
				List<apVertex> meshVerts = mesh._vertexData;
				int nVerts = meshVerts != null ? meshVerts.Count : 0;

				List<int> meshIndexBuffers = mesh._indexBuffer;
				int nIndexBuffers = meshIndexBuffers != null ? meshIndexBuffers.Count : 0;

				List<apMeshEdge> meshEdges = mesh._edges;
				int nEdges = meshEdges != null ? meshEdges.Count : 0;


				apTextureData linkedTextureData = mesh.LinkedTextureData;

				//추가 12.4 : Extra Option에 의해 Texture가 바귀었을 경우
				if(renderUnit.IsExtraTextureChanged)
				{
					linkedTextureData = renderUnit.ChangedExtraTextureData;
				}

				if(linkedTextureData == null)
				{
					return;
				}
				

				//미리 GL 좌표를 연산하고, 나중에 중복 연산(World -> GL)을 하지 않도록 하자
				apRenderVertex rVert = null;
				for (int i = 0; i < nRenderVerts; i++)
				{
					rVert = renderUnit._renderVerts[i];
					
					//변경 v1.4.4 : Ref 이용
					//rVert._pos_GL = World2GL(rVert._pos_World);
					World2GL(ref rVert._pos_GL, ref rVert._pos_World);
				}


				bool isAnyVertexSelected = false;
				bool isWeightedSelected = false;

				//이전
				//bool isBoneWeightColor = (renderType & RENDER_TYPE.BoneRigWeightColor) != 0;
				//bool isPhyVolumeWeightColor = (renderType & RENDER_TYPE.PhysicsWeightColor) != 0 || (renderType & RENDER_TYPE.VolumeWeightColor) != 0;

				//변경 22.3.3 (v1.4.0)
				bool isBoneWeightColor = renderRequest.BoneRigWeightColor;
				bool isPhyVolumeWeightColor = renderRequest.PhysicsWeightColor || renderRequest.VolumeWeightColor;

				bool isBoneColor = false;
				bool isCircleRiggingVert = editor._rigViewOption_CircleVert;
				float vertexColorRatio = 0.0f;

				if (select != null)
				{
					_tmpSelectedVertices.Clear();
					_tmpSelectedVertices_Weighted.Clear();
					//_tmpSelectedVertices_WeightedValue.Clear();

					//Soft Selection + TODO 나중에 Volume 등에서 Weighted 설정을 하자
					if (select.Editor.Gizmos.IsSoftSelectionMode)
					{
						isWeightedSelected = true;
					}

					//isBoneColor = select._rigEdit_isBoneColorView;//이전
					isBoneColor = editor._rigViewOption_BoneColor;//변경 19.7.31

					if (isBoneWeightColor)
					{
						//if (select._rigEdit_viewMode == apSelection.RIGGING_EDIT_VIEW_MODE.WeightColorOnly)
						if(editor._rigViewOption_WeightOnly)
						{
							vertexColorRatio = 1.0f;
						}
						else
						{
							vertexColorRatio = 0.5f;
						}
					}
					else if (isPhyVolumeWeightColor)
					{
						vertexColorRatio = 0.7f;
					}

					isAnyVertexSelected = true;
					if (select.SelectionType == apSelection.SELECTION_TYPE.MeshGroup
						|| select.SelectionType == apSelection.SELECTION_TYPE.Animation//<<추가 20.6.29 : 통합됨
						)
					{
						if (select.ModRenderVerts_All != null && select.ModRenderVerts_All.Count > 0)
						{
							List<apSelection.ModRenderVert> selectedMRVs = select.ModRenderVerts_All;
							int nSelectedMRV = selectedMRVs.Count;

							for (int i = 0; i < nSelectedMRV; i++)
							{
								_tmpSelectedVertices.Add(selectedMRVs[i]._renderVert);
							}

							if (isWeightedSelected)
							{
								List<apSelection.ModRenderVert> weightedMRVs = select.ModRenderVerts_Weighted;
								int nWeightedMRV = weightedMRVs.Count;

								if (nWeightedMRV > 0)
								{
									apSelection.ModRenderVert curMRV = null;
									for (int i = 0; i < nWeightedMRV; i++)
									{
										curMRV = weightedMRVs[i];
										curMRV._renderVert._renderWeightByTool = curMRV._vertWeightByTool;

										_tmpSelectedVertices_Weighted.Add(curMRV._renderVert);
									}
								}
								
							}
						}
					}
				}


				//렌더링 방식은 Mesh (with Color) 또는 Vertex / Outline이 있다.
				bool isMeshRender = false;
				
				//이전
				//bool isVertexRender = ((renderType & RENDER_TYPE.Vertex) != 0);
				//bool isOutlineRender = ((renderType & RENDER_TYPE.Outlines) != 0);
				//bool isAllEdgeRender = ((renderType & RENDER_TYPE.AllEdges) != 0);
				//bool isToneColor = ((renderType & RENDER_TYPE.ToneColor) != 0);

				//변경 22.3.3 (v1.4.0)
				bool isVertexRender =	renderRequest.Vertex != RenderTypeRequest.VISIBILITY.Hidden;
				bool isOutlineRender =	renderRequest.Outlines;
				bool isAllEdgeRender =	renderRequest.AllEdges;
				bool isToneColor =		renderRequest.ToneColor;
				bool isMaskOnly = meshTF._isMaskOnlyMesh;



				if (!isVertexRender && !isOutlineRender)
				{
					isMeshRender = true;
				}
				bool isNotEditedGrayColor = false;

				if(editor._exModObjOption_ShowGray && 
					(	renderUnit._exCalculateMode == apRenderUnit.EX_CALCULATE.Disabled_NotEdit ||
						renderUnit._exCalculateMode == apRenderUnit.EX_CALCULATE.Disabled_ExRun))
				{
					//선택되지 않은 건 Gray 색상으로 표시하고자 할 때
					isNotEditedGrayColor = true;
				}

				
				//bool isDrawTFBorderLine = ((int)(renderType & RENDER_TYPE.TransformBorderLine) != 0);	//이전
				bool isDrawTFBorderLine = renderRequest.TransformBorderLine;							//변경 22.3.3 (v1.4.0)

				//2. 메시를 렌더링하자
				if (nIndexBuffers >= 3 && isMeshRender && isVisible)
				{
					//------------------------------------------
					// Drawcall Batch를 했을때
					Color color0 = Color.black, color1 = Color.black, color2 = Color.black;

					int iVertColor = 0;

					if (renderRequest.VolumeWeightColor)						//변경 22.3.3
					{
						iVertColor = 1;
					}
					//else if ((renderType & RENDER_TYPE.PhysicsWeightColor) != 0)	//이전
					else if (renderRequest.PhysicsWeightColor)						//변경 22.3.3
					{
						iVertColor = 2;
					}
					//else if ((renderType & RENDER_TYPE.BoneRigWeightColor) != 0)	//이전
					else if (renderRequest.BoneRigWeightColor)						//변경 22.3.3
					{
						iVertColor = 3;
					}
					else if(isMaskOnly)
					{
						//Mask Only인 경우 기본 색상값이 녹색
						iVertColor = 0;
						color0 = Color.green;
						color1 = Color.green;
						color2 = Color.green;
					}
					else
					{
						iVertColor = 0;
						color0 = Color.black;
						color1 = Color.black;
						color2 = Color.black;


					}


					if (isToneColor)
					{
						_matBatch.BeginPass_ToneColor_Normal(GL.TRIANGLES, _toneColor, linkedTextureData._image);

					}
					else if (isNotEditedGrayColor)
					{
						//추가 21.2.16 : 편집되지 않은 경우
						_matBatch.BeginPass_Gray_Normal(GL.TRIANGLES, textureColor, linkedTextureData._image);
					}
					else if (isBoneWeightColor || isPhyVolumeWeightColor)
					{
						//가중치 색상
						_matBatch.BeginPass_Texture_VColor(GL.TRIANGLES, textureColor, linkedTextureData._image, vertexColorRatio, renderUnit.ShaderType, false, Vector4.zero);
					}
					else if(isMaskOnly)
					{
						//마스크 Only 메시라면 색상 무시
						textureColor.r = 0.5f;
						textureColor.g = 0.5f;
						textureColor.b = 0.5f;

						//Alpha는 0.5를 곱한다.
						textureColor.a *= 0.5f;

						//Vert Ratio에 1.0 곱해서 버텍스 색상 (위에서 정한 녹색) Tint가 100 적용되도록
						_matBatch.BeginPass_Texture_VColor(GL.TRIANGLES, textureColor, linkedTextureData._image, 1.0f, apPortrait.SHADER_TYPE.AlphaBlend, false, Vector4.zero);
					}
					else
					{
						//기본 색상
						_matBatch.BeginPass_Texture_VColor(GL.TRIANGLES, textureColor, linkedTextureData._image, 0.0f, renderUnit.ShaderType, false, Vector4.zero);
					}

					//삭제 21.5.18 : SetPass시 자동으로 설정한다.
					//_matBatch.SetClippingSize(_glScreenClippingSize);					
					//GL.Begin(GL.TRIANGLES);

					//------------------------------------------
					//apVertex vert0, vert1, vert2;
					apRenderVertex rVert0 = null, rVert1 = null, rVert2 = null;


					Vector3 pos_0 = Vector3.zero;
					Vector3 pos_1 = Vector3.zero;
					Vector3 pos_2 = Vector3.zero;


					Vector2 uv_0 = Vector2.zero;
					Vector2 uv_1 = Vector2.zero;
					Vector2 uv_2 = Vector2.zero;

					int iVert_0 = 0;
					int iVert_1 = 0;
					int iVert_2 = 0;

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

						rVert0 = renderUnit._renderVerts[iVert_0];
						rVert1 = renderUnit._renderVerts[iVert_1];
						rVert2 = renderUnit._renderVerts[iVert_2];

						pos_0.x = rVert0._pos_GL.x;
						pos_0.y = rVert0._pos_GL.y;
						pos_0.z = rVert0._vertex._zDepth * 0.5f;

						pos_1.x = rVert1._pos_GL.x;
						pos_1.y = rVert1._pos_GL.y;
						pos_1.z = rVert1._vertex._zDepth * 0.5f;

						pos_2.x = rVert2._pos_GL.x;
						pos_2.y = rVert2._pos_GL.y;
						pos_2.z = rVert2._vertex._zDepth * 0.5f;


						//uv_0 = mesh._vertexData[mesh._indexBuffer[i + 0]]._uv;
						//uv_1 = mesh._vertexData[mesh._indexBuffer[i + 1]]._uv;
						//uv_2 = mesh._vertexData[mesh._indexBuffer[i + 2]]._uv;

						uv_0 = rVert0._vertex._uv;
						uv_1 = rVert1._vertex._uv;
						uv_2 = rVert2._vertex._uv;

						switch (iVertColor)
						{
							case 1: //VolumeWeightColor
								color0 = GetWeightGrayscale(rVert0._renderWeightByTool);
								color1 = GetWeightGrayscale(rVert1._renderWeightByTool);
								color2 = GetWeightGrayscale(rVert2._renderWeightByTool);
								break;

							case 2: //PhysicsWeightColor
								color0 = GetWeightColor4(rVert0._renderWeightByTool);
								color1 = GetWeightColor4(rVert1._renderWeightByTool);
								color2 = GetWeightColor4(rVert2._renderWeightByTool);
								break;

							case 3: //BoneRigWeightColor
									//TODO : 본 리스트를 받아서 해야하는디..
								if (isBoneColor)
								{
									color0 = rVert0._renderColorByTool * (vertexColorRatio) + Color.black * (1.0f - vertexColorRatio);
									color1 = rVert1._renderColorByTool * (vertexColorRatio) + Color.black * (1.0f - vertexColorRatio);
									color2 = rVert2._renderColorByTool * (vertexColorRatio) + Color.black * (1.0f - vertexColorRatio);
								}
								else
								{
									color0 = _func_GetWeightColor3(rVert0._renderWeightByTool);
									color1 = _func_GetWeightColor3(rVert1._renderWeightByTool);
									color2 = _func_GetWeightColor3(rVert2._renderWeightByTool);
								}
								color0.a = 1.0f;
								color1.a = 1.0f;
								color2.a = 1.0f;
								
								break;
						}
						////------------------------------------------

						GL.Color(color0); GL.TexCoord(uv_0); GL.Vertex(pos_0); // 0
						GL.Color(color1); GL.TexCoord(uv_1); GL.Vertex(pos_1); // 1
						GL.Color(color2); GL.TexCoord(uv_2); GL.Vertex(pos_2); // 2

						// Back Side
						GL.Color(color2); GL.TexCoord(uv_2); GL.Vertex(pos_2); // 2
						GL.Color(color1); GL.TexCoord(uv_1); GL.Vertex(pos_1); // 1
						GL.Color(color0); GL.TexCoord(uv_0); GL.Vertex(pos_0); // 0

						////------------------------------------------
					}

					//삭제 21.5.18 : 자동으로 End 호출되도록 변경
					//GL.End();//전환 완료 > 외부에서 한번에 EndPass
					//GL.Flush();

				}

				//3. Edge를 렌더링하자
				if (isAllEdgeRender && nEdges > 0)
				{
					Vector2 pos0 = Vector2.zero, pos1 = Vector2.zero;

					apMeshEdge curEdge = null;

					apRenderVertex rVert0 = null, rVert1 = null;
					
					//변경 21.5.18
					_matBatch.BeginPass_Color(GL.LINES);
					
					for (int i = 0; i < nEdges; i++)
					{
						curEdge = meshEdges[i];

						rVert0 = renderUnit._renderVerts[curEdge._vert1._index];
						rVert1 = renderUnit._renderVerts[curEdge._vert2._index];

						pos0 = rVert0._pos_GL;
						pos1 = rVert1._pos_GL;

						DrawLineGL(pos0, pos1, editor._colorOption_MeshEdge, false);
					}

					//삭제 21.5.18
					//GL.End();//전환 완료 > 외부에서 한번에 EndPass
				}
				else if (isOutlineRender && nEdges > 0)
				{
					Vector2 pos0 = Vector2.zero, pos1 = Vector2.zero;
					apRenderVertex rVert0 = null, rVert1 = null;
					
					
					//변경 21.5.18
					_matBatch.BeginPass_Color(GL.TRIANGLES);
					//_matBatch.SetClippingSize(_glScreenClippingSize);
					//GL.Begin(GL.TRIANGLES);

					apMeshEdge curEdge = null;

					for (int i = 0; i < nEdges; i++)
					{
						curEdge = meshEdges[i];

						if (!curEdge._isOutline) { continue; }

						rVert0 = renderUnit._renderVerts[curEdge._vert1._index];
						rVert1 = renderUnit._renderVerts[curEdge._vert2._index];
							
						pos0 = rVert0._pos_GL;
						pos1 = rVert1._pos_GL;

						DrawBoldLineGL(pos0, pos1, 6.0f, editor._colorOption_Outline, false);
					}
				}

				if (isDrawTFBorderLine && nEdges > 0)
				{
					float minPosLocal_X = float.MaxValue;
					float maxPosLocal_X = float.MinValue;
					float minPosLocal_Y = float.MaxValue;
					float maxPosLocal_Y = float.MinValue;

					Vector2 pos0 = Vector2.zero, pos1 = Vector2.zero;
					apRenderVertex rVert0 = null, rVert1 = null;

					apMeshEdge curEdge = null;

					for (int i = 0; i < nEdges; i++)
					{
						curEdge = mesh._edges[i];
						
						if (!curEdge._isOutline) { continue; }

						rVert0 = renderUnit._renderVerts[curEdge._vert1._index];
						rVert1 = renderUnit._renderVerts[curEdge._vert2._index];

						pos0 = rVert0._pos_World;
						pos1 = rVert1._pos_World;

						if (rVert0._pos_LocalOnMesh.x < minPosLocal_X) { minPosLocal_X = rVert0._pos_LocalOnMesh.x; }
						if (rVert0._pos_LocalOnMesh.x > maxPosLocal_X) { maxPosLocal_X = rVert0._pos_LocalOnMesh.x; }
						if (rVert0._pos_LocalOnMesh.y < minPosLocal_Y) { minPosLocal_Y = rVert0._pos_LocalOnMesh.y; }
						if (rVert0._pos_LocalOnMesh.y > maxPosLocal_Y) { maxPosLocal_Y = rVert0._pos_LocalOnMesh.y; }

						if (rVert1._pos_LocalOnMesh.x < minPosLocal_X) { minPosLocal_X = rVert1._pos_LocalOnMesh.x; }
						if (rVert1._pos_LocalOnMesh.x > maxPosLocal_X) { maxPosLocal_X = rVert1._pos_LocalOnMesh.x; }
						if (rVert1._pos_LocalOnMesh.y < minPosLocal_Y) { minPosLocal_Y = rVert1._pos_LocalOnMesh.y; }
						if (rVert1._pos_LocalOnMesh.y > maxPosLocal_Y) { maxPosLocal_Y = rVert1._pos_LocalOnMesh.y; }
					}


					DrawTransformBorderFormOfRenderUnit(editor._colorOption_TransformBorder, minPosLocal_X, maxPosLocal_X, maxPosLocal_Y, minPosLocal_Y, renderUnit.WorldMatrix);
				}


				//3. 버텍스를 렌더링하자
				if (isVertexRender && nRenderVerts > 0)
				{
					//float halfPointSize = VERTEX_RENDER_SIZE * 0.5f;//삭제 v1.4.2 : 옵션에 따른 변수값을 바로 사용

					Vector2 posGL = Vector2.zero;
					bool isVertSelected = false;

					float vertAlphaRatio = renderRequest.Vertex == RenderTypeRequest.VISIBILITY.Transparent ? 0.3f : 1.0f;

					if (isAnyVertexSelected)
					{
						bool isDrawRigCircle = (isBoneWeightColor && isCircleRiggingVert);
						if(isDrawRigCircle)
						{
							//원형의 Rigging 버텍스
							_matBatch.BeginPass_RigCircleV2(GL.TRIANGLES);//변경 20.3.25 > V2
							
						}
						else
						{
							//기본 사각형 버텍스
							//_matBatch.BeginPass_Color(GL.TRIANGLES);//이전

							//변경 22.4.12
							_matBatch.BeginPass_VertexAndPin(GL.TRIANGLES);
						}
						
						//삭제 21.5.18
						//_matBatch.SetClippingSize(_glScreenClippingSize);
						//GL.Begin(GL.TRIANGLES);


						Color vColor = Color.black;
						//Color vColorOutline = _vertColor_Outline;
						for (int i = 0; i < nRenderVerts; i++)
						{
							vColor = editor._colorOption_VertColor_NotSelected;
							//vColorOutline = _vertColor_Outline;

							rVert = renderUnit._renderVerts[i];
							isVertSelected = false;

							if (isBoneWeightColor)
							{
								if (isBoneColor)
								{
									vColor = rVert._renderColorByTool;
								}
								else
								{
									vColor = _func_GetWeightColor3_Vert(rVert._renderWeightByTool);
								}
							}
							else if (isPhyVolumeWeightColor)
							{
								vColor = GetWeightColor4_Vert(rVert._renderWeightByTool);
							}

							if (_tmpSelectedVertices != null)
							{
								if (_tmpSelectedVertices.Contains(rVert))
								{
									//선택된 경우
									isVertSelected = true;

									if (isBoneWeightColor || isPhyVolumeWeightColor)
									{
										//vColorOutline = _vertColor_Outline_White;
									}
									else
									{
										vColor = editor._colorOption_VertColor_Selected;
									}
									
								}
								else if (isWeightedSelected && _tmpSelectedVertices_Weighted != null)
								{
									if (_tmpSelectedVertices_Weighted.Contains(rVert))
									{
										vColor = GetWeightColor2(rVert._renderWeightByTool, editor);
									}
								}
							}
							
							vColor.a *= vertAlphaRatio;

							posGL = rVert._pos_GL;
							if(isDrawRigCircle)
							{
								//V1
								//DrawRiggingRenderVert(rVert, vColorOutline, isBoneColor, isVertSelected);
								
								//V2
								float clickSize = DrawRiggingRenderVert_V2(rVert, isBoneColor, isVertSelected);
								
								AddCursorRect(mousePos, posGL, clickSize, clickSize, MouseCursor.MoveArrow);
							}
							else
							{
								//이전의 Box
								//if (isVertSelected || isBoneWeightColor || isPhyVolumeWeightColor)
								//{
								//	DrawBoxGL(posGL, pointSizeOutline, pointSizeOutline, vColorOutline, false, false);
								//}

								//DrawBoxGL(posGL, pointSize, pointSize, vColor, false, false);

								if (isVertSelected && (isBoneWeightColor || isPhyVolumeWeightColor))
								{
									//하얀색 외곽선으로 보인다.
									DrawVertex_WhiteOutline(ref posGL, _vertexRenderSizeHalf, ref vColor);
								}
								else
								{
									DrawVertex(ref posGL, _vertexRenderSizeHalf, ref vColor);
								}

								//선택 영역 : 박스 형태는 고정 크기이다.
								AddCursorRect(mousePos, posGL, 10, 10, MouseCursor.MoveArrow);
							}
							

							
						}

						//삭제 21.5.18
						//GL.End();//전환 완료 > 외부에서 한번에 EndPass
					}
					

					if (isPhyVolumeWeightColor && nRenderVerts > 0)
					{
						float pointSize_PhysicImg = 40.0f / _zoom;
						
						//추가적인 Vertex 이미지를 추가한다.
						//RenderVertex의 Param으로 이미지를 추가한다.

						//1. Physic Main
						//변경 21.5.18
						_matBatch.BeginPass_Texture_Normal(GL.TRIANGLES, _textureColor_Gray, _img_VertPhysicMain, apPortrait.SHADER_TYPE.AlphaBlend);
						
						for (int i = 0; i < nRenderVerts; i++)
						{
							rVert = renderUnit._renderVerts[i];
							if (rVert._renderParam == 1)
							{
								DrawTextureGL(_img_VertPhysicMain, rVert._pos_GL, pointSize_PhysicImg, pointSize_PhysicImg, _textureColor_Gray, 0.0f, false);
							}
						}

						//삭제 21.5.18
						//GL.End();//전환 완료 > 외부에서 한번에 EndPass
						


						//2. Physic Constraint
						//변경 21.5.18
						_matBatch.BeginPass_Texture_Normal(GL.TRIANGLES, _textureColor_Gray, _img_VertPhysicConstraint, apPortrait.SHADER_TYPE.AlphaBlend);
						//_matBatch.SetClippingSize(_glScreenClippingSize);
						//GL.Begin(GL.TRIANGLES);

						for (int i = 0; i < nRenderVerts; i++)
						{
							rVert = renderUnit._renderVerts[i];
							if (rVert._renderParam == 2)
							{
								DrawTextureGL(_img_VertPhysicConstraint, rVert._pos_GL, pointSize_PhysicImg, pointSize_PhysicImg, _textureColor_Gray, 0.0f, false);
							}
						}

						//삭제 21.5.18
						//GL.End();//전환 완료 > 외부에서 한번에 EndPass
						
					}
				}



				//추가 22.4.4 [v1.4.0]
				//4. 핀을 렌더링하자
				if(renderRequest.Pin != RenderTypeRequest.VISIBILITY.Hidden)
				{
					//핀
					int nPins = 0;
					apRenderPinGroup rPinGroup = renderUnit._renderPinGroup;
					if(rPinGroup != null)
					{
						nPins = rPinGroup.NumPins;
					}

					if(nPins > 0)
					{
						//선택된 RenderPin을 가져와야 한다.
						//apRenderPin selectedPin = null;
						_tmpSelectedPins.Clear();
						_tmpSelectedPins_Weighted.Clear();

						if (select != null)
						{
							int nModRenderPins = select.ModRenderPins_All != null ? select.ModRenderPins_All.Count : 0;
							if (nModRenderPins > 0)
							{
								List<apSelection.ModRenderPin> selectedMRPs = select.ModRenderPins_All;

								for (int i = 0; i < nModRenderPins; i++)
								{
									_tmpSelectedPins.Add(selectedMRPs[i]._renderPin);
								}

								if (isWeightedSelected)
								{
									List<apSelection.ModRenderPin> weightedMRPs = select.ModRenderPins_Weighted;
									int nWeightedMRP = weightedMRPs != null ? weightedMRPs.Count : 0;

									apSelection.ModRenderPin curMRP = null;
									for (int i = 0; i < nWeightedMRP; i++)
									{
										curMRP = weightedMRPs[i];
										curMRP._renderPin._renderWeightByTool = curMRP._pinWeightByTool;
										_tmpSelectedPins_Weighted.Add(curMRP._renderPin);
									}
								}
							}
						}
					
						apRenderPin curPin = null;

						//4-1. 핀 라인을 렌더링하자
						if (renderRequest.Pin == RenderTypeRequest.VISIBILITY.Shown)
						{
							apRenderPinCurve cur2NextCurve = null;

							_matBatch.BeginPass_Color(GL.TRIANGLES);


							Vector2 posLineA = Vector2.zero;
							Vector2 posLineB = Vector2.zero;
							int nCurvePoints = 20;

							Color curveLineColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
							Color curveLineSelected = new Color(1.0f, 0.7f, 0.0f, 1.0f);

							Color curCurveColor = Color.black;

							//4-1. 핀 라인을 렌더링하자 (커브)
							for (int iPin = 0; iPin < nPins; iPin++)
							{
								curPin = rPinGroup._pins[iPin];
								cur2NextCurve = curPin._nextRenderCurve;

								//Next로만 연결
								if (cur2NextCurve == null)
								{
									continue;
								}

								bool isSelected = false;
								if (_tmpSelectedPins != null)
								{
									if (_tmpSelectedPins.Contains(cur2NextCurve._prevPin) || _tmpSelectedPins.Contains(cur2NextCurve._nextPin))
									{
										isSelected = true;
									}
								}

								curCurveColor = isSelected ? curveLineSelected : curveLineColor;


								if (cur2NextCurve.IsLinear())
								{
									//두개의 핀 사이가 직선이라면
									posLineA = cur2NextCurve.GetCurvePosW(0.0f);
									posLineB = cur2NextCurve.GetCurvePosW(1.0f);
									DrawBoldLine(posLineA, posLineB, _pinLineThickness, curCurveColor, false);
								}
								else
								{
									//두개의 핀 사이가 커브라면
									for (int iLerp = 0; iLerp < nCurvePoints; iLerp++)
									{
										float lerpA = (float)iLerp / (float)nCurvePoints;
										float lerpB = (float)(iLerp + 1) / (float)nCurvePoints;

										posLineA = cur2NextCurve.GetCurvePosW(lerpA);
										posLineB = cur2NextCurve.GetCurvePosW(lerpB);
										DrawBoldLine(posLineA, posLineB, _pinLineThickness, curCurveColor, false);
									}
								}
							}
						}
						
						

						//4-2. 핀을 렌더링하자
						Color pinColor_None = new Color(1.0f, 1.0f, 0.0f, 1.0f);
						Color pinColor_Selected = new Color(1.0f, 0.15f, 0.5f, 1.0f);
						Color pinColor_Black = new Color(0.2f, 0.2f, 0.2f, 1.0f);
						
						if (renderRequest.Pin == RenderTypeRequest.VISIBILITY.Transparent)
						{
							pinColor_None.a = 0.4f;
							pinColor_Selected.a = 0.6f;
							pinColor_Black.a = 0.3f;
						}

						_matBatch.BeginPass_VertexAndPin(GL.TRIANGLES);

						//float halfPointSize = PIN_RENDER_SIZE * 0.5f;//삭제 v1.4.2 : 옵션에 따른 
						
						Vector2 posGL = Vector2.zero;
						Color vColor = Color.black;

						Vector2 cpPoint_Prev = Vector2.zero;
						Vector2 cpPoint_Next = Vector2.zero;

						for (int iPin = 0; iPin < nPins; iPin++)
						{
							curPin = rPinGroup._pins[iPin];

							vColor = pinColor_None;

							if (_tmpSelectedPins != null && _tmpSelectedPins.Contains(curPin))
							{
								vColor = pinColor_Selected;
							}
							else if (isWeightedSelected)
							{
								if (_tmpSelectedPins_Weighted.Contains(curPin))
								{
									vColor = GetWeightColor2(curPin._renderWeightByTool, editor);
								}
								else
								{
									//SoftSelection시의 선택되지 않은 핀들
									vColor = pinColor_Black;
								}
							}

							//v1.4.4 : Ref 이용
							//posGL = World2GL(curPin._pos_World);
							World2GL(ref posGL, ref curPin._pos_World);
							
							DrawPin(ref posGL, _pinRenderSizeHalf, ref vColor);

							AddCursorRect(mousePos, posGL, 10, 10, MouseCursor.MoveArrow);//이건 옵션 켤때만
						}
						

						//EndPass();
					}
				}
				//DrawText("<-[" + renderUnit.Name + "_" + renderUnit._debugID + "]", renderUnit.WorldMatrixWrap._pos + new Vector2(10.0f, 0.0f), 100, Color.yellow);
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
		}



		public static void DrawRenderUnit_Basic_ForExport(	apRenderUnit renderUnit,
															//bool isPixelPerfect,
															//Vector2 clipAreaPosGL_LB,
															//Vector2 posSizeGLPerPixel
															RenderTexture clippingMask,
															RenderTexture receiveMaskRT_1, apSendMaskData.MASK_OPERATION receiveMaskOp_1,
															RenderTexture receiveMaskRT_2, apSendMaskData.MASK_OPERATION receiveMaskOp_2,
															RenderTexture receiveMaskRT_3, apSendMaskData.MASK_OPERATION receiveMaskOp_3,
															RenderTexture receiveMaskRT_4, apSendMaskData.MASK_OPERATION receiveMaskOp_4,
															RenderTexture seeThroughRT, float seeThroughAlpha
															)
		{
			try
			{
				//0. 메시, 텍스쳐가 없을 때
				if (renderUnit == null || renderUnit._meshTransform == null || renderUnit._meshTransform._mesh == null)
				{
					return;
				}

				//이전
				//if (renderUnit._renderVerts.Count == 0) { return; }

				//변경 22.3.23 [v1.3.0]
				int nRenderVerts = renderUnit._renderVerts != null ? renderUnit._renderVerts.Length : 0;
				if(nRenderVerts == 0)
				{
					return;
				}

				Color textureColor = renderUnit._meshColor2X;
				apMesh mesh = renderUnit._meshTransform._mesh;
				bool isVisible = renderUnit._isVisible;

				if(!isVisible)
				{
					return;
				}

				//Mask Only 메시라면 렌더링 생략
				if(renderUnit._meshTransform._isMaskOnlyMesh)
				{
					return;
				}


				apTextureData linkedTextureData = mesh.LinkedTextureData;

				//추가 12.4 : Extra Option에 의해 Texture가 바귀었을 경우
				if(renderUnit.IsExtraTextureChanged)
				{
					linkedTextureData = renderUnit.ChangedExtraTextureData;
				}


				//if (mesh.LinkedTextureData == null)//이전
				if(linkedTextureData == null)
				{
					return;
				}

				if(linkedTextureData._image == null)
				{
					return;
				}


				//Clipping Mask가 있는가
				bool isAnyMaskReceived = false;
				if(clippingMask != null
					|| receiveMaskRT_1 != null
					|| receiveMaskRT_2 != null
					|| receiveMaskRT_3 != null
					|| receiveMaskRT_4 != null
					|| seeThroughRT != null)
				{
					isAnyMaskReceived = true;
				}

				//미리 GL 좌표를 연산하고, 나중에 중복 연산(World -> GL)을 하지 않도록 하자
				apRenderVertex rVert = null;
				for (int i = 0; i < nRenderVerts; i++)
				{
					rVert = renderUnit._renderVerts[i];
					
					//v1.4.4 : Ref 이용
					//rVert._pos_GL = World2GL(rVert._pos_World);
					World2GL(ref rVert._pos_GL, ref rVert._pos_World);
				}

				int nIndexBuffers = mesh._indexBuffer != null ? mesh._indexBuffer.Count : 0;
				int nVerts = mesh._vertexData != null ? mesh._vertexData.Count : 0;

				//2. 메시를 렌더링하자
				if (nIndexBuffers >= 3 && isVisible)
				{
					//------------------------------------------
					// Drawcall Batch를 했을때
					// Debug.Log("Texture Color : " + textureColor);
					//Color color0 = Color.black, color1 = Color.black, color2 = Color.black;
					Color color0 = Color.black;

					//int iVertColor = 0;
					color0 = Color.black;
					//color1 = Color.black;
					//color2 = Color.black;

					//변경 21.5.18 : Clipping Size가 바뀐다면 이전 Pass는 종료시키자
					_matBatch.EndPass();

					if(isAnyMaskReceived)
					{
						//if(seeThroughRT != null)
						//{
						//	Debug.Log("캡쳐 : ST 텍스쳐 받음 (" + seeThroughAlpha + ")");
						//}

						// [ 마스크를 받은 메시의 쉐이더로 렌더링 ]
						_matBatch.BeginPass_Clipped_WithMaskRT(
										GL.TRIANGLES,
										textureColor,
										linkedTextureData._image,
										0.0f,
										renderUnit.ShaderType,
										true, new Vector4(0, 0, 1, 1),
										clippingMask,
										receiveMaskRT_1, apSendMaskData.MaskOperationToFloatValue(receiveMaskOp_1),
										receiveMaskRT_2, apSendMaskData.MaskOperationToFloatValue(receiveMaskOp_2),
										receiveMaskRT_3, apSendMaskData.MaskOperationToFloatValue(receiveMaskOp_3),
										receiveMaskRT_4, apSendMaskData.MaskOperationToFloatValue(receiveMaskOp_4),
										seeThroughRT, seeThroughAlpha
										);
					}
					else
					{
						// [ 일반 메시의 쉐이더로 렌더링 ]
						_matBatch.BeginPass_Texture_VColor(	GL.TRIANGLES,
															textureColor,
															linkedTextureData._image,
															0.0f,
															renderUnit.ShaderType,
															true,
															new Vector4(0, 0, 1, 1));//<<이게 Clipped에도 적용
					}
						
					//_matBatch.SetClippingSize(new Vector4(0, 0, 1, 1));
					//GL.Begin(GL.TRIANGLES);


					//------------------------------------------
					//apVertex vert0, vert1, vert2;
					apRenderVertex rVert0 = null, rVert1 = null, rVert2 = null;

					Vector3 pos_0 = Vector3.zero;
					Vector3 pos_1 = Vector3.zero;
					Vector3 pos_2 = Vector3.zero;


					Vector2 uv_0 = Vector2.zero;
					Vector2 uv_1 = Vector2.zero;
					Vector2 uv_2 = Vector2.zero;

					GL.Color(color0);//추가. Color는 한번만 적용

					int index_0 = 0;
					int index_1 = 0;
					int index_2 = 0;


					for (int i = 0; i < nIndexBuffers; i += 3)
					{
						if (i + 2 >= nIndexBuffers) { break; }

						index_0 = mesh._indexBuffer[i + 0];
						index_1 = mesh._indexBuffer[i + 1];
						index_2 = mesh._indexBuffer[i + 2];

						if (index_0 >= nVerts ||
							index_1 >= nVerts ||
							index_2 >= nVerts)
						{
							break;
						}

						rVert0 = renderUnit._renderVerts[index_0];
						rVert1 = renderUnit._renderVerts[index_1];
						rVert2 = renderUnit._renderVerts[index_2];

						//Vector3 pos_0 = World2GL(rVert0._pos_World3);
						//Vector3 pos_1 = World2GL(rVert1._pos_World3);
						//Vector3 pos_2 = World2GL(rVert2._pos_World3);

						pos_0.x = rVert0._pos_GL.x;
						pos_0.y = rVert0._pos_GL.y;
						pos_0.z = rVert0._vertex._zDepth * 0.5f;

						pos_1.x = rVert1._pos_GL.x;
						pos_1.y = rVert1._pos_GL.y;
						pos_1.z = rVert1._vertex._zDepth * 0.5f;

						pos_2.x = rVert2._pos_GL.x;
						pos_2.y = rVert2._pos_GL.y;
						pos_2.z = rVert2._vertex._zDepth * 0.5f;


						uv_0 = mesh._vertexData[index_0]._uv;
						uv_1 = mesh._vertexData[index_1]._uv;
						uv_2 = mesh._vertexData[index_2]._uv;


						////------------------------------------------
						////v1.4.6 : Pixel Perfect 옵션
						//if(isPixelPerfect)
						//{	
						//	ConvertPixelPerfectPos(ref pos_0, ref clipAreaPosGL_LB, ref posSizeGLPerPixel);
						//	ConvertPixelPerfectPos(ref pos_1, ref clipAreaPosGL_LB, ref posSizeGLPerPixel);
						//	ConvertPixelPerfectPos(ref pos_2, ref clipAreaPosGL_LB, ref posSizeGLPerPixel);
						//}

						GL.TexCoord(uv_0); GL.Vertex(pos_0); // 0
						GL.TexCoord(uv_1); GL.Vertex(pos_1); // 1
						GL.TexCoord(uv_2); GL.Vertex(pos_2); // 2

						// Back Side
						GL.TexCoord(uv_2); GL.Vertex(pos_2); // 2
						GL.TexCoord(uv_1); GL.Vertex(pos_1); // 1
						GL.TexCoord(uv_0); GL.Vertex(pos_0); // 0

						////------------------------------------------
					}
					
					//삭제 21.5.18
					//GL.End();//전환 완료

					_matBatch.EndPass();
					_matBatch.SetClippingSize(_glScreenClippingSize);
					
				}
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
		}

		/// <summary>
		/// Export용으로 Alpha채널을 White로 렌더링하는 함수.
		/// Mask를 받는 메시도 자동으로 처리한다.
		/// </summary>
		/// <param name="renderUnit"></param>
		public static void DrawRenderUnit_Basic_Alpha2White_ForExport(	apRenderUnit renderUnit,
																		////v1.4.6 : Pixel Perfect 위치 계산
																		//bool isPixelPerfect,
																		//Vector2 clipAreaPosGL_LB,
																		//Vector2 posSizeGLPerPixel
																		RenderTexture clippingMask,
																		RenderTexture receiveMaskRT_1, apSendMaskData.MASK_OPERATION receiveMaskOp_1,
																		RenderTexture receiveMaskRT_2, apSendMaskData.MASK_OPERATION receiveMaskOp_2,
																		RenderTexture receiveMaskRT_3, apSendMaskData.MASK_OPERATION receiveMaskOp_3,
																		RenderTexture receiveMaskRT_4, apSendMaskData.MASK_OPERATION receiveMaskOp_4
																		)
		{
			try
			{
				//0. 메시, 텍스쳐가 없을 때
				if (renderUnit == null || renderUnit._meshTransform == null || renderUnit._meshTransform._mesh == null)
				{
					return;
				}

				//이전
				//if (renderUnit._renderVerts.Count == 0) { return; }

				//변경 22.3.23 [v1.4.0]
				int nRenderVerts = renderUnit._renderVerts != null ? renderUnit._renderVerts.Length : 0;
				if(nRenderVerts == 0)
				{
					return;
				}


				Color textureColor = renderUnit._meshColor2X;
				apMesh mesh = renderUnit._meshTransform._mesh;
				bool isVisible = renderUnit._isVisible;

				if(!isVisible)
				{
					return;
				}

				//Mask Only 메시라면 렌더링 생략
				if(renderUnit._meshTransform._isMaskOnlyMesh)
				{
					return;
				}

				apTextureData linkedTextureData = mesh.LinkedTextureData;

				//추가 12.4 : Extra Option에 의해 Texture가 바귀었을 경우
				if(renderUnit.IsExtraTextureChanged)
				{
					linkedTextureData = renderUnit.ChangedExtraTextureData;
				}

				//if (mesh.LinkedTextureData == null)//이전
				if(linkedTextureData == null)
				{
					return;
				}


				//미리 GL 좌표를 연산하고, 나중에 중복 연산(World -> GL)을 하지 않도록 하자
				apRenderVertex rVert = null;
				for (int i = 0; i < nRenderVerts; i++)
				{
					rVert = renderUnit._renderVerts[i];

					//v1.4.4 : Ref 이용
					//rVert._pos_GL = World2GL(rVert._pos_World);
					World2GL(ref rVert._pos_GL, ref rVert._pos_World);
				}


				int nIndexBuffers = mesh._indexBuffer != null ? mesh._indexBuffer.Count : 0;
				int nVerts = mesh._vertexData != null ? mesh._vertexData.Count : 0;


				//2. 메시를 렌더링하자
				if(nIndexBuffers < 3)
				{
					return;
				}
				//------------------------------------------
				// Drawcall Batch를 했을때
				// Debug.Log("Texture Color : " + textureColor);
				Color color0 = Color.black;

				color0 = Color.black;
					
				//변경 21.5.18
				//Clipping Size를 바꾼다면, 이전의 Pass를 종료시켜야 한다.
				_matBatch.EndPass();
				_matBatch.BeginPass_Alpha2White(	GL.TRIANGLES,
													textureColor,
													linkedTextureData._image,
													new Vector4(0, 0, 1, 1),
													clippingMask,
													receiveMaskRT_1, receiveMaskOp_1,
													receiveMaskRT_2, receiveMaskOp_2,
													receiveMaskRT_3, receiveMaskOp_3,
													receiveMaskRT_4, receiveMaskOp_4
													);//<<Shader를 Alpha2White로 한다. + ExtraOption
				//GL.Begin(GL.TRIANGLES);

				//------------------------------------------
				//apVertex vert0, vert1, vert2;
				apRenderVertex rVert0 = null, rVert1 = null, rVert2 = null;

				Vector3 pos_0 = Vector3.zero;
				Vector3 pos_1 = Vector3.zero;
				Vector3 pos_2 = Vector3.zero;


				Vector2 uv_0 = Vector2.zero;
				Vector2 uv_1 = Vector2.zero;
				Vector2 uv_2 = Vector2.zero;

				int index_0 = 0;
				int index_1 = 0;
				int index_2 = 0;

				//색상은 한번만 적용하자
				GL.Color(color0);

				for (int i = 0; i < nIndexBuffers; i += 3)
				{
					if (i + 2 >= nIndexBuffers) { break; }

					index_0 = mesh._indexBuffer[i + 0];
					index_1 = mesh._indexBuffer[i + 1];
					index_2 = mesh._indexBuffer[i + 2];

					if (index_0 >= nVerts ||
						index_1 >= nVerts ||
						index_2 >= nVerts)
					{
						break;
					}

					rVert0 = renderUnit._renderVerts[index_0];
					rVert1 = renderUnit._renderVerts[index_1];
					rVert2 = renderUnit._renderVerts[index_2];

					//Vector3 pos_0 = World2GL(rVert0._pos_World3);
					//Vector3 pos_1 = World2GL(rVert1._pos_World3);
					//Vector3 pos_2 = World2GL(rVert2._pos_World3);

					pos_0.x = rVert0._pos_GL.x;
					pos_0.y = rVert0._pos_GL.y;
					pos_0.z = rVert0._vertex._zDepth * 0.5f;

					pos_1.x = rVert1._pos_GL.x;
					pos_1.y = rVert1._pos_GL.y;
					pos_1.z = rVert1._vertex._zDepth * 0.5f;

					pos_2.x = rVert2._pos_GL.x;
					pos_2.y = rVert2._pos_GL.y;
					pos_2.z = rVert2._vertex._zDepth * 0.5f;


					uv_0 = mesh._vertexData[index_0]._uv;
					uv_1 = mesh._vertexData[index_1]._uv;
					uv_2 = mesh._vertexData[index_2]._uv;


					////------------------------------------------

					//v1.4.6 추가 - Pixel Perfect 옵션
					//if(isPixelPerfect)
					//{
					//	ConvertPixelPerfectPos(ref pos_0, ref clipAreaPosGL_LB, ref posSizeGLPerPixel);
					//	ConvertPixelPerfectPos(ref pos_1, ref clipAreaPosGL_LB, ref posSizeGLPerPixel);
					//	ConvertPixelPerfectPos(ref pos_2, ref clipAreaPosGL_LB, ref posSizeGLPerPixel);
					//}

					GL.TexCoord(uv_0); GL.Vertex(pos_0); // 0
					GL.TexCoord(uv_1); GL.Vertex(pos_1); // 1
					GL.TexCoord(uv_2); GL.Vertex(pos_2); // 2

					// Back Side
					GL.TexCoord(uv_2); GL.Vertex(pos_2); // 2
					GL.TexCoord(uv_1); GL.Vertex(pos_1); // 1
					GL.TexCoord(uv_0); GL.Vertex(pos_0); // 0
						

					////------------------------------------------
				}

				//삭제 21.5.18
				//GL.End();//<전환 완료>

				//Clipped Size를 복구하고 Pass를 강제로 종료한다.					
				_matBatch.EndPass();
				_matBatch.SetClippingSize(_glScreenClippingSize);
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
		}

		private static void ConvertPixelPerfectPos(ref Vector3 pos3, ref Vector2 areaPosGL_LB, ref Vector2 unitGLPerPixel)
		{
			//Vector3 prevPos = pos3;
			//pos3.x = Mathf.Round(pos3.x);
			//pos3.y = Mathf.Round(pos3.y);
			//pos3.z = Mathf.Round(pos3.z);

			pos3.x = (Mathf.Floor((pos3.x - areaPosGL_LB.x) * unitGLPerPixel.x) / unitGLPerPixel.x) + areaPosGL_LB.x;
			pos3.y = (Mathf.Floor((pos3.y - areaPosGL_LB.y) * unitGLPerPixel.y) / unitGLPerPixel.y) + areaPosGL_LB.y;

			//Debug.Log("Pixel Perfect : " + prevPos + " > " + pos3);
		}
		private static void ConvertPixelPerfectPos2(ref Vector2 pos2 /*, in Vector2 areaPosGL_LB, in Vector2 unitGLPerPixel*/)
		{
			//pos2.x = Mathf.Round(pos2.x);
			//pos2.y = Mathf.Round(pos2.y);
			pos2.x = Mathf.Floor(pos2.x);
			pos2.y = Mathf.Floor(pos2.y);

			//pos2.x = (Mathf.Round((pos2.x - areaPosGL_LB.x) * unitGLPerPixel.x) / unitGLPerPixel.x) + areaPosGL_LB.x;
			//pos2.y = (Mathf.Round((pos2.y - areaPosGL_LB.y) * unitGLPerPixel.y) / unitGLPerPixel.y) + areaPosGL_LB.y;
		}


		//---------------------------------------------------------------------------------------
		// Draw Render Unit : Clipping
		// RenderType은 MeshColor에 영향을 주는 것들만 허용한다.
		//---------------------------------------------------------------------------------------
		/// <summary>
		/// v1.6.0 : 마스크 렌더링 기능
		/// apMaskRT를 대상으로 마스크를 렌더링한다.		
		/// </summary>
		/// <param name="renderUnit"></param>
		/// <param name="targetMaskRT"></param>
		/// <param name="shaderType"></param>
		/// <param name="editor"></param>
		public static void DrawRenderUnit_ToMaskRT(	apRenderUnit renderUnit,
													apMaskRT targetMaskRT,
													apSendMaskData.RT_SHADER_TYPE shaderType,
													apEditor editor)
		{
			try
			{
				//0. 메시, 텍스쳐가 없을 때
				if (renderUnit == null
					|| renderUnit._meshTransform == null
					|| renderUnit._meshTransform._mesh == null)
				{
					return;
				}

				int nRenderVerts = renderUnit._renderVerts != null ? renderUnit._renderVerts.Length : 0;
				if(nRenderVerts == 0)
				{
					return;
				}

				Color textureColor = Color.black;
				apTransform_Mesh meshTF = renderUnit._meshTransform;
				apMesh mesh = meshTF._mesh;

				bool isAlphaMask = false;

				switch (shaderType)
				{
					case apSendMaskData.RT_SHADER_TYPE.AlphaMask:
						{
							isAlphaMask = true;
							textureColor = renderUnit._meshColor2X;
						}
						break;

					case apSendMaskData.RT_SHADER_TYPE.MainTextureWithColor:
						{
							isAlphaMask = false;
							textureColor = renderUnit._meshColor2X;
						}
						break;

					case apSendMaskData.RT_SHADER_TYPE.MainTextureOnly:
						{
							isAlphaMask = false;
							textureColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);
						}
						break;
				}

				//쉐이더 타입에 상관없이 에디터에서 현재 보여지지 않는 상태라면 alpha를 0으로 설정한다.
				//단, 렌더링을 안하진 않는다.
				if(!renderUnit._isVisible)
				{
					textureColor.a = 0.0f;
				}

				apTextureData linkedTextureData = mesh.LinkedTextureData;

				//추가 12.4 : Extra Option에 의해 Texture가 바뀌었을 경우
				if(renderUnit.IsExtraTextureChanged)
				{
					linkedTextureData = renderUnit.ChangedExtraTextureData;
				}

				if(linkedTextureData == null)
				{
					return;
				}
				
				int nIndexBuffers = mesh._indexBuffer != null ? mesh._indexBuffer.Count : 0;
				int nVerts = mesh._vertexData != null ? mesh._vertexData.Count : 0;

				//렌더링 방식은 Mesh (with Color) 또는 Vertex / Outline이 있다.

				//1. Parent의 기본 렌더링을 하자
				//+2. Parent의 마스크를 렌더링하자
				if (nIndexBuffers < 3)
				{
					return;
				}

				//Chained - 마스크 생성 렌더링이라도 다른 클리핑 마스크의 영향을 받았을 수 있다.
				bool isChainClipped = false;

				apMaskRT clipMaskRT = null;

				// < 임의의 마스크 >
				apMaskRT recvMaskRT_Ch1 = null;
				apMaskRT recvMaskRT_Ch2 = null;
				apMaskRT recvMaskRT_Ch3 = null;
				apMaskRT recvMaskRT_Ch4 = null;

				apSendMaskData.ReceivePropertySet recvPropSet_Ch1 = null;
				apSendMaskData.ReceivePropertySet recvPropSet_Ch2 = null;
				apSendMaskData.ReceivePropertySet recvPropSet_Ch3 = null;
				apSendMaskData.ReceivePropertySet recvPropSet_Ch4 = null;

				apMaskRT recvSeeThroughRT = null;
				apSendMaskData.ReceivePropertySet recvSeeThroughPropSet = null;

				if(meshTF._isClipping_Child)
				{
					//클리핑 자식 메시라면
					//클리핑 부모 메시를 찾고 마스크 정보를 받아온다.
					if(meshTF._clipParentMeshTransform != null
						&& meshTF._clipParentMeshTransform._linkedRenderUnit != null)
					{
						clipMaskRT = editor.RenderTex.GetRT_ClippingParent(meshTF._clipParentMeshTransform);
						isChainClipped = true;
					}
				}

				int nMaskInfos = meshTF._linkedReceivedMasks != null ? meshTF._linkedReceivedMasks.Count : 0;
				if(nMaskInfos > 0)
				{
					//마스크를 수신하는 정보가 있다
					//하나씩 보고 렌더링에 참조할만한게 있는지 확인하자
					apMaskLinkInfo curMaskInfo = null;
					apSendMaskData sendMaskData = null;
					apTransform_Mesh clipParentMeshTF = null;
					apSendMaskData.ReceivePropertySet propSet = null;

					apMaskRT maskRT = null;

					for (int iMaskInfo = 0; iMaskInfo < nMaskInfos; iMaskInfo++)
					{
						curMaskInfo = meshTF._linkedReceivedMasks[iMaskInfo];

						sendMaskData = curMaskInfo._parentMaskData;
						clipParentMeshTF = curMaskInfo._parentMaskMeshTF;
						
						if(sendMaskData == null || clipParentMeshTF == null)
						{
							continue;
						}

						//설정에 따른 Mask를 가져온다.
						if(sendMaskData._isRTShared)
						{
							//공유 RT
							maskRT = editor.RenderTex.GetRT_Shared(sendMaskData._rtShaderType, sendMaskData._sharedRTID);
						}
						else
						{
							//개별 RT
							maskRT = editor.RenderTex.GetRT_PerMeshTF(clipParentMeshTF, sendMaskData._rtShaderType);
						}
						
						

						int nPropSets = sendMaskData._propertySets != null ? sendMaskData._propertySets.Count : 0;
						if(nPropSets == 0)
						{
							continue;
						}

						for (int iPropSet = 0; iPropSet < nPropSets; iPropSet++)
						{
							propSet = sendMaskData._propertySets[iPropSet];
							if(propSet._preset == apSendMaskData.SHADER_PROP_PRESET.AlphaMaskPreset)
							{
								// [ Alpha Mask 프리셋 ]
								//채널을 확인하여 전달할 데이터에 할당을 하자
								switch (propSet._reservedChannel)
								{
									case apSendMaskData.SHADER_PROP_RESERVED_CHANNEL.Channel_1:
										recvPropSet_Ch1 = propSet;
										recvMaskRT_Ch1 = maskRT;
										break;

									case apSendMaskData.SHADER_PROP_RESERVED_CHANNEL.Channel_2:
										recvPropSet_Ch2 = propSet;
										recvMaskRT_Ch2 = maskRT;
										break;

									case apSendMaskData.SHADER_PROP_RESERVED_CHANNEL.Channel_3:
										recvPropSet_Ch3 = propSet;
										recvMaskRT_Ch3 = maskRT;
										break;

									case apSendMaskData.SHADER_PROP_RESERVED_CHANNEL.Channel_4:
										recvPropSet_Ch4 = propSet;
										recvMaskRT_Ch4 = maskRT;
										break;
								}

								isChainClipped = true;
							}
							else if(propSet._preset == apSendMaskData.SHADER_PROP_PRESET.SeeThroughPreset)
							{
								// [ See-Through 프리셋 ]
								recvSeeThroughRT = maskRT;
								recvSeeThroughPropSet = propSet;

								isChainClipped = true;
							}
						}
					}
				}




				apRenderVertex rVert0 = null, rVert1 = null, rVert2 = null;

				Color vertexChannelColor = Color.black;
				//Color vColor0 = Color.black, vColor1 = Color.black, vColor2 = Color.black;

				Vector3 pos_0 = Vector3.zero;
				Vector3 pos_1 = Vector3.zero;
				Vector3 pos_2 = Vector3.zero;
		
				Vector2 uv_0 = Vector2.zero;
				Vector2 uv_1 = Vector2.zero;
				Vector2 uv_2 = Vector2.zero;

				int index_0 = 0;
				int index_1 = 0;
				int index_2 = 0;

				
				RenderTexture.active = null;

				//마스크 렌더링을 하자
				_matBatch.BeginPass_MaskRT(	GL.TRIANGLES,
											textureColor,
											linkedTextureData._image,
											renderUnit.ShaderType,//SendData와 별개의 Shader Type (AB, Add, SoftAdd 등)
											isAlphaMask,
											targetMaskRT,
											
											//클리핑-체인
											isChainClipped,
											clipMaskRT,
											recvMaskRT_Ch1, recvPropSet_Ch1,
											recvMaskRT_Ch2, recvPropSet_Ch2,
											recvMaskRT_Ch3, recvPropSet_Ch3,
											recvMaskRT_Ch4, recvPropSet_Ch4,
											recvSeeThroughRT, recvSeeThroughPropSet
											);
				GL.Color(Color.black);

				for (int i = 0; i < nIndexBuffers; i += 3)
				{
					if (i + 2 >= nIndexBuffers) { break; }

					index_0 = mesh._indexBuffer[i + 0];
					index_1 = mesh._indexBuffer[i + 1];
					index_2 = mesh._indexBuffer[i + 2];

					if (index_0 >= nVerts ||
						index_1 >= nVerts ||
						index_2 >= nVerts)
					{
						break;
					}

					rVert0 = renderUnit._renderVerts[index_0];
					rVert1 = renderUnit._renderVerts[index_1];
					rVert2 = renderUnit._renderVerts[index_2];

					//변경 v1.4.4 : Ref 이용
					World2GL_Vec3(ref pos_0, ref rVert0._pos_World);
					World2GL_Vec3(ref pos_1, ref rVert1._pos_World);
					World2GL_Vec3(ref pos_2, ref rVert2._pos_World);
						
					// uv_0 = mesh._vertexData[index_0]._uv;
					// uv_1 = mesh._vertexData[index_1]._uv;
					// uv_2 = mesh._vertexData[index_2]._uv;

					uv_0 = rVert0._vertex._uv;
					uv_1 = rVert1._vertex._uv;
					uv_2 = rVert2._vertex._uv;
						
					GL.TexCoord(uv_0); GL.Vertex(pos_0); // 0
					GL.TexCoord(uv_1); GL.Vertex(pos_1); // 1
					GL.TexCoord(uv_2); GL.Vertex(pos_2); // 2

					// Back Side
					GL.TexCoord(uv_2); GL.Vertex(pos_2); // 2
					GL.TexCoord(uv_1); GL.Vertex(pos_1); // 1
					GL.TexCoord(uv_0); GL.Vertex(pos_0); // 0
				}

				//------------------------------------------

				//삭제 21.5.18
				//GL.End();//<전환 완료>
				//Clipping Pass마다 Pass 한번씩 종료
				_matBatch.EndPass();

				//GL로 부터 RT를 해제한다.
				_matBatch.DeactiveRenderTexture();

			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
		}


		#region [미사용 코드] v1.6.0부터는 사용되지 않음. Clipping Parent가 Child를 렌더링하는 이전 방식은 사용되지 않는다.
		//[Obsolete("v1.6.0부터는 사용되지 않음. Clipping Parent가 Child를 렌더링하는 이전 방식은 사용되지 않는다.")]
		//public static void DrawRenderUnit_ClippingParent_Renew(	apRenderUnit renderUnit,

		//														//RENDER_TYPE renderType,			//이전
		//														RenderTypeRequest renderRequest,	//변경 22.3.3 (v1.4.0)


		//														List<apTransform_Mesh.ClipMeshSet> childClippedSet,
		//														//List<apTransform_Mesh> childMeshTransforms, 
		//														//List<apRenderUnit> childRenderUnits, 
		//														apVertexController vertexController,
		//														apEditor editor,
		//														apSelection select,
		//														RenderTexture externalRenderTexture = null)
		//{
		//	//렌더링 순서
		//	//Parent - 기본
		//	//Parent - Mask
		//	//(For) Child - Clipped
		//	//Release RenderMask
		//	try
		//	{
		//		//0. 메시, 텍스쳐가 없을 때
		//		if (renderUnit == null || renderUnit._meshTransform == null || renderUnit._meshTransform._mesh == null)
		//		{
		//			return;
		//		}

		//		//이전
		//		//if (renderUnit._renderVerts.Count == 0)
		//		//{
		//		//	return;
		//		//}

		//		int nRenderVerts = renderUnit._renderVerts != null ? renderUnit._renderVerts.Length : 0;
		//		if(nRenderVerts == 0)
		//		{
		//			return;
		//		}


		//		Color textureColor = renderUnit._meshColor2X;
		//		apMesh mesh = renderUnit._meshTransform._mesh;


		//		apTextureData linkedTextureData = mesh.LinkedTextureData;

		//		//추가 12.4 : Extra Option에 의해 Texture가 바귀었을 경우
		//		if(renderUnit.IsExtraTextureChanged)
		//		{
		//			linkedTextureData = renderUnit.ChangedExtraTextureData;
		//		}

		//		//if (mesh.LinkedTextureData == null)//이전
		//		if(linkedTextureData == null)
		//		{
		//			return;
		//		}



		//		int nClipMeshes = childClippedSet.Count;

		//		//이전
		//		//bool isBoneWeightColor = (int)(renderType & RENDER_TYPE.BoneRigWeightColor) != 0;
		//		//bool isPhyVolumeWeightColor = (renderType & RENDER_TYPE.PhysicsWeightColor) != 0 || (renderType & RENDER_TYPE.VolumeWeightColor) != 0;

		//		//변경 22.3.3 (v1.4.0)
		//		bool isBoneWeightColor = renderRequest.BoneRigWeightColor;
		//		bool isPhyVolumeWeightColor = renderRequest.PhysicsWeightColor || renderRequest.VolumeWeightColor;

		//		int iVertColor = 0;

		//		//if ((renderType & RENDER_TYPE.VolumeWeightColor) != 0)	//이전
		//		if (renderRequest.VolumeWeightColor)						//변경 22.3.3
		//		{
		//			iVertColor = 1;
		//		}
		//		//else if ((renderType & RENDER_TYPE.PhysicsWeightColor) != 0)	//이전
		//		else if (renderRequest.PhysicsWeightColor)						//변경 22.3.3
		//		{
		//			iVertColor = 2;
		//		}
		//		//else if ((renderType & RENDER_TYPE.BoneRigWeightColor) != 0)	//이전
		//		else if (renderRequest.BoneRigWeightColor)						//변경 22.3.3
		//		{
		//			iVertColor = 3;
		//		}
		//		else
		//		{
		//			iVertColor = 0;
		//		}

		//		bool isBoneColor = false;
		//		//bool isCircleRiggingVert = editor._rigViewOption_CircleVert;
		//		float vertexColorRatio = 0.0f;

		//		//bool isToneColor = (int)(renderType & RENDER_TYPE.ToneColor) != 0;	//이전
		//		bool isToneColor = renderRequest.ToneColor;								//변경 22.3.3

		//		if (select != null)
		//		{
		//			//isBoneColor = select._rigEdit_isBoneColorView;//이전
		//			isBoneColor = editor._rigViewOption_BoneColor;//변경 19.7.31

		//			if (isBoneWeightColor)
		//			{
		//				//if (select._rigEdit_viewMode == apSelection.RIGGING_EDIT_VIEW_MODE.WeightColorOnly)
		//				if(editor._rigViewOption_WeightOnly)
		//				{
		//					vertexColorRatio = 1.0f;
		//				}
		//				else
		//				{
		//					vertexColorRatio = 0.5f;
		//				}
		//			}
		//			else if (isPhyVolumeWeightColor)
		//			{
		//				vertexColorRatio = 0.7f;
		//			}
		//		}

		//		//추가 21.2.16 : 선택되지 않은 RenderUnit은 회색으로 표시
		//		bool isNotEditedGrayColor_Parent = false;

		//		if (editor._exModObjOption_ShowGray &&
		//			(renderUnit._exCalculateMode == apRenderUnit.EX_CALCULATE.Disabled_NotEdit ||
		//				renderUnit._exCalculateMode == apRenderUnit.EX_CALCULATE.Disabled_ExRun))
		//		{
		//			//선택되지 않은 건 Gray 색상으로 표시하고자 할 때
		//			isNotEditedGrayColor_Parent = true;
		//		}


		//		int nIndexBuffers = mesh._indexBuffer != null ? mesh._indexBuffer.Count : 0;
		//		int nVerts = mesh._vertexData != null ? mesh._vertexData.Count : 0;


		//		//렌더링 방식은 Mesh (with Color) 또는 Vertex / Outline이 있다.

		//		//1. Parent의 기본 렌더링을 하자
		//		//+2. Parent의 마스크를 렌더링하자
		//		if (nIndexBuffers < 3)
		//		{
		//			return;
		//		}

		//		apRenderVertex rVert0 = null, rVert1 = null, rVert2 = null;

		//		Color vertexChannelColor = Color.black;
		//		Color vColor0 = Color.black, vColor1 = Color.black, vColor2 = Color.black;

		//		//Vector2 posGL_0 = Vector2.zero;
		//		//Vector2 posGL_1 = Vector2.zero;
		//		//Vector2 posGL_2 = Vector2.zero;

		//		Vector3 pos_0 = Vector3.zero;
		//		Vector3 pos_1 = Vector3.zero;
		//		Vector3 pos_2 = Vector3.zero;

		//		Vector2 uv_0 = Vector2.zero;
		//		Vector2 uv_1 = Vector2.zero;
		//		Vector2 uv_2 = Vector2.zero;

		//		int index_0 = 0;
		//		int index_1 = 0;
		//		int index_2 = 0;


		//		RenderTexture.active = null;

		//		//Pass 0 : 일반 렌더링
		//		//Pass 1 : 클리핑 마스크 (Render Texture) 렌더링
		//		for (int iPass = 0; iPass < 2; iPass++)
		//		{
		//			bool isRenderTexture = false;
		//			if (iPass == 1)
		//			{
		//				isRenderTexture = true;
		//			}
		//			if(isToneColor)
		//			{
		//				// ToneColor Mask
		//				_matBatch.BeginPass_Mask_ToneColor(GL.TRIANGLES, _toneColor, linkedTextureData._image, isRenderTexture);

		//			}
		//			else if(isNotEditedGrayColor_Parent)
		//			{
		//				_matBatch.BeginPass_Mask_Gray(GL.TRIANGLES, textureColor, linkedTextureData._image, isRenderTexture);
		//			}
		//			else
		//			{
		//				//일반적인 Mask
		//				_matBatch.BeginPass_Mask(GL.TRIANGLES, textureColor, linkedTextureData._image, vertexColorRatio, renderUnit.ShaderType, isRenderTexture, false, Vector4.zero);
		//			}

		//			//삭제 21.5.18
		//			//_matBatch.SetClippingSize(_glScreenClippingSize);
		//			//GL.Begin(GL.TRIANGLES);

		//			//------------------------------------------
		//			for (int i = 0; i < nIndexBuffers; i += 3)
		//			{
		//				if (i + 2 >= nIndexBuffers) { break; }

		//				index_0 = mesh._indexBuffer[i + 0];
		//				index_1 = mesh._indexBuffer[i + 1];
		//				index_2 = mesh._indexBuffer[i + 2];

		//				if (index_0 >= nVerts ||
		//					index_1 >= nVerts ||
		//					index_2 >= nVerts)
		//				{
		//					break;
		//				}

		//				rVert0 = renderUnit._renderVerts[index_0];
		//				rVert1 = renderUnit._renderVerts[index_1];
		//				rVert2 = renderUnit._renderVerts[index_2];

		//				vColor0 = Color.black;
		//				vColor1 = Color.black;
		//				vColor2 = Color.black;


		//				switch (iVertColor)
		//				{
		//					case 1: //VolumeWeightColor
		//						vColor0 = GetWeightGrayscale(rVert0._renderWeightByTool);
		//						vColor1 = GetWeightGrayscale(rVert1._renderWeightByTool);
		//						vColor2 = GetWeightGrayscale(rVert2._renderWeightByTool);
		//						break;

		//					case 2: //PhysicsWeightColor
		//						vColor0 = GetWeightColor4(rVert0._renderWeightByTool);
		//						vColor1 = GetWeightColor4(rVert1._renderWeightByTool);
		//						vColor2 = GetWeightColor4(rVert2._renderWeightByTool);
		//						break;

		//					case 3: //BoneRigWeightColor
		//						if (isBoneColor)
		//						{
		//							vColor0 = rVert0._renderColorByTool * (vertexColorRatio) + Color.black * (1.0f - vertexColorRatio);
		//							vColor1 = rVert1._renderColorByTool * (vertexColorRatio) + Color.black * (1.0f - vertexColorRatio);
		//							vColor2 = rVert2._renderColorByTool * (vertexColorRatio) + Color.black * (1.0f - vertexColorRatio);
		//						}
		//						else
		//						{
		//							vColor0 = _func_GetWeightColor3(rVert0._renderWeightByTool);
		//							vColor1 = _func_GetWeightColor3(rVert1._renderWeightByTool);
		//							vColor2 = _func_GetWeightColor3(rVert2._renderWeightByTool);
		//						}
		//						vColor0.a = 1.0f;
		//						vColor1.a = 1.0f;
		//						vColor2.a = 1.0f;
		//						break;
		//				}

		//				//변경 v1.4.4 : Ref 이요
		//				World2GL_Vec3(ref pos_0, ref rVert0._pos_World);
		//				World2GL_Vec3(ref pos_1, ref rVert1._pos_World);
		//				World2GL_Vec3(ref pos_2, ref rVert2._pos_World);

		//				// uv_0 = mesh._vertexData[index_0]._uv;
		//				// uv_1 = mesh._vertexData[index_1]._uv;
		//				// uv_2 = mesh._vertexData[index_2]._uv;

		//				uv_0 = rVert0._vertex._uv;
		//				uv_1 = rVert1._vertex._uv;
		//				uv_2 = rVert2._vertex._uv;

		//				GL.Color(vColor0); GL.TexCoord(uv_0); GL.Vertex(pos_0); // 0
		//				GL.Color(vColor1); GL.TexCoord(uv_1); GL.Vertex(pos_1); // 1
		//				GL.Color(vColor2); GL.TexCoord(uv_2); GL.Vertex(pos_2); // 2

		//				// Back Side
		//				GL.Color(vColor2); GL.TexCoord(uv_2); GL.Vertex(pos_2); // 2
		//				GL.Color(vColor1); GL.TexCoord(uv_1); GL.Vertex(pos_1); // 1
		//				GL.Color(vColor0); GL.TexCoord(uv_0); GL.Vertex(pos_0); // 0
		//			}



		//			//------------------------------------------

		//			//삭제 21.5.18
		//			//GL.End();//<전환 완료>
		//			//Clipping Pass마다 Pass 한번씩 종료
		//			_matBatch.EndPass();
		//		}

		//		if (externalRenderTexture == null)
		//		{
		//			_matBatch.DeactiveRenderTexture();
		//		}
		//		else
		//		{
		//			RenderTexture.active = externalRenderTexture;
		//		}



		//		//3. Child를 렌더링하자

		//		apTransform_Mesh.ClipMeshSet clipMeshSet = null;

		//		apTransform_Mesh clipMeshTF = null;
		//		apRenderUnit clipRenderUnit = null;
		//		apMesh clipMesh = null;

		//		for (int iClip = 0; iClip < nClipMeshes; iClip++)
		//		{
		//			clipMeshSet = childClippedSet[iClip];
		//			if (clipMeshSet == null || clipMeshSet._meshTransform == null)
		//			{
		//				continue;
		//			}
		//			clipMeshTF = clipMeshSet._meshTransform;					
		//			clipMesh = clipMeshTF._mesh;
		//			clipRenderUnit = clipMeshTF._linkedRenderUnit;//이 부분이 v1.5.0에서 변경됨

		//			if (clipMesh == null || clipRenderUnit == null) { continue; }
		//			if (!clipRenderUnit._isVisible) { continue; }
		//			if (clipMesh._indexBuffer.Count < 3) { continue; }

		//			//추가 12.04 : Extra 옵션 적용
		//			apTextureData childTextureData = clipMesh.LinkedTextureData;
		//			if (clipRenderUnit.IsExtraTextureChanged)
		//			{
		//				childTextureData = clipRenderUnit.ChangedExtraTextureData;
		//			}

		//			//추가 21.2.16 : 선택되지 않은 RenderUnit은 회색으로 표시
		//			bool isNotEditedGrayColor = false;

		//			if (editor._exModObjOption_ShowGray &&
		//				(clipRenderUnit._exCalculateMode == apRenderUnit.EX_CALCULATE.Disabled_NotEdit ||
		//					clipRenderUnit._exCalculateMode == apRenderUnit.EX_CALCULATE.Disabled_ExRun))
		//			{
		//				//선택되지 않은 건 Gray 색상으로 표시하고자 할 때
		//				isNotEditedGrayColor = true;
		//			}


		//			if (isToneColor)
		//			{
		//				//Onion ToneColor Clipping
		//				_matBatch.BeginPass_Clipped_ToneColor(GL.TRIANGLES, _toneColor, childTextureData._image/*, renderUnit._meshColor2X*/);
		//			}
		//			else if (isNotEditedGrayColor)
		//			{
		//				_matBatch.BeginPass_Gray_Clipped(GL.TRIANGLES, clipRenderUnit._meshColor2X, childTextureData._image/*, renderUnit._meshColor2X*/);
		//			}
		//			else
		//			{
		//				//일반 Clipping
		//				_matBatch.BeginPass_Clipped(GL.TRIANGLES, clipRenderUnit._meshColor2X, childTextureData._image, vertexColorRatio, clipRenderUnit.ShaderType/*, renderUnit._meshColor2X*/);
		//			}

		//			//삭제 21.5.18
		//			//_matBatch.SetClippingSize(_glScreenClippingSize);
		//			//GL.Begin(GL.TRIANGLES);

		//			int nClipIndexBuffers = clipMesh._indexBuffer != null ? clipMesh._indexBuffer.Count : 0;
		//			int nClipVerts = clipMesh._vertexData != null ? clipMesh._vertexData.Count : 0;

		//			int clipIndex_0 = 0;
		//			int clipIndex_1 = 0;
		//			int clipIndex_2 = 0;

		//			//------------------------------------------
		//			for (int i = 0; i < nClipIndexBuffers; i += 3)
		//			{
		//				if (i + 2 >= nClipIndexBuffers)
		//				{ break; }

		//				clipIndex_0 = clipMesh._indexBuffer[i + 0];
		//				clipIndex_1 = clipMesh._indexBuffer[i + 1];
		//				clipIndex_2 = clipMesh._indexBuffer[i + 2];

		//				if (clipIndex_0 >= nClipVerts ||
		//					clipIndex_1 >= nClipVerts ||
		//					clipIndex_2 >= nClipVerts)
		//				{
		//					break;
		//				}

		//				rVert0 = clipRenderUnit._renderVerts[clipIndex_0];
		//				rVert1 = clipRenderUnit._renderVerts[clipIndex_1];
		//				rVert2 = clipRenderUnit._renderVerts[clipIndex_2];

		//				vColor0 = Color.black;
		//				vColor1 = Color.black;
		//				vColor2 = Color.black;

		//				if (isBoneWeightColor)
		//				{
		//					if (isBoneColor)
		//					{
		//						vColor0 = rVert0._renderColorByTool * (vertexColorRatio) + Color.black * (1.0f - vertexColorRatio);
		//						vColor1 = rVert1._renderColorByTool * (vertexColorRatio) + Color.black * (1.0f - vertexColorRatio);
		//						vColor2 = rVert2._renderColorByTool * (vertexColorRatio) + Color.black * (1.0f - vertexColorRatio);
		//					}
		//					else
		//					{
		//						vColor0 = _func_GetWeightColor3(rVert0._renderWeightByTool);
		//						vColor1 = _func_GetWeightColor3(rVert1._renderWeightByTool);
		//						vColor2 = _func_GetWeightColor3(rVert2._renderWeightByTool);
		//					}
		//				}
		//				else if (isPhyVolumeWeightColor)
		//				{
		//					vColor0 = GetWeightGrayscale(rVert0._renderWeightByTool);
		//					vColor1 = GetWeightGrayscale(rVert1._renderWeightByTool);
		//					vColor2 = GetWeightGrayscale(rVert2._renderWeightByTool);
		//				}


		//				//변경 v1.4.4 : Ref 이용
		//				World2GL_Vec3(ref pos_0, ref rVert0._pos_World);
		//				World2GL_Vec3(ref pos_1, ref rVert1._pos_World);
		//				World2GL_Vec3(ref pos_2, ref rVert2._pos_World);

		//				// uv_0 = clipMesh._vertexData[clipIndex_0]._uv;
		//				// uv_1 = clipMesh._vertexData[clipIndex_1]._uv;
		//				// uv_2 = clipMesh._vertexData[clipIndex_2]._uv;
		//				uv_0 = rVert0._vertex._uv;
		//				uv_1 = rVert1._vertex._uv;
		//				uv_2 = rVert2._vertex._uv;

		//				GL.Color(vColor0);	GL.TexCoord(uv_0);	GL.Vertex(pos_0); // 0
		//				GL.Color(vColor1);	GL.TexCoord(uv_1);	GL.Vertex(pos_1); // 1
		//				GL.Color(vColor2);	GL.TexCoord(uv_2);	GL.Vertex(pos_2); // 2

		//				//Back Side
		//				GL.Color(vColor2);	GL.TexCoord(uv_2);	GL.Vertex(pos_2); // 2
		//				GL.Color(vColor1);	GL.TexCoord(uv_1);	GL.Vertex(pos_1); // 1
		//				GL.Color(vColor0);	GL.TexCoord(uv_0);	GL.Vertex(pos_0); // 0
		//			}
		//			//------------------------------------------------

		//			//삭제 21.5.18
		//			//GL.End();//<전환 완료> (밑에)

		//		}

		//		//Clipping 렌더링 후 Pass 한번 종료
		//		_matBatch.EndPass();

		//		//사용했던 RenderTexture를 해제한다.
		//		_matBatch.ReleaseRenderTexture();
		//		//_matBatch.DeactiveRenderTexture();

		//	}
		//	catch (Exception ex)
		//	{
		//		Debug.LogException(ex);
		//	}
		//} 
		#endregion


		/// <summary>
		/// 작업 영역 크기의 RenderTexture를 임시로 생성한다.
		/// 다 사용하면 RenderTexture.ReleaseTemporary(_renderTexture)를 호출하자
		/// </summary>
		/// <returns></returns>
		public static RenderTexture GetTempRenderTexture(FilterMode filterMode)
		{
			int rtWidth = Mathf.Max(_windowWidth, 4);
			int rtHeight = Mathf.Max(_windowHeight, 4);
			RenderTexture renderTexture = RenderTexture.GetTemporary(rtWidth, rtHeight, 8, RenderTextureFormat.ARGB32);
			renderTexture.wrapMode = TextureWrapMode.Clamp;
			renderTexture.isPowerOfTwo = false;
			renderTexture.filterMode = filterMode;

			return renderTexture;
		}



		#region [미사용 코드]
		///// <summary>
		///// Clipping Render의 Mask Texture만 취하는 함수
		///// RTT 후 실제 Texture2D로 굽기 때문에 실시간으로는 사용하기 힘들다.
		///// 클리핑을 하지 않는다.
		///// </summary>
		///// <param name="renderUnit"></param>
		///// <returns></returns>
		//public static Texture2D GetMaskTexture_ClippingParent(	apRenderUnit renderUnit,
		//														apSendMaskData.RT_SHADER_TYPE shaderType)
		//{
		//	//렌더링 순서
		//	//Parent - 기본
		//	//Parent - Mask
		//	//(For) Child - Clipped
		//	//Release RenderMask
		//	try
		//	{
		//		//0. 메시, 텍스쳐가 없을 때
		//		if (renderUnit == null
		//			|| renderUnit._meshTransform == null
		//			|| renderUnit._meshTransform._mesh == null)
		//		{
		//			return null;
		//		}

		//		//변경 22.3.23 [v1.4.0]
		//		int nRenderVerts = renderUnit._renderVerts != null ? renderUnit._renderVerts.Length : 0;
		//		if(nRenderVerts == 0)
		//		{
		//			return null;
		//		}

		//		apMesh mesh = renderUnit._meshTransform._mesh;
		//		Color textureColor = Color.black;
		//		bool isAlphaMask = false;

		//		//Shader Type에 따라서 색상이 다르다.
		//		switch (shaderType)
		//		{
		//			case apSendMaskData.RT_SHADER_TYPE.AlphaMask:
		//				{
		//					isAlphaMask = true;
		//					textureColor = renderUnit._meshColor2X;
		//				}
		//				break;
		//			case apSendMaskData.RT_SHADER_TYPE.MainTextureWithColor:
		//				{
		//					isAlphaMask = false;
		//					textureColor = renderUnit._meshColor2X;
		//				}
		//				break;
		//			case apSendMaskData.RT_SHADER_TYPE.MainTextureOnly:
		//				{
		//					isAlphaMask = false;
		//					textureColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);
		//				}
		//				break;
		//		}

		//		if(!renderUnit._isVisible)
		//		{
		//			textureColor.a = 0.0f;
		//		}


		//		apTextureData linkedTextureData = mesh.LinkedTextureData;


		//		//추가 12.4 : Extra Option에 의해 Texture가 바귀었을 경우
		//		if (renderUnit.IsExtraTextureChanged)
		//		{
		//			linkedTextureData = renderUnit.ChangedExtraTextureData;
		//		}

		//		//if (mesh.LinkedTextureData == null)//이전
		//		if(linkedTextureData == null)
		//		{
		//			return null;
		//		}


		//		int nIndexBuffers = mesh._indexBuffer != null ? mesh._indexBuffer.Count : 0;
		//		int nVerts = mesh._vertexData != null ? mesh._vertexData.Count : 0;

		//		//렌더링 방식은 Mesh (with Color) 또는 Vertex / Outline이 있다.

		//		//1. Parent의 기본 렌더링을 하자
		//		//+2. Parent의 마스크를 렌더링하자
		//		if (nIndexBuffers < 3)
		//		{
		//			return null;
		//		}

		//		apRenderVertex rVert0 = null, rVert1 = null, rVert2 = null;

		//		//Pass는 RTT용 Pass 한개만 둔다.
		//		bool isRenderTexture = true; //<<RTT만 한다.

		//		//변경 21.5.18
		//		//클리핑을 안한다면 기존 Pass를 종료한다.
		//		_matBatch.EndPass();
		//		_matBatch.BeginPass_Mask(	GL.TRIANGLES,
		//									textureColor,
		//									linkedTextureData._image,
		//									0.0f,
		//									renderUnit.ShaderType,
		//									isRenderTexture, isAlphaMask,
		//									true,
		//									new Vector4(0, 0, 1, 1)//<<클리핑을 하지 않는다.
		//									);
		//		//_matBatch.SetClippingSize(new Vector4(0, 0, 1, 1));//<<클리핑을 하지 않는다.
		//		//GL.Begin(GL.TRIANGLES);


		//		//Vector2 posGL_0 = Vector2.zero;
		//		//Vector2 posGL_1 = Vector2.zero;
		//		//Vector2 posGL_2 = Vector2.zero;

		//		Vector3 pos_0 = Vector3.zero;
		//		Vector3 pos_1 = Vector3.zero;
		//		Vector3 pos_2 = Vector3.zero;

		//		Vector2 uv_0 = Vector2.zero;
		//		Vector2 uv_1 = Vector2.zero;
		//		Vector2 uv_2 = Vector2.zero;

		//		int index_0 = 0;
		//		int index_1 = 0;
		//		int index_2 = 0;

		//		//색상은 처음에만
		//		GL.Color(Color.black); 

		//		//------------------------------------------
		//		for (int i = 0; i < nIndexBuffers; i += 3)
		//		{
		//			if (i + 2 >= nIndexBuffers) { break; }

		//			index_0 = mesh._indexBuffer[i + 0];
		//			index_1 = mesh._indexBuffer[i + 1];
		//			index_2 = mesh._indexBuffer[i + 2];

		//			if (index_0 >= nVerts ||
		//				index_1 >= nVerts ||
		//				index_2 >= nVerts)
		//			{
		//				break;
		//			}

		//			rVert0 = renderUnit._renderVerts[index_0];
		//			rVert1 = renderUnit._renderVerts[index_1];
		//			rVert2 = renderUnit._renderVerts[index_2];

		//			//변경 v1.4.4 : Ref 이용
		//			World2GL_Vec3(ref pos_0, ref rVert0._pos_World);
		//			World2GL_Vec3(ref pos_1, ref rVert1._pos_World);
		//			World2GL_Vec3(ref pos_2, ref rVert2._pos_World);

		//			uv_0 = mesh._vertexData[index_0]._uv;
		//			uv_1 = mesh._vertexData[index_1]._uv;
		//			uv_2 = mesh._vertexData[index_2]._uv;

		//			/*GL.Color(Color.black);*/ GL.TexCoord(uv_0); GL.Vertex(pos_0); // 0
		//			/*GL.Color(Color.black);*/ GL.TexCoord(uv_1); GL.Vertex(pos_1); // 1
		//			/*GL.Color(Color.black);*/ GL.TexCoord(uv_2); GL.Vertex(pos_2); // 2

		//			// Back Side
		//			/*GL.Color(Color.black);*/ GL.TexCoord(uv_2); GL.Vertex(pos_2); // 2
		//			/*GL.Color(Color.black);*/ GL.TexCoord(uv_1); GL.Vertex(pos_1); // 1
		//			/*GL.Color(Color.black);*/ GL.TexCoord(uv_0); GL.Vertex(pos_0); // 0
		//		}

		//		//------------------------------------------
		//		//삭제 21.5.18
		//		//GL.End();//<변환 완료>

		//		//Clipping Size 복구
		//		_matBatch.EndPass();
		//		_matBatch.SetClippingSize(_glScreenClippingSize);




		//		//Texture2D로 굽자
		//		Texture2D resultTex = new Texture2D(_matBatch.RenderTex.width, _matBatch.RenderTex.height, TextureFormat.RGBA32, false);
		//		resultTex.ReadPixels(new Rect(0, 0, _matBatch.RenderTex.width, _matBatch.RenderTex.height), 0, 0);
		//		resultTex.Apply();

		//		//사용했던 RenderTexture를 해제한다.
		//		_matBatch.ReleaseRenderTexture();
		//		//_matBatch.DeactiveRenderTexture();

		//		return resultTex;

		//	}
		//	catch (Exception ex)
		//	{
		//		Debug.LogException(ex);
		//	}
		//	return null;
		//} 
		#endregion

		/// <summary>
		/// Clipping Render의 Mask Texture만 취하는 함수
		/// RTT 후 실제 Texture2D로 굽기 때문에 실시간으로는 사용하기 힘들다.
		/// 클리핑을 하지 않는다.
		/// </summary>
		/// <param name="renderUnit"></param>
		/// <returns></returns>
		public static void DrawRenderUnitForExport_MaskParent(	apRenderUnit renderUnit,
																apSendMaskData.RT_SHADER_TYPE shaderType,
																RenderTexture targetRT,
																bool needToClearRT,

																//클리핑-체인
																bool isChainClipped,
																RenderTexture clippingMask,
																RenderTexture receiveMaskRT_1, apSendMaskData.MASK_OPERATION receiveMaskOp_1,
																RenderTexture receiveMaskRT_2, apSendMaskData.MASK_OPERATION receiveMaskOp_2,
																RenderTexture receiveMaskRT_3, apSendMaskData.MASK_OPERATION receiveMaskOp_3,
																RenderTexture receiveMaskRT_4, apSendMaskData.MASK_OPERATION receiveMaskOp_4,
																RenderTexture receiveSeeThroughRT, float receiveSeeThroughAlpha
																)
		{
			//렌더링 순서
			//Parent - 기본
			//Parent - Mask
			//(For) Child - Clipped
			//Release RenderMask
			try
			{
				//0. 메시, 텍스쳐가 없을 때
				if (renderUnit == null
					|| renderUnit._meshTransform == null
					|| renderUnit._meshTransform._mesh == null)
				{
					return;
				}

				//변경 22.3.23 [v1.4.0]
				int nRenderVerts = renderUnit._renderVerts != null ? renderUnit._renderVerts.Length : 0;
				if(nRenderVerts == 0)
				{
					return;
				}

				apMesh mesh = renderUnit._meshTransform._mesh;
				Color textureColor = Color.black;
				bool isAlphaMask = false;

				//Shader Type에 따라서 색상이 다르다.
				switch (shaderType)
				{
					case apSendMaskData.RT_SHADER_TYPE.AlphaMask:
						{
							isAlphaMask = true;
							textureColor = renderUnit._meshColor2X;
						}
						break;
					case apSendMaskData.RT_SHADER_TYPE.MainTextureWithColor:
						{
							isAlphaMask = false;
							textureColor = renderUnit._meshColor2X;
						}
						break;
					case apSendMaskData.RT_SHADER_TYPE.MainTextureOnly:
						{
							isAlphaMask = false;
							textureColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);
						}
						break;
				}

				if(!renderUnit._isVisible)
				{
					textureColor.a = 0.0f;
				}


				apTextureData linkedTextureData = mesh.LinkedTextureData;
				Texture2D meshImage = null;

				//추가 12.4 : Extra Option에 의해 Texture가 바귀었을 경우
				if (renderUnit.IsExtraTextureChanged)
				{
					linkedTextureData = renderUnit.ChangedExtraTextureData;
				}

				if(linkedTextureData != null)
				{
					meshImage = linkedTextureData._image;
				}
				

				int nIndexBuffers = mesh._indexBuffer != null ? mesh._indexBuffer.Count : 0;
				int nVerts = mesh._vertexData != null ? mesh._vertexData.Count : 0;

				//렌더링 방식은 Mesh (with Color) 또는 Vertex / Outline이 있다.

				//1. Parent의 기본 렌더링을 하자
				//+2. Parent의 마스크를 렌더링하자
				if (nIndexBuffers < 3)
				{
					return;
				}

				apRenderVertex rVert0 = null, rVert1 = null, rVert2 = null;

				//변경 21.5.18
				//클리핑을 안한다면 기존 Pass를 종료한다.
				_matBatch.EndPass();
				_matBatch.BeginPass_MaskWithRT(	GL.TRIANGLES,
											textureColor,
											meshImage,
											renderUnit.ShaderType,
											isAlphaMask,
											true,
											new Vector4(0, 0, 1, 1),//<<클리핑을 하지 않는다.
											targetRT,
											needToClearRT,

											isChainClipped,
											clippingMask,
											receiveMaskRT_1, apSendMaskData.MaskOperationToFloatValue(receiveMaskOp_1),
											receiveMaskRT_2, apSendMaskData.MaskOperationToFloatValue(receiveMaskOp_2),
											receiveMaskRT_3, apSendMaskData.MaskOperationToFloatValue(receiveMaskOp_3),
											receiveMaskRT_4, apSendMaskData.MaskOperationToFloatValue(receiveMaskOp_4),
											receiveSeeThroughRT, receiveSeeThroughAlpha
											);

				Vector3 pos_0 = Vector3.zero;
				Vector3 pos_1 = Vector3.zero;
				Vector3 pos_2 = Vector3.zero;

				Vector2 uv_0 = Vector2.zero;
				Vector2 uv_1 = Vector2.zero;
				Vector2 uv_2 = Vector2.zero;

				int index_0 = 0;
				int index_1 = 0;
				int index_2 = 0;
				
				//색상은 처음에만
				GL.Color(Color.black); 
				
				//------------------------------------------
				for (int i = 0; i < nIndexBuffers; i += 3)
				{
					if (i + 2 >= nIndexBuffers) { break; }

					index_0 = mesh._indexBuffer[i + 0];
					index_1 = mesh._indexBuffer[i + 1];
					index_2 = mesh._indexBuffer[i + 2];

					if (index_0 >= nVerts ||
						index_1 >= nVerts ||
						index_2 >= nVerts)
					{
						break;
					}

					rVert0 = renderUnit._renderVerts[index_0];
					rVert1 = renderUnit._renderVerts[index_1];
					rVert2 = renderUnit._renderVerts[index_2];

					//변경 v1.4.4 : Ref 이용
					World2GL_Vec3(ref pos_0, ref rVert0._pos_World);
					World2GL_Vec3(ref pos_1, ref rVert1._pos_World);
					World2GL_Vec3(ref pos_2, ref rVert2._pos_World);

					uv_0 = mesh._vertexData[index_0]._uv;
					uv_1 = mesh._vertexData[index_1]._uv;
					uv_2 = mesh._vertexData[index_2]._uv;

					GL.TexCoord(uv_0); GL.Vertex(pos_0); // 0
					GL.TexCoord(uv_1); GL.Vertex(pos_1); // 1
					GL.TexCoord(uv_2); GL.Vertex(pos_2); // 2

					// Back Side
					GL.TexCoord(uv_2); GL.Vertex(pos_2); // 2
					GL.TexCoord(uv_1); GL.Vertex(pos_1); // 1
					GL.TexCoord(uv_0); GL.Vertex(pos_0); // 0
				}

				//------------------------------------------
				//삭제 21.5.18
				//GL.End();//<변환 완료>

				//Clipping Size 복구
				_matBatch.EndPass();
				_matBatch.SetClippingSize(_glScreenClippingSize);

				//사용했던 RenderTexture를 해제한다.
				//_matBatch.ReleaseRenderTexture();//이건 apGL이 자체적으로 RenderTexture를 생성한 경우
				_matBatch.DeactiveRenderTexture();//이건 외부에서 RenderTexture를 생성한 경우

			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
		}


		#region [미사용 코드]
		///// <summary>
		///// RTT 없이 "이미 구워진 MaskTexture"를 이용해서 Clipping 렌더링을 한다.
		///// 클리핑을 하지 않는다.
		///// </summary>
		///// <param name="renderUnit"></param>
		///// <param name="renderType"></param>
		///// <param name="childClippedSet"></param>
		///// <param name="vertexController"></param>
		///// <param name="select"></param>
		///// <param name="externalRenderTexture"></param>
		//public static void DrawRenderUnit_ClippingParent_ForExport_WithoutRTT(	apRenderUnit renderUnit,
		//																		List<apTransform_Mesh.ClipMeshSet> childClippedSet,
		//																		Texture2D maskedTexture

		//																		////v1.4.6 추가
		//																		//bool isPixelPerfect,
		//																		//Vector2 clipAreaPosGL_LB,
		//																		//Vector2 posSizeGLPerPixel
		//																)
		//{
		//	//렌더링 순서
		//	//Parent - 기본
		//	//Parent - Mask
		//	//(For) Child - Clipped
		//	//Release RenderMask
		//	try
		//	{
		//		//0. 메시, 텍스쳐가 없을 때
		//		if (renderUnit == null || renderUnit._meshTransform == null || renderUnit._meshTransform._mesh == null)
		//		{
		//			return;
		//		}

		//		//이전
		//		//if (renderUnit._renderVerts.Count == 0) { return; }

		//		//변경 22.3.23 [v1.4.0]
		//		int nRenderVerts = renderUnit._renderVerts != null ? renderUnit._renderVerts.Length : 0;
		//		if(nRenderVerts == 0)
		//		{
		//			return;
		//		}

		//		Color textureColor = renderUnit._meshColor2X;
		//		apMesh mesh = renderUnit._meshTransform._mesh;


		//		apTextureData linkedTextureData = mesh.LinkedTextureData;

		//		//추가 12.4 : Extra Option에 의해 Texture가 바귀었을 경우
		//		if(renderUnit.IsExtraTextureChanged)
		//		{
		//			linkedTextureData = renderUnit.ChangedExtraTextureData;
		//		}

		//		//if (mesh.LinkedTextureData == null)//이전
		//		if(linkedTextureData == null)
		//		{
		//			return;
		//		}



		//		int nClipMeshes = childClippedSet.Count;

		//		int nIndexBuffers = mesh._indexBuffer != null ? mesh._indexBuffer.Count : 0;
		//		int nVerts = mesh._vertexData != null ? mesh._vertexData.Count : 0;

		//		//렌더링 방식은 Mesh (with Color) 또는 Vertex / Outline이 있다.

		//		//1. Parent의 기본 렌더링을 하자
		//		//+2. Parent의 마스크를 렌더링하자
		//		if (nIndexBuffers < 3)
		//		{
		//			return;
		//		}

		//		apRenderVertex rVert0 = null, rVert1 = null, rVert2 = null;

		//		Color vertexChannelColor = Color.black;
		//		Color vColor0 = Color.black, vColor1 = Color.black, vColor2 = Color.black;


		//		//Vector2 posGL_0 = Vector2.zero;
		//		//Vector2 posGL_1 = Vector2.zero;
		//		//Vector2 posGL_2 = Vector2.zero;

		//		Vector3 pos_0 = Vector3.zero;
		//		Vector3 pos_1 = Vector3.zero;
		//		Vector3 pos_2 = Vector3.zero;

		//		Vector2 uv_0 = Vector2.zero;
		//		Vector2 uv_1 = Vector2.zero;
		//		Vector2 uv_2 = Vector2.zero;

		//		//RTT 관련 코드는 모두 뺀다. Pass도 한번이고 기본 렌더링

		//		//변경 21.5.18
		//		//클리핑을 안한다면 기존의 Pass를 종료시킨다.
		//		_matBatch.EndPass();
		//		_matBatch.BeginPass_Texture_VColor(	GL.TRIANGLES, textureColor, linkedTextureData._image, 0.0f, renderUnit.ShaderType, 
		//											true, new Vector4(0, 0, 1, 1)//<<클리핑을 하지 않는다.
		//											);
		//		//_matBatch.SetClippingSize(new Vector4(0, 0, 1, 1));//<<클리핑을 하지 않는다.
		//		//GL.Begin(GL.TRIANGLES);

		//		int index_0 = 0;
		//		int index_1 = 0;
		//		int index_2 = 0;

		//		//색은 한번만
		//		GL.Color(Color.black);

		//		//------------------------------------------
		//		for (int i = 0; i < nIndexBuffers; i += 3)
		//		{

		//			if (i + 2 >= nIndexBuffers) { break; }

		//			index_0 = mesh._indexBuffer[i + 0];
		//			index_1 = mesh._indexBuffer[i + 1];
		//			index_2 = mesh._indexBuffer[i + 2];

		//			if (index_0 >= nVerts ||
		//				index_1 >= nVerts ||
		//				index_2 >= nVerts)
		//			{
		//				break;
		//			}

		//			rVert0 = renderUnit._renderVerts[index_0];
		//			rVert1 = renderUnit._renderVerts[index_1];
		//			rVert2 = renderUnit._renderVerts[index_2];

		//			//변경 v1.4.4 : Ref 이용
		//			World2GL_Vec3(ref pos_0, ref rVert0._pos_World);
		//			World2GL_Vec3(ref pos_1, ref rVert1._pos_World);
		//			World2GL_Vec3(ref pos_2, ref rVert2._pos_World);

		//			uv_0 = mesh._vertexData[index_0]._uv;
		//			uv_1 = mesh._vertexData[index_1]._uv;
		//			uv_2 = mesh._vertexData[index_2]._uv;

		//			////v1.4.6 : Pixel Perfect 옵션
		//			//if(isPixelPerfect)
		//			//{
		//			//	ConvertPixelPerfectPos(ref pos_0, ref clipAreaPosGL_LB, ref posSizeGLPerPixel);
		//			//	ConvertPixelPerfectPos(ref pos_1, ref clipAreaPosGL_LB, ref posSizeGLPerPixel);
		//			//	ConvertPixelPerfectPos(ref pos_2, ref clipAreaPosGL_LB, ref posSizeGLPerPixel);
		//			//}

		//			GL.TexCoord(uv_0); GL.Vertex(pos_0); // 0
		//			GL.TexCoord(uv_1); GL.Vertex(pos_1); // 1
		//			GL.TexCoord(uv_2); GL.Vertex(pos_2); // 2

		//			// Back Side
		//			GL.TexCoord(uv_2); GL.Vertex(pos_2); // 2
		//			GL.TexCoord(uv_1); GL.Vertex(pos_1); // 1
		//			GL.TexCoord(uv_0); GL.Vertex(pos_0); // 0
		//		}



		//		//------------------------------------------
		//		//삭제 21.5.18
		//		//GL.End();//<변환 완료>

		//		//클리핑 사이즈 복구
		//		_matBatch.EndPass();
		//		_matBatch.SetClippingSize(_glScreenClippingSize);//<<클리핑을 하지 않는다.


		//		//3. Child를 렌더링하자. MaskedTexture를 직접 이용
		//		apTransform_Mesh.ClipMeshSet clipMeshSet = null;

		//		apTransform_Mesh clipMeshTF = null;
		//		apRenderUnit clipRenderUnit = null;
		//		apMesh clipMesh = null;


		//		for (int iClip = 0; iClip < nClipMeshes; iClip++)
		//		{
		//			clipMeshSet = childClippedSet[iClip];
		//			if (clipMeshSet == null || clipMeshSet._meshTransform == null)
		//			{
		//				continue;
		//			}
		//			clipMeshTF = clipMeshSet._meshTransform;
		//			clipMesh = clipMeshTF._mesh;
		//			clipRenderUnit = clipMeshTF._linkedRenderUnit;

		//			if (clipMesh == null || clipRenderUnit == null)		{ continue; }
		//			if (clipRenderUnit._meshTransform == null)			{ continue; }
		//			if (!clipRenderUnit._isVisible)						{ continue; }

		//			int nClipIndexBuffers = clipMesh._indexBuffer != null ? clipMesh._indexBuffer.Count : 0;
		//			int nClipVerts = clipMesh._vertexData != null ? clipMesh._vertexData.Count : 0;

		//			if (nClipIndexBuffers < 3)
		//			{
		//				continue;
		//			}

		//			//추가 12.4 : Extra Option
		//			apTextureData clipTextureData = clipMesh.LinkedTextureData;
		//			if(clipRenderUnit.IsExtraTextureChanged)
		//			{
		//				clipTextureData = clipRenderUnit.ChangedExtraTextureData;
		//			}

		//			//변경 21.5.18
		//			//클리핑을 하지 않는다면, 기존 Pass 종료
		//			_matBatch.EndPass();
		//			_matBatch.BeginPass_ClippedWithMaskedTexture(GL.TRIANGLES, clipRenderUnit._meshColor2X,
		//														//clipMesh.LinkedTextureData._image,//이전
		//														clipTextureData._image,
		//														0.0f,
		//														clipRenderUnit.ShaderType,
		//														//renderUnit._meshColor2X,
		//														maskedTexture,
		//														new Vector4(0, 0, 1, 1)//<<클리핑을 하지 않는다.
		//														);

		//			int clipIndex_0 = 0;
		//			int clipIndex_1 = 0;
		//			int clipIndex_2 = 0;

		//			//_matBatch.SetClippingSize(new Vector4(0, 0, 1, 1));//<<클리핑을 하지 않는다.
		//			//GL.Begin(GL.TRIANGLES);//삭제

		//			//색은 한번만
		//			GL.Color(Color.black);

		//			//------------------------------------------
		//			for (int i = 0; i < nClipIndexBuffers; i += 3)
		//			{
		//				if (i + 2 >= nClipIndexBuffers)
		//				{ break; }

		//				clipIndex_0 = clipMesh._indexBuffer[i + 0];
		//				clipIndex_1 = clipMesh._indexBuffer[i + 1];
		//				clipIndex_2 = clipMesh._indexBuffer[i + 2];

		//				if (clipIndex_0 >= nClipVerts ||
		//					clipIndex_1 >= nClipVerts ||
		//					clipIndex_2 >= nClipVerts)
		//				{
		//					break;
		//				}

		//				rVert0 = clipRenderUnit._renderVerts[clipIndex_0];
		//				rVert1 = clipRenderUnit._renderVerts[clipIndex_1];
		//				rVert2 = clipRenderUnit._renderVerts[clipIndex_2];


		//				//변경 v1.4.4 : Ref 이용
		//				World2GL_Vec3(ref pos_0, ref rVert0._pos_World);
		//				World2GL_Vec3(ref pos_1, ref rVert1._pos_World);
		//				World2GL_Vec3(ref pos_2, ref rVert2._pos_World);


		//				uv_0 = clipMesh._vertexData[clipIndex_0]._uv;
		//				uv_1 = clipMesh._vertexData[clipIndex_1]._uv;
		//				uv_2 = clipMesh._vertexData[clipIndex_2]._uv;

		//				////v1.4.6 : Pixel Perfect 옵션
		//				//if(isPixelPerfect)
		//				//{
		//				//	ConvertPixelPerfectPos(ref pos_0, ref clipAreaPosGL_LB, ref posSizeGLPerPixel);
		//				//	ConvertPixelPerfectPos(ref pos_1, ref clipAreaPosGL_LB, ref posSizeGLPerPixel);
		//				//	ConvertPixelPerfectPos(ref pos_2, ref clipAreaPosGL_LB, ref posSizeGLPerPixel);
		//				//}


		//				/*GL.Color(vColor0);*/ GL.TexCoord(uv_0); GL.Vertex(pos_0); // 0
		//				/*GL.Color(vColor1);*/ GL.TexCoord(uv_1); GL.Vertex(pos_1); // 1
		//				/*GL.Color(vColor2);*/ GL.TexCoord(uv_2); GL.Vertex(pos_2); // 2

		//				//Back Side
		//				/*GL.Color(vColor2);*/ GL.TexCoord(uv_2); GL.Vertex(pos_2); // 2
		//				/*GL.Color(vColor1);*/ GL.TexCoord(uv_1); GL.Vertex(pos_1); // 1
		//				/*GL.Color(vColor0);*/ GL.TexCoord(uv_0); GL.Vertex(pos_0); // 0


		//			}
		//			//------------------------------------------------
		//			//삭제 21.5.18
		//			//GL.End();//<변환 완료>


		//			//클리핑 사이즈 복구
		//			_matBatch.EndPass();
		//			_matBatch.SetClippingSize(_glScreenClippingSize);//<<클리핑을 하지 않는다.

		//		}
		//	}
		//	catch (Exception ex)
		//	{
		//		Debug.LogException(ex);
		//	}
		//} 
		#endregion




		//v1.6.0 : 클리핑 되거나 마스크를 받는 메시를 렌더링하기
		//기존에는 클리핑 Parent 위주로 렌더링했지만, 이제는 Child가 직접 렌더링을 한다.
		public static void DrawRenderUnit_MaskReceived(	apRenderUnit renderUnit,
														RenderTypeRequest renderRequest,
														apVertexController vertexController,
														apEditor editor,
														apSelection select)
		{
			//렌더링 순서
			//Parent - 기본
			//Parent - Mask
			//(For) Child - Clipped
			//Release RenderMask
			try
			{
				//0. 메시, 텍스쳐가 없을 때
				if (renderUnit == null || renderUnit._meshTransform == null || renderUnit._meshTransform._mesh == null)
				{
					return;
				}

				//이전
				//if (renderUnit._renderVerts.Count == 0)
				//{
				//	return;
				//}

				int nRenderVerts = renderUnit._renderVerts != null ? renderUnit._renderVerts.Length : 0;
				if(nRenderVerts == 0)
				{
					return;
				}

				Color textureColor = renderUnit._meshColor2X;
				apTransform_Mesh meshTF = renderUnit._meshTransform;
				apMesh mesh = renderUnit._meshTransform._mesh;


				apTextureData linkedTextureData = mesh.LinkedTextureData;

				//추가 12.4 : Extra Option에 의해 Texture가 바귀었을 경우
				if (renderUnit.IsExtraTextureChanged)
				{
					linkedTextureData = renderUnit.ChangedExtraTextureData;
				}

				if (linkedTextureData == null)
				{
					return;
				}

				bool isBoneWeightColor = renderRequest.BoneRigWeightColor;
				bool isPhyVolumeWeightColor = renderRequest.PhysicsWeightColor || renderRequest.VolumeWeightColor;

				int iVertColor = 0;

				if (renderRequest.VolumeWeightColor)                        //변경 22.3.3
				{
					iVertColor = 1;
				}
				else if (renderRequest.PhysicsWeightColor)                      //변경 22.3.3
				{
					iVertColor = 2;
				}
				else if (renderRequest.BoneRigWeightColor)                      //변경 22.3.3
				{
					iVertColor = 3;
				}
				else
				{
					iVertColor = 0;
				}

				bool isBoneColor = false;
				float vertexColorRatio = 0.0f;

				//bool isToneColor = (int)(renderType & RENDER_TYPE.ToneColor) != 0;	//이전
				bool isToneColor = renderRequest.ToneColor;                             //변경 22.3.3


				if (select != null)
				{
					//isBoneColor = select._rigEdit_isBoneColorView;//이전
					isBoneColor = editor._rigViewOption_BoneColor;//변경 19.7.31

					if (isBoneWeightColor)
					{
						//if (select._rigEdit_viewMode == apSelection.RIGGING_EDIT_VIEW_MODE.WeightColorOnly)
						if (editor._rigViewOption_WeightOnly)
						{
							vertexColorRatio = 1.0f;
						}
						else
						{
							vertexColorRatio = 0.5f;
						}
					}
					else if (isPhyVolumeWeightColor)
					{
						vertexColorRatio = 0.7f;
					}
				}

				//추가 21.2.16 : 선택되지 않은 RenderUnit은 회색으로 표시
				bool isNotEditedGrayColor_Parent = false;

				if (editor._exModObjOption_ShowGray &&
					(renderUnit._exCalculateMode == apRenderUnit.EX_CALCULATE.Disabled_NotEdit ||
						renderUnit._exCalculateMode == apRenderUnit.EX_CALCULATE.Disabled_ExRun))
				{
					//선택되지 않은 건 Gray 색상으로 표시하고자 할 때
					isNotEditedGrayColor_Parent = true;
				}

				//v1.6.0 : Mask Only 메시 (사실 Clipped 메시가 Mask Only면 큰 의미는 없다)
				bool isMaskOnlyMesh = meshTF._isMaskOnlyMesh;



				int nIndexBuffers = mesh._indexBuffer != null ? mesh._indexBuffer.Count : 0;
				int nVerts = mesh._vertexData != null ? mesh._vertexData.Count : 0;


				//렌더링 방식은 Mesh (with Color) 또는 Vertex / Outline이 있다.

				if (nIndexBuffers < 3)
				{
					return;
				}

				// < 중요 >
				//클리핑 여부, 마스크 여부 등을 조회하여 쉐이더에 넣을 정보들을 취득한다.

				//< 클리핑 >
				//Color clipParentMeshColor2X = new Color(0.5f, 0.5f, 0.5f, 1.0f);//부모 Alpha를 받아올 필요가 없어졌다.
				apMaskRT clipMaskRT = null;

				// < 임의의 마스크 >
				apMaskRT recvMaskRT_Ch1 = null;
				apMaskRT recvMaskRT_Ch2 = null;
				apMaskRT recvMaskRT_Ch3 = null;
				apMaskRT recvMaskRT_Ch4 = null;

				apSendMaskData.ReceivePropertySet recvPropSet_Ch1 = null;
				apSendMaskData.ReceivePropertySet recvPropSet_Ch2 = null;
				apSendMaskData.ReceivePropertySet recvPropSet_Ch3 = null;
				apSendMaskData.ReceivePropertySet recvPropSet_Ch4 = null;

				apMaskRT recvSeeThroughRT = null;
				apSendMaskData.ReceivePropertySet recvSeeThroughPropSet = null;


				if (meshTF._isClipping_Child)
				{
					//클리핑 자식 메시라면
					//클리핑 부모 메시를 찾고 마스크 정보를 받아온다.
					if(meshTF._clipParentMeshTransform != null
						&& meshTF._clipParentMeshTransform._linkedRenderUnit != null)
					{
						//clipParentMeshColor2X = meshTF._clipParentMeshTransform._linkedRenderUnit._meshColor2X;
						clipMaskRT = editor.RenderTex.GetRT_ClippingParent(meshTF._clipParentMeshTransform);
					}
				}

				int nMaskInfos = meshTF._linkedReceivedMasks != null ? meshTF._linkedReceivedMasks.Count : 0;
				if(nMaskInfos > 0)
				{
					//마스크를 수신하는 정보가 있다
					//하나씩 보고 렌더링에 참조할만한게 있는지 확인하자
					apMaskLinkInfo curMaskInfo = null;
					apSendMaskData sendMaskData = null;
					apTransform_Mesh clipParentMeshTF = null;
					apSendMaskData.ReceivePropertySet propSet = null;

					apMaskRT maskRT = null;

					for (int iMaskInfo = 0; iMaskInfo < nMaskInfos; iMaskInfo++)
					{
						curMaskInfo = meshTF._linkedReceivedMasks[iMaskInfo];

						sendMaskData = curMaskInfo._parentMaskData;
						clipParentMeshTF = curMaskInfo._parentMaskMeshTF;
						
						if(sendMaskData == null || clipParentMeshTF == null)
						{
							continue;
						}

						//설정에 따른 Mask를 가져온다.
						if(sendMaskData._isRTShared)
						{
							//공유 RT
							maskRT = editor.RenderTex.GetRT_Shared(sendMaskData._rtShaderType, sendMaskData._sharedRTID);
						}
						else
						{
							//개별 RT
							maskRT = editor.RenderTex.GetRT_PerMeshTF(clipParentMeshTF, sendMaskData._rtShaderType);
						}
						
						

						int nPropSets = sendMaskData._propertySets != null ? sendMaskData._propertySets.Count : 0;
						if(nPropSets == 0)
						{
							continue;
						}

						for (int iPropSet = 0; iPropSet < nPropSets; iPropSet++)
						{
							propSet = sendMaskData._propertySets[iPropSet];
							if(propSet._preset == apSendMaskData.SHADER_PROP_PRESET.AlphaMaskPreset)
							{
								// [ Alpha Mask 프리셋 ]
								//채널을 확인하여 전달할 데이터에 할당을 하자
								switch (propSet._reservedChannel)
								{
									case apSendMaskData.SHADER_PROP_RESERVED_CHANNEL.Channel_1:
										recvPropSet_Ch1 = propSet;
										recvMaskRT_Ch1 = maskRT;
										break;

									case apSendMaskData.SHADER_PROP_RESERVED_CHANNEL.Channel_2:
										recvPropSet_Ch2 = propSet;
										recvMaskRT_Ch2 = maskRT;
										break;

									case apSendMaskData.SHADER_PROP_RESERVED_CHANNEL.Channel_3:
										recvPropSet_Ch3 = propSet;
										recvMaskRT_Ch3 = maskRT;
										break;

									case apSendMaskData.SHADER_PROP_RESERVED_CHANNEL.Channel_4:
										recvPropSet_Ch4 = propSet;
										recvMaskRT_Ch4 = maskRT;
										break;
								}
							}
							else if(propSet._preset == apSendMaskData.SHADER_PROP_PRESET.SeeThroughPreset)
							{
								// [ See-Through 프리셋 ]
								recvSeeThroughRT = maskRT;
								recvSeeThroughPropSet = propSet;
							}

							
						}
					}
				}



				apRenderVertex rVert0 = null, rVert1 = null, rVert2 = null;

				Color vertexChannelColor = Color.black;
				Color vColor0 = Color.black, vColor1 = Color.black, vColor2 = Color.black;

				Vector3 pos_0 = Vector3.zero;
				Vector3 pos_1 = Vector3.zero;
				Vector3 pos_2 = Vector3.zero;

				Vector2 uv_0 = Vector2.zero;
				Vector2 uv_1 = Vector2.zero;
				Vector2 uv_2 = Vector2.zero;

				int index_0 = 0;
				int index_1 = 0;
				int index_2 = 0;


				RenderTexture.active = null;

				//BeginPass시 마스크 정보를 전달해야한다.
				if (isToneColor)
				{
					//Onion ToneColor Clipping
					_matBatch.BeginPass_Clipped_ToneColor_WithMaskInfo(	GL.TRIANGLES,
																		_toneColor,
																		linkedTextureData._image,
																		//클리핑
																		clipMaskRT,
																		//clipParentMeshColor2X,
																		
																		//채널별 마스크
																		recvMaskRT_Ch1, recvPropSet_Ch1,
																		recvMaskRT_Ch2, recvPropSet_Ch2,
																		recvMaskRT_Ch3, recvPropSet_Ch3,
																		recvMaskRT_Ch4, recvPropSet_Ch4,
																		
																		//투과
																		recvSeeThroughRT, recvSeeThroughPropSet);
				}
				else if (isNotEditedGrayColor_Parent)
				{
					_matBatch.BeginPass_Gray_Clipped_WithMaskInfo(	GL.TRIANGLES,
																	textureColor,
																	linkedTextureData._image,

																	//클리핑
																	clipMaskRT,
																	//clipParentMeshColor2X,
																	
																	//채널별 마스크
																	recvMaskRT_Ch1, recvPropSet_Ch1,
																	recvMaskRT_Ch2, recvPropSet_Ch2,
																	recvMaskRT_Ch3, recvPropSet_Ch3,
																	recvMaskRT_Ch4, recvPropSet_Ch4,
																	
																	//투과
																	recvSeeThroughRT, recvSeeThroughPropSet);
				}
				else if(isMaskOnlyMesh)
				{
					// Mask Only 메시 (v1.6.0)
					textureColor.r = 0.5f;
					textureColor.g = 0.5f;
					textureColor.b = 0.5f;
					//Alpha는 0.5를 곱한다.
					textureColor.a *= 0.5f;

					//버텍스 색상 기본값을 녹색으로
					vColor0 = Color.green;
					vColor1 = Color.green;
					vColor2 = Color.green;

					_matBatch.BeginPass_Clipped_WithMaskInfo(GL.TRIANGLES,
																textureColor,
																linkedTextureData._image,
																1.0f,//버텍스 색상 (녹색) 100%
																apPortrait.SHADER_TYPE.AlphaBlend,

																//클리핑
																clipMaskRT,
																//clipParentMeshColor2X,

																//채널별 마스크
																recvMaskRT_Ch1, recvPropSet_Ch1,
																recvMaskRT_Ch2, recvPropSet_Ch2,
																recvMaskRT_Ch3, recvPropSet_Ch3,
																recvMaskRT_Ch4, recvPropSet_Ch4,

																//투과
																recvSeeThroughRT, recvSeeThroughPropSet);
				}
				else
				{
					//일반 Clipping
					_matBatch.BeginPass_Clipped_WithMaskInfo(GL.TRIANGLES,
																textureColor,
																linkedTextureData._image,
																vertexColorRatio,
																renderUnit.ShaderType,

																//클리핑
																clipMaskRT,
																//clipParentMeshColor2X,

																//채널별 마스크
																recvMaskRT_Ch1, recvPropSet_Ch1,
																recvMaskRT_Ch2, recvPropSet_Ch2,
																recvMaskRT_Ch3, recvPropSet_Ch3,
																recvMaskRT_Ch4, recvPropSet_Ch4,

																//투과
																recvSeeThroughRT, recvSeeThroughPropSet);
				}

				//삭제 21.5.18
				//_matBatch.SetClippingSize(_glScreenClippingSize);
				//GL.Begin(GL.TRIANGLES);

				//------------------------------------------
				for (int i = 0; i < nIndexBuffers; i += 3)
				{
					if (i + 2 >= nIndexBuffers)
					{ break; }

					index_0 = mesh._indexBuffer[i + 0];
					index_1 = mesh._indexBuffer[i + 1];
					index_2 = mesh._indexBuffer[i + 2];

					if (index_0 >= nVerts ||
						index_1 >= nVerts ||
						index_2 >= nVerts)
					{
						break;
					}

					rVert0 = renderUnit._renderVerts[index_0];
					rVert1 = renderUnit._renderVerts[index_1];
					rVert2 = renderUnit._renderVerts[index_2];

					//경우에 따라선 위에서 정한 기본값을 유지해야하므로 여기서 기본값을 바꾸진 말자
					//vColor0 = Color.black;
					//vColor1 = Color.black;
					//vColor2 = Color.black;


					switch (iVertColor)
					{
						case 1: //VolumeWeightColor
							vColor0 = GetWeightGrayscale(rVert0._renderWeightByTool);
							vColor1 = GetWeightGrayscale(rVert1._renderWeightByTool);
							vColor2 = GetWeightGrayscale(rVert2._renderWeightByTool);
							break;

						case 2: //PhysicsWeightColor
							vColor0 = GetWeightColor4(rVert0._renderWeightByTool);
							vColor1 = GetWeightColor4(rVert1._renderWeightByTool);
							vColor2 = GetWeightColor4(rVert2._renderWeightByTool);
							break;

						case 3: //BoneRigWeightColor
							if (isBoneColor)
							{
								vColor0 = rVert0._renderColorByTool * (vertexColorRatio) + Color.black * (1.0f - vertexColorRatio);
								vColor1 = rVert1._renderColorByTool * (vertexColorRatio) + Color.black * (1.0f - vertexColorRatio);
								vColor2 = rVert2._renderColorByTool * (vertexColorRatio) + Color.black * (1.0f - vertexColorRatio);
							}
							else
							{
								vColor0 = _func_GetWeightColor3(rVert0._renderWeightByTool);
								vColor1 = _func_GetWeightColor3(rVert1._renderWeightByTool);
								vColor2 = _func_GetWeightColor3(rVert2._renderWeightByTool);
							}
							vColor0.a = 1.0f;
							vColor1.a = 1.0f;
							vColor2.a = 1.0f;
							break;
					}

					//변경 v1.4.4 : Ref 이요
					World2GL_Vec3(ref pos_0, ref rVert0._pos_World);
					World2GL_Vec3(ref pos_1, ref rVert1._pos_World);
					World2GL_Vec3(ref pos_2, ref rVert2._pos_World);

					// uv_0 = mesh._vertexData[index_0]._uv;
					// uv_1 = mesh._vertexData[index_1]._uv;
					// uv_2 = mesh._vertexData[index_2]._uv;

					uv_0 = rVert0._vertex._uv;
					uv_1 = rVert1._vertex._uv;
					uv_2 = rVert2._vertex._uv;

					GL.Color(vColor0); GL.TexCoord(uv_0); GL.Vertex(pos_0); // 0
					GL.Color(vColor1); GL.TexCoord(uv_1); GL.Vertex(pos_1); // 1
					GL.Color(vColor2); GL.TexCoord(uv_2); GL.Vertex(pos_2); // 2

					// Back Side
					GL.Color(vColor2); GL.TexCoord(uv_2); GL.Vertex(pos_2); // 2
					GL.Color(vColor1); GL.TexCoord(uv_1); GL.Vertex(pos_1); // 1
					GL.Color(vColor0); GL.TexCoord(uv_0); GL.Vertex(pos_0); // 0
				}



				//------------------------------------------

				//삭제 21.5.18
				//Pass 종료
				_matBatch.EndPass();

				//사용했던 RenderTexture를 해제한다.
				//_matBatch.ReleaseRenderTexture();

			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
		}

		//------------------------------------------------------------------------------------------------
		// Rigging Vertex (Circle)
		//------------------------------------------------------------------------------------------------
		

		// Rig Circle V2
		/// <summary>
		/// v2 방식의 Rig Circle 버텍스 렌더링 함수. 
		/// 리턴값으로 선택 범위를 리턴한다.
		/// </summary>
		/// <param name="renderVertex"></param>
		/// <param name="isUseBoneColor"></param>
		/// <param name="isVertSelected"></param>
		private static float DrawRiggingRenderVert_V2(apRenderVertex renderVertex, bool isUseBoneColor, bool isVertSelected)
		{
			/*
			_matBatch.SetPass_Color();
			_matBatch.SetClippingSize(_glScreenClippingSize);

			GL.Begin(GL.TRIANGLES);
			*/

			Vector2 posCenterGL = renderVertex._pos_GL;

			
			//colorOutline.a = 1.0f;//<<여기선 반투명 Outline을 지원하지 않는다.

			if (renderVertex._renderRigWeightParam._nParam == 0)
			{
				//데이터가 없는 경우 > 고정 크기의 작은 점
				//이전
				//float size_None = 10.0f;
				//float size_None_Outline = 14.0f;

				//변경 20.3.25 : 별도의 정의 값을 이용
				float size_None_Half = (isVertSelected ? (RIG_CIRCLE_SIZE_NORIG_SELECTED * 0.5f) : (RIG_CIRCLE_SIZE_NORIG * 0.5f));

				Vector2 uv_0 = (isVertSelected ? new Vector2(0.5f, 1.0f) : new Vector2(0.0f, 1.0f));
				Vector2 uv_1 = (isVertSelected ? new Vector2(1.0f, 1.0f) : new Vector2(0.5f, 1.0f));
				Vector2 uv_2 = (isVertSelected ? new Vector2(1.0f, 0.0f) : new Vector2(0.5f, 0.0f));
				Vector2 uv_3 = (isVertSelected ? new Vector2(0.5f, 0.0f) : new Vector2(0.0f, 0.0f));

				Vector2 pos_0 = new Vector2(posCenterGL.x - size_None_Half, posCenterGL.y - size_None_Half);
				Vector2 pos_1 = new Vector2(posCenterGL.x + size_None_Half, posCenterGL.y - size_None_Half);
				Vector2 pos_2 = new Vector2(posCenterGL.x + size_None_Half, posCenterGL.y + size_None_Half);
				Vector2 pos_3 = new Vector2(posCenterGL.x - size_None_Half, posCenterGL.y + size_None_Half);

				//그냥 원형 아이콘
				GL.Color(isVertSelected ? Color.red : Color.white);

				GL.TexCoord(uv_0);	GL.Vertex(pos_0); // 0
				GL.TexCoord(uv_1);	GL.Vertex(pos_1); // 1
				GL.TexCoord(uv_2);	GL.Vertex(pos_2); // 2

				GL.TexCoord(uv_2);	GL.Vertex(pos_2); // 2
				GL.TexCoord(uv_3);	GL.Vertex(pos_3); // 3
				GL.TexCoord(uv_0);	GL.Vertex(pos_0); // 0


				return RIG_CIRCLE_SIZE_NORIG_CLICK_SIZE;//선택 범위는 고정값.

			}
			else
			{
				//N개의 데이터

				//Degree 방식으로 단순 증가
				//> -1 * Deg2Rad - (0.5 * PI)
				//UV 중요
				//45도마다 분할
				float prevAngle_Deg = 0.0f;
				float nextAngle_Deg = 0.0f;
				
				//변경 20.3.25 : 옵션으로 정할 수 있다.
				float radius = (isVertSelected ? _rigCircleSize_SelectedVert : _rigCircleSize_NoSelectedVert);
				float radius_SelectedArea = (isVertSelected ? _rigCircleSize_SelectedVert_Enlarged : _rigCircleSize_NoSelectedVert_Enlarged);
				
				//bool isSelectedAreaFlashing = (_rigSelectedWeightGUIType == apEditor.RIG_SELECTED_WEIGHT_GUI_TYPE.Flashing);


				if(_isRigCircleScaledByZoom)
				{
					radius *= _zoom;
					radius_SelectedArea *= _zoom;
				}
				
				//radius = Mathf.Clamp(radius, RIG_CIRCLE_SIZE_DEF * 0.1f, RIG_CIRCLE_SIZE_DEF__SMALL_OUTLINE * 10.0f);
				//radius_SelectedArea = Mathf.Clamp(radius_SelectedArea, RIG_CIRCLE_SIZE_DEF__LARGE_OUTLINE * 0.1f, RIG_CIRCLE_SIZE_DEF__LARGE_OUTLINE * 10.0f);
				
				float curRatio = 0.0f;
				float prevRatio = 0.0f;
				float nextRatio = 0.0f;
				Color curColor = Color.black;
				bool curSelected = false;

				float realAngleDeg_Prev = 0.0f;
				float realAngleDeg_Next = 0.0f;
				//float difAngle = 0.0f;

				bool isAnySelectedBone = false;
				Color selectedAreaColor = Color.black;

				for (int iRig = 0; iRig < renderVertex._renderRigWeightParam._nParam; iRig++)
				{
					curRatio = renderVertex._renderRigWeightParam._ratios[iRig];
					curSelected = renderVertex._renderRigWeightParam._isSelecteds[iRig];

					if (isUseBoneColor)
					{
						curColor = renderVertex._renderRigWeightParam._colors[iRig];
						curColor.a = 1.0f;
					}
					else
					{
						//본 색상을 사용하지 않는 방식
						//이전 방식 : 선택된 영역은 그라디언트, 그외의 영역은 어두운 본 색상
						if (curSelected)
						{
							curColor = _func_GetWeightColor3(curRatio);
						}
						else
						{
							curColor = renderVertex._renderRigWeightParam._colors[iRig];
							curColor *= 0.8f;
							curColor.a = 1.0f;
						}

						//변경 방식 : 20.3.28 : 선택 여부 상관없이 그라디언트. 단, 그 외의 영역은 살짝 반투명(선택된 본에 한해서)
						//curColor = _func_GetWeightColor3(curRatio);
						//if(isVertSelected && !curSelected)
						//{
						//	curColor.a = 0.8f;
						//}
						//else
						//{
						//	curColor.a = 1.0f;
						//}
					}
					nextRatio = prevRatio + curRatio;
					nextAngle_Deg = prevAngle_Deg + (curRatio * 360.0f);


					realAngleDeg_Prev = prevAngle_Deg;
					realAngleDeg_Next = nextAngle_Deg;

					//선택된 본이 있다면
					if(curSelected && isVertSelected)
					{
						isAnySelectedBone = true;
						//if (isUseBoneColor)
						//{
						//	selectedAreaColor = _func_GetWeightColor3(curRatio);
						//}
						//else
						//{
						//	selectedAreaColor = curColor;
						//}
						selectedAreaColor = curColor;
						selectedAreaColor.a = 1.0f;
					}

					if(curSelected && _isRigSelectedWeightArea_Flashing)
					{
						//선택된 본 영역이 반짝거리는 옵션이 켜진 경우
						float flashingLerp = _animRatio_SelectedRigFlashing;
						Color darkColor = curColor * 0.4f;
						Color brightColor = (curColor + new Color(0.1f, 0.1f, 0.1f, 1.0f)) * 2.0f;

						brightColor.r = Mathf.Clamp01(brightColor.r);
						brightColor.g = Mathf.Clamp01(brightColor.g);
						brightColor.b = Mathf.Clamp01(brightColor.b);

						curColor = (darkColor * flashingLerp) + (brightColor * (1.0f - flashingLerp));
						curColor.a = 1.0f;
					}
					
					DrawRigCirclePart_V2(posCenterGL, (curSelected ? radius_SelectedArea : radius), curColor, realAngleDeg_Prev, realAngleDeg_Next, isVertSelected);

					//다음으로 이동
					prevRatio = nextRatio;
					prevAngle_Deg = nextAngle_Deg;

					
				}

				if(isAnySelectedBone && isVertSelected)
				{
					//선택된 본이 있다면 중앙의 원으로 한번 더 보여주자
					float radius_Summary = radius * 0.4f;
					Vector2 uv_0 = new Vector2(0.0f, 1.0f);
					Vector2 uv_1 = new Vector2(0.5f, 1.0f);
					Vector2 uv_2 = new Vector2(0.5f, 0.0f);
					Vector2 uv_3 = new Vector2(0.0f, 0.0f);

					Vector2 pos_0 = new Vector2(posCenterGL.x - radius_Summary, posCenterGL.y - radius_Summary);
					Vector2 pos_1 = new Vector2(posCenterGL.x + radius_Summary, posCenterGL.y - radius_Summary);
					Vector2 pos_2 = new Vector2(posCenterGL.x + radius_Summary, posCenterGL.y + radius_Summary);
					Vector2 pos_3 = new Vector2(posCenterGL.x - radius_Summary, posCenterGL.y + radius_Summary);

					//그냥 박스
					GL.Color(selectedAreaColor);

					GL.TexCoord(uv_0);	GL.Vertex(pos_0); // 0
					GL.TexCoord(uv_1);	GL.Vertex(pos_1); // 1
					GL.TexCoord(uv_2);	GL.Vertex(pos_2); // 2

					GL.TexCoord(uv_2);	GL.Vertex(pos_2); // 2
					GL.TexCoord(uv_3);	GL.Vertex(pos_3); // 3
					GL.TexCoord(uv_0);	GL.Vertex(pos_0); // 0
				}

				return _isRigCircleScaledByZoom ? (_rigCircleSize_ClickSize_Rigged * _zoom) : (_rigCircleSize_ClickSize_Rigged);
			}


		}


		private static void DrawRigCirclePart_V2(Vector2 posCenterGL, 
												float radius, Color color,
												float angleDeg_Prev, float angleDeg_Next,
												bool isVertSelected)
		{
			//45, 135, 225, 315를 사이에 두면 꼭지점을 추가해야한다.
			int iPart_Prev = 0;
			int iPart_Next = 0;

			//color.a = 1.0f;

			//Vector2 uv_Center = new Vector2(0.5f, 0.5f);
			Vector2 uv_Center = new Vector2((isVertSelected ? 0.75f : 0.25f), 0.5f);

			if(angleDeg_Prev > angleDeg_Next)
			{
				float tmpAngle = angleDeg_Next;
				angleDeg_Next = angleDeg_Prev;
				angleDeg_Prev = tmpAngle;
			}

			if (angleDeg_Prev < 45.0f)			{ iPart_Prev = 0; }
			else if(angleDeg_Prev < 135.0f)		{ iPart_Prev = 1; }
			else if(angleDeg_Prev < 225.0f)		{ iPart_Prev = 2; }
			else if(angleDeg_Prev < 315.0f)		{ iPart_Prev = 3; }
			else								{ iPart_Prev = 4; }

			if (angleDeg_Next < 45.0f)			{ iPart_Next = 0; }
			else if(angleDeg_Next < 135.0f)		{ iPart_Next = 1; }
			else if(angleDeg_Next < 225.0f)		{ iPart_Next = 2; }
			else if(angleDeg_Next < 315.0f)		{ iPart_Next = 3; }
			else								{ iPart_Next = 4; }

			Vector2 pos_Prev = Vector2.zero;
			Vector2 pos_Next = Vector2.zero;
			
			Vector2 uv_Prev = Vector2.zero;
			Vector2 uv_Next = Vector2.zero;
			float uOffset = (isVertSelected ? 0.5f : 0.0f);

			float c2S_Ratio_Prev = 1.0f;
			float c2S_Ratio_Next = 1.0f;

			float angleRad_Prev = 0.0f;
			float angleRad_Next = 0.0f;

			//위치 좌표계 : LT > RB (CCW?)
			
			//UV 좌표계 : LB > RT

			while(true)
			{
				//만약 같은 대각-사분면에 위치한다면 > 바로 삼각형을 만들어서 그리기 > break;
				//그렇지 않다면 > next에 해당 꼭지점과 삼각형을 만들어서 그리기 > 그 꼭지점을 prev로 하여 한번 더 반복. 단, 사분면 증가
				if(iPart_Prev == iPart_Next)
				{
					angleRad_Prev = angleDeg_Prev * Mathf.Deg2Rad;
					angleRad_Next = angleDeg_Next * Mathf.Deg2Rad;

					switch (iPart_Prev)
					{
						
						//위/아래 : Y 고정으로 비율 계산
						case 0:
						case 4:
						case 2:
							c2S_Ratio_Prev = 1.0f / Mathf.Abs(Mathf.Cos(angleRad_Prev));
							c2S_Ratio_Next = 1.0f / Mathf.Abs(Mathf.Cos(angleRad_Next));
							break;

						//좌/우 : X 고정으로 비율 계산
						case 1:
						case 3:
							c2S_Ratio_Prev = 1.0f / Mathf.Abs(Mathf.Sin(angleRad_Prev));
							c2S_Ratio_Next = 1.0f / Mathf.Abs(Mathf.Sin(angleRad_Next));
							break;
					}
					
					//Sin, Cos을 반대로
					pos_Prev.x = posCenterGL.x + (Mathf.Sin(angleRad_Prev) * radius * c2S_Ratio_Prev);
					pos_Prev.y = posCenterGL.y + (-Mathf.Cos(angleRad_Prev) * radius * c2S_Ratio_Prev);

					pos_Next.x = posCenterGL.x + (Mathf.Sin(angleRad_Next) * radius * c2S_Ratio_Next);
					pos_Next.y = posCenterGL.y + (-Mathf.Cos(angleRad_Next) * radius * c2S_Ratio_Next);

					uv_Prev.x = Mathf.Cos(angleRad_Prev) * c2S_Ratio_Prev;
					uv_Prev.y = Mathf.Sin(angleRad_Prev) * c2S_Ratio_Prev;

					uv_Next.x = Mathf.Cos(angleRad_Next) * c2S_Ratio_Next;
					uv_Next.y = Mathf.Sin(angleRad_Next) * c2S_Ratio_Next;

					//uv_Prev.x = (uv_Prev.x * 0.5f) + 0.5f;//0 ~ 1
					uv_Prev.x = ((uv_Prev.x * 0.5f) + 0.5f) * 0.5f + uOffset;//(0 ~ 0.5) or (0.5 ~ 1)
					uv_Prev.y = (uv_Prev.y * -0.5f) + 0.5f;

					//uv_Next.x = (uv_Next.x * 0.5f) + 0.5f;//0 ~ 1
					uv_Next.x = ((uv_Next.x * 0.5f) + 0.5f) * 0.5f + uOffset;//(0 ~ 0.5) or (0.5 ~ 1)
					uv_Next.y = (uv_Next.y * -0.5f) + 0.5f;

					//안쪽
					GL.Color(color);
					
					GL.TexCoord(uv_Center);	GL.Vertex(posCenterGL);
					GL.TexCoord(uv_Prev);	GL.Vertex(pos_Prev);
					GL.TexCoord(uv_Next);	GL.Vertex(pos_Next);
					

					//종료!
					break;
				}
				else
				{
					//일단 Prev의 각도부터
					angleRad_Prev = angleDeg_Prev * Mathf.Deg2Rad;

					switch (iPart_Prev)
					{
						case 0:case 4:case 2:	c2S_Ratio_Prev = 1.0f / Mathf.Abs(Mathf.Cos(angleRad_Prev));	break;
						case 1:case 3:			c2S_Ratio_Prev = 1.0f / Mathf.Abs(Mathf.Sin(angleRad_Prev));	break;
					}

					//Sin, Cos를 반대로
					pos_Prev.x = posCenterGL.x + (Mathf.Sin(angleRad_Prev) * radius * c2S_Ratio_Prev);
					pos_Prev.y = posCenterGL.y + (-Mathf.Cos(angleRad_Prev) * radius * c2S_Ratio_Prev);

					uv_Prev.x = Mathf.Cos(angleRad_Prev) * c2S_Ratio_Prev;
					uv_Prev.y = Mathf.Sin(angleRad_Prev) * c2S_Ratio_Prev;

					//Next의 각도는 꼭지점이다.
					switch (iPart_Prev)
					{
						case 0:
						case 4:
							//45도보다 작은 경우 (Pos : 1, -1 / UV : 1, 1)
							angleRad_Next = 45.0f * Mathf.Deg2Rad;
							break;

						case 1:
							//135도보다 작은 경우 (Pos : 1, 1 / UV : 1, 0)
							angleRad_Next = 135.0f * Mathf.Deg2Rad;
							break;

						case 2:
							//225도보다 작은 경우 (Pos : -1, 1 / UV : 0, 0)
							angleRad_Next = 225.0f * Mathf.Deg2Rad;
							break;

						case 3:
							//315도보다 작은 경우 (Pos : -1, -1 / UV : 0, 1)
							angleRad_Next = 315.0f * Mathf.Deg2Rad;
							break;

					}

					switch (iPart_Prev)
					{
						case 0:case 4:case 2:	c2S_Ratio_Next = 1.0f / Mathf.Abs(Mathf.Cos(angleRad_Next));	break;
						case 1:case 3:			c2S_Ratio_Next = 1.0f / Mathf.Abs(Mathf.Sin(angleRad_Next));	break;
					}
					
					//Sin, Cos를 반대로
					pos_Next.x = posCenterGL.x + (Mathf.Sin(angleRad_Next) * radius * c2S_Ratio_Next);
					pos_Next.y = posCenterGL.y + (-Mathf.Cos(angleRad_Next) * radius * c2S_Ratio_Next);

					uv_Next.x = Mathf.Cos(angleRad_Next) * c2S_Ratio_Next;
					uv_Next.y = Mathf.Sin(angleRad_Next) * c2S_Ratio_Next;

					//uv_Prev.x = (uv_Prev.x * 0.5f) + 0.5f;//0 ~ 1
					uv_Prev.x = ((uv_Prev.x * 0.5f) + 0.5f) * 0.5f + uOffset;//(0 ~ 0.5) or (0.5 ~ 1)
					uv_Prev.y = (uv_Prev.y * -0.5f) + 0.5f;

					//uv_Next.x = (uv_Next.x * 0.5f) + 0.5f;//0 ~ 1
					uv_Next.x = ((uv_Next.x * 0.5f) + 0.5f) * 0.5f + uOffset;//(0 ~ 0.5) or (0.5 ~ 1)
					uv_Next.y = (uv_Next.y * -0.5f) + 0.5f;

					//안쪽
					GL.Color(color);
					
					GL.TexCoord(uv_Center);	GL.Vertex(posCenterGL);
					GL.TexCoord(uv_Prev);	GL.Vertex(pos_Prev);
					GL.TexCoord(uv_Next);	GL.Vertex(pos_Next);
					

					//일부의 렌더링이 끝나고 Prev 꼭지점을 옮긴다.
					switch (iPart_Prev)
					{
						case 0:
							angleDeg_Prev = 45.0f;
							iPart_Prev = 1;
							break;

						case 1:
							angleDeg_Prev = 135.0f;
							iPart_Prev = 2;
							break;

						case 2:
							angleDeg_Prev = 225.0f;
							iPart_Prev = 3;
							break;

						case 3:
							angleDeg_Prev = 315.0f;
							iPart_Prev = 4;
							break;

						case 4:
							return;
					}
				}
			}
		}



		//------------------------------------------------------------------------------------------------
		// Draw Transform Border Form of Render Unit
		//------------------------------------------------------------------------------------------------
		public static void DrawTransformBorderFormOfRenderUnit(Color lineColor, float posL, float posR, float posT, float posB, apMatrix3x3 worldMatrix)
		{
			float marginOffset = 10;
			posL -= marginOffset;
			posR += marginOffset;
			posT += marginOffset;
			posB -= marginOffset;

			//Vector3 pos3W_LT = worldMatrix.MultiplyPoint3x4(new Vector3(posL, posT, 0));
			//Vector3 pos3W_RT = worldMatrix.MultiplyPoint3x4(new Vector3(posR, posT, 0));
			//Vector3 pos3W_LB = worldMatrix.MultiplyPoint3x4(new Vector3(posL, posB, 0));
			//Vector3 pos3W_RB = worldMatrix.MultiplyPoint3x4(new Vector3(posR, posB, 0));

			//Vector2 posW_LT = new Vector2(pos3W_LT.x, pos3W_LT.y);
			//Vector2 posW_RT = new Vector2(pos3W_RT.x, pos3W_RT.y);
			//Vector2 posW_LB = new Vector2(pos3W_LB.x, pos3W_LB.y);
			//Vector2 posW_RB = new Vector2(pos3W_RB.x, pos3W_RB.y);


			Vector2 posW_LT = worldMatrix.MultiplyPoint(new Vector2(posL, posT));
			Vector2 posW_RT = worldMatrix.MultiplyPoint(new Vector2(posR, posT));
			Vector2 posW_LB = worldMatrix.MultiplyPoint(new Vector2(posL, posB));
			Vector2 posW_RB = worldMatrix.MultiplyPoint(new Vector2(posR, posB));


			float tfFormLineLength = 32.0f;

			//변경 21.5.18
			_matBatch.BeginPass_Color(GL.LINES);
			//_matBatch.SetClippingSize(_glScreenClippingSize);
			//GL.Begin(GL.LINES);

			DrawLine(posW_LT, GetUnitLineEndPoint(posW_LT, posW_RT, tfFormLineLength), lineColor, false);
			DrawLine(posW_RT, GetUnitLineEndPoint(posW_RT, posW_RB, tfFormLineLength), lineColor, false);
			DrawLine(posW_RB, GetUnitLineEndPoint(posW_RB, posW_LB, tfFormLineLength), lineColor, false);
			DrawLine(posW_LB, GetUnitLineEndPoint(posW_LB, posW_LT, tfFormLineLength), lineColor, false);

			DrawLine(posW_LT, GetUnitLineEndPoint(posW_LT, posW_LB, tfFormLineLength), lineColor, false);
			DrawLine(posW_LB, GetUnitLineEndPoint(posW_LB, posW_RB, tfFormLineLength), lineColor, false);
			DrawLine(posW_RB, GetUnitLineEndPoint(posW_RB, posW_RT, tfFormLineLength), lineColor, false);
			DrawLine(posW_RT, GetUnitLineEndPoint(posW_RT, posW_LT, tfFormLineLength), lineColor, false);

			//삭제 21.5.18
			//GL.End();//<변환 완료>
			_matBatch.EndPass();

		}

		private static Vector2 GetUnitLineEndPoint(Vector2 startPos, Vector2 endPos, float maxLength)
		{
			Vector2 dir = endPos - startPos;
			if (dir.sqrMagnitude <= maxLength * maxLength)
			{
				return endPos;
			}
			return startPos + dir.normalized * maxLength;
		}


		//------------------------------------------------------------------------------------------------
		// Draw Bone
		//------------------------------------------------------------------------------------------------

		//	본 그리기 > 버전 1 (화살촉 형태)
		public static void DrawBone_V1(apBone bone, bool isDrawOutline, bool isBoneIKUsing, bool isUseBoneToneColor, bool isAvailable)
		{
			if (bone == null)
			{
				return;
			}

			
			Color boneColor = bone._color;
			if(isUseBoneToneColor)
			{
				boneColor = _toneBoneColor;
			}
			else if(!isAvailable)
			{
				//추가 : 사용 불가능하다면 회색으로 보인다.
				boneColor = Color.gray;
			}

			Color boneOutlineColor = boneColor * 0.5f;
			boneOutlineColor.a = 1.0f;

			
			apMatrix worldMatrix = null;//이전 > 다시 이거 20.8.23
			//apBoneWorldMatrix worldMatrix = null;//변경 20.8.12 : CompleMatrix 방식 > BoneWorldMatrix

			Vector2 posW_Start = Vector2.zero;

			bool isHelperBone = bone._shapeHelper;
			Vector2 posGL_Start = Vector2.zero;
			Vector2 posGL_Mid1 = Vector2.zero;
			Vector2 posGL_Mid2 = Vector2.zero;
			Vector2 posGL_End1 = Vector2.zero;
			Vector2 posGL_End2 = Vector2.zero;

			if(isBoneIKUsing)
			{
				//IK 값이 포함될 때
				//worldMatrix = bone._worldMatrix_IK;
				//posW_Start = worldMatrix.Pos;

				//GUIMatrix로 변경 (20.8.23)
				worldMatrix = bone._guiMatrix_IK;
				posW_Start = worldMatrix._pos;

				if(!isUseBoneToneColor)
				{
					//이전
					//posGL_Start = apGL.World2GL(posW_Start);
					//posGL_Mid1 = apGL.World2GL(bone._shapePoints_V1_IK.Mid1);
					//posGL_Mid2 = apGL.World2GL(bone._shapePoints_V1_IK.Mid2);
					//posGL_End1 = apGL.World2GL(bone._shapePoints_V1_IK.End1);
					//posGL_End2 = apGL.World2GL(bone._shapePoints_V1_IK.End2);

					//변경 v1.4.4 : Ref 이용
					World2GL(ref posGL_Start, ref posW_Start);
					World2GL(ref posGL_Mid1, ref bone._shapePoints_V1_IK.Mid1);
					World2GL(ref posGL_Mid2, ref bone._shapePoints_V1_IK.Mid2);
					World2GL(ref posGL_End1, ref bone._shapePoints_V1_IK.End1);
					World2GL(ref posGL_End2, ref bone._shapePoints_V1_IK.End2);
				}
				else
				{
					//Onion Skin 전용 좌표 계산
					Vector2 deltaOnionPos = _tonePosOffset * _zoom;
					
					//이전
					//posGL_Start = apGL.World2GL(posW_Start) + deltaOnionPos;
					//posGL_Mid1 = apGL.World2GL(bone._shapePoints_V1_IK.Mid1) + deltaOnionPos;
					//posGL_Mid2 = apGL.World2GL(bone._shapePoints_V1_IK.Mid2) + deltaOnionPos;
					//posGL_End1 = apGL.World2GL(bone._shapePoints_V1_IK.End1) + deltaOnionPos;
					//posGL_End2 = apGL.World2GL(bone._shapePoints_V1_IK.End2) + deltaOnionPos;

					//변경 v1.4.4 : Ref 이용
					World2GL(ref posGL_Start, ref posW_Start);
					World2GL(ref posGL_Mid1, ref bone._shapePoints_V1_IK.Mid1);
					World2GL(ref posGL_Mid2, ref bone._shapePoints_V1_IK.Mid2);
					World2GL(ref posGL_End1, ref bone._shapePoints_V1_IK.End1);
					World2GL(ref posGL_End2, ref bone._shapePoints_V1_IK.End2);
					posGL_Start += deltaOnionPos;
					posGL_Mid1 += deltaOnionPos;
					posGL_Mid2 += deltaOnionPos;
					posGL_End1 += deltaOnionPos;
					posGL_End2 += deltaOnionPos;
				}
				
			}
			else
			{
				//worldMatrix = bone._worldMatrix;
				//posW_Start = worldMatrix.Pos;

				//GUIMatrix로 변경 (20.8.23)
				worldMatrix = bone._guiMatrix;
				posW_Start = worldMatrix._pos;

				if (!isUseBoneToneColor)
				{
					//이전
					//posGL_Start = apGL.World2GL(posW_Start);
					//posGL_Mid1 = apGL.World2GL(bone._shapePoints_V1_Normal.Mid1);
					//posGL_Mid2 = apGL.World2GL(bone._shapePoints_V1_Normal.Mid2);
					//posGL_End1 = apGL.World2GL(bone._shapePoints_V1_Normal.End1);
					//posGL_End2 = apGL.World2GL(bone._shapePoints_V1_Normal.End2);

					//v1.4.4 : Ref 이용
					World2GL(ref posGL_Start, ref posW_Start);
					World2GL(ref posGL_Mid1, ref bone._shapePoints_V1_Normal.Mid1);
					World2GL(ref posGL_Mid2, ref bone._shapePoints_V1_Normal.Mid2);
					World2GL(ref posGL_End1, ref bone._shapePoints_V1_Normal.End1);
					World2GL(ref posGL_End2, ref bone._shapePoints_V1_Normal.End2);
				}
				else
				{
					//Onion Skin 전용 좌표 계산
					Vector2 deltaOnionPos = _tonePosOffset * _zoom;

					//이전
					//posGL_Start = apGL.World2GL(posW_Start) + deltaOnionPos;
					//posGL_Mid1 = apGL.World2GL(bone._shapePoints_V1_Normal.Mid1) + deltaOnionPos;
					//posGL_Mid2 = apGL.World2GL(bone._shapePoints_V1_Normal.Mid2) + deltaOnionPos;
					//posGL_End1 = apGL.World2GL(bone._shapePoints_V1_Normal.End1) + deltaOnionPos;
					//posGL_End2 = apGL.World2GL(bone._shapePoints_V1_Normal.End2) + deltaOnionPos;

					//변경 v1.4.4 : Ref 이용
					World2GL(ref posGL_Start, ref posW_Start);
					World2GL(ref posGL_Mid1, ref bone._shapePoints_V1_Normal.Mid1);
					World2GL(ref posGL_Mid2, ref bone._shapePoints_V1_Normal.Mid2);
					World2GL(ref posGL_End1, ref bone._shapePoints_V1_Normal.End1);
					World2GL(ref posGL_End2, ref bone._shapePoints_V1_Normal.End2);

					posGL_Start += deltaOnionPos;
					posGL_Mid1 += deltaOnionPos;
					posGL_Mid2 += deltaOnionPos;
					posGL_End1 += deltaOnionPos;
					posGL_End2 += deltaOnionPos;
				}
				
			}

			

			//float orgSize = 10.0f * Zoom;
			//float orgSize = bone._shapePoints_V1_Normal.Radius * Zoom;
			float orgSize = apBone.RenderSetting_V1_Radius_Org * Zoom;
			Vector3 orgPos_Up = new Vector3(posGL_Start.x, posGL_Start.y + orgSize, 0);
			Vector3 orgPos_Left = new Vector3(posGL_Start.x - orgSize, posGL_Start.y, 0);
			Vector3 orgPos_Down = new Vector3(posGL_Start.x, posGL_Start.y - orgSize, 0);
			Vector3 orgPos_Right = new Vector3(posGL_Start.x + orgSize, posGL_Start.y, 0);

			if (!isDrawOutline)
			{
				//1. 전부다 그릴때
				//_matBatch.SetPass_Color();
				//_matBatch.SetClippingSize(_glScreenClippingSize);

				if (!isHelperBone)//<헬퍼가 아닐때
				{
					//GL.Begin(GL.TRIANGLES);

					//변경 5.18
					_matBatch.BeginPass_Color(GL.TRIANGLES);

					GL.Color(boneColor);

					//1. 사다리꼴 모양을 먼저 그리자
					//    [End1]    [End2]
					//
					//
					//
					//[Mid1]            [Mid2]
					//        [Start]

					//1) Start - Mid1 - End1
					//2) Start - Mid2 - End2
					//3) Start - End1 - End2

					//1) Start - Mid1 - End1
					GL.Vertex(posGL_Start);
					GL.Vertex(posGL_Mid1);
					GL.Vertex(posGL_End1);
					GL.Vertex(posGL_Start);
					GL.Vertex(posGL_End1);
					GL.Vertex(posGL_Mid1);

					//2) Start - Mid2 - End2
					GL.Vertex(posGL_Start);
					GL.Vertex(posGL_Mid2);
					GL.Vertex(posGL_End2);
					GL.Vertex(posGL_Start);
					GL.Vertex(posGL_End2);
					GL.Vertex(posGL_Mid2);

					//3) Start - End1 - End2 (taper가 100 미만일 때)
					if (bone._shapeTaper < 100)
					{
						GL.Vertex(posGL_Start);
						GL.Vertex(posGL_End1);
						GL.Vertex(posGL_End2);
						GL.Vertex(posGL_Start);
						GL.Vertex(posGL_End2);
						GL.Vertex(posGL_End1);
					}
					
					//삭제 21.5.18
					//GL.End();//<나중에 일괄 EndPass>
					//GL.Begin(GL.LINES);


					//변경 5.18
					_matBatch.BeginPass_Color(GL.LINES);

					DrawLineGL(posGL_Start, posGL_Mid1, boneOutlineColor, false);
					DrawLineGL(posGL_Mid1, posGL_End1, boneOutlineColor, false);
					DrawLineGL(posGL_End1, posGL_End2, boneOutlineColor, false);
					DrawLineGL(posGL_End2, posGL_Mid2, boneOutlineColor, false);
					DrawLineGL(posGL_Mid2, posGL_Start, boneOutlineColor, false);

					//삭제
					//GL.End();//<나중에 일괄 EndPass>
				}

				//삭제 21.5.18
				//GL.Begin(GL.TRIANGLES);

				//변경 21.5.18
				_matBatch.BeginPass_Color(GL.TRIANGLES);

				GL.Color(boneColor);

				//2. 원점 부분은 다각형 형태로 다시 그려주자
				//다이아몬드 형태로..



				//       Up
				// Left  |   Right
				//      Down

				GL.Vertex(orgPos_Up);
				GL.Vertex(orgPos_Left);
				GL.Vertex(orgPos_Down);
				GL.Vertex(orgPos_Up);
				GL.Vertex(orgPos_Down);
				GL.Vertex(orgPos_Left);

				GL.Vertex(orgPos_Up);
				GL.Vertex(orgPos_Right);
				GL.Vertex(orgPos_Down);
				GL.Vertex(orgPos_Up);
				GL.Vertex(orgPos_Down);
				GL.Vertex(orgPos_Right);

				//삭제
				//GL.End();//<나중에 일괄 EndPass>
				//GL.Begin(GL.LINES);

				//qusrud 21.5.18
				_matBatch.BeginPass_Color(GL.LINES);

				DrawLineGL(orgPos_Up, orgPos_Left, boneOutlineColor, false);
				DrawLineGL(orgPos_Left, orgPos_Down, boneOutlineColor, false);
				DrawLineGL(orgPos_Down, orgPos_Right, boneOutlineColor, false);
				DrawLineGL(orgPos_Right, orgPos_Up, boneOutlineColor, false);

				//삭제
				//GL.End();//<나중에 일괄 EndPass>
			}
			else
			{
				//변경 21.5.18
				_matBatch.BeginPass_Color(GL.LINES);
				//_matBatch.SetClippingSize(_glScreenClippingSize);

				//2. Outline만 그릴때
				//1> 헬퍼가 아니라면 사다리꼴만
				//2> 헬퍼라면 다이아몬드만
				//GL.Begin(GL.LINES);
				if (!isHelperBone)
				{
					DrawLineGL(posGL_Start, posGL_Mid1, boneColor, false);
					DrawLineGL(posGL_Mid1, posGL_End1, boneColor, false);
					DrawLineGL(posGL_End1, posGL_End2, boneColor, false);
					DrawLineGL(posGL_End2, posGL_Mid2, boneColor, false);
					DrawLineGL(posGL_Mid2, posGL_Start, boneColor, false);
				}
				else
				{
					DrawLineGL(orgPos_Up, orgPos_Left, boneColor, false);
					DrawLineGL(orgPos_Left, orgPos_Down, boneColor, false);
					DrawLineGL(orgPos_Down, orgPos_Right, boneColor, false);
					DrawLineGL(orgPos_Right, orgPos_Up, boneColor, false);
				}

				//삭제
				//GL.End();//<나중에 일괄 EndPass>
			}
		}

		//추가 20.5.29 : 선택된 본의 색상 방식
		public enum BONE_SELECTED_OUTLINE_COLOR
		{
			MainSelected, SubSelected, LinkTarget
		}

		//색상간의 차이를 계산해서 리턴한다. 이 값에 따라서 Default/Reverse
		private static float GetColorDif(Color colorA, Color colorB)
		{
			float diff_R = Mathf.Abs(colorA.r - colorB.r);
			float diff_G = Mathf.Abs(colorA.g - colorB.g);
			float diff_B = Mathf.Abs(colorA.b - colorB.b);
			if(diff_R > 0.5f) { diff_R -= 0.5f; }
			if(diff_G > 0.5f) { diff_G -= 0.5f; }
			if(diff_B > 0.5f) { diff_B -= 0.5f; }
			return ((diff_R * 0.3f) + (diff_G * 0.6f) + (diff_B * 0.1f)) * 2.0f;
		}

		public static void DrawSelectedBone_V1(apBone bone, BONE_SELECTED_OUTLINE_COLOR outlineColor, bool isBoneIKUsing = false)
		{
			//TODO : isMainSelect > 3개의 Enum으로 바꾸자 (메인 선택 색상, 서브 선택 생상, Link시 마우스 롤 오버 색상)
			if (bone == null)
			{
				return;
			}

			apMatrix worldMatrix = null;//이전 > 다시 이걸로 변경 (20.8.23)
			//apBoneWorldMatrix worldMatrix = null;//변경 20.8.12 : CompleMatrix 방식 > BoneWorldMatrix

			Vector2 posW_Start = Vector2.zero;

			bool isHelperBone = bone._shapeHelper;
			Vector2 posGL_Start = Vector2.zero;
			Vector2 posGL_Mid1 = Vector2.zero;
			Vector2 posGL_Mid2 = Vector2.zero;
			Vector2 posGL_End1 = Vector2.zero;
			Vector2 posGL_End2 = Vector2.zero;
			
			if(isBoneIKUsing)
			{
				//worldMatrix = bone._worldMatrix_IK;
				//posW_Start = worldMatrix.Pos;

				//GUIMatrix로 변경 (20.8.23)
				worldMatrix = bone._guiMatrix_IK;
				posW_Start = worldMatrix._pos;

				//이전
				//posGL_Start = apGL.World2GL(posW_Start);
				//posGL_Mid1 = apGL.World2GL(bone._shapePoints_V1_IK.Mid1);
				//posGL_Mid2 = apGL.World2GL(bone._shapePoints_V1_IK.Mid2);
				//posGL_End1 = apGL.World2GL(bone._shapePoints_V1_IK.End1);
				//posGL_End2 = apGL.World2GL(bone._shapePoints_V1_IK.End2);

				//변경 v1.4.4 : Ref 이용
				World2GL(ref posGL_Start, ref posW_Start);
				World2GL(ref posGL_Mid1, ref bone._shapePoints_V1_IK.Mid1);
				World2GL(ref posGL_Mid2, ref bone._shapePoints_V1_IK.Mid2);
				World2GL(ref posGL_End1, ref bone._shapePoints_V1_IK.End1);
				World2GL(ref posGL_End2, ref bone._shapePoints_V1_IK.End2);
			}
			else
			{
				//worldMatrix = bone._worldMatrix;
				//posW_Start = worldMatrix.Pos;

				//GUIMatrix로 변경 (20.8.23)
				worldMatrix = bone._guiMatrix;
				posW_Start = worldMatrix._pos;
			
				//이전
				//posGL_Start = apGL.World2GL(posW_Start);
				//posGL_Mid1 = apGL.World2GL(bone._shapePoints_V1_Normal.Mid1);
				//posGL_Mid2 = apGL.World2GL(bone._shapePoints_V1_Normal.Mid2);
				//posGL_End1 = apGL.World2GL(bone._shapePoints_V1_Normal.End1);
				//posGL_End2 = apGL.World2GL(bone._shapePoints_V1_Normal.End2);

				//변경 v1.4.4 : Ref 이용
				World2GL(ref posGL_Start, ref posW_Start);
				World2GL(ref posGL_Mid1, ref bone._shapePoints_V1_Normal.Mid1);
				World2GL(ref posGL_Mid2, ref bone._shapePoints_V1_Normal.Mid2);
				World2GL(ref posGL_End1, ref bone._shapePoints_V1_Normal.End1);
				World2GL(ref posGL_End2, ref bone._shapePoints_V1_Normal.End2);
			}



			//2. 원점 부분은 다각형 형태로 다시 그려주자
			//다이아몬드 형태로..
			//float orgSize = 10.0f * Zoom;
			//float orgSize = bone._shapePoints_V1_Normal.Radius * Zoom;
			float orgSize = apBone.RenderSetting_V1_Radius_Org * Zoom;
			Vector3 orgPos_Up = new Vector3(posGL_Start.x, posGL_Start.y + orgSize, 0);
			Vector3 orgPos_Left = new Vector3(posGL_Start.x - orgSize, posGL_Start.y, 0);
			Vector3 orgPos_Down = new Vector3(posGL_Start.x, posGL_Start.y - orgSize, 0);
			Vector3 orgPos_Right = new Vector3(posGL_Start.x + orgSize, posGL_Start.y, 0);

			//변경 21.5.18
			_matBatch.BeginPass_Color(GL.TRIANGLES);
			//_matBatch.SetClippingSize(_glScreenClippingSize);
			//GL.Begin(GL.TRIANGLES);

			Color lineColor;


			//변경 20.5.29
			Color lineColor_Default = Color.black;
			Color lineColor_Reserve = Color.black;

			switch (outlineColor)
			{
				case BONE_SELECTED_OUTLINE_COLOR.MainSelected:
				case BONE_SELECTED_OUTLINE_COLOR.SubSelected:
					lineColor_Default = _lineColor_BoneOutline_V1_Default;
					lineColor_Reserve = _lineColor_BoneOutline_V1_Reverse;
					break;

				case BONE_SELECTED_OUTLINE_COLOR.LinkTarget:
					lineColor_Default = _lineColor_BoneOutlineRollOver_V1_Default;
					lineColor_Reserve = _lineColor_BoneOutlineRollOver_V1_Reverse;
					break;
			}

			float diff_Default = GetColorDif(bone._color, lineColor_Default);
			//float diff_Reverse = GetColorDif(bone._color, lineColor_Reserve);

			//if(diff_Default * COLOR_DEFAULT_BIAS > diff_Reverse)
			if(diff_Default > COLOR_SIMILAR_BIAS)
			{
				//Default 색상이 더 차이가 크다. (다른 색상을 이용해야함)
				lineColor = lineColor_Default;
			}
			else
			{
				//Reserve 색상이 더 가깝다.
				lineColor = lineColor_Reserve;
			}


			lineColor.a = _animRatio_BoneOutlineAlpha;


			float lineThickness = 8.0f;

			if (!isHelperBone)
			{
				//헬퍼가 아닐때
				//1. 사다리꼴 모양을 먼저 그리자
				//    [End1]    [End2]
				//
				//
				//
				//[Mid1]            [Mid2]
				//        [Start]

				//1) Start - Mid1 - End1
				//2) Start - Mid2 - End2
				//3) Start - End1 - End2

				//1) Start - Mid1 - End1
				

				
				DrawBoldLineGL(posGL_Start, posGL_Mid1, lineThickness, lineColor, false);
				DrawBoldLineGL(posGL_Mid1, posGL_End1, lineThickness, lineColor, false);

				if (bone._shapeTaper < 100)
				{
					DrawBoldLineGL(posGL_End1, posGL_End2, lineThickness, lineColor, false);
				}
				DrawBoldLineGL(posGL_End2, posGL_Mid2, lineThickness, lineColor, false);
				DrawBoldLineGL(posGL_Mid2, posGL_Start, lineThickness, lineColor, false);
			}
			DrawBoldLineGL(orgPos_Up, orgPos_Left, lineThickness, lineColor, false);
			DrawBoldLineGL(orgPos_Left, orgPos_Down, lineThickness, lineColor, false);
			DrawBoldLineGL(orgPos_Down, orgPos_Right, lineThickness, lineColor, false);
			DrawBoldLineGL(orgPos_Right, orgPos_Up, lineThickness, lineColor, false);

			//삭제 21.5.18
			//GL.End();//<나중에 일괄 EndPass>

			//추가 : IK 속성이 있는 경우, GUI에 표시하자
			if (bone._IKTargetBone != null)
			{
				Vector2 IKTargetPos = Vector2.zero;
				if (isBoneIKUsing)
				{
					IKTargetPos = World2GL(bone._IKTargetBone._worldMatrix_IK.Pos);
				}
				else
				{
					IKTargetPos = World2GL(bone._IKTargetBone._worldMatrix.Pos);
				}
				DrawAnimatedLineGL(posGL_Start, IKTargetPos, Color.magenta, true);
			}

			if (bone._IKHeaderBone != null)
			{
				Vector2 IKHeadPos = Vector2.zero;
				if (isBoneIKUsing)
				{
					IKHeadPos = World2GL(bone._IKHeaderBone._worldMatrix_IK.Pos);
				}
				else
				{
					IKHeadPos = World2GL(bone._IKHeaderBone._worldMatrix.Pos);
				}
				DrawAnimatedLineGL(IKHeadPos, posGL_Start, Color.magenta, true);
			}

			if(bone._IKController._controllerType != apBoneIKController.CONTROLLER_TYPE.None)
			{
				if(bone._IKController._effectorBone != null)
				{
					Color lineColorIK = Color.yellow;
					if(bone._IKController._controllerType == apBoneIKController.CONTROLLER_TYPE.LookAt)
					{
						lineColorIK = Color.cyan;
					}
					Vector2 effectorPos = Vector2.zero;
					if(isBoneIKUsing)
					{
						effectorPos = World2GL(bone._IKController._effectorBone._worldMatrix_IK.Pos);
					}
					else
					{
						effectorPos = World2GL(bone._IKController._effectorBone._worldMatrix.Pos);
					}
					DrawAnimatedLineGL(posGL_Start, effectorPos, lineColorIK, true);
				}
			}
		}


		



		public static void DrawBoneOutline_V1(apBone bone, Color outlineColor, bool isBoneIKUsing)
		{
			if (bone == null)
			{
				return;
			}

			
			apMatrix worldMatrix = null;//이전 > 다시 이걸로 변경 20.8.23
			//apBoneWorldMatrix worldMatrix = null;//변경 20.8.12 : CompleMatrix 방식


			Vector2 posW_Start = Vector2.zero;
			bool isHelperBone = bone._shapeHelper;

			Vector2 posGL_Start = Vector2.zero;
			Vector2 posGL_Mid1 = Vector2.zero;
			Vector2 posGL_Mid2 = Vector2.zero;
			Vector2 posGL_End1 = Vector2.zero;
			Vector2 posGL_End2 = Vector2.zero;

			if(isBoneIKUsing)
			{
				//worldMatrix = bone._worldMatrix_IK;
				//posW_Start = worldMatrix.Pos;
				
				//GUI Matrix로 변경 (20.8.23)
				worldMatrix = bone._guiMatrix_IK;
				posW_Start = worldMatrix._pos;

				//이전
				//posGL_Start = apGL.World2GL(posW_Start);
				//posGL_Mid1 = apGL.World2GL(bone._shapePoints_V1_IK.Mid1);
				//posGL_Mid2 = apGL.World2GL(bone._shapePoints_V1_IK.Mid2);
				//posGL_End1 = apGL.World2GL(bone._shapePoints_V1_IK.End1);
				//posGL_End2 = apGL.World2GL(bone._shapePoints_V1_IK.End2);

				//변경 v1.4.4 : Ref 이용
				World2GL(ref posGL_Start, ref posW_Start);
				World2GL(ref posGL_Mid1, ref bone._shapePoints_V1_IK.Mid1);
				World2GL(ref posGL_Mid2, ref bone._shapePoints_V1_IK.Mid2);
				World2GL(ref posGL_End1, ref bone._shapePoints_V1_IK.End1);
				World2GL(ref posGL_End2, ref bone._shapePoints_V1_IK.End2);
			}
			else
			{
				//worldMatrix = bone._worldMatrix;
				//posW_Start = worldMatrix.Pos;

				//GUI Matrix로 변경 (20.8.23)
				worldMatrix = bone._guiMatrix;
				posW_Start = worldMatrix._pos;

				//이전
				//posGL_Start = apGL.World2GL(posW_Start);
				//posGL_Mid1 = apGL.World2GL(bone._shapePoints_V1_Normal.Mid1);
				//posGL_Mid2 = apGL.World2GL(bone._shapePoints_V1_Normal.Mid2);
				//posGL_End1 = apGL.World2GL(bone._shapePoints_V1_Normal.End1);
				//posGL_End2 = apGL.World2GL(bone._shapePoints_V1_Normal.End2);

				//변경 v1.4.4 : Ref 이용
				World2GL(ref posGL_Start, ref posW_Start);
				World2GL(ref posGL_Mid1, ref bone._shapePoints_V1_Normal.Mid1);
				World2GL(ref posGL_Mid2, ref bone._shapePoints_V1_Normal.Mid2);
				World2GL(ref posGL_End1, ref bone._shapePoints_V1_Normal.End1);
				World2GL(ref posGL_End2, ref bone._shapePoints_V1_Normal.End2);
			}

			//float orgSize = 10.0f * Zoom;
			//float orgSize = bone._shapePoints_V1_Normal.Radius * Zoom;
			float orgSize = apBone.RenderSetting_V1_Radius_Org * Zoom;
			Vector3 orgPos_Up = new Vector3(posGL_Start.x, posGL_Start.y + orgSize, 0);
			Vector3 orgPos_Left = new Vector3(posGL_Start.x - orgSize, posGL_Start.y, 0);
			Vector3 orgPos_Down = new Vector3(posGL_Start.x, posGL_Start.y - orgSize, 0);
			Vector3 orgPos_Right = new Vector3(posGL_Start.x + orgSize, posGL_Start.y, 0);

			//변경 21.5.18
			_matBatch.BeginPass_Color(GL.TRIANGLES);
			//_matBatch.SetClippingSize(_glScreenClippingSize);
			//GL.Begin(GL.TRIANGLES);


			//2. Outline만 그릴때
			//1> 헬퍼가 아니라면 사다리꼴만
			//2> 헬퍼라면 다이아몬드만
			float width = 3.0f;
			
			if (!isHelperBone)
			{
				DrawBoldLineGL(posGL_Start, posGL_Mid1, width, outlineColor, false);
				DrawBoldLineGL(posGL_Mid1, posGL_End1, width, outlineColor, false);
				if (Mathf.Abs(posGL_End1.x - posGL_End2.x) > 2f
					&& Mathf.Abs(posGL_End1.y - posGL_End2.y) > 2f)
				{
					DrawBoldLineGL(posGL_End1, posGL_End2, width, outlineColor, false);
				}
				DrawBoldLineGL(posGL_End2, posGL_Mid2, width, outlineColor, false);
				DrawBoldLineGL(posGL_Mid2, posGL_Start, width, outlineColor, false);
			}
			else
			{
				DrawBoldLineGL(orgPos_Up, orgPos_Left, width, outlineColor, false);
				DrawBoldLineGL(orgPos_Left, orgPos_Down, width, outlineColor, false);
				DrawBoldLineGL(orgPos_Down, orgPos_Right, width, outlineColor, false);
				DrawBoldLineGL(orgPos_Right, orgPos_Up, width, outlineColor, false);
			}

			//삭제 21.5.18
			//GL.End();//<나중에 일괄 EndPass>
		}

		//게산을 위한 임시 변수들
		private static Vector2 _tmp_BonePos_Local_1 = Vector2.zero;
		private static Vector2 _tmp_BonePos_Local_2 = Vector2.zero;
		private static Vector2 _tmp_BonePos_Local_3 = Vector2.zero;
		private static Vector2 _tmp_BonePos_Local_4 = Vector2.zero;
		private static Vector2 _tmp_BonePos_Local_5 = Vector2.zero;
		private static Vector2 _tmp_BonePos_Local_6 = Vector2.zero;

		private static Vector2 _tmp_BonePos_World_1 = Vector2.zero;
		private static Vector2 _tmp_BonePos_World_2 = Vector2.zero;
		private static Vector2 _tmp_BonePos_World_3 = Vector2.zero;
		private static Vector2 _tmp_BonePos_World_4 = Vector2.zero;
		private static Vector2 _tmp_BonePos_World_5 = Vector2.zero;
		private static Vector2 _tmp_BonePos_World_6 = Vector2.zero;

		public static void DrawBone_Virtual_V1(Vector2 startPosW,
												Vector2 endPosW,
												Color boneColor,
												Color outlineColor,
												int shapeWidth,
												int shapeTaper)
		{
			float length = (endPosW - startPosW).magnitude;
			float angle = 0.0f;

			if (length > 0.0f)
			{
				angle = Mathf.Atan2(endPosW.y - startPosW.y, endPosW.x - startPosW.x) * Mathf.Rad2Deg;
				angle += 90.0f;
			}

			angle += 180.0f;
			angle = apUtil.AngleTo180(angle);

			if (_cal_TmpMatrix == null)
			{
				_cal_TmpMatrix = new apMatrix();
			}
			_cal_TmpMatrix.SetIdentity();
			_cal_TmpMatrix.SetTRS(startPosW, angle, Vector2.one, true);

			//본 계산은 apBone의 GUIUpdate 중 V1 관련 코드를 기반으로 한다.

			float boneWidth = shapeWidth * apBone.RenderSetting_ScaleRatio;
			if (!apBone.RenderSetting_IsScaledByZoom)
			{
				boneWidth /= apBone.RenderSetting_WorkspaceZoom;
			}
			float boneRadius = boneWidth * 0.5f;
			float taperRatio = Mathf.Clamp01((float)(100 - shapeTaper) / 100.0f);

			//이전
			//Vector2 bonePos_End1 = apGL.World2GL(_cal_TmpMatrix.MulPoint2(new Vector2(-boneRadius * taperRatio, length)));
			//Vector2 bonePos_End2 = apGL.World2GL(_cal_TmpMatrix.MulPoint2(new Vector2(boneRadius * taperRatio, length)));
			//Vector2 bonePos_Mid1 = apGL.World2GL(_cal_TmpMatrix.MulPoint2(new Vector2(-boneRadius, length * 0.2f)));
			//Vector2 bonePos_Mid2 = apGL.World2GL(_cal_TmpMatrix.MulPoint2(new Vector2(boneRadius, length * 0.2f)));

			//변경 v1.4.4 : Ref를 이용
			_tmp_BonePos_Local_1 = new Vector2(-boneRadius * taperRatio, length);
			_tmp_BonePos_Local_2 = new Vector2(boneRadius * taperRatio, length);
			_tmp_BonePos_Local_3 = new Vector2(-boneRadius, length * 0.2f);
			_tmp_BonePos_Local_4 = new Vector2(boneRadius, length * 0.2f);

			_cal_TmpMatrix.MulPoint2(ref _tmp_BonePos_World_1, ref _tmp_BonePos_Local_1);
			_cal_TmpMatrix.MulPoint2(ref _tmp_BonePos_World_2, ref _tmp_BonePos_Local_2);
			_cal_TmpMatrix.MulPoint2(ref _tmp_BonePos_World_3, ref _tmp_BonePos_Local_3);
			_cal_TmpMatrix.MulPoint2(ref _tmp_BonePos_World_4, ref _tmp_BonePos_Local_4);

			Vector2 bonePos_End1 = apGL.World2GL(_tmp_BonePos_World_1);
			Vector2 bonePos_End2 = apGL.World2GL(_tmp_BonePos_World_2);
			Vector2 bonePos_Mid1 = apGL.World2GL(_tmp_BonePos_World_3);
			Vector2 bonePos_Mid2 = apGL.World2GL(_tmp_BonePos_World_4);


			Vector2 bonePos_Start = apGL.World2GL(startPosW);

			//float orgSize = 10.0f * Zoom;
			//float orgSize = bone._shapePoints_V1_Normal.Radius * Zoom;
			float orgSize = apBone.RenderSetting_V1_Radius_Org * Zoom;
			Vector3 orgPos_Up = new Vector3(bonePos_Start.x, bonePos_Start.y + orgSize, 0);
			Vector3 orgPos_Left = new Vector3(bonePos_Start.x - orgSize, bonePos_Start.y, 0);
			Vector3 orgPos_Down = new Vector3(bonePos_Start.x, bonePos_Start.y - orgSize, 0);
			Vector3 orgPos_Right = new Vector3(bonePos_Start.x + orgSize, bonePos_Start.y, 0);



			_matBatch.BeginPass_Color(GL.TRIANGLES);

			GL.Color(boneColor);

			//1. 사다리꼴 모양을 먼저 그리자
			//    [End1]    [End2]
			//
			//
			//
			//[Mid1]            [Mid2]
			//        [Start]

			//1) Start - Mid1 - End1
			//2) Start - Mid2 - End2
			//3) Start - End1 - End2

			//1) Start - Mid1 - End1
			GL.Vertex(bonePos_Start);
			GL.Vertex(bonePos_Mid1);
			GL.Vertex(bonePos_End1);
			GL.Vertex(bonePos_Start);
			GL.Vertex(bonePos_End1);
			GL.Vertex(bonePos_Mid1);

			//2) Start - Mid2 - End2
			GL.Vertex(bonePos_Start);
			GL.Vertex(bonePos_Mid2);
			GL.Vertex(bonePos_End2);
			GL.Vertex(bonePos_Start);
			GL.Vertex(bonePos_End2);
			GL.Vertex(bonePos_Mid2);

			//3) Start - End1 - End2 (taper가 100 미만일 때)
			if (shapeTaper < 100)
			{
				GL.Vertex(bonePos_Start);
				GL.Vertex(bonePos_End1);
				GL.Vertex(bonePos_End2);
				GL.Vertex(bonePos_Start);
				GL.Vertex(bonePos_End2);
				GL.Vertex(bonePos_End1);
			}


			//변경 5.18
			_matBatch.BeginPass_Color(GL.LINES);

			DrawLineGL(bonePos_Start,	bonePos_Mid1,	outlineColor, false);
			DrawLineGL(bonePos_Mid1,	bonePos_End1,	outlineColor, false);
			DrawLineGL(bonePos_End1,	bonePos_End2,	outlineColor, false);
			DrawLineGL(bonePos_End2,	bonePos_Mid2,	outlineColor, false);
			DrawLineGL(bonePos_Mid2,	bonePos_Start,	outlineColor, false);

			
			//2. 원점 부분은 다각형 형태로 다시 그려주자
			_matBatch.BeginPass_Color(GL.TRIANGLES);

			GL.Color(boneColor);
			
			//다이아몬드 형태로..
			//       Up
			// Left  |   Right
			//      Down

			GL.Vertex(orgPos_Up);
			GL.Vertex(orgPos_Left);
			GL.Vertex(orgPos_Down);
			GL.Vertex(orgPos_Up);
			GL.Vertex(orgPos_Down);
			GL.Vertex(orgPos_Left);

			GL.Vertex(orgPos_Up);
			GL.Vertex(orgPos_Right);
			GL.Vertex(orgPos_Down);
			GL.Vertex(orgPos_Up);
			GL.Vertex(orgPos_Down);
			GL.Vertex(orgPos_Right);

			
			
			_matBatch.BeginPass_Color(GL.LINES);

			DrawLineGL(orgPos_Up, orgPos_Left,		outlineColor, false);
			DrawLineGL(orgPos_Left, orgPos_Down,	outlineColor, false);
			DrawLineGL(orgPos_Down, orgPos_Right,	outlineColor, false);
			DrawLineGL(orgPos_Right, orgPos_Up,		outlineColor, false);

			_matBatch.EndPass();
		}


		public static void DrawSelectedBonePost(apBone bone, apPortrait portrait, bool isBoneIKUsing)
		{
			if (bone == null)
			{
				return;
			}

			//여기서 IK 범위 / 지글본 범위를 그린다.
			//조건에 맞지 않으면 return
			bool isDraw_IK = false;
			bool isDraw_Jiggle = false;
			
			if (bone._isIKAngleRange 
				&& bone._optionIK != apBone.OPTION_IK.Disabled)
			{
				//IK 범위를 그리는 경우
				isDraw_IK = true;
			}
			if(bone._isJiggle && bone._isJiggleAngleConstraint)
			{
				isDraw_Jiggle = true;
			}


			if(!isDraw_IK && !isDraw_Jiggle)
			{
				//그릴게 읍당
				return;
			}


			Vector2 posW_Start = Vector2.zero;
			
			//apMatrix worldMatrix = null;//이전
			apBoneWorldMatrix worldMatrix = null;//변경 20.8.12 : CompleMatrix 방식
			
			
			if(isBoneIKUsing)
			{
				worldMatrix = bone._worldMatrix_IK;
			}
			else
			{
				worldMatrix = bone._worldMatrix;
			}
			posW_Start = worldMatrix.Pos;

			Vector2 unitVector = new Vector2(0, 1);
			if (bone._parentBone != null)
			{
				//이전 방식
				//if (isBoneIKUsing)
				//{
				//	unitVector = bone._parentBone._worldMatrix_IK.MtrxOnlyRotation.MultiplyPoint(new Vector2(0, 1));
				//}
				//else
				//{
				//	unitVector = bone._parentBone._worldMatrix.MtrxOnlyRotation.MultiplyPoint(new Vector2(0, 1));
				//}

				//변경 20.8.12 : ComplexMatrix에 MtrxOnlyRotation이 없으므로 다르게 계산한다.
				if (isBoneIKUsing)
				{
					//이전
					//unitVector = bone._parentBone._worldMatrix_IK.MulPoint2(new Vector2(0, 1)) - bone._parentBone._worldMatrix_IK.Pos;

					//변경 v1.4.4 : Ref를 이용
					_tmp_BonePos_Local_1 = new Vector2(0, 1);					
					bone._parentBone._worldMatrix_IK.MulPoint2(ref _tmp_BonePos_World_1, ref _tmp_BonePos_Local_1);
					unitVector = _tmp_BonePos_World_1 - bone._parentBone._worldMatrix_IK.Pos;
					
				}
				else
				{
					//이전
					//unitVector = bone._parentBone._worldMatrix.MulPoint2(new Vector2(0, 1)) - bone._parentBone._worldMatrix.Pos;

					//변경 v1.4.4 : Ref를 이용
					_tmp_BonePos_Local_1 = new Vector2(0, 1);					
					bone._parentBone._worldMatrix.MulPoint2(ref _tmp_BonePos_World_1, ref _tmp_BonePos_Local_1);
					unitVector = _tmp_BonePos_World_1 - bone._parentBone._worldMatrix.Pos;
				}
			}

			float defaultAngle = bone._defaultMatrix._angleDeg;
			if(bone._renderUnit != null 
				&& bone._parentBone == null//버그 수정 v1.4.2 : 부모 본이 있다면 이미 부모 본이 회전하였으므로 그냥 따라가면 된다.
				)
			{
				defaultAngle += bone._renderUnit.WorldMatrixWrap._angleDeg;
			}

			bool isFliped_X = bone._worldMatrix.Scale.x < 0.0f;
			bool isFliped_Y = bone._worldMatrix.Scale.y < 0.0f;
			bool is1AxisFlipped = isFliped_X != isFliped_Y;//1개의 축만 뒤집힌 경우

			//추가 20.10.6 : defaultAngle은 부모 본의 Scale에 따라 반전된다. (자식 본은 제외..?)
			//bool isParentFlipped_X = false;
			//bool isParentFlipped_Y = false;
			
			if(bone._parentBone != null)
			{
				//isParentFlipped_X = bone._parentBone._worldMatrix.Scale.x < 0.0f;
				//isParentFlipped_Y = bone._parentBone._worldMatrix.Scale.y < 0.0f;
				//Y
				if(bone._parentBone._worldMatrix.Scale.y < 0.0f)
				{
					defaultAngle = 180.0f - defaultAngle;
				}
				//X
				if(bone._parentBone._worldMatrix.Scale.x < 0.0f)
				{
					defaultAngle = -defaultAngle;
				}
			}
			else if (bone._renderUnit != null)
			{
				//isParentFlipped_X = bone._renderUnit.WorldMatrixWrap._scale.x < 0.0f;
				//isParentFlipped_Y = bone._renderUnit.WorldMatrixWrap._scale.y < 0.0f;

				//원래는 Scale되는 (반전 뿐만 아니라) 벡터를 이용해서 계산해야한다.
				//defaultAngle = Mathf.Atan(Mathf.Tan((defaultAngle + 90) * Mathf.Deg2Rad) * (bone._renderUnit.WorldMatrixWrap._scale.y / bone._renderUnit.WorldMatrixWrap._scale.x)) - 90;
				
				//Y
				if (bone._renderUnit.WorldMatrixWrap._scale.y < 0.0f)
				{
					defaultAngle = -defaultAngle;
				}
				//X
				if (bone._renderUnit.WorldMatrixWrap._scale.x < 0.0f)
				{
					defaultAngle = -defaultAngle;
				}
			}

			////if(isFliped_Y)
			//if(isParentFlipped_Y)
			//{
			//	defaultAngle = 180.0f - defaultAngle;
			//}

			////if(isFliped_X)
			//if(isParentFlipped_X)
			//{
			//	defaultAngle = -defaultAngle;
			//}

			
			defaultAngle = apUtil.AngleTo180(defaultAngle);

			if (isDraw_IK)
			{
				//IK Angle 범위를 그려준다.
				Vector2 unitVector_Lower = Vector2.zero;
				Vector2 unitVector_Upper = Vector2.zero;
				Vector2 unitVector_Pref = Vector2.zero;

				//추가 20.8.8 : 스케일에 따라서 벡터의 방향이 바뀌어야 한다.
				if(is1AxisFlipped)
				{
					//한개의 축만 뒤집혀진 경우
					//부호와 Lower<->Upper가 반대이다.
					unitVector_Lower = apMatrix3x3.TRS(Vector2.zero, defaultAngle - bone._IKAngleRange_Upper, Vector2.one).MultiplyPoint(unitVector);
					unitVector_Upper = apMatrix3x3.TRS(Vector2.zero, defaultAngle - bone._IKAngleRange_Lower, Vector2.one).MultiplyPoint(unitVector);
					unitVector_Pref = apMatrix3x3.TRS(Vector2.zero, defaultAngle - bone._IKAnglePreferred, Vector2.one).MultiplyPoint(unitVector);
				}
				else
				{
					//일반적인 경우
					unitVector_Lower = apMatrix3x3.TRS(Vector2.zero, defaultAngle + bone._IKAngleRange_Lower, Vector2.one).MultiplyPoint(unitVector);
					unitVector_Upper = apMatrix3x3.TRS(Vector2.zero, defaultAngle + bone._IKAngleRange_Upper, Vector2.one).MultiplyPoint(unitVector);
					unitVector_Pref = apMatrix3x3.TRS(Vector2.zero, defaultAngle + bone._IKAnglePreferred, Vector2.one).MultiplyPoint(unitVector);
				}
				

				unitVector_Lower.Normalize();
				unitVector_Upper.Normalize();
				unitVector_Pref.Normalize();

				unitVector_Lower *= bone._shapeLength * worldMatrix.Scale.y * 1.2f;
				unitVector_Upper *= bone._shapeLength * worldMatrix.Scale.y * 1.2f;
				unitVector_Pref *= bone._shapeLength * worldMatrix.Scale.y * 1.5f;

				//BeginBatch_ColoredPolygon();//이전				
				_matBatch.BeginPass_Color(GL.TRIANGLES);//변경 21.5.18

				

				DrawBoldLine(posW_Start, posW_Start + new Vector2(unitVector_Lower.x, unitVector_Lower.y), 3, Color.magenta, false);
				DrawBoldLine(posW_Start, posW_Start + new Vector2(unitVector_Upper.x, unitVector_Upper.y), 3, Color.magenta, false);
				
				//Prefer를 그릴지 여부
				//FABRIK + KeepCurrent를 제외하곤 Prefer를 그리자
				if(portrait._IKMethod == apPortrait.IK_METHOD.CCD || bone._IKInitPoseType != apBone.IK_START_POSE.KeepCurrent)
				{
					DrawBoldLine(posW_Start, posW_Start + new Vector2(unitVector_Pref.x, unitVector_Pref.y), 3, Color.green, false);
				}
				
				
				//삭제 21.5.18
				//EndBatch();


			}

			//추가 20.5.24 : 지글 본의 각도 제한을 보여주자
			if(isDraw_Jiggle)
			{	
				Vector2 unitVector_Lower = Vector2.zero;
				Vector2 unitVector_Upper = Vector2.zero;

				//추가 20.8.8 : 스케일에 따라서 벡터의 방향이 바뀌어야 한다.
				if (is1AxisFlipped)
				{
					//한개의 축만 뒤집혀진 경우
					//부호와 Lower<->Upper가 반대이다.
					unitVector_Lower = apMatrix3x3.TRS(Vector2.zero, defaultAngle - bone._jiggle_AngleLimit_Max, Vector2.one).MultiplyPoint(unitVector);
					unitVector_Upper = apMatrix3x3.TRS(Vector2.zero, defaultAngle - bone._jiggle_AngleLimit_Min, Vector2.one).MultiplyPoint(unitVector);
				}
				else
				{
					//일반적인 경우
					unitVector_Lower = apMatrix3x3.TRS(Vector2.zero, defaultAngle + bone._jiggle_AngleLimit_Min, Vector2.one).MultiplyPoint(unitVector);
					unitVector_Upper = apMatrix3x3.TRS(Vector2.zero, defaultAngle + bone._jiggle_AngleLimit_Max, Vector2.one).MultiplyPoint(unitVector);
				}
				
				
				unitVector_Lower.Normalize();
				unitVector_Upper.Normalize();

				unitVector_Lower *= bone._shapeLength * worldMatrix.Scale.y * 1.4f;
				unitVector_Upper *= bone._shapeLength * worldMatrix.Scale.y * 1.4f;

				//BeginBatch_ColoredPolygon();
				_matBatch.BeginPass_Color(GL.TRIANGLES);//변경 21.5.18

				DrawBoldLine(posW_Start, posW_Start + new Vector2(unitVector_Lower.x, unitVector_Lower.y), 3, Color.yellow, false);
				DrawBoldLine(posW_Start, posW_Start + new Vector2(unitVector_Upper.x, unitVector_Upper.y), 3, Color.yellow, false);
				
				
				//EndBatch();//삭제 21.5.18
			}

			//if(bone._isIKtargetDebug)
			//{
			//	DrawBox(bone._calculatedIKTargetPosDebug, 30, 30, new Color(1.0f, 0.0f, 1.0f, 1.0f), false);
			//	int nBosDebug = bone._calculatedIKBonePosDebug.Count;
			//	for (int i = 0; i < nBosDebug; i++)
			//	{
			//		Color debugColor = (new Color(0.0f, 1.0f, 0.0f, 1.0f) * ((nBosDebug - 1)- i) + new Color(0.0f, 0.0f, 1.0f, 1.0f) * i) / (float)(nBosDebug - 1);

			//		DrawBox(bone._calculatedIKBonePosDebug[i], 20 + i * 5, 20 + i * 5, debugColor, false);
			//	}

			//}
		}


		//	본 그리기 > 버전 2 (바늘 형태)
		/// <summary>
		/// DrawBone_V2, DrawBoneOutline_V2 함수를 연속으로 사용할때는 Batch가 가능하다.
		/// Begin / End 함수를 이용하자.
		/// </summary>
		public static void BeginBatch_DrawBones_V2()
		{
			_matBatch.BeginPass_BoneV2(GL.TRIANGLES);
			//_matBatch.SetClippingSize(_glScreenClippingSize);

			//GL.Begin(GL.TRIANGLES);
		}

		

		public static void DrawBone_V2(	apBone bone, 
										bool isDrawOutline, 
										bool isBoneIKUsing, 
										bool isUseBoneToneColor, 
										bool isAvailable, 
										bool isNeedResetMat, 
										bool isTransculentRender)
		{
			if (bone == null)
			{
				return;
			}

			

			Color boneColor = bone._color;
			if(isUseBoneToneColor)
			{
				boneColor = _toneBoneColor;
			}
			else if(!isAvailable)
			{
				//추가 : 사용 불가능하다면 회색으로 보인다.
				boneColor = Color.gray;
			}
			else
			{
				//그 외의 모든 경우는 Bone 색상을 이용하되, Alpha는 상황에 따라 결정하자.
				if(isTransculentRender)
				{
					//반투명 옵션으로 렌더링 + 회색
					//Debug.Log("Transculent Render [" + bone._name + "]");
					boneColor = Color.gray;
					boneColor.a = 0.25f;
				}
				else
				{
					//그 외는 모두 불투명 렌더링
					boneColor.a = 1.0f;
				}
			}

			
			apMatrix worldMatrix = null;//이전
			//apBoneWorldMatrix worldMatrix = null;//변경 20.8.12 : CompleMatrix 방식
			

			Vector2 posW_Start = Vector2.zero;
			Vector2 posGL_Start = Vector2.zero;

			bool isHelperBone = bone._shapeHelper;

			if(!isHelperBone)
			{
				//헬퍼 본이 아닐때
				
				Vector2 posGL_Mid1 = Vector2.zero;
				Vector2 posGL_Mid2 = Vector2.zero;
				Vector2 posGL_End1 = Vector2.zero;
				Vector2 posGL_End2 = Vector2.zero;
				Vector2 posGL_Back1 = Vector2.zero;
				Vector2 posGL_Back2 = Vector2.zero;

				float uOffset = (isDrawOutline ? 0.25f : 0.0f);

				Vector2 uv_Back1 = new Vector2(0.25f + uOffset, 1.0f);
				Vector2 uv_Back2 = new Vector2(0.0f + uOffset, 1.0f);
				Vector2 uv_Mid1 = new Vector2(0.25f + uOffset, 0.9375f);
				Vector2 uv_Mid2 = new Vector2(0.0f + uOffset, 0.9375f);
				Vector2 uv_End1 = new Vector2(0.25f + uOffset, 0.0f);
				Vector2 uv_End2 = new Vector2(0.0f + uOffset, 0.0f);

				if (isBoneIKUsing)
				{
					//worldMatrix = bone._worldMatrix_IK;//이전
					worldMatrix = bone._guiMatrix_IK;//변경 20.8.23 : GUIMatrix IK 이용

					posW_Start = worldMatrix._pos;
					if(!isUseBoneToneColor)
					{
						//posGL_Start = apGL.World2GL(posW_Start);
						//posGL_Mid1 = apGL.World2GL(bone._shapePoints_V2_IK.Mid1);
						//posGL_Mid2 = apGL.World2GL(bone._shapePoints_V2_IK.Mid2);
						//posGL_End1 = apGL.World2GL(bone._shapePoints_V2_IK.End1);
						//posGL_End2 = apGL.World2GL(bone._shapePoints_V2_IK.End2);
						//posGL_Back1 = apGL.World2GL(bone._shapePoints_V2_IK.Back1);
						//posGL_Back2 = apGL.World2GL(bone._shapePoints_V2_IK.Back2);

						//변경 v1.4.4 : Ref 이용
						World2GL(ref posGL_Start, ref posW_Start);
						World2GL(ref posGL_Mid1, ref bone._shapePoints_V2_IK.Mid1);
						World2GL(ref posGL_Mid2, ref bone._shapePoints_V2_IK.Mid2);
						World2GL(ref posGL_End1, ref bone._shapePoints_V2_IK.End1);
						World2GL(ref posGL_End2, ref bone._shapePoints_V2_IK.End2);
						World2GL(ref posGL_Back1, ref bone._shapePoints_V2_IK.Back1);
						World2GL(ref posGL_Back2, ref bone._shapePoints_V2_IK.Back2);
					}
					else
					{
						//Onion Skin 전용 좌표 계산
						Vector2 deltaOnionPos = apGL._tonePosOffset * apGL._zoom;
						//Vector2 deltaOnionPos = apGL._tonePosOffset * 0.001f;
						//Vector2 deltaOnionPos = apGL._tonePosOffset;
					
						//posGL_Start = apGL.World2GL(posW_Start) + deltaOnionPos;
						//posGL_Mid1 = apGL.World2GL(bone._shapePoints_V2_IK.Mid1) + deltaOnionPos;
						//posGL_Mid2 = apGL.World2GL(bone._shapePoints_V2_IK.Mid2) + deltaOnionPos;
						//posGL_End1 = apGL.World2GL(bone._shapePoints_V2_IK.End1) + deltaOnionPos;
						//posGL_End2 = apGL.World2GL(bone._shapePoints_V2_IK.End2) + deltaOnionPos;
						//posGL_Back1 = apGL.World2GL(bone._shapePoints_V2_IK.Back1) + deltaOnionPos;
						//posGL_Back2 = apGL.World2GL(bone._shapePoints_V2_IK.Back2) + deltaOnionPos;

						//변경 v1.4.4 : Ref 이용
						World2GL(ref posGL_Start, ref posW_Start);
						World2GL(ref posGL_Mid1, ref bone._shapePoints_V2_IK.Mid1);
						World2GL(ref posGL_Mid2, ref bone._shapePoints_V2_IK.Mid2);
						World2GL(ref posGL_End1, ref bone._shapePoints_V2_IK.End1);
						World2GL(ref posGL_End2, ref bone._shapePoints_V2_IK.End2);
						World2GL(ref posGL_Back1, ref bone._shapePoints_V2_IK.Back1);
						World2GL(ref posGL_Back2, ref bone._shapePoints_V2_IK.Back2);
						posGL_Start += deltaOnionPos;
						posGL_Mid1 += deltaOnionPos;
						posGL_Mid2 += deltaOnionPos;
						posGL_End1 += deltaOnionPos;
						posGL_End2 += deltaOnionPos;
						posGL_Back1 += deltaOnionPos;
						posGL_Back2 += deltaOnionPos;
					}
				}
				else
				{
					//worldMatrix = bone._worldMatrix;
					worldMatrix = bone._guiMatrix;
					posW_Start = worldMatrix._pos;

					if (!isUseBoneToneColor)
					{
						//posGL_Start = apGL.World2GL(posW_Start);
						//posGL_Mid1 = apGL.World2GL(bone._shapePoints_V2_Normal.Mid1);
						//posGL_Mid2 = apGL.World2GL(bone._shapePoints_V2_Normal.Mid2);
						//posGL_End1 = apGL.World2GL(bone._shapePoints_V2_Normal.End1);
						//posGL_End2 = apGL.World2GL(bone._shapePoints_V2_Normal.End2);
						//posGL_Back1 = apGL.World2GL(bone._shapePoints_V2_Normal.Back1);
						//posGL_Back2 = apGL.World2GL(bone._shapePoints_V2_Normal.Back2);

						//v1.4.4 : Ref 이용
						World2GL(ref posGL_Start, ref posW_Start);
						World2GL(ref posGL_Mid1, ref bone._shapePoints_V2_Normal.Mid1);
						World2GL(ref posGL_Mid2, ref bone._shapePoints_V2_Normal.Mid2);
						World2GL(ref posGL_End1, ref bone._shapePoints_V2_Normal.End1);
						World2GL(ref posGL_End2, ref bone._shapePoints_V2_Normal.End2);
						World2GL(ref posGL_Back1, ref bone._shapePoints_V2_Normal.Back1);
						World2GL(ref posGL_Back2, ref bone._shapePoints_V2_Normal.Back2);
					}
					else
					{
						//Onion Skin 전용 좌표 계산
						Vector2 deltaOnionPos = apGL._tonePosOffset * apGL._zoom;
						//Vector2 deltaOnionPos = apGL._tonePosOffset * 0.001f;
						//Vector2 deltaOnionPos = apGL._tonePosOffset;

						//posGL_Start = apGL.World2GL(posW_Start) + deltaOnionPos;
						//posGL_Mid1 = apGL.World2GL(bone._shapePoints_V2_Normal.Mid1) + deltaOnionPos;
						//posGL_Mid2 = apGL.World2GL(bone._shapePoints_V2_Normal.Mid2) + deltaOnionPos;
						//posGL_End1 = apGL.World2GL(bone._shapePoints_V2_Normal.End1) + deltaOnionPos;
						//posGL_End2 = apGL.World2GL(bone._shapePoints_V2_Normal.End2) + deltaOnionPos;
						//posGL_Back1 = apGL.World2GL(bone._shapePoints_V2_Normal.Back1) + deltaOnionPos;
						//posGL_Back2 = apGL.World2GL(bone._shapePoints_V2_Normal.Back2) + deltaOnionPos;

						//v1.4.4 : Ref 이용
						World2GL(ref posGL_Start, ref posW_Start);
						World2GL(ref posGL_Mid1, ref bone._shapePoints_V2_Normal.Mid1);
						World2GL(ref posGL_Mid2, ref bone._shapePoints_V2_Normal.Mid2);
						World2GL(ref posGL_End1, ref bone._shapePoints_V2_Normal.End1);
						World2GL(ref posGL_End2, ref bone._shapePoints_V2_Normal.End2);
						World2GL(ref posGL_Back1, ref bone._shapePoints_V2_Normal.Back1);
						World2GL(ref posGL_Back2, ref bone._shapePoints_V2_Normal.Back2);
						
						posGL_Start += deltaOnionPos;
						posGL_Mid1 += deltaOnionPos;
						posGL_Mid2 += deltaOnionPos;
						posGL_End1 += deltaOnionPos;
						posGL_End2 += deltaOnionPos;
						posGL_Back1 += deltaOnionPos;
						posGL_Back2 += deltaOnionPos;
					}

				
				}

				//그려보자
				if (isNeedResetMat)
				{
					//변경 21.5.18
					_matBatch.BeginPass_BoneV2(GL.TRIANGLES);
					//_matBatch.SetClippingSize(_glScreenClippingSize);

					//GL.Begin(GL.TRIANGLES);
				}
				
				GL.Color(boneColor);

				//CCW
				GL.TexCoord(uv_Back1);	GL.Vertex(posGL_Back1);
				GL.TexCoord(uv_Back2);	GL.Vertex(posGL_Back2);
				GL.TexCoord(uv_Mid2);	GL.Vertex(posGL_Mid2);

				GL.TexCoord(uv_Mid2);	GL.Vertex(posGL_Mid2);
				GL.TexCoord(uv_Mid1);	GL.Vertex(posGL_Mid1);
				GL.TexCoord(uv_Back1);	GL.Vertex(posGL_Back1);

				GL.TexCoord(uv_Mid1);	GL.Vertex(posGL_Mid1);
				GL.TexCoord(uv_Mid2);	GL.Vertex(posGL_Mid2);
				GL.TexCoord(uv_End2);	GL.Vertex(posGL_End2);

				GL.TexCoord(uv_End2);	GL.Vertex(posGL_End2);
				GL.TexCoord(uv_End1);	GL.Vertex(posGL_End1);
				GL.TexCoord(uv_Mid1);	GL.Vertex(posGL_Mid1);

				//CW
				GL.TexCoord(uv_Back1);	GL.Vertex(posGL_Back1);
				GL.TexCoord(uv_Mid2);	GL.Vertex(posGL_Mid2);
				GL.TexCoord(uv_Back2);	GL.Vertex(posGL_Back2);

				GL.TexCoord(uv_Mid2);	GL.Vertex(posGL_Mid2);
				GL.TexCoord(uv_Back1);	GL.Vertex(posGL_Back1);
				GL.TexCoord(uv_Mid1);	GL.Vertex(posGL_Mid1);
				

				GL.TexCoord(uv_Mid1);	GL.Vertex(posGL_Mid1);
				GL.TexCoord(uv_End2);	GL.Vertex(posGL_End2);
				GL.TexCoord(uv_Mid2);	GL.Vertex(posGL_Mid2);
				

				GL.TexCoord(uv_End2);	GL.Vertex(posGL_End2);
				GL.TexCoord(uv_Mid1);	GL.Vertex(posGL_Mid1);
				GL.TexCoord(uv_End1);	GL.Vertex(posGL_End1);

				if(!isDrawOutline)
				{
					//외곽선 렌더링이 아닌 경우에만 원점 그리기
					//float orgRadius = bone._shapePoints_V2_Normal.Radius * Zoom;
					float radius_Org = apBone.RenderSetting_V2_Radius_Org * Zoom;
					Vector2 posGL_Org_LT = new Vector2(posGL_Start.x - radius_Org, posGL_Start.y + radius_Org);
					Vector2 posGL_Org_RT = new Vector2(posGL_Start.x + radius_Org, posGL_Start.y + radius_Org);
					Vector2 posGL_Org_LB = new Vector2(posGL_Start.x - radius_Org, posGL_Start.y - radius_Org);
					Vector2 posGL_Org_RB = new Vector2(posGL_Start.x + radius_Org, posGL_Start.y - radius_Org);

					Vector2 uv_Org_LT = new Vector2(0.75f, 1.0f);
					Vector2 uv_Org_RT = new Vector2(1.0f, 1.0f);
					Vector2 uv_Org_LB = new Vector2(0.75f, 0.875f);
					Vector2 uv_Org_RB = new Vector2(1.0f, 0.875f);

					//ORG
					GL.TexCoord(uv_Org_LT);	GL.Vertex(posGL_Org_LT);
					GL.TexCoord(uv_Org_LB);	GL.Vertex(posGL_Org_LB);
					GL.TexCoord(uv_Org_RB);	GL.Vertex(posGL_Org_RB);

					GL.TexCoord(uv_Org_RB);	GL.Vertex(posGL_Org_RB);
					GL.TexCoord(uv_Org_RT);	GL.Vertex(posGL_Org_RT);
					GL.TexCoord(uv_Org_LT);	GL.Vertex(posGL_Org_LT);
				}

				//삭제 21.5.18
				if (isNeedResetMat)
				{
					//GL.End();//<전환 완료>
					_matBatch.EndPass();
				}
			}
			else
			{
				//헬퍼본일때
				if (isBoneIKUsing)
				{
					//worldMatrix = bone._worldMatrix_IK;
					//posW_Start = worldMatrix.Pos;
					//GUIMatrix로 변경
					worldMatrix = bone._guiMatrix_IK;
					posW_Start = worldMatrix._pos;
				}
				else
				{
					//worldMatrix = bone._worldMatrix;
					//posW_Start = worldMatrix.Pos;
					//GUIMatrix로 변경
					worldMatrix = bone._guiMatrix;
					posW_Start = worldMatrix._pos;
				}

				if (!isUseBoneToneColor)
				{
					posGL_Start = apGL.World2GL(posW_Start);
				}
				else
				{
					//Onion Skin 전용 좌표 계산
					Vector2 deltaOnionPos = apGL._tonePosOffset * apGL._zoom;
					posGL_Start = apGL.World2GL(posW_Start) + deltaOnionPos;
				}
				//float orgRadius = bone._shapePoints_V2_Normal.Radius * Zoom;
				
				float radius_Helper = apBone.RenderSetting_V2_Radius_Helper * Zoom;

				

				Vector2 posGL_Helper_LT = new Vector2(posGL_Start.x - radius_Helper, posGL_Start.y + radius_Helper);
				Vector2 posGL_Helper_RT = new Vector2(posGL_Start.x + radius_Helper, posGL_Start.y + radius_Helper);
				Vector2 posGL_Helper_LB = new Vector2(posGL_Start.x - radius_Helper, posGL_Start.y - radius_Helper);
				Vector2 posGL_Helper_RB = new Vector2(posGL_Start.x + radius_Helper, posGL_Start.y - radius_Helper);

				
				
				float vOffset = (isDrawOutline ? -0.125f : 0.0f);

				Vector2 uv_Helper_LT = new Vector2(0.75f, 0.875f + vOffset);
				Vector2 uv_Helper_RT = new Vector2(1.0f, 0.875f + vOffset);
				Vector2 uv_Helper_LB = new Vector2(0.75f, 0.75f + vOffset);
				Vector2 uv_Helper_RB = new Vector2(1.0f, 0.75f + vOffset);

				//그려보자
				if (isNeedResetMat)
				{
					//변경 21.5.18
					_matBatch.BeginPass_BoneV2(GL.TRIANGLES);
					//_matBatch.SetClippingSize(_glScreenClippingSize);

					//GL.Begin(GL.TRIANGLES);
				}
				
				GL.Color(boneColor);

				//Helper
				GL.TexCoord(uv_Helper_LT);	GL.Vertex(posGL_Helper_LT);
				GL.TexCoord(uv_Helper_LB);	GL.Vertex(posGL_Helper_LB);
				GL.TexCoord(uv_Helper_RB);	GL.Vertex(posGL_Helper_RB);

				GL.TexCoord(uv_Helper_RB);	GL.Vertex(posGL_Helper_RB);
				GL.TexCoord(uv_Helper_RT);	GL.Vertex(posGL_Helper_RT);
				GL.TexCoord(uv_Helper_LT);	GL.Vertex(posGL_Helper_LT);

				if(!isDrawOutline)
				{
					//외곽선 렌더링이 아닌 경우에만 원점 그리기
					float radius_Org = apBone.RenderSetting_V2_Radius_Org * Zoom;

					Vector2 posGL_Org_LT = new Vector2(posGL_Start.x - radius_Org, posGL_Start.y + radius_Org);
					Vector2 posGL_Org_RT = new Vector2(posGL_Start.x + radius_Org, posGL_Start.y + radius_Org);
					Vector2 posGL_Org_LB = new Vector2(posGL_Start.x - radius_Org, posGL_Start.y - radius_Org);
					Vector2 posGL_Org_RB = new Vector2(posGL_Start.x + radius_Org, posGL_Start.y - radius_Org);

					Vector2 uv_Org_LT = new Vector2(0.75f, 1.0f);
					Vector2 uv_Org_RT = new Vector2(1.0f, 1.0f);
					Vector2 uv_Org_LB = new Vector2(0.75f, 0.875f);
					Vector2 uv_Org_RB = new Vector2(1.0f, 0.875f);

					//ORG
					GL.TexCoord(uv_Org_LT);	GL.Vertex(posGL_Org_LT);
					GL.TexCoord(uv_Org_LB);	GL.Vertex(posGL_Org_LB);
					GL.TexCoord(uv_Org_RB);	GL.Vertex(posGL_Org_RB);

					GL.TexCoord(uv_Org_RB);	GL.Vertex(posGL_Org_RB);
					GL.TexCoord(uv_Org_RT);	GL.Vertex(posGL_Org_RT);
					GL.TexCoord(uv_Org_LT);	GL.Vertex(posGL_Org_LT);
				}

				//삭제 21.5.18
				if (isNeedResetMat)
				{
					//GL.End();//<전환 완료>
					_matBatch.EndPass();
				}
			}
		}

		public static void DrawSelectedBone_V2(apBone bone, BONE_SELECTED_OUTLINE_COLOR outlineColor, bool isBoneIKUsing = false)
		{
			//본 그리기 + 두꺼운 외곽선
			//+ IK 속성이 있는 경우, GUI에 표시하자
			if (bone == null)
			{
				return;
			}

			Color lineColor;


			Color lineColor_Default = Color.black;
			Color lineColor_Reserve = Color.black;

			switch (outlineColor)
			{
				case BONE_SELECTED_OUTLINE_COLOR.MainSelected:
				case BONE_SELECTED_OUTLINE_COLOR.SubSelected:
					lineColor_Default = _lineColor_BoneOutline_V2_Default;
					lineColor_Reserve = _lineColor_BoneOutline_V2_Reverse;
					break;

				case BONE_SELECTED_OUTLINE_COLOR.LinkTarget:
					lineColor_Default = _lineColor_BoneOutlineRollOver_V2_Default;
					lineColor_Reserve = _lineColor_BoneOutlineRollOver_V2_Reverse;
					break;
			}

			float diff_Default = GetColorDif(bone._color, lineColor_Default);
			//float diff_Reverse = GetColorDif(bone._color, lineColor_Reserve);

			//if(diff_Default * COLOR_DEFAULT_BIAS > diff_Reverse)
			if(diff_Default > COLOR_SIMILAR_BIAS)
			{
				//Default 색상이 더 차이가 크다.
				lineColor = lineColor_Default;
			}
			else
			{
				//Reserve 색상이 더 차이가 크다.
				lineColor = lineColor_Reserve;
			}

			lineColor.a = _animRatio_BoneOutlineAlpha;

			
			apMatrix worldMatrix = null;//이전
			//apBoneWorldMatrix worldMatrix = null;//변경 20.8.12 : ComplexMatrix로 변경



			Vector2 posW_Start = Vector2.zero;
			Vector2 posGL_Start = Vector2.zero;

			bool isHelperBone = bone._shapeHelper;

			if(!isHelperBone)
			{
				//헬퍼 본이 아닐때
				Vector2 posGL_Mid1 = Vector2.zero;
				Vector2 posGL_Mid2 = Vector2.zero;
				Vector2 posGL_End1 = Vector2.zero;
				Vector2 posGL_End2 = Vector2.zero;
				Vector2 posGL_Back1 = Vector2.zero;
				Vector2 posGL_Back2 = Vector2.zero;

				Vector2 uv_Outline_Back1 = new Vector2(0.75f, 1.0f);
				Vector2 uv_Outline_Back2 = new Vector2(0.5f, 1.0f);
				Vector2 uv_Outline_Mid1 = new Vector2(0.75f, 0.9375f);
				Vector2 uv_Outline_Mid2 = new Vector2(0.5f, 0.9375f);
				Vector2 uv_Outline_End1 = new Vector2(0.75f, 0.0f);
				Vector2 uv_Outline_End2 = new Vector2(0.5f, 0.0f);

				if (isBoneIKUsing)
				{
					//worldMatrix = bone._worldMatrix_IK;
					//posW_Start = worldMatrix.Pos;

					//GUI Matrix로 변경 (20.8.23)
					worldMatrix = bone._guiMatrix_IK;
					posW_Start = worldMatrix._pos;
					
					//posGL_Start = apGL.World2GL(posW_Start);
					//posGL_Mid1 = apGL.World2GL(bone._shapePoints_V2_IK.Mid1);
					//posGL_Mid2 = apGL.World2GL(bone._shapePoints_V2_IK.Mid2);
					//posGL_End1 = apGL.World2GL(bone._shapePoints_V2_IK.End1);
					//posGL_End2 = apGL.World2GL(bone._shapePoints_V2_IK.End2);
					//posGL_Back1 = apGL.World2GL(bone._shapePoints_V2_IK.Back1);
					//posGL_Back2 = apGL.World2GL(bone._shapePoints_V2_IK.Back2);

					//v1.4.4 : Ref 이용
					World2GL(ref posGL_Start, ref posW_Start);
					World2GL(ref posGL_Mid1, ref bone._shapePoints_V2_IK.Mid1);
					World2GL(ref posGL_Mid2, ref bone._shapePoints_V2_IK.Mid2);
					World2GL(ref posGL_End1, ref bone._shapePoints_V2_IK.End1);
					World2GL(ref posGL_End2, ref bone._shapePoints_V2_IK.End2);
					World2GL(ref posGL_Back1, ref bone._shapePoints_V2_IK.Back1);
					World2GL(ref posGL_Back2, ref bone._shapePoints_V2_IK.Back2);
				}
				else
				{
					//worldMatrix = bone._worldMatrix;
					//posW_Start = worldMatrix.Pos;

					//GUI Matrix로 변경 (20.8.23)
					worldMatrix = bone._guiMatrix;
					posW_Start = worldMatrix._pos;

					//posGL_Start = apGL.World2GL(posW_Start);
					//posGL_Mid1 = apGL.World2GL(bone._shapePoints_V2_Normal.Mid1);
					//posGL_Mid2 = apGL.World2GL(bone._shapePoints_V2_Normal.Mid2);
					//posGL_End1 = apGL.World2GL(bone._shapePoints_V2_Normal.End1);
					//posGL_End2 = apGL.World2GL(bone._shapePoints_V2_Normal.End2);
					//posGL_Back1 = apGL.World2GL(bone._shapePoints_V2_Normal.Back1);
					//posGL_Back2 = apGL.World2GL(bone._shapePoints_V2_Normal.Back2);

					//v1.4.4 : Ref 이용
					World2GL(ref posGL_Start, ref posW_Start);
					World2GL(ref posGL_Mid1, ref bone._shapePoints_V2_Normal.Mid1);
					World2GL(ref posGL_Mid2, ref bone._shapePoints_V2_Normal.Mid2);
					World2GL(ref posGL_End1, ref bone._shapePoints_V2_Normal.End1);
					World2GL(ref posGL_End2, ref bone._shapePoints_V2_Normal.End2);
					World2GL(ref posGL_Back1, ref bone._shapePoints_V2_Normal.Back1);
					World2GL(ref posGL_Back2, ref bone._shapePoints_V2_Normal.Back2);
				}

				
				//그려보자
				//변경 21.5.18
				_matBatch.BeginPass_BoneV2(GL.TRIANGLES);
				//_matBatch.SetClippingSize(_glScreenClippingSize);

				//GL.Begin(GL.TRIANGLES);


				GL.Color(lineColor);

				GL.TexCoord(uv_Outline_Back1);	GL.Vertex(posGL_Back1);
				GL.TexCoord(uv_Outline_Back2);	GL.Vertex(posGL_Back2);
				GL.TexCoord(uv_Outline_Mid2);	GL.Vertex(posGL_Mid2);

				GL.TexCoord(uv_Outline_Mid2);	GL.Vertex(posGL_Mid2);
				GL.TexCoord(uv_Outline_Mid1);	GL.Vertex(posGL_Mid1);
				GL.TexCoord(uv_Outline_Back1);	GL.Vertex(posGL_Back1);

				GL.TexCoord(uv_Outline_Mid1);	GL.Vertex(posGL_Mid1);
				GL.TexCoord(uv_Outline_Mid2);	GL.Vertex(posGL_Mid2);
				GL.TexCoord(uv_Outline_End2);	GL.Vertex(posGL_End2);

				GL.TexCoord(uv_Outline_End2);	GL.Vertex(posGL_End2);
				GL.TexCoord(uv_Outline_End1);	GL.Vertex(posGL_End1);
				GL.TexCoord(uv_Outline_Mid1);	GL.Vertex(posGL_Mid1);

				//CW
				GL.TexCoord(uv_Outline_Back1);	GL.Vertex(posGL_Back1);
				GL.TexCoord(uv_Outline_Mid2);	GL.Vertex(posGL_Mid2);
				GL.TexCoord(uv_Outline_Back2);	GL.Vertex(posGL_Back2);

				GL.TexCoord(uv_Outline_Mid2);	GL.Vertex(posGL_Mid2);
				GL.TexCoord(uv_Outline_Back1);	GL.Vertex(posGL_Back1);
				GL.TexCoord(uv_Outline_Mid1);	GL.Vertex(posGL_Mid1);
				

				GL.TexCoord(uv_Outline_Mid1);	GL.Vertex(posGL_Mid1);
				GL.TexCoord(uv_Outline_End2);	GL.Vertex(posGL_End2);
				GL.TexCoord(uv_Outline_Mid2);	GL.Vertex(posGL_Mid2);
				

				GL.TexCoord(uv_Outline_End2);	GL.Vertex(posGL_End2);
				GL.TexCoord(uv_Outline_Mid1);	GL.Vertex(posGL_Mid1);
				GL.TexCoord(uv_Outline_End1);	GL.Vertex(posGL_End1);
				
				
				//삭제
				//GL.End();//<나중에 일괄 EndPass>
			}
			else
			{
				//헬퍼본일때
				if (isBoneIKUsing)
				{
					//worldMatrix = bone._worldMatrix_IK;
					//posW_Start = worldMatrix.Pos;

					//GUI Matrix로 변경 (20.8.23)
					worldMatrix = bone._guiMatrix_IK;
					posW_Start = worldMatrix._pos;
				}
				else
				{
					//worldMatrix = bone._worldMatrix;
					//posW_Start = worldMatrix.Pos;

					//GUI Matrix로 변경 (20.8.23)
					worldMatrix = bone._guiMatrix;
					posW_Start = worldMatrix._pos;
				}

				posGL_Start = apGL.World2GL(posW_Start);

				//float orgRadius = bone._shapePoints_V2_Normal.Radius * Zoom;
				float radius_Helper = apBone.RenderSetting_V2_Radius_Helper * Zoom;

				Vector2 posGL_Helper_LT = new Vector2(posGL_Start.x - radius_Helper, posGL_Start.y + radius_Helper);
				Vector2 posGL_Helper_RT = new Vector2(posGL_Start.x + radius_Helper, posGL_Start.y + radius_Helper);
				Vector2 posGL_Helper_LB = new Vector2(posGL_Start.x - radius_Helper, posGL_Start.y - radius_Helper);
				Vector2 posGL_Helper_RB = new Vector2(posGL_Start.x + radius_Helper, posGL_Start.y - radius_Helper);

				Vector2 uv_Helper_LT = new Vector2(0.75f, 0.625f);
				Vector2 uv_Helper_RT = new Vector2(1.0f, 0.625f);
				Vector2 uv_Helper_LB = new Vector2(0.75f, 0.5f);
				Vector2 uv_Helper_RB = new Vector2(1.0f, 0.5f);

				//그려보자
				//변경 21.5.18
				_matBatch.BeginPass_BoneV2(GL.TRIANGLES);
				//_matBatch.SetClippingSize(_glScreenClippingSize);

				//GL.Begin(GL.TRIANGLES);


				GL.Color(lineColor);

				//Helper
				GL.TexCoord(uv_Helper_LT);	GL.Vertex(posGL_Helper_LT);
				GL.TexCoord(uv_Helper_LB);	GL.Vertex(posGL_Helper_LB);
				GL.TexCoord(uv_Helper_RB);	GL.Vertex(posGL_Helper_RB);

				GL.TexCoord(uv_Helper_RB);	GL.Vertex(posGL_Helper_RB);
				GL.TexCoord(uv_Helper_RT);	GL.Vertex(posGL_Helper_RT);
				GL.TexCoord(uv_Helper_LT);	GL.Vertex(posGL_Helper_LT);
				
				//삭제 21.5.18
				//GL.End();//<나중에 일괄 EndPass>
			}

			//추가 : IK 속성이 있는 경우, GUI에 표시하자
			if (bone._IKTargetBone != null)
			{
				Vector2 IKTargetPos = Vector2.zero;
				if (isBoneIKUsing)
				{
					//IKTargetPos = World2GL(bone._IKTargetBone._worldMatrix_IK._pos);
					IKTargetPos = World2GL(bone._IKTargetBone._worldMatrix_IK.Pos);
				}
				else
				{
					//IKTargetPos = World2GL(bone._IKTargetBone._worldMatrix._pos);
					IKTargetPos = World2GL(bone._IKTargetBone._worldMatrix.Pos);
				}
				DrawAnimatedLineGL(posGL_Start, IKTargetPos, Color.magenta, true);
			}

			if (bone._IKHeaderBone != null)
			{
				Vector2 IKHeadPos = Vector2.zero;
				if (isBoneIKUsing)
				{
					IKHeadPos = World2GL(bone._IKHeaderBone._worldMatrix_IK.Pos);
				}
				else
				{
					IKHeadPos = World2GL(bone._IKHeaderBone._worldMatrix.Pos);
				}
				DrawAnimatedLineGL(IKHeadPos, posGL_Start, Color.magenta, true);
			}

			if(bone._IKController._controllerType != apBoneIKController.CONTROLLER_TYPE.None)
			{
				if(bone._IKController._effectorBone != null)
				{
					Color lineColorIK = Color.yellow;
					if(bone._IKController._controllerType == apBoneIKController.CONTROLLER_TYPE.LookAt)
					{
						lineColorIK = Color.cyan;
					}
					Vector2 effectorPos = Vector2.zero;
					if(isBoneIKUsing)
					{
						effectorPos = World2GL(bone._IKController._effectorBone._worldMatrix_IK.Pos);
					}
					else
					{
						effectorPos = World2GL(bone._IKController._effectorBone._worldMatrix.Pos);
					}
					DrawAnimatedLineGL(posGL_Start, effectorPos, lineColorIK, true);
				}
			}
		}

		public static void DrawBoneOutline_V2(apBone bone, Color outlineColor, bool isBoneIKUsing, bool isNeedResetMat)
		{
			if (bone == null)
			{
				return;
			}
			
			apMatrix worldMatrix = null;//이전
			//apBoneWorldMatrix worldMatrix = null;//변경 20.8.12 : ComplexMatrix로 변경


			Vector2 posW_Start = Vector2.zero;
			Vector2 posGL_Start = Vector2.zero;

			bool isHelperBone = bone._shapeHelper;

			if(!isHelperBone)
			{
				//헬퍼 본이 아닐때
				
				Vector2 posGL_Mid1 = Vector2.zero;
				Vector2 posGL_Mid2 = Vector2.zero;
				Vector2 posGL_End1 = Vector2.zero;
				Vector2 posGL_End2 = Vector2.zero;
				Vector2 posGL_Back1 = Vector2.zero;
				Vector2 posGL_Back2 = Vector2.zero;

				//Outline (비선택)
				Vector2 uv_Back1 = new Vector2(0.5f, 1.0f);
				Vector2 uv_Back2 = new Vector2(0.25f, 1.0f);
				Vector2 uv_Mid1 = new Vector2(0.5f, 0.9375f);
				Vector2 uv_Mid2 = new Vector2(0.25f, 0.9375f);
				Vector2 uv_End1 = new Vector2(0.5f, 0.0f);
				Vector2 uv_End2 = new Vector2(0.25f, 0.0f);

				if (isBoneIKUsing)
				{
					//worldMatrix = bone._worldMatrix_IK;
					//posW_Start = worldMatrix.Pos;

					//GUI Matrix로 변경 (20.8.23)
					worldMatrix = bone._guiMatrix_IK;
					posW_Start = worldMatrix._pos;

					//posGL_Start = apGL.World2GL(posW_Start);
					//posGL_Mid1 = apGL.World2GL(bone._shapePoints_V2_IK.Mid1);
					//posGL_Mid2 = apGL.World2GL(bone._shapePoints_V2_IK.Mid2);
					//posGL_End1 = apGL.World2GL(bone._shapePoints_V2_IK.End1);
					//posGL_End2 = apGL.World2GL(bone._shapePoints_V2_IK.End2);
					//posGL_Back1 = apGL.World2GL(bone._shapePoints_V2_IK.Back1);
					//posGL_Back2 = apGL.World2GL(bone._shapePoints_V2_IK.Back2);

					//v1.4.4 : Ref 이용
					World2GL(ref posGL_Start, ref posW_Start);
					World2GL(ref posGL_Mid1, ref bone._shapePoints_V2_IK.Mid1);
					World2GL(ref posGL_Mid2, ref bone._shapePoints_V2_IK.Mid2);
					World2GL(ref posGL_End1, ref bone._shapePoints_V2_IK.End1);
					World2GL(ref posGL_End2, ref bone._shapePoints_V2_IK.End2);
					World2GL(ref posGL_Back1, ref bone._shapePoints_V2_IK.Back1);
					World2GL(ref posGL_Back2, ref bone._shapePoints_V2_IK.Back2);
				}
				else
				{
					//worldMatrix = bone._worldMatrix;
					//posW_Start = worldMatrix.Pos;

					//GUI Matrix로 변경 (20.8.23)
					worldMatrix = bone._guiMatrix;
					posW_Start = worldMatrix._pos;

					//posGL_Start = apGL.World2GL(posW_Start);
					//posGL_Mid1 = apGL.World2GL(bone._shapePoints_V2_Normal.Mid1);
					//posGL_Mid2 = apGL.World2GL(bone._shapePoints_V2_Normal.Mid2);
					//posGL_End1 = apGL.World2GL(bone._shapePoints_V2_Normal.End1);
					//posGL_End2 = apGL.World2GL(bone._shapePoints_V2_Normal.End2);
					//posGL_Back1 = apGL.World2GL(bone._shapePoints_V2_Normal.Back1);
					//posGL_Back2 = apGL.World2GL(bone._shapePoints_V2_Normal.Back2);

					//v1.4.4 : Ref 이용
					World2GL(ref posGL_Start, ref posW_Start);
					World2GL(ref posGL_Mid1, ref bone._shapePoints_V2_Normal.Mid1);
					World2GL(ref posGL_Mid2, ref bone._shapePoints_V2_Normal.Mid2);
					World2GL(ref posGL_End1, ref bone._shapePoints_V2_Normal.End1);
					World2GL(ref posGL_End2, ref bone._shapePoints_V2_Normal.End2);
					World2GL(ref posGL_Back1, ref bone._shapePoints_V2_Normal.Back1);
					World2GL(ref posGL_Back2, ref bone._shapePoints_V2_Normal.Back2);
				}

				//그려보자
				if (isNeedResetMat)
				{
					//변경 21.5.18
					_matBatch.BeginPass_BoneV2(GL.TRIANGLES);
					//_matBatch.SetClippingSize(_glScreenClippingSize);

					//GL.Begin(GL.TRIANGLES);
				}
				
				GL.Color(outlineColor);

				//CCW
				GL.TexCoord(uv_Back1);	GL.Vertex(posGL_Back1);
				GL.TexCoord(uv_Back2);	GL.Vertex(posGL_Back2);
				GL.TexCoord(uv_Mid2);	GL.Vertex(posGL_Mid2);

				GL.TexCoord(uv_Mid2);	GL.Vertex(posGL_Mid2);
				GL.TexCoord(uv_Mid1);	GL.Vertex(posGL_Mid1);
				GL.TexCoord(uv_Back1);	GL.Vertex(posGL_Back1);

				GL.TexCoord(uv_Mid1);	GL.Vertex(posGL_Mid1);
				GL.TexCoord(uv_Mid2);	GL.Vertex(posGL_Mid2);
				GL.TexCoord(uv_End2);	GL.Vertex(posGL_End2);

				GL.TexCoord(uv_End2);	GL.Vertex(posGL_End2);
				GL.TexCoord(uv_End1);	GL.Vertex(posGL_End1);
				GL.TexCoord(uv_Mid1);	GL.Vertex(posGL_Mid1);

				//CW
				GL.TexCoord(uv_Back1);	GL.Vertex(posGL_Back1);
				GL.TexCoord(uv_Mid2);	GL.Vertex(posGL_Mid2);
				GL.TexCoord(uv_Back2);	GL.Vertex(posGL_Back2);

				GL.TexCoord(uv_Mid2);	GL.Vertex(posGL_Mid2);
				GL.TexCoord(uv_Back1);	GL.Vertex(posGL_Back1);
				GL.TexCoord(uv_Mid1);	GL.Vertex(posGL_Mid1);
				

				GL.TexCoord(uv_Mid1);	GL.Vertex(posGL_Mid1);
				GL.TexCoord(uv_End2);	GL.Vertex(posGL_End2);
				GL.TexCoord(uv_Mid2);	GL.Vertex(posGL_Mid2);
				

				GL.TexCoord(uv_End2);	GL.Vertex(posGL_End2);
				GL.TexCoord(uv_Mid1);	GL.Vertex(posGL_Mid1);
				GL.TexCoord(uv_End1);	GL.Vertex(posGL_End1);

				//삭제 21.5.18
				if (isNeedResetMat)
				{
					//GL.End();//<전환 완료>
					_matBatch.EndPass();
				}
			}
			else
			{
				//헬퍼본일때
				if (isBoneIKUsing)
				{
					//worldMatrix = bone._worldMatrix_IK;
					//posW_Start = worldMatrix.Pos;

					//GUI Matrix로 변경 (20.8.23)
					worldMatrix = bone._guiMatrix_IK;
					posW_Start = worldMatrix._pos;
				}
				else
				{
					//worldMatrix = bone._worldMatrix;
					//posW_Start = worldMatrix.Pos;

					//GUI Matrix로 변경 (20.8.23)
					worldMatrix = bone._guiMatrix;
					posW_Start = worldMatrix._pos;
				}

				posGL_Start = apGL.World2GL(posW_Start);

				//float orgRadius = bone._shapePoints_V2_Normal.Radius * Zoom;
				float radius_Helper = apBone.RenderSetting_V2_Radius_Helper * Zoom;

				Vector2 posGL_Helper_LT = new Vector2(posGL_Start.x - radius_Helper, posGL_Start.y + radius_Helper);
				Vector2 posGL_Helper_RT = new Vector2(posGL_Start.x + radius_Helper, posGL_Start.y + radius_Helper);
				Vector2 posGL_Helper_LB = new Vector2(posGL_Start.x - radius_Helper, posGL_Start.y - radius_Helper);
				Vector2 posGL_Helper_RB = new Vector2(posGL_Start.x + radius_Helper, posGL_Start.y - radius_Helper);

				//Outline (비선택)
				Vector2 uv_Helper_LT = new Vector2(0.75f, 0.75f);
				Vector2 uv_Helper_RT = new Vector2(1.0f, 0.75f);
				Vector2 uv_Helper_LB = new Vector2(0.75f, 0.625f);
				Vector2 uv_Helper_RB = new Vector2(1.0f, 0.625f);

				//그려보자
				if (isNeedResetMat)
				{
					//변경 21.5.18
					_matBatch.BeginPass_BoneV2(GL.TRIANGLES);
					//_matBatch.SetClippingSize(_glScreenClippingSize);

					//GL.Begin(GL.TRIANGLES);
				}
				
				GL.Color(outlineColor);

				//Helper
				GL.TexCoord(uv_Helper_LT);	GL.Vertex(posGL_Helper_LT);
				GL.TexCoord(uv_Helper_LB);	GL.Vertex(posGL_Helper_LB);
				GL.TexCoord(uv_Helper_RB);	GL.Vertex(posGL_Helper_RB);

				GL.TexCoord(uv_Helper_RB);	GL.Vertex(posGL_Helper_RB);
				GL.TexCoord(uv_Helper_RT);	GL.Vertex(posGL_Helper_RT);
				GL.TexCoord(uv_Helper_LT);	GL.Vertex(posGL_Helper_LT);

				//삭제 21.5.18
				if (isNeedResetMat)
				{
					//GL.End();//<전환 완료>
					_matBatch.EndPass();
				}
			}
		}



		//본은 아니지만 시작>끝>Width를 계산하여 가상으로 본 그리기
		public static void DrawBone_Virtual_V2(	Vector2 startPosW,
												Vector2 endPosW,
												Color boneColor,
												bool isNeedResetMat)
		{	
			float length = (endPosW - startPosW).magnitude;
			float angle = 0.0f;

			if(length > 0.0f)
			{
				angle = Mathf.Atan2(endPosW.y - startPosW.y, endPosW.x - startPosW.x) * Mathf.Rad2Deg;
				angle += 90.0f;
			}

			angle += 180.0f;
			angle = apUtil.AngleTo180(angle);

			if(_cal_TmpMatrix == null)
			{
				_cal_TmpMatrix = new apMatrix();
			}
			_cal_TmpMatrix.SetIdentity();
			_cal_TmpMatrix.SetTRS(startPosW, angle, Vector2.one, true);
			
			//실제 Bone Length, Width를 계산하자
			//V2 에선 메시 여백때문에 약간 더 길어야 한다.
			float boneLength = length * apBone.BONE_V2_REAL_LENGTH_RATIO;
			float boneWidthHalf = apBone.RenderSetting_V2_WidthHalf;
			
			//이 계산+코드는 apBone의 GUIUpdate 함수 중 V2 코드를 참고한다.
			//이전			
			//Vector2 bonePos_End1 =	apGL.World2GL(_cal_TmpMatrix.MulPoint2(new Vector2(-boneWidthHalf,	boneLength)));
			//Vector2 bonePos_End2 =	apGL.World2GL(_cal_TmpMatrix.MulPoint2(new Vector2(boneWidthHalf,	boneLength)));
			//Vector2 bonePos_Back1 = apGL.World2GL(_cal_TmpMatrix.MulPoint2(new Vector2(-boneWidthHalf,	-boneWidthHalf)));
			//Vector2 bonePos_Back2 =	apGL.World2GL(_cal_TmpMatrix.MulPoint2(new Vector2(boneWidthHalf,	-boneWidthHalf)));
			//Vector2 bonePos_Mid1 =	apGL.World2GL(_cal_TmpMatrix.MulPoint2(new Vector2(-boneWidthHalf,	0.0f)));
			//Vector2 bonePos_Mid2 =	apGL.World2GL(_cal_TmpMatrix.MulPoint2(new Vector2(boneWidthHalf,	0.0f)));

			//변경 v1.4.4 : Ref 이용
			_tmp_BonePos_Local_1 = new Vector2(-boneWidthHalf,	boneLength);
			_tmp_BonePos_Local_2 = new Vector2(boneWidthHalf,	boneLength);
			_tmp_BonePos_Local_3 = new Vector2(-boneWidthHalf,	-boneWidthHalf);
			_tmp_BonePos_Local_4 = new Vector2(boneWidthHalf,	-boneWidthHalf);
			_tmp_BonePos_Local_5 = new Vector2(-boneWidthHalf,	0.0f);
			_tmp_BonePos_Local_6 = new Vector2(boneWidthHalf,	0.0f);

			_cal_TmpMatrix.MulPoint2(ref _tmp_BonePos_World_1, ref _tmp_BonePos_Local_1);
			_cal_TmpMatrix.MulPoint2(ref _tmp_BonePos_World_2, ref _tmp_BonePos_Local_2);
			_cal_TmpMatrix.MulPoint2(ref _tmp_BonePos_World_3, ref _tmp_BonePos_Local_3);
			_cal_TmpMatrix.MulPoint2(ref _tmp_BonePos_World_4, ref _tmp_BonePos_Local_4);
			_cal_TmpMatrix.MulPoint2(ref _tmp_BonePos_World_5, ref _tmp_BonePos_Local_5);
			_cal_TmpMatrix.MulPoint2(ref _tmp_BonePos_World_6, ref _tmp_BonePos_Local_6);

			Vector2 bonePos_End1 =	apGL.World2GL(_tmp_BonePos_World_1);
			Vector2 bonePos_End2 =	apGL.World2GL(_tmp_BonePos_World_2);
			Vector2 bonePos_Back1 = apGL.World2GL(_tmp_BonePos_World_3);
			Vector2 bonePos_Back2 =	apGL.World2GL(_tmp_BonePos_World_4);
			Vector2 bonePos_Mid1 =	apGL.World2GL(_tmp_BonePos_World_5);
			Vector2 bonePos_Mid2 =	apGL.World2GL(_tmp_BonePos_World_6);


			
			Vector2 uv_Back1 = new Vector2(0.25f, 1.0f);
			Vector2 uv_Back2 = new Vector2(0.0f, 1.0f);
			Vector2 uv_Mid1 = new Vector2(0.25f, 0.9375f);
			Vector2 uv_Mid2 = new Vector2(0.0f, 0.9375f);
			Vector2 uv_End1 = new Vector2(0.25f, 0.0f);
			Vector2 uv_End2 = new Vector2(0.0f, 0.0f);

			//그려보자
			if (isNeedResetMat)
			{
				//변경 21.5.18
				_matBatch.BeginPass_BoneV2(GL.TRIANGLES);
			}
				
			GL.Color(boneColor);

			//CCW
			GL.TexCoord(uv_Back1);	GL.Vertex(bonePos_Back1);
			GL.TexCoord(uv_Back2);	GL.Vertex(bonePos_Back2);
			GL.TexCoord(uv_Mid2);	GL.Vertex(bonePos_Mid2);

			GL.TexCoord(uv_Mid2);	GL.Vertex(bonePos_Mid2);
			GL.TexCoord(uv_Mid1);	GL.Vertex(bonePos_Mid1);
			GL.TexCoord(uv_Back1);	GL.Vertex(bonePos_Back1);

			GL.TexCoord(uv_Mid1);	GL.Vertex(bonePos_Mid1);
			GL.TexCoord(uv_Mid2);	GL.Vertex(bonePos_Mid2);
			GL.TexCoord(uv_End2);	GL.Vertex(bonePos_End2);

			GL.TexCoord(uv_End2);	GL.Vertex(bonePos_End2);
			GL.TexCoord(uv_End1);	GL.Vertex(bonePos_End1);
			GL.TexCoord(uv_Mid1);	GL.Vertex(bonePos_Mid1);

			//CW
			GL.TexCoord(uv_Back1);	GL.Vertex(bonePos_Back1);
			GL.TexCoord(uv_Mid2);	GL.Vertex(bonePos_Mid2);
			GL.TexCoord(uv_Back2);	GL.Vertex(bonePos_Back2);

			GL.TexCoord(uv_Mid2);	GL.Vertex(bonePos_Mid2);
			GL.TexCoord(uv_Back1);	GL.Vertex(bonePos_Back1);
			GL.TexCoord(uv_Mid1);	GL.Vertex(bonePos_Mid1);

			GL.TexCoord(uv_Mid1);	GL.Vertex(bonePos_Mid1);
			GL.TexCoord(uv_End2);	GL.Vertex(bonePos_End2);
			GL.TexCoord(uv_Mid2);	GL.Vertex(bonePos_Mid2);

			GL.TexCoord(uv_End2);	GL.Vertex(bonePos_End2);
			GL.TexCoord(uv_Mid1);	GL.Vertex(bonePos_Mid1);
			GL.TexCoord(uv_End1);	GL.Vertex(bonePos_End1);

			//외곽선 렌더링이 아닌 경우에만 원점 그리기
			//float orgRadius = bone._shapePoints_V2_Normal.Radius * Zoom;
			float radius_Org = apBone.RenderSetting_V2_Radius_Org * Zoom;


			Vector2 startPosGL = World2GL(startPosW);
			Vector2 posGL_Org_LT = new Vector2(startPosGL.x - radius_Org, startPosGL.y + radius_Org);
			Vector2 posGL_Org_RT = new Vector2(startPosGL.x + radius_Org, startPosGL.y + radius_Org);
			Vector2 posGL_Org_LB = new Vector2(startPosGL.x - radius_Org, startPosGL.y - radius_Org);
			Vector2 posGL_Org_RB = new Vector2(startPosGL.x + radius_Org, startPosGL.y - radius_Org);

			Vector2 uv_Org_LT = new Vector2(0.75f, 1.0f);
			Vector2 uv_Org_RT = new Vector2(1.0f, 1.0f);
			Vector2 uv_Org_LB = new Vector2(0.75f, 0.875f);
			Vector2 uv_Org_RB = new Vector2(1.0f, 0.875f);

			//ORG
			GL.TexCoord(uv_Org_LT);	GL.Vertex(posGL_Org_LT);
			GL.TexCoord(uv_Org_LB);	GL.Vertex(posGL_Org_LB);
			GL.TexCoord(uv_Org_RB);	GL.Vertex(posGL_Org_RB);

			GL.TexCoord(uv_Org_RB);	GL.Vertex(posGL_Org_RB);
			GL.TexCoord(uv_Org_RT);	GL.Vertex(posGL_Org_RT);
			GL.TexCoord(uv_Org_LT);	GL.Vertex(posGL_Org_LT);

			if (isNeedResetMat)
			{
				_matBatch.EndPass();
			}

				
			
		}
	}
}
