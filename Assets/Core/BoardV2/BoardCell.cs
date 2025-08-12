using UnityEngine;

namespace Core.BoardV2
{
    public class BoardCell : MonoBehaviour
    {
        [SerializeField] private Renderer targetRenderer;

        public Vector2Int Coordinate { get; private set; }
        public CellState State { get; private set; }

        private Color baseColor = new Color(0.2f, 0.8f, 0.2f, 1f);
        private Color lockedColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        private Color destroyedColor = new Color(0.5f, 0.2f, 0.2f, 1f);
        private Color invalidColor = new Color(0.9f, 0.2f, 0.2f, 1f);
        private Color validColor = new Color(0.2f, 0.9f, 0.2f, 1f);

        public void Init(Vector2Int coordinate, float cellSize)
        {
            Coordinate = coordinate;
            transform.localScale = new Vector3(cellSize, transform.localScale.y, cellSize);
            if (targetRenderer == null) targetRenderer = GetComponentInChildren<Renderer>();
            ApplyVisual();
        }

        public void SetState(CellState state)
        {
            State = state;
            ApplyVisual();
        }

        public void SetHighlightValid()
        {
            if (targetRenderer == null) return;
            targetRenderer.material.color = validColor;
        }

        public void SetHighlightInvalid()
        {
            if (targetRenderer == null) return;
            targetRenderer.material.color = invalidColor;
        }

        public void ClearHighlight()
        {
            ApplyVisual();
        }

        private void ApplyVisual()
        {
            if (targetRenderer == null) return;
            if ((State & CellState.HighlightInvalid) != 0) { targetRenderer.material.color = invalidColor; return; }
            if ((State & CellState.HighlightValid) != 0)   { targetRenderer.material.color = validColor; return; }
            if ((State & CellState.Destroyed) != 0)        { targetRenderer.material.color = destroyedColor; return; }
            if ((State & CellState.Locked) != 0)           { targetRenderer.material.color = lockedColor; return; }
            if ((State & CellState.Occupied) != 0)         { targetRenderer.material.color = invalidColor; return; }
            targetRenderer.material.color = baseColor;
        }
    }
}


