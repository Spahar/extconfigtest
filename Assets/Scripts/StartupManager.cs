using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartupManager : MonoBehaviour
{
    public static string BaseDomain { get; private set; } = @"http://www.spahar.me/";
    public static string BaseApiUrl => $"{BaseDomain}/api";

    private const string configFileName = "config.txt";

    private void Awake()
    {
#if CLIENT_0
        BaseDomain = @"http://www.spahar.me/d1";
#elif CLIENT_1
        BaseDomain = @"http://www.spahar.me/d2";
#else
        // Get BaseDomain from an external config file
        GetSettingsFromExternalConfig();
#endif

        Debug.Log($"BaseDomain: {BaseDomain}");

        // Load the main scene, after having set the BaseDomain
        SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
    }

    private void GetSettingsFromExternalConfig()
    {
        // Search for the file in the same folder as the executable (in the build) / in the project folder (in the editor)
        string configPath = Path.Combine(Application.dataPath, "..", configFileName);

        if (!File.Exists(configPath))
        {
            Debug.LogError("Config file not found!");
            return;
        }

        string configContent = File.ReadAllText(configPath);
        BaseDomain = configContent;
    }
}
