# AGENTS.md

## โปรเจค: CapsLock King 🔥

AutoHotkey (v1) script ที่เปลี่ยนปุ่ม CapsLock ให้เป็น **Super Key** สำหรับ
Vim-style navigation และ keyboard shortcuts บน Windows (Windows 10/11)

## โครงสร้างไฟล์

| ไฟล์ | คำอธิบาย | commit ขึ้น git? |
|------|----------|------------------|
| `ahk.ahk` | สคริปต์หลัก (AutoHotkey v1) — จุดเดียวที่แก้ code | ✅ |
| `README.md` | เอกสารโปรเจค + ตาราง keybindings ครบทุก mapping | ✅ |
| `.gitignore` | กันไฟล์ binary ขึ้น git | ✅ |
| `readme.txt` | คู่มือติดตั้ง startup (ภาษาไทย) | ⚠️ ไฟล์ล้าสมัย เขียนว่า ask.exe ผิด — ควรแก้หรือลบ |
| `ahk.exe` | เวอร์ชัน compiled (1381 KB) | ❌ ถูก ignore แล้ว |
| `ahk.ico` | ไอคอนสำหรับ exe | ❌ ถูก ignore แล้ว |

## ข้อมูลสำคัญ (ต้องรู้ก่อนแก้)

- ใช้ **AutoHotkey v1 syntax** (`SendMode, Input`, `Capslock & h::`) — **ห้ามใช้ v2 syntax** (`SendInput`, `Hotkey` object ฯลฯ)
- บรรทัด `SetCapsLockState, AlwaysOff` จำเป็น — ห้ามลบ ไม่งั้น CapsLock กลับมาทำงานเดิม
- `SendMode, Input` ต้องอยู่บนสุดของสคริปต์
- `#MaxHotkeysPerInterval 200` กันไม่ให้ Windows ตีว่า hotkey spam

## กฎการแก้ code

1. Mapping ใหม่ต้องใช้รูปแบบ `Capslock & <คีย์>::<คำสั่ง>` เสมอ พร้อม comment กำกับ
2. ไล่ลำดับ section ให้ตรงกับเดิม: Navigation → Clipboard → File → Delete → Function Keys → App Controls → Window Management
3. ถ้าเพิ่ม/แก้ mapping ต้องอัปเดตตารางใน `README.md` พร้อมกันเสมอ
4. ระวังคีย์ซ้ำ — ถ้าจะ map คีย์ที่ใช้แล้ว ต้องตัด mapping เดิมออก
5. ทดสอบโดยรัน `ahk.exe ahk.ahk` หรือดับเบิลคลิก `ahk.ahk` (ต้องติดตั้ง AutoHotkey v1)

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

- [ ] `readme.txt` เขียนชื่อไฟล์ผิด (`ask.exe` → ควรเป็น `ahk.exe`) — แก้หรือลบ
- [ ] ยังไม่มี git commit แรก

## คำสั่งที่ใช้บ่อย

```bash
# ทดสอบรันสคริปต์ (ต้องมี AutoHotkey v1)
ahk.exe ahk.ahk

# ดูสถานะ git
git status
```
