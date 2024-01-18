using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressablesManager : MonoBehaviour
{
    [SerializeField] AssetReference cubeReference;
    [SerializeField] TMP_Text serverResponse;

    GameObject cube;

    private void Start()
    {
        // Initialize Addressables, at this point the the remote load path would be based on the StartupManager.BaseDomain that we set in the previous scene
        Addressables.InitializeAsync().Completed += OnInitializeCompleted;
    }

    private void OnInitializeCompleted(AsyncOperationHandle<IResourceLocator> obj)
    {
        // Instantiate the cube prefab from the remote server
        Addressables.InstantiateAsync(cubeReference).Completed += (handle) => {
            cube = handle.Result;
            cube.transform.position = new Vector3(0f, 0.5f, 0f);
        };

        GetServerResponse();
    }

    private void GetServerResponse()
    {
        // Web Get Request to StartupManager.BaseApiUrl/cubecolor
        var request = new UnityEngine.Networking.UnityWebRequest($"{StartupManager.BaseApiUrl}/cubecolor.php");
        request.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
        request.SendWebRequest().completed += (operation) =>
        {
            // Set the cube color based on the server response
            string response = request.downloadHandler.text;
            serverResponse.text = response;
        };
    }
}
