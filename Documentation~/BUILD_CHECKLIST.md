# Build Checklist - Mobile

Trước khi build release, check qua hết list này.

## Chung

- [ ] Bootstrap là scene index 0 trong Build Settings
- [ ] Tất cả scene cần dùng đã add vào Build Settings
- [ ] Bỏ `ENABLE_GAME_LOG` khỏi Scripting Define Symbols (Release)
- [ ] Version number tăng đúng (Player Settings → Bundle Version Code)
- [ ] Test trên device thật, không chỉ Editor

## Android

- [ ] Package Name đúng format `com.company.gamename`
- [ ] Minimum API Level: 23 (Android 6.0) - cover ~95% device
- [ ] Target API Level: lấy mới nhất Google Play yêu cầu
- [ ] Scripting Backend: IL2CPP (bắt buộc cho 64-bit)
- [ ] Target Architectures: ARMv7 + ARM64 (bỏ x86 trừ khi cần)
- [ ] Keystore production có backup an toàn (mất là không update app được)
- [ ] Build AAB (Android App Bundle), không phải APK, để upload Google Play
- [ ] Proguard/Minification: bật, test kỹ vì có thể strip nhầm code

## iOS

- [ ] Bundle Identifier khớp với cert + provisioning profile
- [ ] iOS Target: 12.0 trở lên
- [ ] Architecture: ARM64
- [ ] Camera/Mic/Photo usage description nếu app có request (Info.plist)
- [ ] Capability: Push, IAP, GameCenter nếu cần
- [ ] Build Xcode project, mở bằng Xcode mới nhất để Archive

## Performance check trước khi ship

- [ ] FPS ổn định 60 trên mid-range device (Galaxy A50, iPhone 8)
- [ ] Memory usage < 500MB trên Android low-end
- [ ] Startup time < 5s đến Main Menu
- [ ] Không có GC spike >5ms trong gameplay (check Profiler)
- [ ] Draw call < 100 trong scene gameplay (mobile)
- [ ] Battery drain check 15 phút gameplay, < 10% pin

## Quality settings

- [ ] Tạo profile riêng cho Mobile (Edit → Project Settings → Quality)
- [ ] Tắt shadows cho object không cần
- [ ] Anti-aliasing: tắt hoặc FXAA (MSAA quá nặng cho mobile)
- [ ] Texture quality: Full Res trên flagship, Half trên low-end (dynamic detect)

## Build size

- [ ] Texture compression: ASTC (Android) + ASTC (iOS), fallback ETC2
- [ ] Audio: Vorbis quality 70% cho music, ADPCM/PCM cho SFX ngắn
- [ ] Strip Engine Code: bật (Player Settings → Other Settings)
- [ ] Build size cuối < 150MB (Google Play yêu cầu < 200MB cho APK base)

## Post-build smoke test

- [ ] Install lên 2 device khác nhau (1 Android low-end, 1 iOS)
- [ ] Play từ đầu đến Game Over không crash
- [ ] Tắt app giữa chừng → mở lại, save vẫn còn
- [ ] Tắt mạng → app không crash, có thông báo phù hợp nếu cần
- [ ] Xoay màn hình (nếu support) → UI không vỡ
