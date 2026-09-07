using System;

/// <summary>
/// 생산 항목의 보관 아이템을 Inventory에 전달하는 회수 요청 처리
/// Inventory에 들어가지 않은 수량은 시설에 다시 보관
/// </summary>
public class ProductionCollectService
{
    /// <summary>
    /// 생산 항목에 보관된 아이템을 Inventory에 전달
    /// 일부만 전달되거나 전달에 실패하면 남은 수량을 시설에 다시 보관
    /// 진행 중인 생산 시간은 유지
    /// </summary>
    /// <param name="output">아이템을 회수할 생산 항목</param>
    /// <param name="itemReceiver">회수한 아이템을 받을 Inventory 접근점</param>
    /// <param name="result">Inventory 추가 결과이며 보관 아이템이 없으면 null</param>
    /// <returns>하나 이상의 아이템이 Inventory에 추가되면 true</returns>
    public bool TryCollect(ProductionOutputRuntime output, IInventoryItemReceiver itemReceiver, out InventoryAddResult result)
    {
        result = null;

        if (output == null)
            throw new ArgumentNullException(nameof(output));

        if (itemReceiver == null)
            throw new ArgumentNullException(nameof(itemReceiver));

        // Inventory 변경 이벤트에서 같은 수량이 다시 회수되지 않도록 먼저 꺼냄
        int requestAmount = output.TakeStoredAmount();

        if (requestAmount == 0)
            return false;

        result = itemReceiver.TryAdd(output.OutputItem, requestAmount);

        output.RestoreStoredAmount(result.RemainAmount);
        output.NotifyStoredAmountChanged(requestAmount);

        return result.AddAmount > 0;
    }
}