using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Cubo.View;

namespace Cubo.Tests
{
    // Confere os modelos que vieram do Blender e os prefabs montados com eles. Precisa do UnityEditor.
    public class ModelTests
    {
        private const string PieceFbx = "Assets/Game/Content/Models/Piece.fbx";
        private const string CoreFbx = "Assets/Game/Content/Models/Core.fbx";
        private const string PiecePrefab = "Assets/Game/View/Prefabs/Piece.prefab";
        private const string CorePrefab = "Assets/Game/View/Prefabs/Core.prefab";

        private static Mesh LoadMesh(string path)
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset is Mesh mesh) return mesh;
            Assert.Fail("não achei uma malha em " + path);
            return null;
        }

        private static GameObject LoadPrefab(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(prefab, "falta o prefab " + path);
            return prefab;
        }

        // O lado fino da peça tem de ser o Z: é o eixo que o jogo aponta para fora da face.
        [TestCase(PieceFbx, 0.9f, 0.9f, 0.1f)]
        [TestCase(CoreFbx, 1f, 1f, 1f)]
        public void Model_has_the_right_size_is_centered_and_light(string path, float x, float y, float z)
        {
            var mesh = LoadMesh(path);
            var size = mesh.bounds.size;
            Assert.AreEqual(x, size.x, 0.005f, "tamanho em X");
            Assert.AreEqual(y, size.y, 0.005f, "tamanho em Y");
            Assert.AreEqual(z, size.z, 0.005f, "tamanho em Z");
            Assert.AreEqual(0f, mesh.bounds.center.magnitude, 0.005f, "a origem deve estar no centro");
            Assert.LessOrEqual(mesh.triangles.Length / 3, 300, "triângulos demais para até 384 peças");
        }

        [Test]
        public void Piece_prefab_uses_the_model_at_scale_one_with_a_matching_collider()
        {
            var prefab = LoadPrefab(PiecePrefab);
            var mesh = LoadMesh(PieceFbx);
            Assert.AreEqual(0f, Vector3.Distance(Vector3.one, prefab.transform.localScale), 0.0001f,
                "a escala do prefab deve ser (1,1,1): o tamanho vem da malha");
            Assert.IsNotNull(prefab.GetComponent<PieceView>(), "falta o componente PieceView");
            Assert.AreEqual(mesh, prefab.GetComponent<MeshFilter>().sharedMesh, "o Mesh Filter não usa a malha do Piece.fbx");
            var box = prefab.GetComponent<BoxCollider>();
            Assert.IsNotNull(box, "falta o Box Collider (o toque da Fase 3 precisa dele)");
            Assert.AreEqual(0f, Vector3.Distance(mesh.bounds.size, box.size), 0.02f, "o Box Collider não tem o tamanho da malha");
        }

        [Test]
        public void Core_prefab_uses_the_model_has_a_material_and_no_collider()
        {
            var prefab = LoadPrefab(CorePrefab);
            Assert.AreEqual(0f, Vector3.Distance(Vector3.one, prefab.transform.localScale), 0.0001f, "escala (1,1,1)");
            Assert.AreEqual(LoadMesh(CoreFbx), prefab.GetComponent<MeshFilter>().sharedMesh, "o Mesh Filter não usa a malha do Core.fbx");
            Assert.IsNotNull(prefab.GetComponent<MeshRenderer>().sharedMaterial, "falta o material M_Core");
            Assert.IsNull(prefab.GetComponent<Collider>(), "o núcleo não pode ter colisor: ele roubaria o toque das peças");
        }
    }
}