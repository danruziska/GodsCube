using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cubo.Domain;

namespace Cubo.View
{
    // Um giro de fatia em andamento: as peças da fatia presas ao Pivot do prefab, que gira. Ao terminar, tudo
    // volta para o lugar de origem; quem muda é a cor, repintada logo depois. Só um giro acontece por vez.
    // Com Move.Slice = -1 (Fase 10), todas as peças vão para o pivô: é o cubo inteiro girando (a entrega da face).
    public sealed class SliceTurn
    {
        private readonly CubeView _view;
        private readonly Transform _pivot;
        private readonly List<PieceView> _pieces = new List<PieceView>();
        private readonly Transform _core;             // só no giro do cubo inteiro: o núcleo gira junto

        public Move Move { get; }
        public float Fraction { get; private set; }

        public SliceTurn(CubeView view, Move move)
        {
            _view = view;
            Move = move;
            _pivot = view.Pivot;
            if (_pivot.childCount > 0) Debug.LogError("SliceTurn: começou um giro antes de o anterior terminar");
            _pivot.localRotation = Quaternion.identity;
            var g = view.Geo;
            for (int c = 0; c < g.Total; c++)
            {
                if (move.Slice >= 0 && g.CellPos[c][move.Axis] != move.Slice) continue;   // Slice = -1: o cubo inteiro
                var piece = view.Piece(c);
                piece.transform.SetParent(_pivot, false);
                _pieces.Add(piece);
            }
            if (move.Slice < 0)
            {
                _core = view.Core;
                _core.SetParent(_pivot, false);
            }
            else view.SplitCore(move);                    // a laje de núcleo da fatia gira junto: nenhuma peça some
        }

        // 0 = parado, 1 = 90 graus completos na direção do giro.
        public void SetFraction(float fraction)
        {
            Fraction = fraction;
            _pivot.localRotation = Quaternion.AngleAxis(CubeLayout.SpinAngle(Move.Dir) * fraction, CubeLayout.SpinAxis(Move.Axis));
        }

        // Anima até `to` (0 ou 1). A duração é proporcional ao que falta.
        public IEnumerator Animate(float to, float seconds)
        {
            float from = Fraction;
            float duration = Mathf.Max(0.04f, seconds * Mathf.Abs(to - from));
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                SetFraction(Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / duration)));
                yield return null;
            }
            SetFraction(to);
        }

        // Devolve as peças ao cubo, cada uma no seu lugar de origem, e zera o pivô para o próximo giro.
        public void End()
        {
            var g = _view.Geo;
            foreach (var piece in _pieces)
            {
                piece.transform.SetParent(_view.Root, false);
                piece.transform.localPosition = CubeLayout.HomePosition(g, piece.Cell);
                piece.transform.localRotation = CubeLayout.HomeRotation(g, piece.Cell);
            }
            _pieces.Clear();
            if (_core != null)
            {
                _core.SetParent(_view.Root, false);
                _core.localPosition = Vector3.zero;
                _core.localRotation = Quaternion.identity;
            }
            else _view.JoinCore();
            _pivot.localRotation = Quaternion.identity;
        }
    }
}