using UnityEngine;
using UnityEngine.InputSystem;

namespace Core.BoardV2
{
    public class Draggable3DV2 : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;

        private PlaceableObject placeable;
        private bool dragging;
        private Vector3 dragOffset;
        private Plane dragPlane;
        private BoardGridV2 hoverBoard;
        private Vector2Int lastSnappedOrigin;
        private bool hasLastSnappedOrigin;
        private BoardGridV2 prevPreviewBoard;

        private BoardGridV2 originalBoard;
        private Vector2Int originalOrigin;

        private InputAction pressAction;
        private InputAction positionAction;
        private InputAction rotateAction;

        private void Awake() => Init();

        private void Init()
        {
            placeable = GetComponent<PlaceableObject>();
            if (targetCamera == null) targetCamera = Camera.main;

            pressAction = new InputAction(type: InputActionType.Button);
            pressAction.AddBinding("<Pointer>/press");
            pressAction.AddBinding("<Touchscreen>/primaryTouch/press");

            positionAction = new InputAction(type: InputActionType.Value);
            positionAction.AddBinding("<Pointer>/position");
            positionAction.AddBinding("<Touchscreen>/primaryTouch/position");

            pressAction.started += OnPressStarted;
            pressAction.canceled += OnPressCanceled;
            positionAction.performed += OnPositionPerformed;

            rotateAction = new InputAction(type: InputActionType.Button);
            rotateAction.AddBinding("<Keyboard>/r");
            rotateAction.started += OnRotateStarted;
        }

        private void OnEnable()
        {
            pressAction.Enable();
            positionAction.Enable();
            rotateAction.Enable();
        }

        private void OnDisable()
        {
            positionAction.performed -= OnPositionPerformed;
            pressAction.started -= OnPressStarted;
            pressAction.canceled -= OnPressCanceled;
            pressAction.Disable();
            positionAction.Disable();
            rotateAction.started -= OnRotateStarted;
            rotateAction.Disable();
        }

        private void OnPressStarted(InputAction.CallbackContext ctx)
        {
            var screenPos = positionAction.ReadValue<Vector2>();
            if (!RaycastSelf(screenPos)) return;

            if (placeable == null) return;
            if (placeable.CurrentBoard is ShopBoardV2 shop && !shop.CanPickUp(placeable)) return;

            dragging = true;
            originalBoard = placeable.CurrentBoard;
            originalOrigin = placeable.OriginCell;
            hasLastSnappedOrigin = false;
            prevPreviewBoard = null;

            var ray = targetCamera.ScreenPointToRay(screenPos);
            float enter;
            var planeY = (originalBoard != null) ? originalBoard.PlaneY : transform.position.y;
            dragPlane = new Plane(Vector3.up, new Vector3(0, planeY, 0));
            if (dragPlane.Raycast(ray, out enter))
            {
                var hit = ray.GetPoint(enter);
                dragOffset = transform.position - hit;
            }
            else
            {
                dragOffset = Vector3.zero;
            }

            if (originalBoard != null) originalBoard.Remove(placeable);
        }

        private void OnPositionPerformed(InputAction.CallbackContext ctx)
        {
            if (!dragging) return;
            var screenPos = ctx.ReadValue<Vector2>();
            var ray = targetCamera.ScreenPointToRay(screenPos);
            float enter;
            if (!dragPlane.Raycast(ray, out enter)) return;
            var hit = ray.GetPoint(enter);
            var targetWorld = hit + dragOffset;

            hoverBoard = BoardRegistryV2.GetBoardAtPosition(targetWorld);

            if (hoverBoard != null)
            {
                if (prevPreviewBoard != hoverBoard && prevPreviewBoard != null)
                {
                    prevPreviewBoard.ClearHighlights();
                }
                prevPreviewBoard = hoverBoard;

                // Сначала вычисляем позицию и валидность области
                var origin = hoverBoard.SnapWorldToOrigin(targetWorld, placeable.SizeX, placeable.SizeZ);
                bool can = hoverBoard.IsAreaFree(origin, placeable.SizeX, placeable.SizeZ);

                // Очищаем предыдущую подсветку на текущем борде и рисуем новую
                hoverBoard.ClearHighlights();
                ApplyPreview(hoverBoard, origin, can);

                if (can)
                {
                    lastSnappedOrigin = origin;
                    hasLastSnappedOrigin = true;
                    var pos = hoverBoard.GetAreaCenterWorld(origin, placeable.SizeX, placeable.SizeZ);
                    transform.position = pos;
                }
                else
                {
                    // Не магнитим к занятой области — оставляем свободное перемещение
                    transform.position = targetWorld;
                }
            }
            else
            {
                ClearAllPreviews();
                transform.position = targetWorld;
            }
        }

        private void OnPressCanceled(InputAction.CallbackContext ctx)
        {
            if (!dragging) return;
            dragging = false;

            ClearAllPreviews();

            if (hoverBoard != null)
            {
                var origin = hasLastSnappedOrigin ? lastSnappedOrigin : hoverBoard.SnapWorldToOrigin(transform.position, placeable.SizeX, placeable.SizeZ);
                if (hoverBoard.IsAreaFree(origin, placeable.SizeX, placeable.SizeZ))
                {
                    if (hoverBoard.TryPlace(placeable, origin))
                    {
                        return;
                    }
                }
            }

            if (hoverBoard == null)
            {
                BoardGridV2 preferred = null;
                BoardGridV2 secondary = null;

                if (originalBoard != null)
                {
                    if (originalBoard.BoardType == BoardTypeV2.Battle)
                    {
                        // Из битвы в пустоту -> пробуем инвентарь (НЕ в треш)
                        preferred = BoardRegistryV2.Get(BoardTypeV2.Inventory);
                        secondary = null;
                    }
                    else if (originalBoard.BoardType == BoardTypeV2.Inventory)
                    {
                        // Только инвентарь -> треш
                        preferred = BoardRegistryV2.GetTrashBoard();
                        secondary = null;
                    }
                    else
                    {
                        // Из других столов в пустоту — НЕ в треш
                        preferred = null;
                        secondary = null;
                    }
                }
                else
                {
                    // Объект не был на столе — не отправляем в треш
                    preferred = null;
                }

                if (preferred != null)
                {
                    var free = preferred.FindFirstFreePosition(placeable.SizeX, placeable.SizeZ);
                    if (free.x >= 0 && preferred.TryPlace(placeable, free)) return;
                }

                // secondary отключен согласно правилу: в треш только из инвентаря
            }

            if (originalBoard != null && originalOrigin.x >= 0)
            {
                if (originalBoard.TryPlace(placeable, originalOrigin)) return;
            }
        }

        private void OnRotateStarted(InputAction.CallbackContext ctx)
        {
            if (!dragging) return;
            placeable?.Rotate90();
            // После смены размеров пересчитать снап
            if (hoverBoard != null)
            {
                var origin = hoverBoard.SnapWorldToOrigin(transform.position, placeable.SizeX, placeable.SizeZ);
                bool can = hoverBoard.IsAreaFree(origin, placeable.SizeX, placeable.SizeZ);
                hoverBoard.ClearHighlights();
                ApplyPreview(hoverBoard, origin, can);
                if (can)
                {
                    lastSnappedOrigin = origin;
                    hasLastSnappedOrigin = true;
                    var pos = hoverBoard.GetAreaCenterWorld(origin, placeable.SizeX, placeable.SizeZ);
                    transform.position = pos;
                }
            }
        }

        private void ApplyPreview(BoardGridV2 board, Vector2Int origin, bool can)
        {
            for (int x = origin.x; x < origin.x + placeable.SizeX; x++)
            {
                for (int z = origin.y; z < origin.y + placeable.SizeZ; z++)
                {
                    var c = new Vector2Int(x, z);
                    var state = board.GetCellState(c);
                    if ((state & CellState.Allowed) == 0) continue;
                    if (board == null) continue;
                    if (can) board.SetCellState(c, (state | CellState.HighlightValid) & ~CellState.HighlightInvalid);
                    else board.SetCellState(c, (state | CellState.HighlightInvalid) & ~CellState.HighlightValid);
                }
            }
        }

        private void ClearAllPreviews()
        {
            if (hoverBoard == null) return;
            hoverBoard.ClearHighlights();
        }

        private bool RaycastSelf(Vector2 screenPos)
        {
            var ray = targetCamera.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out var hit, 200f))
            {
                return hit.collider != null && hit.collider.transform.IsChildOf(transform);
            }
            return false;
        }
    }
}


