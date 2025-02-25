using BrainAtlas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Urchin.API;
using Urchin.Managers;

public class ParticleManager : Manager
{
    #region Serialized
    [SerializeField] List<string> _materialNames;
    [SerializeField] List<Material> _materials;
    [SerializeField] private GameObject _psystemPrefabGO;
    [SerializeField] private Transform _psystemParentT;
    #endregion

    #region Variables
    private Dictionary<string, ParticleBehavior> _psystemMapping;
    private TaskCompletionSource<bool> _loadSource;
    #endregion

    #region Properties
    public static Dictionary<string, Material> ParticleMaterials;

    public override ManagerType Type => ManagerType.ParticleManager;
    public override Task LoadTask => _loadSource.Task;
    #endregion

    #region Unity
    private void Awake()
    {
        _psystemMapping = new();
        ParticleMaterials = new();
        _loadSource = new();

        if (_materialNames.Count != _materials.Count)
            throw new System.Exception("(ParticleManager) Material names list and material list must have the same length");

        for (int i = 0; i < _materialNames.Count; i++)
            ParticleMaterials.Add(_materialNames[i], _materials[i]);
    }

    private void Start()
    {
        Client_SocketIO.ParticlesUpdate += UpdateData;
        Client_SocketIO.ParticlesDelete += Delete;

        Client_SocketIO.ParticlesSetColors += SetColors;
        Client_SocketIO.ParticlesSetPositions += SetPositions;
        Client_SocketIO.ParticlesSetSizes += SetSizes;

        //// Note to self: you can delete particles by setting lifetime to -1
    }
    #endregion

    #region Manager

    public override string ToSerializedData()
    {
        return JsonUtility.ToJson(new ParticleManagerModel
        {
            Data = _psystemMapping.Values.Select(x => x.Data).ToArray(),
        });
    }

    public override void FromSerializedData(string serializedData)
    {
        ParticleManagerModel particleManagerModel = JsonUtility.FromJson<ParticleManagerModel>(serializedData);

        foreach (ParticleSystemModel model in particleManagerModel.Data)
            UpdateData(model);

        _loadSource.SetResult(true);
    }

    private struct ParticleManagerModel
    {
        public ParticleSystemModel[] Data;
    }
    #endregion

    public void Clear()
    {
        foreach (var kvp in _psystemMapping)
            Destroy(kvp.Value.gameObject);
        _psystemMapping.Clear();
    }

    public void UpdateData(ParticleSystemModel data)
    {
        if (_psystemMapping.ContainsKey(data.ID))
        {
            _psystemMapping[data.ID].UpdateData(data);
        }
        else
            Create(data);
    }

    public void SetSizes(FloatList list)
    {
        if (_psystemMapping.ContainsKey(list.ID))
            _psystemMapping[list.ID].SetSizes(list.Values);
        else
            Client_SocketIO.LogError($"Cannot set sizes. Particle system {list.ID} does not exist in Urchin");
    }

    public void SetPositions(Vector3List list)
    {
        if (_psystemMapping.ContainsKey(list.ID))
            _psystemMapping[list.ID].SetPositions(list.Values);
        else
            Client_SocketIO.LogError($"Cannot set sizes. Particle system {list.ID} does not exist in Urchin");
    }

    public void SetColors(ColorList list)
    {
        if (_psystemMapping.ContainsKey(list.ID))
            _psystemMapping[list.ID].SetColors(list.Values);
        else
            Client_SocketIO.LogError($"Cannot set sizes. Particle system {list.ID} does not exist in Urchin");
    }

    private void Create(ParticleSystemModel data)
    {
        GameObject psystemGO = Instantiate(_psystemPrefabGO, _psystemParentT);
        ParticleBehavior pbehavior = psystemGO.GetComponent<ParticleBehavior>();

        pbehavior.UpdateData(data, true);

        _psystemMapping.Add(data.ID, pbehavior);
    }

    public void Delete(IDData data)
    {
        Destroy(_psystemMapping[data.ID].gameObject);
        _psystemMapping.Remove(data.ID);
    }

}
