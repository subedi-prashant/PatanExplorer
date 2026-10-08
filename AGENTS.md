# Project Guidance

## Scope

This repository is a browser-first Unity vertical slice of Patan Durbar Square. Keep the MVP limited to the documented 120 m by 90 m core. The approved slice includes one rectangular road loop and one noncommercial enterable vehicle with lightweight arcade handling, a third-person camera, and original synthesized placeholder audio. Do not add more vehicles, traffic AI, racing systems, NPCs, quests, combat, inventory, multiplayer, backend services, runtime AI, procedural generation, interiors, Addressables, runtime GIS, or mobile controls without an approved scope change.

Krishna Mandir and major site layout may be called survey-derived only after written data permission is archived. Never commit restricted survey scans or references.

## Toolchain

- Unity Editor: `6000.3.23f1` at `$HOME\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe`
- Unity project: `Unity/PatanExplorer`
- URP: `17.3.0`
- Input System: `1.20.0`
- Blender: `5.2.1 LTS`
- Web output: `Builds/WebGreybox`, ignored by Git

## Build and verify

Double-click `Run-PatanExplorer.cmd` for the normal one-click workflow. Automated rebuilds must use `BuildCurrentSceneWebRelease`, which builds the saved scene without regenerating it. `BuildWebRelease` and `Patan/Create Public Milestone` overwrite `PatanSquare.unity` and must only be used when scene regeneration is explicitly intended.

```powershell
$unityEditor = "$HOME\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe"
$projectPath = "$PWD\Unity\PatanExplorer"
$buildArguments = @('-batchmode', '-quit', '-projectPath', $projectPath, '-buildTarget', 'WebGL', '-executeMethod', 'PatanExplorer.Editor.PatanMilestoneBuilder.BuildCurrentSceneWebRelease', '-logFile', "$PWD\Builds\WebGreybox-build.log")
Start-Process -FilePath $unityEditor -ArgumentList $buildArguments -Wait
$validationArguments = @('-batchmode', '-quit', '-projectPath', $projectPath, '-buildTarget', 'WebGL', '-executeMethod', 'PatanExplorer.Editor.PatanMilestoneValidator.ValidateMilestone', '-logFile', "$PWD\Builds\GreyboxValidation.log")
Start-Process -FilePath $unityEditor -ArgumentList $validationArguments -Wait
python Deployment/serve_web.py --directory Builds/WebGreybox --port 8080
```

Verify the Brotli response headers, browser console, loading state, walking and driving controls, Enter transitions, third-person camera collision, vehicle audio, road and temple collision routes, build bytes, and frame cadence. Update `Docs/PerformanceBudget.md` after each major art phase.

## Hard-won lessons

- Batch-mode Unity needs Unity Hub running (its licensing client supplies the Personal entitlement). Exit code 198 or `com.unity.editor.headless was not found` means start Hub and retry.
- `ValidateMilestone` runs the drive smoke test (`PatanDriveSmokeTest`). Keep it passing; it verifies nose direction, acceleration, steering and wheel direction, braking, and reverse without entering Play mode. When changing a sign or axis, confirm the test fails with the wrong sign before trusting it.
- The Ferrari body collider must keep the zero-friction `FerrariBody` physics material. With default friction the box on asphalt cancels nearly all engine force and the car crawls at 0.1 m/s. Traction, braking, and rolling resistance are handled in `ArcadeVehicleController`.
- Never generate audio at runtime with `AudioClip.Create` or `SetData`; Unity WebGL does not support that for uncompressed clips and the sound is silent. Pre-render WAV files with `ArtSource/Audio/generate_vehicle_audio.py` and import them as `Decompress On Load`.
- The headless Chromium used for automated checks cannot hold pointer lock (`WrongDocumentError`), so Enter, driving, and audio need a human pass in a normal browser.

## Code and assets

Use braces for every conditional and loop. Use PascalCase for classes and methods, camelCase for private/local variables and parameters, and fully capitalized constants. Do not add speculative systems or dependencies.

Keep `.blend` source outside Unity `Assets`; export explicit FBX files. Use Git LFS only for permitted large binary source assets. Register every external asset and its redistribution terms in `Licenses/AssetRegister.csv` before release.
