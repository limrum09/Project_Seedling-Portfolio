using UnityEngine;

/// <summary>
/// 권한 영역에서 받은 플레이어 자원 상태를 Replica에 적용하는 공통 접근점임
/// </summary>
public abstract class PlayerStatusBridge : MonoBehaviour
{
    [SerializeField]
    private PlayerStatusReplica statusReplica;

    /// <summary>
    /// 권한 영역의 전체 플레이어 자원 상태를 Replica에 적용
    /// </summary>
    /// <param name="snapshot">적용할 전체 상태</param>
    protected void ApplyFullSnapshot(PlayerStatusSnapshot snapshot)
    {
        statusReplica.ApplyFullSnapshot(snapshot);
    }

    /// <summary>
    /// 권한 영역의 플레이어 자원 변경값을 Replica에 적용
    /// </summary>
    /// <param name="changeSet">적용할 자원 변경값</param>
    protected void ApplyChangeSet(PlayerStatusChangeSet changeSet)
    {
        statusReplica.ApplyChangeSet(changeSet);
    }

    /// <summary>
    /// 필수 Replica 참조가 연결되어 있는지 Editor에서 확인
    /// </summary>
    protected virtual void OnValidate()
    {
        if (statusReplica == null)
            Debug.LogError("Player Status Bridge에 Player Status Replica 참조가 없음", this);
    }
}
