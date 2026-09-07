using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 건물 영역의 원드 좌표와 그리드에서 셀 좌표를 서로 변환
/// 건물의 배치 가능 여부와 셀 점유 상태는 관리하지 않음 
/// </summary>
public class BuildingGrid : MonoBehaviour
{
    [SerializeField]
    private float cellSize;

    [SerializeField]
    private int gizmoHalfCellCount = 50;
    [SerializeField]
    private Color gizmoColor = new Color(0f, 1f, 1f, 0.35f);

    [SerializeField]
    private GridRegistry gridRegistry;
    [SerializeField]
    private Color occupiedCellColor = new Color(1f, 0.2f, 0.1f, 0.35f);
    [SerializeField, Range(0f, 0.5f)]
    private float occupiedCellInset = 0.05f;

    private bool showGizmo;
    public float CellSize => cellSize;

    private void Start()
    {
        SetGizmoVisible(true);
    }

    /// <summary>
    /// 월드 위치를 중심으로 건물이 점유할 영역의 시작 셀을 계산
    /// 건물의 중심 셀이 아닌 건물 점유 영역의 시작 셀을 반환
    /// </summary>
    /// <param name="worldPos">월드 위치</param>
    /// <param name="footprint">건물의 크기</param>
    /// <returns>시작할 셀의 위치</returns>
    public Vector2Int WorldToStartCell(Vector3 worldPos, Vector2Int footprint)
    {
        Vector3 localPos = worldPos - transform.position;

        float gridX = localPos.x / cellSize - footprint.x * 0.5f;
        float gridY = localPos.z / cellSize - footprint.y * 0.5f;

        int startX = Mathf.RoundToInt(gridX);
        int startY = Mathf.RoundToInt(gridY);

        return new Vector2Int(startX, startY);
    }

    /// <summary>
    /// 월드 위치를 해당 위치가 포함된 그리드 셀 좌표로 변환
    /// </summary>
    /// <param name="worldPos">월드 위치</param>
    /// <returns>월드 위치의 그리드 셀</returns>
    public Vector2Int WorldToCell(Vector3 worldPos)
    {
        Vector3 localPos = worldPos - transform.position;

        int x = Mathf.FloorToInt(localPos.x / cellSize);
        int z = Mathf.FloorToInt(localPos.z / cellSize);

        return new Vector2Int(x, z);
    }

    /// <summary>
    /// 주어진 셀들이 차지하는 영역의 월드 중심점을 계산
    /// 셀 목록이 없으면 그리드 원점의 XZ좌표를 반환
    /// 셀의 중심은 XZ 평면에서 계산하고 경과릐 Y 좌표에는 worldY를 사용
    /// </summary>
    /// <param name="cells">중심점을 게산할 셀들</param>
    /// <param name="worldY">좌표의 높이</param>
    /// <returns>중심 좌표</returns>
    public Vector3 GetCellsWorldCenter(IReadOnlyList<Vector2Int> cells, float worldY)
    {
        if(cells == null || cells.Count == 0)
            return new Vector3(transform.position.x, worldY, transform.position.z);

        Vector3 totlaPosition = Vector3.zero;

        foreach(Vector2Int cell in cells)
        {
            totlaPosition += CellToWorldCenter(cell);
        }

        Vector3 center = totlaPosition / cells.Count;
        center.y = worldY;

        return center;
    }

    /// <summary>
    /// 그리드의 좌표가 주어지면 cellSize를 이용해서 셀의 중심점을 월드 위치로 변환
    /// </summary>
    /// <param name="cell">중심점을 찾을 셀</param>
    /// <returns>셀의 중심 월드 좌표</returns>
    public Vector3 CellToWorldCenter(Vector2Int cell)
    {
        float x = (cell.x + 0.5f) * cellSize;
        float z = (cell.y + 0.5f) * cellSize;

        return transform.position + new Vector3(x, 0f, z);
    }

    public void SetGizmoVisible(bool visible)
    {
        showGizmo = visible;
    }

    private void DrawOccupiedCells()
    {
        if (gridRegistry == null)
            return;

        Gizmos.color = occupiedCellColor;

        foreach (Vector2Int cell in gridRegistry.OccupiedCells)
        {
            // 현재 표시 중인 Gizmo 범위 밖이면 그리지 않음
            if (cell.x < -gizmoHalfCellCount || cell.x >= gizmoHalfCellCount)
                continue;

            if (cell.y < -gizmoHalfCellCount || cell.y >= gizmoHalfCellCount)
                continue;

            Vector3 center = CellToWorldCenter(cell);
            center.y += 0.03f;

            float size = cellSize * (1f - occupiedCellInset);

            Vector3 cubeSize = new Vector3(size, 0.03f, size);

            Gizmos.DrawCube(center, cubeSize);
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmo)
            return;

        if (cellSize <= 0f)
            return;

        Gizmos.color = gizmoColor;

        Vector3 origin = transform.position;
        origin.y += 0.02f;

        float halfSize = gizmoHalfCellCount * cellSize;

        for (int i = -gizmoHalfCellCount; i <= gizmoHalfCellCount; i++)
        {
            float offset = i * cellSize;

            Vector3 xStart = origin + new Vector3(offset, 0f, -halfSize);
            Vector3 xEnd = origin + new Vector3(offset, 0f, halfSize);

            Gizmos.DrawLine(xStart, xEnd);

            Vector3 zStart = origin + new Vector3(-halfSize, 0f, offset);
            Vector3 zEnd = origin + new Vector3(halfSize, 0f, offset);

            Gizmos.DrawLine(zStart, zEnd);
        }

        DrawOccupiedCells();
    }
}
