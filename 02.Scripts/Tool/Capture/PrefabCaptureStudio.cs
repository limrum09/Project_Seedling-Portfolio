using UnityEngine;

/// <summary>
/// Prefab 촬영에 사용할 Camera, Preview와 출력 설정을 보관
/// Prefab 생성과 이미지 저장은 Editor에서만 처리
/// </summary>
public sealed class PrefabCaptureStudio : MonoBehaviour
{
    [SerializeField]
    private Camera captureCamera;
    [SerializeField]
    private Transform previewRoot;
    [SerializeField]
    private GameObject targetPrefab;
    [SerializeField]
    private Vector3 defaultLocalPosition;
    [SerializeField]
    private Vector3 defaultLocalEulerAngles;
    [SerializeField]
    private Vector3 defaultLocalScale = Vector3.one;
    [SerializeField, Min(1)]
    private int imageWidth = 512;
    [SerializeField, Min(1)]
    private int imageHeight = 512;
    [SerializeField]
    private bool importAsSprite = true;
    [SerializeField]
    private string outputFolder = "Assets/_Project/04.Art/Captures";
    [SerializeField, HideInInspector]
    private GameObject currentPreview;

    public Camera CaptureCamera => captureCamera;
    public Transform PreviewRoot => previewRoot;
    public GameObject TargetPrefab => targetPrefab;
    public Vector3 DefaultLocalPosition => defaultLocalPosition;
    public Vector3 DefaultLocalEulerAngles => defaultLocalEulerAngles;
    public Vector3 DefaultLocalScale => defaultLocalScale;
    public int ImageWidth => imageWidth;
    public int ImageHeight => imageHeight;
    public bool ImportAsSprite => importAsSprite;
    public string OutputFolder => outputFolder;
    public GameObject CurrentPreview => currentPreview;

    /// <summary>
    /// 현재 Scene에 생성된 촬영 대상을 저장
    /// </summary>
    /// <param name="preview">현재 촬영에 사용할 Prefab Instance</param>
    public void SetCurrentPreview(GameObject preview)
    {
        currentPreview = preview;
    }
}
