using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Cubo.View
{
    // Deixa o cubo 3D com cara de pixel art: a câmera desenha em resolução baixa e o URP amplia com filtro Point,
    // então cada "pixel" do cubo vira um bloco do mesmo tamanho dos pixels da arte da interface (PixelScale).
    // Mexe no Render Scale e no Upscaling Filter do URP Asset ativo, que valem para todas as câmeras 3D.
    // A interface (Canvas Overlay) não passa por isso e continua nítida. Ao desligar, devolve os valores de antes;
    // sem isso, sair do Play no editor deixaria o asset alterado.
    public sealed class PixelatedRender : MonoBehaviour
    {
        [Tooltip("Tamanho de um pixel do cubo, em unidades da resolução de referência. Mesmo número do PixelScale da interface.")]
        [SerializeField, Min(1)] private int artPixel = 6;
        [Tooltip("Canvas da interface, para ler a escala dele. Vazio: calcula pela largura da tela (referência 1080).")]
        [SerializeField] private Canvas canvas;
        [Tooltip("Desliga o anti-aliasing (MSAA): borda serrilhada, como pixel art desenhada à mão.")]
        [SerializeField] private bool hardEdges = true;

        private UniversalRenderPipelineAsset _asset;
        private float _scale;
        private UpscalingFilterSelection _filter;
        private int _msaa;
        private float _canvasScale = -1f;

        private void OnEnable()
        {
            _asset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (_asset == null)
            {
                Debug.LogWarning("PixelatedRender: o pipeline ativo não é URP; o cubo fica como está.");
                enabled = false;
                return;
            }
            _scale = _asset.renderScale;
            _filter = _asset.upscalingFilter;
            _msaa = _asset.msaaSampleCount;
            _canvasScale = -1f;
            _asset.upscalingFilter = UpscalingFilterSelection.Point;
            if (hardEdges) _asset.msaaSampleCount = 1;
        }

        private void OnDisable()
        {
            if (_asset == null) return;
            _asset.renderScale = _scale;
            _asset.upscalingFilter = _filter;
            _asset.msaaSampleCount = _msaa;
        }

        // Escala do Canvas muda com a tela (girar o aparelho, trocar a janela do editor): recalcula.
        private void LateUpdate()
        {
            float canvasScale = canvas != null ? canvas.scaleFactor : Screen.width / 1080f;
            if (Mathf.Approximately(canvasScale, _canvasScale)) return;
            _canvasScale = canvasScale;
            _asset.renderScale = UiMath.PixelatedRenderScale(artPixel, canvasScale);
        }
    }
}
