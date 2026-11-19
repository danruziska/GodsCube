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
    // Monta a tela da porta (DoorScreen) com a API da Unity, a partir da arte de Art/Door:
    //   o corredor com as duas portas (door_hall), as áreas de toque em cima de cada porta (filhas do cenário, então
    //   acompanham o corte em qualquer tela), o brilho e o escuro de cada porta, o nome no chão embaixo de cada uma;
    //   no alto o caminho dos andares (sem placa: os números e a coroa do chefe), o título e a frase com o X;
    //   embaixo a ficha de ferro (door_plate) e o botão Entrar à direita.
    // Roda sozinho uma vez (prefab ainda não existe, arte presente, fora do Play) e pelo menu Cubo ▸ Telas.
    [InitializeOnLoad]
    public static class DoorScreenBuilder
    {
        private const string ScreenPath = "Assets/Game/View/Prefabs/UI/DoorScreen.prefab";
        private const string MainScene = "Assets/Game/Content/Materials/Scenes/Main.unity";
        private const string Art = "Assets/Game/View/Art/";
        private const string DoorArt = Art + "Door/";
        private const string LayoutKey = "Cubo.Door.Layout.v1";

        private static readonly Color Gold = Hex("FFD27A"), Body = Hex("DBDBE6"), Warm = Hex("FFB25A"), Blood = Hex("FF5A4A");

        static DoorScreenBuilder()
        {
            EditorApplication.delayCall += RunPending;
            EditorApplication.playModeStateChanged += st => { if (st == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += RunPending; };
        }

        private static void RunPending()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorSceneManager.GetActiveScene().path != MainScene || !File.Exists(DoorArt + "door_hall.png")) return;
            if (!File.Exists(ScreenPath) || !EditorPrefs.GetBool(LayoutKey, false)) { EditorPrefs.SetBool(LayoutKey, true); Build(); }
        }

        [MenuItem("Cubo/Telas/Montar a Porta (DoorScreen)")]
        public static void BuildFromMenu()
        {
            if (EditorApplication.isPlaying) { EditorUtility.DisplayDialog("Cubo", "Saia do Play antes de montar a porta.", "OK"); return; }
            if (File.Exists(ScreenPath) && !EditorUtility.DisplayDialog("Cubo", "O prefab DoorScreen já existe. Montar de novo apaga o que você mudou nele.", "Montar de novo", "Cancelar")) return;
            Build();
        }

        public static void Build()
        {
            ImportArt();
            var alagard = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Game/View/Fonts/alagard.asset");
            var romulus = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Game/View/Fonts/Romulus.asset");
            var plate = LoadSprite(DoorArt + "door_plate.png");

            var root = new GameObject("DoorScreen", typeof(RectTransform)) { layer = 5 };
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

            // ---- o corredor, cobrindo a tela sem achatar; as portas vão dentro dele
            var back = Node("Back", root.transform);
            Stretch(Rt(back));
            Img(back, null, Hex("0B0B10"), true);
            back.AddComponent<RectMask2D>();
            var hallGo = Node("Hall", back.transform);
            Stretch(Rt(hallGo));
            var hallArt = LoadSprite(DoorArt + "door_hall.png");
            Img(hallGo, hallArt, Color.white, false);
            if (hallArt != null)
            {
                var fit = hallGo.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fit.aspectRatio = hallArt.rect.width / hallArt.rect.height;
            }
            var glowArt = LoadSprite(DoorArt + "door_glow.png");
            var normal = MakeDoor(hallGo.transform, "NormalDoor", new Vector2(0.18f, 0.39f), new Vector2(0.415f, 0.71f), Warm, glowArt, plate, alagard);
            var elite = MakeDoor(hallGo.transform, "EliteDoor", new Vector2(0.625f, 0.40f), new Vector2(0.865f, 0.75f), Blood, glowArt, plate, alagard);

            var safe = Node("SafeArea", root.transform);
            Stretch(Rt(safe));
            var sa = new SerializedObject(safe.AddComponent<Cubo.View.SafeArea>());
            Float(sa, "minBottom", 0); Float(sa, "sidePadding", 24); Float(sa, "topPadding", 16); Float(sa, "bottomPadding", 16);
            sa.ApplyModifiedPropertiesWithoutUndo();

            // ---- no alto: o caminho dos andares (uma corrente com as marcas), o título e a frase
            var track = Node("FloorTrack", safe.transform);
            Set(Rt(track), new Vector2(0.06f, 0.905f), new Vector2(0.94f, 0.975f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var chain = Node("Chain", track.transform);
            Set(Rt(chain), new Vector2(0.04f, 0.5f), new Vector2(0.96f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0, 8));
            Img(chain, null, Hex("6B5432"), false);
            var pips = Node("Pips", track.transform);
            Stretch(Rt(pips));
            var h = pips.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 24; h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = false; h.childControlHeight = false; h.childForceExpandWidth = true; h.childForceExpandHeight = false;
            var pip = Node("Pip", pips.transform);
            Size(Rt(pip), 92, 92);
            Img(pip, LoadSprite(Art + "Armory/Armory-square-item-slot.png"), Color.white, false).preserveAspect = true;
            var num = Node("Number", pip.transform); Stretch(Rt(num), 0, 0, 0, 6);
            Txt(num, alagard, 44, Gold, TextAlignmentOptions.Center, "1", false);
            var crown = Node("Crown", pip.transform);
            Set(Rt(crown), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60, 60));
            Img(crown, LoadSprite(Art + "crown_icon.png"), Color.white, false).preserveAspect = true;
            pip.SetActive(false);

            var title = Node("Title", safe.transform);
            Set(Rt(title), new Vector2(0.04f, 0.845f), new Vector2(0.96f, 0.9f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var titleT = Txt(title, alagard, 56, Gold, TextAlignmentOptions.Center, "Floor 1 of 6", false, 30, 56);

            var band = Node("TextBand", safe.transform);
            Set(Rt(band), new Vector2(0.04f, 0.775f), new Vector2(0.96f, 0.84f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Img(band, null, new Color(0.05f, 0.035f, 0.025f, 0.9f), false);
            var text = Node("Text", band.transform); Stretch(Rt(text), 24, 6, 64, 6);
            var textT = Txt(text, romulus, 30, Gold, TextAlignmentOptions.Center, "The elite rival is stronger and pays better.", true, 18, 30);
            var toggle = AddTextToggle(band, safe, new Vector2(0.96f, 0.84f), alagard);

            // ---- embaixo: a ficha de ferro (a dica quando nenhuma porta foi tocada)
            var dossier = Node("Dossier", safe.transform);
            Set(Rt(dossier), new Vector2(0.04f, 0.10f), new Vector2(0.96f, 0.325f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Img(dossier, null, new Color(0.04f, 0.035f, 0.045f, 0.9f), false);
            var dFrame = Node("Frame", dossier.transform); Stretch(Rt(dFrame), -6, -6, -6, -6);
            Sliced(Img(dFrame, plate, Color.white, false), 0.45f);

            var sheet = Node("Sheet", dossier.transform); Stretch(Rt(sheet), 56, 40, 56, 40);
            var v = sheet.AddComponent<VerticalLayoutGroup>();
            v.spacing = 2; v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            var nameGo = Node("Name", sheet.transform); Pref(nameGo, 54);
            var nameT = Txt(nameGo, alagard, 42, Gold, TextAlignmentOptions.Center, "Elite rival", false, 26, 42);
            var tagGo = Node("Tagline", sheet.transform); Pref(tagGo, 70);
            var tagT = Txt(tagGo, romulus, 26, Hex("A9A9B8"), TextAlignmentOptions.Center, "Chains, a skull, a red glow.", true, 16, 26);
            var stat = Node("Stat", sheet.transform); Pref(stat, 42);
            var lbl = Node("Label", stat.transform); Set(Rt(lbl), new Vector2(0, 0), new Vector2(0.6f, 1), new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
            Txt(lbl, romulus, 30, Body, TextAlignmentOptions.Left, "Rival health", false, 18, 30);
            var val = Node("Value", stat.transform); Set(Rt(val), new Vector2(0.6f, 0), new Vector2(1, 1), new Vector2(1, 0.5f), Vector2.zero, Vector2.zero);
            Txt(val, alagard, 32, Body, TextAlignmentOptions.Right, "262", false, 18, 32);
            var dots = Node("Dots", stat.transform); Set(Rt(dots), new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(0, 2));
            Img(dots, null, new Color(1f, 0.82f, 0.48f, 0.18f), false);         // a linha fina sob cada número, como num livro-caixa
            stat.SetActive(false);
            sheet.SetActive(false);

            var hint = Node("Hint", dossier.transform); Stretch(Rt(hint), 60, 40, 60, 40);
            var hintT = Txt(hint, romulus, 32, Body, TextAlignmentOptions.Center, "Tap a door to see who waits behind it.", true, 18, 32);

            // ---- o botão Entrar, embaixo à direita (a mesma ficha de ferro)
            var btn = Node("EnterButton", safe.transform);
            Set(Rt(btn), new Vector2(0.50f, 0.02f), new Vector2(0.96f, 0.088f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var fill = Img(btn, null, new Color(0.09f, 0.08f, 0.1f, 0.95f), true);
            var bFrame = Node("Frame", btn.transform); Stretch(Rt(bFrame), -6, -6, -6, -6);
            Sliced(Img(bFrame, plate, Color.white, false), 0.45f);
            var button = btn.AddComponent<Button>();
            button.targetGraphic = fill;
            var colors = button.colors; colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.78f); button.colors = colors;
            var bl = Node("Label", btn.transform); Stretch(Rt(bl), 40, 10, 40, 10);
            var blT = Txt(bl, alagard, 40, Gold, TextAlignmentOptions.Center, "Enter", false, 22, 40);

            // ---- o componente
            var screen = root.AddComponent<DoorScreen>();
            var so = new SerializedObject(screen);
            Ref(so, "screen", group); Ref(so, "title", titleT); Ref(so, "text", textT); Ref(so, "textToggle", toggle);
            Ref(so, "track", Rt(pips)); Ref(so, "pipTemplate", Rt(pip));
            Ref(so, "sheet", sheet); Ref(so, "sheetName", nameT); Ref(so, "tagline", tagT); Ref(so, "statTemplate", Rt(stat)); Ref(so, "hint", hintT);
            Ref(so, "action", button); Ref(so, "actionLabel", blT);
            var dp = so.FindProperty("doors");
            dp.arraySize = 2;
            SetDoor(dp.GetArrayElementAtIndex(0), "normal", normal, Warm);
            SetDoor(dp.GetArrayElementAtIndex(1), "elite", elite, Blood);
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, ScreenPath);
            Object.DestroyImmediate(root);
            Settle(ScreenPath);
            WireScene();
            Debug.Log("Porta montada: " + ScreenPath + ".");
        }

        private struct DoorParts { public Button spot; public Image glow, shade; public TMP_Text label; }

        // Uma porta: a área de toque em cima da arte, o brilho (maior que a porta, pulsa), o escuro e o nome no chão.
        private static DoorParts MakeDoor(Transform hall, string name, Vector2 min, Vector2 max, Color color, Sprite glowArt, Sprite plate, TMP_FontAsset font)
        {
            var spot = Node(name, hall);
            Set(Rt(spot), min, max, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var hit = Img(spot, null, new Color(1, 1, 1, 0), true);
            var b = spot.AddComponent<Button>();
            b.targetGraphic = hit; b.transition = Selectable.Transition.None;

            var glow = Node("Glow", spot.transform); Stretch(Rt(glow), -70, -70, -70, -50);
            var c = color; c.a = 0.45f;
            var gi = Img(glow, glowArt, c, false);
            glow.SetActive(false);
            var shade = Node("Shade", spot.transform); Stretch(Rt(shade), 6, 6, 6, 6);
            var si = Img(shade, null, new Color(0, 0, 0, 0.5f), false);
            shade.SetActive(false);

            // o nome numa plaquinha de ferro no chão, logo embaixo da porta
            var tag = Node("Name", spot.transform);
            Set(Rt(tag), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(0, -14), new Vector2(300, 78));
            Img(tag, null, new Color(0.04f, 0.035f, 0.045f, 0.9f), false);
            var tf = Node("Frame", tag.transform); Stretch(Rt(tf), -4, -4, -4, -4);
            Sliced(Img(tf, plate, Color.white, false), 0.6f);
            var tl = Node("Label", tag.transform); Stretch(Rt(tl), 26, 8, 26, 8);
            var label = Txt(tl, font, 32, color, TextAlignmentOptions.Center, name, false, 18, 32);
            return new DoorParts { spot = b, glow = gi, shade = si, label = label };
        }

        private static void SetDoor(SerializedProperty e, string key, DoorParts d, Color color)
        {
            e.FindPropertyRelative("key").stringValue = key;
            e.FindPropertyRelative("spot").objectReferenceValue = d.spot;
            e.FindPropertyRelative("glow").objectReferenceValue = d.glow;
            e.FindPropertyRelative("shade").objectReferenceValue = d.shade;
            e.FindPropertyRelative("label").objectReferenceValue = d.label;
            e.FindPropertyRelative("color").colorValue = color;
        }

        // O X no canto da faixa de texto e o botão que a traz de volta.
        private static TextPanelToggle AddTextToggle(GameObject band, GameObject safe, Vector2 corner, TMP_FontAsset font)
        {
            var close = Node("CloseText", band.transform);
            Set(Rt(close), Vector2.one, Vector2.one, Vector2.one, new Vector2(-6, -6), new Vector2(56, 56));
            var ci = Img(close, null, new Color(0, 0, 0, 0.55f), true);
            var cb = close.AddComponent<Button>(); cb.targetGraphic = ci;
            var cl = Node("Label", close.transform); Stretch(Rt(cl));
            Txt(cl, font, 34, Gold, TextAlignmentOptions.Center, "X", false);
            var open = Node("ShowText", safe.transform);
            Set(Rt(open), corner, corner, Vector2.one, Vector2.zero, new Vector2(72, 72));
            var oi = Img(open, null, new Color(0.05f, 0.035f, 0.025f, 0.9f), true);
            var ob = open.AddComponent<Button>(); ob.targetGraphic = oi;
            var ol = Node("Label", open.transform); Stretch(Rt(ol));
            Txt(ol, font, 40, Gold, TextAlignmentOptions.Center, "i", false);
            open.SetActive(false);
            var t = safe.AddComponent<TextPanelToggle>();
            var so = new SerializedObject(t);
            Ref(so, "panel", band); Ref(so, "closeButton", cb); Ref(so, "openButton", ob);
            so.ApplyModifiedPropertiesWithoutUndo();
            return t;
        }

        // ------------------------------------------------------------------ cena

        private static void WireScene()
        {
            var sceneObj = EditorSceneManager.GetActiveScene();
            if (sceneObj.path != MainScene) { Debug.LogWarning("Porta: abra a cena Main e use Cubo ▸ Telas ▸ Montar a Porta."); return; }
            bool wasDirty = sceneObj.isDirty;
            foreach (var old in Object.FindObjectsByType<DoorScreen>(FindObjectsInactive.Include)) Object.DestroyImmediate(old.gameObject);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ScreenPath), sceneObj);
            var anchor = Object.FindAnyObjectByType<BlessingScreen>(FindObjectsInactive.Include);
            if (anchor != null && anchor.transform.parent == null) inst.transform.SetSiblingIndex(anchor.transform.GetSiblingIndex() + 1);
            var boot = Object.FindAnyObjectByType<GameBootstrap>(FindObjectsInactive.Include);
            if (boot != null)
            {
                var bs = new SerializedObject(boot);
                Ref(bs, "door", inst.GetComponent<DoorScreen>());
                bs.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorSceneManager.MarkSceneDirty(sceneObj);
            if (!wasDirty) EditorSceneManager.SaveScene(sceneObj);
            else Debug.LogWarning("Porta: a cena Main tinha mudanças não salvas; aperte Cmd+S para salvar a porta junto.");
        }

        // ------------------------------------------------------------------ a arte

        private static void ImportArt()
        {
            if (!Directory.Exists(DoorArt)) return;
            AssetDatabase.Refresh();
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { DoorArt.TrimEnd('/') }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(path) is TextureImporter imp)) continue;
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.spritePixelsPerUnit = 100f;
                imp.filterMode = FilterMode.Point;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.mipmapEnabled = false;
                imp.alphaIsTransparency = true;
                imp.spriteBorder = Path.GetFileNameWithoutExtension(path) == "door_plate" ? new Vector4(18, 18, 18, 18) : Vector4.zero;
                var st = new TextureImporterSettings();
                imp.ReadTextureSettings(st);
                st.spriteMeshType = SpriteMeshType.FullRect;
                st.spriteGenerateFallbackPhysicsShape = false;
                imp.SetTextureSettings(st);
                imp.SaveAndReimport();
            }
        }

        private static void Settle(string path)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            foreach (var t in root.GetComponentsInChildren<TMP_Text>(true)) { t.color = t.color; t.ForceMeshUpdate(true, true); }
            foreach (var g in root.GetComponentsInChildren<LayoutGroup>(true)) LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)g.transform);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        // ------------------------------------------------------------------ ajudantes

        private static void Sliced(Image img, float density)
        {
            if (img.sprite == null) return;
            img.type = Image.Type.Sliced; img.fillCenter = true; img.pixelsPerUnitMultiplier = density;
        }

        private static void Pref(GameObject go, float h)
        {
            var le = go.AddComponent<LayoutElement>(); le.preferredHeight = h; le.minHeight = h * 0.7f;
        }

        private static GameObject Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = 5 };
            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }

        private static RectTransform Rt(GameObject go) => (RectTransform)go.transform;

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

        private static Image Img(GameObject go, Sprite sprite, Color color, bool raycast)
        {
            var img = go.AddComponent<Image>();
            img.sprite = sprite; img.color = color; img.raycastTarget = raycast;
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

        private static Sprite LoadSprite(string path)
        {
            var s = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
            if (s == null) Debug.LogWarning("Porta: não achei o sprite " + path);
            return s;
        }

        private static void Ref(SerializedObject so, string field, Object value)
        {
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError("Porta: campo não encontrado: " + field); return; }
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
