using System;
using UnityEngine;

/// <summary>
/// 플레이어 무기 요청이 중단된 이유
/// </summary>
public enum PlayerWeaponStopReason
{
    None,
    InvalidWeapon,
    AlreadyUnlocked,
    NotEnoughMaterials,
    NotUnlocked,
    InvalidSlot,
    AlreadyEquipped,
    AlreadyEmpty,
    NoEquippedWeapon,
}

/// <summary>
/// 플레이어 무기 요청의 처리 결과
/// </summary>
public readonly struct PlayerWeaponOperationResult
{
    public bool Success { get; }
    public PlayerWeaponStopReason Reason { get; }

    private PlayerWeaponOperationResult(bool success, PlayerWeaponStopReason reason)
    {
        Success = success;
        Reason = reason;
    }

    /// <summary>
    /// 성공한 플레이어 무기 요청 결과 생성
    /// </summary>
    /// <returns>성공 결과</returns>
    public static PlayerWeaponOperationResult Succeeded()
    {
        return new PlayerWeaponOperationResult(true, PlayerWeaponStopReason.None);
    }

    /// <summary>
    /// 실패한 플레이어 무기 요청 결과 생성
    /// </summary>
    /// <param name="reason">실패 사유</param>
    /// <returns>실패 결과</returns>
    public static PlayerWeaponOperationResult Failed(PlayerWeaponStopReason reason)
    {
        if (reason == PlayerWeaponStopReason.None)
            throw new ArgumentOutOfRangeException(nameof(reason));

        return new PlayerWeaponOperationResult(false, reason);
    }
}

/// <summary>
/// 클라이언트의 무기 해금과 Loadout 변경 의도를 권한 영역에 전달
/// </summary>
public interface IPlayerWeaponCommandGateway
{
    event Action<int, PlayerWeaponOperationResult> OnUnlockCompleted;
    event Action<int, PlayerWeaponOperationResult> OnSetLoadoutCompleted;
    event Action<int, PlayerWeaponOperationResult> OnSwapCompleted;

    /// <summary>
    /// 지정한 무기의 해금을 요청
    /// </summary>
    /// <param name="weaponId">해금할 Weapon ID</param>
    /// <returns>요청 식별자</returns>
    int RequestUnlockWeapon(string weaponId);

    /// <summary>
    /// 지정한 Station에서 Loadout 슬롯 변경을 요청
    /// </summary>
    /// <param name="slot">변경할 무기 슬롯</param>
    /// <param name="weaponId">배치할 Weapon ID</param>
    /// <returns>요청 식별자</returns>
    int RequestSetLoadout(WeaponSlot slot, string weaponId);

    int RequestClearLoadout(WeaponSlot slot);

    /// <summary>
    /// Main Weapon과 Reserve Weapon 교환을 요청
    /// </summary>
    /// <returns>요청 식별자</returns>
    int RequestSwapWeapons();
}

/// <summary>
/// 플레이어 무기 요청 식별자와 완료 결과 전달 방식을 정의
/// </summary>
public abstract class PlayerWeaponGateway : MonoBehaviour, IPlayerWeaponCommandGateway
{
    private int nextRequestId = 1;

    public event Action<int, PlayerWeaponOperationResult> OnUnlockCompleted;
    public event Action<int, PlayerWeaponOperationResult> OnSetLoadoutCompleted;
    public event Action<int, PlayerWeaponOperationResult> OnSwapCompleted;

    /// <summary>
    /// 다음 플레이어 무기 요청 식별자를 생성
    /// </summary>
    /// <returns>생성된 요청 식별자</returns>
    private int CreateRequestId()
    {
        int requestId = nextRequestId;

        if (nextRequestId == int.MaxValue)
            nextRequestId = 1;
        else
            nextRequestId++;

        return requestId;
    }

    /// <summary>
    /// 완료 결과의 성공 여부와 실패 사유 조합을 확인
    /// </summary>
    /// <param name="result">확인할 요청 결과</param>
    private void ValidateResult(PlayerWeaponOperationResult result)
    {
        if (result.Success && result.Reason != PlayerWeaponStopReason.None)
            throw new InvalidOperationException("성공한 무기 요청 결과에 실패 사유가 있음");

        if (!result.Success && result.Reason == PlayerWeaponStopReason.None)
            throw new InvalidOperationException("실패한 무기 요청 결과에 실패 사유가 없음");
    }

    /// <summary>
    /// 구체적인 전달 방식으로 무기 해금 요청 전달
    /// </summary>
    /// <param name="requestId">요청 식별자</param>
    /// <param name="weaponId">해금할 Weapon ID</param>
    protected abstract void SendUnlockRequest(int requestId, string weaponId);

    /// <summary>
    /// 구체적인 전달 방식으로 Loadout 변경 요청 전달
    /// </summary>
    /// <param name="requestId">요청 식별자</param>
    /// <param name="station">사용 중인 Loadout Station</param>
    /// <param name="slot">변경할 무기 슬롯</param>
    /// <param name="weaponId">배치할 Weapon ID</param>
    protected abstract void SendSetLoadoutRequest(int requestId, WeaponSlot slot, string weaponId);

    protected abstract void SendClearLoadoutRequest(int requestId, WeaponSlot slot);

    /// <summary>
    /// 구체적인 전달 방식으로 무기 스왑 요청 전달
    /// </summary>
    /// <param name="requestId">요청 식별자</param>
    protected abstract void SendSwapRequest(int requestId);

    /// <summary>
    /// 무기 해금 완료 결과 전달
    /// </summary>
    /// <param name="requestId">완료된 요청 식별자</param>
    /// <param name="result">무기 해금 결과</param>
    protected void NotifyUnlockCompleted(int requestId, PlayerWeaponOperationResult result)
    {
        if (requestId <= 0)
            throw new ArgumentOutOfRangeException(nameof(requestId));

        ValidateResult(result);
        OnUnlockCompleted?.Invoke(requestId, result);
    }

    /// <summary>
    /// Loadout 변경 완료 결과 전달
    /// </summary>
    /// <param name="requestId">완료된 요청 식별자</param>
    /// <param name="result">Loadout 변경 결과</param>
    protected void NotifySetLoadoutCompleted(int requestId, PlayerWeaponOperationResult result)
    {
        if (requestId <= 0)
            throw new ArgumentOutOfRangeException(nameof(requestId));

        ValidateResult(result);
        OnSetLoadoutCompleted?.Invoke(requestId, result);
    }

    /// <summary>
    /// 무기 스왑 완료 결과 전달
    /// </summary>
    /// <param name="requestId">완료된 요청 식별자</param>
    /// <param name="result">무기 스왑 결과</param>
    protected void NotifySwapCompleted(int requestId, PlayerWeaponOperationResult result)
    {
        if (requestId <= 0)
            throw new ArgumentOutOfRangeException(nameof(requestId));

        ValidateResult(result);
        OnSwapCompleted?.Invoke(requestId, result);
    }

    /// <summary>
    /// 지정한 무기의 해금을 요청
    /// </summary>
    /// <param name="weaponId">해금할 Weapon ID</param>
    /// <returns>요청 식별자</returns>
    public int RequestUnlockWeapon(string weaponId)
    {
        int requestId = CreateRequestId();

        SendUnlockRequest(requestId, weaponId);

        return requestId;
    }

    /// <summary>
    /// 지정한 Station에서 Loadout 슬롯 변경을 요청
    /// </summary>
    /// <param name="station">현재 사용 중인 Loadout Station</param>
    /// <param name="slot">변경할 무기 슬롯</param>
    /// <param name="weaponId">배치할 Weapon ID</param>
    /// <returns>요청 식별자</returns>
    public int RequestSetLoadout(WeaponSlot slot, string weaponId)
    {
        int requestId = CreateRequestId();

        SendSetLoadoutRequest(requestId, slot, weaponId);

        return requestId;
    }

    public int RequestClearLoadout(WeaponSlot slot)
    {
        int requestId = CreateRequestId();

        SendClearLoadoutRequest(requestId, slot);

        return requestId;
    }

    /// <summary>
    /// Main Weapon과 Reserve Weapon 교환을 요청
    /// </summary>
    /// <returns>요청 식별자</returns>
    public int RequestSwapWeapons()
    {
        int requestId = CreateRequestId();

        SendSwapRequest(requestId);

        return requestId;
    }
}