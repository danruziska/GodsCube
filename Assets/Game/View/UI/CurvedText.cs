using TMPro;
using UnityEngine;

namespace Cubo.View
{
    // Curva um texto TextMeshPro em arco, como a fita do banner. A curva é a mesma para qualquer texto:
    // quanto mais largo o texto, mais as pontas sobem em relação ao meio. Cada letra também gira para
    // acompanhar a inclinação. Curvature positiva = pontas para cima (sorriso, como a sua fita);
    // negativa = pontas para baixo; zero = texto reto.
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    public sealed class CurvedText : MonoBehaviour
    {
        [Tooltip("O quanto o arco fecha. 0,001 é um bom ponto de partida; tente de 0,0005 (suave) a 0,003 (forte).")]
        [SerializeField] private float curvature = 0.001f;

        private TMP_Text _text;

        private void OnEnable()
        {
            _text = GetComponent<TMP_Text>();
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
            _text.ForceMeshUpdate();                       // gera o texto de novo; o evento acima aplica a curva
        }

        private void OnDisable()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
            if (_text != null) _text.ForceMeshUpdate();    // volta ao texto reto
        }

        private void OnValidate()
        {
            if (isActiveAndEnabled && _text != null) _text.ForceMeshUpdate();
        }

        private void OnTextChanged(Object changed)
        {
            if (_text == null || changed != _text) return;

            var info = _text.textInfo;
            int count = info.characterCount;
            if (count == 0) return;

            // Os limites do texto visível, para achar o meio e a meia-largura.
            float minX = float.MaxValue, maxX = float.MinValue;
            for (int i = 0; i < count; i++)
            {
                var c = info.characterInfo[i];
                if (!c.isVisible) continue;
                if (c.bottomLeft.x < minX) minX = c.bottomLeft.x;
                if (c.topRight.x > maxX) maxX = c.topRight.x;
            }
            if (maxX <= minX) return;

            float mid = (minX + maxX) * 0.5f;
            float half = (maxX - minX) * 0.5f;
            float meanLift = curvature * half * half / 3f;  // média do arco, para o texto não subir ou descer no conjunto

            for (int i = 0; i < count; i++)
            {
                var c = info.characterInfo[i];
                if (!c.isVisible) continue;

                var verts = info.meshInfo[c.materialReferenceIndex].vertices;
                int v = c.vertexIndex;

                Vector3 center = (verts[v] + verts[v + 2]) * 0.5f;
                float dx = center.x - mid;
                float lift = curvature * dx * dx - meanLift;                     // altura desta letra
                float angle = Mathf.Atan(2f * curvature * dx) * Mathf.Rad2Deg;   // inclinação do arco neste ponto
                var rotation = Quaternion.Euler(0f, 0f, angle);

                for (int j = 0; j < 4; j++)
                {
                    Vector3 p = rotation * (verts[v + j] - center) + center;
                    p.y += lift;
                    verts[v + j] = p;
                }
            }

            _text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
        }
    }
}