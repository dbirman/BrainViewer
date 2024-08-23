using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    [SerializeField] List<ManagerType> _managerTypes;
    [SerializeField] List<Manager> _managers;
    [SerializeField] List<int> _loadOrder;

    [SerializeField] private string apiURL;

    #region Variables
    //private List<Manager> _managers;
    private DockModel Data;
    #endregion


    #region Unity
    private void Awake()
    {
        Data.DockUrl = apiURL;

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

    public LoadModel GetAllData()
    {
        // Generate a LoadModel holding the data
        LoadModel allData = new LoadModel(new int[_managers.Count], new string[_managers.Count]);

        for (int i = 0; i < _managers.Count; i++)
        {
            allData.Types[i] = (int)_managers[i].Type;
            allData.Data[i] = _managers[i].ToSerializedData();
        }

        return allData;
    }

    public IEnumerator Save(SaveRequest data)
    {
        LoadModel allData = GetAllData();

        if (data.Filename != "")
        {
            // Save data to a local file
            string filePath = Path.Combine(Application.persistentDataPath, data.Filename);
            string serializedJson = JsonUtility.ToJson(allData);

            Debug.Log($"Sending save data: {serializedJson}");
            Client_SocketIO.Emit("urchin-dock-callback", serializedJson);
#if UNITY_WEBGL && !UNITY_EDITOR
            DownloadFile(data.Filename, serializedJson);
#else
            File.WriteAllText(filePath, serializedJson);
            Client_SocketIO.Log($"File saved to {filePath}, re-load this file by passing just the filename, not the full path.");
#endif
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

    /// <summary>
    /// Load the data using an external request, either to a local file or to the cloud server
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Load data from an internal request
    /// </summary>
    /// <param name="allData"></param>
    public void Load(LoadModel allData)
    {
        ParseLoadData(allData);
    }

    /// <summary>
    /// Load data from an internal serialized string
    /// </summary>
    /// <param name="serializedData"></param>
    public void Load(string serializedData)
    {
        ParseLoadData(JsonUtility.FromJson<LoadModel>(serializedData));
    }

    /// <summary>
    /// Load all of the managers in priority order
    /// </summary>
    /// <param name="data"></param>
    private async void ParseLoadData(LoadModel data)
    {
        // Create pairs of string, type, and priority
        var combined = data.Data
            .Select((s, i) => new { Data = s, Type = data.Types[i], Priority = _loadOrder[_managerTypes.FindIndex(x => x == (ManagerType)data.Types[i])] })
            .ToList();

        // Sort by priority in descending order
        var sortedCombined = combined
            .OrderByDescending(c => c.Priority)
            .ToList();

        for (int i = 0; i < sortedCombined.Count; i++)
        {
            Manager manager = _managers.Find(x => x.Type.Equals(sortedCombined[i].Type));
            manager.FromSerializedData(sortedCombined[i].Data);
            await manager.LoadTask;
        }
    }
}
