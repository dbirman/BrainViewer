using BrainAtlas;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Urchin.API;

namespace Urchin.Managers
{
   
    public class PrimitiveMeshManager : Manager
    {
        [SerializeField] private Transform _primitiveMeshParentT;
        [SerializeField] private GameObject _primitivePrefabGO;

        [SerializeField] List<string> meshNames;
        [SerializeField] List<Mesh> meshOpts;

        //Keeping a dictionary mapping names of objects to the game object in schene
        private Dictionary<string, MeshBehavior> _meshBehaviors;
        private Dictionary<string, Mesh> _meshOptions;

        #region Properties
        public override ManagerType Type { get { return ManagerType.PrimitiveMeshManager; } }
        #endregion

        #region Unity
        private void Awake()
        {
            _meshBehaviors = new();

            _meshOptions = new();
            for (int i = 0; i < meshNames.Count; i++)
                _meshOptions.Add(meshNames[i], meshOpts[i]);
        }

        private void Start()
        {
            // singular
            Client_SocketIO.MeshUpdate += UpdateData;

            // plural
            Client_SocketIO.MeshDelete += Delete;
            Client_SocketIO.MeshDeletes += DeleteList;

            Client_SocketIO.MeshSetPositions += SetPositions;
            Client_SocketIO.MeshSetScales += SetScales;
            Client_SocketIO.MeshSetColors += SetColors;
            Client_SocketIO.MeshSetMaterials += SetMaterials;

            // clear
            Client_SocketIO.ClearMeshes += Clear;
        }

        #endregion

        public void UpdateData(MeshModel data)
        {
            if (_meshBehaviors.ContainsKey(data.ID))
            {
                // Update
                _meshBehaviors[data.ID].Data = data;
                _meshBehaviors[data.ID].UpdateAll();
            }
            else
            {
                // Create
                Create(data);
            }
        }

        public void Create(MeshModel data) //instantiates cube as default
        {
            GameObject go = Instantiate(_primitivePrefabGO, _primitiveMeshParentT);
            go.name = $"primMesh_{data.ID}";

            MeshBehavior meshBehavior = go.GetComponent<MeshBehavior>();

            meshBehavior.Data = data;
            meshBehavior.UpdateAll();

            _meshBehaviors.Add(data.ID, meshBehavior);
        }

        public void Clear()
        {
            foreach (var kvp in _meshBehaviors)
            {
                if (kvp.Value != null && kvp.Value.gameObject != null)
                    Destroy(kvp.Value.gameObject);
            }
            _meshBehaviors.Clear();
        }

        #region Delete
        public void Delete(IDData data)
        {
            _Delete(data.ID);
        }

        public void DeleteList(IDList data)
        {
            foreach (string id in data.IDs)
                _Delete(id);
        }

        private void _Delete(string id)
        {
            if (_meshBehaviors.ContainsKey(id))
            {
                Destroy(_meshBehaviors[id].gameObject);
                _meshBehaviors.Remove(id);
            }
            else
                Debug.LogError($"Mesh {id} does not exist, can't delete");
        }
        #endregion

        #region Plural setters
        public void SetPositions(IDListVector3List data)
        {
            for (int i = 0; i < data.IDs.Length; i++)
                _meshBehaviors[data.IDs[i]].SetPosition(data.Values[i]);
        }

        public void SetScales(IDListVector3List data)
        {
            for (int i = 0; i < data.IDs.Length; i++)
                _meshBehaviors[data.IDs[i]].SetScale(data.Values[i]);
        }

        public void SetColors(IDListColorList data)
        {
            for (int i = 0; i < data.IDs.Length; i++)
                _meshBehaviors[data.IDs[i]].SetColor(data.Values[i]);
        }

        public void SetMaterials(IDListStringList data)
        {
            for (int i = 0; i < data.IDs.Length; i++)
                _meshBehaviors[data.IDs[i]].SetMaterial(data.Values[i]);
        }

        #endregion

        #region Manager
        public override void FromSerializedData(string serializedData)
        {
            Clear();

            PrimitiveMeshModel data = JsonUtility.FromJson<PrimitiveMeshModel>(serializedData);

            foreach (MeshModel modelData in data.Data)
                Create(modelData);
        }

        public override string ToSerializedData()
        {
            PrimitiveMeshModel data = new();
            data.Data = new MeshModel[_meshBehaviors.Count];

            MeshBehavior[] meshBehaviors = _meshBehaviors.Values.ToArray();

            for (int i = 0; i < _meshBehaviors.Count; i++)
                data.Data[i] = meshBehaviors[i].Data;

            return JsonUtility.ToJson(data);
        }

        #endregion
    }
}