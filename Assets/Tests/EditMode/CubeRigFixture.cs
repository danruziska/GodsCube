using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Cubo.View;

namespace Cubo.Tests
{
    // Os testes usam o prefab de verdade. Se faltar uma referência no Inspector, é aqui que aparece,
    // e não no meio de uma partida.
    public static class CubeRigFixture
    {
        public const string RigPath = "Assets/Game/View/Prefabs/CubeRig.prefab";

        public static CubeView Create()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RigPath);
            Assert.IsNotNull(prefab, "falta o prefab " + RigPath);
            var cube = Object.Instantiate(prefab).GetComponent<CubeView>();
            Assert.IsNotNull(cube, "o prefab CubeRig não tem o componente CubeView na raiz");
            return cube;
        }

        public static void Destroy(CubeView cube)
        {
            if (cube != null) Object.DestroyImmediate(cube.gameObject);
        }
    }
}