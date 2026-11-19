using System;
using UnityEngine;
using Cubo.App;

namespace Cubo.View
{
    // Um botão de poder: o nome, uma barra de mana na cor do domínio e o detalhe ao segurar.
    // Variante do prefab TextButton, com uma FillBar e um HoldDetail a mais.
    [RequireComponent(typeof(TextButton), typeof(HoldDetail))]
    public sealed class PowerButton : MonoBehaviour
    {
        [SerializeField] private FillBar mana;

        public event Action Pressed;                // tocou para usar o poder
        public event Action<string> DetailChanged;  // o texto do detalhe; null esconde

        private TextButton _button;
        private HoldDetail _hold;
        private string _detail;
        private bool _wired;

        public TextButton Button => _button != null ? _button : (_button = GetComponent<TextButton>());
        private HoldDetail Hold => _hold != null ? _hold : (_hold = GetComponent<HoldDetail>());

        private void Awake() => Wire();

        public void Show(PowerView power, Color domainColor)
        {
            Wire();
            _detail = power.Detail;
            Button.Text = power.Name;
            Button.Interactable = power.Enabled;
            mana.Set(power.Fill);
            mana.Color = domainColor;
        }

        // Só a barra, em tempo real, enquanto o lote toca; o resto (texto, ativo) é relido ao fim.
        public void SetFill(float fill) => mana.Set(fill);

        private void Wire()
        {
            if (_wired) return;
            _wired = true;
            Button.OnClick(OnClick);
            Hold.Showing += on => DetailChanged?.Invoke(on ? _detail : null);
        }

        private void OnClick()
        {
            if (Hold.ConsumeHold()) return;          // foi uma segurada para ler o detalhe
            Pressed?.Invoke();
        }

        public string MissingReference()
        {
            if (mana == null) return "mana";
            if (mana.MissingReference() is string m) return "mana." + m;
            return Button == null ? "TextButton" : Button.MissingReference();
        }
    }
}