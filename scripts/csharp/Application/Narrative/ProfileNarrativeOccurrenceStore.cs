namespace Fateforged.Application.Narrative;

using System.Collections.Generic;
using Fateforged.Data.Summoners;
using Fateforged.Domain.Profile.Account;
using Fateforged.Infrastructure.Persistence;

public sealed class ProfileNarrativeOccurrenceStore : INarrativeOccurrenceStore
{
    private readonly IProfileRepository _profiles;
    private readonly HashSet<string> _attemptFlags = [];

    public ProfileNarrativeOccurrenceStore(IProfileRepository profiles) => _profiles = profiles;

    public bool HasCompleted(string cueId, NarrativeOccurrencePolicy policy, string scopeId) =>
        policy switch
        {
            NarrativeOccurrencePolicy.Always => false,
            NarrativeOccurrencePolicy.OncePerAttempt => _attemptFlags.Contains(Key(cueId, scopeId)),
            NarrativeOccurrencePolicy.OncePerSummoner =>
                Summoner(scopeId)?.NarrativeFlags.GetValueOrDefault(cueId) == true,
            NarrativeOccurrencePolicy.OncePerAccount =>
                _profiles.GetProfileMetadata()?.Meta.NarrativeFlags.GetValueOrDefault(cueId) == true,
            _ => false,
        };

    public void MarkCompleted(string cueId, NarrativeOccurrencePolicy policy, string scopeId)
    {
        switch (policy)
        {
            case NarrativeOccurrencePolicy.Always:
                return;
            case NarrativeOccurrencePolicy.OncePerAttempt:
                _attemptFlags.Add(Key(cueId, scopeId));
                return;
            case NarrativeOccurrencePolicy.OncePerSummoner:
                var summoner = Summoner(scopeId);
                if (summoner == null)
                    return;
                summoner.NarrativeFlags[cueId] = true;
                _profiles.SaveSummonerInstance(summoner);
                return;
            case NarrativeOccurrencePolicy.OncePerAccount:
                _profiles.UpdateProfileMeta(
                    new MetaUpdate { NarrativeFlags = new Dictionary<string, bool> { [cueId] = true } }
                );
                return;
        }
    }

    public void ResetAttempt() => _attemptFlags.Clear();

    private Fateforged.Domain.Profile.Summoners.SummonerInstance? Summoner(string scopeId)
    {
        var id = string.IsNullOrWhiteSpace(scopeId)
            ? _profiles.GetProfileMetadata()?.Meta.SelectedSummoner ?? ""
            : scopeId;
        return string.IsNullOrWhiteSpace(id)
            ? null
            : _profiles.GetSummonerInstance(SummonerId.FromString(id));
    }

    private static string Key(string cueId, string scopeId) => $"{scopeId}:{cueId}";
}
