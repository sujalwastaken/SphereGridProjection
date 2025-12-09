using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SkyboxPatchProjector : MonoBehaviour
{
    [Header("Input")]
    public Texture2D editedPatch;         // Your CSP edited/painted image
    public Texture2D skyboxTexture;       // Duplicate of your skybox equirectangular texture
    public Camera sourceCamera;           // The camera used to capture the original frame

    [Header("Screenshot Resolution")]
    public int outputWidth = 1920;        // screenshot width
    public int outputHeight = 1080;       // screenshot height

    [Range(0f, 1f)]
    public float opacity = 1f;            // Blend amount



    // ---------------------------------------------------------
    //   PROJECT PATCH INTO SKYBOX
    // ---------------------------------------------------------
    public void ProjectPatch()
    {
        if (!editedPatch || !skyboxTexture || !sourceCamera)
        {
            Debug.LogError("Missing references on SkyboxPatchProjector!");
            return;
        }

        Debug.Log("Projecting texture patch onto skybox…");

        int W = skyboxTexture.width;
        int H = skyboxTexture.height;

        int PW = editedPatch.width;
        int PH = editedPatch.height;

        float fov = sourceCamera.fieldOfView * Mathf.Deg2Rad;
        float aspect = sourceCamera.aspect;
        Quaternion rot = sourceCamera.transform.rotation;

        Color[] skyPixels = skyboxTexture.GetPixels();

        for (int y = 0; y < H; y++)
        {
            float v = (float)y / (H - 1);
            float lat = (v - 0.5f) * Mathf.PI;

            for (int x = 0; x < W; x++)
            {
                float u = (float)x / (W - 1);
                float lon = (u - 0.5f) * Mathf.PI * 2f;

                // Direction from pano pixel
                Vector3 dir = new Vector3(
                    Mathf.Sin(lon) * Mathf.Cos(lat),
                    Mathf.Sin(lat),
                    Mathf.Cos(lon) * Mathf.Cos(lat)
                );

                // FIX: align pano → Unity forward direction
                dir = Quaternion.Euler(0, 90, 0) * dir;

                // Rotate into camera local space
                Vector3 camDir = Quaternion.Inverse(rot) * dir;

                // Behind camera → skip
                if (camDir.z <= 0) continue;

                float px = camDir.x / camDir.z;
                float py = camDir.y / camDir.z;

                float tanFov = Mathf.Tan(fov / 2f);

                if (Mathf.Abs(px) > aspect * tanFov) continue;
                if (Mathf.Abs(py) > tanFov) continue;

                float uPatch = (px / (aspect * tanFov)) * 0.5f + 0.5f;
                float vPatch = (py / tanFov) * 0.5f + 0.5f;

                if (uPatch < 0f || uPatch > 1f || vPatch < 0f || vPatch > 1f)
                    continue;

                int pxX = Mathf.FloorToInt(uPatch * (PW - 1));
                int pxY = Mathf.FloorToInt(vPatch * (PH - 1));

                Color patchColor = editedPatch.GetPixel(pxX, pxY);

                if (patchColor.a > 0.001f)
                {
                    int idx = y * W + x;
                    skyPixels[idx] = Color.Lerp(skyPixels[idx], patchColor, patchColor.a * opacity);
                }
            }
        }

        skyboxTexture.SetPixels(skyPixels);
        skyboxTexture.Apply();

        Debug.Log("Skybox patch projection complete.");
    }



    // ---------------------------------------------------------
    //   HIGH-RES, CORRECT-ASPECT CAMERA SCREENSHOT
    // ---------------------------------------------------------
    public void CaptureScreenshot()
    {
        if (!sourceCamera)
        {
            Debug.LogError("No camera assigned for screenshot!");
            return;
        }

        int width = outputWidth;
        int height = outputHeight;

        // Correct RenderTexture matching your chosen resolution
        RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 1;
        sourceCamera.targetTexture = rt;

        Texture2D screenshot = new Texture2D(width, height, TextureFormat.RGBA32, false);

        sourceCamera.Render();
        RenderTexture.active = rt;

        screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        screenshot.Apply();

        sourceCamera.targetTexture = null;
        RenderTexture.active = null;
        DestroyImmediate(rt);

        // Calculate real FOVs for metadata
        float vfov = sourceCamera.fieldOfView;
        float hfov = 2f * Mathf.Atan(Mathf.Tan(vfov * Mathf.Deg2Rad / 2f) * sourceCamera.aspect) * Mathf.Rad2Deg;

        Vector3 e = sourceCamera.transform.eulerAngles;

        // Filename with rotation and FOV and resolution
        string filename =
            $"{Mathf.RoundToInt(e.x)}_{Mathf.RoundToInt(e.y)}_{Mathf.RoundToInt(e.z)}_" +
            $"HFOV_{Mathf.RoundToInt(hfov)}_VFOV_{Mathf.RoundToInt(vfov)}_" +
            $"{width}x{height}.jpg";

        // Ensure folder exists: Assets/Screenshots/
        string folder = Path.Combine(Application.dataPath, "Screenshots");
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        // Save inside Assets/Screenshots/
        string path = Path.Combine(folder, filename);

        File.WriteAllBytes(path, screenshot.EncodeToJPG());

        Debug.Log("Screenshot saved to: " + path);

#if UNITY_EDITOR
        AssetDatabase.Refresh();
#endif

        // Automatically use the screenshot for projection
        editedPatch = screenshot;
        Debug.Log("editedPatch updated to the latest screenshot.");
    }



    // Context menu shortcuts
    [ContextMenu("Project Patch Now")]
    void ContextProject() => ProjectPatch();

    [ContextMenu("Capture Screenshot Now")]
    void ContextScreenshot() => CaptureScreenshot();
}



// ---------------------------------------------------------
//      CUSTOM INSPECTOR BUTTONS
// ---------------------------------------------------------
#if UNITY_EDITOR
[CustomEditor(typeof(SkyboxPatchProjector))]
public class SkyboxPatchProjectorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SkyboxPatchProjector projector = (SkyboxPatchProjector)target;

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Capture Camera Screenshot"))
            projector.CaptureScreenshot();

        if (GUILayout.Button("Project Patch Into Skybox"))
            projector.ProjectPatch();
    }
}
#endif
