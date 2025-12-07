using System;
using UnityEngine;
using Match3.Data;

namespace Match3.Core
{
    public class ScoreCalculator : MonoBehaviour
    {
        public event Action<int> OnScoreCalculated;

        [Header("Base Points")]
        [SerializeField] private int _line3Points = 50;
        [SerializeField] private int _line4Points = 100;
        [SerializeField] private int _line5Points = 200;
        [SerializeField] private int _lShapePoints = 150;
        [SerializeField] private int _tShapePoints = 150;
        [SerializeField] private int _crossPoints = 200;

        [Header("Multipliers")]
        [SerializeField] private float _cascadeMultiplier = 1.5f;

        private int _cascadeLevel;

        public int CascadeLevel => _cascadeLevel;

        public void ResetCascade() => _cascadeLevel = 0;
        public void IncrementCascade() => _cascadeLevel++;

        public int CalculateScore(MatchResult result)
        {
            int baseScore = 0;

            foreach (var match in result.Matches)
                baseScore += GetBasePoints(match.Type);

            float multiplier = Mathf.Pow(_cascadeMultiplier, _cascadeLevel);
            int finalScore = Mathf.RoundToInt(baseScore * multiplier);

            OnScoreCalculated?.Invoke(finalScore);
            return finalScore;
        }

        private int GetBasePoints(MatchType type)
        {
            return type switch
            {
                MatchType.Line3 => _line3Points,
                MatchType.Line4 => _line4Points,
                MatchType.Line5 => _line5Points,
                MatchType.LShape => _lShapePoints,
                MatchType.TShape => _tShapePoints,
                MatchType.Cross => _crossPoints,
                _ => _line3Points
            };
        }
    }
}
