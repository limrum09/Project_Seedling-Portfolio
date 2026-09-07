using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 하나의 생산 아이템과 생산 규칙을 정의
/// </summary>
[Serializable]
public sealed class ProductionOutputDefine
{
    [SerializeField]
    private ItemDefine outputItem;
    [SerializeField, Min(1)]
    private int outputAmount = 1;
    [SerializeField, Min(0.01f)]
    private float intervalSeconds = 60f;
    [SerializeField, Min(1)]
    private int maxStoredAmount = 1;

    public ItemDefine OutputItem => outputItem;
    public int OutputAmount => outputAmount;
    public float IntervalSeconds => intervalSeconds;
    public int MaxStoredAmount => maxStoredAmount;
}

/// <summary>
/// 설치물이 사용할 생산 항목 목록을 정적 데이터로 정의
/// </summary>
[CreateAssetMenu(fileName = "_Production_Define", menuName = "Project/Production/Production Define")]
public sealed class ProductionDefine : ScriptableObject
{
    [SerializeField]
    private List<ProductionOutputDefine> outputs = new List<ProductionOutputDefine>();

    public IReadOnlyList<ProductionOutputDefine> Outputs => outputs;
}