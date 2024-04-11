using UnityEngine;

public class ProbeBehavior : MonoBehaviour
{
    [SerializeField] private GameObject _probeModelGO;

    public ProbeModel Data { get; private set; }

    public void UpdateData(ProbeModel data)
    {
        Data = data;

        _SetPosition();
        _SetAngles();
        _SetColor();
        _SetScale();
    }

    public void SetPosition(Vector3 tipWorldU)
    {
        ProbeModel temp = Data;
        temp.Position = tipWorldU;
        Data = temp;
        _SetPosition();
    }

    public void SetAngles(Vector3 angles)
    {
        ProbeModel temp = Data;
        temp.Angles = angles;
        Data = temp;
        _SetAngles();
    }

    public void SetStyle(string style)
    {
        //todo
    }

    public void SetColor(Color color)
    {
        ProbeModel temp = Data;
        temp.Color = color;
        Data = temp;
        _SetColor();
    }

    public void SetScale(Vector3 scale)
    {
        ProbeModel temp = Data;
        temp.Scale = scale;
        Data = temp;
        _SetScale();
    }

    private void _SetPosition()
    {
        transform.localPosition = Data.Position;
    }
    private void _SetAngles()
    {
        transform.rotation = Quaternion.identity;
        // rotate around azimuth first
        transform.RotateAround(transform.position, Vector3.up, Data.Angles.x);
        // then elevation
        transform.RotateAround(transform.position, transform.right, Data.Angles.y);
        // then spin
        transform.RotateAround(transform.position, transform.forward, Data.Angles.z);
    }
    private void _SetColor()
    {
        _probeModelGO.GetComponent<Renderer>().material.color = Data.Color;
    }
    private void _SetScale()
    {
        _probeModelGO.transform.localScale = Data.Scale;
        _probeModelGO.transform.localPosition = new Vector3(0f, 0f, -Data.Scale.y / 2);
    }
}
