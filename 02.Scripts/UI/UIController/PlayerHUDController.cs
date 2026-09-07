using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PlayerStatusReplica에서 전달받은 자원 상태를 HUD에 표시
/// </summary>
public class PlayerHUDController : MonoBehaviour
{
    /// <summary>
    /// HUD Slot의 위치와 크기를 보관
    /// </summary>
    [Serializable]
    private struct HUDSlotLayout
    {
        [SerializeField]
        private Vector2 pos;
        [SerializeField]
        public Vector2 size;

        public Vector2 Pos => pos;
        public Vector2 Size => size;
    }

    [Header("Fill Images")]
    [SerializeField]
    private Image energyFill;
    [SerializeField]
    private Image hpFill;
    [SerializeField]
    private Image FoodFill;
    [SerializeField]
    private Image oxygenFill;

    [Header("Status Info")]
    [SerializeField]
    private TextMeshProUGUI statusInfoText;

    [Header("HUD Slots")]
    [SerializeField]
    private HUDSlotLayout slot1Layout;
    [SerializeField]
    private HUDSlotLayout slot2Layout;
    [SerializeField]
    private HUDSlotLayout slot3Layout;
    [SerializeField]
    private HUDSlotLayout slot4Layout;

    [Header("Rects")]
    [SerializeField]
    private RectTransform energyRect;
    [SerializeField]
    private RectTransform foodRect;
    [SerializeField]
    private RectTransform oxygenRect;
    [SerializeField]
    private RectTransform hpRect;

    private IPlayerStatusReadAccess status;
    private PlayerStatusSnapshot lastSnapshot;
    private PlayerHUDResourceType? activeInfoType;

    private int energySlot = 1;
    private int foodSlot = 2;
    private int oxygenSlot = 3;
    private int hpSlot = 4;

    /// <summary>
    /// 상태 정보 UI를 숨기고 초기 자원 HUD 배치를 적용
    /// </summary>
    private void Awake()
    {
        statusInfoText.gameObject.SetActive(false);
        ApplyResourceLayouts();
    }

    /// <summary>
    /// HUD가 제거될 때, PlayerStatus 이벤트 연결 해제
    /// </summary>
    private void OnDestroy()
    {
        Unbind();
    }

    /// <summary>
    /// 현재값과 최대값을 0-1범위의 Fill 값으로 변환
    /// </summary>
    /// <param name="current">현재값</param>
    /// <param name="max">최대값</param>
    /// <returns>0-1범위로 제한된 비율</returns>
    private float GetFillAmount(float current,  float max)
    {
        if (max <= 0f)
            return 0f;

        return Mathf.Clamp01(current / max);
    }

    /// <summary>
    /// 전달받은 상태값을 각 HUD Fill Image에 반영
    /// </summary>
    /// <param name="snapshot">플레이어 자원 상태</param>
    private void ApplySnapshot(PlayerStatusSnapshot snapshot)
    {
        lastSnapshot = snapshot;

        energyFill.fillAmount = GetFillAmount(snapshot.CurrentEnergy, snapshot.MaxEnergy);
        hpFill.fillAmount = GetFillAmount(snapshot.CurrentHP, snapshot.MaxHP);
        FoodFill.fillAmount = GetFillAmount(snapshot.CurrentFood, snapshot.MaxFood);
        oxygenFill.fillAmount = GetFillAmount(snapshot.CurrentOxygen, snapshot.MaxOxygen);

        if (activeInfoType.HasValue)
            RefreshStatusInfo();
    }

    /// <summary>
    /// 현재 선택된 자원 종류에 따라 정보 문자열 갱신
    /// </summary>
    private void RefreshStatusInfo()
    {
        if (!activeInfoType.HasValue)
            return;

        switch (activeInfoType)
        {
            case PlayerHUDResourceType.None:
                break;
            case PlayerHUDResourceType.Energy:
                statusInfoText.text = $"에너지 : {lastSnapshot.CurrentEnergy:F1} / {lastSnapshot.MaxEnergy:F1}";
                break;
            case PlayerHUDResourceType.HP:
                statusInfoText.text = $"체력 : {lastSnapshot.CurrentHP} / {lastSnapshot.MaxHP}";
                break;
            case PlayerHUDResourceType.Food:
                statusInfoText.text = $"허기 : {lastSnapshot.CurrentFood} / {lastSnapshot.MaxFood}";
                break;
            case PlayerHUDResourceType.Oxygen:
                statusInfoText.text = $"산소 : {lastSnapshot.CurrentOxygen} / {lastSnapshot.MaxOxygen}";
                break;
        }
    }

    /// <summary>
    /// 모든 자원 HUD를 현재 지정된 Slot번호에 마주처 배치
    /// 지금은 Awake에서 호출 하지만 이후 Save Load가 추가되면 Load이후에 호출
    /// </summary>
    private void ApplyResourceLayouts()
    {
        ApplySlotLayout(energyRect, energySlot);
        ApplySlotLayout(foodRect, foodSlot);
        ApplySlotLayout(oxygenRect, oxygenSlot);
        ApplySlotLayout(hpRect, hpSlot);
    }

    /// <summary>
    /// 자원 HUB에 지정된 slot의 위치와 크기를 적용
    /// </summary>
    /// <param name="resourceRect">변결항 자원 HUD</param>
    /// <param name="slotNumber">적용할 slot의 번호</param>
    private void ApplySlotLayout(RectTransform resourceRect, int slotNumber)
    {
        HUDSlotLayout layout = GetSlotLayout(slotNumber);
        resourceRect.anchoredPosition = layout.Pos;
        resourceRect.sizeDelta = layout.size;

        resourceRect.SetSiblingIndex(slotNumber - 1);
    }

    /// <summary>
    /// 지정한 Slot번호에 대응하는 HUD 레이아웃을 반환
    /// </summary>
    /// <param name="slotNumber">1-4까지 Slot번호</param>
    /// <returns>해당 Slot의 위치와 크기</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    private HUDSlotLayout GetSlotLayout(int slotNumber)
    {
        switch (slotNumber)
        {
            case 1:
                return slot1Layout;
            case 2:
                return slot2Layout;
            case 3:
                return slot3Layout;
            case 4:
                return slot4Layout;
            default:
                throw new ArgumentOutOfRangeException(nameof(slotNumber), slotNumber, "Slot 번호가 일치하지 않음");
        }
    }

    /// <summary>
    /// 지정한 자원에 현재 할당된 Slot 번호를 반환
    /// </summary>
    /// <param name="resourceType">확인할 자원 종류</param>
    /// <returns>할당된 Slot의 번호</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    private int GetResourceSlot(PlayerHUDResourceType resourceType)
    {
        switch (resourceType)
        {
            case PlayerHUDResourceType.Energy:
                return energySlot;
            case PlayerHUDResourceType.Food:
                return foodSlot;
            case PlayerHUDResourceType.Oxygen:
                return oxygenSlot;
            case PlayerHUDResourceType.HP:
                return hpSlot;
            default:
                throw new ArgumentOutOfRangeException(nameof(resourceType), resourceType, "Type이 존재하지 않음");
        }
    }

    /// <summary>
    /// 지정한 Slot 번호에서 사용 중인 자원의 종류를 반환
    /// </summary>
    /// <param name="slotNumber">확인할 Slot 번호</param>
    /// <returns>해당 Slot을 사용 중인 자원 종류</returns>
    /// <exception cref="InvalidOperationException"></exception>
    private PlayerHUDResourceType GetResourceAsSlot(int slotNumber)
    {
        if (energySlot == slotNumber)
            return PlayerHUDResourceType.Energy;
        if (hpSlot == slotNumber)
            return PlayerHUDResourceType.HP;
        if (foodSlot == slotNumber)
            return PlayerHUDResourceType.Food;
        if (oxygenSlot == slotNumber)
            return PlayerHUDResourceType.Oxygen;

        throw new InvalidOperationException($"{slotNumber}번 HUD는 존재하지 않습니다.");
    }

    /// <summary>
    /// 지정한 자원에 새로운 Slot번호를 할당
    /// </summary>
    /// <param name="resourceType">변경할 자원의 종류</param>
    /// <param name="slotNumber">새로운 Slot번호</param>
    private void SetResourceSlot(PlayerHUDResourceType resourceType, int slotNumber)
    {
        switch (resourceType)
        {
            case PlayerHUDResourceType.Energy:
                energySlot = slotNumber;
                break;
            case PlayerHUDResourceType.HP:
                hpSlot = slotNumber;
                break;
            case PlayerHUDResourceType.Food:
                foodSlot = slotNumber;
                break;
            case PlayerHUDResourceType.Oxygen:
                oxygenSlot = slotNumber;
                break;
        }
    }

    /// <summary>
    /// 지정한 자원 종류에 대응하는 RectTransform을 반환
    /// </summary>
    /// <param name="type">확인할 자원 종류</param>
    /// <returns>자원 종류에 대응하는 RectTransform이며 대응 항목이 없으면 null</returns>
    private RectTransform GetResourceRect(PlayerHUDResourceType type)
    {
        switch (type)
        {
            case PlayerHUDResourceType.Energy:
                return energyRect;
            case PlayerHUDResourceType.HP:
                return hpRect;
            case PlayerHUDResourceType.Food:
                return foodRect;
            case PlayerHUDResourceType.Oxygen:
                return oxygenRect;
            default:
                return null;
        }
    }

    /// <summary>
    /// 표시할 플레이어 상태를 HUD에 연결
    /// </summary>
    /// <param name="playerStatus">플레이어 상태 읽기 접근점</param>
    public void Bind(IPlayerStatusReadAccess playerStatus)
    {
        if (playerStatus == null)
            throw new ArgumentNullException(nameof(playerStatus));

        Unbind();

        status = playerStatus;
        status.OnStatusChanged += ApplySnapshot;

        ApplySnapshot(status.GetSnapshot());
    }

    /// <summary>
    /// 현재 연결된 플레이어 상태와 HUD의 연결을 해제
    /// </summary>
    public void Unbind()
    {
        if (status == null)
            return;

        status.OnStatusChanged -= ApplySnapshot;
        status = null;
    }

    /// <summary>
    /// 지전항 자원의 현재 값과 최재 값을 고정 UI에 표시
    /// </summary>
    /// <param name="type">표시할 자워 종류</param>
    public void ShowStatusInfo(PlayerHUDResourceType type)
    {
        activeInfoType = type;

        statusInfoText.gameObject.SetActive(true);

        RefreshStatusInfo();
    }

    /// <summary>
    /// 현재 표시중인 고정 정보 UI를 숨김
    /// </summary>
    public void HideStatusInfo()
    {
        statusInfoText.gameObject.SetActive(false);
        
        activeInfoType = null;
    }

    /// <summary>
    /// 선택한 자원 HUD를 첫 번째 Slot으로 이동하고 기존 자원과 위치를 교환
    /// </summary>
    /// <param name="selectType">첫 번째 Slot으로 이동할 자원 종류</param>
    public void MoveResourceToPrimarySlot(PlayerHUDResourceType selectType)
    {
        int selectedSlot = GetResourceSlot(selectType);

        if (selectedSlot == 1)
            return;

        PlayerHUDResourceType currentPrimaryType = GetResourceAsSlot(1);

        RectTransform primaryRect = GetResourceRect(currentPrimaryType);
        RectTransform selectedRect = GetResourceRect(selectType);

        Vector3 originPrimary = primaryRect.localScale;
        Vector3 originSelect = selectedRect.localScale;

        Sequence se = DOTween.Sequence();

        se.Append(selectedRect.ShrinkAndGrow(0.2f, 0.07f, 0.07f));
        se.Join(primaryRect.ShrinkAndGrow(0.2f, 0.07f, 0.07f));

        se.AppendCallback(() =>
        {
            SetResourceSlot(currentPrimaryType, selectedSlot);
            SetResourceSlot(selectType, 1);

            ApplyResourceLayouts();
        });

        se.Append(selectedRect.ScaleTo(originSelect, 0.07f, Ease.OutBack));
        se.Join(primaryRect.ScaleTo(originPrimary, 0.07f, Ease.OutBack));

        se.SetUpdate(true);
        se.SetLink(gameObject);
    }
}
