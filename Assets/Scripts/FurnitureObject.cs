using UnityEngine;
using System.Collections.Generic;

namespace ARInteriorDesign
{
    [System.Serializable]
    public enum FurnitureCategory
    {
        Seating,
        Tables,
        Storage,
        Lighting,
        Decor,
        Appliances,
        Bedroom,
        Kitchen,
        Bathroom,
        Miscellaneous
    }

    [System.Serializable]
    public class FurnitureItem
    {
        public string name;
        public string displayName;
        public string description;
        public FurnitureCategory category;
        public string prefabName;
        public Sprite icon;
        public Vector3 defaultScale = Vector3.one;
        public float price;
        public string brand;
        public List<string> tags = new List<string>();
        public bool isAvailable = true;
    }

    public class FurnitureObject : MonoBehaviour
    {
        [Header("Furniture Data")]
        [SerializeField] private FurnitureItem item;
        
        [Header("Interaction")]
        [SerializeField] private bool isSelected = false;
        [SerializeField] private bool isInteractable = true;
        
        [Header("Visual")]
        [SerializeField] private GameObject selectionIndicator;
        [SerializeField] private Material originalMaterial;
        [SerializeField] private Material highlightMaterial;
        
        [Header("Physics")]
        [SerializeField] private Collider objectCollider;
        [SerializeField] private Rigidbody objectRigidbody;
        
        [Header("Spatial Anchoring")]
        [SerializeField] private string anchorId;
        [SerializeField] private bool isAnchored = false;
        
        private Renderer[] renderers;
        private Material[] originalMaterials;
        private Vector3 originalPosition;
        private Quaternion originalRotation;
        private Vector3 originalScale;
        private bool isDragging = false;
        
        // Events
        public System.Action<FurnitureObject> OnSelected;
        public System.Action<FurnitureObject> OnDeselected;
        public System.Action<FurnitureObject> OnMoved;
        public System.Action<FurnitureObject> OnRotated;
        public System.Action<FurnitureObject> OnScaled;
        public System.Action<FurnitureObject> OnDestroyed;
        
        public FurnitureItem Item => item;
        public bool IsSelected => isSelected;
        public bool IsInteractable => isInteractable;
        public bool IsAnchored => isAnchored;
        public string AnchorId => anchorId;
        
        private void Awake()
        {
            InitializeComponents();
        }
        
        private void InitializeComponents()
        {
            // Get collider
            if (objectCollider == null)
            {
                objectCollider = GetComponent<Collider>();
                if (objectCollider == null)
                {
                    objectCollider = gameObject.AddComponent<BoxCollider>();
                }
            }
            
            // Get rigidbody
            if (objectRigidbody == null)
            {
                objectRigidbody = GetComponent<Rigidbody>();
                if (objectRigidbody == null)
                {
                    objectRigidbody = gameObject.AddComponent<Rigidbody>();
                    objectRigidbody.isKinematic = true; // Prevent physics interference
                }
            }
            
            // Get renderers
            renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                originalMaterials = new Material[renderers.Length];
                for (int i = 0; i < renderers.Length; i++)
                {
                    originalMaterials[i] = renderers[i].material;
                }
            }
            
            // Create selection indicator if not assigned
            if (selectionIndicator == null)
            {
                CreateSelectionIndicator();
            }
            
            // Store original transform
            originalPosition = transform.position;
            originalRotation = transform.rotation;
            originalScale = transform.localScale;
        }
        
        private void CreateSelectionIndicator()
        {
            selectionIndicator = new GameObject("Selection Indicator");
            selectionIndicator.transform.SetParent(transform);
            selectionIndicator.transform.localPosition = Vector3.zero;
            selectionIndicator.transform.localRotation = Quaternion.identity;
            selectionIndicator.transform.localScale = Vector3.one;
            
            // Create a wireframe box around the object
            LineRenderer lineRenderer = selectionIndicator.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.color = Color.yellow;
            lineRenderer.startWidth = 0.01f;
            lineRenderer.endWidth = 0.01f;
            lineRenderer.useWorldSpace = false;
            
            // Draw wireframe box
            Bounds bounds = GetBounds();
            Vector3[] corners = GetBoundsCorners(bounds);
            
            // Define the lines for a wireframe box
            int[] lineIndices = {
                0, 1, 1, 2, 2, 3, 3, 0, // bottom face
                4, 5, 5, 6, 6, 7, 7, 4, // top face
                0, 4, 1, 5, 2, 6, 3, 7  // vertical edges
            };
            
            lineRenderer.positionCount = lineIndices.Length;
            for (int i = 0; i < lineIndices.Length; i++)
            {
                lineRenderer.SetPosition(i, corners[lineIndices[i]]);
            }
            
            selectionIndicator.SetActive(false);
        }
        
        private Bounds GetBounds()
        {
            Bounds bounds = new Bounds(transform.position, Vector3.zero);
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }
            return bounds;
        }
        
        private Vector3[] GetBoundsCorners(Bounds bounds)
        {
            Vector3 center = bounds.center;
            Vector3 size = bounds.size;
            
            return new Vector3[]
            {
                center + new Vector3(-size.x, -size.y, -size.z) * 0.5f,
                center + new Vector3(size.x, -size.y, -size.z) * 0.5f,
                center + new Vector3(size.x, -size.y, size.z) * 0.5f,
                center + new Vector3(-size.x, -size.y, size.z) * 0.5f,
                center + new Vector3(-size.x, size.y, -size.z) * 0.5f,
                center + new Vector3(size.x, size.y, -size.z) * 0.5f,
                center + new Vector3(size.x, size.y, size.z) * 0.5f,
                center + new Vector3(-size.x, size.y, size.z) * 0.5f,
            };
        }
        
        public void Initialize(FurnitureItem furnitureItem)
        {
            item = furnitureItem;
            
            // Set the name
            gameObject.name = $"Furniture_{item.name}";
            
            // Set default scale
            if (item.defaultScale != Vector3.zero)
            {
                transform.localScale = item.defaultScale;
            }
            
            // Set layer
            gameObject.layer = LayerMask.NameToLayer("Furniture");
            
            // Add tags
            if (item.tags.Count > 0)
            {
                gameObject.tag = item.tags[0];
            }
        }
        
        public void SetSelected(bool selected)
        {
            if (isSelected == selected) return;
            
            isSelected = selected;
            
            if (selectionIndicator != null)
            {
                selectionIndicator.SetActive(selected);
            }
            
            // Apply highlight material
            if (selected)
            {
                ApplyHighlightMaterial();
                OnSelected?.Invoke(this);
            }
            else
            {
                RestoreOriginalMaterial();
                OnDeselected?.Invoke(this);
            }
        }
        
        private void ApplyHighlightMaterial()
        {
            if (highlightMaterial == null) return;
            
            foreach (Renderer renderer in renderers)
            {
                renderer.material = highlightMaterial;
            }
        }
        
        private void RestoreOriginalMaterial()
        {
            if (originalMaterials == null) return;
            
            for (int i = 0; i < renderers.Length && i < originalMaterials.Length; i++)
            {
                renderers[i].material = originalMaterials[i];
            }
        }
        
        public void SetPosition(Vector3 position)
        {
            Vector3 oldPosition = transform.position;
            transform.position = position;
            
            if (oldPosition != position)
            {
                OnMoved?.Invoke(this);
            }
        }
        
        public void SetRotation(Quaternion rotation)
        {
            Quaternion oldRotation = transform.rotation;
            transform.rotation = rotation;
            
            if (oldRotation != rotation)
            {
                OnRotated?.Invoke(this);
            }
        }
        
        public void SetScale(Vector3 scale)
        {
            Vector3 oldScale = transform.localScale;
            transform.localScale = scale;
            
            if (oldScale != scale)
            {
                OnScaled?.Invoke(this);
            }
        }
        
        public void SetSpatialAnchor(Vector3 position, Quaternion rotation)
        {
            // In a real implementation, this would create a spatial anchor
            // For now, we'll just store the anchor data
            anchorId = System.Guid.NewGuid().ToString();
            isAnchored = true;
            
            Debug.Log($"Created spatial anchor {anchorId} for {item.name}");
        }
        
        public void SetAnchorId(string id)
        {
            anchorId = id;
            isAnchored = !string.IsNullOrEmpty(id);
        }
        
        public string GetAnchorId()
        {
            return anchorId;
        }
        
        public void SetInteractable(bool interactable)
        {
            isInteractable = interactable;
            
            if (objectCollider != null)
            {
                objectCollider.enabled = interactable;
            }
        }
        
        public void StartDrag()
        {
            isDragging = true;
            
            if (objectRigidbody != null)
            {
                objectRigidbody.isKinematic = true;
            }
        }
        
        public void EndDrag()
        {
            isDragging = false;
            
            if (objectRigidbody != null)
            {
                objectRigidbody.isKinematic = true; // Keep kinematic for AR
            }
        }
        
        public void ResetToOriginal()
        {
            transform.position = originalPosition;
            transform.rotation = originalRotation;
            transform.localScale = originalScale;
            
            OnMoved?.Invoke(this);
            OnRotated?.Invoke(this);
            OnScaled?.Invoke(this);
        }
        
        public void Duplicate()
        {
            // This would typically be handled by the FurnitureManager
            Debug.Log($"Duplicating furniture: {item.name}");
        }
        
        public void Delete()
        {
            OnDestroyed?.Invoke(this);
            Destroy(gameObject);
        }
        
        public Vector3 GetSize()
        {
            if (objectCollider != null)
            {
                return objectCollider.bounds.size;
            }
            
            return GetBounds().size;
        }
        
        public Vector3 GetCenter()
        {
            if (objectCollider != null)
            {
                return objectCollider.bounds.center;
            }
            
            return transform.position;
        }
        
        public bool IsInBounds(Vector3 point)
        {
            if (objectCollider != null)
            {
                return objectCollider.bounds.Contains(point);
            }
            
            return GetBounds().Contains(point);
        }
        
        public float GetDistanceToPoint(Vector3 point)
        {
            return Vector3.Distance(transform.position, point);
        }
        
        public bool IsVisibleFromCamera(Camera camera)
        {
            if (renderers.Length == 0) return false;
            
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
            Bounds bounds = GetBounds();
            
            return GeometryUtility.TestPlanesAABB(planes, bounds);
        }
        
        // Touch/Click handling
        private void OnMouseDown()
        {
            if (isInteractable)
            {
                SetSelected(true);
            }
        }
        
        private void OnMouseDrag()
        {
            if (isInteractable && isDragging)
            {
                // Handle dragging logic here
                // This would typically be handled by the AR system
            }
        }
        
        private void OnMouseUp()
        {
            if (isDragging)
            {
                EndDrag();
            }
        }
        
        // Collision detection
        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Furniture"))
            {
                Debug.Log($"Furniture collision: {item.name} with {collision.gameObject.name}");
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Wall") || other.CompareTag("Obstacle"))
            {
                Debug.Log($"Furniture {item.name} near wall/obstacle");
            }
        }
        
        private void OnDestroy()
        {
            OnDestroyed?.Invoke(this);
        }
        
        // Serialization helpers
        [System.Serializable]
        public class FurnitureObjectData
        {
            public FurnitureItem item;
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;
            public string anchorId;
            public bool isAnchored;
        }
        
        public FurnitureObjectData GetData()
        {
            return new FurnitureObjectData
            {
                item = item,
                position = transform.position,
                rotation = transform.rotation,
                scale = transform.localScale,
                anchorId = anchorId,
                isAnchored = isAnchored
            };
        }
        
        public void LoadData(FurnitureObjectData data)
        {
            item = data.item;
            transform.position = data.position;
            transform.rotation = data.rotation;
            transform.localScale = data.scale;
            anchorId = data.anchorId;
            isAnchored = data.isAnchored;
        }
    }
} 