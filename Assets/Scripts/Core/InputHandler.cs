using UnityEngine;
using UnityEngine.InputSystem;

namespace Core
{
    public class InputHandler : MonoBehaviour
    {
        private Camera _mainCamera;

        private void Awake()
        {
            _mainCamera = Camera.main;
        }
        private void Update()
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                Vector2 mousePosition = Mouse.current.position.ReadValue();
                Vector3 worldPosition = _mainCamera.ScreenToWorldPoint(mousePosition);  
                
                RaycastHit2D hit = Physics2D.Raycast(worldPosition, Vector2.zero);
                if (hit.collider == null) return;
                
                if (hit.collider.gameObject.TryGetComponent(out BoardItem item))
                {
                    Debug.Log($"Clicked item at: {item.X}, {item.Y}");
                    GameEvents.ItemClicked(item.X, item.Y);
                }
            }
        }
    }
}