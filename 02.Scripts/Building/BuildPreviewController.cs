using UnityEngine;

/// <summary>
/// 선물 배치와 Preview의 생성, 위치, 회전과 유효성 시각화를 관리
/// 실제 배치 가능 여부를 판단하거나 건물을 생성하지는 않음
/// </summary>
public class BuildPreviewController : MonoBehaviour
{
    [SerializeField]
    private Material validMaterial;
    [SerializeField]
    private Material invalidMaterial;

    private GameObject currentPreview;
    private BuildingDefine currentDefine;
    private BuildingConnector[] previewConnectors;
    private Renderer previewRenderer;
    private bool currentValidStat;

    /// <summary>
    /// 지정한 건물의 Preview를 생성하고 배치 표시에 필요한 구성 요소를 준비
    /// 같은 정의의 preview가 이미 표시 중이면 기존 상태를 유지
    /// </summary>
    /// <param name="getDefine"></param>
    public void Show(BuildingDefine getDefine)
    {
        if (getDefine == null)
            return;

        if (currentDefine == getDefine && currentPreview != null)
            return;

        Hide();

        currentDefine = getDefine;

        currentPreview = Instantiate(currentDefine.PreviewPrefab);

        // Preview가 배치 판정 Raycast나 물리 충돌에 관여라지 않고록 Collider를 비활성화
        foreach (Collider collider in currentPreview.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;

        previewRenderer = currentPreview.GetComponentInChildren<Renderer>();
        previewConnectors = currentPreview.GetComponentsInChildren<BuildingConnector>();

        SetValid(currentValidStat);
    }

    /// <summary>
    /// 현재 Preview를 제거하고 사용 중인 Preview 정보를 초기화
    /// </summary>
    public void Hide()
    {
        if (currentPreview != null)
            Destroy(currentPreview);

        currentPreview = null;
        currentDefine = null;
        previewRenderer = null;
    }

    /// <summary>
    /// 현재 Preview의 우치롸 회전을 변경
    /// Preview가 없으면 상태를 변경하지 않음
    /// </summary>
    /// <param name="pos">이동할 월드 위치</param>
    /// <param name="rotate">회전할 회전 값</param>
    public void SetPos(Vector3 pos, Quaternion rotate)
    {
        if (currentPreview == null)
            return;

        currentPreview.transform.SetPositionAndRotation(pos, rotate);
    }

    /// <summary>
    /// 현재 배치 유효 상태를 저장, Preview의 모든 Material을 해당 상태의 material로 변경
    /// </summary>
    /// <param name="isValid">배치 가능한 상태</param>
    public void SetValid(bool isValid)
    {
        currentValidStat = isValid;

        Material[] materials = previewRenderer.sharedMaterials;

        Material targetMaterial = currentValidStat ? validMaterial : invalidMaterial;

        for(int i = 0; i < previewRenderer.sharedMaterials.Length; i++)
        {
            materials[i] = targetMaterial;
        }

        previewRenderer.sharedMaterials = materials;
    }

    /// <summary>
    /// 지정한 방향의 Preview Connector를 찾아 대상 Connector에 맞춤
    /// 필요한 Connector를 찾기 못하면 Preview를 변경하지 않음
    /// </summary>
    /// <param name="previewDir">대상과 연결할 Preview Connector의 방향</param>
    /// <param name="targetConnector">Preivew를 맞출 대상</param>
    public void SnapToConnector(ConnectorDirection previewDir, BuildingConnector targetConnector)
    {
        BuildingConnector previewConnector = GetPreviewConnector(previewDir);

        if (previewConnector == null || targetConnector == null)
            return;

        SnapToConnector(previewConnector, targetConnector);
    }

    /// <summary>
    /// 두 Connector가 서로 마주보도록 Preview를 회전한 후 위치를 일치시킴
    /// Preview 또는 Connector가 없으면 상태를 변경하지 않음
    /// </summary>
    /// <param name="previewConnecotr">이동할 Preview의 Connector</param>
    /// <param name="targetConnector">정렬 기준이 되는 대상 Connector</param>
    public void SnapToConnector(BuildingConnector previewConnecotr, BuildingConnector targetConnector)
    {
        if (currentPreview == null)
            return;

        if (previewConnecotr == null || targetConnector == null)
            return;

        Transform previewRoot = currentPreview.transform;

        // 두 Connector가 서로 마주보도록 Preview의 회전 보정값을 적용
        Quaternion targetRatation = Quaternion.LookRotation(-targetConnector.transform.forward, targetConnector.transform.up);

        Quaternion rotationDelta = targetRatation * Quaternion.Inverse(previewConnecotr.transform.rotation);

        previewRoot.rotation = rotationDelta * previewRoot.rotation;

        // 회전으로 변경된 Preview Connector 위치를 기준으로 대상 위치까지 이동
        Vector3 positionDelta = targetConnector.transform.position - previewConnecotr.transform.position;

        previewRoot.position += positionDelta;
    }

    /// <summary>
    /// 현재 Preivew에서 지정한 방향을 가진 Connector를 검색
    /// </summary>
    /// <param name="direction">검색할 Connector 방향</param>
    /// <returns>처음 발견한 Connector, 해당 방향의 Connector가 없으면 null</returns>
    public BuildingConnector GetPreviewConnector(ConnectorDirection direction)
    {
        if (previewConnectors == null || previewConnectors.Length == 0)
            return null;

        foreach(BuildingConnector connector in previewConnectors)
        {
            if (connector.ConnectDir == direction)
                return connector;
        }

        return null;
    }
}
