using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BrainAtlas;
using Urchin.API;

namespace Urchin.Behaviors
{

    public class CameraBehavior : MonoBehaviour
    {
        #region Serialized
        [SerializeField] BrainCameraController _cameraControl;
        [SerializeField] RectTransform _cropWindowRT;

        [SerializeField] Camera _orthoCamera;
        [SerializeField] Camera _perspectiveCamera;
        #endregion

        #region Properties
        public string Name;
        public RenderTexture RenderTexture
        {
            get
            {
                return _renderTexture;
            }

            set
            {
                _renderTexture = value;
                _orthoCamera.targetTexture = _renderTexture;
                _perspectiveCamera.targetTexture = _renderTexture;
            }
        }

        public Camera ActiveCamera
        {
            get
            {
                if (_orthoCamera.isActiveAndEnabled)
                {
                    return _orthoCamera;
                }
                else
                {
                    return _perspectiveCamera;
                }
            }
        }

        public CameraModel Data;
        #endregion

        #region Variables
        private RenderTexture _renderTexture;
        #endregion

        #region Public functions
        public void UpdateSettings()
        {
            _cameraControl.UserControllable = Data.Controllable;
            _cameraControl.SetBrainAxisAngles(Data.Rotation);
            _cameraControl.SetZoom(Data.Zoom);
            SetCameraMode(Data.Mode.Equals("orthographic"));
        }

        private void SetCameraMode(bool orthographic)
        {
            if (orthographic)
            {
                _orthoCamera.gameObject.SetActive(true);
                _perspectiveCamera.gameObject.SetActive(false);

                _cameraControl.SetCamera(_orthoCamera);
            }
            else
            {
                _orthoCamera.gameObject.SetActive(false);
                _perspectiveCamera.gameObject.SetActive(true);
                _cameraControl.SetCamera(_perspectiveCamera);
            }
        }

        public void SetBackgroundWhite(bool backgroundWhite)
        {
            SetBackgroundColor(backgroundWhite ? Color.white : Color.black);
        }

        public void SetBackgroundColor(string hexColor)
        {
            SetBackgroundColor(Utils.Utils.Hex2Color(hexColor));
        }

        public void SetBackgroundColor(Color newColor)
        {
            ActiveCamera.backgroundColor = newColor;
        }

        /// <summary>
        /// Take a screenshot and send it back via the ReceiveCameraImgMeta and ReceiveCameraImg messages
        /// </summary>
        /// <param name="size"></param>
        public void Screenshot(Vector2 size)
        {
            StartCoroutine(ScreenshotHelper(size));
        }

        /// <summary>
        /// Capture the output from this camera into a texture
        /// </summary>
        /// <returns></returns>
        private IEnumerator ScreenshotHelper(Vector2 size)
        {
            RenderTexture originalTexture = ActiveCamera.targetTexture;
            //int originalCullingMask = ActiveCamera.cullingMask;

            // Set the UI layer to not be rendered
            int uiLayer = LayerMask.NameToLayer("UI");
            ActiveCamera.cullingMask &= ~(1 << uiLayer);

            int width = Mathf.RoundToInt(size.x);
            int height = Mathf.RoundToInt(size.y);

            RenderTexture captureTexture = new RenderTexture(width, height, 24);
            ActiveCamera.targetTexture = captureTexture;

            yield return new WaitForEndOfFrame();

            // Save to Texture2D
            Texture2D screenshotTexture = new Texture2D(width, height, TextureFormat.RGB24, false);
            RenderTexture.active = captureTexture;
            screenshotTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            screenshotTexture.Apply();

            // return the camera
            ActiveCamera.targetTexture = originalTexture;
            //ActiveCamera.cullingMask = originalCullingMask;
            RenderTexture.active = null;
            captureTexture.Release();

            // Convert to PNG
            byte[] bytes = screenshotTexture.EncodeToPNG();

            // Build the messages and send them
            ScreenshotReturnMeta meta = new();
            meta.name = Name;
            meta.totalBytes = bytes.Length;
            Client_SocketIO.Emit("CameraImgMeta", JsonUtility.ToJson(meta));

            int nChunks = Mathf.CeilToInt((float)bytes.Length / (float)Client_SocketIO.SOCKET_IO_MAX_CHUNK_BYTES);

            for (int i = 0; i < nChunks; i++)
            {
                ScreenshotChunk chunk = new();
                chunk.name = Name;

                int cChunkSize = Mathf.Min(Client_SocketIO.SOCKET_IO_MAX_CHUNK_BYTES, bytes.Length - i * Client_SocketIO.SOCKET_IO_MAX_CHUNK_BYTES);
                chunk.data = new byte[cChunkSize];
                Buffer.BlockCopy(bytes, i * Client_SocketIO.SOCKET_IO_MAX_CHUNK_BYTES, chunk.data, 0, cChunkSize);
                Client_SocketIO.Emit("CameraImg", JsonUtility.ToJson(chunk));
            }
        }

        [Serializable]
        private struct ScreenshotReturnMeta
        {
            public string name;
            public int totalBytes;
        }

        [Serializable, PreferBinarySerialization]
        private class ScreenshotChunk
        {
            public string name;
            public byte[] data;
        }

        public void IncrementRoll(float yaw)
        {
            Vector3 angles = _cameraControl.PitchYawRoll;
            angles.y += yaw;
            _cameraControl.SetBrainAxisAngles(angles);
        }

        public void SetCameraTarget(Vector3 coordAtlas)
        {
            // data comes in in um units in ml/ap/dv
            // note that (0,0,0) in world is the center of the brain
            // so offset by (-6.6 ap, -4 dv, -5.7 lr) to get to the corner
            // in world space, x = ML, y = DV, z = AP
            _cameraControl.SetCameraTarget(BrainAtlasManager.ActiveReferenceAtlas.Atlas2World(coordAtlas));
        }

        public void SetCameraPan(List<float> panXY)
        {
            throw new NotImplementedException();
            //_cameraControl.SetP(new Vector2(panXY[0], panXY[1]));
        }
        #endregion
    }
}