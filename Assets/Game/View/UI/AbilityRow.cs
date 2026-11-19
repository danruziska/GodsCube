using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cubo.View
{
    // Uma linha de habilidade (ou da passiva): o ícone, o nome e a descrição.
    // Nos três itens do topo a descrição fica desligada e a linha é tocável (o ClickableArea, filho com Raycast Target ligado,
    // garante a área de toque); a linha selecionada acende o título. Na linha grande do meio a descrição aparece inteira.
    public sealed class AbilityRow : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text description;
        [Tooltip("Um sprite por cor, na ordem da Palette: 0 amarelo, 1 azul, 2 vermelho, 3 marrom, 4 verde, 5 azul-claro. Vazio = quadrado colorido.")]
        [SerializeField] private Sprite[] colorIcons = new Sprite[6];
        [Tooltip("Opcional: o aro do ícone (sprite branco), tingido com a cor do domínio do poder.")]
        [SerializeField] private Image ring;
        [Tooltip("Opcional: a moldura da passiva, um Image irmão do Icon, do mesmo tamanho e logo acima dele na Hierarchy (atrás). Só aparece na passiva.")]
        [SerializeField] private Image frame;
        [Tooltip("Com a moldura, o ícone encolhe para caber dentro dela (1 = mesmo tamanho da moldura).")]
        [SerializeField] private float framedIconScale = 0.5f;

        [Header("Custo")]
        [Tooltip("Opcional: o selo com a mana que o poder custa. Some na passiva, que não tem custo.")]
        [SerializeField] private GameObject costBadge;
        [SerializeField] private TMP_Text costText;

        [Header("Descrição")]
        [Tooltip("Destaca os números da descrição (10, 30%) em negrito e na Number Color. Ligue na linha grande do meio.")]
        [SerializeField] private bool highlightNumbers;
        [SerializeField] private Color numberColor = new Color(0.95f, 0.78f, 0.35f, 1f);

        [Header("Seleção")]
        [Tooltip("A cor do título quando a linha está selecionada. Sem seleção, vale a cor que o título tem no prefab.")]
        [SerializeField] private Color titleSelectedColor = new Color(0.9f, 0.74f, 0.32f, 1f);

        [Tooltip("Opcional: a imagem de fundo que aparece na habilidade selecionada.")]
        [SerializeField] private Image selectionArt;
        [Tooltip("Duração do fade ao selecionar e desmarcar, em segundos.")]
        [SerializeField] private float selectionFade = 0.12f;
        [Tooltip("Duração do fade de entrada quando a linha passa a mostrar outra habilidade (Reveal). 0 = sem fade.")]
        [SerializeField] private float revealSeconds = 0.2f;

        public event Action<AbilityRow> Clicked;

        private Vector2? _iconPos;                        // a posição do Icon no prefab, para encolher pelo centro
        private Sprite _squareSprite;                      // o sprite que o Image tinha no prefab (normalmente nenhum)
        private Color _titleIdleColor = Color.white;

        public string Title => title.text;
        public string Description => description.text;
        public Color IconColor => icon.color;
        public Sprite IconSprite => icon.sprite;

        private void Awake()
        {
            _squareSprite = icon != null ? icon.sprite : null;
            if (icon != null) _iconPos = icon.rectTransform.anchoredPosition;
            if (title != null) _titleIdleColor = title.color;
            if (selectionArt != null) selectionArt.canvasRenderer.SetAlpha(0f);
        }

        public void OnPointerClick(PointerEventData e) => Clicked?.Invoke(this);

        // Ícone, aro e textos surgem do transparente: usado na linha grande quando ela troca de habilidade.
        public void Reveal()
        {
            if (revealSeconds <= 0f || !Application.isPlaying) return;
            FadeIn(icon);
            FadeIn(ring);
            FadeIn(frame);
            FadeIn(title);
            FadeIn(description);
        }

        private void FadeIn(Graphic g)
        {
            if (g == null) return;
            g.canvasRenderer.SetAlpha(0f);
            g.CrossFadeAlpha(1f, revealSeconds, true);
        }

        public void SetSelected(bool selected)
        {
            if (title != null) title.color = selected ? titleSelectedColor : _titleIdleColor;
            if (selectionArt != null) selectionArt.CrossFadeAlpha(selected ? 1f : 0f, selectionFade, true);
        }

        // colorIndex: a cor do poder (0 a 5). A passiva e quem não tem cor passa -1. cost: a mana do poder; 0 esconde o selo.
        public void Show(Color iconColor, int colorIndex, string name, string text, int cost = 0)
        {
            var art = colorIcons != null && colorIndex >= 0 && colorIndex < colorIcons.Length ? colorIcons[colorIndex] : null;
            Show(iconColor, art, name, text, cost);
        }

        // art: o ícone pronto (o de uma passiva, por exemplo). Sem art, o quadrado na cor iconColor.
        // frameArt: a moldura da passiva. Sem ela (os poderes), a moldura some e o ícone volta ao tamanho cheio.
        public void Show(Color iconColor, Sprite art, string name, string text, int cost = 0, Sprite frameArt = null)
        {
            if (frame != null)
            {
                frame.sprite = frameArt;
                frame.color = Color.white;
                frame.preserveAspect = true;
                frame.enabled = frameArt != null;
            }
            float k = frame != null && frameArt != null ? framedIconScale : 1f;
            // Encolhe pelo centro, seja qual for o Pivot do Icon (no prefab ele fica na borda esquerda).
            var rt = icon.rectTransform;
            if (_iconPos == null) _iconPos = rt.anchoredPosition;
            if (frame != null && frameArt != null)
            {
                // A moldura copia o lugar e o tamanho do Icon: numa linha com Icon maior (a do meio), ela acompanha.
                var fr = frame.rectTransform;
                fr.anchorMin = rt.anchorMin; fr.anchorMax = rt.anchorMax; fr.pivot = rt.pivot;
                fr.sizeDelta = rt.sizeDelta; fr.anchoredPosition = _iconPos.Value; fr.localScale = Vector3.one;
            }
            rt.localScale = new Vector3(k, k, 1f);
            rt.anchoredPosition = _iconPos.Value + Vector2.Scale(new Vector2(0.5f, 0.5f) - rt.pivot, rt.rect.size) * (1f - k);
            // Dentro da moldura, o que fica no meio é o centro do desenho (onde está o "peso"), não o centro da imagem:
            // um cálice pesado embaixo com gotas leves em cima sobe até o meio do aro.
            if (frame != null && frameArt != null && art != null)
            {
                // Com Preserve Aspect, o Unity encosta a imagem estreita (ou baixa) no Pivot do Icon, não no meio
                // (aqui o Pivot X é 0: ela ia para a esquerda). Corrige para o meio da caixa, seja qual for o Pivot e o tamanho.
                var box = rt.rect.size;
                rt.anchoredPosition += Vector2.Scale(new Vector2(0.5f, 0.5f) - rt.pivot, box - DrawnSize(art, box)) * k;
                rt.anchoredPosition -= VisualOffset(art, box) * k;
            }
            if (art != null)
            {
                icon.sprite = art;
                icon.color = Color.white;                  // branco: o sprite aparece com as cores dele
                icon.preserveAspect = true;
            }
            else
            {
                icon.sprite = _squareSprite;
                icon.color = iconColor;
            }
            if (ring != null) ring.color = iconColor;
            if (costBadge != null) costBadge.SetActive(cost > 0);
            if (costText != null) costText.text = cost > 0 ? cost.ToString() : "";
            title.text = name;
            description.text = highlightNumbers ? RichNumbers.Highlight(text, numberColor) : text;
        }

        public void Show(Color iconColor, string name, string text) => Show(iconColor, -1, name, text);

        // Quanto o centro do desenho está fora do centro da imagem, já no tamanho em que ela aparece dentro de `box`
        // (Preserve Aspect). O desenho vem da malha do sprite: com Mesh Type = Tight ela contorna só a parte pintada, e o
        // centro é o das áreas dos triângulos. Com Full Rect a malha é o retângulo todo: o centro é o da imagem (zero).
        private static readonly System.Collections.Generic.Dictionary<Sprite, Vector2> s_centres =
            new System.Collections.Generic.Dictionary<Sprite, Vector2>();

        public static Vector2 VisualOffset(Sprite sprite, Vector2 box)
        {
            if (sprite == null || box.x <= 0f || box.y <= 0f) return Vector2.zero;
            if (!s_centres.TryGetValue(sprite, out var frac))
            {
                frac = Vector2.zero;
                var v = sprite.vertices; var tri = sprite.triangles; var b = sprite.bounds;
                double area = 0, cx = 0, cy = 0;
                for (int i = 0; i + 2 < tri.Length; i += 3)
                {
                    Vector2 p = v[tri[i]], q = v[tri[i + 1]], r = v[tri[i + 2]];
                    double a = Mathf.Abs((q.x - p.x) * (r.y - p.y) - (r.x - p.x) * (q.y - p.y)) * 0.5;
                    area += a; cx += a * (p.x + q.x + r.x) / 3.0; cy += a * (p.y + q.y + r.y) / 3.0;
                }
                if (area > 0 && b.size.x > 0f && b.size.y > 0f)
                    frac = new Vector2((float)((cx / area - b.center.x) / b.size.x), (float)((cy / area - b.center.y) / b.size.y));
                s_centres[sprite] = frac;
            }
            return Vector2.Scale(frac, DrawnSize(sprite, box));
        }

        // O tamanho em que o sprite aparece dentro de `box` com Preserve Aspect (encaixado, sem esticar).
        public static Vector2 DrawnSize(Sprite sprite, Vector2 box)
        {
            if (sprite == null || box.x <= 0f || box.y <= 0f) return box;
            float aspect = sprite.rect.width / Mathf.Max(1f, sprite.rect.height);
            return box.x / box.y > aspect ? new Vector2(box.y * aspect, box.y) : new Vector2(box.x, box.x / aspect);
        }

        public string MissingReference() =>
            icon == null ? "icon" : title == null ? "title" : description == null ? "description" : null;
    }
}