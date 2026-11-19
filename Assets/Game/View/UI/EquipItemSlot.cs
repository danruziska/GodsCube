using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cubo.View
{
    // Um equipamento na grade do Arsenal. Moldura acesa: é o que o painel do meio mostra. Selo verde: o herói que
    // está sendo equipado o leva. Rostinho no canto: outro herói o leva. Toque: mostra no painel do meio.
    public sealed class EquipItemSlot : MonoBehaviour, IPointerClickHandler
    {
        [Tooltip("A moldura do espaço. A arte (normal ou acesa) vem do EquipScreen.")]
        [SerializeField] private Image frame;
        [Tooltip("O ícone do equipamento, por cima do fundo escuro da moldura.")]
        [SerializeField] private Image icon;
        [Tooltip("O nome do equipamento, mostrado só quando ele ainda não tem ícone.")]
        [SerializeField] private TMP_Text label;
        [Tooltip("O selo redondo do canto de baixo: aparece quando algum herói leva o equipamento.")]
        [SerializeField] private Image owner;
        [Tooltip("O rosto do herói dentro do selo redondo.")]
        [SerializeField] private Image ownerFace;

        public event Action<string> Clicked;
        public string ItemId { get; private set; }
        public bool Owned => owner.gameObject.activeSelf;
        public Image Owner => owner;

        public void Show(string itemId, string itemName, Sprite itemIcon, Sprite frameArt, Color frameTint, Sprite ownerPortrait, bool owned, int iconCanvas = 0)
        {
            ItemId = itemId;
            frame.sprite = frameArt;
            frame.color = frameArt != null ? frameTint : new Color(0.35f, 0.25f, 0.18f, 0.9f);
            EquipScreen.SetIcon(icon, itemIcon, iconCanvas);
            label.text = itemIcon == null ? itemName : "";
            owner.gameObject.SetActive(owned);
            ownerFace.sprite = ownerPortrait;
            ownerFace.preserveAspect = true;
            ownerFace.enabled = ownerPortrait != null;
        }

        public void Click() { if (ItemId != null) Clicked?.Invoke(ItemId); }
        public void OnPointerClick(PointerEventData e) => Click();

        public string MissingReference() =>
            frame == null ? "frame" : icon == null ? "icon" : label == null ? "label" :
            owner == null ? "owner" : ownerFace == null ? "ownerFace" : null;
    }
}
