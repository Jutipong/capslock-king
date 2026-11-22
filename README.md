# 🔥 OpenKey

> key remapper สำหรับ Windows — **คีย์อะไรก็ได้ + คีย์อะไรก็ได้ → คีย์ที่ต้องการ**
> หน้าจอตั้งค่าภาษาไทย, mapping เก็บใน `config.json`, ไม่ต้องใช้ AutoHotkey

- **C# / .NET 10 + WPF (Fluent theme)**
- **Self-contained** — ไม่ต้องลง .NET runtime (แพ็กเป็น installer ได้ด้วย `publish\`)
- keyboard hook ระดับ low-level (`WH_KEYBOARD_LL`) — จับด้วย **scancode** (ตำแหน่งปุ่ม) จึงใช้กับ layout ไทย/อังกฤษได้เหมือนกัน

```
publish\
├─ OpenKey.exe     ← ดับเบิลคลิกแล้วใช้ได้ (มี tray icon)
├─ config.json     ← mapping ทั้งหมด
└─ ... DLL ของ .NET (self-contained)
```

---

## ✨ แนวคิด

ทุกคีย์ทำได้ 3 อย่าง (จะทำอย่างไหนก็ได้):

| บทบาท | ความหมาย | ตัวอย่าง |
|---|---|---|
| **trigger** | กดค้างแล้วเปลี่ยนความหมายของคีย์อื่น | `caps_lock + h` → ←, `space + j` → ↓, `; + a` → `ctrl+a` |
| **tap** | แตะเดี่ยวแล้วได้คีย์นี้ | `space` → space (พิมพ์ปกติ), `caps_lock` → `Escape` |
| **direct** | map ตรง ๆ ไม่ต้องกดค้าง | `alt` → `win`, `f1` → `ctrl+shift+p` |

CapsLock ไม่ใช่กรณีพิเศษ — เป็นแค่ trigger ตัวหนึ่งใน `config.json` (mapping เริ่มต้นให้มาแบบเดิม)

## 🚀 วิธีใช้

1. รัน `OpenKey.exe` → ไอคอนใน system tray
2. คลิกขวาที่ tray: **Settings…** / Reload config / Open config.json / Open config folder / Exit
3. ในหน้าตั้งค่า:
   - **Triggers** (ซ้าย) — เพิ่มด้วยการกดคีย์จริง, เลือกแล้วแก้ตารางทางขวา
   - **แตะเดี่ยว →** — ตั้ง action เมื่อแตะ trigger เดี่ยว (ปกติ = ตัวมันเอง เพื่อให้พิมพ์ได้)
   - **ตารางคีย์ลัด** — กดปุ่มซ้าย = กดคีย์จริง, `จับ combo` = กดชุดคีย์, หรือพิมพ์เองก็ได้
   - **Direct mappings** — map ตรง ๆ ไม่ต้องมี trigger
   - **tapTimeout** — `0` = แตะแล้วได้ tap ทุกครั้ง / `>0` = ต้องปล่อยภายในเวลานี้ (กันการพิมพ์เร็ว)
   - **บันทึก & ใช้** — validate, สำรอง `.bak`, เขียนแบบ atomic, reload ทันที

> ดับเบิลคลิก exe ขณะโปรแกรมรันอยู่ = เปิดหน้าตั้งค่าของตัวที่รันอยู่

### ⚙️ เปิดพร้อม Windows

`Win + R` → `shell:startup` → วาง shortcut ของ `OpenKey.exe`

> ถ้าเคยตั้ง Startup ไว้กับ AutoHotkey/CapsLock King เวอร์ชันเก่า ให้ลบ shortcut เก่าก่อน

---

## 📝 รูปแบบ `config.json`

```json
{
  "version": 2,
  "settings": { "tapTimeoutMs": 0 },
  "triggers": {
    "caps_lock": {
      "tap": "escape",
      "map": {
        "h": "left",
        "c": "ctrl+c",
        "shift+h": "home"
      }
    },
    "space": {
      "tap": "space",
      "map": { "j": "down", "k": "up" }
    }
  },
  "bindings": [
    { "from": "alt", "to": "win" },
    { "from": "ctrl+alt+p", "to": "print_screen" }
  ]
}
```

| ฟิลด์ | ความหมาย |
|-------|----------|
| `settings.tapTimeoutMs` | `0` = แตะแล้วได้ tap ทุกครั้ง / `>0` = ต้องปล่อยภายใน N ms ไม่งั้นถือว่ากดค้าง |
| `triggers.<คีย์>.tap` | action เมื่อแตะ trigger เดี่ยว (ไม่ใส่ = ไม่ส่งอะไร) |
| `triggers.<คีย์>.map` | `ปุ่ม (+ modifier)` → `action` เมื่อ trigger ถูกกดค้าง |
| `bindings[].from` | คีย์ต้นทาง (ใส่ modifier ได้, เป็น modifier เดี่ยวก็ได้) |
| `bindings[].to` | คีย์/ชุดคีย์ที่จะส่งออก |

**รูปแบบค่า**: `<ปุ่ม>` หรือ `modifier+modifier+...+ปุ่ม` (modifier: `ctrl`, `shift`, `alt`, `win`)
ตัวอย่าง: `left`, `ctrl+c`, `shift+home`, `win+ctrl+right`, `alt`

**ชื่อปุ่ม**: `a`–`z`, `0`–`9`, `escape`, `tab`, `backspace`, `enter`, `space`, `caps_lock`,
`left/right/up/down`, `home/end/page_up/page_down`, `insert/delete`, `f1`–`f12`,
`- = [ ] ; ' \` \ , . /`, `print_screen`, `scroll_lock`
(+ ชื่อพ้อง `esc`, `return`, `control`, `windows`, `pgup`, `pgdn`, `del`, `ins`)

### กติกาการ match

- `map` ที่ระบุ modifier ชัดเจนกว่า (เช่น `shift+h`) ชนะ `h` เมื่อกด Shift
- `h` (ไม่ระบุ modifier) ยังทำงานแม้กด modifier อื่นค้าง — modifier ที่ค้างจะติดไปกับคีย์ที่ส่งออกด้วย
- trigger ซ้อน trigger ได้ (ตัวล่าสุดชนะ) — เช่น `caps_lock` + `space` + `j`
- คีย์ที่เป็น trigger จะส่ง `tap` ตอน **ปล่อยปุ่ม** (ช้ากว่าปกติไม่กี่ ms)
- ไฟล์ v1 (CapsLock King เดิม) แปลงเป็น v2 อัตโนมัติเมื่อโหลด

---

## 🧪 Debug / log

```powershell
.\OpenKey.exe --debug                 # เขียน log ที่ openkey.log
.\OpenKey.exe --config D:\my.json     # ใช้ config คนละไฟล์
.\OpenKey.exe --settings              # เปิดหน้าตั้งค่าทันที
```

---

## 🩺 Troubleshooting

**ปุ่มไม่ทำงานในบางแอป** — แอปที่รันแบบ Administrator จะไม่รับคีย์จากโปรแกรมระดับ user
(คลิกขวา → **Run as administrator**)

**เกมบางเกมไม่รับคีย์** — ข้อจำกัดของ Windows (raw input / anti-cheat) และไม่รองรับ per-device remap

**LED CapsLock** — ถ้าใช้ CapsLock เป็น trigger มันจะถูกกลืนทั้งหมด (ต้องการพิมพ์ใหญ่ใช้ `Shift`)

**ตั้งตัวอักษรเป็น trigger แล้วพิมพ์ช้า** — เพิ่ม `tapTimeout` หรือเลี่ยงใช้ปุ่มที่พิมพ์บ่อยเป็น trigger

---

## 🔨 Build & Test

ต้องมี .NET 10 SDK (ไม่ต้องมี Visual Studio)

```powershell
dotnet test                                          # unit test
dotnet build src\OpenKey -c Release                  # build
powershell -ExecutionPolicy Bypass -File scripts\publish.ps1   # เตรียมไฟล์ให้ installer
```

- `scripts\publish.ps1` → `publish\` แบบ self-contained (~160 MB / 255 ไฟล์)
- `-SingleFile` ถ้าอยากได้ exe เดี่ยว (แตกไฟล์ตอนรันครั้งแรก)
- ไอคอน: `OpenKey.ico` (สร้างใหม่ได้ด้วย `tools\generate-icon.ps1`)

## 📁 โครงสร้างโปรเจค

| ไฟล์ | คำอธิบาย |
|------|----------|
| `src/OpenKey/App.xaml(.cs)` | entry, single instance, tray, เปิดหน้าตั้งค่า |
| `src/OpenKey/Core/Keys.cs` | ตารางชื่อปุ่ม ↔ scancode/VK + ModMask |
| `src/OpenKey/Core/ConfigLoader.cs` | อ่าน/เขียน/validate config + migration v1→v2 |
| `src/OpenKey/Core/ConfigModel.cs` | โมเดลที่ compile แล้ว (Trigger/DirectBinding) |
| `src/OpenKey/Core/HookEngine.cs` | state machine (pure, unit test) |
| `src/OpenKey/Core/KeyboardHook.cs` | `WH_KEYBOARD_LL` + โหมดจับคีย์ |
| `src/OpenKey/Core/Sender.cs` | `SendInput` แบบ batch |
| `src/OpenKey/Views/MainWindow.xaml(.cs)` | หน้าตั้งค่า |
| `tests/OpenKey.Tests/` | xUnit |
| `legacy/` | เวอร์ชัน AutoHotkey เดิม (ทางถอย) |

---

## 📌 หมายเหตุ

- Windows 10/11
- ถ้าทำ installer: เอาโฟลเดอร์ `publish\` ไปแพ็ก (Inno Setup/WiX) — อย่าลืม `config.json`
- License: MIT
