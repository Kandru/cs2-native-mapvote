using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
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
            RegisterListener<Listeners.OnMapEnd>(OnMapEnd);
        }

        public override void OnAllPluginsLoaded(bool hotReload)
        {
            _voteManager = VoteAPI.Get();
        }

        public override void Unload(bool hotReload)
        {
            RemoveListener<Listeners.OnMapEnd>(OnMapEnd);
        }

        private void OnMapEnd()
        {
            // reset
            RtvReset();
        }
    }
}
