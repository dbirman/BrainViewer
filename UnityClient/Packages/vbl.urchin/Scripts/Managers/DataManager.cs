using System.Collections;
using System.Collections.Generic;
using System.Text;
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
        Client_SocketIO.Load += Load;
    }
    #endregion

    public IEnumerator Save(SaveModel data)
    {
        for (int i = 0; i < _managers.Count; i++)
        {
            string type = Manager.Type2String(_managers[i].Type);

            //// Push data up to the REST API
            var uploadModel = new UploadModel
            {
                Data = _managers[i].ToSerializedData(),
                Password = "5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8"
            };

            Debug.Log(_managers[i].ToSerializedData());

            yield return null;

            //string json = JsonUtility.ToJson(uploadModel);
            //byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

            //UnityWebRequest request = new UnityWebRequest($"{apiURL}/{data.Bucket}/{type}/data", "POST");
            //request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            //request.SetRequestHeader("Content-Type", "application/json");

            //yield return request.SendWebRequest();

            //Debug.Log($"Response code{request.responseCode}");
        }
    }

    public void Load(LoadModel data)
    {
        throw new System.NotImplementedException();

        //// Get the manager data back from the API
        //FlattenedData data = new();

        //foreach (ManagerData managerData in data.Data)
        //{
        //    switch (managerData.Type)
        //    {
        //        case ManagerType.PrimitiveMeshManager:
        //            _primitiveMeshManager.FromSerializedData(managerData.Data);
        //            break;
        //    }
        //}
    }
}
