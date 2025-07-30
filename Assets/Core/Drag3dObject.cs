using UnityEngine;
using UnityEngine.EventSystems;     // для интерфейсов ввода

public class Drag3dObject : MonoBehaviour,
                            IPointerDownHandler,
                            IPointerUpHandler,
                            IDragHandler
{
    #region FIELDS
    private Camera  _mainCamera;
    private Vector3 _offsetToMouse;

    private Plane   _dragPlane;

    private GridPlacement _placement; // логика размещения
    private BoardGrid _currentHoverBoard; // текущая доска под курсором
    #endregion

    #region UNITY LIFECYCLE
    private void Awake() => Init();
    #endregion

    #region INIT
    private void Init()
    {
        _mainCamera = Camera.main;

        _placement = GetComponent<GridPlacement>();
        if (_placement == null)
        {
            Debug.LogError($"{nameof(Drag3dObject)}: {nameof(GridPlacement)} component not found on the same GameObject!");
            enabled = false;
            return;
        }

        // Плоскость доски (горизонтальная, на высоте доски)
        _dragPlane = new Plane(Vector3.up, _placement.BoardPlaneY);
        
        // Изначально текущая доска под курсором - это доска объекта
        _currentHoverBoard = _placement.CurrentBoard;
    }
    #endregion

    #region EVENT SYSTEM CALLBACKS
    public void OnPointerDown(PointerEventData eventData)
    {
        _placement.StartDrag();

        Ray ray = _mainCamera.ScreenPointToRay(eventData.position);
        if (_dragPlane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            _offsetToMouse = transform.position - hitPoint;
        }
        else
        {
            _offsetToMouse = Vector3.zero;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        Ray ray = _mainCamera.ScreenPointToRay(eventData.position);
        if (_dragPlane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            
            // Определяем, над какой доской находится курсор
            BoardGrid boardUnderCursor = BoardManager.GetBoardAtPosition(hitPoint);
            _currentHoverBoard = boardUnderCursor ?? _placement.CurrentBoard;
            
            Vector3 targetPos = hitPoint + _offsetToMouse;

            // Привязываем к гриду через GridPlacement, используя доску под курсором
            Vector3 snappedPos;
            if (_currentHoverBoard != _placement.CurrentBoard)
            {
                // Если курсор над другой доской, используем её для привязки
                snappedPos = _placement.GetSnappedPositionOnBoard(targetPos, _currentHoverBoard);
            }
            else
            {
                // Если курсор над текущей доской, используем стандартный метод
                snappedPos = _placement.GetSnappedPosition(targetPos);
            }
            
            transform.position = snappedPos;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Ray ray = _mainCamera.ScreenPointToRay(eventData.position);
        if (_dragPlane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            _placement.EndDrag(hitPoint);
        }
        else
        {
            _placement.EndDrag(transform.position);
        }
    }
    #endregion


}