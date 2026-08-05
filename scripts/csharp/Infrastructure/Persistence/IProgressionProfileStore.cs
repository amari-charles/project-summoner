using System.Collections.Generic;
using Fateforged.Domain.Profile;
using Fateforged.Meta.Rewards;

namespace Fateforged.Infrastructure.Persistence;

/// <summary>
/// Local durable-store capability used by the local progression authority.
/// The public progression port remains provider-neutral; a future server-backed
/// authority does not need to implement this local persistence detail.
/// </summary>
public interface IProgressionProfileStore : IRewardProfileStore
{
    ProfileData GetProgressionSnapshot();

    bool TryCommitProgression(IReadOnlyList<IRewardGrantMutation> mutations, out string error);
}
