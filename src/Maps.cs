using CounterStrikeSharp.API.Modules.Cvars;
using NativeMapVote.Services;
using NativeMapVote.State;

namespace NativeMapVote
{
    public partial class NativeMapVote
    {
        private readonly List<string> _localMaps = [];
        private readonly List<string> _workshopMaps = [];
        private MapLoader? _mapLoader;

        private void InitializeMapLoader()
        {
            _mapLoader ??= new MapLoader();
        }

        private void LoadLocalMaps()
        {
            InitializeMapLoader();
            if (!TryGetRconCredentials(out int port, out string password))
            {
                return;
            }

            _ = Task.Run(() =>
            {
                _mapLoader!.LoadMaps(
                    _localMaps,
                    port,
                    password,
                    "maps *",
                    0,
                    AddMapConfig,
                    OnLocalMapsLoaded,
                    line => !line.Contains('\\') &&
                            !line.Contains('/') &&
                            !line.Contains("lobby") &&
                            !line.Contains("error") &&
                            !line.Contains("workshop") &&
                            !line.Contains("graphics_settings") &&
                            !line.EndsWith("_vanity")
                );
            });
        }

        private void LoadWorkshopMaps()
        {
            InitializeMapLoader();
            if (!TryGetRconCredentials(out int port, out string password))
            {
                return;
            }

            _ = Task.Run(() =>
            {
                _mapLoader!.LoadMaps(
                    _workshopMaps,
                    port,
                    password,
                    "print_mapgroup_sv",
                    1,
                    AddMapConfig,
                    OnWorkshopMapsLoaded,
                    line => !line.Contains(':') &&
                            !line.Contains("No maps in mapgroup map list")
                );
            });
        }

        private bool TryGetRconCredentials(out int port, out string password)
        {
            var cvarHostPort = ConVar.Find("hostport");
            var cvarRconPassword = ConVar.Find("rcon_password");

            port = 0;
            password = "";

            if (cvarHostPort == null || cvarRconPassword == null)
            {
                return false;
            }

            port = cvarHostPort.GetPrimitiveValue<int>();
            password = cvarRconPassword.StringValue;
            return !string.IsNullOrEmpty(password);
        }

        private void OnLocalMapsLoaded(int count, string status)
        {
            if (status == "success")
            {
                DebugPrint(Localizer["localmaps.success"].Value
                    .Replace("{amount}", count.ToString()));
            }
            else if (status != "response_empty")
            {
                DebugPrint(Localizer["localmaps.error"].Value
                    .Replace("{error}", status));
            }
        }

        private void OnWorkshopMapsLoaded(int count, string status)
        {
            if (status == "success")
            {
                DebugPrint(Localizer["workshop.success"].Value
                    .Replace("{amount}", count.ToString()));
            }
            else if (status != "response_empty")
            {
                DebugPrint(Localizer["workshop.error"].Value
                    .Replace("{error}", status));
            }
        }
    }
}
