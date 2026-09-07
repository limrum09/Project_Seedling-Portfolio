using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 생성된 건물이 사용하는 건물 정의와 하위 커넥터 목록을 관리
/// 건물 생성, 배치 검증과 그리드 등록은 담당하지 않음
/// </summary>
[RequireComponent(typeof(OutlineVisual))]
public class BuildingRuntime : MonoBehaviour, IWorldHoverTarget, IWorldInspectable, IProductionSource
{
    [SerializeField]
    private BuildingConnector[] connectors;
    [SerializeField]
    private OutlineVisual visual;
    [SerializeField]
    private BuildingDefine define;

    private ProductionRuntime production;
    private bool isHovered;
    private bool isSelected;

    public IReadOnlyList<BuildingConnector> Connectors => connectors;
    public BuildingDefine Define => define;
    public ProductionRuntime Production => production;
    public string DisplayName => define.DisplayName;
    public string Description => string.Empty;

    public bool HasAvailableConnector
    {
        get
        {
            EnsureConnectors();

            foreach (BuildingConnector connector in connectors)
            {
                if (connector != null && !connector.IsOccupied)
                    return true;
            }

            return false;
        }
    }

    public int ConnectedConnectorCount
    {
        get
        {
            EnsureConnectors();

            int count = 0;

            foreach(BuildingConnector connector in connectors)
            {
                if (connector != null && connector.IsOccupied)
                    count++;
            }

            return count;
        }
    }

    private void Awake()
    {
        EnsureConnectors();

        visual = GetComponent<OutlineVisual>();
    }

    private void EnsureConnectors()
    {
        if (connectors != null && connectors.Length > 0)
            return;

        // 바활성화된 오브젝트를 포함한 하위에 있는 모든 BuildingConnetor들을 수집
        connectors = GetComponentsInChildren<BuildingConnector>(true);
    }

    private void RefreshOutline()
    {
        visual.SetVisible(isHovered || isSelected);
    }

    /// <summary>
    /// Runtime 건물에 생성할 때 사용한 건물 정의 연결
    /// </summary>
    /// <param name="getDefine"></param>
    public void Init(BuildingDefine getDefine)
    {
        define = getDefine;

        if(getDefine.ProductionDefine != null)
            production = new ProductionRuntime(getDefine.ProductionDefine);
    }

    /// <summary>
    /// 건물이 보유한 모든 커넥터의 선택 가능 상태를 변경
    /// 이미 점유된 커넥터는 선택 가능한 상태로 변경하지 않음
    /// </summary>
    /// <param name="selecable">커넥터의 선택 가능 여부</param>
    public void SetConnectorsSelectable(bool selecable)
    {
        EnsureConnectors();

        foreach (BuildingConnector connector in connectors)
        {
            if (connector == null)
                continue;

            connector.SetSelectable(selecable && !connector.IsOccupied);
        }
    }

    /// <summary>
    /// 건물의 선택 상태를 반영하고 Outline 표시 갱신
    /// 선택이 해제되어도 Hover 중이면 표시를 유지
    /// </summary>
    /// <param name="selected">건물 선택 여부</param>
    public void SetSelected(bool selected)
    {
        isSelected = selected;

        RefreshOutline();
    }

    /// <summary>
    /// 건물의 Hover 상태를 반영하고 Outline 표시 갱신
    /// Hover가 해제되어도 선택 중이면 표시를 유지
    /// </summary>
    /// <param name="hovered">Pointer Hover 여부</param>
    public void SetHovered(bool hovered)
    {
        isHovered = hovered && production != null;

        RefreshOutline();
    }

    /// <summary>
    /// 요청한 방향과 일치하는 첫 번째 커넥터를 반환
    /// 일치하는 커넥터가 없으면 null을 반환
    /// </summary>
    /// <param name="dir">찾을 커넥터의 방향</param>
    /// <returns>방향과 일치하는 커넥터</returns>
    public BuildingConnector GetConnector(ConnectorDirection dir)
    {
        EnsureConnectors();

        foreach (BuildingConnector connector in connectors)
        {
            if (connector == null)
                continue;

            if(connector.ConnectDir == dir) 
                return connector;
        }

        return null;
    }
}
