using UnityEngine;

/// <summary>
/// 가구 Preview의 생성, 위치, 회전과 배치 유효성 시각화를 관리
/// 배치 가능 여부를 판단하거나 실제 가구를 설치하지 않음
/// </summary>
public class FurniturePreviewController : MonoBehaviour
{
    [SerializeField]
    private Material validMaterial;
    [SerializeField]
    private Material invalidMaterial;

    private FurnitureDefine currentDefine;
    private FurnitureRuntime currentPreview;
    private Renderer[] previewRenderers;
    private bool currentValidState;

    /// <summary>
    /// 지정한 가구의 Prefab을 이용해 Preview를 생성
    /// 같은 가구의 Preview가 표시 중이면 기존 상태를 유지
    /// </summary>
    /// <param name="define">표시할 가구의 정적 데이터</param>
    public void Show(FurnitureDefine define)
    {
        if (define == null)
            return;

        if (currentDefine == define && currentPreview != null)
            return;

        Hide();

        currentDefine = define;
        currentPreview = Instantiate(currentDefine.Prefab);

        if(currentPreview.PreviewForward != null )
            currentPreview.PreviewForward.gameObject.SetActive(true);

        Collider[] colliders = currentPreview.GetComponentsInChildren<Collider>(true);

        foreach (Collider previewCollider in colliders)
        {
            previewCollider.enabled = false;
        }

        previewRenderers = currentPreview.GetComponentsInChildren<Renderer>(true);

        SetValid(false);
    }

    /// <summary>
    /// 현재 Preview의 월드 위치와 회전을 변경
    /// Preview가 없으면 상태를 변경하지 않음
    /// </summary>
    /// <param name="pos">Preview를 이동할 월드 위치</param>
    /// <param name="rotation">Preview에 적용할 월드 회전</param>
    public void SetPose(Vector3 pos, Quaternion rotation)
    {
        if (currentPreview == null)
            return;

        currentPreview.transform.SetPositionAndRotation(pos, rotation);
    }

    /// <summary>
    /// 현재 배치 유효 상태를 저장하고 모든 Renderer의 Material을 변경
    /// </summary>
    /// <param name="isValid">현재 위치에 배치할 수 있는지 여부</param>
    public void SetValid(bool isValid)
    {
        currentValidState = isValid;

        Material targetMaterial = currentValidState ? validMaterial : invalidMaterial;

        foreach (Renderer previewRenderer in previewRenderers)
        {
            Material[] materials = previewRenderer.sharedMaterials;

            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = targetMaterial;
            }

            previewRenderer.sharedMaterials = materials;
        }
    }

    /// <summary>
    /// 현재 Preview를 제거하고 사용 중인 가구 정보를 초기화
    /// </summary>
    public void Hide()
    {
        if (currentPreview != null)
        {
            if (currentPreview.PreviewForward != null)
                currentPreview.PreviewForward.gameObject.SetActive(false);
            Destroy(currentPreview.gameObject);
        }   

        currentDefine = null;
        currentPreview = null;
        previewRenderers = null;
        currentValidState = false;
    }
}