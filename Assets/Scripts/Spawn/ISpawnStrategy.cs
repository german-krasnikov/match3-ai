using Match3.Core;
using Match3.Data;
using Match3.Grid;

namespace Match3.Spawn
{
    public interface ISpawnStrategy
    {
        ElementType GetElementType(GridPosition position, GridData grid, GridConfig config);
    }
}
