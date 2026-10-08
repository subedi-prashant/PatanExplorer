# Browser Performance Budget

## Release targets

| Metric | Target | Review threshold |
|---|---:|---:|
| Compressed final transfer | 50 MB or less | 70 MB |
| Compressed greybox transfer | 25 MB or less | 35 MB |
| Cold interaction on tested 50 Mbps connection | 15 seconds or less | 20 seconds |
| Warm-cache interaction | 5 seconds or less | 8 seconds |
| Mid-range desktop at 1920 by 1080 | 60 FPS | 45 FPS sustained |
| Modern integrated graphics at reduced render scale | 30 FPS | No sustained sub-30 FPS |
| Peak visible triangles | 600,000 or less | 800,000 |
| Draw calls | 150 or less | 200 |
| SetPass calls | 80 or less | 120 |
| Typical Unity heap peak | 350 MB or less | 500 MB |
| Texture and lightmap memory estimate | 220 MB or less | 300 MB |
| Exploration-time managed allocation | 0 B per frame preferred | No visible GC spikes |

## Measurement gates

Record an empty/greybox release baseline before art import. Re-measure after the modular environment, Krishna Mandir, secondary structures, lighting bake, and release stripping. Optimize by reducing assets and rendering features before introducing streaming architecture.

## Greybox baseline — 2026-09-09

Test environment: Windows 11, Intel Iris Xe Graphics, 32 GB system memory, headless Chromium at a 1280 by 609 CSS viewport with DPR capped at 1.5, plus a human control/collision pass in the interactive browser preview.

| Measurement | Result |
|---|---:|
| Total generated Web files | 8,337,260 bytes / 7.95 MiB |
| Brotli data file | 3,184,931 bytes |
| Brotli WebAssembly file | 5,054,807 bytes |
| Cold Unity-ready time at simulated 50 Mbps and 40 ms latency | 2.878 seconds |
| Focused five-second frame sample | 300 frames |
| Average frame interval | 16.67 ms |
| 95th percentile frame interval | 16.9 ms |
| Renderers / materials / colliders | 129 / 7 / 130 |
| Instanced scene triangles | 4,480 |
| Missing scripts | 0 |
| Placeholder Krishna Mandir top | 19.67 m |
| Application console errors | 0 |

The six repeated `getInternalformatParameter` warnings in automated Chromium come from its headless SwiftShader WebGL capability probe; they are not emitted as application errors and did not appear as a functional issue in the human browser pass. Unity heap and production-CDN timing remain future measurements; JavaScript heap is not a valid substitute for Unity WebAssembly heap usage.

## Detailed Krishna Mandir baseline — 2026-09-10

Test environment: Windows 11, Intel Iris Xe Graphics, 32 GB system memory, automated Chromium with DPR capped at 1.5, and the Brotli-aware local server.

| Measurement | Result |
|---|---:|
| Total generated Web files | 13,432,834 bytes / 12.81 MiB |
| Brotli data file | 8,285,268 bytes |
| Brotli WebAssembly file | 5,050,040 bytes |
| Renderers / materials / colliders | 88 / 11 / 87 |
| Instanced scene triangles | 113,938 |
| Missing scripts | 0 |
| Detailed Krishna Mandir top | 19.67 m |
| Application console errors | 0 |

The detailed original Krishna Mandir remains below the compressed transfer and triangle budgets. Browser loading reached 100%, the detailed textured exterior rendered successfully, and all compressed data, JavaScript, and WebAssembly responses returned the required MIME and Brotli headers.

## Driving milestone baseline — 2026-10-08

Test environment: Windows 11, automated Chromium with DPR capped at 1.5, the Brotli-aware local server, a cleared browser cache, and simulated 50 Mbps download bandwidth with 40 ms latency. Frame cadence was sampled after Unity reached ready state. This replaces the 2026-10-02 measurement, which predated the pre-rendered audio, the zero-friction body collider, and the nose-direction fix.

| Measurement | Result |
|---|---:|
| Total generated Web files | 15,905,256 bytes / 15.17 MiB |
| Brotli data file | 10,655,322 bytes |
| Brotli WebAssembly file | 5,148,378 bytes |
| Cold Unity-ready time at simulated 50 Mbps and 40 ms latency | 3.991 seconds |
| Warm-cache Unity-ready time | 1.550 seconds |
| Focused five-second frame sample | 300 frames |
| Average frame interval | 16.635 ms |
| 95th percentile frame interval | 17.0 ms |
| Renderers / materials / colliders | 121 / 23 / 101 |
| Instanced scene triangles | 262,718 |
| Optimized Ferrari triangles | 148,576 |
| Vehicle audio (five 22.05 kHz mono WAV clips, in the Brotli data file) | about 0.1 MB compressed |
| Playable area | 90 m east-west by 120 m north-south |
| Drive smoke test | Passed |
| Missing scripts | 0 |
| Detailed Krishna Mandir top | 19.67 m |
| Application console errors | 0 |

The expanded road, vehicle, textures, audio, and vehicle code remain below the compressed transfer and 600,000-triangle budgets. The Ferrari rendered upright with its nose pointing along the road at its spawn, loading reached 100%, all Brotli headers were correct, and the only automated-browser warnings were the six previously documented headless WebGL capability probes.

Drive smoke test measurements (physics stepped at 50 Hz in the Editor): 2.47 m measured wheelbase against the F40's real 2.46 m, no drift when parked, 20.0 m/s after two seconds of throttle over 20.6 m, 38.7 degrees of yaw in 0.6 s at 8 m/s in both directions, a stop from 12 m/s in 2.9 m, and 10.2 m of travel in reverse over two seconds.

Headless Chromium cannot sustain Unity pointer lock (`WrongDocumentError`), so Enter transitions, camera collision, and audible output still require the normal human browser control pass.
