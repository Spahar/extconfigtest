using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class LogoLoader : MonoBehaviour
{
    Image logoIMG;

    void Awake()
    {
        logoIMG = GetComponent<Image>();
    }

    void Start()
    {
        LoadLogo();
    }

    private void LoadLogo()
    {
        #if CLIENT_0
                logoIMG.sprite = ClientsManager.GetLogoByScriptingSymbol("CLIENT_0");
        #elif CLIENT_1
                logoIMG.sprite = ClientsManager.GetLogoByScriptingSymbol("CLIENT_1");
        #endif
    }
}
