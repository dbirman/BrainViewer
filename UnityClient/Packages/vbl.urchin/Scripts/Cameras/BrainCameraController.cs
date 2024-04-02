using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class BrainCameraController : MonoBehaviour
{
    #region Exposed fields
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private GameObject _mainCameraRotator;
    [SerializeField] private Transform _mainCameraTargetT;
    [SerializeField] private Transform _zoomT;
    #endregion

    #region Properties
    public Vector3 CameraTarget { get { return _mainCameraTargetT.localPosition; } }

    public float minFoV = 15.0f;
    public float maxFoV = 90.0f;
    public float fovDelta = 15.0f;
    public float orthoDelta = 5.0f;
    public float moveSpeed = 10.0f;
    public float minXRotation = -180f;
    public float maxXRotation = 180f;
    public float minZRotation = -180f;
    public float maxZRotation = 180f;
    #endregion

    #region Private vars
    private bool mouseDownOverBrain;
    private int mouseButtonDown;
    private bool brainTransformChanged;
    private float lastLeftClick;
    private float lastRightClick;


    // auto-rotation
    private bool autoRotate;
    private float autoRotateSpeed = 10.0f;
    #endregion

    #region Events
    public UnityEvent<Vector3> RotationChangedEvent;
    #endregion

    // Speed and rotation controls

    #region Rotation
    private Quaternion _initialRotation;
    public float rotSpeed = 200.0f;

    private float zoom;
    private Vector3 _pitchYawRoll;

    public Vector3 PitchYawRoll { get { return _pitchYawRoll; } }
    #endregion

    public static bool BlockBrainControl;
    public bool UserControllable = true;

    public static float doubleClickTime = 0.2f;

    private void Awake()
    {
        // Artifically limit the framerate
#if !UNITY_WEBGL
        Application.targetFrameRate = 144;
#endif

        _initialRotation = transform.rotation;
        autoRotate = false;

        lastLeftClick = Time.realtimeSinceStartup;
        lastRightClick = Time.realtimeSinceStartup;
    }

    // Update is called once per frame
    void Update()
    {
        if (!UserControllable)
            return;

        // Now check if the mouse wheel is being held down
        if (Input.GetMouseButton(1) && !BlockBrainControl && !EventSystem.current.IsPointerOverGameObject())
        {
            mouseDownOverBrain = true;
            mouseButtonDown = 1;
        }

        bool thisFrameDown = false;
        // Now deal with dragging
        if (Input.GetMouseButtonDown(0) && !BlockBrainControl && !EventSystem.current.IsPointerOverGameObject())
        {
            //BrainCameraDetectTargets();
            mouseDownOverBrain = true;
            mouseButtonDown = 0;
            autoRotate = false;
            thisFrameDown = true;
        }

        // Check the scroll wheel and deal with the field of view
        if (!EventSystem.current.IsPointerOverGameObject() && !thisFrameDown)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
            {
                Vector3 localZoom = _zoomT.localPosition;
                zoom = localZoom.z * (1 - scroll / 10f);

                if (_mainCamera.orthographic)
                    _mainCamera.orthographicSize = -zoom/2f;

                localZoom.z = zoom;
                _zoomT.localPosition = localZoom;
            }

        }

        if (autoRotate)
        {
            _pitchYawRoll += Vector3.up * autoRotateSpeed * Time.deltaTime;
            ApplyBrainCameraPositionAndRotation();
        }
        else if (!thisFrameDown)
            BrainCameraControl_noTarget();
    }

    public void SetControlBlock(bool state)
    {
        BlockBrainControl = state;
    }


    void BrainCameraControl_noTarget()
    {
        if (Input.GetMouseButtonUp(0))
            SetControlBlock(false);

        if (mouseDownOverBrain)
        {
            // Deal with releasing the mouse (anywhere)
            if (mouseButtonDown == 0 && Input.GetMouseButtonUp(0))
            {
                lastLeftClick = Time.realtimeSinceStartup;
                ClearMouseDown(); return;
            }
            if (mouseButtonDown == 1 && Input.GetMouseButtonUp(1))
            {
                if (!brainTransformChanged)
                {
                    // Check for double click
                    if ((Time.realtimeSinceStartup - lastRightClick) < doubleClickTime)
                    {
                        // Reset the brainCamera transform position
                        _mainCamera.transform.localPosition = Vector3.zero;
                    }
                }

                lastRightClick = Time.realtimeSinceStartup;
                ClearMouseDown(); return;
            }

            if (mouseButtonDown == 1)
            {
                // While right-click is held down 
                float xMove = -Input.GetAxis("Mouse X") * moveSpeed * Time.deltaTime;
                float yMove = -Input.GetAxis("Mouse Y") * moveSpeed * Time.deltaTime;

                if (xMove != 0 || yMove != 0)
                {
                    brainTransformChanged = true;
                    _mainCamera.transform.Translate(xMove, yMove, 0, Space.Self);
                }
            }

            // If the mouse is down, even if we are far way now we should drag the brain
            if (mouseButtonDown == 0)
            {
                float roll = Input.GetAxis("Mouse X") * rotSpeed * Time.deltaTime;
                float pitch = Input.GetAxis("Mouse Y") * rotSpeed * Time.deltaTime;

                if (roll != 0 || pitch != 0)
                {
                    brainTransformChanged = true;

                    // Pitch Locally, Yaw Globally. See: https://gamedev.stackexchange.com/questions/136174/im-rotating-an-object-on-two-axes-so-why-does-it-keep-twisting-around-the-thir

                    // if space is down, we can apply yaw instead of roll
                    if (Input.GetKey(KeyCode.Space))
                    {
                        _pitchYawRoll.z = Mathf.Clamp(_pitchYawRoll.z + roll, minXRotation, maxXRotation);
                    }
                    else
                    {
                        _pitchYawRoll.x = Mathf.Clamp(_pitchYawRoll.x - pitch, minXRotation, maxXRotation);
                        _pitchYawRoll.y = Mathf.Clamp(_pitchYawRoll.y + roll, minZRotation, maxZRotation);
                    }
                    ApplyBrainCameraPositionAndRotation();
                }
            }
        }
    }
    void ApplyBrainCameraPositionAndRotation()
    {
        //See the bottom example for that.Yaw locally, pitch globally is the order you want if you're manipulating a spinning globe,
        //and you want it to always yaw around ITS north & south poles, but pitch toward/away from YOU (here taken as a fixed
        //observer in global space). When controlling the viewpoint, you want to do the opposite: pitch around YOUR x axis
        //(running from your left to right ear, for example), but yaw around the WORLD's y axis(parallel to the gravity vector)

        _mainCameraRotator.transform.rotation = _initialRotation;

        _mainCameraRotator.transform.Rotate(_mainCameraRotator.transform.right, _pitchYawRoll.x);
        _mainCameraRotator.transform.Rotate(Vector3.up, _pitchYawRoll.y);
        _mainCameraRotator.transform.Rotate(Vector3.forward, _pitchYawRoll.z);
        

        //_mainCameraRotator.transform.rotation = _initialRotation * Quaternion.Euler(_pitchYawRoll);
        RotationChangedEvent.Invoke(_pitchYawRoll);
    }
    void ClearMouseDown()
    {
        mouseDownOverBrain = false;
        //brainCameraClickthroughTarget = null;
        brainTransformChanged = false;
    }

    public float GetZoom()
    {
        return _mainCamera.orthographic ? _mainCamera.orthographicSize : _mainCamera.fieldOfView;
    }

    public void SetZoom(float zoom)
    {
        if (_mainCamera.orthographic)
        {
            //Settings.CameraZoom = zoom;
            _mainCamera.orthographicSize = zoom;
        }
        else
            _mainCamera.fieldOfView = zoom;
    }

    public void SetBrainAxisAngles(Vector3 yawPitchRoll)
    {
        _pitchYawRoll = yawPitchRoll;
        ApplyBrainCameraPositionAndRotation();
    }

    public void SetCameraTarget(Vector3 newTarget)
    {
        Debug.Log("Setting camera target to: " + newTarget);
        _mainCameraTargetT.localPosition = newTarget;

        // Reset any panning 
        _mainCamera.transform.localPosition = Vector3.zero;
    }

    public void ResetCameraTarget()
    {
        _mainCameraTargetT.localPosition = Vector3.zero;
        ApplyBrainCameraPositionAndRotation();
    }

    public void SetCameraContinuousRotation(bool state)
    {
        autoRotate = state;
    }

    public void SetCameraRotationSpeed(float speed)
    {
        autoRotateSpeed = speed;
    }

    public void SetCamera(Camera newCamera)
    {
        _mainCamera = newCamera;
        ApplyBrainCameraPositionAndRotation();
    }

    public Camera GetCamera()
    {
        return _mainCamera;
    }

    public void SetCameraBackgroundColor(Color newColor)
    {
        _mainCamera.backgroundColor = newColor;
    }
}