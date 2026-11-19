using UnityEngine;

namespace Cubo.View
{
    // A entrada em cascata de um grupo: os filhos ligados que têm UiBounce crescem um depois do outro.
    // Vai no pai do grupo (o Content da grade, a linha do time, a coluna de habilidades); quem chama Play é a tela,
    // quando ela aparece. Filhos sem UiBounce entram sem animar.
    public sealed class UiEnter : MonoBehaviour
    {
        [Tooltip("Segundos entre um filho e o próximo.")]
        [SerializeField] private float stagger = 0.04f;

        // delay: espera antes do primeiro filho (para um grupo entrar depois do outro).
        public void Play(float delay)
        {
            int n = 0;
            foreach (Transform child in transform)
            {
                if (!child.gameObject.activeSelf) continue;
                var bounce = child.GetComponent<UiBounce>();
                if (bounce == null) continue;
                bounce.Pop(delay + stagger * n++);
            }
        }
    }
}
