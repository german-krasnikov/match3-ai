// IMatchService.cs
using System.Collections.Generic;
using Features.Board.Models;
using Common;

namespace Features.Board.Services
{
    public interface IMatchService
    {
        /// <summary>
        /// Finds all matches on the board (horizontal + vertical lines of 3+).
        /// Returns unique positions (no duplicates for intersections).
        /// </summary>
        List<GridPosition> FindAllMatches(BoardModel board);

        /// <summary>
        /// Finds matches that include the specified position.
        /// Used after swap to check if swap created a match.
        /// </summary>
        List<GridPosition> FindMatchesAt(BoardModel board, GridPosition pos);

        /// <summary>
        /// Checks if placing element of given type at position would create a match.
        /// Used by SpawnService to avoid initial matches.
        /// </summary>
        bool WouldCreateMatch(BoardModel board, GridPosition pos, ElementType type);
    }
}
