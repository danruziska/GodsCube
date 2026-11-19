using UnityEngine;
using Cubo.Domain;

namespace Cubo.View
{
    // O cubo na cena. Fica na raiz do prefab CubeRig, que já traz a hierarquia pronta:
    //   CubeRig (este componente) > Root > Core e Pivot.
    // Aqui nada é modelado nem criado do zero: as peças são cópias do prefab Piece, postas no lugar.
    public sealed class CubeView : MonoBehaviour
    {
        private const float CoreGap = 0.1f;           // o núcleo fica um pouco menor que o cubo

        [Header("Partes do prefab CubeRig (arraste os filhos)")]
        [SerializeField] private Transform root;      // tudo o que gira junto; inclinar o cubo é girar este
        [SerializeField] private Transform core;      // o núcleo escuro, que tapa as frestas
        [SerializeField] private Transform pivot;     // vazio; a fatia que gira fica presa nele durante o giro

        [Header("Assets")]
        [SerializeField] private PieceView piecePrefab;
        [SerializeField] private Material[] palette = new Material[7];   // 0 a 5 = cores; 6 = a antiga peça crítica (não aparece mais desde a Fase 10)

        [Header("Imagens das peças (opcional)")]
        [Tooltip("A frente de cada peça, na mesma ordem da Palette: 0 Sol, 1 Mar, 2 Guerra, 3 Morte, 4 Colheita, 5 Tempestade. " +
                 "Elemento vazio: a peça fica só com a cor do material.")]
        [SerializeField] private Sprite[] faces = new Sprite[6];
        [Tooltip("Largura e altura da imagem na peça, em unidades do cubo. A peça tem 0,9; menos deixa a borda colorida aparecendo.")]
        [SerializeField, Min(0.1f)] private float faceSize = CubeLayout.PieceSize;

        [Header("Brilho da face escolhida na entrega")]
        [Tooltip("Cor e força (alpha) da luz sobre a face escolhida. Mesmo dourado do brilho dos espaços do time.")]
        [SerializeField] private Color faceGlowColor = new Color(1f, 0.78f, 0.30f, 0.43f);

        [Header("Moldura dos altares (achar a sua face)")]
        [Tooltip("A borda em volta da sua face (a da frente). Alpha 0 esconde.")]
        [SerializeField] private Color playerFrameColor = new Color(1f, 0.78f, 0.30f, 1f);
        [Tooltip("A borda em volta da face do rival (a de trás). Alpha 0 esconde.")]
        [SerializeField] private Color rivalFrameColor = new Color(0.72f, 0.16f, 0.14f, 1f);

        [Header("Veneno e fogo (as áreas na face)")]
        [Tooltip("A cor de uma área envenenada. O alpha é a força do brilho.")]
        [SerializeField] private Color poisonColor = new Color(0.35f, 1f, 0.25f, 0.45f);
        [Tooltip("A cor de uma área em chamas. O alpha é a força do brilho.")]
        [SerializeField] private Color burnColor = new Color(1f, 0.28f, 0.12f, 0.45f);
        [Tooltip("Enquanto o dedo mira a área nova: quanto o brilho fica mais forte (multiplica o alpha).")]
        [SerializeField, Min(1f)] private float aimBoost = 1.6f;

        private const string FrameName = "AltarFrame";
        private readonly int[] _faceTurns = new int[6];   // quartos de volta das imagens de cada face, para ficarem de pé
        private Quaternion _viewedFrom = new Quaternion(0f, 0f, 0f, 0f);
        private PieceView[] _pieces = new PieceView[0];
        private Transform[] _coreParts;                   // [0] a laje que gira, [1] e [2] as partes paradas
        private SpriteRenderer _glow;
        private int _glowN;
        private Sprite _hazardTile;
        private sbyte[] _hazards = new sbyte[0];          // o mapa das áreas (0 nada, 1 veneno, 2 fogo)
        private int[] _aim = new int[0];                  // as casas da área que o dedo está mirando
        private int _aimKind;
        private float _aimStrength = 1f;

        public Geometry Geo { get; private set; }
        public Transform Root => root;
        public Transform Pivot => pivot;
        public Transform Core => core;                // Fase 10: gira junto quando o cubo inteiro gira (a entrega)

        // O que falta arrastar no Inspector; null se está tudo ligado. Os testes e o Build usam.
        public string MissingReference()
        {
            if (root == null) return "Root";
            if (core == null) return "Core";
            if (pivot == null) return "Pivot";
            if (piecePrefab == null) return "Piece Prefab";
            if (palette == null || palette.Length != 7) return "Palette (precisa de 7 materiais)";
            for (int i = 0; i < palette.Length; i++) if (palette[i] == null) return "Palette, elemento " + i;
            return null;
        }

        // Monta o cubo de tamanho n: ajusta o núcleo e põe uma cópia do prefab Piece em cada casa.
        // Pode ser chamado de novo com outro tamanho.
        public void Build(int n)
        {
            string missing = MissingReference();
            if (missing != null) { Debug.LogError("CubeView: falta ligar no Inspector: " + missing); return; }

            Clear();
            Geo = Geometry.For(n);
            core.localScale = Vector3.one * (n - CoreGap);
            _pieces = new PieceView[Geo.Total];
            for (int c = 0; c < Geo.Total; c++)
            {
                var p = Instantiate(piecePrefab, root);
                p.name = "Piece " + c;
                p.Cell = c;
                p.transform.localPosition = CubeLayout.HomePosition(Geo, c);
                p.transform.localRotation = CubeLayout.HomeRotation(Geo, c);
                _pieces[c] = p;
            }
            BuildFrames();
        }

        // As duas molduras ficam presas no Root: giram com a vista, mas não com as fatias nem com a entrega,
        // porque a face da frente é sempre a sua e a de trás é sempre a do rival.
        private void BuildFrames()
        {
            ClearFrames();

            const int size = CubeLayout.FrameBorder * 2 + 1;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = FrameName, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Point
            };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int ring = Mathf.Min(Mathf.Min(x, y), Mathf.Min(size - 1 - x, size - 1 - y));
                    bool corner = (x == 0 || x == size - 1) && (y == 0 || y == size - 1);   // canto arredondado
                    float tone = corner ? -1f : CubeLayout.FrameTone(ring);
                    byte v = (byte)Mathf.RoundToInt(Mathf.Max(0f, tone) * 255f);
                    px[y * size + x] = new Color32(v, v, v, (byte)(tone < 0f ? 0 : 255));
                }
            tex.SetPixels32(px);
            tex.Apply();
            float ppu = CubeLayout.FrameBorder / CubeLayout.FrameThickness;
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), ppu, 0,
                SpriteMeshType.FullRect, Vector4.one * CubeLayout.FrameBorder);
            sprite.name = FrameName;

            MakeFrame(sprite, Faces.Front, playerFrameColor);
            MakeFrame(sprite, Faces.Back, rivalFrameColor);
        }

        private void MakeFrame(Sprite sprite, int face, Color color)
        {
            if (color.a <= 0f) return;
            var go = new GameObject(FrameName) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(root, false);
            Vector3 n = CubeLayout.Normal(face);
            go.transform.localPosition = n * (Geo.N * 0.5f + CubeLayout.FrameLift);
            go.transform.localRotation = Quaternion.LookRotation(-n, Vector3.up);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.drawMode = SpriteDrawMode.Sliced;
            float s = CubeLayout.FrameSize(Geo.N);
            sr.size = new Vector2(s, s);
            sr.color = color;
            sr.sortingOrder = 5;                  // por cima das imagens das peças, por baixo do brilho da entrega
        }

        // Remove todas as peças (inclusive as de uma pré-visualização antiga).
        public void Clear()
        {
            if (root == null) return;
            foreach (var p in root.GetComponentsInChildren<PieceView>(true)) Discard(p.gameObject);
            _pieces = new PieceView[0];
            ClearFrames();
        }

        private void ClearFrames()
        {
            var frames = new System.Collections.Generic.List<GameObject>();
            foreach (Transform child in root)
                if (child.name == FrameName) frames.Add(child.gameObject);
            foreach (var f in frames) Discard(f);
        }

        public int PieceCount => _pieces.Length;
        public PieceView Piece(int cell) => _pieces[cell];

        // A cor de um domínio (0 a 5), para a interface usar a mesma das peças.
        public Color PaletteColor(int index) =>
            index >= 0 && index < palette.Length && palette[index] != null ? palette[index].color : Color.white;

        // A imagem da frente das peças de uma cor (0 a 5), para a interface usar como ícone do elemento.
        public Sprite FaceSprite(int index) => faces != null && index >= 0 && index < faces.Length ? faces[index] : null;

        // O único caminho pelo qual cor chega à tela.
        public void Paint(sbyte[] colors)
        {
            for (int c = 0; c < _pieces.Length; c++)
                _pieces[c].SetColor(colors[c], palette, faces, faceSize, _faceTurns[Geo.CellFace[c]]);
        }

        public SliceTurn BeginTurn(Move move) => new SliceTurn(this, move);

        // As áreas de veneno e de fogo que estão na face (o mapa vem do evento HazardsChanged).
        public void ShowHazards(sbyte[] map)
        {
            _hazards = map ?? new sbyte[0];
            PaintOverlays();
        }

        // A área que o dedo está mirando (o centro é a casa do dedo): brilha mais forte; `strength` faz a respiração.
        // kind 1 veneno, 2 fogo. Sem casas: some.
        public void ShowAim(int[] cells, int kind, float strength)
        {
            _aim = cells ?? new int[0];
            _aimKind = kind;
            _aimStrength = strength;
            PaintOverlays();
        }

        private void PaintOverlays()
        {
            if (_pieces.Length == 0) return;
            if (_hazardTile == null) _hazardTile = MakeHazardTile();
            for (int c = 0; c < _pieces.Length; c++)
            {
                int kind = c < _hazards.Length ? _hazards[c] : 0;
                float boost = 1f;
                if (System.Array.IndexOf(_aim, c) >= 0) { kind = _aimKind; boost = aimBoost * _aimStrength; }
                var color = kind == 1 ? poisonColor : kind == 2 ? burnColor : Color.clear;
                color.a = Mathf.Clamp01(color.a * boost);
                _pieces[c].SetOverlay(_hazardTile, color);
            }
        }

        // Um quadrado branco de 16x16, um pouco mais forte na borda (a mancha da área ganha textura de "brilho").
        private static Sprite MakeHazardTile()
        {
            const int size = 16;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "HazardTile", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Point
            };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int ring = Mathf.Min(Mathf.Min(x, y), Mathf.Min(size - 1 - x, size - 1 - y));
                    byte a = (byte)(ring == 0 ? 255 : ring == 1 ? 215 : 170);
                    px[y * size + x] = new Color32(255, 255, 255, a);
                }
            tex.SetPixels32(px);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.name = "HazardTile";
            return sprite;
        }

        // Parte o núcleo em três para o giro de uma fatia: a laje dela vai para o Pivot e gira junto.
        public void SplitCore(Move move)
        {
            if (Geo == null || core == null) return;
            if (_coreParts == null || _coreParts[0] == null)
            {
                _coreParts = new Transform[3];
                for (int i = 0; i < 3; i++)
                {
                    var copy = Instantiate(core.gameObject, root);
                    copy.name = "CoreSlab " + i;
                    copy.hideFlags = HideFlags.DontSave;
                    foreach (Transform child in copy.transform) Discard(child.gameObject);
                    _coreParts[i] = copy.transform;
                }
            }
            CubeLayout.CoreSlabs(Geo.N, move.Axis, move.Slice, CoreGap, out var lower, out var turning, out var upper);
            float side = Geo.N - CoreGap;
            PlaceSlab(_coreParts[0], move.Axis, turning, side, pivot);
            PlaceSlab(_coreParts[1], move.Axis, lower, side, root);
            PlaceSlab(_coreParts[2], move.Axis, upper, side, root);
            core.gameObject.SetActive(false);
        }

        // Junta o núcleo de novo depois do giro.
        public void JoinCore()
        {
            if (core != null) core.gameObject.SetActive(true);
            if (_coreParts == null) return;
            foreach (var part in _coreParts)
            {
                if (part == null) continue;
                part.SetParent(root, false);
                part.gameObject.SetActive(false);
            }
        }

        private static void PlaceSlab(Transform part, int axis, Vector2 range, float side, Transform parent)
        {
            float length = range.y - range.x;
            part.gameObject.SetActive(length > 0.001f);
            if (length <= 0.001f) return;
            part.SetParent(parent, false);
            var pos = Vector3.zero; pos[axis] = (range.x + range.y) * 0.5f;
            var scale = Vector3.one * side; scale[axis] = length;
            part.localPosition = pos;
            part.localRotation = Quaternion.identity;
            part.localScale = scale;
        }

        // As imagens das peças ficam sempre de pé na tela: quando a vista gira (o Root), cada face gira as suas
        // em quartos de volta. A câmera fica parada com o "para cima" do mundo (CubeCamera.Frame).
        private void LateUpdate()
        {
            if (Geo == null || root == null || _pieces.Length == 0) return;
            var r = root.rotation;
            if (Mathf.Abs(Quaternion.Dot(r, _viewedFrom)) > 0.999999f) return;
            _viewedFrom = r;
            Vector3 up = Quaternion.Inverse(r) * Vector3.up;
            bool changed = false;
            for (int f = 0; f < 6; f++)
            {
                int k = CubeLayout.FaceQuarterTurns(f, up);
                if (k < 0 || k == _faceTurns[f]) continue;
                _faceTurns[f] = k;
                changed = true;
            }
            if (!changed) return;
            for (int c = 0; c < _pieces.Length; c++) _pieces[c].SetFaceTurns(_faceTurns[Geo.CellFace[c]]);
        }

        // Acende a luz sobre uma face (0 a 5). `strength` multiplica o alpha da cor (a respiração vem de fora).
        public void ShowFaceGlow(int face, float strength)
        {
            if (face < 0 || face > 5 || Geo == null || root == null) { HideFaceGlow(); return; }
            if (_glow == null || _glowN != Geo.N) CreateGlow();
            Vector3 n = CubeLayout.Normal(face);
            var t = _glow.transform;
            t.localPosition = n * (Geo.N * 0.5f + CubeLayout.GlowLift);
            t.localRotation = Quaternion.LookRotation(-n, Mathf.Abs(n.y) > 0.5f ? Vector3.forward : Vector3.up);
            float size = Geo.N + 2f * CubeLayout.GlowMargin;
            t.localScale = new Vector3(size, size, 1f);
            var c = faceGlowColor;
            c.a = Mathf.Clamp01(c.a * strength);
            _glow.color = c;
            _glow.enabled = true;
        }

        public void HideFaceGlow()
        {
            if (_glow != null) _glow.enabled = false;
        }

        // A textura do brilho é desenhada aqui, uma vez por tamanho de cubo: não precisa de arte nem de material.
        private void CreateGlow()
        {
            const int size = 64;
            if (_glow != null) Discard(_glow.gameObject);
            _glowN = Geo.N;
            float edge = CubeLayout.GlowEdge(Geo.N);
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "FaceGlow", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear
            };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float a = CubeLayout.GlowAlpha((x + 0.5f) / size * 2f - 1f, (y + 0.5f) / size * 2f - 1f, edge);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            tex.SetPixels32(px);
            tex.Apply();
            var go = new GameObject("FaceGlow") { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(root, false);
            _glow = go.AddComponent<SpriteRenderer>();
            _glow.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            _glow.sortingOrder = 10;              // por cima das imagens das peças
            _glow.enabled = false;
        }

        // Põe de volta no lugar as peças que uma animação deixou no meio do caminho (uma queda interrompida
        // pela troca de batalha, por exemplo). Só as que estão no Root: as de um giro em andamento são do giro.
        public void SnapPiecesHome()
        {
            if (_pieces == null) return;
            for (int c = 0; c < _pieces.Length; c++)
            {
                var t = _pieces[c].transform;
                if (t.parent == root) t.localPosition = CubeLayout.HomePosition(Geo, c);
            }
        }

        // Toda peça está no seu lugar de origem, dentro do Root? Depois de cada giro tem de ser verdade.
        public bool AllAtHome()
        {
            for (int c = 0; c < _pieces.Length; c++)
            {
                var t = _pieces[c].transform;
                if (t.parent != root) return false;
                if ((t.localPosition - CubeLayout.HomePosition(Geo, c)).sqrMagnitude > 1e-6f) return false;
                if (Quaternion.Angle(t.localRotation, CubeLayout.HomeRotation(Geo, c)) > 0.1f) return false;
            }
            return true;
        }

        // Ver o cubo sem dar Play: menu ⋮ do componente no Inspector. As peças da pré-visualização
        // não são salvas na cena (DontSaveInEditor); o Build do jogo e o "Limpar" as removem.
        [ContextMenu("Pré-visualizar 5×5")]
        private void Preview()
        {
            Build(5);
            if (_pieces.Length == 0) return;
            var colors = new sbyte[Geo.Total];
            for (int c = 0; c < colors.Length; c++) colors[c] = (sbyte)Geo.CellFace[c];   // uma cor por face
            Paint(colors);
            foreach (var p in _pieces)
                foreach (var t in p.GetComponentsInChildren<Transform>(true)) t.gameObject.hideFlags = HideFlags.DontSaveInEditor;
        }

        [ContextMenu("Limpar pré-visualização")]
        private void ClearPreview() { Clear(); }

        // Destroy não vale no modo de edição (onde os testes e a pré-visualização rodam); DestroyImmediate vale.
        public static void Discard(Object o)
        {
            if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
        }
    }
}