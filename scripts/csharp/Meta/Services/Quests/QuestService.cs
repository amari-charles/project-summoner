using System;
using Fateforged.Data.Academy;
using Fateforged.Data.Summoners;
using Fateforged.Infrastructure.Persistence;
using Fateforged.Meta.Rewards;
using Fateforged.Meta.Summoner;
using Godot;

namespace Fateforged.Meta.Quests;

[GlobalClass]
public partial class QuestService : Node
{
    public static QuestService? Instance { get; private set; }

    [Signal]
    public delegate void ProgressChangedEventHandler();

    private IProfileRepository? _profileRepo;
    private Func<SummonerId>? _getActiveSummoner;
    private QuestProgressHandler? _quests;

    public override void _Ready()
    {
        Instance = this;
        Initialize(ProfileRepository.Instance);
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    public void InitForTesting(IProfileRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        Initialize(repository);
    }

    public void SetActiveSummonerGetter(Callable getter)
    {
        _getActiveSummoner = () => SummonerId.FromString(getter.Call().AsString());
        InitializeHandler();
    }

    public bool AcceptQuest(string questId)
    {
        var accepted = _quests?.Accept(questId) ?? false;
        if (accepted)
            EmitSignal(SignalName.ProgressChanged);
        return accepted;
    }

    public bool TrackQuest(string questId)
    {
        var tracked = _quests?.Track(questId) ?? false;
        if (tracked)
            EmitSignal(SignalName.ProgressChanged);
        return tracked;
    }

    public Godot.Collections.Dictionary GetJournalState() =>
        _quests?.GetJournalState() ?? [];

    public Godot.Collections.Dictionary GetNpcQuestState(string npcId)
    {
        var state = _quests?.GetNpcState(npcId) ?? [];
        var professor = AcademyProfessorCatalog.Find(ProfessorId.FromString(npcId));
        if (professor != null)
        {
            state["name_key"] = professor.NameKey;
            state["role_key"] = professor.RoleKey;
            state["landmark_key"] = professor.LandmarkKey;
        }
        return state;
    }

    public Godot.Collections.Array<Godot.Collections.Dictionary> GetProfessorQuestStates()
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var professor in AcademyProfessorCatalog.All)
            result.Add(GetNpcQuestState(professor.Id.Value));
        return result;
    }

    public Godot.Collections.Dictionary GetProfessorQuestState(string professorId) =>
        GetNpcQuestState(professorId);

    public Godot.Collections.Dictionary RecordWorldInteraction(string targetId)
    {
        var result = _quests?.RecordWorldInteraction(targetId) ?? [];
        NotifyIfChanged(result);
        return result;
    }

    public Godot.Collections.Dictionary RecordNpcInteraction(string npcId)
    {
        var result = _quests?.RecordNpcInteraction(npcId) ?? [];
        NotifyIfChanged(result);
        return result;
    }

    public Godot.Collections.Dictionary RecordEncounterCompleted(
        string encounterId,
        string outcome
    )
    {
        var result = _quests?.RecordEncounterCompleted(encounterId, outcome) ?? [];
        NotifyIfChanged(result);
        return result;
    }

    private void Initialize(IProfileRepository? repository)
    {
        _profileRepo = repository;
        if (_profileRepo == null)
        {
            GD.PushError("QuestService: ProfileRepository is unavailable");
            return;
        }
        _getActiveSummoner ??= ResolveActiveSummoner;
        InitializeHandler();
    }

    private void InitializeHandler()
    {
        if (_profileRepo == null || _getActiveSummoner == null)
            return;
        var runtime =
            RewardService.Instance?.UniversalRuntime
            ?? (
                _profileRepo is IRewardProfileStore rewardStore
                    ? UniversalRewardRuntime.Create(rewardStore)
                    : UniversalRewardRuntime.CreateUnavailable()
            );
        _quests = new QuestProgressHandler(
            _profileRepo,
            _getActiveSummoner,
            new QuestRewardProcessor(runtime, _getActiveSummoner)
        );
    }

    private SummonerId ResolveActiveSummoner()
    {
        return SummonerId.FromString(
            SummonerSelectionService.Instance?.GetActiveSummonerId() ?? ""
        );
    }

    private void NotifyIfChanged(Godot.Collections.Dictionary result)
    {
        if (result.Count > 0)
            EmitSignal(SignalName.ProgressChanged);
    }
}
