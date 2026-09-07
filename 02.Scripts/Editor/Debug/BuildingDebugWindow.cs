using UnityEditor;
using UnityEngine;

/// <summary>
/// Play Mode에서 Building 상태와 공개 요청 API를 제어하는 Editor Window
/// </summary>
public sealed class BuildingDebugWindow : EditorWindow
{
    private BuildingDefine selectedDefine;
    private string resultMessage;
    private MessageType resultMessageType = MessageType.Info;

    /// <summary>
    /// Building Debug Window를 연다.
    /// </summary>
    [MenuItem("Seedling/Debug/Building Control")]
    public static void OpenWindow()
    {
        BuildingDebugWindow window =
            GetWindow<BuildingDebugWindow>("Building Control");

        window.minSize = new Vector2(400f, 520f);
    }

    /// <summary>
    /// Runtime Building 상태를 Window에 지속적으로 반영한다.
    /// </summary>
    private void OnInspectorUpdate()
    {
        Repaint();
    }

    /// <summary>
    /// Building 상태와 실행 가능한 요청 버튼을 표시한다.
    /// </summary>
    private void OnGUI()
    {
        EditorGUILayout.LabelField(
            "Building Runtime Control",
            EditorStyles.boldLabel);

        EditorGUILayout.Space();

        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox(
                "Play Mode에서만 사용할 수 있습니다.",
                MessageType.Info);

            return;
        }

        if (!TryFindBuilding(
                out Building building,
                out string findError))
        {
            EditorGUILayout.HelpBox(
                findError,
                MessageType.Error);

            return;
        }

        DrawState(building);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField(
            "Build Mode",
            EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(
                   building.CurrentState != BuildState.None))
        {
            if (GUILayout.Button("건설 모드 시작"))
            {
                building.StartBuildMode();

                SetResult(
                    building.CurrentState == BuildState.Idle,
                    "건설 모드 시작");
            }
        }

        using (new EditorGUI.DisabledScope(
                   building.CurrentState == BuildState.None))
        {
            if (GUILayout.Button("건설 모드 종료"))
            {
                building.ExitBuildMode();

                SetResult(
                    building.CurrentState == BuildState.None,
                    "건설 모드 종료");
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField(
            "Direct Placement Test",
            EditorStyles.boldLabel);

        selectedDefine =
            (BuildingDefine)EditorGUILayout.ObjectField(
                "Building Define",
                selectedDefine,
                typeof(BuildingDefine),
                false);

        using (new EditorGUI.DisabledScope(
                   selectedDefine == null
                   || building.CurrentState == BuildState.None))
        {
            if (GUILayout.Button("선택한 건물 배치 시작"))
            {
                building.StartSingleBuild(selectedDefine);

                SetResult(
                    building.CurrentState
                    == BuildState.PreviewNewBuilding,
                    "단일 건물 배치 시작");
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField(
            "Selected Building Action",
            EditorStyles.boldLabel);

        BuildingAction action = building.CurrentBuildingAction;

        using (new EditorGUI.DisabledScope(
                   building.CurrentState
                       != BuildState.BuildingSelected
                   || !action.CanBuildCorridor))
        {
            if (GUILayout.Button("통로 건설 시작"))
            {
                SetResult(
                    building.RequestCorridorBuild(),
                    "통로 건설 시작");
            }
        }

        using (new EditorGUI.DisabledScope(
                   building.CurrentState
                       != BuildState.BuildingSelected
                   || !action.CanModify))
        {
            if (GUILayout.Button("선택 건물 재배치"))
            {
                SetResult(
                    building.RequestBuildingReplace(),
                    "건물 재배치");
            }
        }

        using (new EditorGUI.DisabledScope(
                   building.CurrentState
                       != BuildState.BuildingSelected
                   || !action.CanRemove))
        {
            if (GUILayout.Button("선택 건물 철거 요청"))
            {
                SetResult(
                    building.RequestBuildingRemove(),
                    "건물 철거 요청");
            }
        }

        using (new EditorGUI.DisabledScope(
                   building.CurrentState
                       != BuildState.ConfirmRemove))
        {
            if (GUILayout.Button("건물 철거 확정"))
            {
                SetResult(
                    building.ConfirmBuildingRemove(),
                    "건물 철거 확정");
            }
        }

        using (new EditorGUI.DisabledScope(
                   building.CurrentState == BuildState.None
                   || building.CurrentState == BuildState.Idle))
        {
            if (GUILayout.Button("현재 건설 행동 취소"))
            {
                building.CancelCurrentAction();

                SetResult(
                    building.CurrentState == BuildState.Idle,
                    "현재 건설 행동 취소");
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
    /// 현재 Building 상태와 선택된 Runtime 정보를 표시한다.
    /// </summary>
    /// <param name="building">상태를 표시할 Building</param>
    private static void DrawState(Building building)
    {
        EditorGUILayout.ObjectField(
            "Building",
            building,
            typeof(Building),
            true);

        EditorGUILayout.LabelField(
            "Current State",
            building.CurrentState.ToString());

        EditorGUILayout.ObjectField(
            "Selected Runtime",
            building.SelectedRuntime,
            typeof(BuildingRuntime),
            true);

        BuildingAction action = building.CurrentBuildingAction;

        EditorGUILayout.LabelField(
            "Can Build Corridor",
            action.CanBuildCorridor.ToString());

        EditorGUILayout.LabelField(
            "Can Modify",
            action.CanModify.ToString());

        EditorGUILayout.LabelField(
            "Can Remove",
            action.CanRemove.ToString());

        EditorGUILayout.LabelField(
            "Connected Count",
            action.ConnectedCount.ToString());
    }

    /// <summary>
    /// 현재 열린 Play Scene에서 Building을 찾는다.
    /// </summary>
    /// <param name="building">단일 Building 대상</param>
    /// <param name="errorMessage">검색 실패 원인</param>
    /// <returns>정확히 하나의 Building을 찾았으면 true</returns>
    private static bool TryFindBuilding(
        out Building building,
        out string errorMessage)
    {
        Building[] candidates =
            Object.FindObjectsByType<Building>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        building = null;
        int sceneObjectCount = 0;

        for (int i = 0; i < candidates.Length; i++)
        {
            Building candidate = candidates[i];

            if (!candidate.gameObject.scene.IsValid()
                || !candidate.gameObject.scene.isLoaded)
            {
                continue;
            }

            building = candidate;
            sceneObjectCount++;
        }

        if (sceneObjectCount == 1)
        {
            errorMessage = null;
            return true;
        }

        errorMessage = sceneObjectCount == 0
            ? "현재 Play Scene에서 Building을 찾지 못했습니다."
            : $"Building이 {sceneObjectCount}개 있습니다. 단일 대상을 결정할 수 없습니다.";

        building = null;
        return false;
    }

    /// <summary>
    /// Building 요청 결과를 Window에 기록한다.
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