import math
import os
import random
import struct
import sys
import wave

SAMPLE_RATE = 22050
LOOP_SAMPLE_COUNT = SAMPLE_RATE


def ClampSample(value):
    return max(-1.0, min(1.0, value))


def WriteWave(path, samples):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    frames = b"".join(struct.pack("<h", round(ClampSample(sample) * 32767)) for sample in samples)
    with wave.open(path, "wb") as audioFile:
        audioFile.setnchannels(1)
        audioFile.setsampwidth(2)
        audioFile.setframerate(SAMPLE_RATE)
        audioFile.writeframes(frames)


def CreateEngineLoop():
    samples = []
    for index in range(LOOP_SAMPLE_COUNT):
        time = index / SAMPLE_RATE
        pulse = math.sin(2.0 * math.pi * 55.0 * time)
        harmonic = math.sin(2.0 * math.pi * 110.0 * time) * 0.42
        upperHarmonic = math.sin(2.0 * math.pi * 220.0 * time) * 0.16
        modulation = 0.82 + math.sin(2.0 * math.pi * 11.0 * time) * 0.18
        samples.append((pulse + harmonic + upperHarmonic) * modulation * 0.38)

    return samples


def CreateNoiseLoop(seed, brightness):
    samples = [0.0] * LOOP_SAMPLE_COUNT
    randomGenerator = random.Random(seed)
    partialCount = 18
    for partial in range(partialCount):
        frequency = 37 + randomGenerator.randrange(10, 1450)
        phase = randomGenerator.random() * math.pi * 2.0
        amplitude = (0.08 + (0.012 - 0.08) * partial / (partialCount - 1)) * brightness
        for index in range(LOOP_SAMPLE_COUNT):
            time = index / SAMPLE_RATE
            samples[index] += math.sin(2.0 * math.pi * frequency * time + phase) * amplitude

    return samples


def CreateStartClip():
    sampleCount = round(SAMPLE_RATE * 0.75)
    samples = []
    phase = 0.0
    for index in range(sampleCount):
        progress = index / (sampleCount - 1)
        smoothProgress = progress * progress * (3.0 - 2.0 * progress)
        frequency = 38.0 + (92.0 - 38.0) * smoothProgress
        phase += 2.0 * math.pi * frequency / SAMPLE_RATE
        envelope = math.sin(progress * math.pi)
        samples.append((math.sin(phase) + math.sin(phase * 2.0) * 0.35) * envelope * 0.48)

    return samples


def CreateImpactClip():
    sampleCount = round(SAMPLE_RATE * 0.3)
    samples = []
    randomGenerator = random.Random(47)
    filteredNoise = 0.0
    for index in range(sampleCount):
        progress = index / (sampleCount - 1)
        noise = randomGenerator.random() * 2.0 - 1.0
        filteredNoise += (noise - filteredNoise) * 0.18
        samples.append(filteredNoise * ((1.0 - progress) ** 3.0) * 0.75)

    return samples


def Main():
    if len(sys.argv) != 2:
        raise ValueError("Expected one output directory argument.")

    outputDirectory = os.path.abspath(sys.argv[1])
    WriteWave(os.path.join(outputDirectory, "FerrariEngineLoop.wav"), CreateEngineLoop())
    WriteWave(os.path.join(outputDirectory, "FerrariRoadLoop.wav"), CreateNoiseLoop(11, 0.7))
    WriteWave(os.path.join(outputDirectory, "FerrariSkidLoop.wav"), CreateNoiseLoop(29, 1.0))
    WriteWave(os.path.join(outputDirectory, "FerrariEngineStart.wav"), CreateStartClip())
    WriteWave(os.path.join(outputDirectory, "FerrariImpact.wav"), CreateImpactClip())
    print(f"Generated vehicle audio in {outputDirectory}")


Main()
