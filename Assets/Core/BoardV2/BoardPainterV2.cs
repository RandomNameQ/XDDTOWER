using UnityEngine;
using UnityEngine.InputSystem;

namespace Core.BoardV2
{
    public class BoardPainterV2 : MonoBehaviour
    {
        [SerializeField] private BoardGridV2 board;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private bool enabledPainting;

        private InputAction leftButton;
        private InputAction rightButton;
        private InputAction positionAction;

        private void Awake() => Init();

        private void Init()
        {
            if (board == null) board = GetComponent<BoardGridV2>();
            if (targetCamera == null) targetCamera = Camera.main;

            leftButton = new InputAction(type: InputActionType.Button, binding: "<Mouse>/leftButton");
            rightButton = new InputAction(type: InputActionType.Button, binding: "<Mouse>/rightButton");
            positionAction = new InputAction(type: InputActionType.Value);
            positionAction.AddBinding("<Pointer>/position");
        }

        private void OnEnable()
        {
            leftButton.Enable();
            rightButton.Enable();
            positionAction.Enable();
        }

        private void OnDisable()
        {
            leftButton.Disable();
            rightButton.Disable();
            positionAction.Disable();
        }

        private void Update()
        {
            if (!enabledPainting || board == null) return;
            if (leftButton.WasPerformedThisFrame() || rightButton.WasPerformedThisFrame())
            {
                var screenPos = positionAction.ReadValue<Vector2>();
                var world = ScreenToPlane(screenPos, board.PlaneY);
                var cell = board.WorldToCell(world);
                bool add = leftButton.WasPerformedThisFrame();
                board.PaintCell(cell, add);
            }
        }

        private Vector3 ScreenToPlane(Vector2 screen, float y)
        {
            var ray = targetCamera.ScreenPointToRay(screen);
            var plane = new Plane(Vector3.up, new Vector3(0, y, 0));
            if (plane.Raycast(ray, out var enter)) return ray.GetPoint(enter);
            return Vector3.zero;
        }
    }
}


