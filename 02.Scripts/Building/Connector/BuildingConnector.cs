using UnityEngine;

/// <summary>
/// Connector가 연결하는 건물 영역의 종류
/// </summary>
public enum ConnectorType
{
    Room,
    Corridor,
    Default
}

/// <summary>
/// 건물을 기준으로 Connector가 배치된 방향을 정의
/// </summary>
public enum ConnectorDirection
{
    Forward,
    Back,
    Left,
    Right,
    None
}

/// <summary>
/// 건물 연결 지점의 방향, 종류와 상대 Connector 참조를 관리하고 점유 상태에 따라 선택 표시와 Door 상태를 갱신
/// Connecotr 사이의 연결 가능 여부는 판단하지 않음
/// </summary>
public class BuildingConnector : MonoBehaviour
{
    [SerializeField]
    private Door door;
    [SerializeField]
    private ConnectorDirection connectDir;
    [SerializeField]
    private ConnectorType type;
    [SerializeField]
    private Collider selectCollider;
    [SerializeField]
    private Material basicMaterial;
    [SerializeField]
    private Material selectMaterial;
    [SerializeField]
    private MeshRenderer indicator;

    private BuildingConnector connectedConnector;

    public ConnectorDirection ConnectDir => connectDir;
    public ConnectorType Type => type;
    public Transform BuildTransform => this.transform;
    public bool IsOccupied => connectedConnector != null;   // 다른 Connector가 연결되어 있는 지 반환, 연결되어 있다면 true를 반환
    public BuildingConnector ConnectedConnector => connectedConnector;  // 현재 연결 상대를 반환, 연결이 없다면 null 반환

    private void OnEnable()
    {
        if(door != null)
            door.BindConnector(this);
    }

    /// <summary>
    /// 커넥터의 선택 표시 여부에 따라 Indicator의 활성화와 Material을 변경
    /// 이미 점유된 커넥터는 선택 상태로 표시하지 않음
    /// </summary>
    /// <param name="display">선택 상태 표시 여부</param>
    public void SetDisplay(bool display)
    {
        bool canSelect = display && !IsOccupied;

        Material currentMaterail = canSelect ? selectMaterial : basicMaterial;

        indicator.gameObject.SetActive(canSelect);
        indicator.material = currentMaterail;
    }

    /// <summary>
    /// 커넥터의 선택 가능 여부에 따라 표시 오브젝트와 선택 Collider를 활성화
    /// 이미 점유된 커넥터는 선택 가능한 상태로 변경하지 않음
    /// </summary>
    /// <param name="selectable">커넥터의 선택 가능 여부</param>
    public void SetSelectable(bool selectable)
    {
        SetVisible(selectable);
        SetInteractable(selectable);
    }

    /// <summary>
    /// Connector의 선택 표시를 활성화 하거나 비활성화
    /// 점유된 Connector는 표시하지 않음
    /// </summary>
    /// <param name="isVisible">선택 표시 활성화 여부</param>
    public void SetVisible(bool isVisible)
    {
        bool canShow = isVisible && !IsOccupied;

        indicator.gameObject.SetActive(canShow);
    }

    /// <summary>
    /// Connector의 선택 Collider를 활성화하거나 비활성화
    /// 점유된 Connector는 상호작용할 수 없도록 유지
    /// </summary>
    /// <param name="interactable"></param>
    public void SetInteractable(bool interactable)
    {
        bool canInteract = interactable && !IsOccupied;

        selectCollider.enabled = canInteract;
    }

    /// <summary>
    /// 상대 Connector 참조를 설정하고 선택 표시와 상호작용을 비활성화한 뒤, Door에 점유 상태를 전달
    /// 상대 Connector의 연결 상태는 변경하지 않기에 양방향 설정은 호출부에서 처리해야 함
    /// </summary>
    /// <param name="other">연결 상대로 저장할 상대 Connector</param>
    public void SetConnector(BuildingConnector other)
    {
        connectedConnector = other;

        SetVisible(false);
        SetInteractable(false);

        door.ChangedOccupied(IsOccupied);
    }

    /// <summary>
    /// 연결된 Connector 참조를 제거하고, 선택 표시와 상호작용을 비활성화 한 뒤, Door에 점유 해제 상태를 전달
    /// </summary>
    public void ClearConnector()
    {
        connectedConnector = null;

        SetVisible(false);
        SetInteractable(false);

        door.ChangedOccupied(IsOccupied);
    }
}
