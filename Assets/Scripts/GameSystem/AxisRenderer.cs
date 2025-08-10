// AxisRenderer.cs
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class AxisRenderer : MonoBehaviour
{
    // 이 값을 Material의 Line Length와 일치시켜야 합니다.
    [Tooltip("축의 길이입니다. Material의 Line Length와 일치시켜야 합니다.")]
    public float lineLength = 1.0f;

    void Awake()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null) return;

        Mesh mesh = new Mesh();
        mesh.name = "PointMesh";
        mesh.vertices = new Vector3[] { Vector3.zero };
        mesh.SetIndices(new int[] { 0 }, MeshTopology.Points, 0);

        // 축이 양쪽으로 뻗어나가므로, 전체 길이는 lineLength * 2 입니다.
        // 따라서 경계의 크기는 지름이 lineLength * 4인 구와 같게 설정합니다.
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * lineLength * 4);

        meshFilter.mesh = mesh;
    }

    // Inspector에서 값이 변경될 때마다 경계를 다시 계산하도록 합니다.
    void OnValidate()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            meshFilter.sharedMesh.bounds = new Bounds(Vector3.zero, Vector3.one * lineLength * 4);
        }
    }
}
