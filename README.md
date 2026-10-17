# 🔥 CapsLock King

> เปลี่ยนปุ่ม CapsLock ให้เป็นปุ่มวิเศษ (Modifier Key) สำหรับ Vim-style navigation และช็อตคัทสุดคุ้มบน Windows

สคริปต์ [AutoHotkey](https://www.autohotkey.com/) ที่เปลี่ยน CapsLock ที่แทบไม่ได้ใช้ ให้กลายเป็น **Super Key** ควบคุมทุกอย่างบนคีย์บอร์ดแบบเดียวกับ Vim — ไม่ต้องง้อเมาส์ ไม่ต้องงอข้อมือ

---

## ✨ คุณสมบัติ

### 🧭 Navigation แบบ Vim
| คีย์ | ฟังก์ชัน |
|------|----------|
| `CapsLock` (กดเดี่ยว) | `Esc` |
| `CapsLock + h` | ← ซ้าย |
| `CapsLock + j` | ↓ ล่าง |
| `CapsLock + k` | ↑ บน |
| `CapsLock + l` | → ขวา |

### 📋 Clipboard
| คีย์ | ฟังก์ชัน |
|------|----------|
| `CapsLock + c` | Copy (`Ctrl+C`) |
| `CapsLock + v` | Paste (`Ctrl+V`) |
| `CapsLock + x` | Cut (`Ctrl+X`) |
| `CapsLock + z` | Undo (`Ctrl+Z`) |
| `CapsLock + y` | Redo (`Ctrl+Y`) |

### 📁 ไฟล์
| คีย์ | ฟังก์ชัน |
|------|----------|
| `CapsLock + s` | Save (`Ctrl+S`) |
| `CapsLock + a` | Select All (`Ctrl+A`) |
| `CapsLock + f` | Find (`Ctrl+F`) |
| `CapsLock + w` | Close Tab (`Ctrl+W`) |

### 🗑️ การลบ
| คีย์ | ฟังก์ชัน |
|------|----------|
| `CapsLock + .` | Delete (ลบตัวอักษรข้างหน้า) |
| `CapsLock + ,` | Backspace (ลบตัวอักษรข้างหลัง) |

### ⌨️ Function Keys (เหมาะกับ Developer)
| คีย์ | ฟังก์ชัน |
|------|----------|
| `CapsLock + r` | Refresh (`F5`) |
| `CapsLock + g` | Debug (`F9`) |
| `CapsLock + d` | Step Over (`F10`) |
| `CapsLock + e` | Step Into (`F11`) |
| `CapsLock + t` | `F12` |

### 🪟 การจัดการ Window
| คีย์ | ฟังก์ชัน |
|------|----------|
| `CapsLock + q` | Close App (`Alt+F4`) |
| `CapsLock + u` | สลับไป Virtual Desktop ซ้าย |
| `CapsLock + i` | สลับไป Virtual Desktop ขวา |

### 🔀 ทริค: กดคู่กับ Shift / Ctrl / Alt

ทุก hotkey ทำงานผ่านกลไก remap ของ AutoHotkey ที่ปล่อยให้ modifier ที่กดค้างอยู่ส่งผ่านไปด้วย — ลองกดค้างผสมได้เลย:

| ทริค | ผลลัพธ์ |
|------|----------|
| `CapsLock + Shift + h/j/k/l` | เลือกข้อความทีละตัว (เหมือนกด `Shift` + ลูกศร) |
| `CapsLock + Ctrl + h/l` | กระโดดทีละคำ (เหมือนกด `Ctrl` + ลูกศร) |
| `CapsLock + Ctrl + Shift + l` | เลือกข้อความทีละคำ |
| `CapsLock + Alt + h/l` | ย้อนกลับ / ไปข้างหน้า (เหมือน `Alt` + ←/→) |

---

## 🚀 วิธีติดตั้ง

1. ดาวน์โหลดและติดตั้ง [AutoHotkey](https://www.autohotkey.com/) (v1.x)
2. ดับเบิลคลิกไฟล์ `ahk.ahk` เพื่อรัน
3. เท่านี้ก็ใช้งานได้เลย! 🎉

> 💡 หรือใช้ไฟล์ `CapsLockKing.exe` ที่ compile ไว้แล้ว โดยไม่ต้องติดตั้ง AutoHotkey

---

## ⚙️ ตั้งค่ารันอัตโนมัติตอนเปิดเครื่อง (Startup)

1. กด `Win + R` พิมพ์ `shell:startup` แล้วกด Enter
2. สร้าง Shortcut จากไฟล์ `CapsLockKing.exe` (หรือ `ahk.ahk`)
3. วาง Shortcut ลงในโฟลเดอร์ Startup

เรียบร้อย! ทุกครั้งที่เปิดเครื่อง Windows สคริปต์จะรันอัตโนมัติ

> ⚠️ ถ้าเคยตั้ง Startup ไว้กับเวอร์ชันเก่า (ชื่อไฟล์ `ahk.exe`) ต้องลบ Shortcut เก่าและสร้างใหม่ เพราะไฟล์ถูกเปลี่ยนชื่อเป็น `CapsLockKing.exe` แล้ว

---

## 🛠️ ปรับแต่งเพิ่มเติม

เปิดไฟล์ `ahk.ahk` ด้วย Notepad หรือ editor ใดก็ได้ แล้วเพิ่ม/แก้บรรทัดตามรูปแบบนี้:

```autohotkey
Capslock & <คีย์>::<คำสั่ง>   ; คำอธิบาย
```

ตัวอย่างเพิ่มเติม:
```autohotkey
Capslock & n::^n              ; เปิด New Window (Ctrl+N)
Capslock & m::Send {Media_Play_Pause}  ; เล่น/หยุดเพลง
```

---

## 🔧 Troubleshooting / ข้อควรรู้

**ปุ่มไม่ทำงานในบางแอป**
แอปนั้นอาจรันด้วยสิทธิ์ Administrator (เช่น Task Manager หรือ terminal ที่เปิดแบบ admin) — Windows จะกัน hotkey ไว้ ต้องรันสคริปต์แบบ Administrator ด้วย (คลิกขวาที่ `CapsLockKing.exe` → **Run as administrator**)

**อยากปิด/หยุดสคริปต์**
คลิกขวาที่ไอคอนมงกุฎ 👑 ใน system tray → **Exit**

**ไฟ LED ที่ปุ่ม CapsLock ไม่ติดอีกแล้ว**
เป็นเรื่องตั้งใจ — สคริปต์ปิดการทำงานเดิมของ CapsLock (`SetCapsLockState, AlwaysOff`) ถ้าต้องการพิมพ์ตัวพิมพ์ใหญ่ให้ใช้ `Shift`

**แก้ `ahk.ahk` แล้วพฤติกรรมไม่เปลี่ยน**
ไฟล์ `CapsLockKing.exe` คือสำเนาที่ compile ไว้ ณ ตอนนั้น — ต้อง compile ใหม่ทุกครั้งหลังแก้สคริปต์:

```bash
& "C:\Program Files\AutoHotkey\Compiler\Ahk2Exe.exe" /in ahk.ahk /out CapsLockKing.exe /icon CapsLockKing.ico
```

> 💡 ไฟล์ icon สร้างใหม่ได้ด้วย `powershell -ExecutionPolicy Bypass -File tools\generate-icon.ps1`

---

## 📌 หมายเหตุ

- สคริปต์ปิดการทำงานเดิมของ CapsLock (`SetCapsLockState, AlwaysOff`) — ถ้าต้องการพิมพ์ตัวพิมพ์ใหญ่ให้ใช้ `Shift` แทน
- รองรับ Windows 10/11 (Virtual Desktop ต้องใช้ Windows 10 ขึ้นไป)
- ถ้าเจอบั๊กหรืออยากเพิ่มฟีเจอร์ ส่ง Pull Request หรือเปิด Issue ได้เลย 👑

---

**Made with ❤️ + ☕ + CapsLock**
