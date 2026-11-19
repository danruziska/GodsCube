using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Cubo.View
{
    // Um toque numa figura do palco (o BattleStage põe sozinho nas figuras do rival). A Image da figura precisa de
    // Raycast Target ligado para receber o toque.
    public sealed class StageTap : MonoBehaviour, IPointerClickHandler
    {
        public int Index;
        public event Action<int> Tapped;
        public void OnPointerClick(PointerEventData e) => Tapped?.Invoke(Index);
    }
}
