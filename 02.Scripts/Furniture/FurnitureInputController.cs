using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 가구 배치 입력을 해석하고 FurnitureController에 전달
/// 플레이어 이동이나 실제 가구 설치 규칙은 처리하지 않음
/// </summary>
public class FurnitureInputController : MonoBehaviour
{
    [SerializeField]
    private FurnitureController placementController;
    [SerializeField]
    private Camera worldCamera;
    [SerializeField]
    private LayerMask placementSurfaceMask;
    [SerializeField]
    private LayerMask furnitureMask;
    [SerializeField, Min(0.1f)]
    private float placementDistance = 30f;

    private IPlacementInputSource inputSource;

    /// <summary>
    /// Furniture 입력에 사용할 Placement Input 연결
    /// </summary>
    /// <param name="source">Placement 입력 접근점</param>
    public void BindInput(IPlacementInputSource source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        inputSource = source;
    }

    /// <summary>
    /// 가구 배치 상태에서 Pointer, 설치, 회전과 취소 입력 처리
    /// </summary>
    private void Update()
    {
        if (!placementController.IsActive)
            return;

        if (inputSource.SecondaryPressedThisFrame)
        {
            if (placementController.CurrentState == FurnitureState.SelectingFurniture)
                placementController.CancelPlaceMode();
            else
                placementController.CancelCurrentAction();

            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Ray ray = worldCamera.ScreenPointToRay(inputSource.PointerPosition);

        switch (placementController.CurrentState)
        {
            case FurnitureState.SelectingFurniture:
            case FurnitureState.SelectedFurniture:
                UpdateSelectionInput(ray);
                break;
            case FurnitureState.PreviewPlacement:
            case FurnitureState.PreviewRelocation:
                UpdatePreviewInput(ray);
                break;
            case FurnitureState.ConfirmRemove:
                break;
        }
    }

    /// <summary>
    /// Pointer Ray가 가리키는 Runtime 가구 확인
    /// </summary>
    /// <param name="ray">현재 Pointer Ray</param>
    /// <param name="runtime">찾은 Runtime 가구</param>
    /// <returns>Runtime 가구를 찾았으면 true</returns>
    private bool TryGetFurniture(Ray ray, out FurnitureRuntime runtime)
    {
        runtime = null;

        if (!Physics.Raycast(ray, out RaycastHit hit, placementDistance, furnitureMask, QueryTriggerInteraction.Ignore))
            return false;

        runtime = hit.collider.GetComponentInParent<FurnitureRuntime>();

        return runtime != null;
    }

    /// <summary>
    /// 가구 선택 상태의 Runtime 가구 선택 입력 처리
    /// </summary>
    /// <param name="ray">현재 Pointer Ray</param>
    private void UpdateSelectionInput(Ray ray)
    {
        if (!inputSource.PrimaryPressedThisFrame)
            return;

        if (TryGetFurniture(ray, out FurnitureRuntime runtime))
            placementController.SelectFurniture(runtime);
    }

    /// <summary>
    /// 가구 Preview 상태의 회전, 표면 Pointer와 배치 입력 처리
    /// </summary>
    /// <param name="ray">현재 Pointer Ray</param>
    private void UpdatePreviewInput(Ray ray)
    {
        if (inputSource.RotateClockwisePressedThisFrame)
            placementController.Rotate(1);

        if (inputSource.RotateCounterClockwisePressedThisFrame)
            placementController.Rotate(-1);

        if (!Physics.Raycast(ray, out RaycastHit surfaceHit, placementDistance, placementSurfaceMask, QueryTriggerInteraction.Ignore))
        {
            placementController.ClearSurfacePointer();
            return;
        }

        placementController.SetSurfacePointer(surfaceHit);

        if (inputSource.PrimaryPressedThisFrame)
            placementController.TryPlace();
    }
}
