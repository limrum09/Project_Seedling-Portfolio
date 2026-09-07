using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 시설 하나의 생산 항목을 World Canvas에 표시
/// 수량 변경을 구독하고 아이콘 더블클릭을 회수 요청으로 전달
/// </summary>
[RequireComponent(typeof(Canvas), typeof(GraphicRaycaster))]
public sealed class ProductionWorldUI : MonoBehaviour, IPointerClickHandler
{
    /// <summary>
    /// 생산 항목 하나의 UI 참조와 수량 변경 구독을 관리
    /// </summary>
    private sealed class OutputView
    {
        private readonly RectTransform root;
        private readonly Image icon;
        private readonly TMP_Text amountText;
        private readonly ProductionOutputRuntime output;

        public RectTransform Root => root;
        public Image Icon => icon;
        public ProductionOutputRuntime Output => output;

        /// <summary>
        /// 현재 보관량과 최대 보관량 도달 여부를 텍스트에 반영
        /// 글자색은 변경하지 않음
        /// </summary>
        private void RefreshAmount()
        {
            amountText.text = output.IsFull ? $"{output.StoredAmount}(max)" : output.StoredAmount.ToString();
        }

        public OutputView(RectTransform getRoot, ProductionOutputRuntime getOutput)
        {
            root = getRoot;
            output = getOutput;

            icon = root.Find("Icon").GetComponent<Image>();
            amountText = root.Find("AmountText").GetComponent<TMP_Text>();

            // 장식과 텍스트는 입력을 받지 않고 Icon만 입력을 받도록 설정
            foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
            {
                graphic.raycastTarget = false;
            }

            icon.raycastTarget = true;
            icon.sprite = output.OutputItem.Icon;
        }

        /// <summary>
        /// 수량 변경을 구독하고 현재 보관량을 즉시 표시
        /// </summary>
        public void Subscribe()
        {
            output.OnStoredAmountChanged += RefreshAmount;

            RefreshAmount();
        }

        /// <summary>
        /// 수량 변경 구독 해제
        /// </summary>
        public void Unsubscribe()
        {
            output.OnStoredAmountChanged -= RefreshAmount;
        }
    }

    [Header("Item")]
    [SerializeField]
    private RectTransform itemTemplate;
    [SerializeField]
    private Vector2 itemSize = new Vector2(3f, 3f);
    [SerializeField, Min(0f)]
    private float itemSpacing = 0.2f;

    [Header("Click")]
    [SerializeField, Min(0.01f)]
    private float doubleClickInterval = 0.3f;

    private readonly List<OutputView> views = new List<OutputView>();

    private Canvas worldCanvas;
    private Camera worldCamera;
    private Action<ProductionOutputRuntime> requestCollect;

    private OutputView lastClickedView;
    private float lastClickTime;
    private int lastPointerID;

    // 생성 직후에는 Bind를 기다리는 상태
    private bool isBound;

    private void Awake()
    {
        worldCanvas = GetComponent<Canvas>();

        itemTemplate.gameObject.SetActive(false);
        worldCanvas.enabled = false;
    }

    private void OnEnable()
    {
        if (!isBound)
            return;

        SubscribeViews();
    }

    private void LateUpdate()
    {
        if (!isBound)
            return;

        transform.rotation = worldCamera.transform.rotation;
    }

    private void OnDisable()
    {
        foreach (OutputView view in views)
        {
            view.Unsubscribe();
        }

        worldCanvas.enabled = false;
        lastClickedView = null;
    }

    /// <summary>
    /// 모든 표시 행의 수량을 구독하고 Canvas 표시를 시작
    /// </summary>
    private void SubscribeViews()
    {
        foreach (OutputView view in views)
        {
            view.Subscribe();
        }

        transform.rotation = worldCamera.transform.rotation;
        worldCanvas.enabled = true;
    }

    /// <summary>
    /// 기존 표시 행의 구독을 해제하고 재연결을 위해 제거
    /// </summary>
    private void ClearViews()
    {
        foreach (OutputView view in views)
        {
            view.Unsubscribe();

            // Destroy가 실행되는 프레임 끝까지 기존 행이 입력을 받지 않도록 비활성화
            view.Root.gameObject.SetActive(false);
            Destroy(view.Root.gameObject);
        }

        views.Clear();
        lastClickedView = null;
    }

    /// <summary>
    /// 생산 항목 수만큼 표시 행을 생성하고 가로 중앙에 배치
    /// </summary>
    /// <param name="production">표시할 시설의 생산 상태</param>
    private void CreateViews(ProductionRuntime production)
    {
        int count = production.Outputs.Count;
        float totalWidth = count * itemSize.x + Mathf.Max(0, count - 1) * itemSpacing;

        RectTransform canvasRect = (RectTransform)transform;
        canvasRect.sizeDelta = new Vector2(totalWidth, itemSize.y);

        for (int i = 0; i < count; i++)
        {
            RectTransform row = Instantiate(itemTemplate, itemTemplate.parent);

            row.anchorMin = new Vector2(0.5f, 0.5f);
            row.anchorMax = new Vector2(0.5f, 0.5f);
            row.pivot = new Vector2(0.5f, 0.5f);
            row.sizeDelta = itemSize;

            float positionX = (i - (count - 1) * 0.5f)
                * (itemSize.x + itemSpacing);

            row.anchoredPosition = new Vector2(positionX, 0f);

            OutputView view = new OutputView(row, production.Outputs[i]);
            views.Add(view);

            row.gameObject.SetActive(true);
        }
    }



    /// <summary>
    /// 생산 상태, World Camera와 회수 요청을 연결
    /// 재연결하면 기존 표시 행과 수량 구독을 먼저 정리
    /// </summary>
    /// <param name="production">표시할 시설의 생산 상태</param>
    /// <param name="camera">표시와 입력 판정에 사용할 World Camera</param>
    /// <param name="collectRequest">생산 항목의 회수 요청</param>
    public void Bind(ProductionRuntime production, Camera camera, Action<ProductionOutputRuntime> collectRequest)
    {
        ClearViews();

        worldCamera = camera;
        requestCollect = collectRequest;
        worldCanvas.worldCamera = camera;

        CreateViews(production);

        isBound = true;

        if (isActiveAndEnabled)
            SubscribeViews();
    }

    /// <summary>
    /// 같은 Pointer로 같은 Icon을 왼쪽 더블클릭하면 해당 항목의 회수를 요청
    /// 서로 다른 Icon의 연속 클릭은 더블클릭으로 처리하지 않음
    /// </summary>
    /// <param name="eventData">Pointer 클릭 정보</param>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isBound || eventData.button != PointerEventData.InputButton.Left)
            return;

        GameObject clickedObject = eventData.pointerPressRaycast.gameObject;

        // 다른 Icon에서 버튼을 놓은 경우에는 클릭 기록을 해제
        if (clickedObject != eventData.pointerCurrentRaycast.gameObject)
        {
            lastClickedView = null;
            return;
        }

        foreach (OutputView view in views)
        {
            if (view.Icon.gameObject != clickedObject)
                continue;

            float clickTime = Time.unscaledTime;

            bool isDoubleClick = lastClickedView == view && lastPointerID == eventData.pointerId && clickTime - lastClickTime <= doubleClickInterval;

            if (isDoubleClick)
            {
                lastClickedView = null;

                requestCollect(view.Output);
            }
            else
            {
                lastClickedView = view;
                lastPointerID = eventData.pointerId;
                lastClickTime = clickTime;
            }

            return;
        }
    }
}