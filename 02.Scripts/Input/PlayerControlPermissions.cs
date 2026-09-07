using System;

/// <summary>
/// Player가 사용할 수 있는 조작 권한 정의
/// UI 입력은 권한과 관계없이 항상 처리
/// </summary>
[Flags]
public enum PlayerControlPermissions
{
    None = 0,
    Movement = 1 << 0,
    Combat = 1 << 1,
    WorldInteraction = 1 << 2,
    Hotkeys = 1 << 3,
    All = Movement | Combat | WorldInteraction | Hotkeys
}
