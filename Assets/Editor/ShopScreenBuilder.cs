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
    // Monta a loja desenhada (ShopSetupGuide.md, partes 3 a 5) com a API da Unity: os prefabs ShopOffer e ShopScreen,
    // a cópia na cena Main e as ligações (Court e Game Bootstrap ▸ Shop). Reaproveita a arte do Arsenal; as peças novas
    // (as do PixelLab, em Art/Shop) entram depois, pelo ApplyArt: ele acerta a importação dos PNGs (Sprite, Point, sem
    // compressão, as bordas das peças que esticam) e põe cada sprite na sua Image, dentro dos prefabs. Enquanto um PNG
    // não existe, a Image dele fica SEM Source Image, só com uma cor de rascunho para dar para ver o lugar.
    // Roda sozinho (quando o prefab ShopScreen ainda não existe, ou quando a arte chegou e o prefab ainda está no
    // rascunho) e pelo menu Cubo ▸ Telas.
    [InitializeOnLoad]
    public static class ShopScreenBuilder
    {
        private const string Prefabs = "Assets/Game/View/Prefabs/UI/";
        private const string OfferPath = Prefabs + "ShopOffer.prefab", ScreenPath = Prefabs + "ShopScreen.prefab";
        private const string MainScene = "Assets/Game/Content/Materials/Scenes/Main.unity";
        private const string Art = "Assets/Game/View/Art/";

        // Cores de rascunho das peças que ainda não têm arte.
        private static readonly Color Parchment = Hex("D9C9A3"), Wood = Hex("5A3A22"), GoldC = Hex("FFD34D"), Heart = Hex("D9433B"),
                                      SaleRed = Hex("C8322B"), SealRed = Hex("7A1C1C"), SceneC = new Color(0.16f, 0.18f, 0.22f, 1f);
        private static readonly Color Ink = Hex("3A2A1A"), TitleGold = Hex("FFD27A"), NameGold = Hex("F2CC73"), Body = Hex("DBDBE6");

        private const string CleanedKey = "Cubo.ShopScreen.OverridesCleaned.v1";
        private const string LayoutKey = "Cubo.ShopScreen.LayoutV2";
        private const string LayoutV3Key = "Cubo.ShopScreen.LayoutV3";
        private const string LayoutV4Key = "Cubo.ShopScreen.LayoutV4";
        private const string StrikeKey = "Cubo.ShopOffer.Strike";
        private const string NoFloorKey = "Cubo.ShopScreen.NoFloor";
        private const string LayoutV5Key = "Cubo.ShopScreen.LayoutV5";

        // A arte do PixelLab.
        private const string ShopArt = Art + "Shop/";
        private const string ArtTriedKey = "Cubo.ShopScreen.ArtTried";
        private const float Density = 0.29f;                 // o mesmo Pixels Per Unit Multiplier do armário do Arsenal
        private const float PlankHeight = 44f;               // a tábua da prateleira (12 px de arte)

        static ShopScreenBuilder()
        {
            EditorApplication.delayCall += RunPending;
            // em Play os passos de uma vez esperam: rodam quando o Play acaba (sair do Play não recarrega os scripts)
            EditorApplication.playModeStateChanged += st => { if (st == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += RunPending; };
        }

        private static void RunPending()
        {
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (!File.Exists(ScreenPath)) { Build(); return; }
                // a loja v2: topo numa faixa acima da cena, a vida de cada herói, o balão ao lado do vendedor (uma vez)
                if (!EditorPrefs.GetBool(LayoutKey, false) && EditorSceneManager.GetActiveScene().path == MainScene)
                {
                    EditorPrefs.SetBool(LayoutKey, true);
                    UpgradeLayout();
                    CleanOverrides();
                }
                // v3: o topo alinhado numa linha só, o balão mais alto, a cena com o lampião corrigido (uma vez)
                if (!EditorPrefs.GetBool(LayoutV3Key, false) && EditorSceneManager.GetActiveScene().path == MainScene)
                {
                    EditorPrefs.SetBool(LayoutV3Key, true);
                    UpgradeLayoutV3();
                    CleanOverrides();
                }
                // v4: a placa de volta no alto da cena (como na 1ª versão) e o balão um pouco mais à esquerda (uma vez)
                if (!EditorPrefs.GetBool(LayoutV4Key, false) && EditorSceneManager.GetActiveScene().path == MainScene)
                {
                    EditorPrefs.SetBool(LayoutV4Key, true);
                    UpgradeLayoutV4();
                    CleanOverrides();
                }
                // o preço antigo da promoção ganha um risco de verdade (o <s> do TextMeshPro não aparece na fonte Romulus)
                if (!EditorPrefs.GetBool(StrikeKey, false))
                {
                    EditorPrefs.SetBool(StrikeKey, true);
                    AddStrike();
                }
                // o texto "Andar 2/6" saiu do topo da loja (uma vez)
                if (!EditorPrefs.GetBool(NoFloorKey, false) && EditorSceneManager.GetActiveScene().path == MainScene)
                {
                    EditorPrefs.SetBool(NoFloorKey, true);
                    RemoveFloor();
                    CleanOverrides();
                }
                // v5: vida dos heróis maior, bolsa de runas maior com o balão, a moldura do armário em volta do vendedor
                if (!EditorPrefs.GetBool(LayoutV5Key, false) && EditorSceneManager.GetActiveScene().path == MainScene)
                {
                    EditorPrefs.SetBool(LayoutV5Key, true);
                    UpgradeLayoutV5();
                    CleanOverrides();
                }
                // a arte nova chegou e o prefab ainda está no rascunho: aplica (uma tentativa por sessão do editor)
                if (ArtExists && !SessionState.GetBool(ArtTriedKey, false) && ArtMissingInPrefab())
                {
                    SessionState.SetBool(ArtTriedKey, true);
                    ApplyArt();
                }
                // a 1ª montagem deixou overrides na cópia da cena (cores do texto, posições da layout): limpa uma vez
                if (!EditorPrefs.GetBool(CleanedKey, false) && EditorSceneManager.GetActiveScene().path == MainScene)
                {
                    EditorPrefs.SetBool(CleanedKey, true);
                    CleanOverrides();
                }
            }
        }

        [MenuItem("Cubo/Telas/Limpar os overrides da Loja na cena")]
        public static void CleanOverrides()
        {
            Settle(OfferPath);
            Settle(ScreenPath);
            var sceneObj = EditorSceneManager.GetActiveScene();
            var shop = Object.FindAnyObjectByType<ShopScreen>(FindObjectsInactive.Include);
            if (shop == null || sceneObj.path != MainScene) { Debug.LogWarning("Loja: abra a cena Main com a ShopScreen."); return; }
            bool wasDirty = sceneObj.isDirty;
            var inst = PrefabUtility.GetOutermostPrefabInstanceRoot(shop.gameObject);
            var mods = PrefabUtility.GetPropertyModifications(inst) ?? new PropertyModification[0];
            // fica só o que a cena precisa: a Corte (um objeto da cena) e o que é da raiz (o Canvas sempre difere na cena)
            var keep = mods.Where(m => m.propertyPath == "court" || IsRoot(m.target)).ToArray();
            PrefabUtility.SetPropertyModifications(inst, keep);
            EditorSceneManager.MarkSceneDirty(sceneObj);
            if (!wasDirty) EditorSceneManager.SaveScene(sceneObj);
            Debug.Log("Loja: " + (mods.Length - keep.Length) + " overrides removidos da ShopScreen na cena" +
                      (wasDirty ? " (aperte Cmd+S para salvar a cena)." : "."));
        }

        private static bool IsRoot(Object target)
        {
            var go = target is Component c ? c.gameObject : target as GameObject;
            return go != null && go.transform.parent == null;
        }

        // Abre o prefab, deixa o TextMeshPro e as layouts calcularem tudo e salva: assim a cópia na cena nasce igual
        // ao prefab e não grava overrides de cor do texto nem de posição.
        private static void Settle(string path)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            foreach (var t in root.GetComponentsInChildren<TMP_Text>(true)) { t.color = t.color; t.ForceMeshUpdate(true, true); }
            foreach (var g in root.GetComponentsInChildren<LayoutGroup>(true))
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)g.transform);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        [MenuItem("Cubo/Telas/Montar a Loja (ShopScreen)")]
        public static void BuildFromMenu()
        {
            if (EditorApplication.isPlaying) { EditorUtility.DisplayDialog("Cubo", "Saia do Play antes de montar a loja.", "OK"); return; }
            if (File.Exists(ScreenPath) && !EditorUtility.DisplayDialog("Cubo", "Os prefabs ShopOffer e ShopScreen já existem. Montar de novo apaga o que você mudou neles.", "Montar de novo", "Cancelar")) return;
            Build();
        }

        public static void Build()
        {
            var alagard = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Game/View/Fonts/alagard.asset");
            var romulus = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Game/View/Fonts/Romulus.asset");
            var offer = BuildOffer(romulus, alagard);
            Settle(OfferPath);
            offer = AssetDatabase.LoadAssetAtPath<GameObject>(OfferPath).GetComponent<ShopOfferSlot>();
            BuildScreen(offer, alagard, romulus);
            Settle(ScreenPath);
            UpgradeLayout();
            UpgradeLayoutV3();
            UpgradeLayoutV4();
            AddStrike();
            RemoveFloor();
            UpgradeLayoutV5();
            if (ArtExists) ApplyArt();
            WireScene();
            Debug.Log("Loja montada: " + OfferPath + " e " + ScreenPath + (ArtExists ? "." : ". As peças sem Source Image esperam a arte do PixelLab."));
        }

        // ------------------------------------------------------------------ ShopOffer

        private static ShopOfferSlot BuildOffer(TMP_FontAsset romulus, TMP_FontAsset alagard)
        {
            var root = Node("ShopOffer", null);
            Size(Rt(root), 150, 186);
            Img(root, null, new Color(1f, 1f, 1f, 0f), true);                       // invisível, pega o toque

            var frame = Node("Frame", root.transform);
            Set(Rt(frame), new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 150));
            Img(frame, LoadSprite(Art + "Armory/Armory-square-item-slot.png"), Color.white, false).preserveAspect = true;

            var icon = Center(frame.transform, "Icon", 96, 96);
            Img(icon, null, Color.white, false).enabled = false;                     // o jogo põe o ícone
            var portrait = Center(frame.transform, "Portrait", 110, 110);
            Img(portrait, null, Color.white, false).enabled = false;

            var tier = Node("Tier", frame.transform);
            Set(Rt(tier), new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-8, -6), new Vector2(70, 36));
            Txt(tier, alagard, 28, NameGold, TextAlignmentOptions.TopRight, "", false);

            var sale = Node("SaleBadge", frame.transform);
            Set(Rt(sale), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(-8, 8), new Vector2(56, 56));
            Img(sale, null, SaleRed, false);
            sale.SetActive(false);

            var seal = Center(frame.transform, "SoldSeal", 84, 84);
            Rt(seal).localEulerAngles = new Vector3(0, 0, -12);
            Img(seal, null, SealRed, false);
            seal.SetActive(false);

            var tag = Node("PriceTag", root.transform);
            Set(Rt(tag), new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(-16, 44));
            Img(tag, null, Parchment, false);
            Row(tag, 8, 8, 4, 4, 6, TextAnchor.MiddleCenter, true);
            var coin = Node("Coin", tag.transform);
            Img(coin, null, GoldC, false);
            Pref(coin, 28, 28);
            var price = Node("Price", tag.transform);
            var priceText = Txt(price, romulus, 28, Ink, TextAlignmentOptions.Center, "00", false);
            var old = Node("OldPrice", tag.transform);
            var oldText = Txt(old, romulus, 20, Hex("7A6A5A"), TextAlignmentOptions.Center, "00", false);
            old.SetActive(false);

            var slot = root.AddComponent<ShopOfferSlot>();
            var so = new SerializedObject(slot);
            Ref(so, "frame", frame.GetComponent<Image>());
            Ref(so, "icon", icon.GetComponent<Image>());
            Ref(so, "portrait", portrait.GetComponent<Image>());
            Ref(so, "tierTag", tier.GetComponent<TMP_Text>());
            Ref(so, "price", priceText);
            Ref(so, "oldPrice", oldText);
            Ref(so, "saleBadge", sale);
            Ref(so, "soldStamp", seal);
            Ref(so, "priceTag", tag);
            so.FindProperty("soldAlpha").floatValue = 0.35f;
            so.ApplyModifiedPropertiesWithoutUndo();

            var saved = PrefabUtility.SaveAsPrefabAsset(root, OfferPath);
            Object.DestroyImmediate(root);
            return saved.GetComponent<ShopOfferSlot>();
        }

        // ------------------------------------------------------------------ ShopScreen

        private static void BuildScreen(ShopOfferSlot offer, TMP_FontAsset alagard, TMP_FontAsset romulus)
        {
            var root = new GameObject("ShopScreen", typeof(RectTransform)) { layer = 5 };
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

            var back = Node("Back", root.transform);
            Stretch(Rt(back));
            Img(back, LoadSprite(Art + "background_stone.png"), Color.white, true);

            var safe = Node("SafeArea", root.transform);
            Stretch(Rt(safe));
            var sa = new SerializedObject(safe.AddComponent<Cubo.View.SafeArea>());
            Float(sa, "minBottom", 0); Float(sa, "sidePadding", 24); Float(sa, "topPadding", 16); Float(sa, "bottomPadding", 16);
            sa.ApplyModifiedPropertiesWithoutUndo();

            // ---- o vendedor
            var merchant = Band(safe.transform, "MerchantPanel", 0.70f, 1f);
            var scene = Node("Scene", merchant.transform);
            Stretch(Rt(scene));
            Img(scene, null, SceneC, false);                                         // shop_merchant_scene (PixelLab)

            var plate = Node("Nameplate", merchant.transform);
            Set(Rt(plate), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(294, 72));
            Img(plate, LoadSprite(Art + "Armory/Armory_nameplate.png"), Color.white, false);
            var title = Node("Title", plate.transform);
            Stretch(Rt(title), 0, 0, 0, 4);
            var titleText = Txt(title, alagard, 36, TitleGold, TextAlignmentOptions.Center, "Shop", false);

            var floor = Node("Floor", merchant.transform);
            Set(Rt(floor), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -76), new Vector2(400, 36));
            var floorText = Txt(floor, romulus, 24, Body, TextAlignmentOptions.Center, "Floor 2/6", false);

            var goldText = Chip(merchant.transform, "GoldChip", "Coin", GoldC, "Gold", "85", false, 190, romulus);
            var healthText = Chip(merchant.transform, "HealthChip", "Heart", Heart, "Health", "72/125", true, 230, romulus);

            var bubble = Node("Bubble", merchant.transform);
            Set(Rt(bubble), new Vector2(0.50f, 0.10f), new Vector2(0.97f, 0.48f), new Vector2(0, 0), Vector2.zero, Vector2.zero);
            Img(bubble, null, Parchment, false);                                     // shop_speech_bubble (PixelLab)
            var line = Node("Line", bubble.transform);
            Stretch(Rt(line), 28, 20, 24, 30);
            var lineText = Txt(line, romulus, 26, Ink, TextAlignmentOptions.Center, "Welcome, traveller.", true, 18, 26);

            // ---- as prateleiras
            var cabinet = Band(safe.transform, "Cabinet", 0.11f, 0.70f);
            var cab = Img(cabinet, LoadSprite(Art + "Armory/Armory_cabinet_panel.png"), Color.white, false);
            cab.type = Image.Type.Tiled; cab.pixelsPerUnitMultiplier = 0.29f;

            var shelves = Band(safe.transform, "Shelves", 0.25f, 0.70f);
            var vlg = shelves.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(50, 50, 52, 26);
            vlg.spacing = 4;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true; vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            var upTitle = ShelfTitle(shelves.transform, "UpgradesTitle", "Upgrades", alagard);
            var upRow = ShelfRow(shelves.transform, "UpgradeRow");
            var ruTitle = ShelfTitle(shelves.transform, "RunesTitle", "Runes", alagard);
            var ruRow = ShelfRow(shelves.transform, "RuneRow");
            var seTitle = ShelfTitle(shelves.transform, "ServicesTitle", "Services", alagard);
            var seRow = ShelfRow(shelves.transform, "ServiceRow");

            var rail = Node("ShelfRail", safe.transform);
            Set(Rt(rail), new Vector2(0, 0.25f), new Vector2(1, 0.25f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0, 44.83f));
            var railImg = Img(rail, LoadSprite(Art + "Armory/Armory_cabinet_rail.png"), Color.white, false);
            railImg.type = Image.Type.Tiled; railImg.pixelsPerUnitMultiplier = 0.29f;

            // ---- o detalhe
            var detail = Band(safe.transform, "DetailPanel", 0.11f, 0.25f);
            var iconFrame = Node("IconFrame", detail.transform);
            Set(Rt(iconFrame), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(60, 0), new Vector2(150, 150));
            Img(iconFrame, LoadSprite(Art + "Armory/Armory-square-item-slot.png"), Color.white, false).preserveAspect = true;
            var dIcon = Center(iconFrame.transform, "Icon", 96, 96);
            Img(dIcon, null, Color.white, false).enabled = false;
            var dPortrait = Center(iconFrame.transform, "Portrait", 110, 110);
            Img(dPortrait, null, Color.white, false).enabled = false;
            var dName = Node("Name", detail.transform);
            Set(Rt(dName), new Vector2(0.23f, 0.5f), new Vector2(0.66f, 0.5f), new Vector2(0.5f, 1), new Vector2(0, 75), new Vector2(0, 50));   // do topo da moldura (150 de altura, centrada) para baixo
            var dNameText = Txt(dName, alagard, 32, NameGold, TextAlignmentOptions.TopLeft, "Name", false, 20, 32);
            var dEffect = Node("Effect", detail.transform);
            Set(Rt(dEffect), new Vector2(0.23f, 0.5f), new Vector2(0.66f, 0.5f), new Vector2(0.5f, 1), new Vector2(0, 25), new Vector2(0, 100));   // até o pé da moldura
            var dEffectText = Txt(dEffect, romulus, 24, Body, TextAlignmentOptions.TopLeft, "Effect", true, 16, 24);
            var (buy, buyLabel) = MakeButton(detail.transform, "BuyButton", new Vector2(0.68f, 0.22f), new Vector2(0.96f, 0.78f), "Buy · 45", 30, 18, alagard);

            // ---- a bolsa e a saída
            var bottom = Band(safe.transform, "BottomBar", 0f, 0.11f);
            var bagTitle = Node("BagTitle", bottom.transform);
            Set(Rt(bagTitle), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(8, -4), new Vector2(360, 34));
            var bagTitleText = Txt(bagTitle, romulus, 24, Body, TextAlignmentOptions.Left, "Your runes", false);
            var bag = Node("Bag", bottom.transform);
            Set(Rt(bag), Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(8, 10), new Vector2(340, 100));
            Row(bag, 0, 0, 0, 0, 12, TextAnchor.MiddleLeft, false);
            var socket = LoadSprite(Art + "hero_equipment_column.png", "hero_equipment_column_2");
            var bagIcons = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var s = Node("Socket" + i, bag.transform);
                Size(Rt(s), 100, 100);
                Img(s, socket, Color.white, false).preserveAspect = true;
                var ic = Center(s.transform, "Icon", 64, 64);
                bagIcons[i] = Img(ic, null, Color.white, false);
                bagIcons[i].enabled = false;
            }
            var (leave, leaveLabel) = MakeButton(bottom.transform, "LeaveButton", new Vector2(0.52f, 0.14f), new Vector2(1f, 0.86f), "On to floor 2", 32, 20, alagard);

            // ---- o componente
            var screen = root.AddComponent<ShopScreen>();
            var so = new SerializedObject(screen);
            Ref(so, "screen", group);
            Ref(so, "title", titleText); Ref(so, "gold", goldText); Ref(so, "health", healthText); Ref(so, "floor", floorText);
            Ref(so, "line", lineText); Ref(so, "bubble", Rt(bubble));
            Ref(so, "upgradesTitle", upTitle); Ref(so, "runesTitle", ruTitle); Ref(so, "servicesTitle", seTitle);
            Ref(so, "upgradeRow", Rt(upRow)); Ref(so, "runeRow", Rt(ruRow)); Ref(so, "serviceRow", Rt(seRow));
            Ref(so, "offerPrefab", offer);
            Ref(so, "detailIcon", dIcon.GetComponent<Image>()); Ref(so, "detailPortrait", dPortrait.GetComponent<Image>());
            Ref(so, "detailName", dNameText); Ref(so, "detailEffect", dEffectText);
            Ref(so, "action", buy); Ref(so, "actionLabel", buyLabel);
            Ref(so, "bagTitle", bagTitleText);
            var bi = so.FindProperty("bagIcons");
            bi.arraySize = 3;
            for (int i = 0; i < 3; i++) bi.GetArrayElementAtIndex(i).objectReferenceValue = bagIcons[i];
            Ref(so, "leave", leave); Ref(so, "leaveLabel", leaveLabel);
            // os ícones: só os que já existem têm imagem; os do PixelLab ficam vazios até a arte chegar
            var ids = new[] { "folego", "estrela", "fluxo", "escudo", "vida", "presa", "brasa", "potion", "reroll" };
            var icons = so.FindProperty("icons");
            icons.arraySize = ids.Length;
            for (int i = 0; i < ids.Length; i++)
            {
                var e = icons.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("id").stringValue = ids[i];
                e.FindPropertyRelative("sprite").objectReferenceValue =
                    ids[i] == "presa" ? LoadSprite(Art + "Armory/Equipments/fang.png") :
                    ids[i] == "brasa" ? LoadSprite(Art + "Armory/Equipments/ember.png") : null;
            }
            so.FindProperty("iconCanvas").intValue = 48;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, ScreenPath);
            Object.DestroyImmediate(root);
        }

        // ------------------------------------------------------------------ a loja v2

        [MenuItem("Cubo/Telas/Atualizar o layout da Loja (v2)")]
        public static void UpgradeLayoutFromMenu()
        {
            if (EditorApplication.isPlaying) { EditorUtility.DisplayDialog("Cubo", "Saia do Play antes.", "OK"); return; }
            UpgradeLayout();
            CleanOverrides();
        }

        // Mexe no prefab salvo (mantém a arte e os ajustes): o nome, o andar e o ouro sobem para uma faixa (Header) acima da
        // cena, para nada cobrir os lampiões; a vida do time vira três rostos com barra (cada herói guarda a sua); o balão
        // fica menor, ao lado da cabeça do vendedor; as prateleiras, o detalhe e a barra de baixo descem um pouco.
        public static void UpgradeLayout()
        {
            if (!File.Exists(ScreenPath)) return;
            var romulus = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Game/View/Fonts/Romulus.asset");
            var root = PrefabUtility.LoadPrefabContents(ScreenPath);
            try
            {
                var safe = root.transform.Find("SafeArea");
                if (safe == null || safe.Find("Header") != null) return;              // já está na v2
                var merchant = safe.Find("MerchantPanel");

                var header = Band(safe, "Header", 0.925f, 1f);
                header.transform.SetSiblingIndex(merchant.GetSiblingIndex() + 1);
                Set((RectTransform)merchant, new Vector2(0, 0.62f), new Vector2(1, 0.925f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

                foreach (var name in new[] { "Nameplate", "Floor", "GoldChip" })
                {
                    var t = merchant.Find(name);
                    if (t != null) t.SetParent(header.transform, false);
                }
                var gold = header.transform.Find("GoldChip") as RectTransform;
                if (gold != null) { gold.anchorMin = gold.anchorMax = gold.pivot = new Vector2(0, 0.5f); gold.anchoredPosition = new Vector2(8, 0); }
                var floorT = header.transform.Find("Floor") as RectTransform;
                if (floorT != null) floorT.anchoredPosition = new Vector2(0, -74);
                var health = merchant.Find("HealthChip");
                if (health != null) Object.DestroyImmediate(health.gameObject);

                // a vida de cada herói: rosto, barra e "28/42"
                var row = Node("HeroHealth", header.transform);
                Set(Rt(row), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-8, 0), new Vector2(344, 124));
                Row(row, 0, 0, 0, 0, 8, TextAnchor.MiddleRight, false);
                var chips = new ShopHeroChip[3];
                for (int i = 0; i < 3; i++)
                {
                    var c = Node("Hero" + i, row.transform);
                    Size(Rt(c), 104, 124);
                    var face = Node("Face", c.transform);
                    Set(Rt(face), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(64, 64));
                    Img(face, null, Color.white, false).enabled = false;            // o jogo põe o rosto da Corte
                    var bar = Node("Bar", c.transform);
                    Set(Rt(bar), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -70), new Vector2(96, 12));
                    Img(bar, null, new Color(0.07f, 0.05f, 0.035f, 0.86f), false);
                    var fill = Node("Fill", bar.transform);
                    Stretch(Rt(fill));
                    var fillImg = Img(fill, null, Hex("5BBF4A"), false);
                    var fb = bar.AddComponent<FillBar>();
                    var fso = new SerializedObject(fb);
                    Ref(fso, "fill", fillImg);
                    fso.ApplyModifiedPropertiesWithoutUndo();
                    var label = Node("Label", c.transform);
                    Set(Rt(label), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -86), new Vector2(104, 30));
                    var lt = Txt(label, romulus, 22, Body, TextAlignmentOptions.Center, "28/42", false, 14, 22);
                    var chip = c.AddComponent<ShopHeroChip>();
                    var cso = new SerializedObject(chip);
                    Ref(cso, "face", face.GetComponent<Image>());
                    Ref(cso, "bar", fb);
                    Ref(cso, "label", lt);
                    cso.ApplyModifiedPropertiesWithoutUndo();
                    chips[i] = chip;
                }

                // o balão: menor, logo à direita da cabeça do vendedor, com o rabicho apontando para ele
                var bubble = merchant.Find("Bubble") as RectTransform;
                if (bubble != null) Set(bubble, new Vector2(0.30f, 0.55f), new Vector2(0.60f, 0.86f), new Vector2(0, 0), Vector2.zero, Vector2.zero);

                // o resto desce para caber a faixa de cima
                SetBand(safe, "Cabinet", 0.10f, 0.62f);
                SetBand(safe, "Shelves", 0.23f, 0.62f);
                SetBand(safe, "DetailPanel", 0.10f, 0.23f);
                SetBand(safe, "BottomBar", 0f, 0.10f);
                var rail = safe.Find("ShelfRail") as RectTransform;
                if (rail != null) { rail.anchorMin = new Vector2(0, 0.23f); rail.anchorMax = new Vector2(1, 0.23f); }
                var shelves = safe.Find("Shelves");
                var vlg = shelves != null ? shelves.GetComponent<VerticalLayoutGroup>() : null;
                if (vlg != null) { vlg.padding = new RectOffset(50, 50, 48, 16); vlg.spacing = 0; }
                foreach (var title in new[] { "UpgradesTitle", "RunesTitle", "ServicesTitle" })
                {
                    var le = shelves != null && shelves.Find(title) != null ? shelves.Find(title).GetComponent<LayoutElement>() : null;
                    if (le != null) le.preferredHeight = 30;
                }

                var so = new SerializedObject(root.GetComponent<ShopScreen>());
                Ref(so, "health", null);
                var hc = so.FindProperty("heroChips");
                hc.arraySize = 3;
                for (int i = 0; i < 3; i++) hc.GetArrayElementAtIndex(i).objectReferenceValue = chips[i];
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, ScreenPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            Settle(ScreenPath);
            Debug.Log("Loja: layout v2 aplicado (faixa de cima, vida de cada herói, balão ao lado do vendedor).");
        }

        // A cena do vendedor: a cópia com o lampião no balcão (feita no PixelLab), se existir; o PNG original não é mexido.
        private static string SceneArt => File.Exists(ShopArt + "shop_merchant_scene_counter.png") ? "shop_merchant_scene_counter" : "shop_merchant_scene";

        [MenuItem("Cubo/Telas/Atualizar o layout da Loja (v3)")]
        public static void UpgradeLayoutV3FromMenu()
        {
            if (EditorApplication.isPlaying) { EditorUtility.DisplayDialog("Cubo", "Saia do Play antes.", "OK"); return; }
            UpgradeLayoutV3();
            CleanOverrides();
        }

        // O ouro, a placa e os rostos dos heróis na mesma altura (o andar logo abaixo da placa); o balão um pouco mais
        // alto, com o rabicho na altura do rosto do vendedor; e a cena com o lampião sem a mancha marrom.
        public static void UpgradeLayoutV3()
        {
            if (!File.Exists(ScreenPath)) return;
            ImportArt();
            var root = PrefabUtility.LoadPrefabContents(ScreenPath);
            try
            {
                var header = root.transform.Find("SafeArea/Header");
                if (header == null) return;
                const float line = 30f;                                  // a altura do centro dos rostos dentro da faixa
                if (header.Find("Nameplate") is RectTransform plate)
                    Set(plate, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, line), plate.sizeDelta);
                if (header.Find("Floor") is RectTransform floor)
                    Set(floor, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1), new Vector2(0, line - 40), floor.sizeDelta);
                if (header.Find("GoldChip") is RectTransform gold)
                    Set(gold, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(8, line), gold.sizeDelta);

                if (root.transform.Find("SafeArea/MerchantPanel/Bubble") is RectTransform bubble)
                    Set(bubble, new Vector2(0.30f, 0.60f), new Vector2(0.60f, 0.91f), new Vector2(0, 0), Vector2.zero, Vector2.zero);

                var scene = root.transform.Find("SafeArea/MerchantPanel/Scene");
                var img = scene != null ? scene.GetComponent<Image>() : null;
                var lit = ShopSprite(SceneArt);
                if (img != null && lit != null) { img.sprite = lit; img.color = Color.white; }

                PrefabUtility.SaveAsPrefabAsset(root, ScreenPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            Settle(ScreenPath);
            Debug.Log("Loja: layout v3 aplicado (topo alinhado, balão mais alto, lampião corrigido).");
        }

        [MenuItem("Cubo/Telas/Atualizar o layout da Loja (v4)")]
        public static void UpgradeLayoutV4FromMenu()
        {
            if (EditorApplication.isPlaying) { EditorUtility.DisplayDialog("Cubo", "Saia do Play antes.", "OK"); return; }
            UpgradeLayoutV4();
            CleanOverrides();
        }

        // A placa "Loja" volta para dentro da cena, centrada no alto dela (pendurada na viga, como na 1ª versão); o andar
        // fica sozinho no meio da faixa de cima, na linha do ouro e dos rostos; o balão vai um pouco para a esquerda.
        public static void UpgradeLayoutV4()
        {
            if (!File.Exists(ScreenPath)) return;
            var root = PrefabUtility.LoadPrefabContents(ScreenPath);
            try
            {
                var merchant = root.transform.Find("SafeArea/MerchantPanel");
                var header = root.transform.Find("SafeArea/Header");
                if (merchant == null || header == null) return;
                if (header.Find("Nameplate") is RectTransform plate)
                {
                    plate.SetParent(merchant, false);
                    plate.SetAsLastSibling();                                 // por cima da cena
                    if (merchant.Find("Bubble") is Transform b) b.SetAsLastSibling();
                    Set(plate, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, plate.sizeDelta);
                }
                if (header.Find("Floor") is RectTransform floor)
                    Set(floor, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 30), floor.sizeDelta);
                if (merchant.Find("Bubble") is RectTransform bubble)
                    Set(bubble, new Vector2(0.245f, 0.66f), new Vector2(0.515f, 0.94f), new Vector2(0, 0), Vector2.zero, Vector2.zero);   // o rabicho na altura do rosto
                PrefabUtility.SaveAsPrefabAsset(root, ScreenPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            Settle(ScreenPath);
            Debug.Log("Loja: layout v4 aplicado (placa no alto da cena, balão mais à esquerda).");
        }

        [MenuItem("Cubo/Telas/Riscar o preço antigo da Loja")]
        public static void AddStrikeFromMenu()
        {
            if (EditorApplication.isPlaying) { EditorUtility.DisplayDialog("Cubo", "Saia do Play antes.", "OK"); return; }
            AddStrike();
        }

        // Um risco vermelho, fino e levemente inclinado, por cima do preço antigo (PriceTag ▸ OldPrice ▸ Strike). Ele vive
        // dentro do OldPrice, então aparece e some junto com ele (só na oferta em promoção).
        public static void AddStrike()
        {
            if (!File.Exists(OfferPath)) return;
            var root = PrefabUtility.LoadPrefabContents(OfferPath);
            try
            {
                var old = root.transform.Find("PriceTag/OldPrice");
                if (old == null || old.Find("Strike") != null) return;
                var strike = Node("Strike", old);
                Set(Rt(strike), new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(6, 3));
                Rt(strike).localEulerAngles = new Vector3(0, 0, 14);
                Img(strike, null, Hex("B0302A"), false);
                PrefabUtility.SaveAsPrefabAsset(root, OfferPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            Settle(OfferPath);
            Debug.Log("Loja: risco no preço antigo adicionado (ShopOffer ▸ PriceTag ▸ OldPrice ▸ Strike).");
        }

        // Tira o texto do andar ("Andar 2/6") do topo da loja e desliga o campo Floor da Shop Screen.
        public static void RemoveFloor()
        {
            if (!File.Exists(ScreenPath)) return;
            var root = PrefabUtility.LoadPrefabContents(ScreenPath);
            try
            {
                var floor = root.transform.Find("SafeArea/Header/Floor") ?? root.transform.Find("SafeArea/MerchantPanel/Floor");
                if (floor == null) return;
                Object.DestroyImmediate(floor.gameObject);
                var so = new SerializedObject(root.GetComponent<ShopScreen>());
                Ref(so, "floor", null);
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, ScreenPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            Settle(ScreenPath);
            Debug.Log("Loja: o texto do andar saiu do topo.");
        }

        [MenuItem("Cubo/Telas/Atualizar o layout da Loja (v5)")]
        public static void UpgradeLayoutV5FromMenu()
        {
            if (EditorApplication.isPlaying) { EditorUtility.DisplayDialog("Cubo", "Saia do Play antes.", "OK"); return; }
            UpgradeLayoutV5();
            CleanOverrides();
        }

        // v5:
        // - a vida de cada herói: barra maior, com borda escura, verde mais vivo e o número maior, em branco;
        // - a bolsa de runas: título dourado e maior (como os das prateleiras), espaços maiores que aceitam toque, e o
        //   balão de detalhe (uma cópia do PowerTip da batalha, sem o botão Usar);
        // - o armário (Cabinet) sobe e emoldura também o vendedor: a borda corre pelas laterais da tela e encontra a
        //   placa "Loja", que fica em cima do trilho de cima; um trilho separa a cena das prateleiras.
        public static void UpgradeLayoutV5()
        {
            if (!File.Exists(ScreenPath)) return;
            var alagard = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Game/View/Fonts/alagard.asset");
            var romulus = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Game/View/Fonts/Romulus.asset");
            var root = PrefabUtility.LoadPrefabContents(ScreenPath);
            try
            {
                var safe = root.transform.Find("SafeArea");
                if (safe == null || safe.Find("SceneRail") != null) return;            // já está na v5

                // ---- a vida dos heróis
                var row = safe.Find("Header/HeroHealth") as RectTransform;
                if (row != null)
                {
                    row.sizeDelta = new Vector2(350, 128);
                    foreach (Transform c in row)
                    {
                        ((RectTransform)c).sizeDelta = new Vector2(110, 128);
                        if (c.Find("Bar") is RectTransform bar)
                        {
                            Set(bar, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -66), new Vector2(106, 24));
                            var bg = bar.GetComponent<Image>();
                            if (bg != null) bg.color = new Color(0.04f, 0.025f, 0.02f, 1f);          // a borda escura
                            if (bar.Find("Fill") is RectTransform fill) Stretch(fill, 3, 3, 3, 3);
                        }
                        if (c.Find("Label") is RectTransform label)
                        {
                            Set(label, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -92), new Vector2(116, 34));
                            var t = label.GetComponent<TMP_Text>();
                            if (t != null) { t.fontSize = 28; t.fontSizeMax = 28; t.fontSizeMin = 18; t.color = Color.white; t.fontStyle = FontStyles.Bold; }
                        }
                        var chip = c.GetComponent<ShopHeroChip>();
                        if (chip != null)
                        {
                            var cs = new SerializedObject(chip);
                            cs.FindProperty("barColor").colorValue = Hex("6BD35A");
                            cs.ApplyModifiedPropertiesWithoutUndo();
                        }
                    }
                }

                // ---- a bolsa de runas
                if (safe.Find("BottomBar/BagTitle") is RectTransform bagTitle)
                {
                    Set(bagTitle, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(8, -2), new Vector2(420, 40));
                    var t = bagTitle.GetComponent<TMP_Text>();
                    if (t != null) { if (alagard != null) t.font = alagard; t.fontSize = 30; t.color = NameGold; }
                }
                // o botão de seguir na mesma linha da bolsa: mesma altura (124) e mesmo pé (8)
                if (safe.Find("BottomBar/LeaveButton") is RectTransform leaveBtn)
                    Set(leaveBtn, new Vector2(0.52f, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(0, 124));
                if (safe.Find("BottomBar/Bag") is RectTransform bag)
                {
                    Set(bag, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(8, 8), new Vector2(400, 124));
                    foreach (Transform s in bag)
                    {
                        ((RectTransform)s).sizeDelta = new Vector2(124, 124);
                        var img = s.GetComponent<Image>();
                        if (img != null) img.raycastTarget = true;                      // o toque abre o balão
                        if (s.Find("Icon") is RectTransform ic) ic.sizeDelta = new Vector2(84, 84);
                    }
                }

                // o balão: uma cópia do PowerTip da batalha, sem o botão Usar
                PowerTip tipCopy = null;
                var hud = PrefabUtility.LoadPrefabContents("Assets/Game/View/Prefabs/UI/BattleHud.prefab");
                try
                {
                    var src = hud.transform.Find("SafeArea/PowerTip");
                    if (src != null)
                    {
                        var copy = Object.Instantiate(src.gameObject, safe, false);
                        copy.name = "RuneTip";
                        var use = copy.transform.Find("UseButton");
                        if (use != null) Object.DestroyImmediate(use.gameObject);
                        tipCopy = copy.GetComponent<PowerTip>();
                        copy.transform.SetAsLastSibling();
                    }
                    else Debug.LogWarning("Loja: não achei BattleHud ▸ SafeArea ▸ PowerTip para copiar o balão.");
                }
                finally { PrefabUtility.UnloadPrefabContents(hud); }

                // ---- a moldura do armário em volta do vendedor
                var cabinet = safe.Find("Cabinet") as RectTransform;
                var merchant = safe.Find("MerchantPanel") as RectTransform;
                if (cabinet != null && merchant != null)
                {
                    const float border = 44.83f;                                       // 13 px de arte / 0,29
                    cabinet.SetSiblingIndex(0);                                         // o fundo do armário fica atrás de tudo
                    Set(cabinet, new Vector2(0, 0.10f), new Vector2(1, 0.925f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                    merchant.anchorMin = new Vector2(0, 0.62f); merchant.anchorMax = new Vector2(1, 0.925f);
                    merchant.offsetMin = new Vector2(border, 0); merchant.offsetMax = new Vector2(-border, -border);

                    var sceneRail = Node("SceneRail", safe);
                    sceneRail.transform.SetSiblingIndex(merchant.GetSiblingIndex() + 1);
                    Set(Rt(sceneRail), new Vector2(0, 0.62f), new Vector2(1, 0.62f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0, border));
                    var rail = Img(sceneRail, LoadSprite(Art + "Armory/Armory_cabinet_rail.png"), Color.white, false);
                    rail.type = Image.Type.Tiled; rail.pixelsPerUnitMultiplier = 0.29f;

                    if (merchant.Find("Nameplate") is RectTransform plate)        // a placa em cima do trilho de cima
                    {
                        plate.SetParent(safe, false);
                        Set(plate, new Vector2(0.5f, 0.925f), new Vector2(0.5f, 0.925f), new Vector2(0.5f, 0.5f), new Vector2(0, -border / 2), plate.sizeDelta);
                    }
                }
                if (safe.Find("Nameplate") is Transform np) np.SetAsLastSibling();
                if (safe.Find("RuneTip") is Transform rt) rt.SetAsLastSibling();          // o balão por cima de tudo

                var so = new SerializedObject(root.GetComponent<ShopScreen>());
                if (tipCopy != null) Ref(so, "tip", tipCopy);
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, ScreenPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            Settle(ScreenPath);
            Debug.Log("Loja: layout v5 aplicado (vida maior, bolsa com balão, moldura em volta do vendedor).");
        }

        private static void SetBand(Transform safe, string name, float yMin, float yMax)
        {
            var rt = safe.Find(name) as RectTransform;
            if (rt != null) Set(rt, new Vector2(0, yMin), new Vector2(1, yMax), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        }

        // ------------------------------------------------------------------ a arte do PixelLab

        private static bool ArtExists => File.Exists(ShopArt + "shop_merchant_scene.png");

        // O prefab ainda está no rascunho quando a cena do vendedor não tem Source Image.
        private static bool ArtMissingInPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScreenPath);
            var scene = prefab != null ? prefab.transform.Find("SafeArea/MerchantPanel/Scene") : null;
            var img = scene != null ? scene.GetComponent<Image>() : null;
            return img != null && img.sprite == null;
        }

        [MenuItem("Cubo/Telas/Aplicar a arte da Loja (Art ▸ Shop)")]
        public static void ApplyArtFromMenu()
        {
            if (EditorApplication.isPlaying) { EditorUtility.DisplayDialog("Cubo", "Saia do Play antes de aplicar a arte da loja.", "OK"); return; }
            if (!File.Exists(ScreenPath) || !File.Exists(OfferPath)) { Debug.LogWarning("Loja: monte a loja primeiro (Cubo ▸ Telas ▸ Montar a Loja)."); return; }
            ApplyArt();
        }

        // Acerta a importação dos PNGs de Art/Shop e põe cada sprite na sua Image, DENTRO dos prefabs (não na cópia da
        // cena). Só mexe nas Images da arte nova; pode rodar de novo quando um PNG for trocado ou chegar depois.
        public static void ApplyArt()
        {
            ImportArt();

            var offer = PrefabUtility.LoadPrefabContents(OfferPath);
            try
            {
                Icon(offer.transform, "Frame/SaleBadge", "shop_sale_badge");
                Icon(offer.transform, "Frame/SoldSeal", "shop_sold_seal");
                Strip(offer.transform, "PriceTag", "shop_price_tag");
                Icon(offer.transform, "PriceTag/Coin", "shop_coin");
                PrefabUtility.SaveAsPrefabAsset(offer, OfferPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(offer); }

            var root = PrefabUtility.LoadPrefabContents(ScreenPath);
            try
            {
                const string merchant = "SafeArea/MerchantPanel/";

                // a cena do vendedor cobre o painel inteiro sem achatar: em telas mais altas ela cresce e as sobras dos
                // lados ficam cortadas pela máscara do painel
                var scene = Painted(root.transform, merchant + "Scene", SceneArt);
                if (scene != null)
                {
                    scene.type = Image.Type.Simple;
                    scene.preserveAspect = false;
                    var fit = scene.GetComponent<AspectRatioFitter>();
                    if (fit == null) fit = scene.gameObject.AddComponent<AspectRatioFitter>();
                    fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                    fit.aspectRatio = scene.sprite.rect.width / scene.sprite.rect.height;
                    var panel = scene.transform.parent.gameObject;
                    if (panel.GetComponent<RectMask2D>() == null) panel.AddComponent<RectMask2D>();
                }

                // o balão: estica pelo meio; o rabicho fica inteiro no canto de baixo à esquerda (as bordas do sprite)
                var bubble = Painted(root.transform, merchant + "Bubble", "shop_speech_bubble");
                if (bubble != null)
                {
                    bubble.type = Image.Type.Sliced;
                    bubble.fillCenter = true;
                    bubble.pixelsPerUnitMultiplier = Density;
                    var line = bubble.transform.Find("Line") as RectTransform;
                    if (line != null) Stretch(line, 26, 20, 26, 46);          // o texto fica acima do rabicho
                }

                Strip(root.transform, "SafeArea/Header/GoldChip", "shop_price_tag");
                Icon(root.transform, "SafeArea/Header/GoldChip/Coin", "shop_coin");

                foreach (var row in new[] { "UpgradeRow", "RuneRow", "ServiceRow" })
                    Strip(root.transform, "SafeArea/Shelves/" + row + "/Plank", "shop_shelf_plank", PlankHeight);

                var screen = root.GetComponent<ShopScreen>();
                if (screen != null)
                {
                    var so = new SerializedObject(screen);
                    var icons = so.FindProperty("icons");
                    for (int i = 0; i < icons.arraySize; i++)
                    {
                        var e = icons.GetArrayElementAtIndex(i);
                        var file = IconFile(e.FindPropertyRelative("id").stringValue);
                        var sprite = file != null ? ShopSprite(file) : null;
                        if (sprite != null) e.FindPropertyRelative("sprite").objectReferenceValue = sprite;
                    }
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                PrefabUtility.SaveAsPrefabAsset(root, ScreenPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }

            Settle(OfferPath);
            Settle(ScreenPath);
            Debug.Log("Loja: arte de " + ShopArt + " aplicada nos prefabs ShopOffer e ShopScreen.");
        }

        // O PNG de cada ícone que a ShopScreen procura pelo id.
        private static string IconFile(string id)
        {
            switch (id)
            {
                case "folego": return "rune_folego";
                case "estrela": return "rune_estrela";
                case "fluxo": return "rune_fluxo";
                case "escudo": return "rune_escudo";
                case "vida": return "rune_vida";
                case "potion": return "shop_potion";
                case "reroll": return "shop_restock";
                default: return null;
            }
        }

        // As bordas (esquerda, baixo, direita, cima, em pixels da arte) das peças que esticam.
        private static Vector4 BorderOf(string name)
        {
            switch (name)
            {
                case "shop_speech_bubble": return new Vector4(18, 12, 5, 5);   // o rabicho inteiro fica no canto
                case "shop_price_tag": return new Vector4(8, 0, 8, 0);          // os rebites das pontas
                case "shop_shelf_plank": return new Vector4(24, 0, 18, 0);      // as ferragens das pontas
                default: return Vector4.zero;
            }
        }

        // Todos os PNGs de Art/Shop como no resto do jogo: Sprite (2D and UI), Single, 100 pixels por unidade,
        // Full Rect, Point (no filter), sem compressão, sem mipmaps.
        private static void ImportArt()
        {
            AssetDatabase.Refresh();
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ShopArt.TrimEnd('/') }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp == null) continue;
                var border = BorderOf(Path.GetFileNameWithoutExtension(path));
                var st = new TextureImporterSettings();
                imp.ReadTextureSettings(st);
                bool ok = imp.textureType == TextureImporterType.Sprite && imp.spriteImportMode == SpriteImportMode.Single &&
                          Mathf.Approximately(imp.spritePixelsPerUnit, 100f) && imp.filterMode == FilterMode.Point &&
                          imp.textureCompression == TextureImporterCompression.Uncompressed && !imp.mipmapEnabled &&
                          imp.alphaIsTransparency && imp.spriteBorder == border &&
                          st.spriteMeshType == SpriteMeshType.FullRect && !st.spriteGenerateFallbackPhysicsShape;
                if (ok) continue;
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.spritePixelsPerUnit = 100f;
                imp.filterMode = FilterMode.Point;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.mipmapEnabled = false;
                imp.alphaIsTransparency = true;
                imp.maxTextureSize = 2048;
                imp.spriteBorder = border;
                imp.ReadTextureSettings(st);
                st.spriteMeshType = SpriteMeshType.FullRect;
                st.spriteGenerateFallbackPhysicsShape = false;
                imp.SetTextureSettings(st);
                imp.SaveAndReimport();
            }
        }

        private static Sprite ShopSprite(string name)
        {
            string path = ShopArt + name + ".png";
            return File.Exists(path) ? LoadSprite(path) : null;
        }

        // Põe o sprite na Image do caminho e tira a cor de rascunho. Sem o PNG, a Image fica como está.
        private static Image Painted(Transform root, string path, string spriteName)
        {
            var t = root.Find(path);
            var img = t != null ? t.GetComponent<Image>() : null;
            if (img == null) { Debug.LogWarning("Loja: não achei a Image " + path + " para pôr " + spriteName + "."); return null; }
            var sprite = ShopSprite(spriteName);
            if (sprite == null) return null;
            img.sprite = sprite;
            img.color = Color.white;
            return img;
        }

        // Um ícone inteiro, sem achatar.
        private static void Icon(Transform root, string path, string spriteName)
        {
            var img = Painted(root, path, spriteName);
            if (img == null) return;
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
        }

        // Uma faixa que estica para os lados (etiqueta, placa, tábua): as pontas ficam inteiras e o meio estica. O
        // multiplicador faz a altura da arte caber certinho na altura da Image, então as pontas não achatam.
        private static void Strip(Transform root, string path, string spriteName, float height = 0f)
        {
            var img = Painted(root, path, spriteName);
            if (img == null) return;
            var rt = img.rectTransform;
            if (height > 0f) rt.sizeDelta = new Vector2(rt.sizeDelta.x, height);
            img.type = Image.Type.Sliced;
            img.fillCenter = true;
            if (rt.sizeDelta.y > 0f) img.pixelsPerUnitMultiplier = img.sprite.rect.height / rt.sizeDelta.y;
        }

        // ------------------------------------------------------------------ cena

        // Põe uma cópia do prefab na cena Main (do lado do EquipScreen) e liga a Corte e o Game Bootstrap ▸ Shop.
        private static void WireScene()
        {
            var sceneObj = EditorSceneManager.GetActiveScene();
            if (sceneObj.path != MainScene)
            {
                Debug.LogWarning("Loja: abra a cena Main e use Cubo ▸ Telas ▸ Montar a Loja para pôr a tela na cena.");
                return;
            }
            bool wasDirty = sceneObj.isDirty;
            var existing = Object.FindObjectsByType<ShopScreen>(FindObjectsInactive.Include);
            foreach (var old in existing) Object.DestroyImmediate(old.gameObject);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScreenPath);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, sceneObj);
            var equip = Object.FindAnyObjectByType<EquipScreen>(FindObjectsInactive.Include);
            if (equip != null && equip.transform.parent == null) inst.transform.SetSiblingIndex(equip.transform.GetSiblingIndex() + 1);

            var shop = inst.GetComponent<ShopScreen>();
            var court = Object.FindAnyObjectByType<GodsScreen>(FindObjectsInactive.Include);
            var so = new SerializedObject(shop);
            Ref(so, "court", court);
            so.ApplyModifiedPropertiesWithoutUndo();

            var boot = Object.FindAnyObjectByType<GameBootstrap>(FindObjectsInactive.Include);
            if (boot != null)
            {
                var bs = new SerializedObject(boot);
                Ref(bs, "shop", shop);
                bs.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorSceneManager.MarkSceneDirty(sceneObj);
            if (!wasDirty) EditorSceneManager.SaveScene(sceneObj);
            else Debug.LogWarning("Loja: a cena Main tinha mudanças não salvas; aperte Cmd+S para salvar a loja junto.");
        }

        // ------------------------------------------------------------------ ajudantes

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

        private static GameObject Band(Transform parent, string name, float yMin, float yMax)
        {
            var go = Node(name, parent);
            Set(Rt(go), new Vector2(0, yMin), new Vector2(1, yMax), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            return go;
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
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = raycast;
            if (sprite != null && sprite.border != Vector4.zero) { img.type = Image.Type.Sliced; img.fillCenter = true; }
            return img;
        }

        private static TMP_Text Txt(GameObject go, TMP_FontAsset font, float size, Color color, TextAlignmentOptions align,
                                     string text, bool wrap, float autoMin = 0, float autoMax = 0)
        {
            var t = go.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.text = text;
            t.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            t.raycastTarget = false;
            if (autoMax > 0) { t.enableAutoSizing = true; t.fontSizeMin = autoMin; t.fontSizeMax = autoMax; }
            return t;
        }

        private static void Row(GameObject go, int l, int r, int t, int b, float spacing, TextAnchor align, bool control)
        {
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(l, r, t, b);
            h.spacing = spacing;
            h.childAlignment = align;
            h.childControlWidth = control; h.childControlHeight = control;
            h.childScaleWidth = false; h.childScaleHeight = false;
            h.childForceExpandWidth = false; h.childForceExpandHeight = false;
        }

        private static void Pref(GameObject go, float w, float h)
        {
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = w; le.preferredHeight = h;
        }

        private static TMP_Text ShelfTitle(Transform parent, string name, string text, TMP_FontAsset font)
        {
            var go = Node(name, parent);
            var t = Txt(go, font, 28, NameGold, TextAlignmentOptions.Left, text, false);
            go.AddComponent<LayoutElement>().preferredHeight = 38;
            return t;
        }

        // Uma prateleira: as ofertas nascem aqui dentro (a ShopScreen cria), em pé sobre a tábua.
        private static GameObject ShelfRow(Transform parent, string name)
        {
            var go = Node(name, parent);
            go.AddComponent<LayoutElement>().preferredHeight = 190;
            Row(go, 0, 0, 0, 4, 40, TextAnchor.LowerCenter, false);
            var plank = Node("Plank", go.transform);
            plank.AddComponent<LayoutElement>().ignoreLayout = true;
            Set(Rt(plank), new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(20, 22));
            Img(plank, null, Wood, false);                                           // shop_shelf_plank (ApplyArt)
            return go;
        }

        // Uma placa de número (ouro ou vida): ícone e texto lado a lado. Ancorada no canto esquerdo ou direito do topo.
        private static TMP_Text Chip(Transform parent, string name, string iconName, Color iconColor, string textName, string text,
                                     bool right, float width, TMP_FontAsset font)
        {
            var go = Node(name, parent);
            var corner = new Vector2(right ? 1 : 0, 1);
            Set(Rt(go), corner, corner, corner, new Vector2(right ? -12 : 12, -12), new Vector2(width, 56));
            Img(go, null, Parchment, false);                                         // shop_price_tag (PixelLab)
            Row(go, 12, 12, 6, 6, 8, TextAnchor.MiddleCenter, true);
            var icon = Node(iconName, go.transform);
            Img(icon, null, iconColor, false);                                       // shop_coin / shop_heart (PixelLab)
            Pref(icon, 32, 32);
            var label = Node(textName, go.transform);
            return Txt(label, font, 30, Ink, TextAlignmentOptions.Center, text, false);
        }

        private static (Button, TMP_Text) MakeButton(Transform parent, string name, Vector2 min, Vector2 max, string label,
                                                 float size, float min2, TMP_FontAsset font)
        {
            var go = Node(name, parent);
            Set(Rt(go), min, max, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var img = Img(go, LoadSprite(Art + "Tab/tab.png"), Color.white, true);
            img.type = Image.Type.Sliced; img.fillCenter = true; img.pixelsPerUnitMultiplier = 1f;
            var b = go.AddComponent<Button>();
            b.targetGraphic = img;
            var colors = b.colors;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.78f);
            b.colors = colors;
            var l = Node("Label", go.transform);
            Stretch(Rt(l), 12, 6, 12, 6);
            var t = Txt(l, font, size, TitleGold, TextAlignmentOptions.Center, label, false, min2, size);
            return (b, t);
        }

        private static Sprite LoadSprite(string path, string name = null)
        {
            var all = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>();
            var s = name == null ? all.FirstOrDefault() : all.FirstOrDefault(x => x.name == name);
            if (s == null) Debug.LogWarning("Loja: não achei o sprite " + path + (name != null ? " (" + name + ")" : ""));
            return s;
        }

        private static void Ref(SerializedObject so, string field, Object value)
        {
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError("Loja: campo não encontrado: " + field); return; }
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
