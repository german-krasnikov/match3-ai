using UnityEngine;

namespace Match3.Refill
{
    public readonly struct RefillData
    {
        public Vector2Int TargetPosition { get; }
        public Vector2Int SpawnPosition { get; }
        public Vector3 SpawnWorldPosition { get; }
        public Vector3 TargetWorldPosition { get; }
        public int FallDistance { get; }

        public RefillData(
            Vector2Int targetPosition,
            Vector2Int spawnPosition,
            Vector3 spawnWorldPosition,
            Vector3 targetWorldPosition)
        {
            TargetPosition = targetPosition;
            SpawnPosition = spawnPosition;
            SpawnWorldPosition = spawnWorldPosition;
            TargetWorldPosition = targetWorldPosition;
            FallDistance = spawnPosition.y - targetPosition.y;
        }

        public override string ToString()
            => $"Refill: spawn {SpawnPosition} -> target {TargetPosition} (dist={FallDistance})";
    }
}
