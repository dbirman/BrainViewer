using BrainAtlas;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Urchin.API;

namespace Urchin.Managers
{
    public class CustomMeshManager : Manager
    {
        #region Serialized
        [SerializeField] private Transform _customMeshParentT;
        #endregion

        #region Private
        private Dictionary<string, CustomMeshModel> _customMeshModels;
        private Dictionary<string, GameObject> _customMeshGOs;
        private BlenderSpace _blenderSpace;

        public override ManagerType Type => ManagerType.CustomMeshManager;
        #endregion

        private void Start()
        {
            _customMeshGOs = new();

            _blenderSpace = new();

            Client_SocketIO.CustomMeshUpdate += UpdateData;
            Client_SocketIO.CustomMeshDelete += Delete;

            Client_SocketIO.ClearCustomMeshes += Clear;
        }

        #region Manager
        public override string ToSerializedData()
        {
            CMeshManagerModel model = new CMeshManagerModel()
            {
                Data = _customMeshModels.Values.ToArray()
            };

            return JsonUtility.ToJson(model);
        }

        public override void FromSerializedData(string serializedData)
        {
            CMeshManagerModel model = JsonUtility.FromJson<CMeshManagerModel>(serializedData);

            foreach (CustomMeshModel data in model.Data)
                UpdateData(data);
        }

        private struct CMeshManagerModel
        {
            public CustomMeshModel[] Data;
        }
        #endregion

        public void UpdateData(CustomMeshModel data)
        {
            if (_customMeshGOs.ContainsKey(data.ID))
            {
                // Update
                _customMeshModels[data.ID] = data;
                SetPosition(data);
                SetScale(data);
            }
            else
                Create(data);
        }

        private void Create(CustomMeshModel data)
        {
            GameObject go = new GameObject(data.ID);
            go.transform.SetParent(_customMeshParentT);

            Mesh mesh = new Mesh();

            // the vertices are assumed to have been passed in ap/ml/dv directions
            mesh.vertices = data.Vertices.Select(x => _blenderSpace.Space2World_Vector(x)).ToArray();

            mesh.triangles = data.Triangles;

            if (data.Normals != null)
                mesh.normals = data.Normals;

            go.AddComponent<MeshFilter>().mesh = mesh;
            go.AddComponent<MeshRenderer>().material = MaterialManager.GetMaterial("opaque-lit");
            go.GetComponent<MeshRenderer>().material.color = Color.gray;

            SetPosition(data); 
            SetScale(data);

            _customMeshModels.Add(data.ID, data);
            _customMeshGOs.Add(data.ID, go);
        }

        private void Delete(IDData data)
        {
            if (_customMeshGOs.ContainsKey(data.ID))
            {
                Destroy(_customMeshGOs[data.ID]);
                _customMeshGOs.Remove(data.ID);
            }
            else
                Client_SocketIO.LogWarning($"Custom mesh {data.ID} does not exist in the scene, cannot destroy");

        }

        private void SetPosition(CustomMeshModel data)
        {
            if (_customMeshGOs.ContainsKey(data.ID))
            {
                Transform transform = _customMeshGOs[data.ID].transform;

                transform.position = BrainAtlasManager.ActiveReferenceAtlas.Atlas2World(data.Position, data.UseReference);
            }
            else
                Client_SocketIO.LogWarning($"Custom mesh {data.ID} does not exist in the scene, cannot set position");
        }

        private void SetScale(CustomMeshModel data)
        {
            if (_customMeshGOs.ContainsKey(data.ID))
            {
                Transform transform = _customMeshGOs[data.ID].transform;

                // Set scale, rotating to match the atlas format
                transform.localScale = BrainAtlasManager.ActiveReferenceAtlas.Atlas2World_Vector(data.Scale);
            }
            else
                Client_SocketIO.LogWarning($"Custom mesh {data.ID} does not exist in the scene, cannot set scale");
        }

        public void Clear()
        {
            foreach (var kvp in _customMeshGOs)
                Destroy(kvp.Value);

            _customMeshGOs.Clear();
        }
    }

}