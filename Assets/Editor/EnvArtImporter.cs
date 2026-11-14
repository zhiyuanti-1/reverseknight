using UnityEditor;
using UnityEngine;

// 场景/道具贴图的导入设置：Sprite、双边线性采样、Repeat 平铺、锚点居中。
// 只影响 Assets/Resources/Art/Environment/ 与 Assets/Resources/Art/Props/。
public class EnvArtImporter : AssetPostprocessor
{
    static readonly string[] Folders =
    {
        "Assets/Resources/Art/Environment/",
        "Assets/Resources/Art/Props/",
    };

    void OnPreprocessTexture()
    {
        bool match = false;
        foreach (var f in Folders)
        {
            if (assetPath.StartsWith(f)) { match = true; break; }
        }
        if (!match) return;

        var ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.alphaIsTransparency = true;
        ti.mipmapEnabled = false;
        ti.filterMode = FilterMode.Bilinear;
        ti.wrapMode = TextureWrapMode.Repeat;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.maxTextureSize = 1024;

        var s = new TextureImporterSettings();
        ti.ReadTextureSettings(s);
        s.spritePixelsPerUnit = 1f;   // 运行时按需要的尺寸用 Transform 缩放，真正的 PPU 在下面按名字覆盖
        s.spriteAlignment = (int)SpriteAlignment.Center;
        s.spriteMeshType = SpriteMeshType.FullRect;
        s.alphaIsTransparency = true;
        ti.SetTextureSettings(s);

        // 平铺贴图想 1 张 = 1 世界格
        if (assetPath.EndsWith("stone_wall.png")) ti.spritePixelsPerUnit = 256f;
    }
}
