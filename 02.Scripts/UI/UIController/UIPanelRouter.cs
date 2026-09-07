using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Router에서 식별할 수 있는 UI Panel 종류
/// Inspector 직렬화 값을 유지하기 위해 기존 숫자값 유지
/// </summary>
public enum UIPanelId
{
    Weapon = 0,
    Inventory = 1,
    Building = 2,
    Furniture = 3
}

/// <summary>
/// UIPanelRouter가 표시 상태를 관리할 UI Controller 계약
/// </summary>
public interface IUIPanelController
{
    bool IsOpen { get; }

    /// <summary>
    /// UI Panel 표시
    /// </summary>
    void Show();

    /// <summary>
    /// UI Panel 숨김
    /// </summary>
    void Hide();
}

/// <summary>
/// 최상위 UI Panel이 뒤로 가기 입력을 먼저 처리하는 계약
/// </summary>
public interface IUIBackHandler
{
    /// <summary>
    /// 현재 Panel 내부의 뒤로 가기 요청 처리
    /// </summary>
    /// <returns>Panel을 유지하고 내부 요청을 처리했으면 true</returns>
    bool TryHandleBack();
}

/// <summary>
/// World 상호작용과 Player Input에서 UI 표시를 요청하는 접근점
/// </summary>
public interface IUIOpenRequest
{
    /// <summary>
    /// 지정한 UI Panel 표시
    /// </summary>
    /// <param name="panelId">표시할 Panel 식별자</param>
    void Open(UIPanelId panelId);

    /// <summary>
    /// 지정한 UI Panel을 열거나 최상위 Panel이면 닫기
    /// </summary>
    /// <param name="panelId">Toggle할 Panel 식별자</param>
    void Toggle(UIPanelId panelId);

    /// <summary>
    /// 지정한 UI Panel 닫기
    /// </summary>
    /// <param name="panelId">닫을 Panel 식별자</param>
    void Close(UIPanelId panelId);

    /// <summary>
    /// 최상위 UI Panel에 뒤로 가기 요청 전달
    /// </summary>
    void Back();
}

/// <summary>
/// UIPanelId와 UI Controller 및 Panel Root 연결 정보
/// </summary>
[Serializable]
public sealed class UIPanelBinding
{
    [SerializeField]
    private UIPanelId panelId;
    [SerializeField]
    private MonoBehaviour controllerSource;
    [SerializeField]
    private RectTransform panelRoot;

    public UIPanelId PanelId => panelId;
    public MonoBehaviour ControllerSource => controllerSource;
    public RectTransform PanelRoot => panelRoot;
}

/// <summary>
/// 열린 UI Panel 순서와 표시 우선순위 관리
/// 키보드나 Pointer 입력은 직접 확인하지 않음
/// </summary>
public class UIPanelRouter : MonoBehaviour, IUIOpenRequest
{
    [SerializeField]
    private RectTransform panelLayer;
    [SerializeField]
    private List<UIPanelBinding> bindings = new List<UIPanelBinding>();

    private readonly Dictionary<UIPanelId, IUIPanelController> controllers = new Dictionary<UIPanelId, IUIPanelController>();
    private readonly Dictionary<UIPanelId, RectTransform> panelRoots = new Dictionary<UIPanelId, RectTransform>();
    private readonly List<UIPanelId> openedPanels = new List<UIPanelId>();

    /// <summary>
    /// Inspector Binding을 Runtime Controller Map으로 변환
    /// </summary>
    private void Awake()
    {
        BuildControllerMap();
    }

    /// <summary>
    /// UI Panel Binding 검증과 Controller Map 생성
    /// </summary>
    private void BuildControllerMap()
    {
        if (panelLayer == null)
            throw new InvalidOperationException("UIPanelRouter에 Panel Layer가 연결되지 않음");

        controllers.Clear();
        panelRoots.Clear();
        openedPanels.Clear();

        for (int i = 0; i < bindings.Count; i++)
        {
            UIPanelBinding binding = bindings[i];

            if (binding == null)
                throw new InvalidOperationException($"UIPanelRouter의 {i}번 Binding이 비어 있음");

            if (binding.ControllerSource == null)
                throw new InvalidOperationException($"UIPanelRouter의 {binding.PanelId} Controller가 연결되지 않음");

            if (!(binding.ControllerSource is IUIPanelController controller))
                throw new InvalidOperationException($"{binding.ControllerSource.name}이 IUIPanelController를 구현하지 않음");

            if (binding.PanelRoot == null)
                throw new InvalidOperationException($"UIPanelRouter의 {binding.PanelId} Panel Root가 연결되지 않음");

            if (binding.PanelRoot.parent != panelLayer)
                throw new InvalidOperationException($"UIPanelRouter의 {binding.PanelId} Panel Root가 Panel Layer의 직접 자식이 아님");

            if (controllers.ContainsKey(binding.PanelId))
                throw new InvalidOperationException($"UIPanelRouter에 {binding.PanelId} Binding이 중복 등록됨");

            controllers.Add(binding.PanelId, controller);
            panelRoots.Add(binding.PanelId, binding.PanelRoot);
        }
    }

    /// <summary>
    /// 지정한 UI Panel을 표시하고 최상위 순서로 이동
    /// </summary>
    /// <param name="panelId">표시할 Panel 식별자</param>
    public void Open(UIPanelId panelId)
    {
        IUIPanelController controller = RequireController(panelId);

        openedPanels.Remove(panelId);

        if (!controller.IsOpen)
            controller.Show();

        panelRoots[panelId].SetAsLastSibling();
        openedPanels.Add(panelId);
    }

    /// <summary>
    /// 지정한 Panel이 최상위면 닫고 아니면 최상위로 표시
    /// </summary>
    /// <param name="panelId">Toggle할 Panel 식별자</param>
    public void Toggle(UIPanelId panelId)
    {
        if (IsTopPanel(panelId))
        {
            Close(panelId);
            return;
        }

        Open(panelId);
    }

    /// <summary>
    /// 지정한 UI Panel을 열린 순서에서 제거하고 숨김
    /// </summary>
    /// <param name="panelId">닫을 Panel 식별자</param>
    public void Close(UIPanelId panelId)
    {
        IUIPanelController controller = RequireController(panelId);

        openedPanels.Remove(panelId);

        if (controller.IsOpen)
            controller.Hide();
    }

    /// <summary>
    /// 최상위 Panel 내부 작업을 먼저 취소하고 처리되지 않으면 Panel 닫기
    /// </summary>
    public void Back()
    {
        if (openedPanels.Count == 0)
            return;

        UIPanelId topPanelId = openedPanels[openedPanels.Count - 1];
        IUIPanelController topController = controllers[topPanelId];

        if (topController is IUIBackHandler backHandler && backHandler.TryHandleBack())
            return;

        Close(topPanelId);
    }

    /// <summary>
    /// 지정한 Panel이 현재 최상위인지 확인
    /// </summary>
    /// <param name="panelId">확인할 Panel 식별자</param>
    /// <returns>현재 최상위 Panel이면 true</returns>
    private bool IsTopPanel(UIPanelId panelId)
    {
        return openedPanels.Count > 0 && openedPanels[openedPanels.Count - 1] == panelId;
    }

    /// <summary>
    /// 지정한 Panel의 필수 Controller 반환
    /// </summary>
    /// <param name="panelId">찾을 Panel 식별자</param>
    /// <returns>등록된 UI Panel Controller</returns>
    private IUIPanelController RequireController(UIPanelId panelId)
    {
        if (!controllers.TryGetValue(panelId, out IUIPanelController controller))
            throw new InvalidOperationException($"UIPanelRouter에 {panelId} Controller가 등록되지 않음");

        return controller;
    }
}
