using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Cubo.View;

namespace Cubo.Build
{
    // Monta a bênção desenhada com a API da Unity: os prefabs BlessingCard e BlessingScreen, a cópia na cena Main e as
    // ligações (Court e Game Bootstrap ▸ Blessing). Reaproveita o kit do Arsenal (placa, espaço de item, botão, fontes);
    // a arte própria da bênção (a capela e a moldura da carta) vem de Art/Blessing quando existir, senão fica uma cor
    // de rascunho. Roda sozinho uma vez (quando o prefab ainda não existe, fora do Play) e pelo menu Cubo ▸ Telas.
    [InitializeOnLoad]
    public static class BlessingScreenBuilder
    {
        private const string Prefabs = "Assets/Game/View/Prefabs/UI/";
        private const string CardPath = Prefabs + "BlessingCard.prefab", ScreenPath = Prefabs + "BlessingScreen.prefab";
        private const string MainScene = "Assets/Game/Content/Materials/Scenes/Main.unity";
        private const string Art = "Assets/Game/View/Art/";
        private const string BlessArt = Art + "Blessing/";
        private const float Density = 0.29f;                       // o mesmo Pixels Per Unit Multiplier do armário
        private const string BiggerKey = "Cubo.Blessing.Bigger.v4";

        private static readonly Color TitleGold = Hex("FFD27A"), NameGold = Hex("F2CC73"), Body = Hex("DBDBE6"), Muted = Hex("A9A9B8");

        static BlessingScreenBuilder()
        {
            EditorApplication.delayCall += RunPending;
            EditorApplication.playModeStateChanged += st => { if (st == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += RunPending; };
        }

        private static void RunPending()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            // monta sozinho só quando a arte da bênção já chegou (antes disso, pelo menu, com cores de rascunho)
            if (EditorSceneManager.GetActiveScene().path != MainScene) return;
            if (!File.Exists(ScreenPath) && Directory.Exists(BlessArt)) { Build(); EditorPrefs.SetBool(BiggerKey, true); }
            // uma vez: as cartas e os textos maiores (2026-11-14); os prefabs não tinham mudanças à mão
            else if (File.Exists(ScreenPath) && !EditorPrefs.GetBool(BiggerKey, false)) { EditorPrefs.SetBool(BiggerKey, true); Build(); }
        }

        [MenuItem("Cubo/Telas/Montar a Bênção (BlessingScreen)")]
        public static void BuildFromMenu()
        {
            if (EditorApplication.isPlaying) { EditorUtility.DisplayDialog("Cubo", "Saia do Play antes de montar a bênção.", "OK"); return; }
            if (File.Exists(ScreenPath) && !EditorUtility.DisplayDialog("Cubo", "Os prefabs BlessingCard e BlessingScreen já existem. Montar de novo apaga o que você mudou neles.", "Montar de novo", "Cancelar")) return;
            Build();
        }

        public static void Build()
        {
            ImportArt();
            var alagard = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Game/View/Fonts/alagard.asset");
            var romulus = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Game/View/Fonts/Romulus.asset");
            BuildCard(alagard, romulus);
            Settle(CardPath);
            var card = AssetDatabase.LoadAssetAtPath<GameObject>(CardPath).GetComponent<BlessingCard>();
            BuildScreen(card, alagard, romulus);
            Settle(ScreenPath);
            WireScene();
            Debug.Log("Bênção montada: " + CardPath + " e " + ScreenPath + ".");
        }

        // ------------------------------------------------------------------ a carta

        private static void BuildCard(TMP_FontAsset alagard, TMP_FontAsset romulus)
        {
            var root = Node("BlessingCard", null);
            Size(Rt(root), 336, 820);
            Img(root, null, new Color(1, 1, 1, 0), true);                           // invisível, pega o toque

            var body = Node("Body", root.transform);                                 // o que sobe na carta escolhida
            Stretch(Rt(body));

            var glow = Node("Glow", body.transform);
            Stretch(Rt(glow), -12, -12, -12, -12);
            Img(glow, null, new Color(1f, 0.85f, 0.45f, 0.35f), false);
            glow.SetActive(false);

            var frame = Node("Frame", body.transform);
            Stretch(Rt(frame));
            var cardArt = BlessSprite("blessing_card");
            var fi = Img(frame, cardArt, cardArt != null ? Color.white : Hex("2B2F38"), false);
            if (cardArt != null) { fi.type = Image.Type.Sliced; fi.fillCenter = true; fi.pixelsPerUnitMultiplier = Density; }

            var slot = Node("Slot", body.transform);
            Set(Rt(slot), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -48), new Vector2(190, 190));
            Img(slot, LoadSprite(Art + "Armory/Armory-square-item-slot.png"), Color.white, false).preserveAspect = true;
            var portrait = Center(slot.transform, "Portrait", 152, 152);
            Img(portrait, null, Color.white, false).enabled = false;
            var icon = Center(slot.transform, "Icon", 120, 120);
            Img(icon, null, Color.white, false).enabled = false;
            var tier = Node("Tier", slot.transform);
            Set(Rt(tier), new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-8, -6), new Vector2(84, 44));
            var tierT = Txt(tier, alagard, 34, NameGold, TextAlignmentOptions.TopRight, "", false);

            var title = Line(body.transform, "Title", -256, 52, alagard, 38, NameGold, "Royal order ++", 24);
            var hero = Line(body.transform, "Hero", -312, 40, romulus, 32, Muted, "King", 20);
            var change = Line(body.transform, "Change", -356, 40, romulus, 30, Body, "Royal order + → Royal order ++", 18);

            var effect = Node("Effect", body.transform);
            Stretch(Rt(effect), 26, 410, 26, 50);
            var effT = Txt(effect, romulus, 36, TitleGold, TextAlignmentOptions.Top, "Effect", true, 22, 36);

            var c = root.AddComponent<BlessingCard>();
            var so = new SerializedObject(c);
            Ref(so, "frame", fi);
            Ref(so, "glow", glow);
            Ref(so, "portrait", portrait.GetComponent<Image>());
            Ref(so, "icon", icon.GetComponent<Image>());
            Ref(so, "tierTag", tierT);
            Ref(so, "title", title);
            Ref(so, "hero", hero);
            Ref(so, "change", change);
            Ref(so, "effect", effT);
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, CardPath);
            Object.DestroyImmediate(root);
        }

        // Uma linha de texto centrada, presa no alto da carta.
        private static TMP_Text Line(Transform parent, string name, float y, float h, TMP_FontAsset font, float size, Color color, string text, float min)
        {
            var go = Node(name, parent);
            Set(Rt(go), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, y), new Vector2(296, h));
            return Txt(go, font, size, color, TextAlignmentOptions.Center, text, false, min, size);
        }

        // ------------------------------------------------------------------ a tela

        private static void BuildScreen(BlessingCard card, TMP_FontAsset alagard, TMP_FontAsset romulus)
        {
            var root = new GameObject("BlessingScreen", typeof(RectTransform)) { layer = 5 };
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 11;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;
            scaler.referencePixelsPerUnit = 100;
            root.AddComponent<GraphicRaycaster>();
            var group = root.AddComponent<CanvasGroup>();

            // o fundo: a capela cobre a tela inteira sem achatar (as sobras saem pelos lados)
            var backHolder = Node("Back", root.transform);
            Stretch(Rt(backHolder));
            Img(backHolder, null, Hex("15161B"), true);
            backHolder.AddComponent<RectMask2D>();
            var chapel = Node("Chapel", backHolder.transform);
            Stretch(Rt(chapel));
            var chapelArt = BlessSprite("blessing_chapel") ?? LoadSprite(Art + "background_stone.png");
            Img(chapel, chapelArt, new Color(0.78f, 0.78f, 0.78f, 1f), false);       // a capela 22% mais escura: as cartas e o texto leem melhor
            if (chapelArt != null)
            {
                var fit = chapel.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fit.aspectRatio = chapelArt.rect.width / chapelArt.rect.height;
            }

            var safe = Node("SafeArea", root.transform);
            Stretch(Rt(safe));
            var sa = new SerializedObject(safe.AddComponent<Cubo.View.SafeArea>());
            Float(sa, "minBottom", 0); Float(sa, "sidePadding", 24); Float(sa, "topPadding", 16); Float(sa, "bottomPadding", 16);
            sa.ApplyModifiedPropertiesWithoutUndo();

            var plate = Node("Nameplate", safe.transform);
            Set(Rt(plate), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(380, 96));
            // a placa é a mesma pedra clara com filigrana dourada das cartas (a do armário era de metal e não combinava)
            var plateArt = BlessSprite("blessing_card");
            var pi = Img(plate, plateArt ?? LoadSprite(Art + "Armory/Armory_nameplate.png"), Color.white, false);
            if (plateArt != null) { pi.type = Image.Type.Sliced; pi.fillCenter = true; pi.pixelsPerUnitMultiplier = 0.5f; }
            var title = Node("Title", plate.transform);
            Stretch(Rt(title), 30, 8, 30, 10);
            var titleT = Txt(title, alagard, 36, TitleGold, TextAlignmentOptions.Center, "Blessing", false);

            // a frase, numa faixa escura para ler bem sobre a capela
            var textBack = Node("TextBack", safe.transform);
            Set(Rt(textBack), new Vector2(0.04f, 0.80f), new Vector2(0.96f, 0.89f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Img(textBack, null, new Color(0.05f, 0.035f, 0.025f, 0.9f), false);
            var text = Node("Text", textBack.transform);
            Stretch(Rt(text), 20, 8, 20, 8);
            var textT = Txt(text, romulus, 40, TitleGold, TextAlignmentOptions.Center, "Choose one blessing.", true, 24, 40);
            Stretch(Rt(text), 20, 8, 64, 8);                                   // espaço para o X à direita
            var textToggle = AddTextToggle(textBack, safe, new Vector2(0.96f, 0.89f), alagard);

            var row = Node("Cards", safe.transform);
            Set(Rt(row), new Vector2(0, 0.18f), new Vector2(1, 0.79f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var h = row.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 12; h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = false; h.childControlHeight = false;
            h.childForceExpandWidth = false; h.childForceExpandHeight = false;

            var btn = Node("AcceptButton", safe.transform);
            Set(Rt(btn), new Vector2(0.14f, 0.07f), new Vector2(0.86f, 0.155f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            // o botão também na pedra das cartas, não na madeira do armário
            var btnArt = BlessSprite("blessing_card");
            var bi = Img(btn, btnArt ?? LoadSprite(Art + "Tab/tab.png"), Color.white, true);
            bi.type = Image.Type.Sliced; bi.fillCenter = true; bi.pixelsPerUnitMultiplier = btnArt != null ? 0.4f : 1f;
            var button = btn.AddComponent<Button>();
            button.targetGraphic = bi;
            var colors = button.colors; colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.78f); button.colors = colors;
            var lbl = Node("Label", btn.transform);
            Stretch(Rt(lbl), 40, 12, 40, 12);
            var lblT = Txt(lbl, alagard, 32, TitleGold, TextAlignmentOptions.Center, "Accept blessing", false, 24, 38);

            var screen = root.AddComponent<BlessingScreen>();
            var so = new SerializedObject(screen);
            Ref(so, "screen", group);
            Ref(so, "title", titleT); Ref(so, "text", textT);
            Ref(so, "row", Rt(row)); Ref(so, "cardPrefab", card);
            Ref(so, "action", button); Ref(so, "actionLabel", lblT);
            Ref(so, "textToggle", textToggle);
            so.FindProperty("numberColor").colorValue = Color.white;       // o texto é dourado: os números em branco para saltar
            var icons = so.FindProperty("icons");
            icons.arraySize = 2;
            icons.GetArrayElementAtIndex(0).FindPropertyRelative("id").stringValue = "presa";
            icons.GetArrayElementAtIndex(0).FindPropertyRelative("sprite").objectReferenceValue = LoadSprite(Art + "Armory/Equipments/fang.png");
            icons.GetArrayElementAtIndex(1).FindPropertyRelative("id").stringValue = "brasa";
            icons.GetArrayElementAtIndex(1).FindPropertyRelative("sprite").objectReferenceValue = LoadSprite(Art + "Armory/Equipments/ember.png");
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, ScreenPath);
            Object.DestroyImmediate(root);
        }

        // ------------------------------------------------------------------ cena

        private static void WireScene()
        {
            var sceneObj = EditorSceneManager.GetActiveScene();
            if (sceneObj.path != MainScene) { Debug.LogWarning("Bênção: abra a cena Main e use Cubo ▸ Telas ▸ Montar a Bênção."); return; }
            bool wasDirty = sceneObj.isDirty;
            foreach (var old in Object.FindObjectsByType<BlessingScreen>(FindObjectsInactive.Include)) Object.DestroyImmediate(old.gameObject);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ScreenPath), sceneObj);
            var shop = Object.FindAnyObjectByType<ShopScreen>(FindObjectsInactive.Include);
            if (shop != null && shop.transform.parent == null) inst.transform.SetSiblingIndex(shop.transform.GetSiblingIndex() + 1);

            var screen = inst.GetComponent<BlessingScreen>();
            var so = new SerializedObject(screen);
            Ref(so, "court", Object.FindAnyObjectByType<GodsScreen>(FindObjectsInactive.Include));
            so.ApplyModifiedPropertiesWithoutUndo();

            var boot = Object.FindAnyObjectByType<GameBootstrap>(FindObjectsInactive.Include);
            if (boot != null)
            {
                var bs = new SerializedObject(boot);
                Ref(bs, "blessing", screen);
                bs.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorSceneManager.MarkSceneDirty(sceneObj);
            if (!wasDirty) EditorSceneManager.SaveScene(sceneObj);
            else Debug.LogWarning("Bênção: a cena Main tinha mudanças não salvas; aperte Cmd+S para salvar a bênção junto.");
        }

        // ------------------------------------------------------------------ a arte

        // Os PNGs de Art/Blessing como o resto do jogo; a moldura da carta estica pelo meio (bordas de 14 px).
        private static void ImportArt()
        {
            if (!Directory.Exists(BlessArt)) return;
            AssetDatabase.Refresh();
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { BlessArt.TrimEnd('/') }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(path) is TextureImporter imp)) continue;
                var border = Path.GetFileNameWithoutExtension(path) == "blessing_card" ? new Vector4(14, 14, 14, 14) : Vector4.zero;   // os cantos dourados inteiros
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.spritePixelsPerUnit = 100f;
                imp.filterMode = FilterMode.Point;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.mipmapEnabled = false;
                imp.alphaIsTransparency = true;
                imp.spriteBorder = border;
                var st = new TextureImporterSettings();
                imp.ReadTextureSettings(st);
                st.spriteMeshType = SpriteMeshType.FullRect;
                st.spriteGenerateFallbackPhysicsShape = false;
                imp.SetTextureSettings(st);
                imp.SaveAndReimport();
            }
        }

        private static Sprite BlessSprite(string name) => File.Exists(BlessArt + name + ".png") ? LoadSprite(BlessArt + name + ".png") : null;

        // Abre o prefab, deixa o TextMeshPro e as layouts calcularem e salva (a cópia na cena não grava overrides).
        private static void Settle(string path)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            foreach (var t in root.GetComponentsInChildren<TMP_Text>(true)) { t.color = t.color; t.ForceMeshUpdate(true, true); }
            foreach (var g in root.GetComponentsInChildren<LayoutGroup>(true)) LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)g.transform);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        // ------------------------------------------------------------------ ajudantes

        private static GameObject Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = 5 };
            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }

        private static RectTransform Rt(GameObject go) => (RectTransform)go.transform;

        // O X no canto de cima à direita da faixa de texto (esconde a faixa) e o botão que a traz de volta, no mesmo canto.
        private static TextPanelToggle AddTextToggle(GameObject band, GameObject safe, Vector2 corner, TMP_FontAsset font)
        {
            var close = Node("CloseText", band.transform);
            Set(Rt(close), Vector2.one, Vector2.one, Vector2.one, new Vector2(-6, -6), new Vector2(56, 56));
            var ci = Img(close, null, new Color(0, 0, 0, 0.55f), true);
            var cb = close.AddComponent<Button>(); cb.targetGraphic = ci;
            var cl = Node("Label", close.transform); Stretch(Rt(cl));
            Txt(cl, font, 34, Hex("FFD27A"), TextAlignmentOptions.Center, "X", false);

            var open = Node("ShowText", safe.transform);
            Set(Rt(open), corner, corner, Vector2.one, Vector2.zero, new Vector2(72, 72));
            var oi = Img(open, null, new Color(0.05f, 0.035f, 0.025f, 0.9f), true);
            var ob = open.AddComponent<Button>(); ob.targetGraphic = oi;
            var ol = Node("Label", open.transform); Stretch(Rt(ol));
            Txt(ol, font, 40, Hex("FFD27A"), TextAlignmentOptions.Center, "i", false);
            open.SetActive(false);

            var t = safe.AddComponent<TextPanelToggle>();
            var so = new SerializedObject(t);
            Ref(so, "panel", band); Ref(so, "closeButton", cb); Ref(so, "openButton", ob);
            so.ApplyModifiedPropertiesWithoutUndo();
            return t;
        }

        private static void Set(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = min; rt.anchorMax = max; rt.pivot = pivot; rt.anchoredPosition = pos; rt.sizeDelta = size;
        }

        private static void Size(RectTransform rt, float w, float h) => rt.sizeDelta = new Vector2(w, h);

        private static void Stretch(RectTransform rt, float l = 0, float t = 0, float r = 0, float b = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
        }

        private static GameObject Center(Transform parent, string name, float w, float h)
        {
            var go = Node(name, parent);
            Set(Rt(go), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(w, h));
            return go;
        }

        private static Image Img(GameObject go, Sprite sprite, Color color, bool raycast)
        {
            var img = go.AddComponent<Image>();
            img.sprite = sprite; img.color = color; img.raycastTarget = raycast;
            if (sprite != null && sprite.border != Vector4.zero) { img.type = Image.Type.Sliced; img.fillCenter = true; }
            return img;
        }

        private static TMP_Text Txt(GameObject go, TMP_FontAsset font, float size, Color color, TextAlignmentOptions align,
                                    string text, bool wrap, float autoMin = 0, float autoMax = 0)
        {
            var t = go.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.fontSize = size; t.color = color; t.alignment = align; t.text = text;
            t.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            t.raycastTarget = false;
            if (autoMax > 0) { t.enableAutoSizing = true; t.fontSizeMin = autoMin; t.fontSizeMax = autoMax; }
            return t;
        }

        private static Sprite LoadSprite(string path, string name = null)
        {
            var all = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>();
            var s = name == null ? all.FirstOrDefault() : all.FirstOrDefault(x => x.name == name);
            if (s == null) Debug.LogWarning("Bênção: não achei o sprite " + path);
            return s;
        }

        private static void Ref(SerializedObject so, string field, Object value)
        {
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError("Bênção: campo não encontrado: " + field); return; }
            p.objectReferenceValue = value;
        }

        private static void Float(SerializedObject so, string field, float value)
        {
            var p = so.FindProperty(field);
            if (p != null) p.floatValue = value;
        }

        private static Color Hex(string hex) => ColorUtility.TryParseHtmlString("#" + hex, out var c) ? c : Color.magenta;
    }
}
