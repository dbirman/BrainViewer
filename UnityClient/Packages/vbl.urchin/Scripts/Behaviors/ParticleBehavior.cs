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
    public void UpdateData(ParticleSystemModel data, bool created = false)
    {
        Debug.Log(created);
        if (created)
        {
            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();

            emitParams.position = Vector3.zero;
            emitParams.startColor = Color.white;
            emitParams.startSize = 0.1f;
            _particleSystem.Emit(emitParams, data.N);

            _particles = new ParticleSystem.Particle[_particleSystem.particleCount];
            _particleSystem.GetParticles(_particles);
        }

        Debug.Log(data.Positions.Length);
        Debug.Log(data.Sizes.Length);
        Debug.Log(data.Colors.Length);
        SetPositions(data.Positions);
        SetSizes(data.Sizes);
        SetColors(data.Colors);
    }

    public void SetSizes(float[] sizes)
    {
        ParticleSystemModel data = Data;
        data.Sizes = new float[sizes.Length];

        for (int i = 0; i < _particles.Length; i++)
            data.Sizes[i] = sizes[i];

        Data = data;
        _SetSizes();
    }

    private void _SetSizes()
    {
        for (int i = 0; i < _particles.Length; i++)
            _particles[i].startSize = Data.Sizes[i];
        _particleSystem.SetParticles(_particles);
    }

    public void SetColors(Color[] colors)
    {
        ParticleSystemModel data = Data;
        data.Colors = new Color[colors.Length];

        for (int i = 0; i < _particles.Length; i++)
            data.Colors[i] = colors[i];

        Data = data;
        _SetColors();
    }

    private void _SetColors()
    {
        for (int i = 0; i < _particles.Length; i++)
            _particles[i].startColor = Data.Colors[i];
        _particleSystem.SetParticles(_particles);
    }

    public void SetPositions(Vector3[] positions)
    {
        ParticleSystemModel data = Data;
        data.Positions = new Vector3[positions.Length];

        for (int i = 0; i < _particles.Length; i++)
        {
            data.Positions[i] = positions[i];
        }

        Data = data;
        _SetPositions();
    }

    private void _SetPositions()
    {
        for (int i = 0; i < _particles.Length; i++)
            _particles[i].position = BrainAtlasManager.ActiveReferenceAtlas.Atlas2World(Data.Positions[i]);
        _particleSystem.SetParticles(_particles);
    }
    #endregion
}
