# AGENTS.md

## โปรเจค: CapsLock King 🔥

AutoHotkey (v1) script ที่เปลี่ยนปุ่ม CapsLock ให้เป็น **Super Key** สำหรับ
Vim-style navigation และ keyboard shortcuts บน Windows (Windows 10/11)

## โครงสร้างไฟล์

| ไฟล์ | คำอธิบาย | commit ขึ้น git? |
|------|----------|------------------|
| `ahk.ahk` | สคริปต์หลัก (AutoHotkey v1) — จุดเดียวที่แก้ code | ✅ |
| `README.md` | เอกสารโปรเจค + ตาราง keybindings ครบทุก mapping | ✅ |
| `LICENSE` | สัญญาอนุญาต MIT (js.dev) | ✅ |
| `.gitignore` | กันไฟล์ขยะของ Windows (Thumbs.db ฯลฯ) | ✅ |
| `AGENTS.md` | คู่มือสำหรับ AI agent (ไฟล์นี้) | ✅ |
| `CapsLockKing.exe` | เวอร์ชัน compiled (ฝังสคริปต์ + icon) — **ต้อง rebuild ทุกครั้งที่แก้ `ahk.ahk`** | ✅ |
| `CapsLockKing.ico` | ไอคอนของ exe (สร้างจาก `tools/generate-icon.ps1`) | ✅ |
| `tools/generate-icon.ps1` | สคริปต์ PowerShell สร้างไอคอน (ไม่พึ่งไฟล์ภาพภายนอก) | ✅ |

## ข้อมูลสำคัญ (ต้องรู้ก่อนแก้)

- ใช้ **AutoHotkey v1 syntax** (`SendMode, Input`, `Capslock & h::`) — **ห้ามใช้ v2 syntax** (`SendInput`, `Hotkey` object ฯลฯ)
- บรรทัด `SetCapsLockState, AlwaysOff` จำเป็น — ห้ามลบ ไม่งั้น CapsLock กลับมาทำงานเดิม
- `SendMode, Input` ต้องอยู่ใน auto-execute section ก่อน hotkey ทุกตัว (directive อย่าง `#NoEnv` / `#SingleInstance` วางเหนือได้)
- `#SingleInstance, Force` — กันรันซ้ำแล้ว hotkey ซ้อนกัน 2 ชั้น
- `#MaxHotkeysPerInterval 200` กันไม่ให้ Windows ตีว่า hotkey spam
- mapping แบบ `Capslock & h::Left` คือ remap ซึ่ง AutoHotkey ใช้ `{Blind}` ภายใน — modifier ที่ผู้ใช้กดค้าง (Shift/Ctrl/Alt) จะส่งผ่านไปด้วย และปุ่ม auto-repeat ได้ตอนกดค้าง (ดูทริคใน README)

## กฎการแก้ code

1. Mapping ใหม่ต้องใช้รูปแบบ `Capslock & <คีย์>::<คำสั่ง>` เสมอ พร้อม comment กำกับ
2. ไล่ลำดับ section ให้ตรงกับเดิม: Navigation → Clipboard → File → Delete → Function Keys → App Controls → Window Management
3. ถ้าเพิ่ม/แก้ mapping ต้องอัปเดตตารางใน `README.md` พร้อมกันเสมอ
4. ระวังคีย์ซ้ำ — ถ้าจะ map คีย์ที่ใช้แล้ว ต้องตัด mapping เดิมออก
5. ทุกครั้งที่แก้ `ahk.ahk` ต้อง rebuild `CapsLockKing.exe` (Ahk2Exe) แล้ว commit ทั้ง 2 ไฟล์พร้อมกัน — คำสั่งอยู่ในหัวข้อ "คำสั่งที่ใช้บ่อย"
6. ทดสอบโดยดับเบิลคลิก `ahk.ahk` หรือรัน `CapsLockKing.exe` (ต้องติดตั้ง AutoHotkey v1)

## Mapping ปัจจุบัน (สรุป)

- `CapsLock` เดี่ยว → `Esc`
- `h/j/k/l` → ซ้าย/ล่าง/บน/ขวา (Vim)
- `c/v/x/z/y` → Copy/Paste/Cut/Undo/Redo
- `s/a/f/w` → Save/Select All/Find/Close Tab
- `.`/`,` → Delete/Backspace
- `r/g/d/e/t` → F5/F9/F10/F11/F12
- `q` → Alt+F4
- `u/i` → สลับ Virtual Desktop ซ้าย/ขวา

## งานที่ค้าง

- [ ] อัปเดต shortcut ใน Startup folder ของเครื่องผู้ใช้ให้ชี้ `CapsLockKing.exe` (ของเดิมชี้ `ahk.exe` เวอร์ชันเก่า)
- [ ] พิจารณา migrate จาก AutoHotkey v1 → v2 (v1 เลิก maintain แล้ว) — ยังไม่เริ่ม

## คำสั่งที่ใช้บ่อย

```bash
# ทดสอบรันสคริปต์ตรงๆ (ต้องมี AutoHotkey v1)
AutoHotkey.exe ahk.ahk

# Rebuild exe หลังแก้ ahk.ahk (บังคับ — exe ถูก track ใน git)
"C:\Program Files\AutoHotkey\Compiler\Ahk2Exe.exe" /in ahk.ahk /out CapsLockKing.exe /icon CapsLockKing.ico

# สร้างไอคอนใหม่ (ถ้าต้องการ)
powershell -ExecutionPolicy Bypass -File tools\generate-icon.ps1

# ดูสถานะ git
git status
```
