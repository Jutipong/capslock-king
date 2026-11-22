# AGENTS.md

## โปรเจค: CapsLock King 🔥 (v2 — C# / WPF)

โปรแกรม Windows ที่เปลี่ยน **CapsLock** ให้เป็น **Super Key** (Vim-style navigation)
เขียนด้วย **C# (.NET 10) + WPF + WPF-UI (Fluent theme)**
แพ็กแบบ **self-contained** (ผู้ใช้เอา `publish/` ไปทำ installer เอง) — ไม่ใช้ AutoHotkey แล้ว

## โครงสร้างไฟล์

| ไฟล์ | คำอธิบาย | commit? |
|------|----------|---------|
| `CapsLockKing.sln` | solution (app + tests) | ✅ |
| `src/CapsLockKing/` | แอป WPF | ✅ |
| `src/CapsLockKing/Core/Keys.cs` | ตารางชื่อปุ่ม ↔ scancode/VK | ✅ |
| `src/CapsLockKing/Core/ConfigLoader.cs` | อ่าน/validate config + default (ฝังจาก `config.json`) | ✅ |
| `src/CapsLockKing/Core/ConfigWriter.cs` | สร้าง JSON จากหน้าตั้งค่า | ✅ |
| `src/CapsLockKing/Core/HookEngine.cs` | state machine tap/hold (pure, unit test) | ✅ |
| `src/CapsLockKing/Core/KeyboardHook.cs` | `WH_KEYBOARD_LL` + capture mode | ✅ |
| `src/CapsLockKing/Core/Sender.cs` | `SendInput` | ✅ |
| `src/CapsLockKing/Core/AppHost.cs` | config/hook/tray/เปิดหน้าต่าง | ✅ |
| `src/CapsLockKing/Core/TrayIcon.cs` | tray + context menu (WinForms NotifyIcon) | ✅ |
| `src/CapsLockKing/Core/Log.cs` | log ไฟล์ (`--debug`) | ✅ |
| `src/CapsLockKing/Views/MainWindow.xaml(.cs)` | หน้าตั้งค่า | ✅ |
| `tests/CapsLockKing.Tests/` | xUnit (ต้องผ่านทั้งหมดก่อน commit) | ✅ |
| `config.json` | mapping เริ่มต้น = source of truth | ✅ |
| `CapsLockKing.ico` | ไอคอน (ใช้เป็น ApplicationIcon) | ✅ |
| `scripts/publish.ps1` | publish self-contained ให้ installer | ✅ |
| `tools/generate-icon.ps1` | สร้างไอคอนจากโค้ด | ✅ |
| `legacy/` | เวอร์ชัน AutoHotkey เดิม — **ห้ามแก้/ห้ามลบ** | ✅ |
| `publish/`, `bin/`, `obj/` | ของที่ build ออกมา — gitignore | ❌ |

## สถาปัตยกรรม (ต้องรู้ก่อนแก้)

- **UI thread เดียว** (WPF): keyboard hook ติดตั้งบน UI thread (มี message pump อยู่แล้ว) และ tray (WinForms `NotifyIcon`) ก็สร้างบน thread เดียวกัน
- **ห้าม block UI thread** งานหนักให้ย้ายไป `Task.Run`
- `HookEngine` ต้องเป็น **pure logic** — รับ `(vk, scan, down, nowMs)` คืน `FilterResult` เพื่อเขียน unit testได้ อย่าใส่ P/Invoke หรือ WPF ลงไป
- `KeyboardHook` กรอง `LLKHF_INJECTED` ทิ้งเสมอ (กัน `SendInput` ของตัวเองวนกลับเข้า hook)
- **โหมดจับคีย์ (capture)**: `KeyboardHook.SetCapture(true)` → hook ปล่อยคีย์ดิบ แต่ **กลืน CapsLock** ไว้ (กัน LED/toggle เพี้ยน) และเก็บ scancode ลง slot ให้ UI อ่านผ่าน `TakeCaptured()`
- Modifier ที่ผู้ใช้กดค้าง **ไม่ต้องส่งซ้ำ** — ระบบยังเห็นว่ากดค้างอยู่แล้ว (นี่คือเหตุผลที่ passthrough ทำงานได้เหมือน `{Blind}` ของ AHK)
- Single instance: named `Mutex` + named `EventWaitHandle` (ตัวที่สอง `Set()` → ตัวแรกเปิดหน้าตั้งค่า)
- config save: validate → copy `.bak` → เขียน `.tmp` → `File.Move(overwrite)` → `AppHost.ReloadFromDisk`
- UI: `App.xaml` มี theme (WPF-UI Dark) + style `CardStyle`/`SectionTitleStyle`/`HintStyle`; `DataTemplate` ของแถวอยู่ใน `MainWindow.xaml` (เพราะมี event handler)

## กฎการแก้ code

1. `config.json` = source of truth เดียว — mapping ห้าม hardcode ในโค้ด (default ฝังมาจากไฟล์เดียวกันผ่าน `EmbeddedResource`)
2. เพิ่ม/แก้ชื่อปุ่ม → แก้ `Core/Keys.cs` **ตารางเดียว**
3. ฝั่ง input จับด้วย **scancode** (ห้ามเปลี่ยนไปใช้ VK — จะพังบนคีย์บอร์ดไทย)
4. เปลี่ยนพฤติกรรม tap/hold → ต้องเพิ่ม/แก้ test ใน `tests/` และ `dotnet test` ต้องผ่านทั้งหมด
5. ข้อความใน `config.json` และชื่อปุ่มต้องเป็น ASCII; ข้อความ UI/log เป็นไทยได้
6. หลังแก้โค้ด: `dotnet test` → `dotnet build -c Release` → (`scripts/publish.ps1` ถ้าจะส่งมอบ)
7. ห้ามแตะ `legacy/` (ยกเว้นผู้ใช้สั่งชัดเจน)
8. อย่าเพิ่ม NuGet package ใหม่โดยไม่จำเป็น (ตอนนี้มีแค่ `WPF-UI`)

## ข้อจำกัดที่ตั้งใจ (อย่าไป "แก้" โดยไม่ถาม)

- ไม่ใช้ไดรเวอร์ → per-device remap / เกม anti-cheat ทำไม่ได้
- ไม่มี installer ในโปรเจค (ผู้ใช้ทำเอง) — autostart ใช้ shortcut ใน `shell:startup`
- รันระดับ user → แอป Administrator ต้อง Run as administrator เอง
- `tapTimeoutMs: 0` = พฤติกรรมเดิมของ AHK (ได้ `Esc` ทุกครั้งที่แตะ)
- WinUI 3 ถูกประเมินแล้วว่า **build ไม่ได้บนเครื่องนี้** (ไม่มี VS/Windows SDK → PRI task หาย) จึงใช้ WPF + WPF-UI แทน

## คำสั่งที่ใช้บ่อย

```powershell
dotnet test                                    # unit test ทั้งหมด
dotnet build src\CapsLockKing -c Release       # build
dotnet run --project src\CapsLockKing -- --debug --settings

# เตรียมไฟล์ให้ installer (self-contained)
powershell -ExecutionPolicy Bypass -File scripts\publish.ps1
# exe เดี่ยว (แตกตอนรัน, startup ช้า)
powershell -ExecutionPolicy Bypass -File scripts\publish.ps1 -SingleFile

# log
Get-Content .\src\CapsLockKing\bin\Release\net10.0-windows\capslockking.log
```

## Checklist ทดสอบก่อน commit (manual)

- [ ] CapsLock เดี่ยว → `Esc`
- [ ] `CapsLock + h/j/k/l` → ลูกศร (กดค้างแล้ว repeat)
- [ ] `CapsLock + c/v` → copy/paste; `CapsLock + .` → Delete บน layout ไทย
- [ ] `Shift + CapsLock + l` → เลือกข้อความ
- [ ] LED CapsLock ไม่ติด
- [ ] tray → Settings… เปิดหน้าตั้งค่า; กด "กดปุ่ม…" แล้วกดปุ่มจริง → ชื่อปุ่มขึ้น
- [ ] ＋ เพิ่ม mapping → overlay จับคีย์เด้ง → กดปุ่ม → กด combo → บันทึกแล้วใช้ได้ทันที
- [ ] แก้ `config.json` ให้พัง → Reload → ยังทำงานด้วยของเดิม + แจ้ง error
- [ ] ปิดหน้าตั้งค่าแล้ว remap ยังทำงาน; Exit จาก tray แล้วโปรเซสจบ

## งานที่ค้าง

- [ ] หน้า Settings: ช่องค้นหา/filter mapping (ตอนนี้ 23 แถวเลื่อนดูได้)
- [ ] ตัวเลือก "เปิดพร้อม Windows" (เขียน `HKCU\...\Run`)
- [ ] พิจารณา per-app profile (ต้องดึงชื่อ process ของหน้าต่าง aktif)
