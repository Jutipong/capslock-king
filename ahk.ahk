; ===============================================
; Vim-style Keyboard Shortcuts using CapsLock
; ===============================================
; Author: User
; Date: July 14, 2025
; Description: Transform CapsLock into a powerful modifier key
;              for Vim-like navigation and shortcuts
; ===============================================

SendMode, Input
#MaxHotkeysPerInterval 200

; Disable CapsLock original function
SetCapsLockState, AlwaysOff

; ===============================================
; BASIC NAVIGATION (Vim-style)
; ===============================================
Capslock::Esc                    ; CapsLock alone = Escape key
Capslock & h::Left               ; Move left (Vim h)
Capslock & j::Down               ; Move down (Vim j)  
Capslock & k::Up                 ; Move up (Vim k)
Capslock & l::Right              ; Move right (Vim l)

; ===============================================
; CLIPBOARD OPERATIONS
; ===============================================
Capslock & c::^c                 ; Copy (Ctrl+C)
Capslock & v::^v                 ; Paste (Ctrl+V)
Capslock & x::^x                 ; Cut (Ctrl+X)
Capslock & z::^z                 ; Undo (Ctrl+Z)
Capslock & y::^y                 ; Redo (Ctrl+Y)

; ===============================================
; FILE OPERATIONS
; ===============================================
Capslock & s::^s                 ; Save (Ctrl+S)
Capslock & a::^a                 ; Select All (Ctrl+A)
Capslock & f::^f                 ; Find (Ctrl+F)
Capslock & w::^w                 ; Close Tab (Ctrl+W)

; ===============================================
; DELETE OPERATIONS
; ===============================================
Capslock & .::Delete             ; Delete character forward
Capslock & ,::Backspace          ; Delete character backward

; ===============================================
; FUNCTION KEYS
; ===============================================
Capslock & r::F5                 ; Refresh (F5)
Capslock & g::F9                 ; Debug (F9)
Capslock & d::F10                ; Step Over (F10)
Capslock & e::F11                ; Step Into (F11)
Capslock & t::F12                ; F12

; ===============================================
; APPLICATION CONTROLS
; ===============================================
Capslock & q::Send !{F4}         ; Close Application (Alt+F4)

; ===============================================
; WINDOW MANAGEMENT
; ===============================================
; Virtual Desktop Navigation (Working version)
Capslock & u::Send #^{Left}      ; Move to left virtual desktop
Capslock & i::Send #^{Right}     ; Move to right virtual desktop

Return