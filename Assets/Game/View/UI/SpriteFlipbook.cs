using UnityEngine;
using UnityEngine.UI;

namespace Cubo.View
{
    // Toca uma sequência de sprites num Image, em loop (o "GIF" do herói). O Unity não lê GIF:
    // os quadros vêm de uma folha de sprites fatiada (Sprite Mode = Multiple), na ordem em que devem tocar.
    // Tempo sem escala: continua tocando com o jogo pausado, como o resto da interface.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public sealed class SpriteFlipbook : MonoBehaviour
    {
        private Image _image;
        private Sprite[] _frames;
        private float _fps;
        private float _start;
        private int _shown = -1;

        public bool Playing => _frames != null && _frames.Length > 1 && _fps > 0f;

        // Menos de dois quadros ou fps zero: mostra o primeiro e fica parado.
        public void Play(Sprite[] frames, float fps)
        {
            if (_image == null) _image = GetComponent<Image>();
            _frames = frames;
            _fps = fps;
            _start = Time.unscaledTime;
            _shown = -1;
            if (frames != null && frames.Length > 0) Show(0);
        }

        public void Stop()
        {
            _frames = null;
            _shown = -1;
        }

        private void Update()
        {
            if (!Playing) return;
            Show(Mathf.FloorToInt((Time.unscaledTime - _start) * _fps) % _frames.Length);
        }

        private void Show(int index)
        {
            if (index == _shown) return;
            _shown = index;
            _image.sprite = _frames[index];
        }
    }
}
