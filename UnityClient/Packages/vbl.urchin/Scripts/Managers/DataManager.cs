using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Entities.UniversalDelegates;
using UnityEngine;
using UnityEngine.Networking;
using Urchin.API;
using Urchin.Managers;

public class DataManager : MonoBehaviour
{
    [SerializeField] PrimitiveMeshManager _primitiveMeshManager;

    [SerializeField] private string apiURL;

    private List<Manager> _managers;

    #region Unity
    private void Awake()
    {
        _managers = new();
        _managers.Add(_primitiveMeshManager);

        Client_SocketIO.Save += x => StartCoroutine(Save(x));
        Client_SocketIO.Load += x => StartCoroutine(Load(x));
    }
    #endregion

    public IEnumerator Save(SaveRequest data)
    {
        for (int i = 0; i < _managers.Count; i++)
        {
            //// Push data up to the REST API
            var uploadRequest = new UploadRequest
            {
                Type = (int)_managers[i].Type,
                Data = _managers[i].ToSerializedData(),
                Password = "5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8"
            };

            yield return null;

            string json = JsonUtility.ToJson(uploadRequest);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

            UnityWebRequest request = new UnityWebRequest($"{apiURL}/upload/{data.Bucket}", "POST");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.responseCode != 201)
            {
                Client_SocketIO.LogError(request.error);
            }
        }
    }

    public IEnumerator Load(LoadRequest data)
    {
        string json = JsonUtility.ToJson(data);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        UnityWebRequest request = new UnityWebRequest($"{apiURL}/{data.Bucket}/all", "GET");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            ParseLoadData(JsonUtility.FromJson<LoadModel>(request.downloadHandler.text));
        }
        else
        {
            Client_SocketIO.LogError(request.error);
        }
    }

    private void ParseLoadData(LoadModel data)
    {
        for (int i = 0; i < data.Types.Length; i++)
        {
            string managerData = data.Data[i];

            switch ((ManagerType)data.Types[i])
            {
                case ManagerType.PrimitiveMeshManager:
                    _primitiveMeshManager.FromSerializedData(managerData);
                    break;
            }
        }
    }
}
