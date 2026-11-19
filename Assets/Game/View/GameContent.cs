using System;
using UnityEngine;
using Cubo.Data;

namespace Cubo.View
{
    [CreateAssetMenu(menuName = "Cubo/Game Content", fileName = "GameContent")]
    public sealed class GameContent : ScriptableObject, IContentSource
    {
        [SerializeField] private TextAsset[] files;

        public string Read(string name)
        {
            foreach (var f in files)
                if (f != null && f.name == name) return f.text;
            throw new ArgumentException("Arquivo de conteúdo não encontrado: " + name);
        }
    }
}