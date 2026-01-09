// Assets/Scripts/Features/Board/Services/ISpawnService.cs
using Common;

namespace Features.Board.Services
{
    public interface ISpawnService
    {
        ElementType GetRandomElement();
        ElementType GetRandomElementExcluding(ElementType[] excluded);
    }
}
