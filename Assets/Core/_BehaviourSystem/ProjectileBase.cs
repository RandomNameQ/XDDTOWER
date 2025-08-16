using System.Diagnostics;
using UnityEngine;

/// <summary>
/// Базовый снаряд: поворот на цель → полёт → коллбек при прибытии.
/// </summary>
public class ProjectileBase : MonoBehaviour
{
    public GameObject target;
    public float speed;

    private bool _isFacingTarget;

    /// <summary>
    /// Инициализация цели и коллбека применения эффекта при попадании.
    /// </summary>
    private Creature sources;
    private Creature destiny;

    public void Init(GameObject targetGo, Creature sources, Creature destiny)
    {
        this.sources = sources;
        this.destiny = destiny;
        target = targetGo;
        
        SetInitialPosition();
        RotateToTargetImmediately();
        StartMovement();
    }

    private void SetInitialPosition()
    {
        Vector3 currentPos = transform.position;
        transform.position = new Vector3(currentPos.x, currentPos.y + 1f, currentPos.z);
    }

    private void RotateToTargetImmediately()
    {
        if (target == null) return;
        Vector3 direction = (target.transform.position - transform.position).normalized;
        if (direction == Vector3.zero) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = targetRotation;
        _isFacingTarget = true;
    }

    private void StartMovement()
    {
        _isFacingTarget = true;
    }

    private void Update()
    {
        if (!_isFacingTarget)
        {
            RotateToTarget();
            return;
        }

        MoveToTarget();
        TryArrive();
    }

    private void RotateToTarget()
    {
        if (target == null) return;
        Vector3 direction = (target.transform.position - transform.position).normalized;
        if (direction == Vector3.zero) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, speed * Time.deltaTime);

        if (Quaternion.Angle(transform.rotation, targetRotation) < 1f)
        {
            _isFacingTarget = true;
        }
    }

    private void MoveToTarget()
    {
        if (target == null) return;
        Vector3 direction = (target.transform.position - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;
    }

    private void TryArrive()
    {
        if (target == null) return;
        float distance = Vector3.Distance(transform.position, target.transform.position);
        if (distance <= 0.1f) OnArrive();
    }

    private void OnArrive()
    {
        destiny.GetComponent<Creature>().decideBehavior.ApplyEffect(sources);
        Destroy(gameObject);
    }
}