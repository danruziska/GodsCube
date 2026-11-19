using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cubo.View
{
    // Um efeito na barra de vida de um herói (veneno, queimadura e os que vierem). Fica na raiz do prefab StatusIcon;
    // a barra cria uma cópia por efeito ativo, dentro da fila Status, na ordem em que os efeitos começaram.
    // Tocar no ícone abre o balão dos poderes com o detalhe (quanto tira e quantas rodadas faltam).
    // A Image precisa de Raycast Target ligado para receber o toque.
    public sealed class StatusIcon : MonoBehaviour, IPointerClickHandler
    {
        [Tooltip("A Image com o ícone do efeito. A imagem vem da lista Status Art do Battle Stage.")]
        [SerializeField] private Image icon;

        public string Kind { get; private set; }
        public string Detail { get; private set; }       // o texto do balão, já no idioma da tela
        public RectTransform Rect => (RectTransform)transform;

        public event Action<StatusIcon> Tapped;

        public void Show(string kind, Sprite sprite, string detail)
        {
            Kind = kind;
            Detail = detail;
            if (icon == null) return;
            icon.sprite = sprite;
            icon.enabled = sprite != null;
            icon.preserveAspect = true;
        }

        public void OnPointerClick(PointerEventData e) => Tapped?.Invoke(this);

        public string MissingReference() => icon == null ? "icon" : null;
    }
}
