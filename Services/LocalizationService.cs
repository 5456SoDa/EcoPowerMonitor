using System;
using System.Collections.Generic;

namespace EcoPowerMonitor.Services
{
    public class LocalizationService
    {
        private string _currentLanguage = "th";

        public string CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (_currentLanguage != value)
                {
                    _currentLanguage = value;
                    LanguageChanged?.Invoke();
                }
            }
        }

        public event Action? LanguageChanged;

        private static readonly Dictionary<string, (string Th, string En)> Dictionary = new()
        {
            { "AppTitle", ("EcoPower Monitor - ตรวจสอบการใช้ไฟฮาร์ดแวร์", "EcoPower Monitor - Hardware Power Telemetry") },
            { "TabOverview", ("ภาพรวม", "Overview") },
            { "TabSensors", ("เซนเซอร์ละเอียด", "Detailed Sensors") },
            { "TabHistory", ("ประวัติ & ค่าไฟ", "History & Cost") },
            { "TabSettings", ("การตั้งค่า", "Settings") },
            
            { "CurrentPower", ("กำลังไฟปัจจุบัน", "Current Power") },
            { "RealtimeCost", ("อัตราค่าไฟเฉลี่ย", "Realtime Cost Rate") },
            { "PerHour", ("บาท / ชม.", "THB / hr") },
            { "PerDay", ("บาท / วัน", "THB / day") },
            { "PerMonth", ("บาท / เดือน", "THB / month") },
            
            { "AverageWatts", ("ค่าเฉลี่ย", "Average") },
            { "PeakWatts", ("สูงสุด (Peak)", "Peak") },
            { "MinWatts", ("ต่ำสุด (Min)", "Minimum") },
            { "SessionEnergy", ("พลังงานสะสมรอบนี้", "Session Energy") },
            { "SessionCost", ("ค่าไฟรอบนี้", "Session Cost") },
            { "ActiveTime", ("ระยะเวลาที่เปิด", "Active Time") },

            { "CpuPower", ("กำลังไฟ CPU", "CPU Power") },
            { "GpuPower", ("กำลังไฟ GPU", "GPU Power") },
            { "BatteryStatus", ("สถานะแบตเตอรี่", "Battery Status") },
            { "BatteryRate", ("อัตราคาย/ชาร์จ", "Discharge/Charge Rate") },
            { "BatteryRemaining", ("เวลาใช้งานคงเหลือ", "Estimated Remaining") },
            { "WearLevel", ("ความเสื่อมแบตเตอรี่", "Wear Level") },
            { "Temperature", ("อุณหภูมิ", "Temperature") },
            { "Load", ("โหลดการทำงาน", "Load") },
            { "DesktopPowerNotice", ("ระบบไฟหลัก (AC Power)", "Main Line (AC Power)") },

            { "LiveGraphTitle", ("กราฟคลื่นการกินไฟสด (Live Power Graph - 60s)", "Live Power Wave Graph (60s)") },
            
            { "SearchPlaceholder", ("ค้นหาชื่อเซนเซอร์หรือฮาร์ดแวร์...", "Search sensor or hardware...") },
            { "ColHardware", ("อุปกรณ์", "Hardware") },
            { "ColSensor", ("ชื่อเซนเซอร์", "Sensor") },
            { "ColType", ("ประเภท", "Type") },
            { "ColValue", ("ค่าปัจจุบัน", "Value") },
            { "ColMin", ("ต่ำสุด", "Min") },
            { "ColMax", ("สูงสุด", "Max") },

            { "HistoricalSummary", ("สรุปประวัติพลังงานย้อนหลัง", "Historical Energy Summary") },
            { "ExportCsv", ("ส่งออก CSV", "Export CSV") },
            { "ExportJson", ("ส่งออก JSON", "Export JSON") },
            { "ResetSession", ("รีเซ็ตรอบปัจจุบัน", "Reset Session") },
            { "ColDate", ("วันที่", "Date") },
            { "ColTotalKwh", ("พลังงานรวม (kWh)", "Total Energy (kWh)") },
            { "ColTotalCost", ("ค่าไฟรวม", "Total Cost") },
            { "ColActiveTime", ("เวลาใช้งาน", "Active Time") },

            // Detailed Sensors Category Filters
            { "CatAll", ("ทั้งหมด", "All") },
            { "CatPower", ("⚡ กำลังไฟ (W)", "⚡ Power (W)") },
            { "CatTemp", ("🌡️ อุณหภูมิ (°C)", "🌡️ Temp (°C)") },
            { "CatLoad", ("📊 โหลด (%)", "📊 Load (%)") },
            { "CatClock", ("⏱️ สัญญาณนาฬิกา", "⏱️ Clock") },
            { "CatFan", ("🌀 พัดลม", "🌀 Fans") },

            // History KPI Summary Cards
            { "KpiLifetimeEnergy", ("พลังงานสะสมรวม", "Lifetime Energy") },
            { "KpiLifetimeCost", ("ค่าไฟสะสมรวม", "Lifetime Cost") },
            { "KpiDailyAvg", ("เฉลี่ยต่อวัน", "Daily Average") },
            { "KpiAllTimePeak", ("พีคสูงสุดตลอดกาล", "All-Time Peak") },

            // Settings & System Specs
            { "GeneralSettings", ("การตั้งค่าทั่วไป", "General Settings") },
            { "Language", ("ภาษา (Language)", "Language") },
            { "Theme", ("ธีมสี (Theme)", "Theme") },
            { "CurrencyRegion", ("สกุลเงินและภูมิภาค", "Currency & Region") },
            { "CurrencyRegionDesc", ("รองรับสกุลเงินสากลทั่วโลก (THB ฿, USD $, EUR €, JPY ¥, ฯลฯ)", "Supports global currencies (THB ฿, USD $, EUR €, JPY ¥, etc.)") },
            { "ElectricityTariff", ("อัตราค่าไฟต่อหน่วย", "Electricity Tariff Rate") },
            { "TariffDesc", ("อัตราค่าไฟฟ้าสำหรับการคำนวณ (ปกติ ~4.20 - 4.70 บาท)", "Electricity tariff per kWh for calculation (avg. 4.20 - 4.70 THB)") },
            { "SensorRefreshRate", ("ความถี่การอ่านเซนเซอร์ (Refresh Rate)", "Sensor Refresh Rate") },
            { "RefreshRateDesc", ("500 ms (เร็วพิเศษ), 1000 ms (มาตรฐาน), 2000 ms (ประหยัดพลังงาน)", "500 ms (Fast), 1000 ms (Standard), 2000 ms (Eco Saver)") },
            { "FastRate", ("500 ms (เร็วพิเศษ)", "500 ms (High Refresh)") },
            { "NormalRate", ("1,000 ms (มาตรฐาน - แนะนำ)", "1,000 ms (Standard - Recommended)") },
            { "EcoRate", ("2,000 ms (ประหยัดพลังงาน)", "2,000 ms (Eco Saver)") },
            { "AlertSettings", ("การตั้งค่าระบบแจ้งเตือน", "Alert Thresholds") },
            { "OverpowerLimit", ("แจ้งเตือนเมื่อวัตต์เกิน (Watts)", "Overpower Warning Threshold (W)") },
            { "OverpowerDesc", ("แจ้งเตือนเมื่อระบบใช้พลังงานรวมเกินค่านี้", "Alert when system power exceeds this threshold") },
            { "HighTempLimit", ("แจ้งเตือนเมื่อความร้อนเกิน (°C)", "High Temp Warning Threshold (°C)") },
            { "SaveSettings", ("บันทึกการตั้งค่า", "Save Settings") },
            { "SaveSuccess", ("บันทึกการตั้งค่าเรียบร้อยแล้ว", "Settings saved successfully") },
            { "ExportSuccess", ("ส่งออกข้อมูลเรียบร้อยแล้ว", "Data exported successfully") },
            { "ExportFailed", ("เกิดข้อผิดพลาดในการส่งออกข้อมูล", "Failed to export data") },
            { "StartWithWindows", ("เปิดโปรแกรมอัตโนมัติพร้อม Windows", "Start with Windows") },
            { "StartWithWindowsDesc", ("เปิดโปรแกรมขึ้นมาทำงานอัตโนมัติเมื่อเปิดเครื่อง", "Launch automatically when computer starts") },
            { "MinimizeToTray", ("ย่อซ่อนลง System Tray เมื่อกดปุ่มปิด [X]", "Close to System Tray [X]") },
            { "MinimizeToTrayDesc", ("เมื่อกดปุ่มปิด [X]: ย่อไปทำงานเบื้องหลังในถาดขวาล่าง (ส่วนปุ่มย่อ [-] จะย่อลง Windows Taskbar เสมอ)", "When pressing [X]: Keep running in background tray (Button [-] always minimizes to Taskbar)") },
            { "SystemHardwareSpecs", ("ข้อมูลฮาร์ดแวร์ระบบ", "System Hardware Specifications") },
            { "AppInfo", ("ข้อมูลซอฟต์แวร์และการพัฒนา", "Software & Developer Information") },
            { "AppDevCredits", ("พัฒนาโดย Soda • ระบบ Hardware Power Telemetry ออฟไลน์ 100% ปลอดภัย ไร้คลาวด์", "Created by SoDa • 100% Offline Air-Gapped Telemetry Engine") },
            { "RestoreDefaults", ("คืนค่าเริ่มต้นระบบทั้งหมด", "Restore Factory Defaults") },
            { "TrayOpenMain", ("เปิดหน้าต่างหลัก", "Open Main Window") },
            { "TrayOpenMini", ("เปิด Mini HUD", "Open Mini HUD") },
            { "TrayExit", ("ออกจากโปรแกรม", "Exit") },

            // TOU & Carbon Footprint
            { "TouTariff", ("คำนวณค่าไฟแบบ TOU (Time of Use)", "TOU Electricity Tariff (Time of Use)") },
            { "TouTariffDesc", ("คิดเรทค่าไฟแยกตามช่วงเวลา On-Peak (วันทำงาน 09:00 - 22:00) และ Off-Peak", "Calculate tariff by On-Peak (Workdays 09:00 - 22:00) & Off-Peak") },
            { "TouOnPeak", ("ช่วงเรท On-Peak (ความต้องการใช้ไฟสูง)", "On-Peak Period (High Demand)") },
            { "TouOffPeak", ("ช่วงเรท Off-Peak (ประหยัดค่าไฟ)", "Off-Peak Period (Economy)") },
            { "TouOnPeakRate", ("อัตรา On-Peak (บาท/หน่วย)", "On-Peak Rate (/kWh)") },
            { "TouOffPeakRate", ("อัตรา Off-Peak (บาท/หน่วย)", "Off-Peak Rate (/kWh)") },
            { "CarbonFootprint", ("คาร์บอนฟุตพริ้นท์ (CO₂e)", "Carbon Footprint (CO₂e)") },
            { "CarbonPerHour", ("การปล่อย CO₂ / ชม.", "CO₂ Rate / hr") },
            { "CarbonSession", ("CO₂ สะสมรอบนี้", "Session CO₂") },
            { "CarbonTrees", ("เทียบเท่าการดูดซับของต้นไม้", "Equivalent Tree Absorption") },
            { "CarbonTreesUnit", ("ต้น/ปี", "trees/yr") },
            { "CarbonFactor", ("ค่าสัมประสิทธิ์การปล่อย CO₂ (kg CO₂e/kWh)", "Emission Factor (kg CO₂e/kWh)") },
            { "CarbonFactorDesc", ("มาตรฐานการปล่อยคาร์บอนของโครงข่ายไฟฟ้า (ไทยเฉลี่ย 0.4999 kg/kWh)", "Grid carbon intensity emission factor (Thailand avg. 0.4999 kg/kWh)") },

            // Top 5 Power Apps
            { "TopPowerApps", ("Top 5 โปรแกรมที่กินไฟสูงสุดในเครื่อง", "Top 5 Power-Hungry Applications") },
            { "TopAppsSub", ("วัดการทำงานจริงของ CPU & RAM แบบเรียลไทม์ พร้อมประเมินกำลังวัตต์และค่าไฟรายชั่วโมง", "Real-time process telemetry with live power & cost share estimation") },
            { "ColRank", ("อันดับ", "Rank") },
            { "ColProgram", ("โปรแกรม / กระบวนการ", "Program / Process") },
            { "ColCpuLoad", ("โหลด CPU", "CPU Load") },
            { "ColEstPower", ("กำลังไฟประเมิน", "Est. Power") },
            { "ColCostHour", ("ค่าไฟ / ชม.", "Cost / hr") },
            { "ColMemory", ("หน่วยความจำ (RAM)", "Memory (RAM)") },

            // Caption Buttons Tooltips & Windows 11 System Menu
            { "CaptionMinimize", ("ย่อหน้าต่าง", "Minimize") },
            { "CaptionMaximize", ("ขยายเต็มจอ", "Maximize") },
            { "CaptionRestore", ("คืนขนาดหน้าต่าง", "Restore") },
            { "CaptionClose", ("ปิดหน้าต่าง", "Close") },

            { "SysMenuRestore", ("คืนขนาด", "Restore") },
            { "SysMenuMove", ("ย้าย", "Move") },
            { "SysMenuSize", ("ปรับขนาด", "Size") },
            { "SysMenuMinimize", ("ย่อหน้าต่าง", "Minimize") },
            { "SysMenuMaximize", ("ขยายเต็มจอ", "Maximize") },
            { "SysMenuClose", ("ปิด", "Close") },

            // Mini HUD Tooltips & Labels
            { "MiniPin", ("ปักหมุดให้อยู่บนสุด (Always on Top)", "Pin Always on Top") },
            { "MiniUnpin", ("ยกเลิกการปักหมุด", "Unpin Window") },
            { "MiniExpand", ("ขยายเป็นหน้าต่างหลัก (Full Dashboard)", "Expand to Full Dashboard") },
            { "MiniClose", ("ปิดโปรแกรม", "Close Application") },
            { "MiniPerHr", ("/ชม.", "/hr") }
        };

        public string Get(string key)
        {
            if (Dictionary.TryGetValue(key, out var val))
            {
                return _currentLanguage == "th" ? val.Th : val.En;
            }
            return key;
        }
    }
}
