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
    // Monta as três telas de evento com a API da Unity, cada uma no seu cenário (Art/Events):
    //   IdolEvent:  a caverna do Ídolo; três tábuas de pedra (o sangue de cada herói, com o rosto) e uma tábua larga (a ira);
    //   CampEvent:  o acampamento à noite; o mapa de pergaminho com o relatório do batedor e três placas de madeira;
    //   VowEvent:   a capela em ruínas; três estandartes vermelhos pendurados (os votos).
    // O que é igual nas três (o "sistema"): a placa do título, a faixa da história, o botão de confirmar embaixo à direita
    // e o de ir embora à esquerda, as fontes. O prefab da opção (EventOptionCard) é um só; cada fileira dá a moldura e as
    // cores. Roda sozinho uma vez (prefabs ainda não existem, a arte já está lá, fora do Play) e pelo menu Cubo ▸ Telas.
    [InitializeOnLoad]
    public static class EventScreenBuilder
    {
        private const string Prefabs = "Assets/Game/View/Prefabs/UI/";
        private const string CardPath = Prefabs + "EventOptionCard.prefab";
        private static readonly string[] ScreenPaths = { Prefabs + "IdolEvent.prefab", Prefabs + "CampEvent.prefab", Prefabs + "VowEvent.prefab" };
        private const string MainScene = "Assets/Game/Content/Materials/Scenes/Main.unity";
        private const string Art = "Assets/Game/View/Art/";
        private const string EvArt = Art + "Events/";
        private const string LayoutKey = "Cubo.Events.Layout.v8";

        private static readonly Color TitleGold = Hex("FFD27A"), Body = Hex("DBDBE6"), Ink = Hex("3A2A1A");

        static EventScreenBuilder()
        {
            EditorApplication.delayCall += RunPending;
            EditorApplication.playModeStateChanged += st => { if (st == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += RunPending; };
        }

        private static void RunPending()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorSceneManager.GetActiveScene().path != MainScene || !File.Exists(EvArt + "event_idol_cave.png")) return;
            // monta uma vez; e de novo uma vez a cada versão nova do layout (v2: textos maiores, placa de madeira no acampamento)
            if (!File.Exists(ScreenPaths[0]) || !EditorPrefs.GetBool(LayoutKey, false)) { EditorPrefs.SetBool(LayoutKey, true); Build(); }
        }

        [MenuItem("Cubo/Telas/Montar os Eventos (Ídolo, Acampamento, Juramento)")]
        public static void BuildFromMenu()
        {
            if (EditorApplication.isPlaying) { EditorUtility.DisplayDialog("Cubo", "Saia do Play antes de montar os eventos.", "OK"); return; }
            if (File.Exists(ScreenPaths[0]) && !EditorUtility.DisplayDialog("Cubo", "As telas de evento já existem. Montar de novo apaga o que você mudou nelas.", "Montar de novo", "Cancelar")) return;
            Build();
        }

        public static void Build()
        {
            ImportArt();
            var alagard = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Game/View/Fonts/alagard.asset");
            var romulus = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Game/View/Fonts/Romulus.asset");
            BuildCard(alagard, romulus);
            Settle(CardPath);
            var card = AssetDatabase.LoadAssetAtPath<GameObject>(CardPath).GetComponent<EventOptionCard>();

            // ---- o Ídolo
            _buttonArt = EvSprite("event_tablet");                              // o Ídolo: botões na pedra das tábuas
            BuildScreen(ScreenPaths[0], "IdolEvent", "idolo", "event_idol_cave", card, alagard, romulus, EvSprite("event_tablet"), (safe, rows) =>
            {
                var tablet = EvSprite("event_tablet");
                var blood = Row(safe, "BloodRow", 0.31f, 0.58f, 12);
                var wrath = Row(safe, "WrathRow", 0.17f, 0.30f, 0);
                var purple = Hex("C9A7FF");
                rows.Add(OptionRow("blood", blood, new Vector2(336, 480), tablet, 0.29f, purple, Body, new Color(0.7f, 0.4f, 1f, 0.4f)));
                rows.Add(OptionRow("wrath", wrath, new Vector2(1000, 260), tablet, 0.29f, purple, Body, new Color(0.45f, 1f, 0.45f, 0.35f)));
                return null;
            });

            // ---- o Acampamento
            _buttonArt = null;                                                  // o Acampamento: a madeira de sempre
            BuildScreen(ScreenPaths[1], "CampEvent", "acampamento", "event_scout_camp", card, alagard, romulus, EvSprite("event_sign"), (safe, rows) =>
            {
                var map = Node("InfoPanel", safe.transform);
                Set(Rt(map), new Vector2(0.04f, 0.53f), new Vector2(0.96f, 0.81f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                var mi = Img(map, EvSprite("event_map"), Color.white, false);
                if (mi.sprite != null) { mi.type = Image.Type.Sliced; mi.fillCenter = true; mi.pixelsPerUnitMultiplier = 0.29f; }
                var txt = Node("Info", map.transform);
                Stretch(Rt(txt), 80, 56, 56, 64);
                var info = Txt(txt, romulus, 34, Ink, TextAlignmentOptions.Center, "Next: floor 3 of 6", true, 20, 34);

                var signs = Row(safe, "OrderRow", 0.17f, 0.51f, 12);
                var sign = EvSprite("event_sign");
                var fire = new Color(1f, 0.6f, 0.25f, 0.4f);
                foreach (var g in new[] { "ambush", "mark", "raid" })
                    rows.Add(OptionRow(g, signs, new Vector2(336, 600), sign, 0.29f, TitleGold, Hex("F0E6D8"), fire));
                return (info, map);
            });

            // ---- o Juramento
            _buttonArt = EvSprite("event_vow_plate");                           // o Juramento: botões no pano dos estandartes
            BuildScreen(ScreenPaths[2], "VowEvent", "juramento", "event_vow_ruins", card, alagard, romulus, EvSprite("event_vow_plate"), (safe, rows) =>
            {
                var banners = Row(safe, "VowRow", 0.16f, 0.52f, 12);
                var vow = OptionRow("vow", banners, new Vector2(336, 640), EvSprite("event_banner"), 0.29f, Hex("FFE08A"), Hex("F5E9E9"),
                                    new Color(1f, 0.85f, 0.45f, 0.4f));
                vow.padding = new Vector4(64, 92, 64, 140);                          // longe do friso dourado, da vara e da ponta em V
                rows.Add(vow);
                return null;
            });

            WireScene();
            Debug.Log("Eventos montados: EventOptionCard, IdolEvent, CampEvent e VowEvent.");
        }

        // ------------------------------------------------------------------ a opção

        private static void BuildCard(TMP_FontAsset alagard, TMP_FontAsset romulus)
        {
            var root = Node("EventOptionCard", null);
            Size(Rt(root), 300, 440);
            Img(root, null, new Color(1, 1, 1, 0), true);

            var body = Node("Body", root.transform);
            Stretch(Rt(body));
            var glow = Node("Glow", body.transform);
            Stretch(Rt(glow), -12, -12, -12, -12);
            Img(glow, null, new Color(1f, 0.85f, 0.45f, 0.4f), false);
            glow.SetActive(false);
            var frame = Node("Frame", body.transform);
            Stretch(Rt(frame));
            var fi = Img(frame, null, Hex("2B2F38"), false);

            var content = Node("Content", body.transform);
            Stretch(Rt(content), 34, 44, 34, 56);
            var v = content.AddComponent<VerticalLayoutGroup>();
            v.spacing = 8; v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = true; v.childControlHeight = true;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;

            var slot = Node("PortraitSlot", content.transform);
            var sle = slot.AddComponent<LayoutElement>(); sle.preferredHeight = 128; sle.minHeight = 128;
            var ring = Node("Ring", slot.transform);
            Set(Rt(ring), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(128, 128));
            Img(ring, LoadSprite(Art + "Armory/Armory-square-item-slot.png"), Color.white, false).preserveAspect = true;
            var portrait = Node("Portrait", ring.transform);
            Set(Rt(portrait), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(104, 104));
            Img(portrait, null, Color.white, false).enabled = false;
            slot.SetActive(false);

            var title = Node("Title", content.transform);
            var tle = title.AddComponent<LayoutElement>(); tle.preferredHeight = 72; tle.minHeight = 40;
            var titleT = Txt(title, alagard, 36, TitleGold, TextAlignmentOptions.Center, "Option", true, 22, 36);
            var text = Node("Text", content.transform);
            var xle = text.AddComponent<LayoutElement>(); xle.flexibleHeight = 1; xle.minHeight = 40;
            var textT = Txt(text, romulus, 30, Body, TextAlignmentOptions.Top, "What it does", true, 18, 30);

            var c = root.AddComponent<EventOptionCard>();
            var so = new SerializedObject(c);
            Ref(so, "frame", fi); Ref(so, "glow", glow); Ref(so, "portraitSlot", slot);
            Ref(so, "portrait", portrait.GetComponent<Image>()); Ref(so, "title", titleT); Ref(so, "text", textT);
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, CardPath);
            Object.DestroyImmediate(root);
        }

        // ------------------------------------------------------------------ a tela

        private struct RowSpec
        {
            public string group; public RectTransform row; public Vector2 size; public Sprite frame; public float density;
            public Color title, text, glow;
            public Vector4 padding;
        }

        private static RowSpec OptionRow(string g, GameObject row, Vector2 size, Sprite frame, float density, Color title, Color text, Color glow) =>
            new RowSpec { group = g, row = Rt(row), size = size, frame = frame, density = density, title = title, text = text, glow = glow };

        private static GameObject Row(GameObject safe, string name, float yMin, float yMax, float spacing)
        {
            var choices = safe.transform.Find("Choices").gameObject;
            var row = Node(name, choices.transform);
            Set(Rt(row), new Vector2(0, yMin), new Vector2(1, yMax), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var h = row.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing; h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = false; h.childControlHeight = false;
            h.childForceExpandWidth = false; h.childForceExpandHeight = false;
            return row;
        }

        // `layout` monta o que é próprio do evento dentro da SafeArea e devolve (o texto do relatório, o painel dele) ou null.
        private static void BuildScreen(string path, string name, string eventId, string background, EventOptionCard card,
                                        TMP_FontAsset alagard, TMP_FontAsset romulus, Sprite plaque,
                                        System.Func<GameObject, System.Collections.Generic.List<RowSpec>, (TMP_Text, GameObject)?> layout)
        {
            var root = new GameObject(name, typeof(RectTransform)) { layer = 5 };
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

            // o cenário cobre a tela inteira sem achatar
            var back = Node("Back", root.transform);
            Stretch(Rt(back));
            Img(back, null, Hex("0E0F13"), true);
            back.AddComponent<RectMask2D>();
            var scene = Node("Scene", back.transform);
            Stretch(Rt(scene));
            var art = EvSprite(background);
            var sceneImg = Img(scene, art, Color.white, false);
            if (art != null)
            {
                var fit = scene.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fit.aspectRatio = art.rect.width / art.rect.height;
            }

            var safe = Node("SafeArea", root.transform);
            Stretch(Rt(safe));
            var sa = new SerializedObject(safe.AddComponent<Cubo.View.SafeArea>());
            Float(sa, "minBottom", 0); Float(sa, "sidePadding", 24); Float(sa, "topPadding", 16); Float(sa, "bottomPadding", 16);
            sa.ApplyModifiedPropertiesWithoutUndo();

            var plate = Node("Nameplate", safe.transform);
            var title = Node("Title", plate.transform);
            if (plaque != null)
            {
                // a placa no material do próprio cenário (a madeira das placas do acampamento)
                Set(Rt(plate), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(460, 104));
                var pi = Img(plate, plaque, Color.white, false);
                pi.type = Image.Type.Sliced; pi.fillCenter = true; pi.pixelsPerUnitMultiplier = 0.5f;
                Stretch(Rt(title), 34, 10, 34, 12);
            }
            else
            {
                Set(Rt(plate), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(380, 72));
                Img(plate, LoadSprite(Art + "Armory/Armory_nameplate.png"), Color.white, false);
                Stretch(Rt(title), 16, 0, 16, 4);
            }
            var titleT = Txt(title, alagard, 40, TitleGold, TextAlignmentOptions.Center, "Event", false, 22, 40);

            // a história, numa faixa escura logo abaixo da placa; o aviso (voto) numa linha acima dela
            var storyBack = Node("StoryBand", safe.transform);
            Set(Rt(storyBack), new Vector2(0.04f, 0.82f), new Vector2(0.96f, 0.93f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Img(storyBack, null, new Color(0.05f, 0.035f, 0.025f, 0.9f), false);
            var story = Node("Story", storyBack.transform);
            Stretch(Rt(story), 24, 8, 24, 8);
            var storyT = Txt(story, romulus, 32, TitleGold, TextAlignmentOptions.Center, "Story", true, 20, 32);   // o mesmo dourado da frase da bênção
            var notice = Node("Notice", safe.transform);
            Set(Rt(notice), new Vector2(0.04f, 0.93f), new Vector2(0.96f, 0.955f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var noticeT = Txt(notice, romulus, 28, Hex("8FE07A"), TextAlignmentOptions.Center, "", false, 18, 28);
            notice.SetActive(false);
            Stretch(Rt(story), 24, 8, 64, 8);                                  // espaço para o X à direita
            var textToggle = AddTextToggle(storyBack, safe, new Vector2(0.96f, 0.93f), alagard);

            var choices = Node("Choices", safe.transform);
            Stretch(Rt(choices));
            var rows = new System.Collections.Generic.List<RowSpec>();
            var extra = layout(safe, rows);

            // o resultado: uma faixa escura no meio da tela
            var resultPanel = Node("ResultPanel", safe.transform);
            Set(Rt(resultPanel), new Vector2(0.06f, 0.30f), new Vector2(0.94f, 0.50f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Img(resultPanel, null, new Color(0.05f, 0.04f, 0.035f, 0.85f), false);
            var result = Node("Result", resultPanel.transform);
            Stretch(Rt(result), 30, 20, 30, 20);
            var resultT = Txt(result, romulus, 36, Body, TextAlignmentOptions.Center, "Result", true, 22, 36);
            resultPanel.SetActive(false);

            var (act, actLabel) = MakeButton(safe.transform, "ActButton", new Vector2(0.40f, 0.05f), new Vector2(0.96f, 0.13f), "Confirm", 30, alagard);
            var (leave, leaveLabel) = MakeButton(choices.transform, "LeaveButton", new Vector2(0.04f, 0.05f), new Vector2(0.36f, 0.13f), "Leave", 26, alagard);

            var screen = root.AddComponent<EventScreen>();
            var so = new SerializedObject(screen);
            so.FindProperty("eventId").stringValue = eventId;
            Ref(so, "screen", group);
            Ref(so, "title", titleT); Ref(so, "story", storyT); Ref(so, "notice", noticeT);
            if (extra.HasValue) { Ref(so, "info", extra.Value.Item1); Ref(so, "infoPanel", extra.Value.Item2); }
            Ref(so, "choices", choices); Ref(so, "resultPanel", resultPanel); Ref(so, "result", resultT);
            Ref(so, "cardPrefab", card);
            Ref(so, "scene", sceneImg);
            Ref(so, "textToggle", textToggle);
            // o Ídolo: uma versão do cenário para cada ídolo (event_idol_cave_rei.png etc.), se a arte existe
            var ids = new System.Collections.Generic.List<(string, Sprite)>();
            foreach (var id in new[] { "rei", "rainha", "torre" })
                if (EvSprite(background + "_" + id) is Sprite vs) ids.Add((id, vs));
            var vp = so.FindProperty("variants");
            vp.arraySize = ids.Count;
            for (int i = 0; i < ids.Count; i++)
            {
                vp.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue = ids[i].Item1;
                vp.GetArrayElementAtIndex(i).FindPropertyRelative("sprite").objectReferenceValue = ids[i].Item2;
            }
            Ref(so, "action", act); Ref(so, "actionLabel", actLabel);
            Ref(so, "leave", leave); Ref(so, "leaveLabel", leaveLabel);
            var rp = so.FindProperty("rows");
            rp.arraySize = rows.Count;
            for (int i = 0; i < rows.Count; i++)
            {
                var e = rp.GetArrayElementAtIndex(i); var r = rows[i];
                e.FindPropertyRelative("group").stringValue = r.group;
                e.FindPropertyRelative("row").objectReferenceValue = r.row;
                e.FindPropertyRelative("cardSize").vector2Value = r.size;
                e.FindPropertyRelative("frame").objectReferenceValue = r.frame;
                e.FindPropertyRelative("density").floatValue = r.density;
                e.FindPropertyRelative("titleColor").colorValue = r.title;
                e.FindPropertyRelative("textColor").colorValue = r.text;
                e.FindPropertyRelative("glowColor").colorValue = r.glow;
                e.FindPropertyRelative("padding").vector4Value = r.padding;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Settle(path);
        }

        // ------------------------------------------------------------------ cena

        private static void WireScene()
        {
            var sceneObj = EditorSceneManager.GetActiveScene();
            if (sceneObj.path != MainScene) { Debug.LogWarning("Eventos: abra a cena Main e use Cubo ▸ Telas ▸ Montar os Eventos."); return; }
            bool wasDirty = sceneObj.isDirty;
            foreach (var old in Object.FindObjectsByType<EventScreen>(FindObjectsInactive.Include)) Object.DestroyImmediate(old.gameObject);
            var court = Object.FindAnyObjectByType<GodsScreen>(FindObjectsInactive.Include);
            var anchor = Object.FindAnyObjectByType<BlessingScreen>(FindObjectsInactive.Include);
            int index = anchor != null && anchor.transform.parent == null ? anchor.transform.GetSiblingIndex() + 1 : -1;
            var screens = new EventScreen[ScreenPaths.Length];
            for (int i = 0; i < ScreenPaths.Length; i++)
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ScreenPaths[i]), sceneObj);
                if (index >= 0) inst.transform.SetSiblingIndex(index + i);
                screens[i] = inst.GetComponent<EventScreen>();
                var so = new SerializedObject(screens[i]);
                Ref(so, "court", court);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            var boot = Object.FindAnyObjectByType<GameBootstrap>(FindObjectsInactive.Include);
            if (boot != null)
            {
                var bs = new SerializedObject(boot);
                var arr = bs.FindProperty("events");
                arr.arraySize = screens.Length;
                for (int i = 0; i < screens.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = screens[i];
                bs.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorSceneManager.MarkSceneDirty(sceneObj);
            if (!wasDirty) EditorSceneManager.SaveScene(sceneObj);
            else Debug.LogWarning("Eventos: a cena Main tinha mudanças não salvas; aperte Cmd+S para salvar os eventos junto.");
        }

        // ------------------------------------------------------------------ a arte

        // As bordas (esquerda, baixo, direita, cima) das molduras que esticam, em pixels da arte.
        private static Vector4 BorderOf(string name)
        {
            switch (name)
            {
                case "event_tablet": return new Vector4(12, 12, 12, 12);
                case "event_map": return new Vector4(24, 24, 12, 14);       // o compasso fica inteiro no canto de baixo à esquerda
                case "event_sign": return new Vector4(14, 14, 14, 14);
                case "event_banner": return new Vector4(8, 26, 8, 16);      // a ponta em V embaixo e a vara em cima ficam inteiras
                case "event_vow_plate": return new Vector4(8, 8, 8, 8);     // o pano vermelho com o friso dourado (placa e botões do Juramento)
                default: return Vector4.zero;
            }
        }

        private static void ImportArt()
        {
            if (!Directory.Exists(EvArt)) return;
            AssetDatabase.Refresh();
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { EvArt.TrimEnd('/') }))
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
                imp.spriteBorder = BorderOf(Path.GetFileNameWithoutExtension(path));
                var st = new TextureImporterSettings();
                imp.ReadTextureSettings(st);
                st.spriteMeshType = SpriteMeshType.FullRect;
                st.spriteGenerateFallbackPhysicsShape = false;
                imp.SetTextureSettings(st);
                imp.SaveAndReimport();
            }
        }

        private static Sprite EvSprite(string name) => File.Exists(EvArt + name + ".png") ? LoadSprite(EvArt + name + ".png") : null;

        private static void Settle(string path)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            foreach (var t in root.GetComponentsInChildren<TMP_Text>(true)) { t.color = t.color; t.ForceMeshUpdate(true, true); }
            foreach (var g in root.GetComponentsInChildren<LayoutGroup>(true)) LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)g.transform);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        // ------------------------------------------------------------------ ajudantes

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

        // A moldura dos botões de baixo da tela que está sendo montada (null = a aba de madeira do armário).
        private static Sprite _buttonArt;

        private static (Button, TMP_Text) MakeButton(Transform parent, string name, Vector2 min, Vector2 max, string label, float size, TMP_FontAsset font)
        {
            var go = Node(name, parent);
            Set(Rt(go), min, max, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var img = Img(go, _buttonArt ?? LoadSprite(Art + "Tab/tab.png"), Color.white, true);
            img.type = Image.Type.Sliced; img.fillCenter = true; img.pixelsPerUnitMultiplier = _buttonArt != null ? 0.4f : 1f;
            var b = go.AddComponent<Button>();
            b.targetGraphic = img;
            var colors = b.colors; colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.78f); b.colors = colors;
            var l = Node("Label", go.transform);
            float pad = _buttonArt != null ? 32 : 12;
            Stretch(Rt(l), pad, 6, pad, 6);
            return (b, Txt(l, font, size, TitleGold, TextAlignmentOptions.Center, label, false, 18, size));
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

        private static Sprite LoadSprite(string path)
        {
            var s = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
            if (s == null) Debug.LogWarning("Eventos: não achei o sprite " + path);
            return s;
        }

        private static void Ref(SerializedObject so, string field, Object value)
        {
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError("Eventos: campo não encontrado: " + field); return; }
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
