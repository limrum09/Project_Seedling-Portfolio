using UnityEngine;

/// <summary>
/// Furniture 접근점과 Furniture Presenter 연결 및 Panel 표시 상태 관리
/// </summary>
public class FurnitureUIController : MonoBehaviour, IUIPanelController, IUIBackHandler
{
    [SerializeField]
    private FurniturePanel panel;
    [SerializeField]
    private UITweenSequencePlayer showTween;
    [SerializeField]
    private UITweenSequencePlayer hideTween;

    private IFurniturePlacementAccess placementAccess;
    private FurniturePresenter presenter;
    private bool panelVisible;

    public bool IsOpen => panelVisible;

    private void OnDestroy()
    {
        Unbind();
    }

    private void FurnitureStateChanged(FurnitureState state)
    {
        bool isVibible = state != FurnitureState.None;

        if (panelVisible == isVibible)
            return;

        panelVisible = isVibible;

        if (panelVisible)
        {
            hideTween.Stop();
            showTween.Play();
        }
        else
        {
            showTween.Stop();
            hideTween.Play();
        }
    }

    public void Bind(IFurniturePlacementAccess getPlacementAccess, IInventoryReadAccess readAccess)
    {
        Unbind();

        placementAccess = getPlacementAccess;
        placementAccess.OnChangedState += FurnitureStateChanged;

        presenter = new FurniturePresenter(placementAccess, readAccess, panel);

        panelVisible = false;
        presenter.Init();
        panel.BindCategoryToggle(presenter);
    }

    public void Unbind()
    {
        if (presenter == null)
            return;

        panel.UnBindCategoryToggles();

        placementAccess.OnChangedState -= FurnitureStateChanged;
        placementAccess = null;

        presenter.Dispose();
        presenter = null;
    }

    public void Show()
    {
        presenter.Show();
    }

    public void Hide()
    {
        presenter.Hide();
    }

    /// <summary>
    /// 가구 작업 중이면 현재 작업만 취소하고 선택 상태면 Panel 닫기 허용
    /// </summary>
    /// <returns>현재 가구 작업만 취소했으면 true</returns>
    public bool TryHandleBack()
    {
        switch (placementAccess.CurrentState)
        {
            case FurnitureState.None:
            case FurnitureState.SelectingFurniture:
                return false;
            default:
                placementAccess.CancelCurrentAction();
                return true;
        }
    }
}
