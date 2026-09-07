using System.Collections.Generic;
using UnityEngine;

public static class FurnitureGridCalculator
{
    /// <summary>
    /// 월드 위치를 가구 점유 영역의 시작 셀로 변환
    /// </summary>
    /// <param name="worldPos">변환할 월드 위치</param>
    /// <param name="footprint">가구가 점유하는 셀 크기</param>
    /// <returns>가구 점유 영역의 시작 셀</returns>
    public static Vector2Int WorldToStartCell(Vector3 worldPos, Vector2Int footprint, Vector3 originPos, Quaternion originRotation, float cellSize)
    {
        Vector3 localPos = Quaternion.Inverse(originRotation) * (worldPos - originPos);

        float gridX = localPos.x / cellSize - footprint.x * 0.5f;
        float gridY = localPos.z / cellSize - footprint.y * 0.5f;

        return new Vector2Int(Mathf.RoundToInt(gridX), Mathf.RoundToInt(gridY));
    }


    /// <summary>
    /// 가구 Grid 셀의 월드 중심 위치를 반환
    /// </summary>
    /// <param name="cell">반환 Grid 셀</param>
    /// <param name="worldY">반환 위치에 적용할 월드 높이</param>
    /// <returns>셀의 월드 중심 위치</returns>
    public static Vector3 CellToWorldCenter(Vector2Int cell, float worldY, Vector3 originPos, Quaternion originRotation, float cellSize)
    {
        Vector3 localPos = new Vector3((cell.x + 0.5f) * cellSize, worldY, (cell.y + 0.5f) * cellSize);

        
        return originPos + originRotation * localPos;
    }

    /// <summary>
    /// 가구 Grid 셀이 차지하는 영역의 월드 중심 위치를 반환
    /// </summary>
    /// <param name="cells">중심 위치를 계산할 Grid 셀 목록</param>
    /// <param name="worldY">반환 위치에 적용할 높이</param>
    /// <returns>Grid 셀 영역의월드 중심 위치</returns>
    public static Vector3 GetCellsWorldCenter(IReadOnlyList<Vector2Int> cells, float worldY,Vector3 originPos, Quaternion originRotation, float cellSize)
    {
        Vector3 totalPos = Vector3.zero;

        foreach(Vector2Int cell in cells)
        {
            totalPos += CellToWorldCenter(cell, worldY, originPos, originRotation, cellSize);
        }

        return totalPos / cells.Count;
    }
}
