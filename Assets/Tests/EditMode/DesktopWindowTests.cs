using NUnit.Framework;
using Cubo.View;

namespace Cubo.Tests
{
    public class DesktopWindowTests
    {
        // The window keeps the phone's 9:16 shape and fits under the taskbar, with room for the title bar.
        [TestCase(1040)] [TestCase(1400)] [TestCase(2120)] [TestCase(500)]
        public void Window_is_nine_by_sixteen_and_fits(int workHeight)
        {
            var (w, h) = DesktopWindow.Size(workHeight);
            Assert.AreEqual(0, h % 16);
            Assert.AreEqual(h * 9 / 16, w, "a altura múltipla de 16 dá uma largura exata");
            if (workHeight > 700) Assert.LessOrEqual(h, workHeight - 48);
            Assert.GreaterOrEqual(h, 640, "nunca menor que 640 de altura");
        }
    }
}
