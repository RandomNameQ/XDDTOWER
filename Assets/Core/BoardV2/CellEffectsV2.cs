using System.Collections.Generic;
using UnityEngine;

namespace Core.BoardV2
{
    public class CellEffectsMapV2 : MonoBehaviour
    {
        [SerializeField] private BoardGridV2 board;

        private readonly Dictionary<Vector2Int, List<string>> coordToTags = new Dictionary<Vector2Int, List<string>>();

        private void Awake() => Init();

        private void Init()
        {
            if (board == null) board = GetComponent<BoardGridV2>();
            if (board != null) board.OnObjectPlaced += OnObjectPlaced;
        }

        private void OnDestroy()
        {
            if (board != null) board.OnObjectPlaced -= OnObjectPlaced;
        }

        public void AddEffectTag(Vector2Int coord, string tag)
        {
            if (!coordToTags.TryGetValue(coord, out var list))
            {
                list = new List<string>();
                coordToTags[coord] = list;
            }
            if (!list.Contains(tag)) list.Add(tag);
        }

        public void RemoveEffectTag(Vector2Int coord, string tag)
        {
            if (!coordToTags.TryGetValue(coord, out var list)) return;
            list.Remove(tag);
        }

        public IReadOnlyList<string> GetTags(Vector2Int coord)
        {
            if (!coordToTags.TryGetValue(coord, out var list)) return System.Array.Empty<string>();
            return list;
        }

        public void ClearAllTags()
        {
            coordToTags.Clear();
        }

        public List<(Vector2Int coord, List<string> tags)> GetAll()
        {
            var result = new List<(Vector2Int, List<string>)>();
            foreach (var kv in coordToTags)
            {
                result.Add((kv.Key, new List<string>(kv.Value)));
            }
            return result;
        }

        private void OnObjectPlaced(PlaceableObject obj, Vector2Int origin)
        {
            if (obj == null) return;
            for (int x = origin.x; x < origin.x + obj.SizeX; x++)
            {
                for (int z = origin.y; z < origin.y + obj.SizeZ; z++)
                {
                    var c = new Vector2Int(x, z);
                    var tags = GetTags(c);
                    if (tags.Count == 0) continue;
                    ApplyTags(obj, c, tags);
                }
            }
        }

        protected virtual void ApplyTags(PlaceableObject obj, Vector2Int cell, IReadOnlyList<string> tags)
        {
            // Переопределяй в наследниках и обрабатывай теги (яд, заморозка и т.п.)
        }
    }
}


