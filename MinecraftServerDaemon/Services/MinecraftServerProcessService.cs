using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MinecraftServerDaemon.Settings;

namespace MinecraftServerDaemon.Services
{
    public class MinecraftServerProcessService
    {
        public enum MinecraftServerStatus
        {
            Stopped,
            Starting,
            Running,
            Stopping,
            Error
        }

        public enum MinecraftServerCommand
        {
            Stop,
            List,
        }

        private readonly ILogger<MinecraftServerProcessService> _logger;
        private Process _process;
        private readonly object _processLock = new object();
        private readonly ProcessStartInfo _startInfo;

        public MinecraftServerStatus ServerStatus { get; private set; } =
            MinecraftServerStatus.Stopped;

        public MinecraftServerProcessService(IOptions<MinecraftServerInfo> options, ILogger<MinecraftServerProcessService> logger)
        {
            _logger = logger;
            _startInfo = new ProcessStartInfo
            {
                FileName = $"{options.Value.ServerDirectory}/{options.Value.ServerExecutable}",
                WorkingDirectory = options.Value.ServerDirectory,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
            };

            _startInfo.EnvironmentVariables.Add("LD_LIBRARY_PATH", options.Value.ServerDirectory);
        }

        public void Start()
        {
            lock (_processLock)
            {
                if (ServerStatus != MinecraftServerStatus.Stopped)
                {
                    return;
                }

                ServerStatus = MinecraftServerStatus.Starting;
            }

            _process = new Process
            {
                StartInfo = _startInfo,
                EnableRaisingEvents = true
            };

            _process.Exited += OnProcessExited;
            _process.OutputDataReceived += Process_OutputDataReceived;
            _process.ErrorDataReceived += Process_ErrorDataReceived;

            _processOutput.Clear();
            bool started = _process.Start();
            if (started)
            {
                ServerStatus = MinecraftServerStatus.Running;
                _process.BeginErrorReadLine();
                _process.BeginOutputReadLine();

                _timer = new Timer(PeriodicTasks, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
            }
            else
            {
                ServerStatus = MinecraftServerStatus.Error;
            }
        }

        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                _logger.LogError($"[stderr]:{e.Data.Trim()}");
            }
        }

        private readonly List<string> _processOutput = new List<string>();

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                _processOutput.Add(e.Data);
                ProcessOutput(e.Data);
                _logger.LogInformation($"[stdout]:{e.Data.Trim()}");
            }
        }

        public int MaxPlayers { get; private set; }
        public int OnlinePlayers { get; private set; }

        private const string RegexStrPlayersOnline = @"There are (?<online>\d+)/(?<max>\d+) players online:";
        private static readonly Regex RegexPlayersOnline = new Regex(RegexStrPlayersOnline, RegexOptions.Compiled);
        private Timer _timer;

        private void ProcessOutput(string output)
        {
            var match = RegexPlayersOnline.Match(output);
            //var matches = Regex.Matches(output, RegexPlayersOnline);
            if (match.Success)
            {
                OnlinePlayers = int.Parse(match.Groups["online"].Value);
                MaxPlayers = int.Parse(match.Groups["max"].Value);
            }
        }

        private void OnProcessExited(object sender, EventArgs e)
        {
            lock (_processLock)
            {
                ServerStatus = MinecraftServerStatus.Stopped;
                _process.OutputDataReceived -= Process_OutputDataReceived;
                _process.ErrorDataReceived -= Process_ErrorDataReceived;
                _process = null;
            }

            _logger.LogInformation("Minecraft exited");
        }

        public void Stop(bool gracefully)
        {
            lock (_processLock)
            {
                if (ServerStatus != MinecraftServerStatus.Running)
                {
                    return;
                }

                ServerStatus = MinecraftServerStatus.Stopping;
            }

            _timer?.Change(Timeout.Infinite, 0);
            if (gracefully)
            {
                _process.StandardInput.WriteLine("stop");
            }
            else
            {
                _process.Kill();
            }
        }

        public Task<bool> WaitForProcessExitAsync(int waitTime = -1)
        {
            lock (_processLock)
            {
                if (ServerStatus != MinecraftServerStatus.Stopping)
                    return Task.FromResult(false);
            }

            return Task.Run(() =>
            {
                if (waitTime == -1)
                {
                    _process.WaitForExit();
                }
                else
                {
                    _process.WaitForExit(waitTime);
                }
                return _process?.HasExited ?? true;
            });
        }

        public List<string> GetOutput()
        {
            return new List<string>(_processOutput);
        }

        public bool ExecuteCommand(MinecraftServerCommand command)
        {
            if (ServerStatus != MinecraftServerStatus.Running)
                return false;

            switch (command)
            {
                case MinecraftServerCommand.Stop:
                    _process.StandardInput.WriteLine("stop");
                    break;
                case MinecraftServerCommand.List:
                    _process.StandardInput.WriteLine("list");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(command), command, null);
            }

            return true;
        }

        private void CheckPlayers()
        {
            ExecuteCommand(MinecraftServerCommand.List);
        }

        private void PeriodicTasks(object state)
        {
            CheckPlayers();
        }
    }
}
