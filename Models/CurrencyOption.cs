using System;

namespace EcoPowerMonitor.Models
{
    public class CurrencyOption
    {
        public string Code { get; set; } = "THB";
        public string Symbol { get; set; } = "฿";
        public string CountryName { get; set; } = "Thailand (ไทย)";
        public double DefaultTariff { get; set; } = 4.50;

        public string DisplayText => $"{Symbol}  {Code} - {CountryName}";

        public override string ToString() => DisplayText;
    }
}
