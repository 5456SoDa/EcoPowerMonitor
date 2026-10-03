# ⚡ EcoPower Monitor

[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011%20(x64)-0078D6?logo=windows)](https://github.com/5456SoDa/EcoPowerMonitor)
[![.NET 8.0](https://img.shields.io/badge/.NET-8.0%20LTS-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Status: Production Ready](https://img.shields.io/badge/Release-v1.0.0-emerald.svg)](https://github.com/5456SoDa/EcoPowerMonitor/releases)

**EcoPower Monitor** เป็นโปรแกรมโอเพนซอร์ส Native Windows Desktop Application (.NET 8 + WPF) สำหรับตรวจสอบการใช้พลังงานไฟฟ้าของเครื่องคอมพิวเตอร์และแล็ปท็อปแบบเรียลไทม์ พร้อมคำนวณค่าไฟฟ้าตามบิลจริง (รองรับ TOU) และโหมด **Mini HUD** ลอยมุมจอสุดมินิมอล โดยไม่กินทรัพยากรเครื่องและปลอดภัย 100%

---

## 📥 วิธีดาวน์โหลดและใช้งาน (Quick Download & Run)

> [!TIP]
> **ไม่ต้องติดตั้ง (Zero Installation):** เพียงดาวน์โหลดไฟล์โปรแกรมเดี่ยว (Single-File Portable) ไปวางไว้ที่ใดก็ได้ในเครื่อง แล้วดับเบิลคลิกเปิดใช้งานได้ทันที!

1. ไปที่แท็บ **[Releases](https://github.com/5456SoDa/EcoPowerMonitor/releases)** ทางขวามือของ GitHub
2. ดาวน์โหลดไฟล์ **`EcoPower.exe`** หรือ **`EcoPower-Windows-x64.zip`**
3. ดับเบิลคลิกเปิดโปรแกรมใช้งานได้ทันที (รองรับทั้ง Windows 10 และ Windows 11 แบบ 64-bit)

---

## 🛡️ การันตีความปลอดภัยและข้อมูลส่วนบุคคล 100% (Safety & Privacy)

* 🔒 **ทำงานแบบออฟไลน์ 100% (Air-Gapped / Zero Network):** โปรแกรมไม่มีโค้ดเชื่อมต่ออินเทอร์เน็ตแม้แต่บรรทัดเดียว ไม่มีการส่งสถิติการใช้งานใดๆ ออกนอกเครื่องของคุณ
* 🧼 **โอเพนซอร์สตรวจสอบได้ (100% Transparent):** ซอร์สโค้ดทั้งหมดเปิดเผยบนคลังนี้ สามารถตรวจสอบและคอมไพล์เองได้ทุกบรรทัด
* 💎 **ใช้เฉพาะไลบรารีมาตรฐานที่เป็นทางการ (Audited Libraries):**
  * `LibreHardwareMonitorLib` (v0.9.6) – ไลบรารีระดับโลกสำหรับอ่านเซนเซอร์ฮาร์ดแวร์โดยตรง
  * `Microsoft.Data.Sqlite` (v10.0.12) – ไลบรารีฐานข้อมูลทางการจาก Microsoft บันทึกประวัติในเครื่องลง `%LocalAppData%\EcoPowerMonitor\`

---

## ⚡ ฟังก์ชันเด่น (Key Features)

### 1. Dashboard ภาพรวมพลังงาน (Overview)
* **Total System Power:** วัตต์รวมทั้งระบบแบบเรียลไทม์ (Smooth 30fps LERP Animation ตัวเลขเปลี่ยนนุ่มนวล)
* **Real-time Cost Breakdown:** คำนวณค่าไฟฟ้าทันทีแบบละเอียด (บาท/ชั่วโมง, บาท/วัน, บาท/เดือน)
* **Live Power Wave Chart:** กราฟคลื่นพลังงานสด 60 วินาทีล่าสุด ปรับ Scale อัตโนมัติ พร้อมจุดนำทาง (Live Head Pulse)
* **TOU & Carbon Footprint:** รองรับระบบค่าไฟแบบตามช่วงเวลา (On-Peak / Off-Peak) และประเมินการปล่อยก๊าซคาร์บอน (CO₂e)
* **Component Cards:**
  * **CPU:** วัด Package Power (PPT), อุณหภูมิ (°C), และภาระการทำงาน (%)
  * **GPU:** วัดพลังงานการ์ดจอ (TGP), อุณหภูมิ (°C), และภาระการทำงาน (%) รองรับทั้ง NVIDIA, AMD, Intel
  * **Battery (แล็ปท็อป):** อัตราคาย/ชาร์จไฟ (Watts), แบตเตอรี่คงเหลือ (%), เวลาใช้งานที่เหลือ, และระดับความเสื่อม (Wear Level)

### 2. Mini HUD (แถบลอยมินิมอลมุมจอ)
* แถบแสดงผลกะทัดรัด (370x66px) สำหรับปักหมุดไว้มุมจอขณะทำงาน ท่องเว็บ หรือเล่นเกม
* แสดงวัตต์รวม, อัตราค่าไฟต่อชั่วโมง, อุณหภูมิ CPU และ GPU แบบเรียลไทม์
* **Smart Thermal Alert:** ป้ายอุณหภูมิจะเปลี่ยนเป็นสีแดงเตือนทันทีเมื่อฮาร์ดแวร์ร้อนจัด
* **ฟังก์ชันควบคุม:** ปักหมุดให้อยู่บนสุดเสมอ (Pin On-Top), ดับเบิลคลิกขยายกลับสู่หน้าต่างหลัก, ลากย้ายตำแหน่งได้อิสระ

### 3. ตรวจสอบแอปที่กินไฟสูงสุด (Top 5 Power Apps)
* แสดงรายชื่อ 5 โปรแกรมในเครื่องที่ดึงพลังงานและสร้างค่าไฟมากที่สุดแบบเรียลไทม์

### 4. เซนเซอร์ละเอียดระดับฮาร์ดแวร์ (Detailed Sensors Tab)
* แสดงค่าเซนเซอร์ทุกตัวในเครื่อง (Voltages, Temperatures, Clocks, Fans, Powers, Loads)
* มีช่องค้นหา (Search Filter) และปุ่มกรองหมวดหมู่ (Power, Temp, Load, Clock, Fan)

### 5. ประวัติและสถิติสะสม (History & Export Tab)
* บันทึกข้อมูลสรุปรายวันลงฐานข้อมูล SQLite อัตโนมัติ ข้อมูลไม่สูญหายเมื่อปิดเครื่อง
* ส่งออกข้อมูลสถิติเป็นไฟล์ **CSV** และ **JSON** ได้ในคลิกเดียว

### 6. ประหยัดพลังงานระดับสุดยอด (Eco RAM & Zero Background CPU)
* **ตัดการเรนเดอร์ UI อัตโนมัติ:** เมื่อย่อหน้าต่างลง Taskbar หรือซ่อนลง System Tray โปรแกรมจะตัดการวาดกราฟและ UI ทันที ส่งผลให้ CPU ทำงานแทบจะ **0.0%**
* **คืนหน่วยความจำ RAM ให้ระบบ (Working Set Trimming):** ล้างแคชหน่วยความจำคืนระบบทันทีเมื่อย่อหน้าต่าง ลดการกิน RAM เหลือเพียง **~15 - 25 MB**
* **รองรับ Windows 11:** หน้าต่างมีขอบมน เงา DropShadow และธีม Titlebar เข้ากับระบบปฏิบัติการอย่างสมบูรณ์แบบ

---

## 🛠️ วิธีการสร้างและคอมไพล์จากซอร์สโค้ด (Build from Source)

### ข้อกำหนดของระบบสำหรับการพัฒนา:
* Windows 10 หรือ Windows 11 (64-bit)
* [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) ขึ้นไป

### ขั้นตอนการคอมไพล์:

```bash
# 1. Clone repository
git clone https://github.com/5456SoDa/EcoPowerMonitor.git
cd EcoPowerMonitor

# 2. Restore และ Build
dotnet build -c Release

# 3. สร้างไฟล์ Portable Executable เดี่ยว (Publish Single-File)
dotnet publish -r win-x64 -c Release --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:UseSharedCompilation=false -o ./publish
```

หรือสามารถดับเบิลคลิกที่ไฟล์ **`Publish-Portable.bat`** เพื่อคอมไพล์และสร้างไฟล์ `publish/EcoPower.exe` โดยอัตโนมัติ

---

## 🔑 สิทธิ์ Administrator (Run as Administrator)

* ตัวโปรแกรมสามารถเปิดใช้งานและอ่านค่าพลังงานได้ตามปกติโดยไม่ต้องมีสิทธิ์ Admin
* อย่างไรก็ตาม สถาปัตยกรรม CPU และเมนบอร์ดบางรุ่น (เช่น MSR / RAPL registers ของ Intel หรือ AMD) กำหนดให้ต้องใช้สิทธิ์ Ring-0 เพื่อเข้าถึงเซนเซอร์พลังงานลึก
* หากเครื่องของคุณยังอ่านค่าวัตต์ CPU ไม่ครบ คุณสามารถคลิกปุ่ม **"ยกระดับสิทธิ์ (Run as Admin)"** ที่แถบแจ้งเตือนด้านบนของโปรแกรมได้ทันที

---

## 📄 สัญญาอนุญาต (License)

โปรเจกต์นี้เผยแพร่ภายใต้สัญญาอนุญาตแบบ **[MIT License](LICENSE)** สามารถนำไปใช้งาน ปรับปรุง แก้ไข และเผยแพร่ต่อได้อย่างอิสระ
