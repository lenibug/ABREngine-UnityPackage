/* SimpleLineDataImpression.cs
 *
 * Copyright (c) 2021 University of Minnesota
 * Authors: Bridger Herman <herma582@umn.edu>, Seth Johnson <sethalanjohnson@gmail.com>
 *
 */

using System.Reflection;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

using IVLab.Utilities.GenericObjectPool;

namespace IVLab.ABREngine
{
    class SimpleLineRenderInfo : IDataImpressionRenderInfo
    {
        public Vector3[][] vertices;
        public int[][] indices;
        public Vector3[][] normals;
        public Color[][] scalars;
        public Vector2[][] uvs;

        public Vector4 scalarMin;
        public Vector4 scalarMax;
    }

    [ABRPlateType("Ribbons")]
    public class SimpleLineDataImpression : DataImpression, IDataImpression, IHasDataset
    {
        [ABRInput("Key Data", "Key Data")]
        public LineKeyData keyData;

        [ABRInput("Color Variable", "Color")]
        public ScalarDataVariable colorVariable;

        [ABRInput("Colormap", "Color")]
        public ColormapVisAsset colormap;


        [ABRInput("Texture Variable", "Texture")]
        public ScalarDataVariable lineTextureVariable;

        [ABRInput("Texture", "Texture")]
        public LineTextureVisAsset lineTexture;

        [ABRInput("Texture Cutoff", "Texture")]
        public PercentPrimitive textureCutoff;


        [ABRInput("Ribbon Smooth", "Ribbon")]
        public IntegerPrimitive averageCount;

        [ABRInput("Ribbon Width", "Ribbon")]
        public LengthPrimitive lineWidth;

        [ABRInput("Ribbon Rotation", "Ribbon")]
        public AnglePrimitive ribbonRotationAngle;

        [ABRInput("Ribbon Brightness", "Ribbon")]
        public PercentPrimitive ribbonBrightness;

        [ABRInput("Ribbon Curve", "Ribbon")]
        public AnglePrimitive ribbonCurveAngle;

        protected override string MaterialName { get; } = "ABR_DataTextureRibbon";
        protected override string LayerName { get; } = "ABR_Line";

        /// <summary>
        ///     Construct a data impession with a given UUID. Note that this
        ///     will be called from ABRState and must assume that there's a
        ///     single string argument with UUID.
        /// </summary>
        public SimpleLineDataImpression(string uuid) : base(uuid) { }
        public SimpleLineDataImpression() : base() { }

        public override Dataset GetDataset()
        {
            return keyData?.GetDataset();
        }

        public override void ComputeKeyDataRenderInfo() { }

        public override void ComputeRenderInfo()
        {
            SimpleLineRenderInfo renderInfo = null;

            if (keyData == null)
            {
                renderInfo = new SimpleLineRenderInfo
                {
                    vertices = new Vector3[0][],
                    indices = new int[0][],
                    scalars = new Color[0][],
                    normals = new Vector3[0][],
                    uvs = new Vector2[0][],
                    scalarMax = Vector4.zero,
                    scalarMin = Vector4.zero
                };
            }
            else
            {
                RawDataset dataset;
                ABREngine.Instance.Data.TryGetRawDataset(keyData?.Path, out dataset);

                DataImpressionGroup group = ABREngine.Instance.GetGroupFromImpression(this);

                // Load defaults from configuration / schema
                ABRConfig config = ABREngine.Instance.Config;

                // Width appears double what it should be, so decrease to
                // maintain the actual real world distance
                string plateType = this.GetType().GetCustomAttribute<ABRPlateType>().plateType;

                float ribbonWidth = lineWidth?.Value ??
                    config.GetInputValueDefault<LengthPrimitive>(plateType, "Ribbon Width").Value;
                ribbonWidth /= 2.0f;

                int averageCountN = averageCount?.Value ??
                    config.GetInputValueDefault<IntegerPrimitive>(plateType, "Ribbon Smooth").Value;

                float curveAngle = ribbonCurveAngle?.Value ??
                    config.GetInputValueDefault<AnglePrimitive>(plateType, "Ribbon Curve").Value;

                float ribbonRotation = ribbonRotationAngle?.Value ??
                    config.GetInputValueDefault<AnglePrimitive>(plateType, "Ribbon Rotation").Value;
                    

                int numLines = 0;
                numLines = dataset.cellIndexCounts.Length;
                renderInfo = new SimpleLineRenderInfo
                {
                    vertices = new Vector3[numLines][],
                    indices = new int[numLines][],
                    scalars = new Color[numLines][],
                    normals = new Vector3[numLines][],
                    uvs = new Vector2[numLines][],
                    scalarMax = Vector4.zero,
                    scalarMin = Vector4.zero
                };

                int pointIndex = 0;

                float[] colorVariableArray = null;
                if (colorVariable != null && colorVariable.IsPartOf(keyData))
                {
                    colorVariableArray = colorVariable.GetArray(keyData);
                    renderInfo.scalarMin[0] = colorVariable.MinValue;
                    renderInfo.scalarMax[0] = colorVariable.MaxValue;
                }

                for (int i = 0; i < numLines; i++)
                {
                    Queue<Vector3> smoothingNormals = new Queue<Vector3>(averageCountN);
                    Queue<Vector3> smoothingTangents = new Queue<Vector3>(averageCountN);

                    int numPoints = dataset.cellIndexCounts[i];
                    int numVerts = numPoints * 4;
                    int numIndices = (numPoints - 1) * 12;
                    renderInfo.vertices[i] = new Vector3[numVerts];
                    renderInfo.normals[i] = new Vector3[numVerts];
                    renderInfo.uvs[i] = new Vector2[numVerts];
                    renderInfo.scalars[i] = new Color[numVerts];
                    renderInfo.indices[i] = new int[numIndices];

                    int indexOffset = dataset.cellIndexOffsets[i];

                    int indexEnd = indexOffset + numPoints;

                    float arclength = 0;

                    Vector3 lastV = Vector3.up;
                    for (int index = indexOffset, j = 0; index < indexEnd; index++, j++)
                    {
                        pointIndex = dataset.indexArray[index];
                        var lastPointIndex = (j == 0) ? pointIndex : dataset.indexArray[index - 1];
                        var nextPointIndex = (j == numPoints - 1) ? pointIndex : dataset.indexArray[index + 1];

                        Vector3 point = group.GroupToDataMatrix * dataset.vertexArray[pointIndex].ToHomogeneous();
                        Vector3 lastPoint = group.GroupToDataMatrix * dataset.vertexArray[lastPointIndex].ToHomogeneous();
                        Vector3 nextPoint = group.GroupToDataMatrix * dataset.vertexArray[nextPointIndex].ToHomogeneous();

                        Vector3 tangent;
                        Vector3 normal;
                        Vector3 bitangent;
                        Vector3 fromLast = point - lastPoint;
                        Vector3 toNext = nextPoint - point;
                        Vector4 scalar = Vector4.zero;

                        arclength = arclength + fromLast.magnitude;
                        tangent = (fromLast + toNext).normalized;

                        Vector3 V = Vector3.Cross(fromLast.normalized, toNext.normalized).normalized;
                        if (Vector3.Dot(V, lastV) < 0) V = -V;
                        lastV = V;
                        Vector3 N = Vector3.Cross(V, tangent).normalized;
                        Vector3 normalSum = N;
                        Vector3 tangentDirSum = tangent.normalized;

                        if (smoothingNormals.Count > 0)
                        {
                            if (Vector3.Dot(smoothingNormals.Last(), N) < 0)
                                N = -N;
                            normalSum = N;

                            foreach (var n in smoothingNormals)
                            {
                                normalSum += n;
                            }
                            foreach (var t in smoothingTangents)
                            {
                                tangentDirSum += t.normalized;
                            }
                        }
                        Vector3 normalAvg = normalSum / (smoothingNormals.Count + 1);
                        Vector3 tangentAvg = tangentDirSum / (smoothingNormals.Count + 1);


                        normal = normalAvg;
                        tangent = tangentAvg;

                        smoothingNormals.Enqueue(normal);
                        while (smoothingNormals.Count > averageCountN) smoothingNormals.Dequeue();
                        while (smoothingTangents.Count > averageCountN) smoothingTangents.Dequeue();


                        if (colorVariableArray != null)
                        {
                            scalar[0] = colorVariableArray[index];
                        }

                        normal = normal.normalized;
                        tangent = tangent.normalized;
                        bitangent = -Vector3.Cross(normal, tangent).normalized;



                        normal = Quaternion.AngleAxis(ribbonRotation, tangent) * normal;
                        bitangent = Quaternion.AngleAxis(ribbonRotation, tangent) * bitangent;


                        int indexTopFront = j * 4 + 0;
                        int indexTopBack = j * 4 + 1;
                        int indexBottomFront = j * 4 + 2;
                        int indexBottomBack = j * 4 + 3;

                        int nextIndexTopFront = (j + 1) * 4 + 0;
                        int nextIndexTopBack = (j + 1) * 4 + 1;
                        int nextIndexBottomFront = (j + 1) * 4 + 2;
                        int nextIndexBottomBack = (j + 1) * 4 + 3;

                        renderInfo.vertices[i][indexTopFront] = (point + bitangent * ribbonWidth + normal * ribbonWidth);
                        renderInfo.vertices[i][indexTopBack] = (point + bitangent * ribbonWidth - normal * ribbonWidth);
                        renderInfo.vertices[i][indexBottomFront] = (point - bitangent * ribbonWidth + normal * ribbonWidth);
                        renderInfo.vertices[i][indexBottomBack] = (point - bitangent * ribbonWidth - normal * ribbonWidth);

                        renderInfo.normals[i][indexTopFront] = Quaternion.AngleAxis(curveAngle, tangent) * normal;
                        renderInfo.normals[i][indexTopBack] = Quaternion.AngleAxis(-curveAngle, tangent) * -normal;
                        renderInfo.normals[i][indexBottomFront] = Quaternion.AngleAxis(-curveAngle, tangent) * normal;
                        renderInfo.normals[i][indexBottomBack] = Quaternion.AngleAxis(curveAngle, tangent) * -normal;

                        renderInfo.scalars[i][indexTopFront] = scalar;
                        renderInfo.scalars[i][indexTopBack] = scalar;
                        renderInfo.scalars[i][indexBottomFront] = scalar;
                        renderInfo.scalars[i][indexBottomBack] = scalar;



                        renderInfo.uvs[i][indexTopFront] = new Vector2(arclength / (ribbonWidth * 2), 0);
                        renderInfo.uvs[i][indexTopBack] = new Vector2(arclength / (ribbonWidth * 2), 0);
                        renderInfo.uvs[i][indexBottomFront] = new Vector2(arclength / (ribbonWidth * 2), 1);
                        renderInfo.uvs[i][indexBottomBack] = new Vector2(arclength / (ribbonWidth * 2), 1);



                        if (j < (numPoints - 10) && j > 1)
                        {
                            renderInfo.indices[i][j * 12 + 0] = indexTopFront;
                            renderInfo.indices[i][j * 12 + 1] = nextIndexTopFront;
                            renderInfo.indices[i][j * 12 + 2] = indexBottomFront;

                            renderInfo.indices[i][j * 12 + 3] = nextIndexTopFront;
                            renderInfo.indices[i][j * 12 + 4] = nextIndexBottomFront;
                            renderInfo.indices[i][j * 12 + 5] = indexBottomFront;

                            renderInfo.indices[i][j * 12 + 6] = indexBottomBack;
                            renderInfo.indices[i][j * 12 + 7] = nextIndexTopBack;
                            renderInfo.indices[i][j * 12 + 8] = indexTopBack;

                            renderInfo.indices[i][j * 12 + 9] = indexBottomBack;
                            renderInfo.indices[i][j * 12 + 10] = nextIndexBottomBack;
                            renderInfo.indices[i][j * 12 + 11] = nextIndexTopBack;
                        }

                    }
                }
            }

            RenderInfo = renderInfo;
        }

        public override void ApplyToGameObject(EncodedGameObject currentGameObject)
        {
            var lineResources = RenderInfo as SimpleLineRenderInfo;
            if (currentGameObject == null || lineResources == null)
            {
                return;
            }
            int numLines = lineResources?.indices?.Length ?? 0;
            while (currentGameObject.transform.childCount < numLines)
            {
                GameObject renderObject = GenericObjectPool.Instance.GetObjectFromPool(this.GetType().Name + " meshRenderer", currentGameObject.transform, go =>
                {
                    go.name = "Line Render Object";
                });
                renderObject.transform.SetParent(currentGameObject.transform, false);
                renderObject.transform.localPosition = Vector3.zero;
                renderObject.transform.localScale = Vector3.one;
                renderObject.transform.localRotation = Quaternion.identity;
            }

            while (currentGameObject.transform.childCount > numLines)
            {
                GameObject child = currentGameObject.transform.GetChild(0).gameObject;
                GenericObjectPool.Instance.ReturnObjectToPool(child);
            }

            for (int i = 0; i < numLines; i++)
            {

                var renderObject = currentGameObject.transform.GetChild(i).gameObject;
                var meshFilter = renderObject.GetComponent<MeshFilter>();
                if (meshFilter == null)
                    meshFilter = renderObject.AddComponent<MeshFilter>();

                var meshRenderer = renderObject.GetComponent<MeshRenderer>();
                if (meshRenderer == null)
                    meshRenderer = renderObject.AddComponent<MeshRenderer>();

                int layerID = LayerMask.NameToLayer(LayerName);
                if (layerID >= 0)
                {
                    renderObject.layer = layerID;
                }
                else
                {
                    Debug.LogWarningFormat("Could not find layer {0} for SimpleLineDataImpression", LayerName);
                }

                // SET MATERIAL STUFF



                // build mesh from dataset arrays
                Mesh mesh = renderObject.GetComponent<MeshFilter>().mesh;
                //if(mesh == null)
                //{
                mesh = meshFilter.mesh;
                if (mesh == null) mesh = new Mesh();
                mesh.name = "LRS:368@" + DateTime.Now.ToString();
                //}


                mesh.Clear();
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

                mesh.vertices = lineResources.vertices[i];
                mesh.normals = lineResources.normals[i];
                mesh.colors = lineResources.scalars[i];
                mesh.uv = lineResources.uvs[i];

                mesh.SetIndices(lineResources.indices[i], MeshTopology.Triangles, 0);

                mesh.UploadMeshData(false);


                meshFilter.mesh = mesh;

                meshRenderer.material = ImpressionMaterial;

                // Load defaults from configuration / schema
                ABRConfig config = ABREngine.Instance.Config;

                // Width appears double what it should be, so decrease to
                // maintain the actual real world distance
                string plateType = this.GetType().GetCustomAttribute<ABRPlateType>().plateType;

                float ribbonBrightnessOut = ribbonBrightness?.Value ??
                    config.GetInputValueDefault<PercentPrimitive>(plateType, "Ribbon Brightness").Value;

                float textureCutoffOut = textureCutoff?.Value ??
                    config.GetInputValueDefault<PercentPrimitive>(plateType, "Texture Cutoff").Value;

                meshRenderer.GetPropertyBlock(MatPropBlock);
                MatPropBlock.SetColor("_Color", Color.white);
                MatPropBlock.SetFloat("_TextureCutoff", textureCutoffOut);
                MatPropBlock.SetFloat("_RibbonBrightness", ribbonBrightnessOut);

                if (lineTexture != null)
                {
                    MatPropBlock.SetTexture("_Texture", lineTexture.Texture);
                    MatPropBlock.SetFloat("_TextureAspect", lineTexture.Texture.width / (float)lineTexture.Texture.height);
                    MatPropBlock.SetInt("_UseLineTexture", 1);

                }
                else
                {
                    MatPropBlock.SetInt("_UseLineTexture", 0);

                }
                MatPropBlock.SetFloat("_ColorDataMin", lineResources.scalarMin[0]);
                MatPropBlock.SetFloat("_ColorDataMax", lineResources.scalarMax[0]);
                if (colormap?.GetColorGradient() != null)
                {
                    MatPropBlock.SetInt("_UseColorMap", 1);
                    MatPropBlock.SetTexture("_ColorMap", colormap?.GetColorGradient());
                }
                else
                {
                    MatPropBlock.SetInt("_UseColorMap", 0);
                }
                meshRenderer.SetPropertyBlock(MatPropBlock);


            }
        }
    }
}