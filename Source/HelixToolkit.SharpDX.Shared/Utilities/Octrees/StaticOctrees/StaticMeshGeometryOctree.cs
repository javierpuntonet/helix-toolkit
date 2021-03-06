﻿/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/
//#define DEBUG
using SharpDX;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

#if NETFX_CORE
namespace HelixToolkit.UWP.Utilities
#else
namespace HelixToolkit.Wpf.SharpDX.Utilities
#endif
{
    /// <summary>
    /// Static octree for mesh
    /// </summary>
    public class StaticMeshGeometryOctree : StaticOctree<KeyValuePair<int, BoundingBox>>
    {
        /// <summary>
        /// 
        /// </summary>
        protected readonly IList<Vector3> Positions;
        /// <summary>
        /// 
        /// </summary>
        protected readonly IList<int> Indices;
        /// <summary>
        /// Initializes a new instance of the <see cref="StaticMeshGeometryOctree"/> class.
        /// </summary>
        /// <param name="positions">The positions.</param>
        /// <param name="indices">The indices.</param>
        /// <param name="parameter">The parameter.</param>
        public StaticMeshGeometryOctree(IList<Vector3> positions, IList<int> indices, OctreeBuildParameter parameter)
            : base(parameter)
        {
            Positions = positions;
            Indices = indices;
        }
        /// <summary>
        /// Gets the objects.
        /// </summary>
        /// <returns></returns>
        protected override KeyValuePair<int, BoundingBox>[] GetObjects()
        {
            var objects = new KeyValuePair<int, BoundingBox>[Indices.Count / 3];
            // Construct triangle index and its bounding box KeyValuePair
            for (int i = 0; i < Indices.Count / 3; ++i)
            {
                objects[i] = new KeyValuePair<int, BoundingBox>(i, GetBoundingBox(i));
            }
            return objects;
        }
        protected override BoundingBox GetBoundingBoxFromItem(ref KeyValuePair<int, BoundingBox> item)
        {
            return item.Value;
        }

        protected override BoundingBox GetMaxBound()
        {
            return BoundingBoxExtensions.FromPoints(Positions);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private BoundingBox GetBoundingBox(int triangleIndex)
        {
            var actual = triangleIndex * 3;
            var v1 = Positions[Indices[actual++]];
            var v2 = Positions[Indices[actual++]];
            var v3 = Positions[Indices[actual]];
            var maxX = Math.Max(v1.X, Math.Max(v2.X, v3.X));
            var maxY = Math.Max(v1.Y, Math.Max(v2.Y, v3.Y));
            var maxZ = Math.Max(v1.Z, Math.Max(v2.Z, v3.Z));

            var minX = Math.Min(v1.X, Math.Min(v2.X, v3.X));
            var minY = Math.Min(v1.Y, Math.Min(v2.Y, v3.Y));
            var minZ = Math.Min(v1.Z, Math.Min(v2.Z, v3.Z));

            return new BoundingBox(new Vector3(minX, minY, minZ), new Vector3(maxX, maxY, maxZ));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="octant"></param>
        /// <param name="context"></param>
        /// <param name="model"></param>
        /// <param name="modelMatrix"></param>
        /// <param name="rayWS"></param>
        /// <param name="rayModel"></param>
        /// <param name="hits"></param>
        /// <param name="isIntersect"></param>
        /// <param name="hitThickness"></param>
        /// <returns></returns>
        protected override bool HitTestCurrentNodeExcludeChild(ref Octant octant,
            RenderContext context, object model, Matrix modelMatrix, ref Ray rayWS, ref Ray rayModel, ref List<HitTestResult> hits,
            ref bool isIntersect, float hitThickness)
        {
            isIntersect = false;
            if (!octant.IsBuilt)
            {
                return false;
            }
            var isHit = false;
            var bound = octant.Bound;
            //Hit test in local space.
            if (rayModel.Intersects(ref bound))
            {
                isIntersect = true;
                if (octant.Count == 0)
                {
                    return false;
                }
                var result = new HitTestResult();
                result.Distance = double.MaxValue;
                for (int i = octant.Start; i < octant.End; ++i)
                {
                    var idx = Objects[i].Key * 3;
                    var t1 = Indices[idx];
                    var t2 = Indices[idx + 1];
                    var t3 = Indices[idx + 2];
                    var v0 = Positions[t1];
                    var v1 = Positions[t2];
                    var v2 = Positions[t3];
                    float d;

                    if (Collision.RayIntersectsTriangle(ref rayModel, ref v0, ref v1, ref v2, out d))
                    {
                        if (d >= 0 && d < result.Distance) // If d is NaN, the condition is false.
                        {
                            result.IsValid = true;
                            result.ModelHit = model;
                            // transform hit-info to world space now:
                            var pointWorld = Vector3.TransformCoordinate(rayModel.Position + (rayModel.Direction * d), modelMatrix);
                            result.PointHit = pointWorld;
                            result.Distance = (rayWS.Position - pointWorld).Length();

                            var p0 = Vector3.TransformCoordinate(v0, modelMatrix);
                            var p1 = Vector3.TransformCoordinate(v1, modelMatrix);
                            var p2 = Vector3.TransformCoordinate(v2, modelMatrix);
                            var n = Vector3.Cross(p1 - p0, p2 - p0);
                            n.Normalize();
                            // transform hit-info to world space now:
                            result.NormalAtHit = n;// Vector3.TransformNormal(n, m).ToVector3D();
                            result.TriangleIndices = new Tuple<int, int, int>(t1, t2, t3);
                            result.Tag = idx;
                            isHit = true;
                        }
                    }
                }

                if (isHit)
                {
                    isHit = false;
                    if (hits.Count > 0)
                    {
                        if (hits[0].Distance > result.Distance)
                        {
                            hits[0] = result;
                            isHit = true;
                        }
                    }
                    else
                    {
                        hits.Add(result);
                        isHit = true;
                    }
                }
            }

            return isHit;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="octant"></param>
        /// <param name="context"></param>
        /// <param name="sphere"></param>
        /// <param name="result"></param>
        /// <param name="isIntersect"></param>
        /// <returns></returns>
        protected override bool FindNearestPointBySphereExcludeChild(ref Octant octant, RenderContext context,
            ref BoundingSphere sphere, ref List<HitTestResult> result, ref bool isIntersect)
        {
            bool isHit = false;
            var tempResult = new HitTestResult();
            tempResult.Distance = float.MaxValue;
            if (!BoxDisjointSphere(octant.Bound, ref sphere))
            {
                isIntersect = true;
                for (int i = octant.Start; i < octant.End; ++i)
                {
                    if (!BoxDisjointSphere(Objects[i].Value, ref sphere))
                    {
                        Vector3 cloestPoint;

                        var idx = Objects[i].Key * 3;
                        var t1 = Indices[idx];
                        var t2 = Indices[idx + 1];
                        var t3 = Indices[idx + 2];
                        var v0 = Positions[t1];
                        var v1 = Positions[t2];
                        var v2 = Positions[t3];
                        Collision.ClosestPointPointTriangle(ref sphere.Center, ref v0, ref v1, ref v2, out cloestPoint);
                        var d = (cloestPoint - sphere.Center).Length();
                        if (tempResult.Distance > d)
                        {
                            tempResult.Distance = d;
                            tempResult.IsValid = true;
                            tempResult.PointHit = cloestPoint;
                            tempResult.TriangleIndices = new Tuple<int, int, int>(t1, t2, t3);
                            tempResult.Tag = Objects[i].Key;
                            isHit = true;
                        }
                    }
                }
                if (isHit)
                {
                    isHit = false;
                    if (result.Count > 0)
                    {
                        if (result[0].Distance > tempResult.Distance)
                        {
                            result[0] = tempResult;
                            isHit = true;
                        }
                    }
                    else
                    {
                        result.Add(tempResult);
                        isHit = true;
                    }
                }
            }
            else
            {
                isIntersect = false;
            }
            return isHit;
        }
    }
}
