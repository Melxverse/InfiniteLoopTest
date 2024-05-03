using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.UI;

public class EnemyController : MonoBehaviour
{
    public LayerMask m_targetLayerMask;
    public Transform m_currentTarget;
    public Image m_healthBar;

    private NavMeshAgent m_navMeshAgent;

    public Animator m_animator;
    private AnimatorMessager m_animatorMessager;

    private Transform m_moveTarget;
    private Transform m_detectedTarget;

    public float m_health = 100.0f;

    public enum EnemyState
    {
        Idle,
        Engage,
        Attack,
        Search,
    }
    public EnemyState m_state;


    // enemy detection range for different states
    public float m_attackRange = 1.0f;
    public float m_engageRange = 3.0f;
    public float m_searchRange = 10.0f;

    // stamina system, used for enemy to perform actions
    public float m_currentStamina = 1.0f;
    public float m_maxStamina = 1.0f;
    public float m_attackStaminaCost = 0.1f;

    public UnityAction e_gameTickSim;
    public float m_nextTimer = 0;

    public float m_attackCommand = 0.0f;


    bool m_isDead = false;
    float _movement_FB;

    /// <summary>
    /// draw gizmos for the search range, engage range, and attack range
    /// </summary>
    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        Handles.color = Color.green;
        Handles.DrawSolidDisc(transform.position + Vector3.down * 0.75f, Vector3.up, m_searchRange);
        Handles.color = Color.yellow;
        Handles.DrawSolidDisc(transform.position + Vector3.down * 0.75f, Vector3.up, m_engageRange);
        Handles.color = Color.red;
        Handles.DrawSolidDisc(transform.position + Vector3.down * 0.75f, Vector3.up, m_attackRange);
#endif
    }

    void Start()
    {
        // get core components
        m_navMeshAgent = GetComponent<NavMeshAgent>();
        m_animatorMessager = m_animator.GetComponent<AnimatorMessager>();

        // create a move target for the enemy, always been used as move target
        m_moveTarget = new GameObject("Move Target").transform;
        m_moveTarget.position = this.transform.position;
        m_navMeshAgent.SetDestination(m_moveTarget.position);
        // disable the auto rotation of the nav mesh agent, only use the position update
        m_navMeshAgent.updateRotation = false;

        // register the game tick event, use for simple state machine
        e_gameTickSim += ActorTick;

        // set the next timer to be ticked
        m_nextTimer++;
    }

    void Update()
    {
        // if the enemy is dead, stop doing anything
        if (m_isDead)
            return;


    }


    private void FixedUpdate()
    {
        // if the enemy is dead, stop doing anything
        if (m_isDead)
            return;

        // update the actor sim tick, use for simple state machine. usually it should be called from the game manager
        if (Time.time > m_nextTimer)
        {
            m_nextTimer++;
            e_gameTickSim?.Invoke();
        }

        // get the animation movement dirction based on the nav mesh agent velocity
        float _forward = Mathf.Round(m_navMeshAgent.velocity.x);
        _movement_FB = Mathf.Lerp(_movement_FB, _forward, Time.deltaTime * 3);

        // set the animator parameters for movement
        m_animator.SetFloat("Movement_y", _movement_FB);
        m_animator.SetFloat("Movement_x", -m_navMeshAgent.velocity.z);

        // decrease the attack command value through time
        if (m_attackCommand > 0)
        {
            m_attackCommand -= 3 * Time.deltaTime;
            m_animator.SetFloat("Attack", m_attackCommand);
        }

        // set the nav mesh agent destination to the move target
        m_navMeshAgent.SetDestination(m_moveTarget.position);

        // if there is a detected target, rotate the enemy to face the target
        if (m_detectedTarget != null)
        {
            Vector3 targetDirection = m_detectedTarget.position;
            targetDirection.y = m_animator.transform.position.y;
            m_animator.transform.rotation = Quaternion.LookRotation(targetDirection - m_animator.transform.position, Vector3.up);
        }
    }

    /// <summary>
    /// Update the stamina value by the given parameter
    /// </summary>
    /// <param name="_value"></param>
    private void StaminaUpdate(float _value)
    {
        m_currentStamina += _value;
        m_currentStamina = Mathf.Clamp(m_currentStamina, 0, m_maxStamina);
    }

    /// <summary>
    /// Update the move target position to the given position
    /// </summary>
    /// <param name="_pos"></param>
    private void MoveTargetUpdate(Vector3 _pos)
    {
        m_moveTarget.SetParent(null);
        m_moveTarget.position = _pos;
    }

    /// <summary>
    /// update the move target position to the given transform
    /// </summary>
    /// <param name="_target"></param>
    private void MoveTargetUpdate(Transform _target)
    {
        m_moveTarget.SetParent(_target);

        if (_target == null)
            m_moveTarget.transform.position = this.transform.position;

        m_moveTarget.localPosition = Vector3.zero;
    }

    /// <summary>
    /// check if the value is in the given range
    /// </summary>
    /// <param name="_value"></param>
    /// <param name="_min"></param>
    /// <param name="_max"></param>
    /// <returns></returns>
    bool ValueInRange(float _value, float _min, float _max)
    {
        return _value >= _min && _value < _max;
    }

    /// <summary>
    /// simple state machine for enemy actor, decide the action based on the distance to the target
    /// </summary>
    void DetectionUpdate()
    {
        List<Collider> _detectedTargets = Physics.OverlapSphere(transform.position, m_searchRange, m_targetLayerMask).ToList();

        if (_detectedTargets.Count == 0)
            return;

        m_detectedTarget = _detectedTargets.First().transform;
        float _distance = Vector3.Distance(this.transform.position, m_detectedTarget.position);

        if (ValueInRange(_distance, 0, m_attackRange))
        {
            m_state = EnemyState.Attack;
        }
        else if (ValueInRange(_distance, m_attackRange, m_engageRange))
        {
            m_state = EnemyState.Engage;
        }
        else if (ValueInRange(_distance, m_engageRange, m_searchRange))
        {
            m_state = EnemyState.Search;
        }
        else
        {
            m_state = EnemyState.Idle;
        }



    }

    /// <summary>
    /// simple state machine for enemy actor, decide the action based on the state
    /// </summary>
    private void ActorTick()
    {
        DetectionUpdate();

        switch (m_state)
        {
            case EnemyState.Idle:
                Action_Idle();
                break;
            case EnemyState.Engage:
                Action_EngageTarget();
                break;
            case EnemyState.Attack:
                Action_AttackTarget();
                break;
            case EnemyState.Search:
                Action_SearchTarget();
                break;
        }

    }

    /// <summary>
    /// enemy idle action, used for stamina recovery
    /// </summary>
    private void Action_Idle()
    {
        StaminaUpdate(0.2f);
    }

    /// <summary>
    /// enemy search target action, used for stamina recovery & moving to the target
    /// </summary>
    private void Action_SearchTarget()
    {
        StaminaUpdate(0.15f);

        MoveTargetUpdate(m_detectedTarget);

    }

    Vector3 _safePoint;
    /// <summary>
    /// enemy engage target action, used for stamina recovery & keep distance from the target
    /// </summary>
    private void Action_EngageTarget()
    {
        StaminaUpdate(0.075f);

        // if the stamina is enough, move to the target for attack
        if (m_currentStamina >= m_attackStaminaCost)
        {
            MoveTargetUpdate(m_detectedTarget);
            return;
        }

        // if the stamina is not enough, find a safe point to keep distance from the target
        float _dist = Vector3.Distance(this.transform.position, m_detectedTarget.position);
        if (_dist <= 0.5f || _dist > 5)
        {
            _safePoint = this.transform.position + Vector3.LerpUnclamped(-this.transform.right, this.transform.right, Random.value) * 5;
        }

        // move to the safe point
        MoveTargetUpdate(_safePoint);
    }

    /// <summary>
    /// enemy attack target action, used for attack the target
    /// </summary>
    private void Action_AttackTarget()
    {
        // if the stamina is not enough, return to engage action
        if (m_currentStamina < m_attackStaminaCost)
        {
            m_attackCommand = 0;
            Action_EngageTarget();
            return;
        }

        // comsume stamina for attack
        StaminaUpdate(-m_attackStaminaCost);

        // set the attack command, for combo attack purpose
        m_attackCommand += 1;

        // clamp the attack command based on the current attack action, make sure the attack command only trigger the designed action
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
        m_attackCommand = Mathf.Clamp(m_attackCommand, 0, 5);
    }


    /// <summary>
    /// check if the enemy is hit by the player weapon
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerEnter(Collider other)
    {
        // if the enemy is dead, stop doing anything
        if (m_isDead)
            return;

        // if the enemy is not hit by the player weapon, return
        if (other.gameObject.tag != "PlayerWeapon")
            return;
     
        // decrease the health value by 10
        m_health -= 10.0f;
        // update the health bar ui
        m_healthBar.fillAmount = m_health / 100.0f;

        // trigger a camera shake event for hit effect improvement
        StartCoroutine(PlayerCameraController.instance.CameraShake(0.15f, 0.05f));
        // trigger a time pause event for hit effect improvement
        StartCoroutine(PlayerCameraController.instance.TimePause(0.1f, 0.1f));

        // check if the enemy is dead
        Kill();
    }

    /// <summary>
    /// process the kill event for the enemy
    /// </summary>
    private void Kill()
    {
        // if the enemy HP is not comfirmed dead, return
        if (m_health > 0) return;

        // set the enemy dead flag to true
        m_isDead = true;

        // disable the core components for the enemy
        m_animator.enabled = false;
        m_healthBar.transform.parent.gameObject.SetActive(false);

        // enable the ragdoll effect for the enemy
        EnableRagdoll();

        // trigger the player win event
        GameUIManager.instance.e_playerWinEvent?.Invoke();
    }

    /// <summary>
    /// enable the ragdoll effect
    /// </summary>
    private void EnableRagdoll()
    {
        Rigidbody[] _rigidbodies = GetComponentsInChildren<Rigidbody>();
        Collider[] _colliders = GetComponentsInChildren<Collider>();

        foreach (Rigidbody _rb in _rigidbodies)
        {
            _rb.isKinematic = false;
            _rb.useGravity = true;
        }

        foreach (Collider _col in _colliders)
        {
            _col.enabled = true;
        }

        m_navMeshAgent.enabled = false;

    }
}
