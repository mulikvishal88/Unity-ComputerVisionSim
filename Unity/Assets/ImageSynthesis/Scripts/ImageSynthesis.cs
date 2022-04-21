using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
public class ImageSynthesis : MonoBehaviour
{
    public RenderTexture renderTexture;
    [Header("Shader Setup")]
    public Shader uberReplacementShader;
    public Shader opticalFlowShader;
    public float opticalFlowSensitivity = 1.0f;
    private Camera depthCamera;
    private Material opticalFlowMaterial;

    void Start()
    {
        depthCamera = CreateHiddenCamera();
        OnCameraChange();
        OnSceneChange();
    }
    void LateUpdate()
    {
        OnCameraChange();
    }
    private Camera CreateHiddenCamera()
    {
        var cam = new GameObject("HiddenCamera").AddComponent<Camera>();
        cam.hideFlags = HideFlags.HideAndDontSave;
        cam.transform.parent = transform;
        return cam;
    }
    static private void SetupCameraWithReplacementShader(Camera cam, Shader shader, Color clearColor)
    {
        var cb = new CommandBuffer();
        cb.SetGlobalFloat("_OutputMode", 2); // @TODO: CommandBuffer is missing SetGlobalInt() method
        cam.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, cb);
        cam.AddCommandBuffer(CameraEvent.BeforeFinalPass, cb);
        cam.SetReplacementShader(shader, "");
        cam.backgroundColor = clearColor;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.allowHDR = cam.allowMSAA = false;
    }
    public void OnCameraChange()
    {
        var mainCamera = GetComponent<Camera>();
        if (depthCamera == mainCamera)
            return;

        depthCamera.RemoveAllCommandBuffers();
        depthCamera.CopyFrom(mainCamera);
        depthCamera.targetTexture = renderTexture;
        if (!opticalFlowMaterial || opticalFlowMaterial.shader != opticalFlowShader)
            opticalFlowMaterial = new Material(opticalFlowShader);
        opticalFlowMaterial.SetFloat("_Sensitivity", opticalFlowSensitivity);
        SetupCameraWithReplacementShader(depthCamera, uberReplacementShader, Color.white);
    }
    public void OnSceneChange()
    {
        var renderers = FindObjectsOfType<Renderer>();
        var mpb = new MaterialPropertyBlock();
        foreach (var r in renderers)
        {
            var id = r.gameObject.GetInstanceID();
            var layer = r.gameObject.layer;

            mpb.SetColor("_ObjectColor", ColorEncoding.EncodeIDAsColor(id));
            mpb.SetColor("_CategoryColor", ColorEncoding.EncodeLayerAsColor(layer));
            r.SetPropertyBlock(mpb);
        }
    }
}
