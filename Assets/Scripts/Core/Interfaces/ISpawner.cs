using System;

namespace Match3.Core
{
    public interface ISpawner
    {
        IPiece Spawn(PieceType type, GridPosition position);
        IPiece SpawnRandom(GridPosition position);
        IPiece SpawnRandomNoMatch(GridPosition position, IMatchChecker checker);
        void Despawn(IPiece piece);

        event Action<IPiece> OnPieceSpawned;
    }
}
