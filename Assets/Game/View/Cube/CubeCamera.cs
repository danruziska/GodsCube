using UnityEngine;

namespace Cubo.View
{
    // Enquadra a face da frente do cubo na tela, com margem. A câmera fica no eixo -Z olhando para a origem.
    public static class CubeCamera
    {
        // Distância da câmera ao centro do cubo. Vale a menor das duas medidas (largura ou altura),
        // por isso serve em retrato e em paisagem.
        public static float Distance(int n, float verticalFovDeg, float aspect, float margin = 1.15f)
        {
            float half = n * 0.5f;
            float tanV = Mathf.Tan(verticalFovDeg * 0.5f * Mathf.Deg2Rad);
            float tanMin = Mathf.Min(tanV, tanV * aspect);
            return half + half * margin / tanMin;
        }

        public static void Frame(Camera cam, int n, float margin = 1.15f)
        {
            float d = Distance(n, cam.fieldOfView, cam.aspect, margin);
            cam.transform.position = new Vector3(0f, 0f, -d);
            cam.transform.rotation = Quaternion.identity;
        }
    }
}