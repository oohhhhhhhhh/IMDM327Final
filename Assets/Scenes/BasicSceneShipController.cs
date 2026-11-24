using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class BasicSceneShipController : MonoBehaviour
{
    [SerializeField] private float forwardSpeed = 8f;
    [SerializeField] private float yawSpeed = 50f;
    [SerializeField] private float pitchSpeed = 50f;
    [SerializeField] private float rollSpeed = 75f;
    [SerializeField] private float mouseSensitivity = 0.08f;
    [SerializeField] private float fireInterval = 0.2f;
    [SerializeField] private float projectileSpeed = 60f;
    [SerializeField] private float projectileLifetime = 5f;
    [SerializeField] private float cockpitCameraHeight = 0.8f;
    [SerializeField] private float cockpitCameraBackOffset = 1.5f;
    [SerializeField] private float skyboxRotationSpeed = 1f;

    private readonly InputAction _leftStick = new InputAction(
        "Left Stick", InputActionType.Value, expectedControlType: "Vector2");
    private readonly InputAction _rightStick = new InputAction(
        "Right Stick", InputActionType.Value, expectedControlType: "Vector2");
    private readonly InputAction _keyboardMove = new InputAction(
        "Keyboard Move", InputActionType.Value, expectedControlType: "Vector2");
    private readonly InputAction _keyboardRoll = new InputAction(
        "Keyboard Roll", InputActionType.Value, expectedControlType: "Axis");
    private readonly InputAction _mouseLook = new InputAction(
        "Mouse Look", InputActionType.Value, expectedControlType: "Vector2");
    private readonly InputAction _fire = new InputAction(
        "Fire", InputActionType.Button);
    private Transform _ship;
    private Transform[] _blasters;
    private float _nextFireTime;
    private Material _originalSkybox;
    private Material _rotatingSkybox;
    private float _skyboxRotation;

    private void Awake()
    {
        _leftStick.AddBinding("<XRController>{LeftHand}/{Primary2DAxis}");
        _rightStick.AddBinding("<XRController>{RightHand}/{Primary2DAxis}");
        _keyboardMove.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        _keyboardRoll.AddCompositeBinding("1DAxis")
            .With("Positive", "<Keyboard>/e")
            .With("Negative", "<Keyboard>/q");
        _mouseLook.AddBinding("<Mouse>/delta");
        _fire.AddBinding("<XRController>{LeftHand}/{TriggerButton}");
        _fire.AddBinding("<XRController>{RightHand}/{TriggerButton}");
        _fire.AddBinding("<Keyboard>/space");
        _fire.AddBinding("<Mouse>/leftButton");
    }

    private void OnEnable()
    {
        _leftStick.Enable();
        _rightStick.Enable();
        _keyboardMove.Enable();
        _keyboardRoll.Enable();
        _mouseLook.Enable();
        _fire.Enable();
    }

    private void Start()
    {
        SetupRotatingSkybox();

        var shipObject = GameObject.Find("Example1_Grey");
        var xrOrigin = GameObject.Find("XR Origin (XR Rig)");
        if (shipObject == null || xrOrigin == null)
        {
            Debug.LogError("Ship flight setup requires Example1_Grey and XR Origin (XR Rig) in BasicScene.", this);
            enabled = false;
            return;
        }

        _ship = shipObject.transform;
        var blasters = new List<Transform>();
        foreach (var child in shipObject.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "Blaster")
            {
                blasters.Add(child);
            }
        }

        _blasters = blasters.ToArray();
        if (_blasters.Length == 0)
        {
            Debug.LogError("No Blaster modules were found on Example1_Grey.", this);
            enabled = false;
            return;
        }

        var seat = FindSeat(shipObject.transform);
        var cameraOffset = xrOrigin.transform.Find("Camera Offset");
        if (seat == null || cameraOffset == null)
        {
            Debug.LogError("Could not find the ship cockpit seat or XR camera offset.", this);
            enabled = false;
            return;
        }

        cameraOffset.localPosition = new Vector3(
            cameraOffset.localPosition.x, cockpitCameraHeight, -cockpitCameraBackOffset);
        xrOrigin.transform.SetParent(_ship, false);
        xrOrigin.transform.localPosition = new Vector3(0f, 0.08f, 1.8f);
        xrOrigin.transform.localRotation = Quaternion.identity;
    }

    private void Update()
    {
        if (_rotatingSkybox != null)
        {
            _skyboxRotation = Mathf.Repeat(
                _skyboxRotation + skyboxRotationSpeed * Time.deltaTime, 360f);
            _rotatingSkybox.SetFloat("_Rotation", _skyboxRotation);
        }

        var left = _leftStick.ReadValue<Vector2>();
        var right = _rightStick.ReadValue<Vector2>();
        left = Vector2.ClampMagnitude(left + _keyboardMove.ReadValue<Vector2>(), 1f);
        var mouse = _mouseLook.ReadValue<Vector2>();
        var roll = right.x + _keyboardRoll.ReadValue<float>();
        var deltaTime = Time.deltaTime;

        _ship.Rotate(
            -right.y * pitchSpeed * deltaTime - mouse.y * mouseSensitivity,
            left.x * yawSpeed * deltaTime + mouse.x * mouseSensitivity,
            -roll * rollSpeed * deltaTime,
            Space.Self);
        _ship.position += _ship.forward * (left.y * forwardSpeed * deltaTime);

        if (_fire.IsPressed() && Time.time >= _nextFireTime)
        {
            FireBlasters();
            _nextFireTime = Time.time + fireInterval;
        }
    }

    private void OnDisable()
    {
        _leftStick.Disable();
        _rightStick.Disable();
        _keyboardMove.Disable();
        _keyboardRoll.Disable();
        _mouseLook.Disable();
        _fire.Disable();
    }

    private void OnDestroy()
    {
        _leftStick.Dispose();
        _rightStick.Dispose();
        _keyboardMove.Dispose();
        _keyboardRoll.Dispose();
        _mouseLook.Dispose();
        _fire.Dispose();

        if (_rotatingSkybox != null)
        {
            if (RenderSettings.skybox == _rotatingSkybox)
            {
                RenderSettings.skybox = _originalSkybox;
            }

            Destroy(_rotatingSkybox);
        }
    }

    private void SetupRotatingSkybox()
    {
        _originalSkybox = RenderSettings.skybox;
        if (_originalSkybox == null || !_originalSkybox.HasProperty("_Rotation"))
        {
            Debug.LogError("The active skybox material does not support rotation.", this);
            return;
        }

        _skyboxRotation = _originalSkybox.GetFloat("_Rotation");
        _rotatingSkybox = new Material(_originalSkybox)
        {
            name = _originalSkybox.name + " (Runtime)"
        };
        RenderSettings.skybox = _rotatingSkybox;
    }

    private void FireBlasters()
    {
        foreach (var blaster in _blasters)
        {
            var direction = blaster.forward;
            var projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = "Blaster Projectile";
            projectile.transform.SetPositionAndRotation(
                blaster.position + direction * 2.1f,
                Quaternion.LookRotation(direction));
            projectile.transform.localScale = Vector3.one * 0.12f;

            var renderer = projectile.GetComponent<Renderer>();
            renderer.material.color = new Color(1f, 0.25f, 0.05f);

            var body = projectile.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.linearVelocity = direction * projectileSpeed;
            Destroy(projectile, projectileLifetime);
        }
    }

    private static Transform FindSeat(Transform ship)
    {
        foreach (var child in ship.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "CockpitEquipments_Seat"
                && child.parent != null
                && child.parent.name == "Cockpit3_InteriorDetails"
                && child.childCount > 0)
            {
                return child;
            }
        }

        return null;
    }
}
