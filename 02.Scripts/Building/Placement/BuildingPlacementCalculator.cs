using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 단일 건물 배치 계산으로 결정된 셀, 위치, 회전과 방향 정보를 보관
/// 배치 가능 여부를 판단하거나 실제 건물을 생성하지는 않음
/// </summary>
public sealed class BuildingPlacementCandidate
{
    public Vector2Int StartCell { get; }
    public IReadOnlyList<Vector2Int> OccupiedCells { get; }
    public Vector3 Pos { get; }
    public Quaternion Rotation { get; }
    public ConnectorDirection Dir { get; }
    public int RotationStep { get; }

    public BuildingPlacementCandidate(Vector2Int startCell, IReadOnlyList<Vector2Int> occupiedCells, Vector3 pos, Quaternion rotation, ConnectorDirection dir, int rotationStep)
    {
        StartCell = startCell;
        OccupiedCells = occupiedCells;
        Pos = pos;
        Rotation = rotation;
        Dir = dir;
        RotationStep = rotationStep;
    }
}

/// <summary>
/// BuildingDefine, BuildingGrid와 포인터 위치를 이용해 단일 건물의 배치 후보를 계산
/// 셀 점유 가능 여부를 판단하거나 실제 건물을 배치하지 않음
/// </summary>
public class BuildingPlacementCalculator
{
    /// <summary>
    /// 월드 위치와 회전 단계로 건물의 시작 셀, 점유 셀, 배치 위치와 회전을 계산
    /// 유요한 점유 셀을 게산할 수 없으면 배치 후보를 생성하지 않음
    /// </summary>
    /// <param name="define">배치할 건물의 정적 데이터</param>
    /// <param name="grid">좌표 변환에 사용할 Grid</param>
    /// <param name="worldPos">포인터가 가리키는 월드 위치</param>
    /// <param name="rotationStep">건물의 회전 단계</param>
    /// <param name="candidata">건물의 배치 후보</param>
    /// <returns>배치 후보 생성 성공 여부</returns>
    public static bool TryCreateCandidate(BuildingDefine define, BuildingGrid grid, Vector3 worldPos, int rotationStep, out BuildingPlacementCandidate candidata)
    {
        candidata = null;

        if (define == null || grid == null)
            return false;

        int normalStep = (rotationStep % 4 + 4) % 4;

        ConnectorDirection dir = GetDirectionFromRotationStep(normalStep);

        Quaternion rotation = Quaternion.Euler(0f, normalStep * 90f, 0f);

        Vector2Int footprint = define.GridFootPrint;

        Vector2Int rotatedFootprint = normalStep % 2 == 0 ? footprint : new Vector2Int(footprint.y, footprint.x);

        Vector2Int startCell = grid.WorldToStartCell(worldPos, rotatedFootprint);

        List<Vector2Int> occupiedCells = GridFootprintCalculator.GetOccupiedCells(startCell, define.GridFootPrint, dir);

        if (occupiedCells == null || occupiedCells.Count == 0)
            return false;

        Vector3 pos = grid.GetCellsWorldCenter(occupiedCells, worldPos.y);

        candidata = new BuildingPlacementCandidate(startCell, occupiedCells, pos, rotation, dir, rotationStep);

        return true;
    }

    /// <summary>
    /// Y축 회전 값을 받아 90도 단위의 회전 단계로 변환
    /// </summary>
    /// <param name="rotation">회전 값</param>
    /// <returns>변환한 회전 단계</returns>
    public static int GetRotationStep(Quaternion rotation)
    {
        float angle = rotation.eulerAngles.y % 360f;

        return Mathf.RoundToInt(angle / 90f) % 4;
    }
    
    /// <summary>
    /// 회전 단계를 건물의 배치 방향으로 변환
    /// </summary>
    /// <param name="rotationStep">회전 단계</param>
    /// <returns>회전 단계에 해당하는 Connetor의 방향</returns>
    public static ConnectorDirection GetDirectionFromRotationStep(int rotationStep)
    {
        switch (rotationStep)
        {
            case 0:
                return ConnectorDirection.Forward;
            case 1:
                return ConnectorDirection.Right;
            case 2:
                return ConnectorDirection.Back;
            case 3:
                return ConnectorDirection.Left;
            default:
                return ConnectorDirection.Forward;
        }
    }
}
