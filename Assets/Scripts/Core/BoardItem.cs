using System.Collections;
using Enums;
using UnityEngine;
using Strategies;

namespace Core
{
    /// <summary>
    /// Abstract base class for all items that can exist on the game grid.
    /// Provides grid positioning, movement animation, and strategy execution.
    /// </summary>
    public abstract class BoardItem : MonoBehaviour
    {
        [SerializeField] public ItemType Type;
        
        public int X { get; private set; }
        public int Y { get; private set; }
        
        /// <summary>
        /// Indicates whether this item is currently animating/moving.
        /// </summary>
        public bool IsMoving { get; private set; }
        
        protected IProcessStrategy _processStrategy;
        
        public void Initialize(int x, int y)
        {
            X = x;
            Y = y;
            SetupStrategy();
        }
        
        /// <summary>
        /// Updates the grid coordinates of this item.
        /// Call this after swapping positions in the grid array.
        /// </summary>
        public void SetGridPosition(int x, int y)
        {
            X = x;
            Y = y;
            gameObject.name = $"Cube ({x}, {y})";
            UpdateSortingOrder();
        }
        
        /// <summary>
        /// Updates the sprite sorting order based on Y position.
        /// Higher Y values render on top of lower ones.
        /// </summary>
        private void UpdateSortingOrder()
        {
            if (TryGetComponent(out SpriteRenderer sr))
            {
                sr.sortingOrder = Y;
            }
        }
        
        /// <summary>
        /// Animates the item to a target world position over the specified duration.
        /// </summary>
        /// <param name="targetPosition">The world position to move to.</param>
        /// <param name="duration">Time in seconds for the movement.</param>
        public void MoveTo(Vector3 targetPosition, float duration)
        {
            if (IsMoving) return;
            StartCoroutine(MoveCoroutine(targetPosition, duration));
        }
        
        private IEnumerator MoveCoroutine(Vector3 targetPosition, float duration)
        {
            IsMoving = true;
            
            Vector3 startPosition = transform.position;
            float elapsedTime = 0f;
            
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / duration;
                
                // Ease out cubic for smooth deceleration
                float easedT = 1f - Mathf.Pow(1f - t, 3f);
                
                transform.position = Vector3.Lerp(startPosition, targetPosition, easedT);
                yield return null;
            }
            
            transform.position = targetPosition;
            IsMoving = false;
        }
        
        /// <summary>
        /// Plays the blast/destruction animation and destroys the GameObject.
        /// </summary>
        /// <param name="duration">Time in seconds for the blast animation.</param>
        /// <param name="onComplete">Callback invoked after destruction.</param>
        public void Blast(float duration, System.Action onComplete = null)
        {
            StartCoroutine(BlastCoroutine(duration, onComplete));
        }
        
        private IEnumerator BlastCoroutine(float duration, System.Action onComplete)
        {
            Vector3 startScale = transform.localScale;
            Color startColor = Color.white;
            SpriteRenderer spriteRenderer = null;
            
            if (TryGetComponent(out spriteRenderer))
            {
                startColor = spriteRenderer.color;
            }
            
            float elapsedTime = 0f;
            
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / duration;
                
                // Ease in for acceleration effect
                float easedT = t * t;
                
                // Scale down
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, easedT);
                
                // Fade out
                if (spriteRenderer != null)
                {
                    Color newColor = startColor;
                    newColor.a = Mathf.Lerp(1f, 0f, easedT);
                    spriteRenderer.color = newColor;
                }
                
                yield return null;
            }
            
            onComplete?.Invoke();
            Destroy(gameObject);
        }
        
        protected abstract void SetupStrategy();
        
        public void CallStrategy(GridManager gridManager)
        {
            _processStrategy?.Execute(this, gridManager);
        }
    }
}