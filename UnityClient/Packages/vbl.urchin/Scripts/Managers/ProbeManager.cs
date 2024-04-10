using BrainAtlas;
using System.Collections.Generic;
using System.Linq;
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

        // Actual objects
        private Dictionary<string, ProbeBehavior> _probes;

        public override ManagerType Type => throw new System.NotImplementedException();
        #endregion

        #region Unity

        private void Awake()
        {
            // Initialize variables
            _probeOpts = new Dictionary<string, GameObject>();
            _probes = new();

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
            Client_SocketIO.CreateProbes += CreateProbes;
            Client_SocketIO.DeleteProbes += DeleteProbes;
            Client_SocketIO.SetProbeColors += SetColors;
            Client_SocketIO.SetProbePos += SetPositions;
            Client_SocketIO.SetProbeAngles += SetAngles;
            //Client_SocketIO.SetProbeStyle += SetStyles;
            Client_SocketIO.SetProbeSize += SetSizes;

            Client_SocketIO.ClearProbes += ClearProbes;
        }

        #endregion

        #region Manager


        public override string ToSerializedData()
        {
            return "";
            //return JsonUtility.ToJson(new ProbeManagerModel()
            //{
            //    Data = _probes.Values.Select(x => x.Data).ToArray(),
            //});
        }

        public override void FromSerializedData(string serializedData)
        {
            throw new System.NotImplementedException();
        }

        public struct ProbeManagerModel
        {
            ProbeModel[] Data;
        }

        #endregion

        #region Public object functions

        public void CreateProbes(List<string> probeNames)
        {
            foreach (string probeName in probeNames)
            {
                if (!_probes.ContainsKey(probeName))
                {
                    GameObject newProbe = Instantiate(_defaultPrefab, _probeParentT);
                    newProbe.name = $"probe_{probeName}";
                    ProbeBehavior probeBehavior = newProbe.GetComponent<ProbeBehavior>();

                    probeBehavior.SetPosition(BrainAtlasManager.ActiveReferenceAtlas.Atlas2World(Vector3.zero));
                    probeBehavior.SetAngles(Vector3.zero);

                    _probes.Add(probeName, probeBehavior);
                }
                else
                {
                    Client_SocketIO.LogError(string.Format("Probe {0} already exists in the scene", probeName));
                }
            }
        }

        public void DeleteProbes(List<string> probeNames)
        {
            foreach (string probeName in probeNames)
            {
                Destroy(_probes[probeName]);
                _probes.Remove(probeName);
            }
        }

        public void SetColors(Dictionary<string, string> probeColors)
        {
            foreach (KeyValuePair<string, string> kvp in probeColors)
            {
                if (_probes.ContainsKey(kvp.Key))
                {
                    Color newColor;
                    if (ColorUtility.TryParseHtmlString(kvp.Value, out newColor))
                        _probes[kvp.Key].SetColor(newColor);
                }
                else
                    Client_SocketIO.LogError($"Probe {kvp.Key} not found");
            }
        }

        /// <summary>
        /// Set position, positions should be in mm
        /// </summary>
        /// <param name="probePositions"></param>
        public void SetPositions(Dictionary<string, List<float>> probePositions)
        {
            foreach (KeyValuePair<string, List<float>> kvp in probePositions)
            {
                if (_probes.ContainsKey(kvp.Key))
                {
                    Vector3 coordU = new Vector3(kvp.Value[0], kvp.Value[1], kvp.Value[2]);
                    Vector3 worldU = BrainAtlasManager.ActiveReferenceAtlas.Atlas2World(coordU);
                    _probes[kvp.Key].SetPosition(worldU);
                }
                else
                    Client_SocketIO.LogError($"Probe {kvp.Key} not found");
            }
        }

        public void SetAngles(Dictionary<string, List<float>> probeAngles)
        {
            foreach (KeyValuePair<string, List<float>> kvp in probeAngles)
            {
                if (_probes.ContainsKey(kvp.Key))
                {
                    _probes[kvp.Key].SetAngles(new Vector3(kvp.Value[0], kvp.Value[1], kvp.Value[2]));
                }
                else
                    Client_SocketIO.LogError($"Probe {kvp.Key} not found");
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

        public void SetSizes(Dictionary<string, List<float>> probeScales)
        {
            foreach (KeyValuePair<string, List<float>> kvp in probeScales)
            {
                if (_probes.ContainsKey(kvp.Key))
                {
                    _probes[kvp.Key].SetSize(new Vector3(kvp.Value[0], kvp.Value[1], kvp.Value[2]));
                }
                //else
                //    Debug.Log("Probe " + kvp.Key + " not found");
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