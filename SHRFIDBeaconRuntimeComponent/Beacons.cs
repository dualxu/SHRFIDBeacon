using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Net;
using System.Xml;
using NLog;

namespace SHRFIDBeaconRuntimeComponent
{
    /// <summary>
    /// 微信上Beacons定义，来自接口wx.onSearchBeacons
    /// 参见：https://mp.weixin.qq.com/wiki?t=resource/res_main&id=mp1443448133
    /// </summary>
    public sealed class Beacons
    {
        /// <summary>
        /// 设备ID
        /// </summary>
        public string uuid { set; get; }
        /// <summary>
        /// 主类
        /// </summary>
        public int major { set; get; }
        /// <summary>
        /// 次类
        /// </summary>
        public int minor { set; get; }
        /// <summary>
        /// 距离，单位为米
        /// </summary>
        public string accuracy { set; get; }
        /// <summary>
        /// 接收信号的强度指示 dBm
        /// </summary>
        public string rssi { set; get; }
        /// <summary>
        /// 精度，0：CLProximityUnknown, 1：CLProximityImmediate, 2：CLProximityNear, 3：CLProximityFar
        /// </summary>
        public string proximity { set; get; }
        /// <summary>
        /// 接收信号时设备的方向（安卓设备返回有此字段，iOS无）；iOS设备若需要获取方向，可以利用HTML5标准API获取， 查看示例
        /// </summary>
        public string heading { set; get; }
        /// <summary>
        /// 蓝牙MAC地址
        /// </summary>
        public string address { set; get; }
        /// <summary>
        /// Beacon类型：iBeacon
        /// </summary>
        public string type { set; get; }
        /// <summary>
        /// 最后更新时间
        /// </summary>
        public string lastupdate { set; get; }
        /// <summary>
        /// 蓝牙iBeacon距离1米处的信息强度 dBm
        /// </summary>
        public string txpower { set; get; }
    }

    #region JsonTools
    /// <summary>
    /// JsonTools 对象和Json字符串互转,MD5加密等工具类
    /// </summary>
    public sealed class JsonTools
    {
        private static Logger Jlogger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 从一个对象信息生成Json串
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string ObjectToJson(object obj)
        {
            try
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(obj.GetType());
                MemoryStream stream = new MemoryStream();
                serializer.WriteObject(stream, obj);
                byte[] dataBytes = new byte[stream.Length];
                stream.Position = 0;
                stream.Read(dataBytes, 0, (int)stream.Length);
                return Encoding.UTF8.GetString(dataBytes);
            }
            catch (Exception ex)
            {
                Jlogger.Error("JsonTools.ObjectToJson(): " + ex.Message);
                return null;
            }
        }
        /// <summary>
        /// 从一个Json串生成对象信息
        /// </summary>
        /// <param name="jsonString"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static object JsonToObject(string jsonString, object obj)
        {
            try
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(obj.GetType());
                MemoryStream mStream = new MemoryStream(Encoding.UTF8.GetBytes(jsonString));
                return serializer.ReadObject(mStream);
            }
            catch (Exception ex)
            {
                Jlogger.Error("JsonTools.JSONToObject():" + ex.Message);
                return null;
            }
        }
    }
    #endregion
}
