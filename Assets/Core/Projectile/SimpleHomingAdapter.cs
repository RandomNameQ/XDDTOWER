// using System.Collections.Generic;
// using UnityEngine;
// using static Enums;

// // Облегчённый компонент, чтобы не требовать конкретного типа на префабе
// public class SimpleHomingAdapter : MonoBehaviour
// {
//     public float speed = 8f;
//     private Creature _target;
//     private List<EffectSO> _effects = new();
//     private bool _applied;

//     public void Initialize(Creature target, IEnumerable<EffectSO> effects)
//     {
//         _target = target;
//         _effects = new List<EffectSO>(effects);
//     }

//     private void Update()
//     {
//         if (_target == null) { Destroy(gameObject); return; }

//         var to = _target.transform.position - transform.position;
//         var dist = to.magnitude;
//         if (dist < 0.1f)
//         {
//             if (!_applied)
//             {
//                 _applied = true;
//                 foreach (var e in _effects) e?.Apply(_target);
//             }
//             Destroy(gameObject);
//             return;
//         }

//         var dir = to.normalized;
//         transform.position += dir * speed * Time.deltaTime;
//         if (dir != Vector3.zero) transform.forward = dir;
//     }
// }


