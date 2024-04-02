//using System;
using BrainAtlas;
using System.Collections.Generic;
using UnityEngine;
using Urchin.API;

public class ParticleManager : MonoBehaviour
{
    [SerializeField] ParticleSystem _particleSystem;
    [SerializeField] List<string> _materialNames;
    [SerializeField] List<Material> _materials;

    private Dictionary<string, int> _particleMapping;
    private Dictionary<string, Material> _particleMaterials;

    private ParticleSystem.Particle[] _particles;

    #region Unity
    private void Awake()
    {
        _particleMapping = new();
        _particleMaterials = new();

        if (_materialNames.Count != _materials.Count)
            throw new System.Exception("(ParticleManager) Material names list and material list must have the same length");

        for (int i = 0; i < _materialNames.Count; i++)
            _particleMaterials.Add(_materialNames[i], _materials[i]);
    }

    private void Start()
    {
        Client_SocketIO.CreateParticles += CreateParticles;
        Client_SocketIO.SetParticlePosition += SetPosition;
        Client_SocketIO.SetParticleSize += SetSize;
        //Client_SocketIO.SetParticleShape += SetShape;
        Client_SocketIO.SetParticleColor += SetColor;
        Client_SocketIO.SetParticleMaterial += SetMaterial;

        // Note to self: you can delete particles by setting lifetime to -1

        Client_SocketIO.ClearParticles += Clear;
    }
    #endregion

    public void CreateParticles(List<string> particleNames) //instantiates cube as default
    {
        foreach (string particleName in particleNames)
        {
            if (_particleMapping.ContainsKey(particleName))
                Debug.Log($"Particle id {particleName} already exists.");

#if UNITY_EDITOR
            Debug.Log($"Creating particle {particleName}");
#endif
            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
            emitParams.position = Vector3.zero;
            emitParams.startColor = Color.red;
            emitParams.startSize = 0.1f;
            _particleSystem.Emit(emitParams, 1);

            _particleMapping.Add(particleName, _particleSystem.particleCount - 1);
        }

        _particleSystem.GetParticles(_particles);
    }

    public void Clear()
    {
        _particleSystem.Clear();
        _particleMapping.Clear();
    }

    public void SetPosition(Dictionary<string, float[]> particlePositions)
    {
        //ParticleSystem.Particle[] particles = new ParticleSystem.Particle[_particleSystem.particleCount];
        //int nParticles = _particleSystem.GetParticles(particles);

        foreach (var kvp in particlePositions)
        {
            Vector3 coordU = new Vector3(kvp.Value[0], kvp.Value[1], kvp.Value[2]);
            _particles[_particleMapping[kvp.Key]].position = BrainAtlasManager.ActiveReferenceAtlas.Atlas2World(coordU, false);
        }

        _particleSystem.SetParticles(_particles);
    }

    public void SetSize(IDListFloatList particleSizes)
    {

        for (int i = 0; i < particleSizes.IDs.Length; i++)
        {
            // TODO: Replace with vertex stream size property in Shader Graph
            _particles[_particleMapping[particleSizes.IDs[i]]].startSize = particleSizes.Values[i];
        }

        _particleSystem.SetParticles(_particles);
    }

    public void SetColor(Dictionary<string, string> particleColors)
    {

        foreach (var kvp in particleColors)
        {
            Color newColor;
            if (ColorUtility.TryParseHtmlString(kvp.Value, out newColor))
            {
                _particles[_particleMapping[kvp.Key]].startColor = newColor;
            }
        }

        _particleSystem.SetParticles(_particles);
    }

    public void SetMaterial(string materialName)
    {
        if (_particleMaterials.ContainsKey(materialName))
        {
            var renderer = _particleSystem.GetComponent<ParticleSystemRenderer>();
            renderer.material = _particleMaterials[materialName];
        }
        else
            Debug.LogError("(ParticleManager) Material {materialName} does not exist");
    }
}
