using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.ModAPI;
using Sandbox.Common;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace GuidedMissile.GuidedMissileScript
{
    class Log : MySessionComponentBase
    {
        private const string ModName = "GuidedMissiles";
        private const string LogFile = "info.log";

        private static System.IO.TextWriter _writer = null;
        private static IMyHudNotification _notify = null;
        private static int _indent = 0;
        private static readonly StringBuilder Cache = new StringBuilder();

        public static void IncreaseIndent()
        {
            _indent++;
        }

        public static void DecreaseIndent()
        {
            if (_indent > 0)
                _indent--;
        }

        public static void ResetIndent()
        {
            _indent = 0;
        }

        public static void Error(Exception e)
        {
            Error(e.ToString());
        }

        public static void Error(string msg)
        {
            Info("ERROR: " + msg);

            try
            {
                string text = ModName + " error - open %AppData%/SpaceEngineers/Storage/..._" + ModName + "/" + LogFile + " for details";

                MyLog.Default.WriteLineAndConsole(text);

                if (_notify == null)
                {
                    _notify = MyAPIGateway.Utilities.CreateNotification(text, 10000, MyFontEnum.Red);
                }
                else
                {
                    _notify.Text = text;
                    _notify.ResetAliveTime();
                }

                _notify.Show();
            }
            catch (Exception e)
            {
                Info("ERROR: Could not send notification to local client: " + e.ToString());
            }
        }

        public static void Info(string msg)
        {
            Write(msg);
        }

        private static void Write(string msg)
        {
            try
            {
                if (_writer == null)
                {
                    if (MyAPIGateway.Utilities == null)
                        throw new Exception("API not initialied but got a log message: " + msg);

                    _writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(LogFile, typeof(Log));
                }

                Cache.Clear();
                Cache.Append(DateTime.Now.ToString("[HH:mm:ss] "));

                for (int i = 0; i < _indent; i++)
                {
                    Cache.Append("\t");
                }

                Cache.Append(msg);

                _writer.WriteLine(Cache);
                _writer.Flush();

                Cache.Clear();
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole(ModName + " had an error while logging message='" + msg + "'\nLogger error: " + e.Message + "\n" + e.StackTrace);
            }
        }

        public static void Close()
        {
            if (_writer != null)
            {
                _writer.Flush();
                _writer.Close();
                _writer = null;
            }

            _indent = 0;
            Cache.Clear();
        }
    }
}