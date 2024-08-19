using BrainAtlas;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Urchin.API;

namespace Urchin.Managers
{
    public class ProbeManager : Manager
    {
        #region Variables
        [SerializeField] private List<GameObject> _probePrefabOptions;
        [SerializeField] private List<string> _probePrefabNames;
        [SerializeField] private string _defaultProbeStyle;
        [SerializeField] private Transform _probeParentT;

        // Dictionary of string -> GO keeping track of probe style options
        private Dictionary<string, GameObject> _probeOpts;
        private GameObject _defaultPrefab;
        private TaskCompletionSource<bool> _loadTask;

        // Actual objects
        private Dictionary<string, ProbeBehavior> _probes;

        public override ManagerType Type => ManagerType.ProbeManager;
        public override Task LoadTask => _loadTask.Task;
        #endregion

        #region Unity

        private void Awake()
        {
            // Initialize variables
            _probeOpts = new Dictionary<string, GameObject>();
            _probes = new();
            _loadTask = new();

            _defaultPrefab = _probePrefabOptions[_probePrefabNames.IndexOf(_defaultProbeStyle)];

            // Fill dictionaries
            if (_probePrefabOptions.Count == _probePrefabNames.Count)
            {
                for (int i = 0; i < _probePrefabOptions.Count; i++)
                    _probeOpts.Add(_probePrefabNames[i], _probePrefabOptions[i]);
            }
            else
                throw new System.Exception("Number of prefab options and names must match");
        }

        private void Start()
        {
            Client_SocketIO.ProbeUpdate += UpdateData;
            Client_SocketIO.ProbeDelete += Delete;

            Client_SocketIO.ProbeSetAngles += SetAngles;
            Client_SocketIO.ProbeSetColors += SetColors;
            Client_SocketIO.ProbeSetPositions += SetPositions;
            Client_SocketIO.ProbeSetScales += SetScales;
        }

        #endregion

        #region Manager


        public override string ToSerializedData()
        {
            return JsonUtility.ToJson(new ProbeManagerModel()
            {
                Data = _probes.Values.Select(x => x.Data).ToArray(),
            });
        }

        public override void FromSerializedData(string serializedData)
        {
            ProbeManagerModel model = JsonUtility.FromJson<ProbeManagerModel>(serializedData);

            foreach (ProbeModel data in model.Data)
            {
                UpdateData(data);
            }
            _loadTask.SetResult(true);
        }

        public struct ProbeManagerModel
        {
            public ProbeModel[] Data;
        }

        #endregion

        #region Public object functions

        public void UpdateData(ProbeModel data)
        {
            if (_probes.ContainsKey(data.ID))
            {
                // update
            }
            else
                CreateProbes(data);
        }

        public void CreateProbes(ProbeModel data)
        {
            GameObject newProbe = Instantiate(_defaultPrefab, _probeParentT);
            newProbe.name = $"probe_{data.ID}";
            ProbeBehavior probeBehavior = newProbe.GetComponent<ProbeBehavior>();

            probeBehavior.UpdateData(data);

            _probes.Add(data.ID, probeBehavior);
        }

        public void Delete(IDData data)
        {
            if (_probes.ContainsKey(data.ID))
            {
                Destroy(_probes[data.ID]);
                _probes.Remove(data.ID);
            }
        }

        public void SetColors(IDListColorList data)
        {
            for (int i = 0; i < data.IDs.Length; i++)
            {
                if (_probes.ContainsKey(data.IDs[i]))
                {
                    _probes[data.IDs[i]].SetColor(data.Values[i]);
                }
                else
                    Client_SocketIO.LogError($"Cannot set color. Probe {data.IDs[i]} not found");
            }
        }

        public void SetPositions(IDListVector3List data)
        {
            for (int i = 0; i < data.IDs.Length; i++)
            {
                if (_probes.ContainsKey(data.IDs[i]))
                {
                    _probes[data.IDs[i]].SetPosition(data.Values[i]);
                }
                else
                    Client_SocketIO.LogError($"Cannot set color. Probe {data.IDs[i]} not found");
            }
        }

        public void SetAngles(IDListVector3List data)
        {
            for (int i = 0; i < data.IDs.Length; i++)
            {
                if (_probes.ContainsKey(data.IDs[i]))
                {
                    _probes[data.IDs[i]].SetAngles(data.Values[i]);
                }
                else
                    Client_SocketIO.LogError($"Cannot set color. Probe {data.IDs[i]} not found");
            }
        }

        //public void SetStyles(Dictionary<string, string> probeStyles)
        //{
        //    foreach (KeyValuePair<string, string> kvp in probeStyles)
        //    {
        //        if (_probes.ContainsKey(kvp.Key))
        //        {
        //            Destroy(_probes[kvp.Key]);
        //            _probes[kvp.Key] = Instantiate(_probePrefabOptions[_probePrefabNames.IndexOf(kvp.Value)], _probeParentT);
        //            _probes[kvp.Key].name = $"probe_{kvp.Key}";

        //            SetProbePositionAndAngles(kvp.Key);
        //        }
        //    }
        //}

        public void SetScales(IDListVector3List data)
        {
            for (int i = 0; i < data.IDs.Length; i++)
            {
                if (_probes.ContainsKey(data.IDs[i]))
                {
                    _probes[data.IDs[i]].SetScale(data.Values[i]);
                }
                else
                    Client_SocketIO.LogError($"Cannot set color. Probe {data.IDs[i]} not found");
            }
        }

        public void ClearProbes()
        {
            Debug.Log("(Client) Clearing probes");
            foreach (var probe in _probes.Values)
                Destroy(probe.gameObject);
            _probes.Clear();
        }

        #endregion
    }
}