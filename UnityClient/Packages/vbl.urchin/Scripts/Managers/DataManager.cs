using BrainAtlas;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Urchin.API;
using Urchin.Managers;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

public class DataManager : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void DownloadFile(string filename, string filedata);
#endif

    [SerializeField] AtlasManager _atlasManager;
    [SerializeField] CameraManager _cameraManager;
    [SerializeField] LineRendererManager _lineRendererManager;
    [SerializeField] PrimitiveMeshManager _primitiveMeshManager;
    [SerializeField] TextManager _textManager;
    [SerializeField] ParticleManager _particleManager;
    [SerializeField] CustomMeshManager _customMeshManager;
    [SerializeField] ProbeManager _probeManager;

    [SerializeField] private string apiURL;

    #region Variables
    private List<Manager> _managers;
    private DockModel Data;
    #endregion


    #region Unity
    private void Awake()
    {
        Data.DockUrl = apiURL;

        _managers = new()
        {
            _atlasManager,
            _primitiveMeshManager,
            _cameraManager,
            _lineRendererManager,
            _textManager,
            _particleManager,
            _customMeshManager,
            _probeManager
        };

        Client_SocketIO.Save += x => StartCoroutine(Save(x));
        Client_SocketIO.Load += x => StartCoroutine(Load(x));
        Client_SocketIO.LoadData += ParseLoadData;
        Client_SocketIO.DockData += UpdateData;
    }
    #endregion

    public void UpdateData(DockModel data)
    {
        Data = data;
    }

    public IEnumerator Save(SaveRequest data)
    {
        // Generate a LoadModel holding the data
        LoadModel allData = new LoadModel(new int[_managers.Count], new string[_managers.Count]);

        for (int i = 0; i < _managers.Count; i++)
        {
            allData.Types[i] = (int)_managers[i].Type;
            allData.Data[i] = _managers[i].ToSerializedData();
        }


        if (data.Filename != "")
        {
            // Save data to a local file
            //string filePath = Path.Combine(Application.persistentDataPath, data.Filename);
            string serializedJson = JsonUtility.ToJson(allData);

            Debug.Log($"Sending save data: {serializedJson}");
            Client_SocketIO.Emit("urchin-dock-callback", serializedJson);
//#if UNITY_WEBGL && !UNITY_EDITOR
//            DownloadFile(data.Filename, serializedJson);
//#else
//            File.WriteAllText(filePath, serializedJson);
//            Client_SocketIO.Log($"File saved to {filePath}, re-load this file by passing just the filename, not the full path.");
//#endif
        }
        else
        {
            // Load data to the cloud
            for (int i = 0; i < _managers.Count; i++)
            {
                //// Push data up to the REST API
                var uploadRequest = new UploadRequest
                {
                    Type = allData.Types[i],
                    Data = allData.Data[i],
                    Password = "5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8"
                };

                yield return null;

                string json = JsonUtility.ToJson(uploadRequest);
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

                UnityWebRequest request = new UnityWebRequest($"{Data.DockUrl}/upload/{data.Bucket}", "POST");
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.responseCode != 201)
                {
                    Client_SocketIO.LogError(request.error);
                }
            }
        }
    }

    public IEnumerator Load(LoadRequest data)
    {
        if (data.Filename != "")
        {
            string filePath = Path.Combine(Application.persistentDataPath, data.Filename);

            ParseLoadData(JsonUtility.FromJson<LoadModel>(File.ReadAllText(filePath)));
        }
        else
        {
            string json = JsonUtility.ToJson(data);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

            UnityWebRequest request = new UnityWebRequest($"{Data.DockUrl}/{data.Bucket}/all", "GET");
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
    }

    private async void ParseLoadData(LoadModel data)
    {
        // If any of the models are the atlas model, load that first
        for (int i = 0; i < data.Types.Length; i++)
        {
            if ((ManagerType)data.Types[i] == ManagerType.AtlasManager)
            {
                _atlasManager.FromSerializedData(data.Data[i]);
                await _atlasManager.LoadTask;
            }
        }

        // Then load everything else (these are all independent)
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
