using UnityEngine;

namespace Cubo.View
{
    // Uma peça do cubo na tela. Não conhece regra nenhuma.
    public sealed class PieceView : MonoBehaviour
    {
        public int Cell;                    // a peça do domínio que ela representa

        [Tooltip("Filho com SpriteRenderer: a imagem da frente da peça (o símbolo do elemento em alto-relevo). " +
                 "Vazio: a peça fica só com a cor do material.")]
        [SerializeField] private SpriteRenderer face;

        private Renderer _renderer;
        private SpriteRenderer _overlay;            // o brilho do veneno ou do fogo por cima da peça (criado na hora)
        private MaterialPropertyBlock _block;
        private float _brightness = 1f;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        // `color` vai de 0 a 6; negativo (casa vazia) esconde a peça.
        // `faces` (opcional) traz a imagem da frente de cada cor, na ordem da paleta; uma cor sem imagem mostra só o material.
        // As laterais finas da placa ficam com a cor do material.
        public void SetColor(int color, Material[] palette, Sprite[] faces = null, float faceSize = CubeLayout.PieceSize, int quarterTurns = 0)
        {
            if (_renderer == null) _renderer = GetComponent<Renderer>();
            bool visible = color >= 0 && color < palette.Length;
            _renderer.enabled = visible;
            if (visible) _renderer.sharedMaterial = palette[color];
            ApplyBrightness();

            if (face == null) return;
            Sprite sprite = visible && faces != null && color < faces.Length ? faces[color] : null;
            face.enabled = sprite != null;
            if (sprite == null) return;
            face.sprite = sprite;
            var t = face.transform;
            t.localPosition = new Vector3(0f, 0f, CubeLayout.FaceLift);
            t.localRotation = CubeLayout.FaceRotationTurned(quarterTurns);
            t.localScale = CubeLayout.FaceScale(sprite.bounds.size, new Vector2(faceSize, faceSize));
        }

        // Gira a imagem da frente no plano da peça, para ela ficar de pé na tela (CubeView decide quanto).
        public void SetFaceTurns(int quarterTurns)
        {
            if (face != null) face.transform.localRotation = CubeLayout.FaceRotationTurned(quarterTurns);
        }

        // Pinta a casa com a cor de uma área de veneno ou de fogo (alpha 0 apaga). O brilho é um quadrado do tamanho
        // da casa inteira, para as casas vizinhas da área formarem uma mancha só. Criado aqui, não precisa de prefab.
        public void SetOverlay(Sprite tile, Color color)
        {
            if (color.a <= 0f || tile == null)
            {
                if (_overlay != null) _overlay.enabled = false;
                return;
            }
            if (_overlay == null)
            {
                var go = new GameObject("Hazard") { hideFlags = HideFlags.DontSave };
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(0f, 0f, CubeLayout.FaceLift + 0.01f);
                go.transform.localRotation = CubeLayout.FaceRotationTurned(0);
                _overlay = go.AddComponent<SpriteRenderer>();
                _overlay.sortingOrder = 6;             // por cima da imagem da peça e da moldura do altar
            }
            _overlay.sprite = tile;
            _overlay.transform.localScale = CubeLayout.FaceScale(tile.bounds.size, Vector2.one);   // a casa inteira (1 x 1)
            _overlay.color = color;
            _overlay.enabled = true;
        }

        // Escurece a peça inteira (a imagem e as laterais coloridas): 1 = normal, 0,45 = apagada.
        // Fica valendo quando a peça é repintada, até voltar a 1.
        public void SetBrightness(float brightness)
        {
            _brightness = Mathf.Clamp01(brightness);
            ApplyBrightness();
        }

        private void ApplyBrightness()
        {
            if (_renderer == null) _renderer = GetComponent<Renderer>();
            var shade = new Color(_brightness, _brightness, _brightness, 1f);
            if (face != null) face.color = shade;
            if (_brightness >= 1f || _renderer.sharedMaterial == null)
            {
                _renderer.SetPropertyBlock(null);
                return;
            }
            if (_block == null) _block = new MaterialPropertyBlock();
            _block.SetColor(BaseColorId, _renderer.sharedMaterial.color * shade);
            _renderer.SetPropertyBlock(_block);
        }
    }
}
