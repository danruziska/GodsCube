using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cubo.View
{
    // Um herói do time no Arsenal: o retrato na moldura e, embaixo, o soquete com o equipamento que ele leva.
    // Toque (no retrato ou no soquete): este herói passa a ser o equipado.
    public sealed class EquipHeroSlot : MonoBehaviour, IPointerClickHandler
    {
        [Tooltip("A moldura do retrato. A arte (normal ou acesa) vem do EquipScreen.")]
        [SerializeField] private Image frame;
        [Tooltip("O retrato do herói, desenhado por cima do fundo escuro da moldura.")]
        [SerializeField] private Image art;
        [Tooltip("O nome do herói, mostrado só quando ele ainda não tem retrato.")]
        [SerializeField] private TMP_Text label;
        [Tooltip("O soquete redondo embaixo do retrato.")]
        [SerializeField] private Image socket;
        [Tooltip("O ícone do equipamento dentro do soquete.")]
        [SerializeField] private Image socketIcon;

        public event Action<string> Clicked;
        public string HeroId { get; private set; }
        public Image Socket => socket;

        public void Show(string heroId, string heroName, Sprite portrait, Sprite frameArt, Color frameTint, Sprite itemIcon, int iconCanvas = 0)
        {
            HeroId = heroId;
            frame.sprite = frameArt;
            frame.color = frameArt != null ? frameTint : new Color(0.35f, 0.25f, 0.18f, 0.9f);
            art.sprite = portrait;
            art.preserveAspect = true;
            art.enabled = portrait != null;
            label.text = heroId != null && portrait == null ? heroName : "";
            EquipScreen.SetIcon(socketIcon, itemIcon, iconCanvas);
        }

        public void Click() { if (HeroId != null) Clicked?.Invoke(HeroId); }
        public void OnPointerClick(PointerEventData e) => Click();

        public string MissingReference() =>
            frame == null ? "frame" : art == null ? "art" : label == null ? "label" : socket == null ? "socket" :
            socketIcon == null ? "socketIcon" : null;
    }
}
