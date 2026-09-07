using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// PrefabCaptureStudio에 Prefab 교체, Transform 초기화와 PNG 촬영 기능을 제공
/// </summary>
[CustomEditor(typeof(PrefabCaptureStudio))]
public sealed class PrefabCaptureStudioEditor : Editor
{
    private PrefabCaptureStudio Studio => (PrefabCaptureStudio)target;

    /// <summary>
    /// 촬영 설정과 Editor 작업 버튼을 Inspector에 표시
    /// </summary>
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Prefab Capture", EditorStyles.boldLabel);

        if (Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Prefab 촬영은 Edit Mode에서만 사용할 수 있다.", MessageType.Info);
            return;
        }

        DrawActionButtons();
    }

    /// <summary>
    /// Prefab 적용과 이동, 기본 Transform 재적용 및 PNG 촬영 버튼을 표시
    /// </summary>
    private void DrawActionButtons()
    {
        if (GUILayout.Button("Prefab 적용"))
            ApplyPrefab();

        if (GUILayout.Button("Prefab으로 이동"))
            MoveToPreview();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("기본 Transform 재적용"))
                ApplyDefaultTransform();

            if (GUILayout.Button("PNG 촬영"))
                CapturePng();
        }

        EditorGUILayout.HelpBox("Prefab 적용 후 생성된 Instance를 Scene에서 직접 이동하거나 회전한 상태로 촬영할 수 있다.", MessageType.Info);
    }

    /// <summary>
    /// 기존 Preview를 제거하고 선택한 Prefab을 PreviewRoot 아래에 생성
    /// </summary>
    private void ApplyPrefab()
    {
        if (!TryValidatePrefabSetup(out string error))
        {
            Debug.LogError(error, Studio);
            return;
        }

        if (Studio.CurrentPreview != null)
            Undo.DestroyObjectImmediate(Studio.CurrentPreview);

        GameObject preview = (GameObject)PrefabUtility.InstantiatePrefab(Studio.TargetPrefab, Studio.PreviewRoot);

        Undo.RegisterCreatedObjectUndo(preview, "Apply Capture Prefab");
        ApplyTransform(preview.transform);

        Undo.RecordObject(Studio, "Set Capture Preview");
        Studio.SetCurrentPreview(preview);

        EditorUtility.SetDirty(Studio);
        EditorSceneManager.MarkSceneDirty(Studio.gameObject.scene);
    }

    /// <summary>
    /// 현재 Preview를 선택하고 활성 Scene View의 화면 중앙에 표시
    /// </summary>
    private void MoveToPreview()
    {
        if (Studio.CurrentPreview == null)
        {
            Debug.LogError("이동할 Preview가 없다.", Studio);
            return;
        }

        Selection.activeGameObject = Studio.CurrentPreview;

        if (SceneView.lastActiveSceneView != null)
            SceneView.lastActiveSceneView.FrameSelected();
    }

    /// <summary>
    /// 현재 Preview에 Studio의 기본 위치, 회전과 크기를 다시 적용
    /// </summary>
    private void ApplyDefaultTransform()
    {
        if (Studio.CurrentPreview == null)
        {
            Debug.LogError("기본 Transform을 적용할 Preview가 없다.", Studio);
            return;
        }

        Undo.RecordObject(Studio.CurrentPreview.transform, "Reset Capture Preview Transform");
        ApplyTransform(Studio.CurrentPreview.transform);
        EditorSceneManager.MarkSceneDirty(Studio.gameObject.scene);
    }

    /// <summary>
    /// 지정한 Transform에 Studio의 기본 Local Transform을 적용
    /// </summary>
    /// <param name="previewTransform">기본 Transform을 적용할 Preview Transform</param>
    private void ApplyTransform(Transform previewTransform)
    {
        previewTransform.SetLocalPositionAndRotation(
            Studio.DefaultLocalPosition,
            Quaternion.Euler(Studio.DefaultLocalEulerAngles));
        previewTransform.localScale = Studio.DefaultLocalScale;
    }

    /// <summary>
    /// 현재 Camera 화면을 투명 배경 PNG로 촬영하고 Asset으로 가져옴
    /// </summary>
    private void CapturePng()
    {
        if (!TryValidateCaptureSetup(out string error))
        {
            Debug.LogError(error, Studio);
            return;
        }

        string assetPath = BuildOutputPath();
        string absolutePath = Path.GetFullPath(assetPath);

        Camera captureCamera = Studio.CaptureCamera;
        CameraClearFlags previousClearFlags = captureCamera.clearFlags;
        Color previousBackgroundColor = captureCamera.backgroundColor;
        RenderTexture previousTargetTexture = captureCamera.targetTexture;
        RenderTexture previousActiveTexture = RenderTexture.active;
        bool previousAllowHdr = captureCamera.allowHDR;

        RenderTexture renderTexture = new RenderTexture(
            Studio.ImageWidth,
            Studio.ImageHeight,
            24,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.sRGB)
        {
            antiAliasing = 4
        };

        Texture2D image = new Texture2D(
            Studio.ImageWidth,
            Studio.ImageHeight,
            TextureFormat.RGBA32,
            false,
            false);

        try
        {
            captureCamera.clearFlags = CameraClearFlags.SolidColor;
            captureCamera.backgroundColor = Color.clear;
            captureCamera.allowHDR = false;
            captureCamera.targetTexture = renderTexture;

            if (!renderTexture.Create())
                throw new InvalidOperationException("Prefab 촬영용 RenderTexture를 생성할 수 없다.");

            captureCamera.Render();
            RenderTexture.active = renderTexture;

            image.ReadPixels(new Rect(0f, 0f, Studio.ImageWidth, Studio.ImageHeight), 0, 0, false);
            image.Apply(false, false);

            File.WriteAllBytes(absolutePath, image.EncodeToPNG());
        }
        finally
        {
            captureCamera.clearFlags = previousClearFlags;
            captureCamera.backgroundColor = previousBackgroundColor;
            captureCamera.allowHDR = previousAllowHdr;
            captureCamera.targetTexture = previousTargetTexture;
            RenderTexture.active = previousActiveTexture;

            renderTexture.Release();
            DestroyImmediate(renderTexture);
            DestroyImmediate(image);
        }

        ImportCapturedTexture(assetPath);
    }

    /// <summary>
    /// Prefab 생성에 필요한 Studio 참조와 대상 Prefab을 확인
    /// </summary>
    /// <param name="error">구성이 잘못된 경우 표시할 오류</param>
    /// <returns>Prefab을 생성할 수 있으면 true 반환</returns>
    private bool TryValidatePrefabSetup(out string error)
    {
        if (Studio.PreviewRoot == null)
        {
            error = "PreviewRoot가 연결되지 않았다.";
            return false;
        }

        if (Studio.TargetPrefab == null)
        {
            error = "촬영할 Prefab이 지정되지 않았다.";
            return false;
        }

        if (!PrefabUtility.IsPartOfPrefabAsset(Studio.TargetPrefab))
        {
            error = "Target Prefab에는 Project의 GameObject Prefab Asset을 지정해야 한다.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    /// <summary>
    /// PNG 촬영에 필요한 Camera, Preview, 해상도와 출력 경로를 확인
    /// </summary>
    /// <param name="error">구성이 잘못된 경우 표시할 오류</param>
    /// <returns>PNG를 촬영할 수 있으면 true 반환</returns>
    private bool TryValidateCaptureSetup(out string error)
    {
        if (Studio.CaptureCamera == null)
        {
            error = "Capture Camera가 연결되지 않았다.";
            return false;
        }

        if (Studio.CurrentPreview == null)
        {
            error = "촬영할 Preview가 없다. Prefab 적용을 먼저 실행해야 한다.";
            return false;
        }

        if (Studio.ImageWidth <= 0 || Studio.ImageHeight <= 0)
        {
            error = "촬영 해상도는 1 이상이어야 한다.";
            return false;
        }

        string outputFolder = Studio.OutputFolder.Replace('\\', '/').TrimEnd('/');

        if (string.IsNullOrWhiteSpace(outputFolder) ||
            (!outputFolder.Equals("Assets", StringComparison.Ordinal) &&
             !outputFolder.StartsWith("Assets/", StringComparison.Ordinal)))
        {
            error = "Output Folder는 Assets 내부 경로여야 한다.";
            return false;
        }

        EnsureOutputFolder(outputFolder);

        error = string.Empty;
        return true;
    }

    /// <summary>
    /// 지정한 Asset 출력 폴더가 없으면 생성
    /// </summary>
    /// <param name="outputFolder">Assets부터 시작하는 출력 폴더 경로</param>
    private void EnsureOutputFolder(string outputFolder)
    {
        if (AssetDatabase.IsValidFolder(outputFolder))
            return;

        Directory.CreateDirectory(Path.GetFullPath(outputFolder));
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// 현재 Preview 이름을 이용해 PNG 출력 경로를 생성
    /// </summary>
    /// <returns>Assets부터 시작하는 PNG 출력 경로</returns>
    private string BuildOutputPath()
    {
        string outputFolder = Studio.OutputFolder.Replace('\\', '/').TrimEnd('/');
        string fileName = SanitizeFileName(Studio.CurrentPreview.name);

        return $"{outputFolder}/{fileName}.png";
    }

    /// <summary>
    /// 파일명에 사용할 수 없는 문자를 밑줄로 교체
    /// </summary>
    /// <param name="fileName">정리할 파일명</param>
    /// <returns>파일 경로에 사용할 수 있는 파일명</returns>
    private string SanitizeFileName(string fileName)
    {
        foreach (char invalidCharacter in Path.GetInvalidFileNameChars())
            fileName = fileName.Replace(invalidCharacter, '_');

        return fileName;
    }

    /// <summary>
    /// 생성된 PNG를 가져오고 투명도와 Sprite Import 설정을 적용
    /// </summary>
    /// <param name="assetPath">가져올 PNG의 Asset 경로</param>
    private void ImportCapturedTexture(string assetPath)
    {
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;

        if (importer == null)
            throw new InvalidOperationException($"생성한 PNG의 TextureImporter를 찾을 수 없다: {assetPath}");

        importer.alphaIsTransparency = true;

        importer.textureType = Studio.ImportAsSprite
            ? TextureImporterType.Sprite
            : TextureImporterType.Default;

        importer.SaveAndReimport();

        UnityEngine.Object capturedTexture = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
        EditorGUIUtility.PingObject(capturedTexture);
        Debug.Log($"Prefab PNG 촬영 완료: {assetPath}", Studio);
    }
}
