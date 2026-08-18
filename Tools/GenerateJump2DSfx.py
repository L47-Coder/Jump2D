"""Generate small, original procedural SFX for the Jump2D prototype.

The files are intentionally simple mono PCM WAVs so Unity can import them without
any third-party codec or editor package. Re-running this script overwrites only the
generated WAVs in Assets/Resources/Audio/SFX.
"""

from __future__ import annotations

import math
import random
import struct
import wave
from pathlib import Path


SAMPLE_RATE = 44100
OUTPUT_DIR = Path(__file__).resolve().parents[1] / "Assets" / "Resources" / "Audio" / "SFX"


def envelope(t: float, duration: float, attack: float = 0.005, release: float = 0.08) -> float:
    if t < 0.0 or t >= duration:
        return 0.0
    attack_part = 1.0 if attack <= 0.0 else min(1.0, t / attack)
    release_start = max(0.0, duration - release)
    release_part = 1.0 if t <= release_start else max(0.0, (duration - t) / max(0.0001, release))
    return attack_part * release_part


def sine(freq: float, t: float) -> float:
    return math.sin(2.0 * math.pi * freq * t)


def triangle(freq: float, t: float) -> float:
    phase = (freq * t) % 1.0
    return 4.0 * abs(phase - 0.5) - 1.0


def saw(freq: float, t: float) -> float:
    return 2.0 * ((freq * t) % 1.0) - 1.0


def chirp(frequency_start: float, frequency_end: float, t: float, duration: float) -> float:
    ratio = max(0.0, min(1.0, t / max(0.0001, duration)))
    # Linear frequency sweep; integrating the frequency gives a smooth phase.
    cycles = frequency_start * t + 0.5 * (frequency_end - frequency_start) * t * ratio
    return math.sin(2.0 * math.pi * cycles)


def noise(rng: random.Random) -> float:
    return rng.uniform(-1.0, 1.0)


def low_pass(samples: list[float], cutoff: float = 0.18) -> list[float]:
    result: list[float] = []
    previous = 0.0
    for sample in samples:
        previous += (sample - previous) * cutoff
        result.append(previous)
    return result


def normalize(samples: list[float], target_rms: float = 0.23, peak: float = 0.92) -> list[float]:
    # 仅按峰值归一化会让短促点击声和低频轰鸣声听起来差很多；
    # 这里用有效段 RMS 做统一响度，再用峰值限制避免削波。
    compressed_samples = []
    threshold = 0.55
    ratio = 4.0
    for sample in samples:
        magnitude = abs(sample)
        if magnitude > threshold:
            magnitude = threshold + (magnitude - threshold) / ratio
        compressed_samples.append(math.copysign(magnitude, sample))

    active_samples = [sample for sample in compressed_samples if abs(sample) >= 0.01]
    if not active_samples:
        return samples

    rms = math.sqrt(sum(sample * sample for sample in active_samples) / len(active_samples))
    maximum = max((abs(sample) for sample in compressed_samples), default=0.0)
    if rms <= 0.000001 or maximum <= 0.000001:
        return samples

    scale = target_rms / rms
    if maximum * scale > peak:
        scale = peak / maximum
    return [sample * scale for sample in compressed_samples]


def write_wav(name: str, samples: list[float]) -> None:
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    path = OUTPUT_DIR / f"{name}.wav"
    pcm = bytearray()
    for sample in normalize(samples):
        value = int(max(-1.0, min(1.0, sample)) * 32767.0)
        pcm.extend(struct.pack("<h", value))
    with wave.open(str(path), "wb") as wav_file:
        wav_file.setnchannels(1)
        wav_file.setsampwidth(2)
        wav_file.setframerate(SAMPLE_RATE)
        wav_file.writeframes(pcm)
    print(f"{path.name}: {len(samples) / SAMPLE_RATE:.3f}s, {path.stat().st_size} bytes")


def make_jump() -> list[float]:
    duration = 0.30
    return [
        (0.75 * chirp(230.0, 620.0, i / SAMPLE_RATE, duration)
         + 0.22 * sine(460.0, i / SAMPLE_RATE))
        * envelope(i / SAMPLE_RATE, duration, 0.008, 0.12)
        for i in range(int(duration * SAMPLE_RATE))
    ]


def make_shoot_pea() -> list[float]:
    duration = 0.12
    rng = random.Random(21)
    samples = []
    for i in range(int(duration * SAMPLE_RATE)):
        t = i / SAMPLE_RATE
        click = noise(rng) * math.exp(-t * 70.0)
        body = chirp(760.0, 260.0, t, duration) * math.exp(-t * 26.0)
        samples.append(0.62 * body + 0.18 * click)
    return samples


def make_shoot_machinegun() -> list[float]:
    duration = 0.07
    rng = random.Random(22)
    samples = []
    for i in range(int(duration * SAMPLE_RATE)):
        t = i / SAMPLE_RATE
        body = chirp(680.0, 310.0, t, duration) * math.exp(-t * 38.0)
        punch = sine(1700.0, t) * math.exp(-t * 115.0)
        click = noise(rng) * math.exp(-t * 150.0)
        samples.append((0.82 * body + 0.12 * punch + 0.08 * click) * envelope(t, duration, 0.001, 0.035))
    # 去掉尖锐的高频谐波，只留下短促、偏厚的机枪脉冲。
    return low_pass(samples, 0.16)


def make_shoot_corn() -> list[float]:
    duration = 0.24
    rng = random.Random(23)
    samples = []
    for i in range(int(duration * SAMPLE_RATE)):
        t = i / SAMPLE_RATE
        ratio = t / duration
        # 重新设计成中频明显的“玉米炮”：开头有脆响，中段有厚实炮腔，
        # 避免只剩下普通扬声器难以还原的超低频。
        thump = sine(190.0 - 95.0 * ratio, t) * math.exp(-t * 12.0)
        body = chirp(520.0, 210.0, t, duration) * math.exp(-t * 18.0)
        snap = chirp(1050.0, 420.0, t, 0.085) * math.exp(-t * 42.0)
        crack = noise(rng) * math.exp(-t * 80.0)
        samples.append(0.58 * thump + 0.72 * body + 0.36 * snap + 0.18 * crack)
    return samples


def make_projectile_hit() -> list[float]:
    duration = 0.13
    rng = random.Random(24)
    samples = []
    for i in range(int(duration * SAMPLE_RATE)):
        t = i / SAMPLE_RATE
        impact = chirp(430.0, 145.0, t, duration) * math.exp(-t * 30.0)
        snap = noise(rng) * math.exp(-t * 125.0)
        samples.append(0.64 * impact + 0.28 * snap)
    return samples


def make_enemy_death() -> list[float]:
    duration = 0.27
    samples = []
    for i in range(int(duration * SAMPLE_RATE)):
        t = i / SAMPLE_RATE
        wobble = sine(330.0 - 210.0 * (t / duration), t)
        second = triangle(170.0, t) * 0.28
        samples.append((0.68 * wobble + second) * envelope(t, duration, 0.006, 0.16))
    return samples


def make_weapon_pickup() -> list[float]:
    duration = 0.43
    notes = ((660.0, 0.00, 0.13), (880.0, 0.12, 0.14), (1180.0, 0.24, 0.19))
    rng = random.Random(25)
    samples = []
    for i in range(int(duration * SAMPLE_RATE)):
        t = i / SAMPLE_RATE
        value = 0.0
        for frequency, start, length in notes:
            if start <= t < start + length:
                local_t = t - start
                value += 0.42 * sine(frequency, local_t) * envelope(local_t, length, 0.004, 0.08)
        sparkle = noise(rng) * math.exp(-max(0.0, t - 0.25) * 22.0) if t > 0.25 else 0.0
        samples.append(value + 0.08 * sparkle)
    return samples


def make_explosion() -> list[float]:
    duration = 0.56
    rng = random.Random(26)
    samples = []
    for i in range(int(duration * SAMPLE_RATE)):
        t = i / SAMPLE_RATE
        ratio = t / duration
        # 玉米专属爆炸：先有可辨识的中频爆裂，再落到低频轰鸣。
        burst = chirp(900.0, 250.0, t, 0.12) * math.exp(-t * 24.0)
        boom = chirp(360.0, 92.0, t, duration) * math.exp(-t * 6.0)
        sub = sine(120.0 - 55.0 * ratio, t) * math.exp(-t * 7.0)
        grit = noise(rng) * math.exp(-t * 18.0)
        samples.append(0.52 * burst + 0.72 * boom + 0.42 * sub + 0.24 * grit)
    return samples


def make_player_hurt() -> list[float]:
    duration = 0.34
    rng = random.Random(27)
    samples = []
    for i in range(int(duration * SAMPLE_RATE)):
        t = i / SAMPLE_RATE
        warning = 0.55 * saw(250.0, t) + 0.25 * chirp(300.0, 110.0, t, duration)
        grit = noise(rng) * math.exp(-t * 22.0)
        samples.append((warning + 0.18 * grit) * envelope(t, duration, 0.004, 0.12))
    return samples


def make_game_over() -> list[float]:
    duration = 0.86
    notes = ((370.0, 0.00, 0.23), (294.0, 0.23, 0.25), (196.0, 0.48, 0.36))
    samples = []
    for i in range(int(duration * SAMPLE_RATE)):
        t = i / SAMPLE_RATE
        value = 0.0
        for frequency, start, length in notes:
            if start <= t < start + length:
                local_t = t - start
                value += 0.5 * sine(frequency, local_t) * envelope(local_t, length, 0.008, 0.12)
        samples.append(value)
    return samples


def make_ui_click() -> list[float]:
    duration = 0.075
    return [
        (0.8 * chirp(1050.0, 720.0, i / SAMPLE_RATE, duration))
        * envelope(i / SAMPLE_RATE, duration, 0.001, 0.045)
        for i in range(int(duration * SAMPLE_RATE))
    ]


def make_pause() -> list[float]:
    duration = 0.18
    return [
        (0.5 * sine(450.0, i / SAMPLE_RATE) + 0.3 * sine(675.0, i / SAMPLE_RATE))
        * envelope(i / SAMPLE_RATE, duration, 0.008, 0.09)
        for i in range(int(duration * SAMPLE_RATE))
    ]


def make_resume() -> list[float]:
    duration = 0.18
    return [
        (0.5 * sine(675.0, i / SAMPLE_RATE) + 0.3 * sine(900.0, i / SAMPLE_RATE))
        * envelope(i / SAMPLE_RATE, duration, 0.008, 0.09)
        for i in range(int(duration * SAMPLE_RATE))
    ]


def main() -> None:
    generators = {
        "sfx_jump": make_jump,
        "sfx_shoot_pea": make_shoot_pea,
        "sfx_shoot_machinegun": make_shoot_machinegun,
        "sfx_shoot_corn": make_shoot_corn,
        "sfx_projectile_hit": make_projectile_hit,
        "sfx_enemy_death": make_enemy_death,
        "sfx_weapon_pickup": make_weapon_pickup,
        "sfx_explosion": make_explosion,
        "sfx_player_hurt": make_player_hurt,
        "sfx_game_over": make_game_over,
        "sfx_ui_click": make_ui_click,
        "sfx_pause": make_pause,
        "sfx_resume": make_resume,
    }
    for name, generator in generators.items():
        write_wav(name, generator())


if __name__ == "__main__":
    main()
