using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 건설 모드 입력을 해석하고 Raycast 결과를 Building에 전달
/// 건물을 직접 배치, 수정, 연결 또는 철거하지 않음
/// </summary>
public class BuildInputController : MonoBehaviour
{
    [SerializeField]
    private Building building;
    [SerializeField]
    private LayerMask groundMask;
    [SerializeField]
    private LayerMask buildingMask;

    [SerializeField, Min(50f)]
    private float rayDistance = 100f;

    private Camera worldCamera;
    private IPlacementInputSource inputSource;

    /// <summary>
    /// Building 입력과 Pointer Ray에 사용할 접근점 연결
    /// </summary>
    /// <param name="source">Placement 입력 접근점</param>
    /// <param name="getWorldCamera">Pointer Ray에 사용할 World Camera</param>
    public void BindInput(IPlacementInputSource source, Camera getWorldCamera)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        if (getWorldCamera == null)
            throw new ArgumentNullException(nameof(getWorldCamera));

        inputSource = source;
        worldCamera = getWorldCamera;
    }

    /// <summary>
    /// 현재 Building 상태에 맞는 배치 입력 처리
    /// </summary>
    private void Update()
    {
        if (!building.IsBuildMode)
            return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (!inputSource.ModifierHeld && inputSource.SecondaryPressedThisFrame && building.CurrentState != BuildState.PreviewCorridor)
        {
            if (building.CurrentState == BuildState.Idle)
                building.ExitBuildMode();
            else
                building.CancelCurrentAction();

            return;
        }

        if (inputSource.RotateClockwisePressedThisFrame)
            building.RotateCurrentBuilding(1);

        if (inputSource.RotateCounterClockwisePressedThisFrame)
            building.RotateCurrentBuilding(-1);

        Ray ray = CreatePointerRay();

        // 현재 건설 상태에 맞는 입력 처리로 전달
        switch (building.CurrentState)
        {
            case BuildState.Idle:
                UpdateIdleInput(ray);
                break;
            case BuildState.PreviewNewBuilding:
                UpdateSingleBuildingInput(ray);
                break;
            case BuildState.PreviewModifyBuilding:
                UpdateSingleBuildingInput(ray);
                break;
            case BuildState.PreviewCorridor:
                UpdateRouteInput(ray);
                break;
            case BuildState.BuildingSelected:
                UpdateBuildingSelectInput(ray);
                break;
        }
    }

    /// <summary>
    /// 건설 대기 상태에서 Pointer가 가리키는 Runtime 건물 선택 처리
    /// </summary>
    /// <param name="ray">현재 Pointer Ray</param>
    private void UpdateIdleInput(Ray ray)
    {
        if (!inputSource.PrimaryPressedThisFrame)
            return;

        if (TryGetBuilding(ray, out BuildingRuntime runtime))
            building.SelectBuilding(runtime);
    }

    /// <summary>
    /// 건물 선택 상태에서 다른 Runtime 건물 선택 처리
    /// </summary>
    /// <param name="ray">현재 Pointer Ray</param>
    private void UpdateBuildingSelectInput(Ray ray)
    {
        if (!inputSource.PrimaryPressedThisFrame)
            return;

        if (TryGetBuilding(ray, out BuildingRuntime runtime))
            building.SelectBuilding(runtime);
    }

    /// <summary>
    /// 신규 건물 배치 또는 건물 수정 상태의 지면 Pointer와 배치 입력 처리
    /// </summary>
    /// <param name="ray">현재 Pointer Ray</param>
    private void UpdateSingleBuildingInput(Ray ray)
    {
        if (!TryGetGroundPoint(ray, out Vector3 groundPoint))
            return;

        building.SetGroundPointer(groundPoint);

        if (inputSource.PrimaryPressedThisFrame)
            building.SetPrimaryClick();
    }

    /// <summary>
    /// 통로 Preview 상태의 Ground Pointer, 분기점 생성과 통로 배치 입력 처리
    /// </summary>
    /// <param name="ray">현재 Pointer Ray</param>
    private void UpdateRouteInput(Ray ray)
    {
        bool primaryPressed = inputSource.PrimaryPressedThisFrame;
        bool secondaryPressed = !inputSource.ModifierHeld && inputSource.SecondaryPressedThisFrame;

        if (!TryGetGroundPoint(ray, out Vector3 groundPoint))
        {
            building.ClearRoutePointer();
            return;
        }

        building.SetRoutePointer(groundPoint);

        if (secondaryPressed)
        {
            building.TrySetRouteTurn();
            return;
        }

        if (primaryPressed)
            building.SetPrimaryClick();
    }

    /// <summary>
    /// 현재 Pointer 위치와 World Camera를 이용해 Pointer Ray 생성
    /// </summary>
    /// <returns>현재 Pointer Ray</returns>
    private Ray CreatePointerRay()
    {
        return worldCamera.ScreenPointToRay(inputSource.PointerPosition);
    }

    /// <summary>
    /// Pointer Ray가 가리키는 Collider의 상위 Runtime 건물 확인
    /// </summary>
    /// <param name="ray">현재 Pointer Ray</param>
    /// <param name="runtime">찾은 Runtime 건물</param>
    /// <returns>Runtime 건물을 찾았으면 true</returns>
    private bool TryGetBuilding(Ray ray, out BuildingRuntime runtime)
    {
        runtime = null;

        if (!Physics.Raycast(ray, out RaycastHit hit, rayDistance, buildingMask, QueryTriggerInteraction.Ignore))
            return false;

        runtime = hit.collider.GetComponentInParent<BuildingRuntime>();

        return runtime != null;
    }

    /// <summary>
    /// Pointer Ray와 지면의 충돌 위치 확인
    /// </summary>
    /// <param name="ray">현재 Pointer Ray</param>
    /// <param name="groundPoint">찾은 지면 위치</param>
    /// <returns>지면 위치를 찾았으면 true</returns>
    private bool TryGetGroundPoint(Ray ray, out Vector3 groundPoint)
    {
        groundPoint = Vector3.zero;

        if (!Physics.Raycast(ray, out RaycastHit hit, rayDistance, groundMask, QueryTriggerInteraction.Collide))
            return false;

        groundPoint = hit.point;

        return true;
    }
}
