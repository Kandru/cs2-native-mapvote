using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Extensions;
using PanoramaVoteManagerAPI;

namespace NativeMapVote
{
    public partial class NativeMapVote : BasePlugin
    {
        public override string ModuleName => "CS2 NativeMapVote";
        public override string ModuleAuthor => "Jon-Mailes Graeffe <mail@jonni.it> and Kalle <kalle@kandru.de>";

        private static PluginCapability<IPanoramaVoteManagerAPI> VoteAPI { get; } = new("panoramavotemanager:api");
        private IPanoramaVoteManagerAPI? _voteManager;

        public override void Load(bool hotReload)
        {
            RegisterListener<Listeners.OnMapStart>(OnMapStart);
            RegisterListener<Listeners.OnMapEnd>(OnMapEnd);
            RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
            RegisterEventHandler<EventCsIntermission>(OnIntermission, HookMode.Pre);
            if (hotReload)
            {
                LoadWorkshopMaps();
                LoadLocalMaps();
            }
        }

        public override void OnAllPluginsLoaded(bool hotReload)
        {
            _voteManager = VoteAPI.Get();
        }

        public override void Unload(bool hotReload)
        {
            RtvReset();
            NominateReset();
            ChangelevelReset();
            RemoveListener<Listeners.OnMapStart>(OnMapStart);
            RemoveListener<Listeners.OnMapEnd>(OnMapEnd);
            DeregisterEventHandler<EventRoundEnd>(OnRoundEnd);
            DeregisterEventHandler<EventCsIntermission>(OnIntermission, HookMode.Pre);
        }

        private void OnMapStart(string mapName)
        {
            LoadWorkshopMaps();
            LoadLocalMaps();
        }

        private void OnMapEnd()
        {
            RtvReset();
            NominateReset();
            ChangelevelReset();
            MapFeedbackVoteReset();
            // save config to disk
            Config.Update();
        }

        private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
        {
            // check if we need to change the level on round end
            if (Config.ChangelevelOnRoundEnd) DoChangeLevel();
            return HookResult.Continue;
        }

        private HookResult OnIntermission(EventCsIntermission @event, GameEventInfo info)
        {
            // add map to config if not exists
            if (_localMaps.Any(map => map.Equals(Server.MapName, StringComparison.OrdinalIgnoreCase)))
            {
                AddOrUpdateMapConfig(Server.MapName, 0);
            }
            else if (_workshopMaps.Any(map => map.Equals(Server.MapName, StringComparison.OrdinalIgnoreCase)))
            {
                AddOrUpdateMapConfig(Server.MapName, 1);
            }
            // update end match voting
            UpdateEndMatchVoting();
            // initialize feedback vote
            InitializeMapFeedbackVote();
            return HookResult.Continue;
        }
    }
}
