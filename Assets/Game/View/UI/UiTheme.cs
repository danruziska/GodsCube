using UnityEngine;

namespace Cubo.View
{
    // As cores que a interface escolhe em tempo de jogo (os botões das telas da corrida).
    // É um asset: você ajusta no Inspector, sem recompilar. As outras cores (fundos, botões) ficam nos prefabs.
    [CreateAssetMenu(menuName = "Cubo/UI Theme", fileName = "UiTheme")]
    public sealed class UiTheme : ScriptableObject
    {
        [Tooltip("Botão das telas da Corrida: normal e ligado (herói escolhido).")]
        [SerializeField] private Color button = new Color(0.20f, 0.18f, 0.27f);
        [SerializeField] private Color buttonOn = new Color(0.30f, 0.42f, 0.28f);

        public Color ButtonTint(bool on) => on ? buttonOn : button;
    }
}