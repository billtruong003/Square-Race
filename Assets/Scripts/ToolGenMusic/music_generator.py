import os
import json
import numpy as np
from scipy.io.wavfile import write

# ==========================================
# CONFIGURATION
# ==========================================
SAMPLE_RATE = 44100
BASE_AMPLITUDE = 20000 
NOTE_DURATION_SEC = 0.35 

# ==========================================
# SOUND SYNTHESIS ENGINE
# ==========================================
def note_to_freq(note_name):
    notes = ['C', 'C#', 'D', 'D#', 'E', 'F', 'F#', 'G', 'G#', 'A', 'A#', 'B']
    
    if 'b' in note_name:
        # Xử lý đơn giản cho ký hiệu giáng (flat), thực tế nên map chính xác hơn
        # nhưng ở đây ta giả định input data đã dùng # (Sharp)
        pass 

    if len(note_name) == 3:
        key = note_name[:2]
        octave = int(note_name[2])
    else:
        key = note_name[:1]
        octave = int(note_name[1])

    key_index = notes.index(key)
    midi_num = 12 + (octave * 12) + key_index
    freq = 440.0 * (2.0 ** ((midi_num - 69) / 12.0))
    return freq

def generate_analog_synth_wave(freq, duration):
    t = np.linspace(0, duration, int(SAMPLE_RATE * duration), False)
    
    # 1. Sine (Cơ bản)
    osc1 = np.sin(freq * t * 2 * np.pi)
    
    # 2. Square (Dày)
    osc2 = np.sign(np.sin(freq * t * 2 * np.pi)) 
    
    # 3. Sawtooth (Sắc)
    osc3 = 2 * (t * freq - np.floor(t * freq + 0.5))

    mixed_wave = (0.5 * osc1) + (0.3 * osc2) + (0.2 * osc3)
    
    total_samples = len(t)
    attack_len = int(SAMPLE_RATE * 0.01)
    decay_len = int(SAMPLE_RATE * 0.1)
    release_len = int(SAMPLE_RATE * 0.1)
    sustain_len = total_samples - attack_len - decay_len - release_len

    if sustain_len < 0: sustain_len = 0 

    envelope = np.concatenate([
        np.linspace(0, 1, attack_len),
        np.linspace(1, 0.7, decay_len),
        np.full(sustain_len, 0.7),
        np.linspace(0.7, 0, release_len)
    ])

    envelope = envelope[:len(mixed_wave)]
    final_wave = mixed_wave * envelope
    
    max_val = np.max(np.abs(final_wave))
    if max_val > 0:
        final_wave = final_wave / max_val
        
    return np.int16(final_wave * BASE_AMPLITUDE)

# ==========================================
# EXTENDED SONG LIBRARY (CORRECTED KEYS)
# ==========================================
SONGS = {
    "Astronomia_CoffinDance": {
        "bpm": 132, "division": 2, # Đã sửa key 'div' thành 'division'
        "notes": [
            "F4", "F4", "F4", "F4", 
            "C5", "A#4", "A4", "G4", "F4", "C5", "A#4", "A4", "G4", "F4", 
            "C5", "A#4", "A4", "G4", "E4", "E4", "D4", "D4", "C4", "C4",
            "F4", "F4", "F4", "F4", "C5", "A#4", "A4", "G4", "F4", "C5", "A#4", "A4", "G4", "F4", 
            "C5", "A#4", "A4", "G4", "E4", "E4", "D4", "D4", "C4", "G4"
        ]
    },
    "Megalovania": {
        "bpm": 120, "division": 4,
        "notes": [
            "D4", "D4", "D5", "A4", "G#4", "G4", "F4", "D4", "F4", "G4",
            "C4", "C4", "D5", "A4", "G#4", "G4", "F4", "D4", "F4", "G4",
            "B3", "B3", "D5", "A4", "G#4", "G4", "F4", "D4", "F4", "G4",
            "A#3", "A#3", "D5", "A4", "G#4", "G4", "F4", "D4", "F4", "G4"
        ]
    },
    "Tetris_Korobeiniki": {
        "bpm": 140, "division": 2,
        "notes": [
            "E5", "B4", "C5", "D5", "C5", "B4", "A4", "A4", "C5", "E5", "D5", "C5", "B4", "B4", "C5", "D5", "E5",
            "C5", "A4", "A4", 
            "D5", "F5", "A5", "G5", "F5", "E5", "C5", "E5", "D5", "C5", "B4", "B4", "C5", "D5", "E5",
            "C5", "A4", "A4"
        ]
    },
    "Sandstorm": {
        "bpm": 136, "division": 4,
        "notes": [
            "B4", "B4", "B4", "B4", "B4", 
            "B4", "B4", "B4", "B4", "B4", "B4", "B4",
            "E4", "E4", "E4", "E4", "E4", "E4", "E4", 
            "D4", "D4", "D4", "D4", "D4", "D4", "D4", 
            "A4", "A4", "B4", "B4", "B4", "B4", "B4"
        ]
    },
    "Running_In_The_90s": {
        "bpm": 158, "division": 2,
        "notes": [
            "C#5", "C#5", "C#5", "B4", "A4", "G#4", "A4", "B4",
            "C#5", "C#5", "C#5", "B4", "A4", "G#4", "F#4", "F#4",
            "C#5", "C#5", "C#5", "B4", "A4", "G#4", "A4", "B4",
            "C#5", "B4", "C#5", "E5", "F#5", "E5", "F#5", "G#5"
        ]
    },
    "Blue_DaBaDee": {
        "bpm": 128, "division": 2,
        "notes": [
            "G4", "A#4", "G4", "D5", "C5", "C5", "G4", "F4", "G4", "A#4", 
            "G4", "C#5", "C5", "A#4", "G4", "F4", "G4", "A#4",
            "G4", "A#4", "G4", "D5", "C5", "C5", "G4", "F4", "G4", "A#4"
        ]
    },
    "Crab_Rave": {
        "bpm": 125, "division": 4,
        "notes": [
            "D4", "A#4", "G4", "G4", "D4", "D4", 
            "D4", "A#4", "G4", "G4", "D4", "D4", 
            "D4", "A#4", "G4", "G4", "D4", 
            "F4", "F4", "F4", "F4", "D#4", "D#4", "D#4", "D#4", 
            "D4", "A#4", "G4", "G4"
        ]
    },
    "WilliamTell_Finale": {
        "bpm": 150, "division": 2,
        "notes": [
            "B3", "B3", "B3", "B3", "B3", "B3", "G#4", "B4", "G#4", "B4", "G#4", 
            "E4", "D#4", "F#4", "B4", "F#4", "B4", "F#4", "D#4", 
            "B4", "G#4", "B4", "G#4", "E4", "G#4", "B4", 
            "E5", "B4", "G#4", "E4", "B4", "E4"
        ]
    },
    "NyanCat": {
        "bpm": 144, "division": 2,
        "notes": [
            "F#4", "G#4", "D#4", "D#4", "B3", "D4", "C#4", "B3", 
            "B3", "C#4", "D4", "D4", "C#4", "B3", "C#4", "D#4", "F#4", "G#4", "D#4", "F#4", "C#4", "D#4", "B3", "C#4", "B3",
            "D#5", "E5", "F#5", "C#6", "D#6", "E6", "D#6", "C#6",
            "C#6", "B5", "C#6", "D#6", "F#6", "C#6", "D#6", "C#6", "B5", "C#6", "D#6", "F#6"
        ]
    },
    "SuperMario_Theme": {
        "bpm": 100, "division": 2,
        "notes": [
            "E5", "E5", "E5", "C5", "E5", "G5", "G4",
            "C5", "G4", "E4", "A4", "B4", "A#4", "A4",
            "G4", "E5", "G5", "A5", "F5", "G5",
            "E5", "C5", "D5", "B4", "C5", "G4", "C5"
        ]
    }
}

# ==========================================
# MAIN PROCESSING
# ==========================================
def process_songs():
    output_base = "GeneratedMusic_Ultimate"
    if not os.path.exists(output_base):
        os.makedirs(output_base)
    
    print(f"{'='*40}")
    print(f"STARTING ULTIMATE MUSIC GENERATOR")
    print(f"{'='*40}")

    for song_name, data in SONGS.items():
        folder_path = os.path.join(output_base, song_name)
        if not os.path.exists(folder_path):
            os.makedirs(folder_path)

        sequence = data["notes"]
        unique_notes = sorted(list(set(sequence))) 
        
        print(f"Processing: {song_name:<25} | BPM: {data['bpm']} | Notes: {len(unique_notes)}")
        
        # 1. Generate Audio Files (.wav)
        for note in unique_notes:
            freq = note_to_freq(note)
            wave_data = generate_analog_synth_wave(freq, NOTE_DURATION_SEC)
            write(os.path.join(folder_path, f"{note}.wav"), SAMPLE_RATE, wave_data)

        # 2. Generate Sequence JSON
        sequence_data = {
            "songName": song_name,
            "bpm": data["bpm"],
            "division": data["division"],
            "clipSequence": [f"{note}.wav" for note in sequence]
        }
        
        json_path = os.path.join(folder_path, f"{song_name}_Sequence.json")
        with open(json_path, 'w') as f:
            json.dump(sequence_data, f, indent=4)
            
    print(f"{'='*40}")
    print(f"DONE! Files saved to '{output_base}' folder.")

if __name__ == "__main__":
    process_songs()