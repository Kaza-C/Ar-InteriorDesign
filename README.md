# AR Interior Design - Oculus Quest 3

An AR interior design application for the Oculus Quest 3 that allows users to visualize and arrange furniture in their real-world spaces.

## Features

- **Room Scanning**: Uses Quest 3's AR capabilities to scan and map your room
- **Furniture Placement**: Place virtual furniture in your real space
- **Object Manipulation**: Move, rotate, and scale furniture pieces
- **Furniture Catalog**: Browse and select from various furniture categories
- **Physical Furniture Detection**: Automatically detect and manage existing furniture
- **Mixed Reality Mode**: Hide existing furniture to create clean design spaces
- **Collision Prevention**: Smart placement that avoids physical furniture conflicts
- **Furniture Inventory System**: Create a personal catalog of your real furniture
- **Virtual Furniture Placement**: Use your real furniture in virtual room designs
- **Moving & Planning Tools**: Perfect for relocation and renovation planning
- **Save/Load Rooms**: Save your room configurations for later use
- **Real-time Preview**: See how furniture looks in your actual space

## Requirements

- Oculus Quest 3 headset
- Unity 2022.3 LTS or later
- Meta XR SDK
- AR Foundation

## Setup

1. Open the project in Unity
2. Ensure Meta XR SDK is imported
3. Build and deploy to Quest 3 device
4. Enable passthrough and spatial mapping permissions

## Project Structure

```
Assets/
├── Scripts/          # C# scripts for AR functionality
├── Prefabs/          # Furniture and UI prefabs
├── Materials/        # Materials and shaders
├── Models/           # 3D furniture models
├── Scenes/           # Unity scenes
├── UI/               # UI assets and components
└── Resources/        # Runtime resources
```

## Usage

1. Launch the app on your Quest 3
2. Allow spatial mapping permissions
3. Scan your room by looking around
4. **Detect existing furniture** automatically or manually
5. **Hide physical furniture** if desired for clean design space
6. Select furniture from the catalog
7. Place and arrange items in your space
8. **Manage both virtual and physical** furniture together
9. Save your room configuration

### Physical Furniture Management

- **Auto-Detection**: The app automatically scans for existing furniture
- **Hide/Show**: Toggle visibility of real furniture to create clean spaces
- **Mark for Removal**: Plan furniture removal or relocation
- **Collision Avoidance**: Prevents placing virtual furniture on physical objects
- **Mixed Reality**: Seamlessly blend real and virtual furniture in your designs

### Furniture Inventory System

- **Personal Catalog**: Build a digital inventory of your real furniture
- **Virtual Placement**: Use your real furniture pieces in virtual room designs
- **Moving Assistant**: Perfect for planning furniture arrangements in new homes
- **Renovation Planning**: Temporarily "store" furniture during renovation design
- **Usage Analytics**: Track which furniture pieces you use most in designs
- **Export/Import**: Backup and share your furniture inventory

See `PHYSICAL_FURNITURE_GUIDE.md` for physical furniture management and `FURNITURE_INVENTORY_GUIDE.md` for detailed inventory instructions. 