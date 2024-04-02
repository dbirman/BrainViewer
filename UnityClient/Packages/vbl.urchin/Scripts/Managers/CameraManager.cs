using BrainAtlas;
using System;
using System.Collections.Generic;
using UnityEngine;
using Urchin.API;
using Urchin.Behaviors;
using Urchin.Cameras;

namespace Urchin.Managers
{
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
        [SerializeField] private AtlasManager _areaManager;
        [SerializeField] private Canvas _uiCanvas;

        // Used for doing independent brain Yaw rotations
        [SerializeField] private List<Transform> _yawTransforms;

        [SerializeField] private List<GameObject> _cameraUIGOs;
        #endregion

        #region Variables
        private Dictionary<string, CameraBehavior> _cameras; //directly access the camera nested within the prefab
        private Stack<RenderTexture> textures = new();

        private Quaternion _startRotation;
        private Quaternion _endRotation;
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

        private void Start()
        {
            Client_SocketIO.SetCameraLerpRotation += SetLerpStartEnd;
            Client_SocketIO.SetCameraLerp += SetLerp;
            Client_SocketIO.CameraBrainYaw += SetBrainYaw;

            Client_SocketIO.SetCameraTarget += SetCameraTarget;
            Client_SocketIO.SetCameraRotation += SetCameraRotation;
            Client_SocketIO.SetCameraTargetArea += SetCameraTargetArea;
            Client_SocketIO.SetCameraZoom += SetCameraZoom;
            Client_SocketIO.SetCameraPan += SetCameraPan;
            Client_SocketIO.SetCameraMode += SetCameraMode;
            Client_SocketIO.SetCameraColor += SetCameraColor;
            Client_SocketIO.SetCameraControl += SetCameraControl;
            Client_SocketIO.RequestScreenshot += RequestScreenshot;
            Client_SocketIO.SetCameraYAngle += SetCameraYAngle;
            Client_SocketIO.CreateCamera += CreateCamera;
            Client_SocketIO.DeleteCamera += DeleteCamera;
        }

        #endregion


        #region Public camera functions

        public void CreateCamera(List<string> cameraNames)
        {
            
            //instantiating game object w camera component
            foreach (string cameraName in cameraNames)
            {
                if (_cameras.ContainsKey(cameraName))
                {
                    Debug.LogWarning($"Camera {cameraName} was created twice. The camera will not be re-created");
                    continue;
                }

#if UNITY_EDITOR
                Debug.Log($"{cameraName} created");
#endif
                GameObject tempObject = Instantiate(_cameraPrefab);
                tempObject.name = $"camera_{cameraName}";
                CameraBehavior cameraBehavior = tempObject.GetComponent<CameraBehavior>();
                cameraBehavior.RenderTexture = textures.Pop();
                cameraBehavior.Name = cameraName;
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
                _cameraUIGOs[i].SetActive(i < (_cameras.Count - 1));
        }

        //each function below works by first checking if camera exits in dictionary, and then calling cameraBehavior function
        public void SetCameraRotation(Dictionary<string, List<float>> cameraRotation)
        {
            foreach (var kvp in cameraRotation)
            {
                if (_cameras.ContainsKey(kvp.Key))
                {
                    Vector3 yawPitchRoll = new Vector3(kvp.Value[0], kvp.Value[1], kvp.Value[2]);
                    _cameras[kvp.Key].SetCameraRotation(yawPitchRoll);
                }
            }
        }

        /// <summary>
        /// Set the startRotation/endRotation quaternions
        /// </summary>
        /// <param name="data"></param>
        public void SetLerpStartEnd(CameraRotationModel data)
        {
            _startRotation = Quaternion.Euler(data.StartRotation);
            _endRotation = Quaternion.Euler(data.EndRotation);
        }

        /// <summary>
        /// Interpolate between the active startRotation and endRotation values 0->1
        /// </summary>
        /// <param name="data">(string ID, Vector3 Value)</param>
        public void SetLerp(FloatData data)
        {
            if (_cameras.ContainsKey(data.ID))
            {
                _cameras[data.ID].ActiveCamera.transform.rotation = Quaternion.Lerp(_startRotation, _endRotation, data.Value);
            }
        }

        public void SetCameraZoom(Dictionary<string, float> cameraZoom)
        {
            foreach (var kvp in cameraZoom)
            {
                if (_cameras.ContainsKey(kvp.Key))
                {
                    _cameras[kvp.Key].SetCameraZoom(kvp.Value);
                }
            }
        }

        public void SetCameraMode(Dictionary<string, string> cameraMode)
        {
            foreach (var kvp in cameraMode)
            {
                if (_cameras.ContainsKey(kvp.Key))
                {
                    _cameras[kvp.Key].SetCameraMode(kvp.Value);
                }

                if (kvp.Key == "CameraMain")
                {
                    _uiCanvas.worldCamera = _cameras[kvp.Key].ActiveCamera;
                }
            }
        }

        public void SetCameraColor(Dictionary<string, string> cameraColor)
        {
            foreach (var kvp in cameraColor)
            {
                if (_cameras.ContainsKey(kvp.Key))
                    _cameras[kvp.Key].SetBackgroundColor(kvp.Value);
                else
                    Client_SocketIO.LogError($"(CameraManager) Camera {kvp.Key} does not exist, cannot set background color to {kvp.Value}");
            }
        }

        public void RequestScreenshot(string data)
        {
            ScreenshotData screenshotData = JsonUtility.FromJson<ScreenshotData>(data);

            _cameras[screenshotData.name].Screenshot(screenshotData.size);
        }

        public void SetCameraYAngle(Dictionary<string, float> cameraYAngle)
        {
            foreach (var kvp in cameraYAngle)
            {
                if (_cameras.ContainsKey(kvp.Key))
                {
                    _cameras[kvp.Key].IncrementRoll(kvp.Value);
                }
            }
        }

        public void SetCameraTargetArea(Dictionary<string, string> cameraTargetArea)
        {
            foreach (var kvp in cameraTargetArea)
            {
                if (_cameras.ContainsKey(kvp.Key))
                {
                    // target area needs to be parsed by AtlasManager
                    var areaData = AtlasManager.GetID(kvp.Value);

                    Vector3 coordAtlas = Vector3.zero;
                    if (areaData.leftSide)
                        coordAtlas = BrainAtlasManager.ActiveReferenceAtlas.MeshCenters[areaData.ID].left;
                    else if (areaData.full)
                        coordAtlas = BrainAtlasManager.ActiveReferenceAtlas.MeshCenters[areaData.ID].full;
                    else if (areaData.rightSide)
                    {
                        coordAtlas = BrainAtlasManager.ActiveReferenceAtlas.MeshCenters[areaData.ID].left;
                        // invert the ML axis
                        coordAtlas.y = BrainAtlasManager.ActiveReferenceAtlas.Dimensions.y - coordAtlas.y;
                    }

                    coordAtlas /= 1000f;

                    Vector3 coordWorld = BrainAtlasManager.ActiveReferenceAtlas.Atlas2World(coordAtlas);

                    Debug.LogWarning("Mesh center needs to target full/left/right correctly!!");
                    _cameras[kvp.Key].SetCameraTarget(coordWorld);
                }
            }
        }

        public void SetCameraTarget(Dictionary<string, List<float>> cameraTargetmlapdv)
        {
            foreach (var kvp in cameraTargetmlapdv)
            {
                if (_cameras.ContainsKey(kvp.Key))
                {
                    Vector3 coordAtlas = new Vector3(kvp.Value[0], kvp.Value[1], kvp.Value[2]);
                    _cameras[kvp.Key].SetCameraTarget(coordAtlas);
                }
            }
        }

        public void SetCameraPan(Dictionary<string, List<float>> cameraPanXY)
        {
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
            foreach (var kvp in _cameras)
                kvp.Value.SetCameraControl(kvp.Key == cameraName);
        }

        public void SetBrainYaw(FloatData data)
        {
            foreach (Transform t in _yawTransforms)
                t.rotation = Quaternion.Euler(0f, data.Value, 0f);
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
}