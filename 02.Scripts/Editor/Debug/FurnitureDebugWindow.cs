using UnityEditor;
using UnityEngine;

/// <summary>
/// Play Mode에서 FurnitureController 상태와 공개 요청 API를 제어하는 Editor Window
/// </summary>
public sealed class FurnitureDebugWindow : EditorWindow
{
    private FurnitureDefine selectedDefine;
    private string resultMessage;
    private MessageType resultMessageType = MessageType.Info;

    /// <summary>
    /// Furniture Debug Window를 연다.
    /// </summary>
    [MenuItem("Seedling/Debug/Furniture Control")]
    public static void OpenWindow()
    {
        FurnitureDebugWindow window =
            GetWindow<FurnitureDebugWindow>(
                "Furniture Control");

        window.minSize = new Vector2(400f, 500f);
    }

    /// <summary>
    /// Runtime Furniture 상태를 Window에 지속적으로 반영한다.
    /// </summary>
    private void OnInspectorUpdate()
    {
        Repaint();
    }

    /// <summary>
    /// Furniture 상태와 실행 가능한 요청 버튼을 표시한다.
    /// </summary>
    private void OnGUI()
    {
        EditorGUILayout.LabelField(
            "Furniture Runtime Control",
            EditorStyles.boldLabel);

        EditorGUILayout.Space();

        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox(
                "Play Mode에서만 사용할 수 있습니다.",
                MessageType.Info);

            return;
        }

        if (!TryFindFurnitureController(
                out FurnitureController controller,
                out string findError))
        {
            EditorGUILayout.HelpBox(
                findError,
                MessageType.Error);

            return;
        }

        DrawState(controller);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField(
            "Furniture Mode",
            EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(
                   controller.CurrentState
                       != FurnitureState.None))
        {
            if (GUILayout.Button("가구 모드 시작"))
            {
                controller.StartPlaceMode();

                SetResult(
                    controller.CurrentState
                        == FurnitureState.SelectingFurniture,
                    "가구 모드 시작");
            }
        }

        using (new EditorGUI.DisabledScope(
                   controller.CurrentState
                       == FurnitureState.None))
        {
            if (GUILayout.Button("가구 모드 종료"))
            {
                controller.CancelPlaceMode();

                SetResult(
                    controller.CurrentState
                        == FurnitureState.None,
                    "가구 모드 종료");
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField(
            "Direct Placement Test",
            EditorStyles.boldLabel);

        selectedDefine =
            (FurnitureDefine)EditorGUILayout.ObjectField(
                "Furniture Define",
                selectedDefine,
                typeof(FurnitureDefine),
                false);

        using (new EditorGUI.DisabledScope(
                   selectedDefine == null
                   || !controller.IsActive))
        {
            if (GUILayout.Button("선택한 가구 배치 시작"))
            {
                SetResult(
                    controller.Begin(selectedDefine),
                    "가구 배치 시작");
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField(
            "Selected Furniture Action",
            EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(
                   controller.CurrentState
                       != FurnitureState.SelectedFurniture))
        {
            if (GUILayout.Button("선택 가구 재배치"))
            {
                SetResult(
                    controller.RequestFurnitureReplace(),
                    "가구 재배치");
            }
        }

        using (new EditorGUI.DisabledScope(
                   controller.CurrentState
                       != FurnitureState.SelectedFurniture))
        {
            if (GUILayout.Button("선택 가구 철거 요청"))
            {
                SetResult(
                    controller.RequestFurnitureRemove(),
                    "가구 철거 요청");
            }
        }

        using (new EditorGUI.DisabledScope(
                   controller.CurrentState
                       != FurnitureState.ConfirmRemove))
        {
            if (GUILayout.Button("가구 철거 확정"))
            {
                SetResult(
                    controller.ConfirmFurnitureRemove(),
                    "가구 철거 확정");
            }
        }

        using (new EditorGUI.DisabledScope(
                   controller.CurrentState
                       == FurnitureState.None))
        {
            if (GUILayout.Button("현재 가구 행동 취소"))
            {
                FurnitureState previousState =
                    controller.CurrentState;

                controller.CancelCurrentAction();

                bool changed =
                    controller.CurrentState != previousState;

                SetResult(
                    changed,
                    "현재 가구 행동 취소");
            }
        }

        if (!string.IsNullOrEmpty(resultMessage))
        {
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                resultMessage,
                resultMessageType);
        }
    }

    /// <summary>
    /// 현재 FurnitureController 상태와 선택된 가구 정보를 표시한다.
    /// </summary>
    /// <param name="controller">상태를 표시할 FurnitureController</param>
    private static void DrawState(
        FurnitureController controller)
    {
        EditorGUILayout.ObjectField(
            "Furniture Controller",
            controller,
            typeof(FurnitureController),
            true);

        EditorGUILayout.LabelField(
            "Current State",
            controller.CurrentState.ToString());

        EditorGUILayout.ObjectField(
            "Selected Furniture",
            controller.SelectedFurniture,
            typeof(FurnitureRuntime),
            true);

        EditorGUILayout.ObjectField(
            "Selected Define",
            controller.SelectedFurnitureDefine,
            typeof(FurnitureDefine),
            false);

        EditorGUILayout.LabelField(
            "Is Active",
            controller.IsActive.ToString());

        EditorGUILayout.LabelField(
            "Is Previewing",
            controller.IsPreviewing.ToString());

        EditorGUILayout.LabelField(
            "Current Can Place",
            controller.CurrentCanPlace.ToString());
    }

    /// <summary>
    /// 현재 열린 Play Scene에서 FurnitureController를 찾는다.
    /// </summary>
    /// <param name="controller">단일 FurnitureController 대상</param>
    /// <param name="errorMessage">검색 실패 원인</param>
    /// <returns>정확히 하나의 FurnitureController를 찾았으면 true</returns>
    private static bool TryFindFurnitureController(
        out FurnitureController controller,
        out string errorMessage)
    {
        FurnitureController[] candidates =
            Object.FindObjectsByType<FurnitureController>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        controller = null;
        int sceneObjectCount = 0;

        for (int i = 0; i < candidates.Length; i++)
        {
            FurnitureController candidate =
                candidates[i];

            if (!candidate.gameObject.scene.IsValid()
                || !candidate.gameObject.scene.isLoaded)
            {
                continue;
            }

            controller = candidate;
            sceneObjectCount++;
        }

        if (sceneObjectCount == 1)
        {
            errorMessage = null;
            return true;
        }

        errorMessage = sceneObjectCount == 0
            ? "현재 Play Scene에서 FurnitureController를 찾지 못했습니다."
            : $"FurnitureController가 {sceneObjectCount}개 있습니다. 단일 대상을 결정할 수 없습니다.";

        controller = null;
        return false;
    }

    /// <summary>
    /// Furniture 요청 결과를 Window에 기록한다.
    /// </summary>
    /// <param name="success">요청 성공 여부</param>
    /// <param name="actionName">실행한 요청 이름</param>
    private void SetResult(
        bool success,
        string actionName)
    {
        resultMessage = success
            ? $"{actionName} 요청이 성공했습니다."
            : $"{actionName} 요청이 현재 상태 또는 규칙에 의해 거부되었습니다.";

        resultMessageType =
            success
                ? MessageType.Info
                : MessageType.Warning;
    }
}