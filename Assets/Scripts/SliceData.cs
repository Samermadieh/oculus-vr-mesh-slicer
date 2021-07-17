using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
    public enum Side
    {
        P = 0,
        N = 1
    }

    class SliceData
    {
        private Mesh pMesh;
        private List<Vector3> pVertices;
        private List<int> pTriangles;
        private List<Vector2> pUVs;
        private List<Vector3> pNormals;

        private Mesh nMesh;
        private List<Vector3> nVertices;
        private List<int> nTriangles;
        private List<Vector2> nUVs;
        private List<Vector3> nNormals;

        private readonly List<Vector3> PlanePoints;
        private Plane _plane;
        private Mesh _mesh;
        private bool _isSolid;
        private bool sharedVertices = false;
        private bool smoothVertices = false;

        public bool IsSolid
        {
            get
            {
                return _isSolid;
            }
            set
            {
                _isSolid = value;
            }
        }

        public Mesh PositiveSideMesh
        {
            get
            {
                if (pMesh == null)
                {
                    pMesh = new Mesh();
                }

                SetMeshData(Side.P);
                return pMesh;
            }
        }

        public Mesh NegativeSideMesh
        {
            get
            {
                if (nMesh == null)
                {
                    nMesh = new Mesh();
                }

                SetMeshData(Side.N);

                return nMesh;
            }
        }

        public SliceData(Plane plane, Mesh mesh, bool isSolid, bool shareVertices, bool smoothVertices)
        {
            pTriangles = new List<int>();
            pVertices = new List<Vector3>();
            nTriangles = new List<int>();
            nVertices = new List<Vector3>();
            pUVs = new List<Vector2>();
            nUVs = new List<Vector2>();
            pNormals = new List<Vector3>();
            nNormals = new List<Vector3>();
            PlanePoints = new List<Vector3>();
            _plane = plane;
            _mesh = mesh;
            _isSolid = isSolid;
            sharedVertices = shareVertices;
            this.smoothVertices = smoothVertices;

            ComputeNewMeshes();
        }

        private void AddTrianglesNormalAndUvs(Side side, Vector3 vertex1, Vector3? normal1, Vector2 uv1, Vector3 vertex2, Vector3? normal2, Vector2 uv2, Vector3 vertex3, Vector3? normal3, Vector2 uv3, bool shareVertices, bool addFirst)
        {
            if (side == Side.P)
            {
                AddTrianglesNormalsAndUvs(ref pVertices, ref pTriangles, ref pNormals, ref pUVs, vertex1, normal1, uv1, vertex2, normal2, uv2, vertex3, normal3, uv3, shareVertices, addFirst);
            }
            else
            {
                AddTrianglesNormalsAndUvs(ref nVertices, ref nTriangles, ref nNormals, ref nUVs, vertex1, normal1, uv1, vertex2, normal2, uv2, vertex3, normal3, uv3, shareVertices, addFirst);
            }
        }

        private void AddTrianglesNormalsAndUvs(ref List<Vector3> vertices, ref List<int> triangles, ref List<Vector3> normals, ref List<Vector2> uvs, Vector3 vertex1, Vector3? normal1, Vector2 uv1, Vector3 vertex2, Vector3? normal2, Vector2 uv2, Vector3 vertex3, Vector3? normal3, Vector2 uv3, bool shareVertices, bool addFirst)
        {
            int tri1Index = vertices.IndexOf(vertex1);

            if (addFirst)
            {
                ShiftTriangleIndeces(ref triangles);
            }

            if (tri1Index > -1 && shareVertices)
            {                
                triangles.Add(tri1Index);
            }
            else
            {
                if (normal1 == null)
                {
                    normal1 = ComputeNormal(vertex1, vertex2, vertex3);                    
                }

                int? i = null;
                if (addFirst)
                {
                    i = 0;
                }

                AddVertNormalUv(ref vertices, ref normals, ref uvs, ref triangles, vertex1, (Vector3)normal1, uv1, i);
            }

            int tri2Index = vertices.IndexOf(vertex2);

            if (tri2Index > -1 && shareVertices)
            {
                triangles.Add(tri2Index);
            }
            else
            {
                if (normal2 == null)
                {
                    normal2 = ComputeNormal(vertex2, vertex3, vertex1);
                }
                
                int? i = null;
                
                if (addFirst)
                {
                    i = 1;
                }

                AddVertNormalUv(ref vertices, ref normals, ref uvs, ref triangles, vertex2, (Vector3)normal2, uv2, i);
            }

            int tri3Index = vertices.IndexOf(vertex3);

            if (tri3Index > -1 && shareVertices)
            {
                triangles.Add(tri3Index);
            }
            else
            {               
                if (normal3 == null)
                {
                    normal3 = ComputeNormal(vertex3, vertex1, vertex2);
                }

                int? i = null;
                if (addFirst)
                {
                    i = 2;
                }

                AddVertNormalUv(ref vertices, ref normals, ref uvs, ref triangles, vertex3, (Vector3)normal3, uv3, i);
            }
        }

        private void AddVertNormalUv(ref List<Vector3> vertices, ref List<Vector3> normals, ref List<Vector2> uvs, ref List<int> triangles, Vector3 vertex, Vector3 normal, Vector2 uv, int? index)
        {
            if (index != null)
            {
                int i = (int)index;
                vertices.Insert(i, vertex);
                uvs.Insert(i, uv);
                normals.Insert(i, normal);
                triangles.Insert(i, i);
            }
            else
            {
                vertices.Add(vertex);
                normals.Add(normal);
                uvs.Add(uv);
                triangles.Add(vertices.IndexOf(vertex));
            }
        }

        private void ShiftTriangleIndeces(ref List<int> triangles)
        {
            for (int j = 0; j < triangles.Count; j += 3)
            {
                triangles[j] += + 3;
                triangles[j + 1] += 3;
                triangles[j + 2] += 3;
            }
        }

        private void JoinPointsAlongPlane()
        {
            Vector3 halfway = GetHalfwayPoint(out float distance);

            for (int i = 0; i < PlanePoints.Count; i += 2)
            {
                Vector3 firstVertex;
                Vector3 secondVertex;

                firstVertex = PlanePoints[i];
                secondVertex = PlanePoints[i + 1];

                Vector3 normal3 = ComputeNormal(halfway, secondVertex, firstVertex);
                normal3.Normalize();

                var direction = Vector3.Dot(normal3, _plane.normal);

                if(direction > 0)
                {                                        
                    AddTrianglesNormalAndUvs(Side.P, halfway, -normal3, Vector2.zero, firstVertex, -normal3, Vector2.zero, secondVertex, -normal3, Vector2.zero, false, true);
                    AddTrianglesNormalAndUvs(Side.N, halfway, normal3, Vector2.zero, secondVertex, normal3, Vector2.zero, firstVertex, normal3, Vector2.zero, false, true);
                }
                else
                {
                    AddTrianglesNormalAndUvs(Side.P, halfway, normal3, Vector2.zero, secondVertex, normal3, Vector2.zero, firstVertex, normal3, Vector2.zero, false, true);
                    AddTrianglesNormalAndUvs(Side.N, halfway, -normal3, Vector2.zero, firstVertex, -normal3, Vector2.zero, secondVertex, -normal3, Vector2.zero, false, true);
                }               
            }
        }

        private Vector3 GetHalfwayPoint(out float distance)
        {
            if(PlanePoints.Count > 0)
            {
                Vector3 firstPoint = PlanePoints[0];
                Vector3 furthestPoint = Vector3.zero;
                distance = 0f;

                foreach (Vector3 point in PlanePoints)
                {
                    float currentDistance = 0f;
                    currentDistance = Vector3.Distance(firstPoint, point);

                    if (currentDistance > distance)
                    {
                        distance = currentDistance;
                        furthestPoint = point;
                    }
                }

                return Vector3.Lerp(firstPoint, furthestPoint, 0.5f);
            }
            else
            {
                distance = 0;
                return Vector3.zero;
            }
        }

        private void SetMeshData(Side side)
        {
            if (side == Side.P)
            {
                pMesh.vertices = pVertices.ToArray();
                pMesh.triangles = pTriangles.ToArray();
                pMesh.normals = pNormals.ToArray();
                pMesh.uv = pUVs.ToArray();
            }
            else
            {
                nMesh.vertices = nVertices.ToArray();
                nMesh.triangles = nTriangles.ToArray();
                nMesh.normals = nNormals.ToArray();
                nMesh.uv = nUVs.ToArray();                
            }
        }

        private void ComputeNewMeshes()
        {
            int[] meshTriangles = _mesh.triangles;
            Vector3[] meshVerts = _mesh.vertices;
            Vector3[] meshNormals = _mesh.normals;
            Vector2[] meshUvs = _mesh.uv;

            for (int i = 0; i < meshTriangles.Length; i += 3)
            {
                Vector3 vert1 = meshVerts[meshTriangles[i]];
                int vert1Index = Array.IndexOf(meshVerts, vert1);
                Vector2 uv1 = meshUvs[vert1Index];
                Vector3 normal1 = meshNormals[vert1Index];
                bool vert1Side = _plane.GetSide(vert1);

                Vector3 vert2 = meshVerts[meshTriangles[i + 1]];
                int vert2Index = Array.IndexOf(meshVerts, vert2);
                Vector2 uv2 = meshUvs[vert2Index];
                Vector3 normal2 = meshNormals[vert2Index];
                bool vert2Side = _plane.GetSide(vert2);

                Vector3 vert3 = meshVerts[meshTriangles[i + 2]];
                bool vert3Side = _plane.GetSide(vert3);
                int vert3Index = Array.IndexOf(meshVerts, vert3);
                Vector3 normal3 = meshNormals[vert3Index];
                Vector2 uv3 = meshUvs[vert3Index];

                if (vert1Side == vert2Side && vert2Side == vert3Side)
                {
                    Side side = (vert1Side) ? Side.P : Side.N;
                    AddTrianglesNormalAndUvs(side, vert1, normal1, uv1, vert2, normal2, uv2, vert3, normal3, uv3, true, false);
                }
                else
                {
                    Vector3 intersection1;
                    Vector3 intersection2;

                    Vector2 intersection1Uv;
                    Vector2 intersection2Uv;

                    Side side1 = (vert1Side) ? Side.P : Side.N;
                    Side side2 = (vert1Side) ? Side.N : Side.P;

                    if (vert1Side == vert2Side)
                    {                      
                        intersection1 = GetRayPlaneIntersectionPointAndUv(vert2, uv2, vert3, uv3, out intersection1Uv);
                        intersection2 = GetRayPlaneIntersectionPointAndUv(vert3, uv3, vert1, uv1, out intersection2Uv);

                        AddTrianglesNormalAndUvs(side1, vert1, null, uv1, vert2, null, uv2, intersection1, null, intersection1Uv, sharedVertices, false);
                        AddTrianglesNormalAndUvs(side1, vert1, null, uv1, intersection1, null, intersection1Uv, intersection2, null, intersection2Uv, sharedVertices, false);

                        AddTrianglesNormalAndUvs(side2, intersection1, null, intersection1Uv, vert3, null, uv3, intersection2, null, intersection2Uv, sharedVertices, false);

                    }
                    else if (vert1Side == vert3Side)
                    {                     
                        intersection1 = GetRayPlaneIntersectionPointAndUv(vert1, uv1, vert2, uv2, out intersection1Uv);
                        intersection2 = GetRayPlaneIntersectionPointAndUv(vert2, uv2, vert3, uv3, out intersection2Uv);

                        AddTrianglesNormalAndUvs(side1, vert1, null, uv1, intersection1, null, intersection1Uv, vert3, null, uv3, sharedVertices, false);
                        AddTrianglesNormalAndUvs(side1, intersection1, null, intersection1Uv, intersection2, null, intersection2Uv, vert3, null, uv3, sharedVertices, false);

                        AddTrianglesNormalAndUvs(side2, intersection1, null, intersection1Uv, vert2, null, uv2, intersection2, null, intersection2Uv, sharedVertices, false);
                    }
                    else
                    {                      
                        intersection1 = GetRayPlaneIntersectionPointAndUv(vert1, uv1, vert2, uv2, out intersection1Uv);
                        intersection2 = GetRayPlaneIntersectionPointAndUv(vert1, uv1, vert3, uv3, out intersection2Uv);

                        AddTrianglesNormalAndUvs(side1, vert1, null, uv1, intersection1, null, intersection1Uv, intersection2, null, intersection2Uv, sharedVertices, false);

                        AddTrianglesNormalAndUvs(side2, intersection1, null, intersection1Uv, vert2, null, uv2, vert3, null, uv3, sharedVertices, false);
                        AddTrianglesNormalAndUvs(side2, intersection1, null, intersection1Uv, vert3, null, uv3, intersection2, null, intersection2Uv, sharedVertices, false);
                    }

                    PlanePoints.Add(intersection1);
                    PlanePoints.Add(intersection2);
                }
            }

            if (_isSolid)
            {
                JoinPointsAlongPlane();
            }

            if (smoothVertices)
            {
                SmoothVertices();
            }

        }

        private Vector3 GetRayPlaneIntersectionPointAndUv(Vector3 vertex1, Vector2 vertex1Uv, Vector3 vertex2, Vector2 vertex2Uv, out Vector2 uv)
        {
            float distance = GetDistanceRelativeToPlane(vertex1, vertex2, out Vector3 pointOfIntersection);
            uv = InterpolateUvs(vertex1Uv, vertex2Uv, distance);
            return pointOfIntersection;
        }

        private float GetDistanceRelativeToPlane(Vector3 vertex1, Vector3 vertex2, out Vector3 pointOfintersection)
        {
            Ray ray = new Ray(vertex1, (vertex2 - vertex1));
            _plane.Raycast(ray, out float distance);
            pointOfintersection = ray.GetPoint(distance);
            return distance;
        }

        private Vector2 InterpolateUvs(Vector2 uv1, Vector2 uv2, float distance)
        {
            Vector2 uv = Vector2.Lerp(uv1, uv2, distance);
            return uv;
        }

        private Vector3 ComputeNormal(Vector3 vertex1, Vector3 vertex2, Vector3 vertex3)
        {
            Vector3 side1 = vertex2 - vertex1;
            Vector3 side2 = vertex3 - vertex1;

            Vector3 normal = Vector3.Cross(side1, side2);

            return normal;
        }

        private List<Vector3> FlipNormals(List<Vector3> currentNormals)
        {
            List<Vector3> flippedNormals = new List<Vector3>();

            foreach (Vector3 normal in currentNormals)
            {
                flippedNormals.Add(-normal);
            }

            return flippedNormals;
        }

        //
        private void SmoothVertices()
        {
            DoSmoothing(ref pVertices, ref pNormals, ref pTriangles);
            DoSmoothing(ref nVertices, ref nNormals, ref nTriangles);
        }

        private void DoSmoothing(ref List<Vector3> vertices, ref List<Vector3> normals, ref List<int> triangles)
        {
            normals.ForEach(x =>
            {
                x = Vector3.zero;
            });

            for (int i = 0; i < triangles.Count; i += 3)
            {
                int vertIndex1 = triangles[i];
                int vertIndex2 = triangles[i + 1];
                int vertIndex3 = triangles[i + 2];

                Vector3 triangleNormal = ComputeNormal(vertices[vertIndex1], vertices[vertIndex2], vertices[vertIndex3]);

                normals[vertIndex1] += triangleNormal;
                normals[vertIndex2] += triangleNormal;
                normals[vertIndex3] += triangleNormal;
            }

            normals.ForEach(x =>
            {
                x.Normalize();
            });
        }
    }
}