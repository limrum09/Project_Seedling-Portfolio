using UnityEngine;

/// <summary>
/// Player Weapon 접근점과 Weapon Panel을 연결하고 Station 사용 상태를 관리
/// </summary>
public sealed class WeaponUIController : MonoBehaviour, IUIPanelController
{
    [SerializeField]
    private WeaponCatalog catalog;
    [SerializeField]
    private WeaponPanel panel;

    private WeaponPresenter presenter;

    public bool IsOpen { get; private set; }

    private void OnDestroy()
    {
        Unbind();
    }

    /// <summary>
    /// Player Weapon과 Inventory 접근점을 UI에 연결
    /// </summary>
    /// <param name="weaponReadAccess">Weapon Replica 읽기 접근점</param>
    /// <param name="weaponGateway">Weapon 요청 Gateway</param>
    /// <param name="inventoryReadAccess">Inventory Replica 읽기 접근점</param>
    public void Bind(IPlayerWeaponReadAccess weaponReadAccess, IPlayerWeaponCommandGateway weaponGateway, IInventoryReadAccess inventoryReadAccess)
    {
        Unbind();

        presenter = new WeaponPresenter(catalog, weaponReadAccess, weaponGateway, inventoryReadAccess, panel);

        panel.BindCategoryToggles(presenter);

        presenter.Enable();
        panel.Hide();
        IsOpen = false;
    }

    /// <summary>
    /// 현재 Player와 Weapon UI 연결 해제
    /// </summary>
    public void Unbind()
    {
        if (presenter == null)
            return;

        panel.UnbindCategoryToggles();

        presenter.Disable();
        presenter = null;
    }

    /// <summary>
    /// Weapon Panel 표시
    /// </summary>
    public void Show()
    {
        IsOpen = true;
        panel.Show();
    }

    /// <summary>
    /// Weapon Panel 표시 Tween 결과 적용
    /// </summary>
    public void TweenShow()
    {
        panel.TweenShow();
    }

    /// <summary>
    /// Weapon Panel 숨김
    /// </summary>
    public void Hide()
    {
        IsOpen = false;
        panel.Hide();
    }

    /// <summary>
    /// Weapon Panel 숨김 Tween 결과 적용
    /// </summary>
    public void TweenHide()
    {
        panel.TweenHide();
    }
}
