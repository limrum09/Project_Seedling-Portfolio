using UnityEngine;

[RequireComponent (typeof(OutlineVisual))]
/// <summary>
/// 월드에 설치된 가구 한개의 Runtime 상태를 보관
/// 가구 생성, 배치검증, Grid 등록은 다른 곳에서 담당
/// </summary>
public class FurnitureRuntime : MonoBehaviour, IWorldHoverTarget, IWorldInspectable, IProductionSource
{
    [SerializeField]
    private BoxCollider placementBounds;
    [SerializeField]
    private GameObject previewForward;
    [SerializeField]
    private FurnitureDefine define;

    private string instanceID;
    private FurnitureLocation location;
    private OutlineVisual visual;
    private ProductionRuntime production;

    public BoxCollider PlacementBounds => placementBounds;
    public string InstanceID => instanceID;
    public string DisplayName => define.DisplayName;
    public string Description => define.Description;
    public FurnitureDefine Define => define;
    public FurnitureLocation Location => location;
    public GameObject PreviewForward => previewForward;
    public ProductionRuntime Production => production;

    private void Awake()
    {
        visual = GetComponent<OutlineVisual>();
    }

    /// <summary>
    /// 설치된 가구의 정의와 위치 상태를 초기화
    /// </summary>
    /// <param name="getInstanceID">저장에 사용할 가구 Instance ID</param>
    /// <param name="getDefine">가구의 정적 저의</param>
    /// <param name="getLocation">가구의 설치 위치</param>
    public void Init(string getInstanceID, FurnitureDefine getDefine, FurnitureLocation getLocation)
    {
        instanceID = getInstanceID;
        define = getDefine;
        location = getLocation;

        if (getDefine.ProductionDefine != null)
            production = new ProductionRuntime(getDefine.ProductionDefine);
    }

    public void SetHovered(bool hovered)
    {
        visual.SetVisible(hovered);
    }

    /// <summary>
    /// 가구 이동이 확정된 후 Runtime 위치 상태를 변경
    /// Trandform 이동과 grid 점유 변경은 수행하지 않음
    /// </summary>
    /// <param name="newLocation">새로운 위치</param>
    public void SetLocation(FurnitureLocation newLocation) => location = newLocation;
}
