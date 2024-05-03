using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayerMovementController : MonoBehaviour
{
    public static PlayerMovementController instance;
    public AnimatorMessager m_animatorMessager;
    public GameObject m_swordHitBox;

    private CapsuleCollider m_collider;
    private Rigidbody m_rigidbody;

    public Animator m_animator;
    public float m_animationSwitchSpeed = 0.1f;

    public float m_currentMovementSpeed;
    public float m_acceleration = 1.0f;

    public float m_walkSpeed;
    public float m_runSpeed;

    public bool m_isRunning = false;

    public Vector3 m_currentDirection = Vector3.zero;
    public Vector3 m_currentDirectionTarget
    {
        get
        {
            return m_currentDirection_LR + m_currentDirection_FB;
        }
    }

    public Vector3 m_currentDirection_LR = Vector3.zero;
    public Vector3 m_currentDirection_FB = Vector3.zero;


    public float m_attackCommand = 0.0f;
    public float m_attackCommandDecreaseSpeed = 0.0f;

    public TrailRenderer m_trailRenderer;

    UnityAction e_onPlayerJump;


    private void Awake()
    {
        // Set singleton instance for PlayerMovementController
        instance = this;
    }


    void Start()
    {
        // get core components from the player
        m_collider = this.GetComponent<CapsuleCollider>();
        m_rigidbody = this.GetComponent<Rigidbody>();

        // register events 
        e_onPlayerJump += OnPlayerJump;
        m_animatorMessager.e_onJumpEnd += OnJumpEnd;

        // register function for attack actionsevent
        m_animatorMessager.e_onAttack += () =>
        {
            // enable trail renderer when attack action is playing
            m_trailRenderer.emitting = true;
            // enable weapon hitbox when attack action is playing
            m_swordHitBox.SetActive(true);
        };

        // register function for attack end event
        m_animatorMessager.e_onAttackEnd += () =>
        {
            // disable trail renderer when attack action ends
            m_trailRenderer.emitting = false;
            // disable weapon hitbox when attack action ends
            m_swordHitBox.SetActive(false);
        };

        // register function for movement event
        m_animatorMessager.e_onMovement += () =>
        {
            // haven't decided to do anything here yet
        };

        // hide cursor and lock cursor to center of the screen
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        PlayerInput();
        PlayerMovement();
    }

    /// <summary>
    /// process player input actions
    /// </summary>
    void PlayerInput()
    {
        // enable running when left shift is pressed
        m_isRunning = Input.GetKey(KeyCode.LeftShift);

        // trigger jump event when space key is pressed
        if (Input.GetKeyDown(KeyCode.Space))
        {
            e_onPlayerJump.Invoke();
        }

        // trigger attack event when left mouse button is pressed
        if (Input.GetMouseButtonDown(0))
        {
            m_attackCommand += 1;
            
            switch (m_animatorMessager.m_currentAttackAction)
            {
                case 1:
                    m_attackCommand = Mathf.Clamp(m_attackCommand, 0, 2);
                    break;
                case 2:
                    m_attackCommand = Mathf.Clamp(m_attackCommand, 0, 3);
                    break;
                case 3:
                    m_attackCommand = Mathf.Clamp(m_attackCommand, 0, 4);
                    break;
            }
        }

        // decrease attack command value though time
        if (m_attackCommand > 0)
        {
            m_attackCommand -= m_attackCommandDecreaseSpeed * Time.deltaTime;
            m_attackCommand = Mathf.Clamp(m_attackCommand, 0, 5);
            m_animator.SetFloat("Attack", m_attackCommand);
        }
    }

    /// <summary>
    /// process player movement update
    /// </summary>
    void PlayerMovement()
    {
        // set movement direction based on player input
        m_currentDirection_LR = Input.GetAxis("Horizontal") * Vector3.right * (m_isRunning ? 2 : 1);
        m_currentDirection_FB = Input.GetAxis("Vertical") * Vector3.forward * (m_isRunning ? 2 : 1);

        // use lerping to smooth the movement direction for aniamtor controller
        m_currentDirection = Vector3.Lerp(m_currentDirection, m_currentDirectionTarget, m_animationSwitchSpeed * Time.deltaTime);

        // set animator parameters for movement
        m_animator.SetFloat("Movement_x", m_currentDirection.x);
        m_animator.SetFloat("Movement_y", m_currentDirection.z);

        // use lerping to smooth the movement speed for player movement
        m_currentMovementSpeed = 
            Mathf.Lerp(m_currentMovementSpeed, m_isRunning ? m_runSpeed : m_walkSpeed, m_acceleration * Time.deltaTime)
            * (m_animatorMessager.m_currentAttackAction == 0 ? 1 : 0);

        // set player rotation based on camera focus point
        Vector3 targetDirection = PlayerCameraController.instance.m_focusPoint.position;
        targetDirection.y = m_animator.transform.position.y;
        m_animator.transform.rotation = Quaternion.LookRotation(targetDirection - m_animator.transform.position, Vector3.up);

        // move player based on current movement direction and speed
        Vector3 _currentMoveDir = m_currentDirection.x * m_animator.transform.right + m_currentDirection.z * m_animator.transform.forward;
        this.transform.position += _currentMoveDir * m_currentMovementSpeed * Time.deltaTime;
    }

    /// <summary>
    /// process actions when player jump
    /// </summary>
    void OnPlayerJump()
    {
        // trigger jump to animator
        m_animator.SetTrigger("Jump");
        // disable gravity and set collider height and center for jump action
        m_rigidbody.useGravity = false;
        m_collider.height = 1f;
        m_collider.center = Vector3.up * 0.5f;
    }

    /// <summary>
    /// process actions when player jump ends
    /// </summary>
    void OnJumpEnd()
    {
        // set ridigbody gravity set collider height and center back to default
        m_collider.height = 2f;
        m_collider.center = Vector3.zero;
        m_rigidbody.useGravity = true;

    }


}
