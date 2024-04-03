using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineBehavior : MonoBehaviour
{
    private LineRenderer _lineRenderer;
    private LineModel _data;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
    }

    public void UpdateData(LineModel data)
    {
        _data = data;

        _lineRenderer.SetPositions(_data.Positions);
        _lineRenderer.material.color = _data.Color;
    }
}
