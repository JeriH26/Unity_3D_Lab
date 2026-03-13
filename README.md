# Unity_3D_Lab

A Unity 3D lab project showcasing custom shaders and C# scripting techniques.
Target Unity version: **2022.3 LTS**

## Project Structure

```
Assets/
├── Scripts/
│   ├── PlayerController.cs   – First-person character movement & mouse look
│   ├── GameManager.cs        – Singleton game-state machine with score tracking
│   ├── CameraController.cs   – Follow / orbital third-person camera
│   ├── ShaderController.cs   – Runtime shader property animation (color, float, emission, UV scroll)
│   ├── LightController.cs    – Dynamic lights with flicker, color cycle & day/night cycle
│   └── ObjectSpawner.cs      – Object-pool spawner with configurable lifetime
└── Shaders/
    ├── UnlitColor.shader       – Unlit flat color with alpha
    ├── DiffuseLit.shader       – Lambert diffuse with texture and ambient
    ├── SpecularLit.shader      – Blinn-Phong specular highlight
    ├── WaveDisplacement.shader – Vertex-displaced sine wave surface
    ├── RimLighting.shader      – Fresnel/rim glow effect
    ├── ToonShading.shader      – Cel-shaded toon with outline pass
    ├── Dissolve.shader         – Noise-texture dissolve with glowing edge
    ├── Hologram.shader         – Transparent hologram with scanlines & glitch
    ├── NormalMap.shader        – Tangent-space normal mapping
    └── Wireframe.shader        – Geometry-shader wireframe overlay
```

## Getting Started

1. Open the project in **Unity 2022.3 LTS** (or later).
2. Let Unity import all assets and resolve packages.
3. Open `Assets/Scenes/MainScene.unity`.
4. Press **Play** to explore the scene.

## Shaders Overview

| Shader | Technique |
|--------|-----------|
| `UnlitColor` | Flat HLSL color pass |
| `DiffuseLit` | Lambert diffuse + ambient |
| `SpecularLit` | Blinn-Phong BRDF |
| `WaveDisplacement` | Vertex displacement via sine waves |
| `RimLighting` | Fresnel rim glow |
| `ToonShading` | Step-based toon lighting + geometry outline |
| `Dissolve` | Clip-based dissolve with emissive edge |
| `Hologram` | Fresnel + scanlines + vertex glitch |
| `NormalMap` | TBN tangent-space normal mapping |
| `Wireframe` | Geometry-shader barycentric wireframe |

## C# Scripts Overview

| Script | Purpose |
|--------|---------|
| `PlayerController` | FPS movement with `CharacterController`, mouse look, run & jump |
| `GameManager` | Singleton state machine – Main Menu / Playing / Paused / Game Over / Victory |
| `CameraController` | Follow camera (smooth damp) or orbital camera with collision |
| `ShaderController` | Animates color, float, emission, and UV-scroll shader properties via `MaterialPropertyBlock` |
| `LightController` | Manages flickering, color-cycling, zone-activation, and a full day/night cycle |
| `ObjectSpawner` | Object pool with configurable interval, radius, and lifetime |