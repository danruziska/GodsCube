using UnityEngine;

namespace Cubo.View
{
    // The game runs in a tall 9:16 window, as big as the monitor's free height allows. The whole UI is
    // laid out for a 1080x1920 portrait screen, so a landscape window or full screen would cut it off.
    // In the editor this does nothing: set the Game view to a portrait resolution instead.
    public static class DesktopWindow
    {
        public const float Aspect = 9f / 16f;
        private const int TitleBar = 48;          // room for the window's title bar and border
        private const int MinHeight = 640;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Open()
        {
#if UNITY_STANDALONE && !UNITY_EDITOR
            var (w, h) = Size(Screen.mainWindowDisplayInfo.workArea.height);
            Screen.SetResolution(w, h, FullScreenMode.Windowed);
#endif
        }

        // The window for a monitor whose free height (without the taskbar) is workHeight pixels.
        public static (int width, int height) Size(int workHeight)
        {
            int h = Mathf.Max(MinHeight, workHeight - TitleBar);
            h -= h % 16;                           // a multiple of 16 keeps the width a whole number
            return (Mathf.RoundToInt(h * Aspect), h);
        }
    }
}
