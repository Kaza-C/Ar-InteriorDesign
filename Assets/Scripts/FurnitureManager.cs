using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace ARInteriorDesign
{
    public class FurnitureManager : MonoBehaviour
    {
        [Header("Furniture Settings")]
        [SerializeField] private Transform furnitureContainer;
        [SerializeField] private Material highlightMaterial;
        [SerializeField] private LayerMask furnitureLayer = 1 << 8;
        
        [Header("Placement Settings")]
        [SerializeField] private float snapDistance = 0.1f;
        [SerializeField] private float rotationStep = 45f;
        [SerializeField] private Vector3 defaultScale = Vector3.one;
        
        private List<FurnitureObject> placedFurniture = new List<FurnitureObject>();
        private FurnitureObject selectedFurniture;
        private Dictionary<string, GameObject> furniturePrefabs = new Dictionary<string, GameObject>();
        
        // Events
        public System.Action<FurnitureObject> OnFurnitureSelected;
        public System.Action<FurnitureObject> OnFurnitureDeselected;
        public System.Action<FurnitureObject> OnFurniturePlaced;
        public System.Action<FurnitureObject> OnFurnitureRemoved;
        
        private void Awake()
        {
            if (furnitureContainer == null)
            {
                GameObject container = new GameObject("Furniture Container");
                furnitureContainer = container.transform;
                furnitureContainer.SetParent(transform);
            }
            
            LoadFurniturePrefabs();
        }
        
        private void LoadFurniturePrefabs()
        {
            // Load all furniture prefabs from Resources folder
            GameObject[] prefabs = Resources.LoadAll<GameObject>("Furniture");
            
            foreach (GameObject prefab in prefabs)
            {
                if (prefab.GetComponent<FurnitureObject>() != null)
                {
                    furniturePrefabs[prefab.name] = prefab;
                }
            }
            
            Debug.Log($"Loaded {furniturePrefabs.Count} furniture prefabs");
        }
        
        public GameObject PlaceFurniture(FurnitureItem item, Vector3 position, Quaternion rotation)
        {
            if (item == null || !furniturePrefabs.ContainsKey(item.prefabName))
            {
                Debug.LogWarning($"Furniture prefab not found: {item.prefabName}");
                return null;
            }
            
            GameObject prefab = furniturePrefabs[item.prefabName];
            GameObject furnitureObj = Instantiate(prefab, position, rotation, furnitureContainer);
            
            // Setup furniture object
            FurnitureObject furniture = furnitureObj.GetComponent<FurnitureObject>();
            if (furniture == null)
            {
                furniture = furnitureObj.AddComponent<FurnitureObject>();
            }
            
            furniture.Initialize(item);
            furniture.SetPosition(position);
            furniture.SetRotation(rotation);
            furniture.SetScale(defaultScale);
            
            // Add to placed furniture list
            placedFurniture.Add(furniture);
            
            // Setup interaction events
            furniture.OnSelected += OnFurnitureObjectSelected;
            furniture.OnDeselected += OnFurnitureObjectDeselected;
            
            OnFurniturePlaced?.Invoke(furniture);
            
            Debug.Log($"Placed furniture: {item.name} at {position}");
            
            return furnitureObj;
        }
        
        public void RemoveFurniture(GameObject furnitureObj)
        {
            FurnitureObject furniture = furnitureObj.GetComponent<FurnitureObject>();
            if (furniture != null)
            {
                RemoveFurniture(furniture);
            }
        }
        
        public void RemoveFurniture(FurnitureObject furniture)
        {
            if (furniture == null) return;
            
            // Remove from list
            placedFurniture.Remove(furniture);
            
            // Cleanup events
            furniture.OnSelected -= OnFurnitureObjectSelected;
            furniture.OnDeselected -= OnFurnitureObjectDeselected;
            
            // Deselect if currently selected
            if (selectedFurniture == furniture)
            {
                DeselectFurniture();
            }
            
            OnFurnitureRemoved?.Invoke(furniture);
            
            // Destroy the object
            Destroy(furniture.gameObject);
            
            Debug.Log($"Removed furniture: {furniture.Item.name}");
        }
        
        public void SelectFurniture(FurnitureObject furniture)
        {
            if (furniture == null) return;
            
            // Deselect current selection
            DeselectFurniture();
            
            // Select new furniture
            selectedFurniture = furniture;
            furniture.SetSelected(true);
            
            OnFurnitureSelected?.Invoke(furniture);
        }
        
        public void DeselectFurniture()
        {
            if (selectedFurniture != null)
            {
                selectedFurniture.SetSelected(false);
                OnFurnitureDeselected?.Invoke(selectedFurniture);
                selectedFurniture = null;
            }
        }
        
        public void MoveFurniture(FurnitureObject furniture, Vector3 newPosition)
        {
            if (furniture == null) return;
            
            // Snap to grid if close enough
            Vector3 snappedPosition = SnapToGrid(newPosition);
            furniture.SetPosition(snappedPosition);
            
            // Check for collisions with other furniture
            if (CheckCollision(furniture))
            {
                // Handle collision - could move back or highlight conflict
                Debug.LogWarning("Furniture collision detected");
            }
        }
        
        public void RotateFurniture(FurnitureObject furniture, float angle)
        {
            if (furniture == null) return;
            
            // Snap rotation to steps
            float snappedAngle = Mathf.Round(angle / rotationStep) * rotationStep;
            Quaternion newRotation = Quaternion.Euler(0, snappedAngle, 0);
            furniture.SetRotation(newRotation);
        }
        
        public void ScaleFurniture(FurnitureObject furniture, Vector3 scale)
        {
            if (furniture == null) return;
            
            // Clamp scale to reasonable values
            scale = Vector3.Max(scale, Vector3.one * 0.1f);
            scale = Vector3.Min(scale, Vector3.one * 5f);
            
            furniture.SetScale(scale);
        }
        
        public void DuplicateFurniture(FurnitureObject furniture)
        {
            if (furniture == null) return;
            
            Vector3 offset = Vector3.right * 1f; // Offset duplicate slightly
            Vector3 newPosition = furniture.transform.position + offset;
            
            PlaceFurniture(furniture.Item, newPosition, furniture.transform.rotation);
        }
        
        public void ClearAllFurniture()
        {
            var furnitureToRemove = placedFurniture.ToArray();
            foreach (var furniture in furnitureToRemove)
            {
                RemoveFurniture(furniture);
            }
            
            placedFurniture.Clear();
            selectedFurniture = null;
            
            Debug.Log("Cleared all furniture");
        }
        
        public List<FurnitureObject> GetFurnitureInRadius(Vector3 center, float radius)
        {
            return placedFurniture.Where(f => Vector3.Distance(f.transform.position, center) <= radius).ToList();
        }
        
        public List<FurnitureObject> GetFurnitureByCategory(FurnitureCategory category)
        {
            return placedFurniture.Where(f => f.Item.category == category).ToList();
        }
        
        private Vector3 SnapToGrid(Vector3 position)
        {
            if (snapDistance <= 0) return position;
            
            float snappedX = Mathf.Round(position.x / snapDistance) * snapDistance;
            float snappedZ = Mathf.Round(position.z / snapDistance) * snapDistance;
            
            return new Vector3(snappedX, position.y, snappedZ);
        }
        
        private bool CheckCollision(FurnitureObject furniture)
        {
            Collider furnitureCollider = furniture.GetComponent<Collider>();
            if (furnitureCollider == null) return false;
            
            foreach (var other in placedFurniture)
            {
                if (other == furniture) continue;
                
                Collider otherCollider = other.GetComponent<Collider>();
                if (otherCollider != null && furnitureCollider.bounds.Intersects(otherCollider.bounds))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        private void OnFurnitureObjectSelected(FurnitureObject furniture)
        {
            SelectFurniture(furniture);
        }
        
        private void OnFurnitureObjectDeselected(FurnitureObject furniture)
        {
            if (selectedFurniture == furniture)
            {
                DeselectFurniture();
            }
        }
        
        // Save/Load functionality
        [System.Serializable]
        public class FurnitureData
        {
            public string itemName;
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;
            public string anchorId;
        }
        
        [System.Serializable]
        public class RoomData
        {
            public string roomName;
            public List<FurnitureData> furniture = new List<FurnitureData>();
        }
        
        public RoomData SaveRoom(string roomName)
        {
            RoomData roomData = new RoomData();
            roomData.roomName = roomName;
            
            foreach (var furniture in placedFurniture)
            {
                FurnitureData data = new FurnitureData();
                data.itemName = furniture.Item.name;
                data.position = furniture.transform.position;
                data.rotation = furniture.transform.rotation;
                data.scale = furniture.transform.localScale;
                data.anchorId = furniture.GetAnchorId();
                
                roomData.furniture.Add(data);
            }
            
            return roomData;
        }
        
        public void LoadRoom(RoomData roomData)
        {
            if (roomData == null) return;
            
            ClearAllFurniture();
            
            foreach (var furnitureData in roomData.furniture)
            {
                // Find the furniture item by name
                var item = FindFurnitureItemByName(furnitureData.itemName);
                if (item != null)
                {
                    GameObject furnitureObj = PlaceFurniture(item, furnitureData.position, furnitureData.rotation);
                    if (furnitureObj != null)
                    {
                        furnitureObj.transform.localScale = furnitureData.scale;
                        
                        FurnitureObject furniture = furnitureObj.GetComponent<FurnitureObject>();
                        if (furniture != null && !string.IsNullOrEmpty(furnitureData.anchorId))
                        {
                            furniture.SetAnchorId(furnitureData.anchorId);
                        }
                    }
                }
            }
            
            Debug.Log($"Loaded room: {roomData.roomName} with {roomData.furniture.Count} furniture items");
        }
        
        private FurnitureItem FindFurnitureItemByName(string name)
        {
            // This would typically come from the furniture catalog
            // For now, create a basic item
            return new FurnitureItem
            {
                name = name,
                prefabName = name,
                category = FurnitureCategory.Miscellaneous,
                displayName = name,
                description = $"Loaded {name}"
            };
        }
        
        public FurnitureObject GetSelectedFurniture()
        {
            return selectedFurniture;
        }
        
        public List<FurnitureObject> GetAllFurniture()
        {
            return new List<FurnitureObject>(placedFurniture);
        }
        
        public int GetFurnitureCount()
        {
            return placedFurniture.Count;
        }
    }
} 