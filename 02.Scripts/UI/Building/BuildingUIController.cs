using UnityEngine;

/// <summary>
/// Building 접근점과 Building Presenter 연결 및 Panel 표시 상태 관리
/// </summary>
public class BuildingUIController : MonoBehaviour, IUIPanelController, IUIBackHandler
{
    [SerializeField]
    private BuildingPanel panel;
    [SerializeField]
    private UITweenSequencePlayer showTween;
    [SerializeField]
    private UITweenSequencePlayer hideTween;

    private IBuildingPlacementAccess placementAccess;
    private BuildingPresenter presenter;
    private bool panelVisible;

    public bool IsOpen => panelVisible;

    private void OnDestroy()
    {
        Unbind();
    }

    private void BuildStateChanged(BuildState state)
    {
        bool isVibible = state != BuildState.None;

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

    public void Bind(IBuildingPlacementAccess getPlacementAccess, IInventoryReadAccess readAccess)
    {
        Unbind();

        placementAccess = getPlacementAccess;
        placementAccess.OnChangedState += BuildStateChanged;

        presenter = new BuildingPresenter(placementAccess, readAccess, panel);
        panelVisible = false;
        presenter.Init();
        panel.BindCategoryToggle(presenter);
    }

    public void Unbind()
    {
        if (presenter == null)
            return;

        panel.UnBindCategoryToggles();

        placementAccess.OnChangedState -= BuildStateChanged;
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
    /// 건설 작업 중이면 현재 작업만 취소하고 대기 상태면 Panel 닫기 허용
    /// </summary>
    /// <returns>현재 건설 작업만 취소했으면 true</returns>
    public bool TryHandleBack()
    {
        switch (placementAccess.CurrentState)
        {
            case BuildState.None:
            case BuildState.Idle:
                return false;
            default:
                placementAccess.CancelCurrentAction();
                return true;
        }
    }
}
