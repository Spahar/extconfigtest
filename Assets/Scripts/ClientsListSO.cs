using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ClientsListSO", menuName = "ScriptableObjects/ClientsListSO", order = 1)]
public class ClientsListSO : ScriptableObject
{
    public List<ClientData> clients = new List<ClientData>();
}
