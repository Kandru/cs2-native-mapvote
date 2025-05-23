using CounterStrikeSharp.API.Modules.Cvars;

namespace NativeMapVote
{
    public partial class NativeMapVote
    {
        private readonly List<string> _localMaps = [];

        private void LoadLocalMaps()
        {
            ConVar? cvarHostPort = ConVar.Find("hostport");
            ConVar? cvarRconPassword = ConVar.Find("rcon_password");
            if (cvarHostPort == null || cvarRconPassword == null)
            {
                return;
            }

            _localMaps.Clear();
            _ = Task.Run(async () =>
            {
                RCONClient client = new(
                    "127.0.0.1",
                    cvarHostPort.GetPrimitiveValue<int>(),
                    cvarRconPassword.StringValue,
                    1000);
                try
                {
                    client.Connect();
                    string response = client.SendCommand("maps *");
                    // check for local maps
                    if (string.IsNullOrEmpty(response))
                    {
                        return;
                    }

                    string[] lines = response.Split('\n');
                    foreach (string line in lines)
                    {
                        if (line.Length == 0
                            || line.Contains('\\')
                            || line.Contains('/')
                            || line.Contains("lobby")
                            || line.Contains("error")
                            || line.Contains("workshop")
                            || line.Contains("graphics_settings")
                            || line.EndsWith("_vanity"))
                        {
                            continue;
                        }

                        _localMaps.Add(line.Trim().ToLower(System.Globalization.CultureInfo.CurrentCulture));
                        AddMapConfig(line.Trim().ToLower(System.Globalization.CultureInfo.CurrentCulture), 0);
                    }
                    if (_localMaps.Count == 0)
                    {
                        DebugPrint(Localizer["localmaps.error"].Value
                            .Replace("{error}", "No local maps found"));
                    }
                    else
                    {
                        DebugPrint(Localizer["localmaps.success"].Value
                            .Replace("{amount}", _localMaps.Count.ToString()));
                    }
                }
                catch (Exception ex)
                {
                    DebugPrint(Localizer["localmaps.error"].Value
                        .Replace("{error}", ex.Message));
                }
                finally
                {
                    client.Close();
                }
                await Task.CompletedTask;
            });
        }
    }
}
