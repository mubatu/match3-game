using Enums;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Core
{
    /// <summary>
    /// Handles user input for drag-based swap gestures.
    /// Detects drag start, calculates direction, and identifies target item.
    /// </summary>
    public class InputHandler : MonoBehaviour
    {
        [SerializeField] private float dragThreshold = 0.3f;
        [SerializeField] private CascadeController cascadeController;
        
        private Camera _mainCamera;
        private BoardItem _dragStartItem;
        private Vector2 _dragStartWorldPosition;
        private bool _isDragging;

        private void Awake()
        {
            _mainCamera = Camera.main;
        }
        
        private void Update()
        {
            HandleDragInput();
        }
        
        private void HandleDragInput()
        {
            // Block input when game is not idle
            if (cascadeController != null && !cascadeController.CanAcceptInput())
            {
                if (_isDragging)
                {
                    ResetDragState();
                }
                return;
            }
            
            Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
            Vector2 mouseWorldPosition = _mainCamera.ScreenToWorldPoint(mouseScreenPosition);
            
            // Drag Start
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                TryStartDrag(mouseWorldPosition);
            }
            
            // Drag End
            if (Mouse.current.leftButton.wasReleasedThisFrame && _isDragging)
            {
                CompleteDrag(mouseWorldPosition);
            }
        }
        
        private void TryStartDrag(Vector2 worldPosition)
        {
            RaycastHit2D hit = Physics2D.Raycast(worldPosition, Vector2.zero);
            
            if (hit.collider == null) return;
            
            if (hit.collider.gameObject.TryGetComponent(out BoardItem item))
            {
                // Don't allow dragging items that are currently moving
                if (item.IsMoving) return;
                
                _dragStartItem = item;
                _dragStartWorldPosition = worldPosition;
                _isDragging = true;
                
                GameEvents.DragStarted(item);
            }
        }
        
        private void CompleteDrag(Vector2 endWorldPosition)
        {
            if (_dragStartItem == null)
            {
                ResetDragState();
                return;
            }
            
            Vector2 dragDelta = endWorldPosition - _dragStartWorldPosition;
            SwapDirection direction = CalculateSwapDirection(dragDelta);
            
            if (direction != SwapDirection.None)
            {
                BoardItem targetItem = GetTargetItem(_dragStartItem, direction);
                
                if (targetItem != null && !targetItem.IsMoving)
                {
                    GameEvents.SwapRequested(_dragStartItem, targetItem);
                }
            }
            
            ResetDragState();
        }
        
        private SwapDirection CalculateSwapDirection(Vector2 dragDelta)
        {
            if (dragDelta.magnitude < dragThreshold)
            {
                return SwapDirection.None;
            }
            
            // Calculate angle in degrees (-180 to 180)
            float angle = Mathf.Atan2(dragDelta.y, dragDelta.x) * Mathf.Rad2Deg;
            
            // Determine direction based on angle quadrants
            if (angle >= -45f && angle < 45f)
                return SwapDirection.Right;
            if (angle >= 45f && angle < 135f)
                return SwapDirection.Up;
            if (angle >= -135f && angle < -45f)
                return SwapDirection.Down;
            
            return SwapDirection.Left;
        }
        
        private BoardItem GetTargetItem(BoardItem sourceItem, SwapDirection direction)
        {
            int targetX = sourceItem.X;
            int targetY = sourceItem.Y;
            
            switch (direction)
            {
                case SwapDirection.Up:
                    targetY += 1;
                    break;
                case SwapDirection.Down:
                    targetY -= 1;
                    break;
                case SwapDirection.Left:
                    targetX -= 1;
                    break;
                case SwapDirection.Right:
                    targetX += 1;
                    break;
            }
            
            // Find GridManager to get the target item
            GridManager gridManager = FindFirstObjectByType<GridManager>();
            return gridManager?.GetItemAt(targetX, targetY);
        }
        
        private void ResetDragState()
        {
            _dragStartItem = null;
            _isDragging = false;
        }
    }
}