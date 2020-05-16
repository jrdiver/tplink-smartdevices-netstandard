﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TPLinkSmartDevices.Data
{
    /// <summary>
    /// Encapsulates JSON data structure for current energy use as metered by the HS110 Smart Energy Meter.
    /// </summary>
    public class PowerData
    {
        private readonly dynamic _powerData;

        public PowerData(dynamic powerData)
        {
            _powerData = powerData;
        }

        //for older firmware versions
        //public double Voltage => _powerData.voltage_mv / 1000.0d;
        //public double Amperage => _powerData.current_ma / 1000.0d;
        //public double Power => _powerData.power_mw / 1000.0d;
        //public double Total => _powerData.total_wh / 1000.0d;


        // recent firmware
        /// <summary>
        /// Currently measured voltage in volts.
        /// </summary>
        public double Voltage => _powerData.voltage;

        /// <summary>
        /// Currently measured current in amperes.
        /// </summary>
        public double Amperage => _powerData.current / 1000.0d;

        /// <summary>
        /// Currently measured power in watts.
        /// </summary>
        public double Power => _powerData.power;

        /// <summary>
        /// Total power consumption in kilowatthours. 
        /// </summary>
        public double Total => _powerData.total;

        public int ErrorCode => _powerData.err_code;

        public override string ToString()
        {
            return $"{Voltage:0.00} Volt, {Amperage:0.00} Ampere, {Power:0.00} Watt";
        }
    }
}
