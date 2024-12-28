using UnityEngine;

public class GridDrawer : MonoBehaviour
{
    public int gridSize = 10; // 그리드 크기
    public float gridSpacing = 1.0f; // 그리드 간격

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.gray;

        // 그리드 그리기
        for (int x = -gridSize; x <= gridSize; x++)
        {
            for (int z = -gridSize; z <= gridSize; z++)
            {
                Vector3 position = new Vector3(x * gridSpacing, 0, z * gridSpacing);
                Gizmos.DrawLine(position + Vector3.left * gridSpacing, position + Vector3.right * gridSpacing);
                Gizmos.DrawLine(position + Vector3.forward * gridSpacing, position + Vector3.back * gridSpacing);
            }
        }

        // 축 표시 (X, Y, Z 축)
        Gizmos.color = Color.red; // X 축
        Gizmos.DrawLine(Vector3.zero, Vector3.right * 10);
        
        Gizmos.color = Color.green; // Y 축
        Gizmos.DrawLine(Vector3.zero, Vector3.up * 10);
        
        Gizmos.color = Color.blue; // Z 축
        Gizmos.DrawLine(Vector3.zero, Vector3.forward * 10);
    }
}
