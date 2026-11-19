using System.IO;
using Cubo.View;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Cubo.Build
{
    // Tudo o que entrar em Assets/Game/View/ArtPixel é importado como pixel art: sprite, filtro Point (sem borrar),
    // sem compressão (que mancharia as cores da paleta), sem mipmaps e 100 pixels por unidade, igual ao CanvasScaler.
    // Fica forçado a cada importação: a pasta é a regra, não precisa mexer no Inspector.
    public sealed class PixelArtImporter : AssetPostprocessor
    {
        public const string Folder = "Assets/Game/View/ArtPixel";

        private void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').StartsWith(Folder + "/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            if (importer.spriteImportMode == SpriteImportMode.None) importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            foreach (var platform in new[] { "Android", "iPhone", "Standalone" })
            {
                var settings = importer.GetPlatformTextureSettings(platform);
                if (!settings.overridden) continue;
                settings.format = TextureImporterFormat.RGBA32;
                importer.SetPlatformTextureSettings(settings);
            }
        }

        // Monta uma carta de teste no Canvas selecionado (ou no primeiro da cena) com as sprites da pasta:
        // moldura.png por trás, retrato.png no meio e mana.png no canto. Falta alguma? Fica um quadrado vazio no lugar.
        [MenuItem("Cubo/Pixel Art/Montar carta de teste na cena")]
        public static void BuildTestCard()
        {
            var canvas = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponentInParent<Canvas>() : null;
            if (canvas == null) canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Cubo", "Abra uma cena com um Canvas (ou selecione um) antes de montar a carta.", "OK");
                return;
            }

            var card = new GameObject("CartaPixelTeste", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(card, "Carta de teste pixel art");
            card.transform.SetParent(canvas.transform, false);

            Part(card.transform, "Moldura", "moldura.png", Vector2.zero, new Vector2(0.5f, 0.5f));
            Part(card.transform, "Retrato", "retrato.png", new Vector2(0f, 30f), new Vector2(0.5f, 0.5f));
            Part(card.transform, "Mana", "mana.png", new Vector2(-150f, 240f), new Vector2(0.5f, 0.5f));
            Selection.activeGameObject = card;
        }

        [MenuItem("Cubo/Pixel Art/Mostrar a pasta ArtPixel")]
        public static void Reveal()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder(Path.GetDirectoryName(Folder), Path.GetFileName(Folder));
            EditorUtility.RevealInFinder(Folder);
        }

        private static void Part(Transform parent, string name, string file, Vector2 position, Vector2 pivot)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(PixelScale));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            var image = go.GetComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "/" + file);
            if (image.sprite != null) image.SetNativeSize();
            else rect.sizeDelta = new Vector2(64f, 64f);
            image.raycastTarget = false;
        }
    }
}
