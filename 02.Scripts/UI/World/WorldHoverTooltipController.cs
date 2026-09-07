using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 현재 World 조회 대상의 이름, 설명과 생산 상태를 공용 Tooltip에 표시
/// </summary>
public sealed class WorldHoverTooltipController : MonoBehaviour
{
    [Header("Canvas")]
    [SerializeField]
    private Canvas rootCanvas;
    [SerializeField]
    private RectTransform tooltipRect;
    [SerializeField]
    private CanvasGroup tooltipGroup;

    [Header("Text")]
    [SerializeField]
    private TMP_Text nameText;
    [SerializeField]
    private TMP_Text descriptionText;

    [Header("Position")]
    [SerializeField]
    private Vector2 pointerOffset = new Vector2(20f, -20f);
    [SerializeField]
    private Vector2 canvasPadding = new Vector2(10f, 10f);

    private RectTransform tooltipParent;
    private IWorldInspectionSource inspectionSource;
    private IPointerInputSource pointerInputSource;
    private IWorldInspectable currentTarget;
    private ProductionRuntime currentProduction;

    private readonly List<int> displayedStoredAmounts = new List<int>();
    private readonly List<int> displayedElapsedTenths = new List<int>();
    private readonly StringBuilder descriptionBuilder = new StringBuilder();

    private void Awake()
    {
        ValidateConfiguration();

        tooltipParent = tooltipRect.parent as RectTransform;

        Hide();
    }

    private void Update()
    {
        if (!IsCurrentTargetAvailable())
        {
            if (currentTarget != null)
                ClearCurrentTarget();

            return;
        }

        if (HasProductionDisplayChanged())
        {
            Show(currentTarget.DisplayName, BuildDescription());
            return;
        }

        UpdateTooltipPosition(pointerInputSource.PointerPosition);
    }

    private void OnDestroy()
    {
        Unbind();
    }

    /// <summary>
    /// Tooltip 표시에 필요한 필수 참조 검증
    /// </summary>
    private void ValidateConfiguration()
    {
        if (rootCanvas == null)
            throw new InvalidOperationException("World Hover Tooltip에 Root Canvas가 연결되지 않음");

        if (tooltipRect == null)
            throw new InvalidOperationException("World Hover Tooltip에 Tooltip Rect가 연결되지 않음");

        if (tooltipGroup == null)
            throw new InvalidOperationException("World Hover Tooltip에 Canvas Group이 연결되지 않음");

        if (nameText == null)
            throw new InvalidOperationException("World Hover Tooltip에 Name Text가 연결되지 않음");

        if (descriptionText == null)
            throw new InvalidOperationException("World Hover Tooltip에 Description Text가 연결되지 않음");

        if (!(tooltipRect.parent is RectTransform))
            throw new InvalidOperationException("World Hover Tooltip의 부모가 RectTransform이 아님");

        if (!tooltipRect.IsChildOf(rootCanvas.transform))
            throw new InvalidOperationException("World Hover Tooltip이 지정된 Root Canvas의 하위가 아님");

        if (rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay && rootCanvas.worldCamera == null)
            throw new InvalidOperationException("World Hover Tooltip의 Canvas Camera가 설정되지 않음");
    }

    /// <summary>
    /// 현재 조회 대상이 존재하고 파괴되지 않았는지 확인
    /// </summary>
    /// <returns>현재 대상을 사용할 수 있으면 true</returns>
    private bool IsCurrentTargetAvailable()
    {
        if (currentTarget == null)
            return false;

        if (currentTarget is UnityEngine.Object unityObject && unityObject == null)
            return false;

        return true;
    }

    /// <summary>
    /// 조회 대상의 생산 상태를 연결하고 이름과 설명을 표시
    /// 생산 기능이 없는 건물은 Tooltip을 숨김
    /// 이전 대상의 표시 수량 기록은 초기화
    /// </summary>
    /// <param name="target">새로운 조회 대상</param>
    private void HandleInspectionTargetChanged(IWorldInspectable target)
    {
        currentTarget = target;
        currentProduction = null;

        displayedStoredAmounts.Clear();

        if (!IsCurrentTargetAvailable())
        {
            ClearCurrentTarget();
            return;
        }

        if (currentTarget is BuildingRuntime building && building.Production == null)
        {
            ClearCurrentTarget();
            return;
        }

        if (currentTarget is IProductionSource productionSource)
            currentProduction = productionSource.Production;

        Show(currentTarget.DisplayName, BuildDescription());
    }

    /// <summary>
    /// 현재 생산 항목의 보관량과 0.1초 단위 경과 시간이 마지막 표시값과 다른지 확인
    /// 생산 기능이 없는 대상은 변경 없음으로 처리
    /// </summary>
    /// <returns>표시할 보관량이나 경과 시간이 변경되었으면 true</returns>
    private bool HasProductionDisplayChanged()
    {
        if (currentProduction == null)
            return false;

        IReadOnlyList<ProductionOutputRuntime> outputs = currentProduction.Outputs;

        if (displayedStoredAmounts.Count != outputs.Count)
            return true;

        for (int i = 0; i < outputs.Count; i++)
        {
            ProductionOutputRuntime output = outputs[i];

            if (displayedStoredAmounts[i] != output.StoredAmount)
                return true;

            int elapsedTenths = Mathf.FloorToInt(output.ElapsedSeconds * 10f);

            if (displayedElapsedTenths[i] != elapsedTenths)
                return true;
        }

        return false;
    }

    /// <summary>
    /// 기본 설명 뒤에 생산 항목별 이름, 생산개수, 경과 시간과 보관량을 구성
    /// 경과 시간은 소수점 한 자리까지 표시하고 다음 변경 확인에 사용할 값을 기록
    /// </summary>
    /// <returns>Tooltip의 Description에 표시할 문자열</returns>
    private string BuildDescription()
    {
        descriptionBuilder.Clear();
        displayedStoredAmounts.Clear();
        displayedElapsedTenths.Clear();

        string baseDescription = currentTarget.Description;

        if (!string.IsNullOrWhiteSpace(baseDescription))
            descriptionBuilder.Append(baseDescription);

        if (currentProduction == null)
            return descriptionBuilder.ToString();

        foreach (ProductionOutputRuntime output in currentProduction.Outputs)
        {
            ProductionOutputDefine outputDefine = output.Define;
            int storedAmount = output.StoredAmount;
            int elapsedTenths = Mathf.FloorToInt(output.ElapsedSeconds * 10f);

            if (descriptionBuilder.Length > 0)
                descriptionBuilder.Append("\n\n");

            descriptionBuilder.Append(output.OutputItem.DisplayName + '\n');
            descriptionBuilder.Append($"생산 단위 : {outputDefine.OutputAmount}\n");
            descriptionBuilder.Append($"생산 시간 : {elapsedTenths / 10}초 / {outputDefine.IntervalSeconds:0}초\n");
            descriptionBuilder.Append($"생산 개수 : {storedAmount} / {outputDefine.MaxStoredAmount}");

            displayedStoredAmounts.Add(storedAmount);
            displayedElapsedTenths.Add(elapsedTenths);
        }

        return descriptionBuilder.ToString();
    }

    /// <summary>
    /// 현재 조회 대상과 생산 표시 기록을 비우고 Tooltip을 숨김
    /// </summary>
    private void ClearCurrentTarget()
    {
        currentTarget = null;
        currentProduction = null;

        displayedStoredAmounts.Clear();
        displayedElapsedTenths.Clear();
        descriptionBuilder.Clear();

        Hide();
    }

    /// <summary>
    /// 지정한 이름과 설명을 Tooltip에 표시
    /// </summary>
    /// <param name="displayName">표시할 이름</param>
    /// <param name="description">표시할 설명</param>
    private void Show(string displayName, string description)
    {
        nameText.text = displayName;

        bool hasDescription = !string.IsNullOrWhiteSpace(description);

        descriptionText.gameObject.SetActive(hasDescription);

        descriptionText.text = hasDescription ? description : string.Empty;

        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);

        tooltipGroup.alpha = 1f;
        tooltipGroup.interactable = false;
        tooltipGroup.blocksRaycasts = false;

        UpdateTooltipPosition(pointerInputSource.PointerPosition);
    }

    /// <summary>
    /// Tooltip을 숨기고 Pointer Raycast 방해를 차단
    /// </summary>
    private void Hide()
    {
        tooltipGroup.alpha = 0f;
        tooltipGroup.interactable = false;
        tooltipGroup.blocksRaycasts = false;
    }

    /// <summary>
    /// Tooltip을 마우스 위치로 이동하고 부모 Rect 안으로 제한
    /// </summary>
    /// <param name="screenPosition">현재 마우스 화면 좌표</param>
    private void UpdateTooltipPosition(Vector2 screenPosition)
    {
        Camera canvasCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(tooltipParent, screenPosition, canvasCamera, out Vector2 localPosition))
            return;

        Vector2 desiredPosition = localPosition + pointerOffset;

        Rect parentBounds = tooltipParent.rect;
        Vector2 tooltipSize = tooltipRect.rect.size;
        Vector2 tooltipPivot = tooltipRect.pivot;

        float minimumX = parentBounds.xMin + tooltipSize.x * tooltipPivot.x + canvasPadding.x;
        float maximumX = parentBounds.xMax - tooltipSize.x * (1f - tooltipPivot.x) - canvasPadding.x;

        float minimumY = parentBounds.yMin + tooltipSize.y * tooltipPivot.y + canvasPadding.y;
        float maximumY = parentBounds.yMax - tooltipSize.y * (1f - tooltipPivot.y) - canvasPadding.y;

        desiredPosition.x = Mathf.Clamp(desiredPosition.x, minimumX, maximumX);
        desiredPosition.y = Mathf.Clamp(desiredPosition.y, minimumY, maximumY);

        Vector3 currentLocalPosition = tooltipRect.localPosition;

        tooltipRect.localPosition = new Vector3(desiredPosition.x, desiredPosition.y, currentLocalPosition.z);
    }

    /// <summary>
    /// 현재 Pointer Hover 정보를 제공할 Source 연결
    /// 기존 Source가 있으면 먼저 연결 해제
    /// </summary>
    /// <param name="source">연결할 World Inspection Source</param>
    /// <param name="pointerSource">연결할 Pointer 입력 접근점</param>
    public void Bind(IWorldInspectionSource source, IPointerInputSource pointerSource)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        if (pointerSource == null)
            throw new ArgumentNullException(nameof(pointerSource));

        Unbind();

        inspectionSource = source;
        pointerInputSource = pointerSource;

        inspectionSource.OnInspectionTargetChanged += HandleInspectionTargetChanged;

        HandleInspectionTargetChanged(inspectionSource.CurrentInspectionTarget);
    }

    /// <summary>
    /// 현재 Inspection Source 이벤트 연결 해제
    /// </summary>
    public void Unbind()
    {
        if (inspectionSource != null)
        {
            inspectionSource.OnInspectionTargetChanged -= HandleInspectionTargetChanged;
        }

        inspectionSource = null;
        pointerInputSource = null;
        currentTarget = null;

        ClearCurrentTarget();
    }
}
