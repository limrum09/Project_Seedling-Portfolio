using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 광물 Prefab 하나의 Spawn 가중치와 크기 범위 설정
/// </summary>
[Serializable]
public sealed class MineralSpawnEntry
{
    [SerializeField]
    private MineralRuntime prefab;
    [SerializeField, Min(0.1f)]
    private float weight = 1f;
    [SerializeField, Min(1f)]
    private int minimumSize = 1;
    [SerializeField, Min(1f)]
    private int maximumSize = 1;

    public MineralRuntime Prefab => prefab;
    public float Weight => weight;
    public int MinimumSize => minimumSize;
    public int MaximumSize => maximumSize;
}

/// <summary>
/// 여러 MineralSpawner가 공유할 광물 종류와 Spawn 비율 설정
/// 영역별 개체 수와 Respawn 시간은 포함하지 않음
/// </summary>
[CreateAssetMenu(fileName = "MineralSpawnProfile", menuName = "Project/Mineral/Spawn Profile")]
public sealed class MineralSpawnProfile : ScriptableObject
{
    [SerializeField]
    private List<MineralSpawnEntry> entries = new List<MineralSpawnEntry>();

    public IReadOnlyList<MineralSpawnEntry> Entries => entries;
}