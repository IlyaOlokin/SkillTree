using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;

namespace SkillTree
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class SkillTreeSplineMesh : MonoBehaviour
    {
        [SerializeField] private List<SplineContainer> splines;
        [SerializeField] private int resolutionPerSpline = 20;
        [SerializeField] private float lineWidth = 0.1f;

        private Mesh mesh;

        void Awake()
        {
            mesh = new Mesh();
            mesh.name = "SkillTreeLines";
            GetComponent<MeshFilter>().mesh = mesh;

            BuildMesh();
        }

        void BuildMesh()
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();

            int vertIndex = 0;

            foreach (var container in splines)
            {
                var spline = container.Spline;

                Vector3 prevPos = spline.EvaluatePosition(0f);

                for (int i = 1; i < resolutionPerSpline; i++)
                {
                    float t = i / (float)(resolutionPerSpline - 1);
                    Vector3 currPos = spline.EvaluatePosition(t);

                    Vector3 dir = (currPos - prevPos).normalized;
                    Vector3 normal = Vector3.Cross(dir, Vector3.forward);

                    Vector3 offset = normal * (lineWidth * 0.5f);

                    Vector3 v0 = prevPos + offset;
                    Vector3 v1 = prevPos - offset;
                    Vector3 v2 = currPos + offset;
                    Vector3 v3 = currPos - offset;

                    vertices.Add(v0);
                    vertices.Add(v1);
                    vertices.Add(v2);
                    vertices.Add(v3);

                    triangles.Add(vertIndex + 0);
                    triangles.Add(vertIndex + 2);
                    triangles.Add(vertIndex + 1);

                    triangles.Add(vertIndex + 2);
                    triangles.Add(vertIndex + 3);
                    triangles.Add(vertIndex + 1);

                    uvs.Add(Vector2.zero);
                    uvs.Add(Vector2.right);
                    uvs.Add(Vector2.up);
                    uvs.Add(Vector2.one);

                    vertIndex += 4;
                    prevPos = currPos;
                }
            }

            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateBounds();
        }
    }
}
