using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


/// <summary>
/// This script is used to receive messages from the animator 
/// </summary>
public class AnimatorMessager : MonoBehaviour
{

    public int m_currentAttackAction = 0;

    public UnityAction e_onAttack;
    public UnityAction e_onAttackEnd;
    public UnityAction e_onMovement;
    public UnityAction e_onJump;
    public UnityAction e_onJumpEnd;

    /// <summary>
    /// This function is called from the animator to indicate the attack action is started, and the action clip being played
    /// </summary>
    /// <param name="_value"></param>
    public void PlayingAttackAction(int _value)
    {
        m_currentAttackAction = _value;
        e_onAttack?.Invoke();
    }

    /// <summary>
    /// This function is called from the animator to indicate the attack action is ended
    /// </summary>
    public void OnAttackEnd()
    {
        e_onAttackEnd?.Invoke();
    }

    /// <summary>
    /// this function is called from the animator to indicate the movement action is started
    /// </summary>
    public void PlayingMovementAction()
    {
        m_currentAttackAction = 0;
        e_onMovement?.Invoke();
    }

    /// <summary>
    /// this function is called from the animator to indicate the jump action is started
    /// </summary>
    public void PlayingJumpAction()
    {
        m_currentAttackAction = 0;
        e_onJump?.Invoke();
    }

    /// <summary>
    /// this function is called from the animator to indicate the character is landed
    /// </summary>
    public void OnJumpEnd()
    {
        e_onJumpEnd?.Invoke();
    }
}
