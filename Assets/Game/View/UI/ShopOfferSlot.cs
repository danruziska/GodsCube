using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Cubo.App;

namespace Cubo.View
{
    // Uma oferta na prateleira da loja (fica na raiz do prefab ShopOffer): a moldura, o ícone (ou o rosto do herói, nas
    // melhorias de habilidade), a etiqueta de preço e os selos de promoção e de vendido. Tocar avisa a ShopScreen.
    // A moldura precisa de Raycast Target ligado para receber o toque.
    public sealed class ShopOfferSlot : MonoBehaviour, IPointerClickHandler
    {
        [Tooltip("A moldura do espaço. Fica tingida (Focus Tint da ShopScreen) quando é a oferta em foco.")]
        [SerializeField] private Image frame;
        [Tooltip("O ícone da runa, do item, da poção ou da troca.")]
        [SerializeField] private Image icon;
        [Tooltip("Opcional: o rosto do herói, nas melhorias de habilidade (no lugar do ícone).")]
        [SerializeField] private Image portrait;
        [Tooltip("Opcional: o nível da melhoria (\"++\", \"★\") num canto.")]
        [SerializeField] private TMP_Text tierTag;
        [Tooltip("O preço na etiqueta.")]
        [SerializeField] private TMP_Text price;
        [Tooltip("Opcional: o preço antigo, riscado, aparece só na promoção.")]
        [SerializeField] private TMP_Text oldPrice;
        [Tooltip("Opcional: o selo de promoção (uma fita ou estrela), aceso só na oferta em promoção.")]
        [SerializeField] private GameObject saleBadge;
        [Tooltip("Opcional: o carimbo VENDIDO, aceso quando a oferta já foi comprada.")]
        [SerializeField] private GameObject soldStamp;
        [Tooltip("Opcional: a etiqueta de preço inteira (some quando vendido).")]
        [SerializeField] private GameObject priceTag;
        [Tooltip("A transparência do ícone de uma oferta vendida.")]
        [Range(0f, 1f)] [SerializeField] private float soldAlpha = 0.35f;

        public string Id { get; private set; }
        public event Action<string> Clicked;

        private bool _wasSold;

        public void Show(ShopOfferView o, Sprite iconSprite, Sprite portraitSprite, Color frameTint, Color priceColor, int iconCanvas)
        {
            bool fresh = Id != o.Id;
            Id = o.Id;
            bool face = portrait != null && portraitSprite != null;
            if (portrait != null)
            {
                portrait.sprite = portraitSprite;
                portrait.enabled = face;
                portrait.preserveAspect = true;
                portrait.color = new Color(1f, 1f, 1f, o.Sold ? soldAlpha : 1f);
            }
            EquipScreen.SetIcon(icon, face ? null : iconSprite, iconCanvas);
            icon.color = new Color(1f, 1f, 1f, o.Sold ? soldAlpha : 1f);
            if (frame != null) frame.color = frameTint;
            if (tierTag != null) tierTag.text = o.Tag ?? "";
            price.text = o.Price.ToString();
            price.color = priceColor;
            if (oldPrice != null)
            {
                bool sale = o.OnSale && o.FullPrice > o.Price;
                oldPrice.gameObject.SetActive(sale);
                oldPrice.text = sale ? "<s>" + o.FullPrice + "</s>" : "";
            }
            if (saleBadge != null) saleBadge.SetActive(o.OnSale);
            if (soldStamp != null) soldStamp.SetActive(o.Sold && o.Kind != "reroll");
            if (priceTag != null) priceTag.SetActive(!o.Sold);
            if (!fresh && o.Sold && !_wasSold) Bounce();             // acabou de ser comprada
            _wasSold = o.Sold;
        }

        // Um pulinho na compra.
        public void Bounce()
        {
            if (!Application.isPlaying) return;
            transform.DOKill(true);
            transform.DOPunchScale(Vector3.one * 0.12f, 0.25f, 6, 0.6f).SetUpdate(true).SetLink(gameObject);
        }

        public void OnPointerClick(PointerEventData e) => Clicked?.Invoke(Id);

        public string MissingReference() => icon == null ? "icon" : price == null ? "price" : null;
    }
}
