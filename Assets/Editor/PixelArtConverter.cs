using System.Collections.Generic;
using Cubo.View;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Cubo.Build
{
    // Passa os prefabs da interface para pixel art, uma etapa por botão, sem mexer em nada fora da pasta de prefabs:
    //   1. PixelScale em toda Image cuja sprite vem da pasta ArtPixel (a arte nova já desenhada).
    //   2. Fonte de pixel em todo texto, com tamanho arredondado para múltiplos do pixel da fonte.
    //   3. Gradientes em faixas chapadas e Breathe/UiBounce já ficam certos sozinhos com o PixelScale.
    //   4. PixelatedRender na câmera do cubo, na cena aberta.
    // Pode rodar de novo quando mais arte ficar pronta: o que já está convertido fica como está.
    public sealed class PixelArtConverter : EditorWindow
    {
        private const string PrefabFolder = "Assets/Game/View/Prefabs";

        [SerializeField] private int artPixel = 6;
        [SerializeField] private TMP_FontAsset pixelFont;
        [SerializeField] private int fontPixelSize = 8;
        [SerializeField] private int gradientBands = 5;

        [MenuItem("Cubo/Pixel Art/Converter para pixel art...")]
        public static void Open() => GetWindow<PixelArtConverter>("Pixel Art");

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Valem para todos os prefabs em " + PrefabFolder, EditorStyles.miniLabel);
            artPixel = Mathf.Max(1, EditorGUILayout.IntField(new GUIContent("Art Pixel", "Unidades da referência (1080x1920) por pixel da arte."), artPixel));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("1. Imagens", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Só imagens com sprite da pasta ArtPixel. Troque a sprite no prefab primeiro, depois rode.", MessageType.None);
            if (GUILayout.Button("Adicionar PixelScale")) Report("PixelScale", EachPrefab(AddPixelScale));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("2. Fontes", EditorStyles.boldLabel);
            pixelFont = (TMP_FontAsset)EditorGUILayout.ObjectField("Fonte de pixel (TMP)", pixelFont, typeof(TMP_FontAsset), false);
            fontPixelSize = Mathf.Max(1, EditorGUILayout.IntField(new GUIContent("Altura da fonte (px)", "Altura em pixels com que a fonte foi desenhada (8, 16...)."), fontPixelSize));
            using (new EditorGUI.DisabledScope(pixelFont == null))
            {
                if (GUILayout.Button("Trocar fontes")) Report("textos", EachPrefab(SwapFonts));
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("3. Gradientes", EditorStyles.boldLabel);
            gradientBands = Mathf.Clamp(EditorGUILayout.IntField("Faixas", gradientBands), 2, 16);
            if (GUILayout.Button("Gradientes em faixas")) Report("gradientes", EachPrefab(BandGradients));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("4. Cubo (cena aberta)", EditorStyles.boldLabel);
            if (GUILayout.Button("Adicionar PixelatedRender à câmera")) AddPixelatedRender();
        }

        // Abre cada prefab, aplica a etapa e salva só se algo mudou. Peças de prefabs aninhados ficam para o próprio arquivo.
        private static int EachPrefab(System.Func<GameObject, int> step)
        {
            int total = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    int n = step(root);
                    if (n > 0) PrefabUtility.SaveAsPrefabAsset(root, path);
                    total += n;
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            return total;
        }

        private static IEnumerable<T> Own<T>(GameObject root) where T : Component
        {
            foreach (var c in root.GetComponentsInChildren<T>(true))
                if (!PrefabUtility.IsPartOfPrefabInstance(c)) yield return c;
        }

        private int AddPixelScale(GameObject root)
        {
            int n = 0;
            foreach (var image in Own<Image>(root))
            {
                if (image.sprite == null || image.GetComponent<PixelScale>() != null) continue;
                if (!AssetDatabase.GetAssetPath(image.sprite).StartsWith(PixelArtImporter.Folder + "/")) continue;
                var pixel = image.gameObject.AddComponent<PixelScale>();
                var so = new SerializedObject(pixel);
                so.FindProperty("artPixel").intValue = artPixel;
                // Esticada pelas âncoras ou cortada em 9: o layout manda no tamanho, não a sprite.
                var rect = image.rectTransform;
                bool stretched = rect.anchorMin != rect.anchorMax || image.type != Image.Type.Simple;
                so.FindProperty("nativeSize").boolValue = !stretched;
                so.ApplyModifiedPropertiesWithoutUndo();
                n++;
            }
            return n;
        }

        // Fonte de pixel só fica nítida em tamanhos múltiplos da altura em que foi desenhada (8, 16, 24...).
        private int SwapFonts(GameObject root)
        {
            int n = 0;
            foreach (var text in Own<TMP_Text>(root))
            {
                if (text.font == pixelFont && text.fontSize % fontPixelSize == 0) continue;
                text.font = pixelFont;
                text.fontSharedMaterial = pixelFont.material;
                text.fontSize = Mathf.Max(1, Mathf.Round(text.fontSize / fontPixelSize)) * fontPixelSize;
                if (text.enableAutoSizing)
                {
                    text.fontSizeMin = Mathf.Max(1, Mathf.Round(text.fontSizeMin / fontPixelSize)) * fontPixelSize;
                    text.fontSizeMax = Mathf.Max(text.fontSizeMin, Mathf.Round(text.fontSizeMax / fontPixelSize) * fontPixelSize);
                }
                n++;
            }
            if (n > 0) PointFilter(pixelFont);
            return n;
        }

        // O atlas da fonte com filtro Bilinear borra as letras; com Point fica no pixel.
        private static void PointFilter(TMP_FontAsset font)
        {
            var atlas = font.atlasTexture;
            if (atlas == null || atlas.filterMode == FilterMode.Point) return;
            atlas.filterMode = FilterMode.Point;
            EditorUtility.SetDirty(atlas);
            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssetIfDirty(font);
        }

        private int BandGradients(GameObject root)
        {
            int n = 0;
            foreach (var g in Own<VerticalGradient>(root))
            {
                var so = new SerializedObject(g);
                var bands = so.FindProperty("hardBands");
                var slices = so.FindProperty("slices");
                if (bands.boolValue && slices.intValue == gradientBands) continue;
                bands.boolValue = true;
                slices.intValue = gradientBands;
                so.ApplyModifiedPropertiesWithoutUndo();
                n++;
            }
            return n;
        }

        private void AddPixelatedRender()
        {
            var cam = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            if (cam == null)
            {
                EditorUtility.DisplayDialog("Cubo", "Abra a cena Main (com a câmera do cubo) antes.", "OK");
                return;
            }
            var render = cam.GetComponent<PixelatedRender>();
            if (render == null) render = Undo.AddComponent<PixelatedRender>(cam.gameObject);
            var so = new SerializedObject(render);
            so.FindProperty("artPixel").intValue = artPixel;
            so.ApplyModifiedProperties();
            EditorGUIUtility.PingObject(cam.gameObject);
            Debug.Log("Cubo: PixelatedRender em " + cam.name + ". Salve a cena para manter.");
        }

        private static void Report(string what, int n) =>
            Debug.Log("Cubo: " + n + " " + what + " convertido(s) nos prefabs de " + PrefabFolder + ".");
    }
}
