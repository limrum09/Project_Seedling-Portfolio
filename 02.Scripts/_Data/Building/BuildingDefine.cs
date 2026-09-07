using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 건물의 배치 뱅식, 식별 정보, 프리펩 등 정적 데이터를 정의
/// 실제로 건물을 생성하거나 Runtime 상태를 관리하지 않는다.
/// 건물 생성은 BuildingService, 런타임 상태는 BuildingRuntime에서 관리
/// </summary>
[CreateAssetMenu(fileName = "Building__Define", menuName = "Project/BuildingDefine")]
public class BuildingDefine : ScriptableObject
{
    [SerializeField]
    private BuildingCategory category;
    [SerializeField]
    private BuildPlacementMode placementMode;
    [SerializeField]
    private string buildingUID;
    [SerializeField]
    private string displayName;
    [SerializeField]
    private List<ItemRequirement> materialItemRequirements = new();
    [SerializeField]
    private bool allowsFurniturePlacement;
    [SerializeField]
    private Sprite icon;
    [SerializeField]
    private BuildingRuntime buildingPrefab;
    [SerializeField]
    private ProductionDefine productionDefine;
    [SerializeField]
    private GameObject previewPrefab;

    [SerializeField]
    private Vector2Int gridFootPrint;
    [SerializeField]
    private Vector3 buildingSize = new Vector3(4f, 4f, 4f);
    [SerializeField]
    private Vector3 buildingOffset;

    public BuildingCategory Category => category;
    public BuildPlacementMode PlaceMode => placementMode;
    public string BuildingUID => buildingUID;
    public string DisplayName => displayName;
    public IReadOnlyList<ItemRequirement> MaterialItemRequirements => materialItemRequirements;
    public bool AllowsFurniturePlacement => allowsFurniturePlacement;
    public Sprite Icon => icon;
    public BuildingRuntime BuildingPrefab => buildingPrefab;
    public ProductionDefine ProductionDefine => productionDefine;
    public GameObject PreviewPrefab => previewPrefab;
    public Vector2Int GridFootPrint => gridFootPrint;
    public Vector3 BuildingSize => buildingSize;
    public Vector3 BuildingOffset => buildingOffset;
}
