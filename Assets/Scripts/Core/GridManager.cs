using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Core
{
    public class GridManager : MonoBehaviour
    {
        [SerializeField] private int width;
        [SerializeField] private int height;
        [SerializeField] private GameObject[] objects;

        private BoardItem[,] _gridObjects;
        
        public int Width => width;
        public int Height => height;
        
        private void Start()
        {
            GenerateGrid();
        }

        private Vector3 GetWorldPosition(int x, int y)
        {
            float xOffset = width / 2f;
            float yOffset = height / 2f;
            
            float xPosition = x - xOffset + 0.5f;
            float yPosition = y - yOffset + 0.5f;
            
            Vector3 position = new Vector3(xPosition, yPosition, 0);
            return position;
        }
        
        private void GenerateGrid()
        {
            // Initialize 2D array
            _gridObjects = new BoardItem[width, height];
        
            // Loop through every column and row
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // Select a random prefab
                    int randomIndex = Random.Range(0, objects.Length);
                    GameObject prefabToSpawn = objects[randomIndex];
                
                    // Determine its position
                    Vector3 position = GetWorldPosition(x, y);
                
                    // Spawn the object
                    GameObject newObject = Instantiate(prefabToSpawn, position, Quaternion.identity);
                
                    // Set the parent to this object
                    newObject.transform.parent = transform;
                
                    // Name the object for easy debugging
                    newObject.name = $"Cube ({x}, {y})";

                    // Adjust the sorting
                    if (newObject.gameObject.TryGetComponent (out SpriteRenderer sr)) {
                        sr.sortingOrder = y;
                    }
                
                    // Get the BoardItem component from newObject and initialize it
                    BoardItem item = newObject.GetComponent<BoardItem>();
                    item.Initialize(x, y);
                    
                    // Fill the array
                    _gridObjects[x, y] = item;
                }
            }
        }

        public void HandleItemClick(int x, int y)
        {
            // Get the item at (x,y)
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