using UnityEngine;

public static class ClientsManager
{
    private static ClientsListSO _clientsList;

    // Get the clients list from the ClientsListSO asset. The first time this is called, the asset is loaded from the Resources folder.
    // Every other time, the asset is already loaded, so it's just returned.
    public static ClientsListSO ClientsList
    {
        get
        {
            if (_clientsList == null)
            {
                _clientsList = Resources.Load<ClientsListSO>("ClientsList");
                if (_clientsList == null)
                {
                    Debug.LogError("ClientsList not found in Resources folder.");
                }
            }
            return _clientsList;
        }
    }

    public static ClientData GetClientDataByScriptingSymbol(string scriptingSymbol)
    {
        foreach (var client in ClientsList.clients)
        {
            if (client.scriptingSymbol == scriptingSymbol)
            {
                return client;
            }
        }
        return null;
    }

    public static string GetUrlByScriptingSymbol(string scriptingSymbol)
    {
        foreach (var client in ClientsList.clients)
        {
            if (client.scriptingSymbol == scriptingSymbol)
            {
                return client.url;
            }
        }
        return string.Empty;
    }

    public static Sprite GetLogoByScriptingSymbol(string scriptingSymbol)
    {
        foreach (var client in ClientsList.clients)
        {
            if (client.scriptingSymbol == scriptingSymbol)
            {
                return client.logo;
            }
        }
        return null;
    }
}
