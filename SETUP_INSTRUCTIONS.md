# AR Interior Design - Setup Instructions

## Prerequisites

1. **Unity 2022.3 LTS** or newer
2. **Oculus Quest 3** headset
3. **Meta Quest Developer Hub** or **SideQuest** for deployment
4. **Android SDK** (automatically installed with Unity)
5. **Meta XR SDK** (Oculus Integration)

## Unity Setup

### 1. Create New Unity Project
1. Open Unity Hub
2. Click "New Project"
3. Select "3D" template
4. Name your project "AR-InteriorDesign"
5. Click "Create"

### 2. Import Project Files
1. Copy all files from this repository into your Unity project folder
2. The project structure should look like this:
   ```
   AR-InteriorDesign/
   ├── Assets/
   │   ├── Scripts/
   │   ├── Prefabs/
   │   ├── Materials/
   │   └── Scenes/
   ├── Packages/
   ├── ProjectSettings/
   └── README.md
   ```

### 3. Install Required Packages
1. Open **Window > Package Manager**
2. Install the following packages:
   - XR Interaction Toolkit
   - AR Foundation
   - AR Core XR Plugin (for Android)
   - OpenXR Plugin
   - Oculus XR Plugin

### 4. Import Oculus Integration
1. Go to **Asset Store** in Unity
2. Search for "Oculus Integration"
3. Download and import the package
4. When prompted, click "Yes" to enable the new input system

### 5. Configure XR Settings
1. Go to **Edit > Project Settings**
2. Navigate to **XR Plug-in Management**
3. Check **Oculus** under Android settings
4. Go to **XR Plug-in Management > Oculus**
5. Enable **Passthrough** support
6. Enable **Hand Tracking** support

## Android Build Settings

### 1. Platform Settings
1. Go to **File > Build Settings**
2. Select **Android** platform
3. Click **Switch Platform**
4. Set **Texture Compression** to ASTC

### 2. Player Settings
1. Go to **Edit > Project Settings > Player**
2. Under **Android Settings**:
   - Set **Minimum API Level** to 26
   - Set **Target API Level** to 33
   - Set **Graphics APIs** to OpenGLES3, Vulkan
   - Enable **Multithreaded Rendering**

### 3. XR Settings
1. Under **XR Settings**:
   - Set **Stereo Rendering Mode** to Multiview
   - Enable **Virtual Reality Supported**
   - Add **Oculus** to VR SDKs

## Scene Setup

### 1. Create Main Scene
1. Create a new scene: **File > New Scene**
2. Save it as **Assets/Scenes/MainScene.unity**

### 2. Setup AR Session
1. Create empty GameObject named "AR Session Origin"
2. Add the following components:
   - **AR Session Origin**
   - **AR Camera**
   - **AR Plane Manager**
   - **AR Raycast Manager**
   - **AR Anchor Manager**

### 3. Add AR Session
1. Create another empty GameObject named "AR Session"
2. Add **AR Session** component

### 4. Setup Main Components
1. Create empty GameObject named "AR Interior Design Manager"
2. Add the **ARInteriorDesignManager** script
3. Create UI Canvas for the interface
4. Add **ARUIManager** script to UI Manager GameObject

## Oculus Quest 3 Setup

### 1. Enable Developer Mode
1. Install **Meta Quest Developer Hub**
2. Create developer account at developer.oculus.com
3. Enable Developer Mode on your Quest 3
4. Enable USB Debugging

### 2. Build and Deploy
1. Connect Quest 3 to computer via USB-C
2. In Unity, go to **File > Build Settings**
3. Click **Build And Run**
4. Select a location to save the APK
5. Unity will build and install the app on your Quest 3

## Testing the Application

### 1. Launch the App
1. Put on your Quest 3 headset
2. Navigate to **Library > Unknown Sources**
3. Find and launch "AR Interior Design"

### 2. Basic Testing
1. Allow camera permissions when prompted
2. Look around to scan your room
3. Tap the furniture catalog button
4. Select a furniture item
5. Tap on a detected surface to place furniture
6. Use hand tracking or controllers to manipulate objects

## Troubleshooting

### Common Issues

1. **App won't launch**: Check that Developer Mode is enabled and USB debugging is allowed
2. **No plane detection**: Ensure good lighting and textured surfaces
3. **Poor tracking**: Move slowly and ensure the room has good lighting
4. **UI not responding**: Check that the UI Canvas is properly configured

### Performance Tips

1. **Optimize for mobile**: Use simple materials and low-poly models
2. **Limit furniture count**: Don't place too many objects simultaneously
3. **Use occlusion culling**: Enable to improve performance
4. **Reduce texture sizes**: Use compressed textures for better performance

## Additional Resources

- [Meta Quest Developer Documentation](https://developer.oculus.com/documentation/unity/)
- [Unity XR Interaction Toolkit](https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@latest)
- [AR Foundation Documentation](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@latest)

## Support

If you encounter any issues:
1. Check the Unity Console for error messages
2. Verify all dependencies are properly installed
3. Ensure your Quest 3 firmware is up to date
4. Test in a well-lit environment with textured surfaces

## Next Steps

Once you have the basic app running, you can:
1. Add more furniture models to the Resources/Furniture folder
2. Customize the UI layout and colors
3. Add new features like room sharing or furniture shopping
4. Implement cloud save functionality
5. Add sound effects and haptic feedback 