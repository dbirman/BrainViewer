using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineBehavior : MonoBehaviour
{
    private LineRenderer _lineRenderer;
    public LineModel Data { get; private set; }

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
    }

    public void UpdateData(LineModel data)
    {
        Data = data;

        _lineRenderer.SetPositions(Data.Positions);
        _lineRenderer.material.color = Data.Color;
    }
}
