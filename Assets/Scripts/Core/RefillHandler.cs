using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Handles spawning new items to fill empty cells after gravity.
    /// Items spawn above the grid and animate down into position.
    /// </summary>
    public class RefillHandler : MonoBehaviour
    {
        [SerializeField] private GridManager gridManager;
        [SerializeField] private float spawnHeight = 2f;
        [SerializeField] private float fallDuration = 0.4f;
        [SerializeField] private float spawnDelayPerColumn = 0.05f;
        
        private int _pendingSpawns;
        
        private void OnEnable()
        {
            GameEvents.OnGravityCompleted += HandleGravityCompleted;
        }
        
        private void OnDisable()
        {
            GameEvents.OnGravityCompleted -= HandleGravityCompleted;
        }
        
        private void HandleGravityCompleted()
        {
            StartCoroutine(RefillBoard());
        }
        
        /// <summary>
        /// Fills all empty cells in the grid with new items.
        /// </summary>
        private IEnumerator RefillBoard()
        {
            List<SpawnData> spawnOperations = CalculateSpawnOperations();
            
            if (spawnOperations.Count == 0)
            {
                // No items need to be spawned
                GameEvents.RefillCompleted();
                yield break;
            }
            
            GameEvents.RefillStarted();
            
            _pendingSpawns = spawnOperations.Count;
            
            // Group spawns by column for staggered animation
            var spawnsByColumn = GroupSpawnsByColumn(spawnOperations);
            
            // Execute spawns with stagger delay per column
            foreach (var columnGroup in spawnsByColumn)
            {
                // Spawn items in this column from bottom to top
                foreach (var spawn in columnGroup.Value)
                {
                    ExecuteSpawn(spawn);
                }
                
                // Small delay before next column starts spawning
                if (spawnDelayPerColumn > 0)
                {
                    yield return new WaitForSeconds(spawnDelayPerColumn);
                }
            }
            
            // Wait for all spawns to complete
            while (_pendingSpawns > 0)
            {
                yield return null;
            }
            
            GameEvents.RefillCompleted();
        }
        
        /// <summary>
        /// Calculates all spawn operations needed for empty cells.
        /// </summary>
        private List<SpawnData> CalculateSpawnOperations()
        {
            List<SpawnData> spawnOperations = new List<SpawnData>();
            
            int width = gridManager.Width;
            int height = gridManager.Height;
            
            // Process each column
            for (int x = 0; x < width; x++)
            {
                int spawnIndex = 0;
                
                // Find empty cells from bottom to top
                for (int y = 0; y < height; y++)
                {
                    if (gridManager.GetItemAt(x, y) == null)
                    {
                        spawnOperations.Add(new SpawnData
                        {
                            TargetX = x,
                            TargetY = y,
                            SpawnIndex = spawnIndex
                        });
                        spawnIndex++;
                    }
                }
            }
            
            return spawnOperations;
        }
        
        /// <summary>
        /// Groups spawn operations by column for staggered animation.
        /// </summary>
        private SortedDictionary<int, List<SpawnData>> GroupSpawnsByColumn(List<SpawnData> spawnOperations)
        {
            var groups = new SortedDictionary<int, List<SpawnData>>();
            
            foreach (var spawn in spawnOperations)
            {
                if (!groups.ContainsKey(spawn.TargetX))
                {
                    groups[spawn.TargetX] = new List<SpawnData>();
                }
                groups[spawn.TargetX].Add(spawn);
            }
            
            // Sort each column's spawns by Y (bottom to top)
            foreach (var group in groups.Values)
            {
                group.Sort((a, b) => a.TargetY.CompareTo(b.TargetY));
            }
            
            return groups;
        }
        
        /// <summary>
        /// Executes a single spawn operation.
        /// </summary>
        private void ExecuteSpawn(SpawnData spawn)
        {
            GameObject[] prefabs = gridManager.GetColoredPrefabs();
            int randomIndex = Random.Range(0, prefabs.Length);
            GameObject prefabToSpawn = prefabs[randomIndex];
            
            // Calculate spawn position (above the grid)
            Vector3 targetPosition = gridManager.GetWorldPosition(spawn.TargetX, spawn.TargetY);
            float spawnYOffset = spawnHeight + (spawn.SpawnIndex * 1f);
            Vector3 spawnPosition = targetPosition + Vector3.up * spawnYOffset;
            
            // Instantiate the new item
            GameObject newObject = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
            newObject.transform.parent = gridManager.transform;
            newObject.name = $"Cube ({spawn.TargetX}, {spawn.TargetY})";
            
            // Setup sprite sorting
            if (newObject.TryGetComponent(out SpriteRenderer sr))
            {
                sr.sortingOrder = spawn.TargetY;
            }
            
            // Initialize the BoardItem
            BoardItem item = newObject.GetComponent<BoardItem>();
            item.Initialize(spawn.TargetX, spawn.TargetY);
            
            // Add to grid
            gridManager.SetItemAt(spawn.TargetX, spawn.TargetY, item);
            
            // Animate fall into position
            StartCoroutine(AnimateSpawn(item, targetPosition));
        }
        
        /// <summary>
        /// Animates a spawned item falling into its target position.
        /// </summary>
        private IEnumerator AnimateSpawn(BoardItem item, Vector3 targetPosition)
        {
            // Calculate duration based on distance for natural feel
            float distance = Vector3.Distance(item.transform.position, targetPosition);
            float adjustedDuration = fallDuration * (distance / spawnHeight);
            adjustedDuration = Mathf.Clamp(adjustedDuration, 0.2f, 0.6f);
            
            item.MoveTo(targetPosition, adjustedDuration);
            
            // Wait for movement to complete
            while (item != null && item.IsMoving)
            {
                yield return null;
            }
            
            _pendingSpawns--;
        }
        
        /// <summary>
        /// Data structure to hold information about a single spawn operation.
        /// </summary>
        private struct SpawnData
        {
            public int TargetX;
            public int TargetY;
            public int SpawnIndex;
        }
    }
}
