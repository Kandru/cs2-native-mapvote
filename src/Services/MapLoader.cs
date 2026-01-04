namespace NativeMapVote.Services
{
    public class MapLoader
    {
        public void LoadMaps(
            List<string> mapList,
            int port,
            string password,
            string command,
            int mapType,
            Action<string, int> onMapLoaded,
            Action<int, string> onComplete,
            Func<string, bool> filterPredicate)
        {
            mapList.Clear();

            using (RCONClient client = new("127.0.0.1", port, password, 1000))
            {
                try
                {
                    client.Connect();
                    string response = client.SendCommand(command);

                    if (string.IsNullOrEmpty(response))
                    {
                        onComplete(0, "response_empty");
                        return;
                    }

                    string[] lines = response.Split('\n');
                    foreach (string line in lines)
                    {
                        if (line.Length == 0 || !filterPredicate(line))
                        {
                            continue;
                        }

                        string mapName = line.Trim().ToLower(System.Globalization.CultureInfo.CurrentCulture);
                        mapList.Add(mapName);
                        onMapLoaded(mapName, mapType);
                    }

                    onComplete(mapList.Count, "success");
                }
                catch (Exception ex)
                {
                    onComplete(0, ex.Message);
                }
            }
        }
    }
}
