using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    public static PlayerCameraController instance;

    public LayerMask m_targetCameraBlockingLayer;

    public Transform m_positionRoot;
    public Transform m_rotationRoot;

    public Transform m_focusPoint;

    public Transform m_target;
    public Transform m_camera;


    public Vector3 m_cameraOffset;
    public Vector3 m_cameraRotation;
    public Vector3 m_cameraRotationOffset;

    public float m_cameraFollowSpeed = 1.0f;

    public float m_cameraZoomSpeed = 1.0f;
    public float m_targetArmLength;
    public float m_currentArmLength;
    public float m_armLengthMin;
    public float m_armLengthMax;


    public Vector3 m_cameraShake;

    private void Awake()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        // set initial camera position at the middle of the player and the target
        m_targetArmLength = (m_armLengthMax + m_armLengthMin) / 2; 
        // create a focus point for the camera
        m_focusPoint = new GameObject("Focus Point").transform;
    }

    // Update is called once per frame
    void Update()
    {
        // update the focus point position to be in front of the camera
        m_focusPoint.position = m_camera.position + m_camera.forward * 10;

        CameraZooming();

        CameraRotation();

    }

    void CameraRotation()
    {
        m_cameraRotationOffset.x -= Input.GetAxis("Mouse Y");
        m_cameraRotationOffset.y += Input.GetAxis("Mouse X");

        m_rotationRoot.localRotation = Quaternion.Euler(m_cameraRotation + m_cameraRotationOffset);
        m_rotationRoot.localPosition = m_cameraOffset;

    }

    /// <summary>
    /// process camera zooming behavior
    /// </summary>
    void CameraZooming()
    {
        // zooming camera based on player mouse scroll wheel
        if (Input.GetAxis("Mouse ScrollWheel") > 0)
        {
            m_targetArmLength -= 1;
        }
        else if (Input.GetAxis("Mouse ScrollWheel") < 0)
        {
            m_targetArmLength += 1;
        }

        // get desired camera arm length based on player input
        m_targetArmLength = Mathf.Clamp(m_targetArmLength, m_armLengthMin, m_armLengthMax);

        // check if there is any object blocking the camera view
        RaycastHit _hit = new RaycastHit();
        if (Physics.Linecast(m_positionRoot.position, m_camera.position, out _hit, m_targetCameraBlockingLayer))
        {
            // if so, set the camera arm length to the distance from the camera to the blocking object
            if (_hit.collider.gameObject.CompareTag("WorldStatic"))
            {
                m_targetArmLength = Vector3.Distance(m_positionRoot.position, _hit.point) - 1f;
            }
        }

        // lerp the current camera arm length to the desired camera arm length
        m_currentArmLength = Mathf.Lerp(m_currentArmLength, m_targetArmLength, m_cameraZoomSpeed * Time.deltaTime);

        // update camera follow speed based on presetted value & player movement speed
        float _cameraSpeed = m_cameraFollowSpeed + PlayerMovementController.instance.m_currentMovementSpeed;
        // update camera position to followed target position
        m_positionRoot.position = Vector3.Lerp(m_positionRoot.position, m_target.position, _cameraSpeed * Time.deltaTime);

        // set camera position based on the current camera arm length
        // and add camera shake effect
        m_camera.localPosition = Vector3.back * m_currentArmLength + m_cameraShake;
    }

    /// <summary>
    /// shake camera for a certain duration and magnitude
    /// </summary>
    /// <param name="_duration">Camera shaking duration</param>
    /// <param name="_magnitude">Camera shaking power</param>
    /// <returns></returns>
    public IEnumerator CameraShake(float _duration, float _magnitude)
    {
        float _elapsed = 0.0f;

        // shake camera for a certain duration in every frame
        while (_elapsed < _duration)
        {
            m_cameraShake = Vector3.one * Random.Range(-1f, 1f) * _magnitude;

            _elapsed += Time.deltaTime;

            yield return new WaitForEndOfFrame();
        }

        // reset camera shake effect
        m_cameraShake = Vector3.zero;
    }

    /// <summary>
    /// pause the game time for a certain duration
    /// </summary>
    /// <param name="_duration"></param>
    /// <param name="_scale"></param>
    /// <returns></returns>
    public IEnumerator TimePause(float _duration, float _scale)
    {
        Time.timeScale = _scale;

        float _elapsed = 0.0f;

        // pause the game time for a certain duration
        while (_elapsed < _duration)
        {
            _elapsed += Time.unscaledDeltaTime;

            yield return new WaitForEndOfFrame();
        }

        // smoothly return the game time to normal
        while (Time.timeScale < 1)
        {
            Time.timeScale += Time.unscaledDeltaTime * 2;
            yield return new WaitForEndOfFrame();
        }

        // set game time back to normal
        Time.timeScale = 1;

    }
}
