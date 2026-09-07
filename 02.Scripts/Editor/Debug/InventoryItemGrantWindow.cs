using UnityEditor;
using UnityEngine;

/// <summary>
/// Play Mode에서 로컬 플레이어 Inventory에 테스트 아이템을 지급하는 Editor Window
/// </summary>
public sealed class InventoryItemGrantWindow : EditorWindow
{
    private ItemDefine selectedItem;
    private int requestAmount = 1;
    private string resultMessage;
    private MessageType resultMessageType = MessageType.Info;

    /// <summary>
    /// Inventory 아이템 지급 Window를 연다.
    /// </summary>
    [MenuItem("Seedling/Debug/Inventory Item Grant")]
    public static void OpenWindow()
    {
        InventoryItemGrantWindow window =
            GetWindow<InventoryItemGrantWindow>("Inventory Item Grant");

        window.minSize = new Vector2(360f, 220f);
    }

    /// <summary>
    /// Play Mode의 Inventory 상태가 Window에 반영되도록 주기적으로 다시 그린다.
    /// </summary>
    private void OnInspectorUpdate()
    {
        Repaint();
    }

    /// <summary>
    /// 아이템 선택, 수량 입력, 지급 결과 UI를 표시한다.
    /// </summary>
    private void OnGUI()
    {
        EditorGUILayout.LabelField(
            "Local Player Inventory",
            EditorStyles.boldLabel);

        EditorGUILayout.Space();

        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox(
                "Play Mode에서만 사용할 수 있습니다.",
                MessageType.Info);

            return;
        }

        if (!TryFindLocalPlayerSpawner(
                out LocalPlayerSpawner spawner,
                out string findError))
        {
            EditorGUILayout.HelpBox(
                findError,
                MessageType.Error);

            return;
        }

        if (spawner.CurrentAuthority == null)
        {
            EditorGUILayout.HelpBox(
                "Local Player Authority가 아직 생성되지 않았습니다.",
                MessageType.Warning);

            return;
        }

        EditorGUILayout.ObjectField(
            "Local Player Spawner",
            spawner,
            typeof(LocalPlayerSpawner),
            true);

        EditorGUILayout.Space();

        selectedItem = (ItemDefine)EditorGUILayout.ObjectField(
            "Item Define",
            selectedItem,
            typeof(ItemDefine),
            false);

        requestAmount = Mathf.Max(
            1,
            EditorGUILayout.IntField(
                "Amount",
                requestAmount));

        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(selectedItem == null))
        {
            if (GUILayout.Button(
                    "아이템 지급",
                    GUILayout.Height(32f)))
            {
                GrantItem(spawner);
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
    /// 현재 열린 Play Scene에서 LocalPlayerSpawner를 찾는다.
    /// </summary>
    /// <param name="spawner">단일 LocalPlayerSpawner</param>
    /// <param name="errorMessage">검색 실패 원인</param>
    /// <returns>정확히 하나의 LocalPlayerSpawner를 찾았으면 true</returns>
    private static bool TryFindLocalPlayerSpawner(
        out LocalPlayerSpawner spawner,
        out string errorMessage)
    {
        LocalPlayerSpawner[] candidates =
            Object.FindObjectsByType<LocalPlayerSpawner>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        spawner = null;
        int sceneObjectCount = 0;

        for (int i = 0; i < candidates.Length; i++)
        {
            LocalPlayerSpawner candidate = candidates[i];

            if (!candidate.gameObject.scene.IsValid()
                || !candidate.gameObject.scene.isLoaded)
            {
                continue;
            }

            spawner = candidate;
            sceneObjectCount++;
        }

        if (sceneObjectCount == 1)
        {
            errorMessage = null;
            return true;
        }

        errorMessage = sceneObjectCount == 0
            ? "현재 Play Scene에서 LocalPlayerSpawner를 찾지 못했습니다."
            : $"LocalPlayerSpawner가 {sceneObjectCount}개 있습니다. 단일 대상을 결정할 수 없습니다.";

        spawner = null;
        return false;
    }

    /// <summary>
    /// 선택한 아이템을 지정 수량만큼 로컬 플레이어 Inventory에 추가한다.
    /// </summary>
    /// <param name="spawner">현재 로컬 플레이어를 생성한 Spawner</param>
    private void GrantItem(LocalPlayerSpawner spawner)
    {
        InventoryAddResult result =
            spawner.CurrentAuthority.ItemReceiver.TryAdd(
                selectedItem,
                requestAmount);

        resultMessage =
            $"Item: {selectedItem.DisplayName}\n" +
            $"Status: {result.Status}\n" +
            $"Request: {result.RequestAmount}\n" +
            $"Added: {result.AddAmount}\n" +
            $"Remain: {result.RemainAmount}\n" +
            $"Reason: {result.Reason}";

        resultMessageType = result.Status switch
        {
            InventoryCallStatus.Success => MessageType.Info,
            InventoryCallStatus.PartialSuccess => MessageType.Warning,
            _ => MessageType.Error
        };
    }
}