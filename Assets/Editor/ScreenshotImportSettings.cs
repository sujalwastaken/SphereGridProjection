using UnityEngine;
using UnityEditor;

class ScreenshotImportSettings : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        // Only apply to screenshots
        if (!assetPath.Contains("/Screenshots/"))
            return;

        TextureImporter importer = (TextureImporter)assetImporter;

        importer.textureType = TextureImporterType.Default;
        importer.textureShape = TextureImporterShape.Texture2D;
        importer.sRGBTexture = true;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = false;

        importer.npotScale = TextureImporterNPOTScale.ToNearest;
        importer.isReadable = true;
        importer.mipmapEnabled = false;

        importer.wrapMode = TextureWrapMode.Repeat;
        importer.filterMode = FilterMode.Point;
        importer.anisoLevel = 1;

        importer.maxTextureSize = 8192;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.compressionQuality = 100;
    }

    void OnPostprocessTexture(Texture2D texture)
    {
        if (!assetPath.Contains("/Screenshots/"))
            return;

        Debug.Log($"[Screenshot Importer] Settings applied to: {assetPath}");
    }
}
