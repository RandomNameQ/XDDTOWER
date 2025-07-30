using UnityEngine;

public class ProjectileBase : MonoBehaviour
{
    // общий алгоирмт - где-то спавним прожектаел. 
    // делаем какой-то эффект появиления. поворчиваем ротацию, увелчиваем масштаб, етк
    // отправялем к цели
    public GameObject target;
    public float speed;
    private bool _isFacingTarget;

    private void Update()
    {
        if (!_isFacingTarget)
        {
            RotateToTarget();
        }
        else
        {
            MoveToTarget();
            CheckDistanceToTarget();
        }
    }

    private void RotateToTarget()
    {
        if (target == null) return;

        Vector3 direction = (target.transform.position - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, speed * Time.deltaTime);

            // Проверяем, завершен ли поворот
            if (Quaternion.Angle(transform.rotation, targetRotation) < 1f)
            {
                _isFacingTarget = true;
            }
        }
    }

    private void MoveToTarget()
    {
        if (target == null) return;

        Vector3 direction = (target.transform.position - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;
    }

    private void CheckDistanceToTarget()
    {
        if (target == null) return;

        float distance = Vector3.Distance(transform.position, target.transform.position);
        if (distance <= 0.1f)
        {
            DestroyProjectile();
        }
    }

    private void DestroyProjectile()
    {
        Destroy(gameObject);
    }
}
