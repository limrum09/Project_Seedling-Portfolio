using UnityEngine;

/// <summary>
/// Inventory 접근점과 Inventory Presenter 연결 및 Panel 표시 상태 관리
/// </summary>
public class InventoryUIController : MonoBehaviour, IUIPanelController
{
    [SerializeField]
    private InventoryPanel panel;

    private InventoryPresenter presenter;

    public bool IsOpen { get; private set; }

    /// <summary>
    /// Inventory 접근점 연결 해제
    /// </summary>
    private void OnDestroy()
    {
        UnBind();
    }

    /// <summary>
    /// Inventory 접근점과 Hotkey Panel 연결
    /// </summary>
    /// <param name="readAccess">Inventory 읽기 접근점</param>
    /// <param name="gateWay">Inventory 요청 Gateway</param>
    /// <param name="hotkeyPanel">Inventory Hotkey Panel</param>
    public void Bind(IInventoryReadAccess readAccess, IInventoryCommandGateWay gateWay, HotkeyPanel hotkeyPanel)
    {
        UnBind();

        presenter = new InventoryPresenter(readAccess, gateWay, panel, hotkeyPanel);

        presenter.Init();
    }

    /// <summary>
    /// 현재 Inventory Presenter 연결 해제
    /// </summary>
    public void UnBind()
    {
        if (presenter == null)
            return;

        presenter.Dispose();
        presenter = null;
    }

    /// <summary>
    /// Inventory Panel 표시
    /// </summary>
    public void Show()
    {
        IsOpen = true;
        presenter.Show();
    }

    /// <summary>
    /// Inventory Panel 숨김
    /// </summary>
    public void Hide()
    {
        IsOpen = false;
        presenter.Hide();
    }
}
