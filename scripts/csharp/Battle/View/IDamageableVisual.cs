using Fateforged.Session;

namespace Fateforged.View;

/// <summary>
/// Common contract for entity visuals that display HP, react to damage, and die.
/// Implemented by UnitVisual and SummonerVisual.
/// </summary>
public interface IDamageableVisual
{
    void Initialize(IGameSession session, int id);
    void BeginDeath();
    void FlashDamage();
    bool IsAlive { get; }
}
