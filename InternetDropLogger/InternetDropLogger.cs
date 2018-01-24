using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;

namespace InternetDropLogger
{
    public partial class InternetDropLogger : ServiceBase
    {
        string WorkingPath { get; set; }
        public string FileName { get; set; }
        public string FullPath { get; set; }
        Task CheckerTask { get; set; }
        CancellationTokenSource cancellationTokenSource;
        CancellationToken cancellationToken;
        IEnumerable<string> gamesList;
        string GameName;
        int StatDelay = 0;

        public InternetDropLogger()
        {
            InitializeComponent();

            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;
        }

        protected override void OnStart(string[] args)
        {
            //if (args.Any())
            //{
            //    WorkingPath = args[0];
            //    gamesList = args[1].Split(new[] { ',' });
            //}
            //else
            {
                WorkingPath = $"C:/InternetDropLogger";
                gamesList = new[] { "overwatch", "wow", "diablo" };
            }
            FileName = $"Log_{GetShortDateTime()}.log";
            FullPath = Path.Combine(WorkingPath, FileName);
            if (!Directory.Exists(WorkingPath))
                Directory.CreateDirectory(WorkingPath);

            WriteLog(LogType.ServiceStarted);

            cancellationToken = new CancellationToken();
            CheckerTask = Task.Factory.StartNew(() =>
            {
                var lastInternetState = false;
                var lastGameState = false;
                do
                {
                    var internetState = IsInternetUp();
                    var gameState = GameIsRunning();

                    if (lastInternetState != internetState)
                    {
                        if (internetState)
                            WriteLog(LogType.InternetIsUp);
                        else
                            WriteLog(LogType.InternetIsDown);
                    }
                    lastInternetState = internetState;


                    if (lastGameState != gameState)
                    {
                        if (gameState)
                            WriteLog(LogType.GameRunned, GameName);
                        else
                            WriteLog(LogType.GameStopped);
                    }
                    lastGameState = gameState;

                    Thread.Sleep(3000);
                    StatDelay += 3000;

                    if (StatDelay > 30000)
                    {
                        StatDelay = 0;
                        var message = $@"{nameof(lastInternetState)}: {lastInternetState}\n
                                         {nameof(lastGameState)}: {lastGameState}\n
                                         {nameof(GameName)}: {GameName}\n";
                        WriteLog(LogType.ServiceStat, message);
                    }
                } while (true);

            }, cancellationToken);
        }

        private bool GameIsRunning()
        {
            var processes = Process.GetProcesses();
            foreach (var item in processes)
            {
                foreach (var item2 in gamesList)
                {
                    if (item.ProcessName.Contains(item2))
                    {
                        GameName = item.ProcessName;
                    }
                }
            }

            if (GameName != default(string))
                return true;
            return false;
        }

        private void WriteLog(LogType type, string message = "")
        {
            var logString = $"[{DateTime.Now.ToShortDateString()} {DateTime.Now.ToLongTimeString()}] -{type.ToString()}- {message}";

            try
            {
                using (var streamWriter = new StreamWriter(new FileStream(FullPath, FileMode.Append)))
                {
                    streamWriter.WriteLine(logString);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private bool IsInternetUp()
        {
            int desc;
            return InternetGetConnectedState(out desc, 0);
        }

        private string GetShortDateTime()
        {
            var date = DateTime.Now;

            return date.Year.ToString() + date.Month.ToString() + date.Day.ToString();
        }

        protected override void OnStop()
        {
            if (CheckerTask.Status == TaskStatus.Running)
            {
                cancellationTokenSource.Cancel();
            }

            WriteLog(LogType.ServiceStopped);
        }

        [System.Runtime.InteropServices.DllImport("wininet.dll")]
        private extern static bool InternetGetConnectedState(out int Description, int ReservedValue);
    }

    enum LogType
    {
        ServiceStarted,
        ServiceStopped,
        InternetIsUp,
        InternetIsDown,
        GameRunned,
        GameStopped,
        ServiceStat
    }
}
