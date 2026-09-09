# Patan Explorer

A browser-first, first-person 3D vertical slice of Patan Durbar Square in Lalitpur, Nepal, centered on Krishna Mandir.

## MVP

The first playable environment is limited to a 120 m by 90 m core with Krishna Mandir, two simplified secondary temples, the Garuda ensemble, a palace-frontage shell, Newari facade shells, plaza surfaces, collision, baked presentation, and basic exploration.

The project intentionally excludes NPCs, quests, combat, inventory, multiplayer, authentication, backend services, runtime AI, procedural city generation, interiors, and mobile controls.

## Toolchain

- Unity 6000.3.23f1 LTS with Universal Render Pipeline 17.3.0 and Web Build Support
- Blender 5.2.1 LTS
- Git and Git LFS
- Optional QGIS 3.44 LTR for offline geographic preprocessing

The exact Unity patch is pinned in `Unity/PatanExplorer/ProjectSettings/ProjectVersion.txt`.

## Development

Open the project with Unity Hub or the pinned Editor. The `Patan` Editor menu can regenerate the deterministic greybox scene, build the Web release, and run milestone validation.

PowerShell build:

```powershell
$unityEditor = "$HOME\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe"
$projectPath = "$PWD\Unity\PatanExplorer"
$buildLog = "$PWD\Builds\WebGreybox-build.log"
$buildArguments = @('-batchmode', '-quit', '-projectPath', $projectPath, '-buildTarget', 'WebGL', '-executeMethod', 'PatanExplorer.Editor.PatanMilestoneBuilder.BuildWebRelease', '-logFile', $buildLog)
Start-Process -FilePath $unityEditor -ArgumentList $buildArguments -Wait
```

Validate:

```powershell
$validationLog = "$PWD\Builds\GreyboxValidation.log"
$validationArguments = @('-batchmode', '-quit', '-projectPath', $projectPath, '-buildTarget', 'WebGL', '-executeMethod', 'PatanExplorer.Editor.PatanMilestoneValidator.ValidateMilestone', '-logFile', $validationLog)
Start-Process -FilePath $unityEditor -ArgumentList $validationArguments -Wait
```

Serve the Brotli build locally with correct MIME and encoding headers:

```powershell
python Deployment/serve_web.py --directory Builds/WebGreybox --port 8080
```

Then open `http://127.0.0.1:8080`.

## Repository

- `Unity/PatanExplorer`: Unity project
- `ArtSource`: Blender and source texture work
- `WorldData`: OSM provenance, survey metadata, and local world anchor
- `Docs`: scope, art workflow, performance, and survey decisions
- `Licenses`: third-party asset and attribution records
- `Deployment`: static host configuration
- `Builds`: generated local builds; not committed

Survey scans and other restricted references must not be committed unless their license explicitly permits redistribution.
