# 🔥 CapsLock King v2 (C# / WinUI-style WPF)

> เปลี่ยนปุ่ม CapsLock ให้เป็น **Super Key** สำหรับ Vim-style navigation บน Windows
> — หน้าจอตั้งค่าสวย ๆ (Fluent theme), mapping เก็บใน `config.json`, ไม่ต้องใช้ AutoHotkey

- **C# / .NET 10 + WPF (WPF-UI Fluent theme)**
- **Self-contained** — ไม่ต้องติดตั้ง .NET runtime (ผู้ใช้เอาไปแพ็กเป็น installer ได้)
- **Keyboard hook ระดับ low-level** (`WH_KEYBOARD_LL`) เหมือนเดิม แต่เป็นโค้ดของเราเอง
- **ไม่พึ่ง layout** — จับด้วย scancode (ตำแหน่งปุ่ม) ใช้กับคีย์บอร์ดไทย/อังกฤษได้เหมือนกัน

```
publish\
├─ CapsLockKing.exe   ← ดับเบิลคลิกแล้วใช้ได้ (มี tray icon)
├─ config.json        ← mapping ทั้งหมด (แก้มือหรือแก้ผ่าน GUI ก็ได้)
└─ ... DLL ของ .NET (self-contained)
```

---

## ✨ คุณสมบัติ

- **CapsLock เดี่ยว** → `Esc`
- **Vim navigation** — `h/j/k/l` = ซ้าย/ล่าง/บน/ขวา (กดค้าง = auto-repeat)
- **Clipboard / ไฟล์ / Function keys** — ครบเหมือนเวอร์ชันเดิม
- **หน้าตั้งค่า (Settings…)** — เพิ่ม/ลบ/แก้ mapping, **กดปุ่มจริงเพื่อจับคีย์**, ตรวจ config ก่อนบันทึก
- **Reload ได้ทันที** — ไม่ต้องปิดโปรแกรม (tray → Reload config)
- **ภาษาไทยในหน้าจอ** — ใช้ฟอนต์ระบบ + font fallback ของ WPF/DirectWrite

## 🎹 Mapping เริ่มต้น

| คีย์ | ฟังก์ชัน |
|------|----------|
| `CapsLock` (กดเดี่ยว) | `Esc` |
| `CapsLock + h/j/k/l` | ← / ↓ / ↑ / → |
| `CapsLock + c/v/x/z/y` | Copy / Paste / Cut / Undo / Redo |
| `CapsLock + s/a/f/w` | Save / Select All / Find / Close Tab |
| `CapsLock + .` / `,` | Delete / Backspace |
| `CapsLock + r/g/d/e/t` | F5 / F9 / F10 / F11 / F12 |
| `CapsLock + q` | ปิดแอป (`Alt+F4`) |
| `CapsLock + u` / `i` | สลับ Virtual Desktop ซ้าย / ขวา |

### 🔀 ทริค: กดคู่กับ Shift / Ctrl / Alt

Modifier ที่กดค้างอยู่จะถูกส่งผ่านไปด้วยอัตโนมัติ:

| ทริค | ผลลัพธ์ |
|------|----------|
| `CapsLock + Shift + h/j/k/l` | เลือกข้อความ (เหมือน `Shift` + ลูกศร) |
| `CapsLock + Ctrl + h/l` | กระโดดทีละคำ |
| `CapsLock + Ctrl + Shift + l` | เลือกข้อความทีละคำ |
| `CapsLock + Alt + h/l` | ย้อนกลับ / ไปข้างหน้า |

---

## 🚀 วิธีใช้

1. รัน `CapsLockKing.exe` → ไอคอนมงกุฎ 👑 ขึ้นใน system tray
2. คลิกขวาที่ tray icon:

| เมนู | ทำอะไร |
|------|--------|
| **Settings…** | เปิดหน้าตั้งค่า (แก้ mapping ด้วย GUI) |
| **Reload config** | อ่าน `config.json` ใหม่ทันที |
| **Open config.json** | เปิดไฟล์ด้วย editor ที่ตั้งไว้ |
| **Open config folder** | เปิดโฟลเดอร์ที่มี exe/config |
| **Exit** | ปิดโปรแกรม |

> รัน exe ซ้ำขณะโปรแกรมทำงานอยู่ = เปิดหน้าตั้งค่าของตัวที่รันอยู่ (ไม่รันซ้อน)

### ⚙️ เปิดพร้อม Windows

1. `Win + R` → `shell:startup` → Enter
2. สร้าง shortcut ของ `CapsLockKing.exe` วางในโฟลเดอร์ Startup

> ถ้าเคยตั้ง Startup ไว้กับเวอร์ชัน AutoHotkey ให้ลบ shortcut เก่าก่อน

---

## 🖥️ หน้าตั้งค่า (Settings)

- **การ์ด "ปุ่ม CapsLock"** — กำหนด action เมื่อแตะ CapsLock เดี่ยว + `tapTimeoutMs`
- **การ์ด "คีย์ลัด"** — ตาราง mapping: กดปุ่มซ้ายเพื่อจับคีย์จริง, พิมพ์ action เองได้, ปุ่ม `จับ combo` สำหรับชุดคีย์
- ปุ่ม **＋ เพิ่ม mapping** — เพิ่มแถวแล้วเข้าสู่โหมดจับคีย์ทันที (กดปุ่ม → กด combo ต่อ)
- **บันทึก & ใช้** — validate ก่อน, สำรองของเดิมเป็น `config.json.bak`, เขียนแบบ atomic, reload ทันที
- **ค่าเริ่มต้น** — เติม mapping มาตรฐานกลับมา (ยังไม่เขียนไฟล์จนกว่าจะกดบันทึก)

หน้าตั้งค่าเป็นเพียง editor ของ `config.json` — ไม่มีค่าที่ซ่อนอยู่ที่อื่น

---

## 📝 รูปแบบ `config.json`

```json
{
  "version": 1,
  "tapTimeoutMs": 0,
  "remap": {
    "caps_lock": {
      "tap": "escape",
      "hold": {
        "h": "left",
        "c": "ctrl+c",
        "u": "win+ctrl+left"
      }
    }
  }
}
```

| ฟิลด์ | ความหมาย |
|-------|----------|
| `version` | ต้องเป็น `1` |
| `tapTimeoutMs` | `0` = กด CapsLock เดี่ยวแล้วได้ `tap` ทุกครั้ง / เช่น `250` = ต้องปล่อยภายใน 250 ms |
| `remap.caps_lock.tap` | สิ่งที่ส่งเมื่อแตะ CapsLock เดี่ยว (ไม่ใส่ = ไม่ส่งอะไร) |
| `remap.caps_lock.hold.<ปุ่ม>` | สิ่งที่ส่งเมื่อ `CapsLock + ปุ่ม` |

**รูปแบบ action**: `<ปุ่ม>` หรือ `ctrl+shift+left` (modifier: `ctrl`, `shift`, `alt`, `win`)

**ชื่อปุ่ม**: `a`–`z`, `0`–`9`, `escape`, `tab`, `backspace`, `enter`, `space`, `caps_lock`,
`left/right/up/down`, `home/end/page_up/page_down`, `insert/delete`, `f1`–`f12`,
`- = [ ] ; ' \` \ , . /`, `print_screen`, `scroll_lock`
(+ ชื่อพ้อง `esc`, `return`, `control`, `windows`, `pgup`, `pgdn`, `del`, `ins`)

> แก้ผิดแล้ว Reload: โปรแกรมจะ **ไม่ตาย** — ยังใช้ mapping เดิม พร้อมบอกบรรทัด/ชื่อคีย์ที่ผิด

---

## 🧪 Debug / log

```powershell
.\CapsLockKing.exe --debug                # เขียน log ที่ capslockking.log ข้าง exe
.\CapsLockKing.exe --config D:\my.json    # ใช้ config คนละไฟล์
.\CapsLockKing.exe --settings             # เปิดหน้าตั้งค่าทันที
```

---

## 🩺 Troubleshooting

**ปุ่มไม่ทำงานในบางแอป** — แอปที่รันแบบ Administrator จะไม่รับคีย์จากโปรแกรมระดับ user
(คลิกขวา → **Run as administrator**)

**เกมบางเกมไม่รับคีย์** — ข้อจำกัดของ Windows (raw input / anti-cheat) ไม่ใช่บั๊ก
และโปรแกรมนี้ไม่รองรับ per-device remap (ต้องใช้ไดรเวอร์ระดับ kernel)

**LED CapsLock ไม่ติด** — ตั้งใจ: ปุ่ม CapsLock ถูกกลืนทั้งหมด ต้องการพิมพ์ใหญ่ใช้ `Shift`

---

## 🔨 Build & Test

ต้องมี .NET 10 SDK (ไม่ต้องมี Visual Studio)

```powershell
dotnet test                                        # unit test (43 เคส)
dotnet build src\CapsLockKing -c Release           # build ปกติ
powershell -ExecutionPolicy Bypass -File scripts\publish.ps1   # เตรียมไฟล์ให้ installer
```

- `scripts\publish.ps1` → `publish\` แบบ self-contained (~160 MB / 255 ไฟล์, ไม่ต้องลง runtime)
- ใส่ `-SingleFile` ถ้าอยากได้ exe เดี่ยว (แตกไฟล์ตอนรันครั้งแรก, startup ช้าขึ้น)
- ไอคอนมาจาก `CapsLockKing.ico` (สร้างใหม่ได้ด้วย `tools\generate-icon.ps1`)

## 📁 โครงสร้างโปรเจค

| ไฟล์ | คำอธิบาย |
|------|----------|
| `src/CapsLockKing/App.xaml(.cs)` | entry point, single instance, tray, เปิดหน้าตั้งค่า |
| `src/CapsLockKing/Core/Keys.cs` | ตารางชื่อปุ่ม ↔ scancode/VK |
| `src/CapsLockKing/Core/ConfigLoader.cs` | อ่าน/validate `config.json` + default ที่ฝังใน exe |
| `src/CapsLockKing/Core/ConfigWriter.cs` | สร้าง JSON จากหน้าตั้งค่า (atomic save อยู่ที่ MainWindow) |
| `src/CapsLockKing/Core/HookEngine.cs` | state machine tap/hold/passthrough (pure, มี unit test) |
| `src/CapsLockKing/Core/KeyboardHook.cs` | `WH_KEYBOARD_LL` + โหมดจับคีย์ |
| `src/CapsLockKing/Core/Sender.cs` | `SendInput` แบบ batch |
| `src/CapsLockKing/Views/MainWindow.xaml(.cs)` | หน้าตั้งค่า (Fluent UI) |
| `tests/CapsLockKing.Tests/` | xUnit tests |
| `legacy/` | เวอร์ชัน AutoHotkey เดิม (เก็บไว้เป็นทางถอย) |

---

## 📌 หมายเหตุ

- รองรับ Windows 10/11 (Virtual Desktop ต้อง Windows 10 ขึ้นไป)
- ถ้าจะทำ installer: เอาโฟลเดอร์ `publish\` ไปแพ็ก (Inno Setup/WiX) — อย่าลืม `config.json`
- ถ้าเจอบั๊กหรืออยากเพิ่มฟีเจอร์ ส่ง Pull Request หรือเปิด Issue ได้เลย 👑
