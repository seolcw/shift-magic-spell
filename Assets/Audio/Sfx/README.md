# 효과음 (jsfxr)

모든 효과음은 [jsfxr](https://sfxr.me/)(sfxr의 JavaScript 버전)로 만들었다. 2026-11-20.

## 다시 만드는 법
- **링크로:** 아래 표의 permalink(`https://sfxr.me/#<코드>`)를 열면 같은 설정이 불러와진다. 오른쪽 `Sample Rate` 44k, `Sample size` 16bit로 맞춘 뒤 `.wav`를 받는다. 볼륨(`sound_vol`)은 0.4로 만들었다.
- **파라미터로:** 표의 "프리셋"은 출발점이고, 실제 소리는 "바꾼 값"만으로 정해진다. `new Params()`(모든 값 0, `p_lpf_freq` 1, 사각파)에 아래 값을 덮어쓴 것이다.
- Noise 파형(`wave_type: 3`, Thunder·Explosion)은 만들 때마다 잡음이 조금씩 달라진다. 소리 성격은 같다.

| 파일 | 프리셋 | 길이 | 쓰는 곳 |
| --- | --- | --- | --- |
| `Move.wav` | Blip/select | 0.05초 | 이동(아주 작게), 첫 화면 시작(크게) |
| `Rotate.wav` | Jump | 0.12초 | 줄 돌리기 |
| `Deny.wav` | Hit/hurt | 0.14초 | 막힌 동작(돌릴 수 없는 줄, 빈 슬롯에서 Space) |
| `Coin.wav` | Pickup/coin | 0.28초 | 단어 완성, 제자리 글자가 늘 때(작고 높게) |
| `Store.wav` | Powerup | 0.20초 | 블록이 슬롯으로 날아감 |
| `Laser.wav` | Laser/shoot | 0.20초 | BEAM·LASER, 불덩이·로켓·드래곤 발사(낮게) |
| `Thunder.wav` | Explosion | 0.50초 | BOLT·STORM |
| `Sparkle.wav` | Powerup | 0.41초 | 바람·별·얼음·칼날 마법, 콤보(높게) |
| `Finisher.wav` | Powerup | 1.38초 | 마무리 SHIFT |
| `Explosion.wav` | Explosion | 0.77초 | 마법이 손에 맞는 순간, 손 내려찍기(낮게), 블록 깨짐(높게) |
| `Hurt.wav` | Hit/hurt | 0.16초 | 플레이어 피격 |
| `Warn.wav` | Blip/select | 0.24초 | 손 공격 예고 |
| `Click.wav` | Click | 0.03초 | 손 클릭·더블클릭·슬롯 깨기, 줄 섞기 튕김 |
| `Warp.wav` | Jump | 0.28초 | 순간이동 |
| `Fanfare.wav` | Powerup | 1.25초 | 페이즈 클리어, 승리(낮고 느리게) |
| `Death.wav` | Hit/hurt | 0.93초 | 쓰러짐 |
| `Crack.wav` | Explosion | 0.15초 | 엔딩: 손에 금이 감(두 번째는 낮고 크게). 2026-11-25 |
| `Pop.wav` | Powerup | 0.20초 | 엔딩: 커서 모양이 펑 바뀜(모래시계 → I → 화살표로 갈수록 높게). 2026-11-25 |
| `Zip.wav` | Jump | 0.27초 | 엔딩: 작은 화살표가 화면 밖으로 달아남. 2026-11-25 |

## 바꾼 값

```json
{
  "Move":      {"wave_type":0,"p_duty":0.25,"p_base_freq":0.42,"p_env_sustain":0.08,"p_env_decay":0.12,"p_hpf_freq":0.1},
  "Rotate":    {"wave_type":0,"p_duty":0.35,"p_base_freq":0.3,"p_freq_ramp":0.2,"p_env_sustain":0.12,"p_env_decay":0.2,"p_hpf_freq":0.12},
  "Deny":      {"wave_type":1,"p_base_freq":0.14,"p_freq_ramp":-0.08,"p_env_sustain":0.2,"p_env_decay":0.15,"p_lpf_freq":0.55},
  "Coin":      {"wave_type":0,"p_duty":0.5,"p_base_freq":0.5,"p_env_sustain":0.05,"p_env_punch":0.45,"p_env_decay":0.35,"p_arp_speed":0.6,"p_arp_mod":0.35},
  "Store":     {"wave_type":0,"p_duty":0.4,"p_base_freq":0.3,"p_freq_ramp":0.25,"p_env_sustain":0.2,"p_env_decay":0.22},
  "Laser":     {"wave_type":0,"p_duty":0.3,"p_base_freq":0.75,"p_freq_limit":0.2,"p_freq_ramp":-0.25,"p_env_sustain":0.2,"p_env_punch":0.2,"p_env_decay":0.22,"p_hpf_freq":0.05},
  "Thunder":   {"wave_type":3,"p_base_freq":0.35,"p_freq_ramp":-0.2,"p_env_sustain":0.14,"p_env_punch":0.7,"p_env_decay":0.45,"p_repeat_speed":0.55,"p_pha_offset":0.1,"p_pha_ramp":-0.1},
  "Sparkle":   {"wave_type":0,"p_duty":0.3,"p_base_freq":0.45,"p_freq_ramp":0.1,"p_arp_speed":0.45,"p_arp_mod":0.5,"p_repeat_speed":0.55,"p_env_sustain":0.3,"p_env_decay":0.3},
  "Finisher":  {"wave_type":1,"p_base_freq":0.25,"p_freq_ramp":0.22,"p_vib_strength":0.3,"p_vib_speed":0.5,"p_repeat_speed":0.3,"p_env_sustain":0.5,"p_env_punch":0.4,"p_env_decay":0.6},
  "Explosion": {"wave_type":3,"p_base_freq":0.12,"p_freq_ramp":-0.05,"p_env_sustain":0.3,"p_env_punch":0.6,"p_env_decay":0.5,"p_pha_offset":0.2,"p_pha_ramp":-0.15},
  "Hurt":      {"wave_type":1,"p_base_freq":0.45,"p_freq_ramp":-0.45,"p_env_sustain":0.1,"p_env_decay":0.25,"p_hpf_freq":0.1},
  "Warn":      {"wave_type":0,"p_duty":0.5,"p_base_freq":0.55,"p_arp_mod":0.5,"p_arp_speed":0.5,"p_repeat_speed":0.3,"p_env_sustain":0.3,"p_env_decay":0.12},
  "Click":     {"wave_type":0,"p_duty":0.55,"p_base_freq":0.7,"p_freq_ramp":-0.38,"p_env_sustain":0.04,"p_env_decay":0.1,"p_hpf_freq":0.6},
  "Warp":      {"wave_type":0,"p_duty":0.2,"p_base_freq":0.25,"p_freq_ramp":0.3,"p_vib_strength":0.35,"p_vib_speed":0.6,"p_env_sustain":0.25,"p_env_decay":0.25},
  "Fanfare":   {"wave_type":0,"p_duty":0.4,"p_base_freq":0.3,"p_freq_ramp":0.12,"p_arp_mod":0.45,"p_arp_speed":0.6,"p_repeat_speed":0.45,"p_env_sustain":0.5,"p_env_punch":0.3,"p_env_decay":0.55},
  "Death":     {"wave_type":0,"p_duty":0.5,"p_base_freq":0.4,"p_freq_ramp":-0.22,"p_vib_strength":0.3,"p_vib_speed":0.45,"p_env_sustain":0.4,"p_env_decay":0.5,"p_lpf_freq":0.8},
  "Crack":     {"wave_type":3,"p_base_freq":0.55,"p_freq_ramp":-0.3,"p_env_sustain":0.06,"p_env_punch":0.8,"p_env_decay":0.25,"p_hpf_freq":0.2},
  "Pop":       {"wave_type":0,"p_duty":0.3,"p_base_freq":0.35,"p_freq_ramp":0.3,"p_env_sustain":0.1,"p_env_punch":0.5,"p_env_decay":0.28,"p_hpf_freq":0.05},
  "Zip":       {"wave_type":0,"p_duty":0.2,"p_base_freq":0.22,"p_freq_ramp":0.4,"p_env_attack":0.05,"p_env_sustain":0.2,"p_env_decay":0.28,"p_hpf_freq":0.1,"p_pha_offset":0.15,"p_pha_ramp":0.1}
}
```

## permalink

| 파일 | 링크 |
| --- | --- |
| Move | https://sfxr.me/#11111mqnbfG4xmX3bGAvKmeb9aXFDyV7uRApBMDk78WnyZ6PEZyAWPUZptyB6i4tX5A9xZugjrt5xT9Qx9pFyCSipGEyJNbWAfUMb4eJfphS7syGh2n3w67 |
| Rotate | https://sfxr.me/#11111BFZZ5cz8hjugYCusHkGzt2U7JyR1Hq217XWsR3S7MeaMfrbXYX7ykvXx4DHgKqMuRNtuZm9oPVz6BCbDBwLD3oM31vU4GywipBmVXPNVfx633sE6sMR |
| Deny | https://sfxr.me/#34T6PkyDRjVwX8wmZ5qCRwei85nup7p7LrUw57bju8XyFMb7vCc5BwfJDLd7GK78sbN78LcrmAYetZy4tigHeaqnVvhWBcFW2NrgGa3WNTng2oE7cyxjQTZJf |
| Coin | https://sfxr.me/#11111FbzaGZPjufySvbGvxY751wA4A2bDdKG7vGJ3UszWDCgEq9Pk22Qoi8ztqBEd4xsGcTATZDGde7tzPTHZicmNFcu5jpJEW3fMBCZGA34bPQoKevdcnxX |
| Store | https://sfxr.me/#11111FbzaGb4pRBtEg7tuuBrCBJr7iaK4PUbjSV8U8ymNGtnrqNisVQgKf7QhFkCkaWeBCiPtsDdGjA87cotSddK229A4TPqb1v5XytBFWfRJbk7r311711H |
| Laser | https://sfxr.me/#11111FbzaGdJFnZvxLL4UL9YQyuWDkc5ABhyYZqXqWkHQL8Y2LJQzQSjPRDAp5c7MreaowRWHDftTsyx6F9TYRYsg5MKWRqFTmn6kqmHjJjXHc1P5EziEJYX |
| Thunder | https://sfxr.me/#7BMHBGCiDfkiV1PTpRQLfVeDpAwkUY5VQSt3iXi81ozv83uoSFJNszC9wpMBU13tpRRQB9B1asiXq5RWWyBGceCGnLioMh5AUTMd5PjpkS8QnhR1sXHxr6XxT |
| Sparkle | https://sfxr.me/#11111BxFXqy8SqwaMLW3TsATVgWYvPEoZtNyoMsL1Ab1RwiKc4rv7qoKNgjfas18XTGf8Z6CFCP8UX9HNN15QAeFawvFJo7fqQiY1db1dpDJuoRBSdtQRthu |
| Finisher | https://sfxr.me/#34T6PkicSAHRS3ujv4jjX36w8p7TH9agaQvE9YhYibDBbLhnZwLPXJV6sUc33oXbEoDpwJoEEP5ux5bifnF34gs1GPCkuzrr7sqvbRCRD7PxCxFoqMmL2CVZh |
| Explosion | https://sfxr.me/#7BMHBGLnZ1Y4AUwBXUifcENGjFfP1GrdhNkEQrqQyg2gTj4LPzckeGwiQjswooyaT5ZQ5XWKsFLZBEQhTaB3b72vbzxBrFgxMakkNMVKgVNdG62xNMUVRmgbq |
| Hurt | https://sfxr.me/#34T6PkyDSCfwBcofhGkJ53Rs5UCSWXoDXBswg4mKop6WL889gTKh1smuGweAJWhf42PAB3tdtFguT6fa4BmvXkxkoe5YVCeoR5mo9NnDFBvUjZG8gqkPS4zLf |
| Warn | https://sfxr.me/#11111BxFXqy8SqwaLPPPFXreUnaCT3yGhv4DFen5w3kcj5hKPNJC1Ea6SwNgeryrSrZGgNJVAdQ9ANwHSwtV4DLEnUH58BrewsJyroXdPJHXqKHpMbs3BtsH |
| Click | https://sfxr.me/#11111mqKRconoRwXjTTz8z3yHzrR8nc8DVY2ddXhAkVck11cNrFWajUvkYGJZRv76nRBS2JhWvbDJye7RoxZWtHFRRRUsjGBfh2zy2XkdZN1sxaUaN6rszw |
| Warp | https://sfxr.me/#1111111UE2QmoXVJyEhkSLeDZLbNysccLBNxp4GejfWY6cHuZCKoF8SJjmxmMK6Zr572Cv3bZNZS7X8oToirVoXYaSduJsynyFBzK2D6jTfDf7d8EYcYskK |
| Fanfare | https://sfxr.me/#1111111144RNXH8ru8nnY3QExQjHvNjebkxccrzimCsv9avNrB3Zm2XRTYaA8LJxsR3KCzueBPNR1HDyy1KG1zeH6vX38MaNiSA1m8EJi3wP5ywFKokRGhd |
| Death | https://sfxr.me/#11111Fc13SdX6aXTZbZ5RDu1oyFkjq7n32tW4iToEvsW1nNbUtEzkGZtHbWKXFq9tC2oerxJQ41opZQX9J8yQRap8bE5xKiMQzHrb7uYwdiY62GSmsp38Swh |
| Crack | https://sfxr.me/#7BMHBGL5rZbg8FVXPWrK6dn22FxFcJDh7XygAWwHYoawnojcBqKVch2mqT13AYEwPgjBH2DA9inYAu4j8NFnTVBsn2tfhvCbR4U8GqX5F3aCDjMzwoGVUwdGj |
| Pop | https://sfxr.me/#11111Fc13SajJH6VKEJ4rJ7PBah2vdECuGo667iMjNzcmDorkGEmoKr4m7wzL1JEx2hgHfG6oioL9K4aJ8RWfgETVqpTwFLAM1VDNc4wtS8XzwTWQSHfX9zf |
| Zip | https://sfxr.me/#12eZRSDW9BMoj7LS8XKaH2VCTTfkK65fqwRsrfYjD4rN2MWGXwEK32E2xMg1FS2pfDhCMGffbMYGRuwGnBfWjiCuR2yLBdHmazj41beSfzhGMr9xMgVp1Qke9M |

- 볼륨·피치·피치 랜덤폭은 wav가 아니라 `Assets/Audio/SoundLibrary.asset`(인스펙터)에서 이벤트마다 정한다.
