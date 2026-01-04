using CounterStrikeSharp.API.Modules.Cvars;

namespace NativeMapVote
{
    public partial class NativeMapVote
    {
        private readonly List<string> _workshopMaps = [];

        private void LoadWorkshopMaps()
        {
            ConVar? cvarHostPort = ConVar.Find("hostport");
            ConVar? cvarRconPassword = ConVar.Find("rcon_password");
            if (cvarHostPort == null || cvarRconPassword == null)
            {
                return;
            }
            // delete old maps
            _workshopMaps.Clear();
            // get values first to avoid non-main thread error
            int port = cvarHostPort.GetPrimitiveValue<int>();
            string password = cvarRconPassword.StringValue;
            _ = Task.Run(async () =>
            {
                RCONClient client = new(
                    "127.0.0.1",
                    port,
                    password,
                    1000);
                try
                {
                    client.Connect();
                    string response = client.SendCommand("print_mapgroup_sv");
                    // check for workshop maps
                    if (string.IsNullOrEmpty(response))
                    {
                        return;
                    }

                    string[] lines = response.Split('\n');
                    foreach (string line in lines)
                    {
                        if (line.Length == 0
                            || line.Contains(':')
                            || line.Contains("No maps in mapgroup map list"))
                        {
                            continue;
                        }

                        _workshopMaps.Add(line.Trim().ToLower(System.Globalization.CultureInfo.CurrentCulture));
                        AddMapConfig(line.Trim().ToLower(System.Globalization.CultureInfo.CurrentCulture), 1);
                    }
                    if (_workshopMaps.Count == 0)
                    {
                        DebugPrint(Localizer["workshop.error"].Value
                            .Replace("{error}", "No workshop maps found"));
                    }
                    else
                    {
                        DebugPrint(Localizer["workshop.success"].Value
                            .Replace("{amount}", _workshopMaps.Count.ToString()));
                    }
                }
                catch (Exception ex)
                {
                    DebugPrint(Localizer["workshop.error"].Value
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
