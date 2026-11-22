# AGENTS.md

## โปรเจค: OpenKey 🔥

key remapper สำหรับ Windows — **คีย์ใดก็ได้เป็น trigger, กดคู่กับคีย์ใดก็ได้ แล้วได้คีย์ที่ต้องการ**
เขียนด้วย **C# (.NET 10) + WPF + WPF-UI (Fluent theme)**, แพ็กแบบ self-contained
(ผู้ใช้เอา `publish/` ไปทำ installer เอง) — ไม่ใช้ AutoHotkey

> ประวัติ: เดิมชื่อ CapsLock King (AutoHotkey) → Rust prototype (`feat/native-rust-v2`) → C#
> (`feat/csharp-v2`) → เปลี่ยนโมเดลเป็น key mapping ทั่วไปและเปลี่ยนชื่อเป็น OpenKey (`feat/open-key`)

## โครงสร้างไฟล์

| ไฟล์ | คำอธิบาย | commit? |
|------|----------|---------|
| `OpenKey.sln` | solution (app + tests) | ✅ |
| `src/OpenKey/` | แอป WPF | ✅ |
| `src/OpenKey/Core/Keys.cs` | ตารางชื่อปุ่ม ↔ scancode/VK + `ModMask` | ✅ |
| `src/OpenKey/Core/ConfigLoader.cs` | parse/validate/serialize + migration v1→v2 | ✅ |
| `src/OpenKey/Core/ConfigModel.cs` | โมเดล compiled: `Trigger`, `DirectBinding`, `CompiledConfig` | ✅ |
| `src/OpenKey/Core/HookEngine.cs` | state machine (pure) | ✅ |
| `src/OpenKey/Core/KeyboardHook.cs` | `WH_KEYBOARD_LL` + capture mode | ✅ |
| `src/OpenKey/Core/Sender.cs` | `SendInput` (มี guard ขนาด INPUT) | ✅ |
| `src/OpenKey/Core/AppHost.cs` | config/hook/tray/เปิดหน้าต่าง | ✅ |
| `src/OpenKey/Core/TrayIcon.cs` | tray + context menu (WinForms NotifyIcon) | ✅ |
| `src/OpenKey/Core/Log.cs` | log ไฟล์ (`--debug` → `openkey.log`) | ✅ |
| `src/OpenKey/Views/MainWindow.xaml(.cs)` | หน้าตั้งค่า (triggers + direct mappings) | ✅ |
| `tests/OpenKey.Tests/` | xUnit — ต้องผ่านทั้งหมดก่อน commit | ✅ |
| `config.json` | mapping เริ่มต้น (v2) = source of truth | ✅ |
| `OpenKey.ico` | ไอคอน (ApplicationIcon) | ✅ |
| `scripts/publish.ps1` | publish self-contained ให้ installer | ✅ |
| `tools/generate-icon.ps1` | สร้างไอคอนจากโค้ด | ✅ |
| `legacy/` | เวอร์ชัน AutoHotkey เดิม — **ห้ามแก้/ห้ามลบ** | ✅ |
| `publish/`, `bin/`, `obj/` | ของที่ build ออกมา — gitignore | ❌ |

## โมเดล (v2) — ต้องเข้าใจก่อนแก้

```json
{
  "version": 2,
  "settings": { "tapTimeoutMs": 0 },
  "triggers": { "caps_lock": { "tap": "escape", "map": { "h": "left" } } },
  "bindings": [ { "from": "alt", "to": "win" } ]
}
```

- **trigger** = คีย์ใดก็ได้ (ห้ามเป็น modifier จริง) กดค้างแล้วเปิด layer ของตัวเอง
- **tap** = แตะเดี่ยว → action (ปกติใส่ชื่อคีย์เดิม เพื่อให้พิมพ์ได้)
- **map** = `from (+modifier)` → `to` ภายใน layer ของ trigger
- **bindings** = remap ตรง ๆ (รองรับ modifier ที่ `from` และ modifier เดี่ยวเป็นต้นทาง)
- input ใช้ **scancode** (ห้ามเปลี่ยนไปใช้ VK), output ใช้ VK + extended flag

## Engine semantics

| สถานการณ์ | ผล |
|---|---|
| กด trigger | กลืน, เริ่มจับเวลา |
| trigger + ปุ่มที่ map (บนสุดของ stack ก่อน) | ส่ง action, กลืน down/up |
| trigger + ปุ่มไม่ map | ส่ง tap ของ trigger บนสุด (ครั้งเดียว) แล้วปล่อยปุ่มผ่าน |
| ปล่อย trigger ที่ยังไม่ถูกใช้ | ส่ง tap (ถ้า `tapTimeoutMs` = 0 หรือยังไม่เกิน) |
| trigger ซ้อน trigger | stack (ตัวล่าสุดชนะ) |
| ไม่มี trigger | ดู `bindings` → ไม่เจอ = ผ่าน |

- map ที่ระบุ modifier มากกว่าชนะ (`shift+h` ชนะ `h`) — compile เรียงลำดับไว้แล้ว
- `from` ที่ไม่ระบุ modifier = match ได้ทุกกรณี (modifier ที่ค้างจะ passthrough ไปกับ output)

## สถาปัตยกรรม

- UI thread เดียว: hook + tray + หน้าตั้งค่า (WPF dispatcher เป็น message pump)
- `HookEngine` pure: `OnEvent(vk, scan, down, mods, nowMs)` → `FilterResult`
- `KeyboardHook` กรอง `LLKHF_INJECTED` ทิ้ง (กัน SendInput วนกลับ)
- capture mode: ปล่อยคีย์ดิบ แต่กลืน CapsLock และเก็บ scancode ไว้ให้ UI อ่าน (`TakeCaptured`)
- single instance: named Mutex + EventWaitHandle → ตัวที่สองสั่งเปิด Settings
- save flow: validate → `.bak` → `.tmp` → `File.Move(overwrite)` → `AppHost.ReloadFromDisk`

## กฎการแก้ code

1. `config.json` = source of truth — mapping ห้าม hardcode (default ฝังผ่าน `EmbeddedResource` ชื่อ `OpenKey.config.json`)
2. เพิ่ม/แก้ชื่อปุ่ม → `Core/Keys.cs` ตารางเดียว
3. เปลี่ยนพฤติกรรม engine → ต้องมี test ใน `tests/` และ `dotnet test` ผ่านทั้งหมด
4. ข้อความใน config/ชื่อปุ่มเป็น ASCII; UI/log เป็นไทยได้
5. หลังแก้: `dotnet test` → build release → (`scripts/publish.ps1` ถ้าจะส่งมอบ)
6. ห้ามแตะ `legacy/` (ยกเว้นผู้ใช้สั่ง)
7. หลีกเลี่ยง NuGet package ใหม่ (ตอนนี้มีแค่ `WPF-UI`)

## ข้อจำกัดที่ตั้งใจ

- ไม่ใช้ไดรเวอร์ → per-device remap / เกม anti-cheat ทำไม่ได้
- ไม่มี installer ในโปรเจค (ผู้ใช้ทำเอง) — autostart = shortcut ใน `shell:startup`
- รันระดับ user → แอป Administrator ต้อง Run as administrator
- ตัวอักษรเป็น trigger ได้ แต่ tap จะถูกส่งตอนปล่อยปุ่ม (มี `tapTimeoutMs` ช่วย)
- ไม่มี per-app / chord / sequence (ยังไม่ทำ)

## คำสั่งที่ใช้บ่อย

```powershell
dotnet test
dotnet build src\OpenKey -c Release
dotnet run --project src\OpenKey -- --debug --settings

# เตรียมไฟล์ให้ installer
powershell -ExecutionPolicy Bypass -File scripts\publish.ps1
powershell -ExecutionPolicy Bypass -File scripts\publish.ps1 -SingleFile

Get-Content .\publish\openkey.log
```

## Checklist ทดสอบก่อน commit (manual)

- [ ] CapsLock แตะเดี่ยว → `Esc`
- [ ] `CapsLock + h/j/k/l` → ลูกศร (กดค้างแล้ว repeat)
- [ ] `CapsLock + c/v`, `CapsLock + .` บน layout ไทย
- [ ] `Shift + CapsLock + l` → เลือกข้อความ
- [ ] เพิ่ม trigger ใหม่ (เช่น `space`) → `space + j` → ↓ และพิมพ์ space ได้ปกติ
- [ ] Direct mapping `alt → win`
- [ ] แก้ config ให้พัง → Reload → ยังใช้ของเดิม + แจ้ง error
- [ ] ปิดหน้าตั้งค่าแล้ว remap ยังทำงาน; Exit จาก tray แล้วโปรเซสจบ

## งานที่ค้าง

- [ ] หน้า Settings: ค้นหา/filter
- [ ] ตัวเลือก "เปิดพร้อม Windows" (เขียน `HKCU\...\Run`)
- [ ] per-app profile / chord / sequence (ยังไม่เริ่ม)
- [ ] แก้ shortcut ใน Startup ของเครื่องผู้ใช้ให้ชี้ `OpenKey.exe`
