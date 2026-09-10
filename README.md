# Patan Explorer

Patan Explorer is a browser-first, first-person 3D vertical slice of Patan Durbar Square in Lalitpur, Nepal. The current milestone centers on an original, detailed Krishna Mandir exterior surrounded by a simplified environment for testing scale, movement, collision, lighting, browser loading, and performance.

## Current milestone

The playable environment is limited to a 120 m by 90 m core and includes:

- The detailed Krishna Mandir exterior with textured materials and collision
- Simplified Vishwanath Temple and Char Narayan Temple exteriors
- The Garuda column ensemble
- A simplified palace frontage and Newari facade shells
- Plaza surfaces, paths, visual boundaries, lighting, and haze
- First-person walking, running, jumping, mouse look, and cursor capture
- A custom Brotli-compressed WebGL loading page

The MVP intentionally excludes NPCs, quests, combat, inventory, multiplayer, authentication, backend services, runtime AI, procedural city generation, interiors, and mobile controls.

## Controls

| Input | Action |
|---|---|
| `WASD` or arrow keys | Move |
| Mouse | Look around |
| Left or right `Shift` | Run |
| `Space` | Jump |
| Left mouse button | Capture the cursor |
| `Escape` | Release the cursor |
| Fullscreen button | Enter browser fullscreen mode |

Keyboard and mouse are required for this milestone.

## Prerequisites

Install the following before opening or building the project:

- Unity Hub
- Unity Editor `6000.3.23f1` with **Web Build Support**
- Git and Git LFS
- Python 3 for the local WebGL server
- A current desktop browser with WebGL 2 support

The project also uses:

- Universal Render Pipeline `17.3.0`
- Unity Input System `1.20.0`
- Blender `5.2.1` LTS for source art work
- Optional QGIS `3.44` LTR for offline geographic preprocessing

The exact Unity version is pinned in `Unity/PatanExplorer/ProjectSettings/ProjectVersion.txt`. Opening the project with another Unity version can rewrite scenes, materials, or project settings.

## Prepare a fresh checkout

Git LFS is required because the Krishna Mandir FBX is stored as an LFS object.

```powershell
git lfs install
git lfs pull
```

Run these commands from the repository root after cloning. Confirm that this is a real FBX rather than an unresolved LFS pointer:

```powershell
git lfs ls-files
```

The expected Unity project directory is:

```text
PatanExplorer/Unity/PatanExplorer
```

It is the inner directory containing `Assets`, `Packages`, and `ProjectSettings`.

## One-click Windows launcher

The easiest way to run the game on Windows is to double-click this file in the repository root:

```text
Run-PatanExplorer.cmd
```

The launcher performs the following workflow:

1. Verifies the Unity project and local server script.
2. Uses the existing complete WebGL build when one is available.
3. Builds and validates the public milestone automatically when no WebGL build exists.
4. Detects whether Patan Explorer is already running on the selected port.
5. Starts the Brotli-aware Python server when required.
6. Opens `http://127.0.0.1:8080` in the default browser.
7. Keeps the server running until Enter is pressed in the launcher window.

If the launcher encounters an error, its window stays open and displays the missing dependency, occupied port, Unity lock, build log, or Git LFS action needed to fix it.

The equivalent PowerShell command is:

```powershell
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File .\Deployment\Run-PatanExplorer.ps1
```

The default workflow does not rebuild when a complete local build already exists. Force a fresh WebGL build of the current scene, validation, server start, and browser launch after changing Unity content with:

```powershell
.\Run-PatanExplorer.cmd -Rebuild
```

or:

```powershell
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File .\Deployment\Run-PatanExplorer.ps1 -Rebuild
```

The launcher builds the current saved scene without regenerating or overwriting it. Stop an existing Patan Explorer server before forcing a rebuild. To intentionally recreate the deterministic scene, use **Patan > Create Public Milestone** in Unity after committing, stashing, or backing up manual scene edits.

Use another port when `8080` is unavailable:

```powershell
.\Run-PatanExplorer.cmd -Port 8081
```

The reusable workflow implementation is in `Deployment/Run-PatanExplorer.ps1`. The `.cmd` file only provides reliable double-click behavior when Windows does not execute `.ps1` files directly.

## Run in Unity Hub and the Unity Editor

### 1. Install the pinned Editor

1. Open Unity Hub.
2. Select **Installs**.
3. Install Unity Editor `6000.3.23f1` if it is not already available.
4. In the Editor installation options, add **Web Build Support**.

On the configured Windows workstation, the Editor is expected at:

```text
%USERPROFILE%\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe
```

If Unity Hub installed it somewhere else, use that location when running command-line builds.

### 2. Add the project to Unity Hub

1. Open the **Projects** section in Unity Hub.
2. Choose **Add** or **Open project from disk**.
3. Select `Unity/PatanExplorer` inside this repository.
4. Ensure Unity Hub selects Editor `6000.3.23f1`.
5. Open the project and wait for package restoration, asset import, shader compilation, and script compilation to finish.

The first import can take several minutes because Unity must import the FBX and textures and create its local `Library` cache.

### 3. Play the existing scene

1. In the Project window, open `Assets/Patan/Scenes/PatanSquare.unity`.
2. Confirm there are no red errors in **Window > General > Console**.
3. Press the Unity **Play** button.
4. Select the **Game** view.
5. Click inside the Game view to capture the cursor.
6. Use the controls listed above.
7. Press `Escape` to release the cursor and stop Play mode when finished.

You do not need to regenerate the scene simply to play the checked-in version.

### 4. Regenerate the public milestone scene

Use **Patan > Create Public Milestone** to recreate the deterministic scene from the Editor builder.

> **Warning:** This command overwrites `Assets/Patan/Scenes/PatanSquare.unity`. Commit, stash, or back up manual scene edits before running it.

The generated public scene requires the detailed model at:

```text
Assets/Patan/Art/KrishnaMandir/Models/KrishnaMandir.fbx
```

The builder fails rather than falling back to the old placeholder if that model or its material textures are unavailable.

## Run in a browser

Unity WebGL cannot be run reliably by double-clicking `index.html`. The compressed data, JavaScript, and WebAssembly files must be served over HTTP with the correct MIME types and `Content-Encoding: br` headers.

### Option A: Build through the Unity Editor

1. Open the project through Unity Hub.
2. Open **File > Build Profiles**.
3. Select or add the **Web** (`WebGL`) build profile.
4. Choose **Switch Platform** and wait for Unity to finish importing for WebGL.
5. Save or commit any manual scene changes.
6. Select **Patan > Build Web Release**.
7. Wait for Unity to report a successful build in the Console.
8. Select **Patan > Validate Public Milestone**.

> **Warning:** **Build Web Release** regenerates `PatanSquare.unity` before building it.

The generated browser build is written to:

```text
Builds/WebGreybox
```

The directory name is retained for compatibility with the existing deployment configuration, even though the public build now contains the detailed Krishna Mandir.

### Option B: Build and validate from PowerShell

Close any Unity Editor instance that already has the project open, then open PowerShell in the repository root:

```powershell
Set-Location "C:\path\to\PatanExplorer"

$unityEditor = "$HOME\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe"
$projectPath = "$PWD\Unity\PatanExplorer"
$buildDirectory = "$PWD\Builds"

New-Item -ItemType Directory -Force -Path $buildDirectory | Out-Null

$buildArguments = @(
    '-batchmode',
    '-quit',
    '-projectPath', $projectPath,
    '-buildTarget', 'WebGL',
    '-executeMethod', 'PatanExplorer.Editor.PatanMilestoneBuilder.BuildWebRelease',
    '-logFile', "$buildDirectory\WebGreybox-build.log"
)

$buildProcess = Start-Process `
    -FilePath $unityEditor `
    -ArgumentList $buildArguments `
    -PassThru `
    -Wait

if ($buildProcess.ExitCode -ne 0)
{
    throw "WebGL build failed. Read Builds\WebGreybox-build.log."
}

$validationArguments = @(
    '-batchmode',
    '-quit',
    '-projectPath', $projectPath,
    '-buildTarget', 'WebGL',
    '-executeMethod', 'PatanExplorer.Editor.PatanMilestoneValidator.ValidateMilestone',
    '-logFile', "$buildDirectory\GreyboxValidation.log"
)

$validationProcess = Start-Process `
    -FilePath $unityEditor `
    -ArgumentList $validationArguments `
    -PassThru `
    -Wait

if ($validationProcess.ExitCode -ne 0)
{
    throw "Milestone validation failed. Read Builds\GreyboxValidation.log."
}
```

Successful validation creates:

```text
Builds/GreyboxValidation.json
```

The report includes renderer, material, collider, input-action, missing-script, triangle, hero-height, and compressed-build measurements.

### Start the local browser server

From the repository root, run:

```powershell
python Deployment/serve_web.py --directory Builds/WebGreybox --port 8080
```

Keep that terminal open and visit:

```text
http://127.0.0.1:8080
```

Press `Ctrl+C` in the server terminal to stop it.

To use another port:

```powershell
python Deployment/serve_web.py --directory Builds/WebGreybox --port 8081
```

Then open `http://127.0.0.1:8081`.

### Run an existing local build without rebuilding

If `Builds/WebGreybox` already exists and is current, only the server command is required:

```powershell
python Deployment/serve_web.py --directory Builds/WebGreybox --port 8080
```

`Builds` is ignored by Git, so a fresh checkout normally needs a new WebGL build first.

### Verify Brotli response headers

Use the included server rather than `python -m http.server`. Check the compressed responses with:

```powershell
curl.exe --head http://127.0.0.1:8080/Build/WebGreybox.data.br
curl.exe --head http://127.0.0.1:8080/Build/WebGreybox.framework.js.br
curl.exe --head http://127.0.0.1:8080/Build/WebGreybox.wasm.br
```

Expected headers:

| File | `Content-Type` | `Content-Encoding` |
|---|---|---|
| `.data.br` | `application/octet-stream` | `br` |
| `.framework.js.br` | `application/javascript` | `br` |
| `.wasm.br` | `application/wasm` | `br` |

Also verify that the loading screen reaches 100%, the detailed Krishna Mandir renders, the browser console has no application errors, cursor capture works, and the player cannot pass through the temple collision.

## Deploy to Netlify

Netlify hosting is configured through [Deployment/Netlify/_headers](Deployment/Netlify/_headers), which sets the `Content-Encoding` and `Content-Type` headers the Brotli-compressed WebGL build needs. Netlify cannot run the Unity build itself, so build locally first, then upload the finished `Builds/WebGreybox` directory.

1. Install the Netlify CLI once: `npm install -g netlify-cli`.
2. Sign in once: `netlify login`.
3. Link this repository to a Netlify site once: `netlify sites:create --name <site-name> --manual` (or `netlify link` for an existing site).
4. Build the current scene: `.\Run-PatanExplorer.cmd -Rebuild`.
5. Deploy: `.\Deployment\Deploy-Netlify.ps1` for a draft preview, or `.\Deployment\Deploy-Netlify.ps1 -Prod` to publish to the production URL.

The script copies `_headers` into `Builds/WebGreybox` before calling `netlify deploy`, so the Brotli headers are always current. Draft deploys require a Netlify login to view; only a `-Prod` deploy is reachable at the site's public URL.

## Troubleshooting

### Unity reports that the project is already open

Close the Unity Editor instance using `Unity/PatanExplorer` before starting a batch build. Do not run two Unity processes against the same project directory.

### The Krishna Mandir model is missing or invalid

Retrieve the Git LFS objects and reopen Unity:

```powershell
git lfs pull
```

Check that `Assets/Patan/Art/KrishnaMandir/Models/KrishnaMandir.fbx` is a binary FBX and not a small text pointer.

### Web Build Support is missing

In Unity Hub, open the settings for Editor `6000.3.23f1`, select **Add modules**, and install **Web Build Support**.

### The build says the WebGL target is not active

Open **File > Build Profiles**, choose the Web/WebGL profile, and select **Switch Platform** before using **Patan > Build Web Release**.

### The browser displays a decompression or MIME error

Do not open `index.html` directly and do not use a generic static server without Brotli configuration. Start `Deployment/serve_web.py` and inspect the response headers described above.

### Port 8080 is already in use

Stop the process currently using the port or start the server on another port such as `8081`.

### The browser still shows an older build

Stop and restart the local server, then perform a hard refresh with `Ctrl+F5`. The included server sends `Cache-Control: no-cache`, but Unity data can also be retained in browser storage.

### The WebGL build fails

Review these files first:

```text
Builds/WebGreybox-build.log
Builds/GreyboxValidation.log
Builds/GreyboxValidation.json
```

Also check the Unity Console and the browser developer console for the first error rather than later follow-on messages.

## Repository layout

- `Unity/PatanExplorer`: Unity project
- `Unity/PatanExplorer/Assets/Patan/Art/KrishnaMandir`: public optimized Krishna Mandir FBX, textures, and materials
- `ArtSource`: Blender and source texture work
- `WorldData`: OSM provenance, survey metadata, and local world anchor
- `Docs`: MVP scope, performance budgets, and survey/source policy
- `Licenses`: asset ownership, licensing, and attribution records
- `Deployment`: Brotli-aware local server and static-host configuration
- `Builds`: generated local WebGL builds, logs, and validation reports; ignored by Git

## Asset rights and restricted source material

The optimized Krishna Mandir shipping asset is original project artwork recorded as **All rights reserved** in `Licenses/AssetRegister.csv`. Public repository access does not grant reuse rights beyond those explicitly provided by the copyright holder.

Survey scans, restricted photographs, third-party references, and other source material must not be committed unless their terms explicitly permit redistribution. Keep restricted source material in the ignored locations documented by the project policy.
