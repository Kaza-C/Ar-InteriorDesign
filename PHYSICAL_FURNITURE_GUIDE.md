# Physical Furniture Detection & Management Guide

## Overview

The AR Interior Design app now includes advanced capabilities for detecting, managing, and interacting with existing physical furniture in your room. This feature allows you to:

- **Detect existing furniture** automatically using AR scanning
- **Hide physical furniture** virtually to create clean design spaces
- **Mark furniture for removal** in your design planning
- **Prevent collisions** between virtual and physical furniture
- **Integrate real and virtual** furniture in your design workflow

## 🔍 How Physical Furniture Detection Works

### Detection Process

1. **Room Scanning**: The app uses AR Foundation's plane detection to map your room
2. **Grid-based Analysis**: A systematic grid scan across detected floor planes
3. **Ray Casting**: Upward rays from the floor detect objects above
4. **Size Filtering**: Objects must be between 0.3m and 3m in size
5. **Type Classification**: Heuristic-based furniture type estimation
6. **Confidence Scoring**: Quality assessment of each detection

### Furniture Types Detected

| Type | Detection Criteria |
|------|------------------|
| **Table** | Wide/long, moderate height (0.6-1.2m) |
| **Chair** | Moderate height (0.8-1.3m), small footprint |
| **Sofa** | Long (>1.5m), moderate height (0.6-1.0m) |
| **Bed** | Very long (>1.8m), low height (0.4-0.8m) |
| **Storage** | Tall (>1.2m), boxy shape |
| **Lamp** | Tall (>1.0m), narrow (<0.5m) |
| **Plant** | Variable size, organic detection |

### Detection Accuracy

- **Confidence Scoring**: 0-100% based on size, shape, and surface characteristics
- **Minimum Threshold**: 60% confidence required for detection
- **False Positive Filtering**: Duplicate removal within 0.5m radius
- **Adaptive Learning**: Improves with room scanning quality

## 🎮 User Interface Controls

### Main Controls

| Button | Function |
|--------|----------|
| **Detect Furniture** | Start/stop automatic detection |
| **Hide All Physical** | Virtually hide all detected furniture |
| **Restore All** | Show all hidden furniture |
| **Hiding Mode** | Toggle bounding box visualization |

### Individual Furniture Controls

For each detected furniture piece:

- **Hide/Show**: Toggle visibility of individual items
- **Mark/Unmark**: Mark furniture for removal planning
- **Type Info**: View estimated furniture type and size
- **Confidence Level**: See detection confidence percentage

## 🛠 Technical Implementation

### Core Components

1. **PhysicalFurnitureDetector.cs**
   - Main detection and management logic
   - AR plane integration
   - Object classification algorithms

2. **ARUIManager.cs** (Updated)
   - UI controls for physical furniture
   - Event handling and user feedback
   - List management and visualization

3. **ARInteriorDesignManager.cs** (Updated)
   - Integration with existing AR system
   - Collision detection with virtual furniture
   - Scene loading coordination

### Key Features

#### Occlusion Handling
- **Invisible Occluders**: Virtual objects that block view of physical furniture
- **Material Swapping**: Special materials for "hidden" furniture visualization
- **Depth Testing**: Proper rendering order for mixed reality

#### Collision Prevention
- **Spatial Awareness**: Prevents virtual furniture placement on physical objects
- **Radius Checking**: Warns about nearby conflicts
- **Smart Placement**: Suggests alternative locations

#### Visual Feedback
- **Bounding Boxes**: Yellow wireframe outlines around detected furniture
- **Confidence Indicators**: Color coding based on detection accuracy
- **Status Updates**: Real-time feedback on detection progress

## 🎯 Use Cases

### 1. Room Planning
- **Current State Analysis**: See what furniture you already have
- **Space Optimization**: Identify underutilized areas
- **Furniture Inventory**: Catalog existing pieces with measurements

### 2. Design Experimentation
- **Clean Slate Mode**: Hide all physical furniture to start fresh
- **Selective Hiding**: Hide only specific pieces for comparison
- **Mixed Reality**: Combine real and virtual furniture seamlessly

### 3. Renovation Planning
- **Removal Planning**: Mark furniture for removal or relocation
- **Space Visualization**: See rooms without current furniture
- **Before/After Comparison**: Toggle between current and planned layouts

### 4. Furniture Shopping
- **Size Validation**: Ensure new furniture fits around existing pieces
- **Style Coordination**: See how new items work with current furniture
- **Replacement Planning**: Virtually replace specific pieces

## 📱 Usage Instructions

### Getting Started

1. **Enable Detection**
   ```
   Settings > Physical Furniture > Enable Detection
   ```

2. **Scan Your Room**
   - Look around slowly to map all surfaces
   - Ensure good lighting for better detection
   - Focus on areas with furniture

3. **Review Detections**
   - Check the detected furniture list
   - Verify type classifications
   - Adjust confidence thresholds if needed

### Managing Physical Furniture

#### To Hide Furniture:
1. Open Physical Furniture panel
2. Select furniture from the list
3. Tap "Hide" button
4. Furniture disappears from view

#### To Mark for Removal:
1. Select furniture item
2. Tap "Mark" button
3. Furniture outline turns red
4. Use for planning renovations

#### To Restore Hidden Items:
1. Find item in hidden furniture list
2. Tap "Show" button
3. Or use "Restore All" for everything

### Advanced Features

#### Batch Operations
- **Hide All**: Quickly clear the entire room
- **Restore All**: Bring everything back
- **Mark Category**: Mark all chairs, tables, etc.

#### Filtering and Sorting
- **By Type**: Show only specific furniture types
- **By Size**: Filter by dimensions
- **By Confidence**: Show only high-confidence detections

#### Export and Sharing
- **Room Layouts**: Export current vs. planned layouts
- **Furniture Lists**: Generate inventory with measurements
- **Design Comparisons**: Share before/after visualizations

## 🔧 Configuration Options

### Detection Settings

```csharp
// Adjustable parameters in PhysicalFurnitureDetector
[SerializeField] private float minObjectSize = 0.3f;      // Minimum furniture size
[SerializeField] private float maxObjectSize = 3f;        // Maximum furniture size
[SerializeField] private float heightThreshold = 0.1f;    // Min height above floor
[SerializeField] private float confidenceThreshold = 0.6f; // Min confidence level
```

### Performance Tuning

- **Scan Frequency**: Adjust detection update rate
- **Grid Resolution**: Balance accuracy vs. performance
- **Confidence Thresholds**: Reduce false positives
- **Occlusion Quality**: Adjust rendering complexity

## 🐛 Troubleshooting

### Common Issues

#### No Furniture Detected
- **Check Lighting**: Ensure room is well-lit
- **Scan Completely**: Look at all areas slowly
- **Adjust Thresholds**: Lower confidence requirements
- **Check Permissions**: Ensure camera access

#### Incorrect Classifications
- **Size Constraints**: Verify furniture meets size criteria
- **Multiple Angles**: Scan from different positions
- **Clear Obstacles**: Remove items blocking furniture
- **Manual Correction**: Use override options

#### Performance Issues
- **Reduce Scan Rate**: Lower detection frequency
- **Limit Detections**: Set maximum object count
- **Optimize Materials**: Use simpler occlusion shaders
- **Clear Cache**: Reset detection data

### Error Messages

| Error | Meaning | Solution |
|-------|---------|----------|
| "No planes detected" | Room not scanned | Move around to map surfaces |
| "Detection confidence low" | Uncertain furniture type | Improve lighting, scan closer |
| "Collision detected" | Virtual/physical conflict | Adjust placement or hide physical |
| "Performance warning" | System overloaded | Reduce scan frequency/quality |

## 🔮 Future Enhancements

### Planned Features

1. **AI-Powered Recognition**
   - Machine learning furniture classification
   - Brand/model identification
   - Style and color recognition

2. **Cloud Integration**
   - Shared furniture databases
   - Community furniture catalogs
   - Cross-device synchronization

3. **Advanced Interactions**
   - Gesture-based furniture manipulation
   - Voice commands for hiding/showing
   - Automatic room layout suggestions

4. **Measurement Tools**
   - Precise dimension calculation
   - Area utilization analysis
   - Clearance and accessibility checks

### Performance Improvements

- **Optimized Scanning**: Faster detection algorithms
- **Selective Updates**: Only scan changed areas
- **Background Processing**: Non-blocking detection
- **Memory Management**: Efficient object handling

## 📊 Best Practices

### For Optimal Detection

1. **Lighting**: Ensure even, bright lighting
2. **Scanning**: Move slowly and systematically
3. **Angles**: Scan from multiple viewpoints
4. **Cleanup**: Remove clutter that might confuse detection

### For Design Workflow

1. **Start with Detection**: Always scan existing furniture first
2. **Use Selective Hiding**: Hide only what's needed for each design
3. **Mark Strategically**: Use marking to plan phased renovations
4. **Save Configurations**: Export layouts for future reference

### For Performance

1. **Limit Scope**: Focus detection on relevant areas
2. **Adjust Quality**: Balance accuracy with performance needs
3. **Clean Data**: Remove false positives regularly
4. **Monitor Resources**: Watch for memory/battery usage

This comprehensive physical furniture system transforms the AR Interior Design app from a simple placement tool into a complete room planning and design solution that works with your existing furniture! 