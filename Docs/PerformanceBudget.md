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
