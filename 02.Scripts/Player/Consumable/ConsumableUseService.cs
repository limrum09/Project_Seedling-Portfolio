using System;
using UnityEngine;

/// <summary>
/// 권한 영역에서 소모품 사용 규칙을 검사하고 Inventory와 PlayerStatus 변경을 조율
/// </summary>
public sealed class ConsumableUseService : MonoBehaviour
{
    [Header("Consumable Item")]
    [SerializeField]
    private ItemDefine energyItem;
    [SerializeField]
    private ItemDefine foodItem;
    [SerializeField]
    private ItemDefine oxygenItem;

    private IInventoryItemRemover inventoryItemRemover;
    private PlayerStatusService statusService;

    /// <summary>
    /// 소모품 ItemDefine 참조와 설정값이 올바른지 Editor에서 확인
    /// </summary>
    private void OnValidate()
    {
        ValidateItemInEditor(energyItem, ItemConsumableType.Energy);
        ValidateItemInEditor(foodItem, ItemConsumableType.Food);
        ValidateItemInEditor(oxygenItem, ItemConsumableType.Oxygen);
    }

    /// <summary>
    /// Inspector에 연결된 소모품 설정값을 확인
    /// </summary>
    /// <param name="item">확인할 ItemDefine</param>
    /// <param name="expectedType">기대하는 소모품 종류</param>
    private void ValidateItemInEditor(ItemDefine item, ItemConsumableType expectedType)
    {
        if (item == null)
        {
            Debug.LogError($"Consumable Use Service에 {expectedType} ItemDefine 참조가 없음", this);
            return;
        }

        if (item.Category != ItemCategory.Consumable || item.ConsumableType != expectedType)
            Debug.LogError($"{item.name}의 Consumable 설정이 {expectedType}과 일치하지 않음", item);

        if (item.ConsumableAmount <= 0)
            Debug.LogError($"{item.name}의 Consumable Amount가 0 이하임", item);

        if (string.IsNullOrEmpty(item.ItemUID))
            Debug.LogError($"{item.name}의 Item UID가 없음", item);
    }

    /// <summary>
    /// 권한 Inventory와 PlayerStatus 변경 접근점을 연결
    /// </summary>
    /// <param name="itemRemover">권한 Inventory 아이템 제거 접근점</param>
    /// <param name="playerStatusService">권한 PlayerStatusService</param>
    public void Bind(IInventoryItemRemover itemRemover, PlayerStatusService playerStatusService)
    {
        inventoryItemRemover = itemRemover ?? throw new ArgumentNullException(nameof(itemRemover));
        statusService = playerStatusService ?? throw new ArgumentNullException(nameof(playerStatusService));
    }

    /// <summary>
    /// 권한 Inventory와 PlayerStatus 변경 접근점 연결을 해제
    /// </summary>
    public void Unbind()
    {
        inventoryItemRemover = null;
        statusService = null;
    }

    /// <summary>
    /// 권한 변경 접근점이 모두 연결되어 있는지 검사
    /// </summary>
    private void EnsureBound()
    {
        if (inventoryItemRemover == null)
            throw new InvalidOperationException("Consumable Use Service에 Inventory Item Remover가 연결되지 않음");

        if (statusService == null)
            throw new InvalidOperationException("Consumable Use Service에 Player Status Service가 연결되지 않음");
    }

    /// <summary>
    /// 소모품 종류에 해당하는 ItemDefine을 반환
    /// </summary>
    /// <param name="type">찾을 소모품 종류</param>
    /// <returns>종류에 대응하는 ItemDefine이며 지원하지 않는 종류면 null</returns>
    private ItemDefine GetItemDefine(ItemConsumableType type)
    {
        switch (type)
        {
            case ItemConsumableType.Energy:
                return energyItem;
            case ItemConsumableType.Food:
                return foodItem;
            case ItemConsumableType.Oxygen:
                return oxygenItem;
            default:
                return null;
        }
    }

    /// <summary>
    /// 요청한 소모품 종류를 현재 Service가 지원하는지 검사
    /// </summary>
    /// <param name="type">검사할 소모품 종류</param>
    /// <returns>지원하는 소모품 종류면 true</returns>
    private bool IsSupportedType(ItemConsumableType type)
    {
        return type == ItemConsumableType.Energy || type == ItemConsumableType.Food || type == ItemConsumableType.Oxygen;
    }

    /// <summary>
    /// 전달받은 ItemDefine이 요청한 소모품 종류와 일치하는지 검사
    /// </summary>
    /// <param name="item">검사할 ItemDefine</param>
    /// <param name="type">요청한 소모품 종류</param>
    private void ValidateItemConfiguration(ItemDefine item, ItemConsumableType type)
    {
        if (item == null)
            throw new InvalidOperationException($"Consumable Use Service에 {type} ItemDefine 참조가 없음");

        if (item.Category != ItemCategory.Consumable)
            throw new InvalidOperationException($"{item.name}의 Category가 Consumable이 아님");

        if (item.ConsumableType != type)
            throw new InvalidOperationException($"{item.name}의 Consumable Type이 {type}과 일치하지 않음");

        if (item.ConsumableAmount <= 0)
            throw new InvalidOperationException($"{item.name}의 Consumable Amount가 0 이하임");

        if (string.IsNullOrEmpty(item.ItemUID))
            throw new InvalidOperationException($"{item.name}의 Item UID가 없음");
    }

    /// <summary>
    /// 지정한 소모품으로 플레이어 자원을 회복할 수 있는지 검사
    /// </summary>
    /// <param name="type">회복할 자원 종류</param>
    /// <param name="amount">회복량</param>
    /// <returns>현재 상태에서 실제 회복이 가능하면 true</returns>
    private bool CanRestore(ItemConsumableType type, int amount)
    {
        switch (type)
        {
            case ItemConsumableType.Energy:
                return statusService.CanChargeEnergy(amount);
            case ItemConsumableType.Food:
                return statusService.CanChargeFood(amount);
            case ItemConsumableType.Oxygen:
                return statusService.CanChargeOxygen(amount);
            default:
                return false;
        }
    }

    /// <summary>
    /// 지정한 소모품 종류에 대응하는 플레이어 자원을 회복
    /// </summary>
    /// <param name="type">회복할 자원 종류</param>
    /// <param name="amount">회복량</param>
    /// <returns>플레이어 자원이 회복되면 true</returns>
    private bool TryRestore(ItemConsumableType type, int amount)
    {
        switch (type)
        {
            case ItemConsumableType.Energy:
                return statusService.TryChargeEnergy(amount, out _);
            case ItemConsumableType.Food:
                return statusService.TryChargeFood(amount, out _);
            case ItemConsumableType.Oxygen:
                return statusService.TryChargeOxygen(amount, out _);
            default:
                return false;
        }
    }

    /// <summary>
    /// 지정한 종류의 소모품 1개 사용
    /// 상태 회복 가능 여부를 먼저 확인하므로 실패한 요청은 Inventory를 변경하지 않음
    /// </summary>
    /// <param name="type">사용할 소모품 종류</param>
    /// <returns>소모품 사용 결과</returns>
    public ConsumableUseResult TryUse(ItemConsumableType type)
    {
        EnsureBound();

        if (!IsSupportedType(type))
            return ConsumableUseResult.Failed(ConsumableUseStopReason.InvalidType);

        ItemDefine item = GetItemDefine(type);

        ValidateItemConfiguration(item, type);

        if (!CanRestore(type, item.ConsumableAmount))
            return ConsumableUseResult.Failed(ConsumableUseStopReason.StatusFull);

        if (!inventoryItemRemover.TryRemoveItem(item.ItemUID, 1))
            return ConsumableUseResult.Failed(ConsumableUseStopReason.NotEnoughItem);

        if (!TryRestore(type, item.ConsumableAmount))
            throw new InvalidOperationException("회복 가능 검사 후 Player Status 변경에 실패함");

        return ConsumableUseResult.Succeeded();
    }
}
