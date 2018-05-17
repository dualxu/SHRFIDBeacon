using Windows.Foundation.Metadata;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel.Resources;
using System.Collections.Generic;
using Windows.Globalization;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using UniversalBeacon.Library.Core.Entities;
using UniversalBeacon.Library.Core.Interfaces;
using UniversalBeacon.Library.Core.Interop;
using UniversalBeacon.Library.UWP;
using System.Threading;
using NLog;

//This project type is RuntimeComponent(Universal Windows)
//This project type is RuntimeComponent(Universal Windows)
//This project type is RuntimeComponent(Universal Windows)
//Three times for important thing,^_^
namespace SHRFIDBeaconRuntimeComponent
{
    //this class must be sealed, and must have attribuite [Windows.Foundation.Metadata.AllowForWeb]
    [AllowForWeb]
    public sealed class SHRFIDBeaconService
    {
        private static Logger logger = LogManager.GetLogger("SHRFIDBeaconService");

        private static bool _restartingBeaconWatch;
        private static string _errBleMessage;

        private readonly WindowsBluetoothPacketProvider _provider;
        private BeaconManager _beaconManager;

        public SHRFIDBeaconService()
        {
            // Construct the Universal Bluetooth Beacon manager
            _provider = new WindowsBluetoothPacketProvider();
            _beaconManager = new BeaconManager(_provider);

            // Subscribe to status change events of the provider
            _provider.WatcherStopped += WatcherOnStopped;
            _beaconManager.BeaconAdded += BeaconManagerOnBeaconAdded;
            //
            _errBleMessage = "ok";
            _beaconManager.Start();
            _beaconManager.Stop();
        }

        public int GetPlusResult(int param1, int param2)
        {
            return param1 + param2;
        }

        public string GetVersion()
        {
            return "1.0.0.0";
        }

        /// <summary>
        /// 开启查找周边ibeacon设备
        /// </summary>
        /// <returns></returns>
        public string StartSearchBeacons()
        {
            string retstr = "startSearchBeacons:ok";

            if(_errBleMessage == "ErrorNotSupported")
            {
                retstr = "startSearchBeacons:system unsupported";
                return retstr;
            }
            if(_errBleMessage == "ErrorNoRadioAvailable")
            {
                retstr = "startSearchBeacons: bluetooth power off";
                return retstr;
            }
            //
            if (_restartingBeaconWatch == false)
            {
                _beaconManager.Start();
                _restartingBeaconWatch = true;
            }
            else
            {
                retstr = "startSearchBeacons:already started";
            }
    
            return retstr;
        }

        /// <summary>
        /// 关闭查找周边ibeacon设备
        /// </summary>
        /// <returns></returns>
        public string StopSearchBeacons()
        {
            string retstr = "stopSearchBeacons:ok";

            if (_restartingBeaconWatch == true)
            {
                _beaconManager.Stop();
                _restartingBeaconWatch = false;
            }
            else
            {
                retstr = "stopSearchBeacons:already stopped";
            }

            return retstr;
        }

        /// <summary>
        /// 监听周边ibeacon设备
        /// </summary>
        /// <returns></returns>
        public string OnSearchBeacons()
        {
            string onSearchBeaconsStr = "this would be a beaconlist found!";
            double accuracy = 0.0;
            
            List<Beacons> beaconslist = new List<Beacons>();
            Beacons beacons = new Beacons();            
            
            if((_beaconManager.BluetoothBeacons.ToList().Count == 0)||(_restartingBeaconWatch == false))
            {
                onSearchBeaconsStr = "{\"beacons\":[]}";
                return onSearchBeaconsStr;
            }
            //
            Debug.WriteLine("Beacons discovered so far\n-------------------------");
            foreach (var bluetoothBeacon in _beaconManager.BluetoothBeacons.ToList())
            {
                foreach (var beaconFrame in bluetoothBeacon.BeaconFrames.ToList())
                {
                    // Print a small sample of the available data parsed by the library
                    if (beaconFrame is ProximityBeaconFrame)
                    {
                        Debug.WriteLine("\nBeacon: " + bluetoothBeacon.BluetoothAddressAsString);
                        Debug.WriteLine("Type: " + bluetoothBeacon.BeaconType);
                        Debug.WriteLine("Last Update: " + bluetoothBeacon.Timestamp);
                        Debug.WriteLine("RSSI: " + bluetoothBeacon.Rssi);
                        //
                        Debug.WriteLine("Proximity Beacon Frame (iBeacon compatible)");
                        Debug.WriteLine("Uuid: " + ((ProximityBeaconFrame)beaconFrame).UuidAsString);
                        Debug.WriteLine("Major: " + ((ProximityBeaconFrame)beaconFrame).Major.ToString("D5"));
                        Debug.WriteLine("Major: " + ((ProximityBeaconFrame)beaconFrame).Minor.ToString("D5"));
                        Debug.WriteLine("TxPower: " + ((ProximityBeaconFrame)beaconFrame).TxPower);
                        //
                        beacons = new Beacons();
                        beacons.address = bluetoothBeacon.BluetoothAddressAsString;
                        beacons.type = bluetoothBeacon.BeaconType.ToString();
                        beacons.lastupdate = bluetoothBeacon.Timestamp.ToString("s");
                        beacons.txpower = ((ProximityBeaconFrame)beaconFrame).TxPower.ToString();
                        beacons.uuid = ((ProximityBeaconFrame)beaconFrame).UuidAsString;
                        beacons.major = ((ProximityBeaconFrame)beaconFrame).Major;
                        beacons.minor = ((ProximityBeaconFrame)beaconFrame).Minor;
                        beacons.rssi = bluetoothBeacon.Rssi.ToString();
                        accuracy = CalculateAccuracy(bluetoothBeacon.Rssi, ((ProximityBeaconFrame)beaconFrame).TxPower);
                        beacons.proximity = GetProximityByAccuracy(accuracy);
                        beacons.heading = "";
                        beacons.accuracy = accuracy.ToString();
                        //
                        beaconslist.Add(beacons);
                    }
                    else
                    {
                        Debug.WriteLine("Unknown frame - not parsed by the library, write your own derived beacon frame type!");
                        Debug.WriteLine("Payload: " + BitConverter.ToString(((UnknownBeaconFrame)beaconFrame).Payload));
                    }
                }
            }
            
            string beaconsstr = JsonTools.ObjectToJson(beaconslist);
            onSearchBeaconsStr = "{\"beacons\":" + beaconsstr + "}";

            return onSearchBeaconsStr;
        }

        /// <summary>
        /// CalculateAccuracy
        /// refer to:https://gist.github.com/JoostKiens/d834d8acd3a6c78324c9
        /// 通过rssi和tx_power_level计算距离
        /// </summary>
        /// <returns></returns>
        private double CalculateAccuracy(short rssi, sbyte tx_power_level)
        {
            double accuracy = 0.0;

            if (rssi == 0) return -1.0;

            double ratio = rssi * 1.0 / tx_power_level;

            if(ratio < 1.0)
            {
                accuracy = Math.Pow(ratio,10);
            }
            else
            {
                accuracy = 0.89976 * Math.Pow(ratio,7.7095) + 0.111;
            }

            return accuracy;
        }

        /// <summary>
        /// 通过距离计算精度
        /// proximity	精度，0：CLProximityUnknown, 1：CLProximityImmediate, 2：CLProximityNear, 3：CLProximityFar
        /// </summary>
        /// <param name="accuracy"></param>
        /// <returns></returns>
        private string GetProximityByAccuracy(double accuracy)
        {
            if(accuracy == -1.0)
            {
                //CLProximityUnknown
                return "0";
            }
            else if(accuracy <= 0.3)
            {
                //CLProximityImmediate
                return "1";
            }
            else if(accuracy <= 1.0)
            {
                //CLProximityNear
                return "2";
            }
            else
            {
                //CLProximityFar
                return "3";
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="sender">Reference to the sender instance of the event.</param>
        /// <param name="beacon">Beacon class instance containing all known and parsed information about
        /// the Bluetooth beacon.</param>
        private void BeaconManagerOnBeaconAdded(object sender, Beacon beacon)
        {
            Debug.WriteLine("\nBeacon: " + beacon.BluetoothAddressAsString);
            Debug.WriteLine("Type: " + beacon.BeaconType);
            Debug.WriteLine("Last Update: " + beacon.Timestamp);
            Debug.WriteLine("RSSI: " + beacon.Rssi);
            foreach (var beaconFrame in beacon.BeaconFrames.ToList())
            {
                // Print a small sample of the available data parsed by the library
                if (beaconFrame is UidEddystoneFrame)
                {
                    Debug.WriteLine("Eddystone UID Frame");
                    Debug.WriteLine("ID: " + ((UidEddystoneFrame)beaconFrame).NamespaceIdAsNumber.ToString("X") + " / " +
                                    ((UidEddystoneFrame)beaconFrame).InstanceIdAsNumber.ToString("X"));
                }
                else if (beaconFrame is UrlEddystoneFrame)
                {
                    Debug.WriteLine("Eddystone URL Frame");
                    Debug.WriteLine("URL: " + ((UrlEddystoneFrame)beaconFrame).CompleteUrl);
                }
                else if (beaconFrame is TlmEddystoneFrame)
                {
                    Debug.WriteLine("Eddystone Telemetry Frame");
                    Debug.WriteLine("Temperature [°C]: " + ((TlmEddystoneFrame)beaconFrame).TemperatureInC);
                    Debug.WriteLine("Battery [mV]: " + ((TlmEddystoneFrame)beaconFrame).BatteryInMilliV);
                }
                else if (beaconFrame is EidEddystoneFrame)
                {
                    Debug.WriteLine("Eddystone EID Frame");
                    Debug.WriteLine("Ranging Data: " + ((EidEddystoneFrame)beaconFrame).RangingData);
                    Debug.WriteLine("Ephemeral Identifier: " + BitConverter.ToString(((EidEddystoneFrame)beaconFrame).EphemeralIdentifier));
                }
                else if (beaconFrame is ProximityBeaconFrame)
                {
                    Debug.WriteLine("Proximity Beacon Frame (iBeacon compatible)");
                    Debug.WriteLine("Uuid: " + ((ProximityBeaconFrame)beaconFrame).UuidAsString);
                    Debug.WriteLine("Major: " + ((ProximityBeaconFrame)beaconFrame).MajorAsString);
                    Debug.WriteLine("Major: " + ((ProximityBeaconFrame)beaconFrame).MinorAsString);
                }
                else
                {
                    Debug.WriteLine("Unknown frame - not parsed by the library, write your own derived beacon frame type!");
                    Debug.WriteLine("Payload: " + BitConverter.ToString(((UnknownBeaconFrame)beaconFrame).Payload));
                }
            }
        }

        private void WatcherOnStopped(object sender, BTError btError)
        {
            string errorMsg = null;
            if (btError != null)
            {
                switch (btError.BluetoothErrorCode)
                {
                    case BTError.BluetoothError.Success:
                        errorMsg = "WatchingSuccessfullyStopped";
                        break;
                    case BTError.BluetoothError.RadioNotAvailable:
                        errorMsg = "ErrorNoRadioAvailable";
                        break;
                    case BTError.BluetoothError.ResourceInUse:
                        errorMsg = "ErrorResourceInUse";
                        break;
                    case BTError.BluetoothError.DeviceNotConnected:
                        errorMsg = "ErrorDeviceNotConnected";
                        break;
                    case BTError.BluetoothError.DisabledByPolicy:
                        errorMsg = "ErrorDisabledByPolicy";
                        break;
                    case BTError.BluetoothError.NotSupported:
                        errorMsg = "ErrorNotSupported";
                        break;
                    case BTError.BluetoothError.OtherError:
                        errorMsg = "ErrorOtherError";
                        break;
                    case BTError.BluetoothError.DisabledByUser:
                        errorMsg = "ErrorDisabledByUser";
                        break;
                    case BTError.BluetoothError.ConsentRequired:
                        errorMsg = "ErrorConsentRequired";
                        break;
                    case BTError.BluetoothError.TransportNotSupported:
                        errorMsg = "ErrorTransportNotSupported";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            if (errorMsg == null)
            {
                // All other errors - generic error message
                errorMsg = _restartingBeaconWatch
                    ? "FailedRestartingBluetoothWatch"
                    : "AbortedWatchingBeacons";
            }
            //SetStatusOutput(_resourceLoader.GetString(errorMsg));
            _errBleMessage = errorMsg;
        }
    }
}
