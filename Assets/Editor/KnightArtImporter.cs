using UnityEditor;
using UnityEngine;

// 角色美术素材的导入设置：非像素 2D，双线性采样，锚点对齐碰撞盒中心。
// 只影响 Assets/Resources/Art/Characters/ 下的贴图，其它资源不受影响。
public class KnightArtImporter : AssetPostprocessor
{
    const string Folder = "Assets/Resources/Art/Characters/";
    const float PixelsPerUnit = 300f;
    // 画布 704x640，脚底基线距底 48px，锚点取碰撞盒中心 = 基线以上 210px
    // Unity 的 pivot 以左下角为原点：258/640 = 0.403125
    static readonly Vector2 Pivot = new Vector2(0.5f, 258f / 640f);

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Folder)) return;

        var ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.alphaIsTransparency = true;
        ti.mipmapEnabled = false;
        ti.filterMode = FilterMode.Bilinear;
        ti.wrapMode = TextureWrapMode.Clamp;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.maxTextureSize = 1024;

        var s = new TextureImporterSettings();
        ti.ReadTextureSettings(s);
        s.spritePixelsPerUnit = PixelsPerUnit;
        s.spriteAlignment = (int)SpriteAlignment.Custom;
        s.spritePivot = Pivot;
        s.spriteMeshType = SpriteMeshType.FullRect;
        s.alphaIsTransparency = true;
        ti.SetTextureSettings(s);
    }
}
