using System.Collections.Generic;
using Match3.Core;
using Match3.Data;
using Match3.Grid;

namespace Match3.Match
{
    public interface IMatchFinder
    {
        List<MatchData> FindAllMatches(GridData grid);
        List<MatchData> FindMatchesAt(GridData grid, IEnumerable<GridPosition> positions);
    }
}
