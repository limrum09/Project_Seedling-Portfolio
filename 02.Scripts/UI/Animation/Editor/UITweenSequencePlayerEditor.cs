using DG.DOTweenEditor;
using DG.Tweening;
using UnityEditor;
using UnityEngine;

/// <summary>
/// UITweenSequencePlayer에 런타임 및 Editor Preview 버튼을 제공
/// </summary>
[CustomEditor(typeof(UITweenSequencePlayer))]
public class UITweenSequencePlayerEditor : Editor
{
    private UITweenSequencePlayer Player => (UITweenSequencePlayer)target;

    /// <summary>
    /// 기본 Inspector와 Tween 실행 버튼을 표시
    /// </summary>
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        if (targets.Length != 1)
        {
            EditorGUILayout.HelpBox("Tween Preview는 하나의 오브젝트를 선택했을 때만 사용할 수 있다.", MessageType.Info);

            return;
        }

        if (Application.isPlaying)
        {
            DrawPlayModeButtons();
            return;
        }

        DrawEditorPreviewButtons();
    }

    /// <summary>
    /// Play Mode에서 사용할 Play와 Stop 버튼을 표시
    /// </summary>
    private void DrawPlayModeButtons()
    {
        EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Play"))
                Player.Play();

            if (GUILayout.Button("Stop"))
                Player.Stop();
        }
    }

    /// <summary>
    /// Edit Mode에서 사용할 Preview 버튼을 표시
    /// </summary>
    private void DrawEditorPreviewButtons()
    {
        EditorGUILayout.LabelField("Editor Preview",EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Preview Play"))
                PlayPreview();

            if (GUILayout.Button("Preview Stop"))
                StopPreview();
        }

        EditorGUILayout.HelpBox("Editor Preview에서는 OnStarted, OnCompleted 및 Callback Step을 실행하지 않습니다.", MessageType.Info);
    }

    /// <summary>
    /// 등록된 Tween Step을 Edit Mode에서 미리 재생
    /// </summary>
    private void PlayPreview()
    {
        DOTweenEditorPreview.Stop(true, true);

        Sequence sequence = Player.CreateEditorPreviewSequence();

        DOTweenEditorPreview.PrepareTweenForPreview(sequence, true, true, true);
        DOTweenEditorPreview.Start(Repaint);
    }

    /// <summary>
    /// Editor Preview를 중단하고 대상 값을 재생 전 상태로 복구
    /// </summary>
    private void StopPreview()
    {
        DOTweenEditorPreview.Stop(true, true);

        Repaint();
    }

    /// <summary>
    /// Inspector가 닫히거나 선택이 변경되면 Preview를 정리
    /// </summary>
    private void OnDisable()
    {
        if (Application.isPlaying)
            return;

        DOTweenEditorPreview.Stop(false, true);
    }
}
