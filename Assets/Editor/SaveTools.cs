using System.IO;
using UnityEditor;
using UnityEngine;

namespace Cubo.Build
{
    // Atalhos do editor para o salvamento: apagar a corrida (volta à seleção de heróis), apagar tudo, abrir a pasta.
    // Os arquivos moram em Application.persistentDataPath: corrida.json, colecao.json e ajustes.json.
    public static class SaveTools
    {
        private const string Run = "corrida.json", Collection = "colecao.json", Settings = "ajustes.json";

        [MenuItem("Cubo/Salvamento/Apagar a corrida salva (volta à seleção de heróis)")]
        public static void DeleteRun() => Delete(Run);

        [MenuItem("Cubo/Salvamento/Apagar tudo (corrida, coleção e ajustes)")]
        public static void DeleteAll() => Delete(Run, Collection, Settings);

        [MenuItem("Cubo/Salvamento/Mostrar a pasta do salvamento")]
        public static void Reveal() => EditorUtility.RevealInFinder(Application.persistentDataPath);

        private static void Delete(params string[] names)
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Cubo", "Saia do Play antes de apagar o salvamento.", "OK");
                return;
            }
            int n = 0;
            foreach (var name in names)
            {
                var path = Path.Combine(Application.persistentDataPath, name);
                if (!File.Exists(path)) continue;
                File.Delete(path);
                n++;
            }
            Debug.Log("Cubo: " + n + " arquivo(s) de salvamento apagado(s) em " + Application.persistentDataPath);
        }
    }
}