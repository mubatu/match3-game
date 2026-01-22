using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Core
{
    /// <summary>
    /// Manages the game grid, including item spawning, positioning, and grid operations.
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        [SerializeField] private int width;
        [SerializeField] private int height;
        [SerializeField] private GameObject[] objects;
        [SerializeField] private float initialMatchCheckDelay = 0.5f;

        private BoardItem[,] _gridObjects;
        private MatchDetector _matchDetector;
        
        public int Width => width;
        public int Height => height;
        
        private void Start()
        {
            _matchDetector = new MatchDetector(this);
            GenerateGrid();
            StartCoroutine(CheckInitialMatches());
        }
        
        /// <summary>
        /// Checks for existing matches after grid generation and blasts them.
        /// </summary>
        private IEnumerator CheckInitialMatches()
        {
            // Small delay to let everything initialize
            yield return new WaitForSeconds(initialMatchCheckDelay);
            
            CheckAndBlastMatches();
        }
        
        /// <summary>
        /// Finds all matches on the board and triggers blast if any found.
        /// </summary>
        public void CheckAndBlastMatches()
        {
            List<MatchData> matches = _matchDetector.FindAllMatches();
            
            foreach (var match in matches)
            {
                GameEvents.MatchFound(match);
            }
        }

        /// <summary>
        /// Converts grid coordinates to world position (centered on grid).
        /// </summary>
        public Vector3 GetWorldPosition(int x, int y)
        {
            float xOffset = width / 2f;
            float yOffset = height / 2f;
            
            float xPosition = x - xOffset + 0.5f;
            float yPosition = y - yOffset + 0.5f;
            
            return new Vector3(xPosition, yPosition, 0);
        }
        
        /// <summary>
        /// Checks if the given coordinates are within grid bounds.
        /// </summary>
        public bool IsValidPosition(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }
        
        /// <summary>
        /// Returns the item at the specified grid position, or null if invalid.
        /// </summary>
        public BoardItem GetItemAt(int x, int y)
        {
            if (!IsValidPosition(x, y)) return null;
            return _gridObjects[x, y];
        }
        
        /// <summary>
        /// Checks if two items are adjacent (horizontally or vertically, not diagonal).
        /// </summary>
        public bool AreAdjacent(BoardItem itemA, BoardItem itemB)
        {
            if (itemA == null || itemB == null) return false;
            
            int deltaX = Mathf.Abs(itemA.X - itemB.X);
            int deltaY = Mathf.Abs(itemA.Y - itemB.Y);
            
            // Adjacent means exactly one step in either X or Y, but not both
            return (deltaX == 1 && deltaY == 0) || (deltaX == 0 && deltaY == 1);
        }
        
        /// <summary>
        /// Swaps two items in the grid array and updates their grid positions.
        /// Does not handle animation - call MoveTo on items separately.
        /// </summary>
        public void SwapItemsInGrid(BoardItem itemA, BoardItem itemB)
        {
            if (itemA == null || itemB == null) return;
            
            int aX = itemA.X;
            int aY = itemA.Y;
            int bX = itemB.X;
            int bY = itemB.Y;
            
            // Swap in the grid array
            _gridObjects[aX, aY] = itemB;
            _gridObjects[bX, bY] = itemA;
            
            // Update the items' internal coordinates
            itemA.SetGridPosition(bX, bY);
            itemB.SetGridPosition(aX, aY);
        }
        
        /// <summary>
        /// Removes an item from the grid array (sets position to null).
        /// Does not destroy the GameObject - call this before or after destruction.
        /// </summary>
        public void RemoveItem(BoardItem item)
        {
            if (item == null) return;
            
            int x = item.X;
            int y = item.Y;
            
            if (IsValidPosition(x, y) && _gridObjects[x, y] == item)
            {
                _gridObjects[x, y] = null;
            }
        }
        
        /// <summary>
        /// Sets the item at the specified grid position.
        /// Used by gravity system to update item positions.
        /// </summary>
        public void SetItemAt(int x, int y, BoardItem item)
        {
            if (!IsValidPosition(x, y)) return;
            _gridObjects[x, y] = item;
        }
        
        /// <summary>
        /// Clears the item at the specified grid position (sets to null).
        /// Used by gravity system when moving items.
        /// </summary>
        public void ClearItemAt(int x, int y)
        {
            if (!IsValidPosition(x, y)) return;
            _gridObjects[x, y] = null;
        }
        
        /// <summary>
        /// Counts empty cells in a column from a starting Y position upward.
        /// Used by refill system to determine how many items to spawn.
        /// </summary>
        public int CountEmptyCellsInColumn(int x)
        {
            if (x < 0 || x >= width) return 0;
            
            int emptyCount = 0;
            for (int y = 0; y < height; y++)
            {
                if (_gridObjects[x, y] == null)
                {
                    emptyCount++;
                }
            }
            return emptyCount;
        }
        
        /// <summary>
        /// Returns the array of item prefabs used for spawning.
        /// </summary>
        public GameObject[] GetItemPrefabs()
        {
            return objects;
        }
        
        private void GenerateGrid()
        {
            _gridObjects = new BoardItem[width, height];
        
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int randomIndex = Random.Range(0, objects.Length);
                    GameObject prefabToSpawn = objects[randomIndex];
                
                    Vector3 position = GetWorldPosition(x, y);
                
                    GameObject newObject = Instantiate(prefabToSpawn, position, Quaternion.identity);
                    newObject.transform.parent = transform;
                    newObject.name = $"Cube ({x}, {y})";

                    if (newObject.gameObject.TryGetComponent(out SpriteRenderer sr))
                    {
                        sr.sortingOrder = y;
                    }
                
                    BoardItem item = newObject.GetComponent<BoardItem>();
                    item.Initialize(x, y);
                    
                    _gridObjects[x, y] = item;
                }
            }
        }

        public void HandleItemClick(int x, int y)
        {
            BoardItem clickedItem = _gridObjects[x, y];
            clickedItem.CallStrategy(this);
        }
        
        private void OnEnable()
        {
            GameEvents.OnItemClicked += HandleItemClick;
        }
        
        private void OnDisable()
        {
            GameEvents.OnItemClicked -= HandleItemClick;
        }
    }
}