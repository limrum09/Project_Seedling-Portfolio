using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 가구의 Grid 크기와 회전 단계로 실제 점유 셀을 계산
/// 셀 점유 가능 여부와 등록 상태는 관리하지 않음
/// </summary>
public static class FurnitureFootprintCalculator
{
    /// <summary>
    /// 90도 회전 단계가 적용된 가구의 Grid크기를 반환
    /// </summary>
    /// <param name="footprint">회전 전 가구의 grid크기</param>
    /// <param name="rotationStep">90도 단위의 회전 단계</param>
    /// <returns>회전이 적용된 Grid 크기</returns>
    public static Vector2Int GetRotatedFootprint(Vector2Int footprint, int rotationStep)
    {
        int step = (rotationStep % 4 + 4) % 4;

        if (step == 1 || step == 3)
            return new Vector2Int(footprint.y, footprint.x);

        return footprint;
    }

    public static List<Vector2Int> GetOccupiedCells(Vector2Int startCell, Vector2Int footprint, int rotationStep)
    {
        Vector2Int rotatedFootprint = GetRotatedFootprint(footprint, rotationStep);

        List<Vector2Int> cells = new List<Vector2Int>(rotatedFootprint.x * rotatedFootprint.y);

        for(int i = 0; i < rotatedFootprint.x; i++)
        {
            for(int j = 0; j < rotatedFootprint.y; j++)
            {
                cells.Add(startCell + new Vector2Int(i, j));
            }
        }

        return cells;
    }
}
