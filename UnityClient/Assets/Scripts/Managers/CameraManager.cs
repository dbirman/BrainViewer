using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    #region Exposed fields
    [SerializeField] private GameObject _cameraPrefab;
    [SerializeField] private RenderTexture _renderTexture1;
    [SerializeField] private RenderTexture _renderTexture2;
    [SerializeField] private RenderTexture _renderTexture3;
    [SerializeField] private RenderTexture _renderTexture4;
    [SerializeField] private CameraBehavior mainCamera;
    [SerializeField] private LightBehavior _lightBehavior;
    [SerializeField] private AreaManager _areaManager;
    [SerializeField] private Canvas _uiCanvas;

    [SerializeField] private List<GameObject> _cameraUIGOs;
    #endregion

    #region Variables
    private Dictionary<string, CameraBehavior> _cameras; //directly access the camera nested within the prefab
    private Stack<RenderTexture> textures = new();
    #endregion

    #region Unity functions
    private void Awake()
    {
        textures.Push(_renderTexture4);
        textures.Push(_renderTexture3);
        textures.Push(_renderTexture2);
        textures.Push(_renderTexture1);
        _cameras = new();
        _cameras.Add("CameraMain", mainCamera);

        mainCamera.Name = "CameraMain";
    }

    #endregion


     #region Public camera functions

    public void CreateCamera(List<string> cameraNames)
    {
        //instantiating game object w camera component
        foreach (string cameraName in cameraNames)
        {
            if (_cameras.Keys.Contains(cameraName))
            {
                Client.LogWarning($"Camera {cameraName} was created twice. The camera will not be re-created");
                continue;
            }

#if UNITY_EDITOR
            Debug.Log($"{cameraName} created");
#endif
            GameObject tempObject = Instantiate(_cameraPrefab);
            tempObject.name = $"camera_{cameraName}";
            CameraBehavior cameraBehavior = tempObject.GetComponent<CameraBehavior>();
            cameraBehavior.RenderTexture = textures.Pop();
            cameraBehavior.AreaManager = _areaManager;
            throw new NotImplementedException();
            //cameraBehavior.ModelControl = _modelControl;
            cameraBehavior.Name = cameraName;
            // Get all Camera components attached to children of the script's GameObject (since there are multiple)
            _cameras.Add(cameraName, cameraBehavior);

        }
        UpdateVisibleUI();

    }

    public void DeleteCamera(List<string> cameraNames)
    {
        foreach (string cameraName in cameraNames)
        {
            textures.Push(_cameras[cameraName].RenderTexture);
            GameObject.Destroy(_cameras[cameraName].gameObject);
            _cameras.Remove(cameraName);
        }
        UpdateVisibleUI();
    }

    private void UpdateVisibleUI()
    {
        for (int i = 0; i < _cameraUIGOs.Count; i++)
            _cameraUIGOs[i].SetActive(i < (_cameras.Count-1));
    }

    //each function below works by first checking if camera exits in dictionary, and then calling cameraBehavior function
    public void SetCameraRotation(Dictionary<string, List<float>> cameraRotation) {
        foreach (var kvp in cameraRotation)
        {
            if (_cameras.ContainsKey(kvp.Key)) {
                _cameras[kvp.Key].SetCameraRotation(kvp.Value);
            }
        }
    }

    public void SetCameraZoom(Dictionary<string, float> cameraZoom) {
        foreach (var kvp in cameraZoom)
        {
            if (_cameras.ContainsKey(kvp.Key))
            {
                _cameras[kvp.Key].SetCameraZoom(kvp.Value);
            }
        }
    }

    public void SetCameraMode(Dictionary<string, string> cameraMode) {
        foreach (var kvp in cameraMode)
        {
            if (_cameras.ContainsKey(kvp.Key))
            {
                _cameras[kvp.Key].SetCameraMode(kvp.Value);
            }

            if (kvp.Key == "main") {
                _uiCanvas.worldCamera = _cameras[kvp.Key].ActiveCamera;
            }
        }
    }

    public void Screenshot(string data)
    {
        ScreenshotData screenshotData = JsonUtility.FromJson<ScreenshotData>(data);

        _cameras[screenshotData.name].Screenshot(screenshotData.size);
    }

    public void SetCameraPosition(Dictionary<string, List<float>> cameraPosition) {
        foreach (var kvp in cameraPosition)
        {
            if (_cameras.ContainsKey(kvp.Key))
            {
                _cameras[kvp.Key].SetCameraPosition(kvp.Value);
            }
        }
    }

    public void SetCameraYAngle(Dictionary<string, float> cameraYAngle) {
        foreach (var kvp in cameraYAngle)
        {
            if (_cameras.ContainsKey(kvp.Key))
            {
                _cameras[kvp.Key].SetCameraYAngle(kvp.Value);
            }
        }
    }

    public void SetCameraTargetArea(Dictionary<string, string> cameraTargetArea) {
        foreach (var kvp in cameraTargetArea)
        {
            if (_cameras.ContainsKey(kvp.Key))
            {
                _cameras[kvp.Key].SetCameraTargetArea(kvp.Value);
            }
        }
    }

    public void SetCameraTarget(Dictionary<string, List<float>> cameraTargetmlapdv) {
        foreach (var kvp in cameraTargetmlapdv)
        {
            if (_cameras.ContainsKey(kvp.Key))
            {
                _cameras[kvp.Key].SetCameraTarget(kvp.Value);
            }
        }
    }

    public void SetCameraPan(Dictionary<string, List<float>> cameraPanXY) {
        foreach (var kvp in cameraPanXY)
        {
            if (_cameras.ContainsKey(kvp.Key))
            {
                _cameras[kvp.Key].SetCameraPan(kvp.Value);
            }
        }
    }

    public void SetCameraControl(string cameraName)
    {
        //set everything to false except for given camera
        foreach(var kvp in _cameras)
        {
            if (kvp.Key == cameraName)
            {
                kvp.Value.SetCameraControl(true);
            }
            else
            {
                kvp.Value.SetCameraControl(false);
            }

            //kvp.Value.SetCameraControl(kvp.Key == cameraName);
        }
    }
    #endregion

    #region Public light functions

    /// <summary>
    /// Reset the camera-light link to the main camera
    /// </summary>
    public void SetLightCameraLink()
    {
        _lightBehavior.SetCamera(mainCamera.gameObject);
    }

    /// <summary>
    /// Set the camera-light link to a specific camera in the scene
    /// </summary>
    /// <param name="newCameraGO"></param>
    public void SetLightCameraLink(string cameraName)
    {
        _lightBehavior.SetCamera(_cameras[cameraName].gameObject);
    }

    /// <summary>
    /// Rotate the light in the scene to a specific set of euler angles
    /// </summary>
    /// <param name="eulerAngles"></param>
    public void SetLightRotation(List<float> eulerAngles)
    {
        Debug.Log($"Received new light angles");
        _lightBehavior.SetRotation(new Vector3(eulerAngles[0], eulerAngles[1], eulerAngles[2]));
    }

    #endregion

    #region JSON data definitions

    [Serializable]
    private struct ScreenshotData
    {
        public string name;
        public int[] size;
    }
    #endregion
}