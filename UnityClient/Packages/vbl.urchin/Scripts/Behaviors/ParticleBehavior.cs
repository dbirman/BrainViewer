using BrainAtlas;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleBehavior : MonoBehaviour
{
    private ParticleSystem _particleSystem;
    private ParticleSystem.Particle[] _particles;

    public ParticleSystemModel Data { get; private set; }

    private void Awake()
    {
        _particleSystem = GetComponent<ParticleSystem>();
    }

    #region Public
    public void UpdateData(ParticleSystemModel data)
    {
        if (_particleSystem.particleCount == 0)
        {
            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();

            //for (int i = 0; i < data.Sizes)
            emitParams.position = Vector3.zero;
            emitParams.startColor = Color.red;
            emitParams.startSize = 0.1f;
            _particleSystem.Emit(emitParams, 1);

            _particles = new ParticleSystem.Particle[_particleSystem.particleCount];
            _particleSystem.GetParticles(_particles);
        }

        // Convert positions to World
        SetPositions(data.Positions);

        SetSizes();
        SetColors();
        SetPositions();
    }

    public void SetSizes(float[] sizes)
    {
        for (int i = 0; i < _particles.Length; i++)
            Data.Sizes[i] = sizes[i];
        SetSizes();
    }

    private void SetSizes()
    {
        for (int i = 0; i < _particles.Length; i++)
            _particles[i].startSize = Data.Sizes[i];
        _particleSystem.SetParticles(_particles);
    }

    public void SetColors(Color[] colors)
    {
        for (int i = 0; i < _particles.Length; i++)
            Data.Colors[i] = colors[i];
    }

    private void SetColors()
    {
        for (int i = 0; i < _particles.Length; i++)
            _particles[i].startColor = Data.Colors[i];
        _particleSystem.SetParticles(_particles);
    }

    public void SetPositions(Vector3[] positions)
    {
        for (int i = 0; i < _particles.Length; i++)
        {
            Vector3 posWorld = BrainAtlasManager.ActiveReferenceAtlas.Atlas2World(positions[i]);
            Data.Positions[i] = posWorld;
        }
    }

    private void SetPositions()
    {
        for (int i = 0; i < _particles.Length; i++)
            _particles[i].position = Data.Positions[i];
        _particleSystem.SetParticles(_particles);
    }
    #endregion
}
