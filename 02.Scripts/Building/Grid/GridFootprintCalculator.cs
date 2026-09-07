using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 건물의 기준 셀, 크기와 방향을 이용해 건물이 점유할 그리드에서의 셀을 계산
/// 계산만 담당하기에 Register에 등록하지 않으며,
/// 셀 점유 상태를 확인하거나 변경하지는 않음
/// </summary>
public static class GridFootprintCalculator
{
    /// <summary>
    /// 기준 셀부터 건물 크기와 방향에 해당하는 모든 점유 셀을 계산
    /// 크기가 유효하지 않거나 지원하지 않는 방향이면 빈 목록을 반환
    /// Forward, Back은 기존의 크기를 사용하고, Left, Right는 가로와 세로를 ryghks하여 계산
    /// </summary>
    /// <param name="anchorCell">건물 점유 계산을 시작할 셀</param>
    /// <param name="footpirnt">건물이 그리드에서 차지하는 셀 개수</param>
    /// <param name="dir">건물의 배치 방향</param>
    /// <returns>건물이 점유한 모든 그리드의 셀 좌표</returns>
    public static List<Vector2Int> GetOccupiedCells(Vector2Int anchorCell, Vector2Int footpirnt, ConnectorDirection dir)
    {
        List<Vector2Int> occupiedCells = new List<Vector2Int>();

        if(footpirnt.x <= 0 ||  footpirnt.y <= 0)
            return occupiedCells;

        int width;
        int length;

        switch (dir)
        {
            case ConnectorDirection.Forward:
            case ConnectorDirection.Back:
                width = footpirnt.x;
                length = footpirnt.y;
                break;
            case ConnectorDirection.Right:
            case ConnectorDirection.Left:
                width = footpirnt.y;
                length = footpirnt.x;
                break;
            default: 
                return occupiedCells;
        }

        for(int i = 0; i < width; i++)
        {
            for(int j = 0; j < length; j++)
            {
                Vector2Int cell = new Vector2Int(anchorCell.x + i, anchorCell.y + j);

                occupiedCells.Add(cell);
            }
        }

        return occupiedCells;
    }
}
