using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Kudu.Contracts.Jobs;
using Kudu.Contracts.Tracing;
using Kudu.Core.Infrastructure;
using Microsoft.Win32;

namespace Kudu.Core.Jobs
{
    public class JobsManager : IJobsManager
    {
        private const string DefaultScriptFileName = "run";

        private static readonly ScriptHostBase[] ScriptHosts = new ScriptHostBase[]
        {
            new WindowsScriptHost(),
            new BashScriptHost(),
            new PythonScriptHost(),
            new PhpScriptHost(),
            new NodeScriptHost()
        };

        private readonly IEnvironment _environment;
        private readonly IFileSystem _fileSystem;
        private readonly ITracer _tracer;

        public JobsManager(ITracer tracer, IEnvironment environment, IFileSystem fileSystem)
        {
            _tracer = tracer;
            _environment = environment;
            _fileSystem = fileSystem;
        }

        public IEnumerable<AlwaysOnJob> ListAlwaysOnJobs()
        {
            return ListJobs(_environment.AlwaysOnJobsPaths, BuildAlwaysOnJob);
        }

        public IEnumerable<TriggeredJob> ListTriggeredJobs()
        {
            return ListJobs(_environment.TriggeredJobsPaths, BuildTriggeredJob);
        }

        public async Task InvokeTriggeredJob(string name)
        {
        }

        private IEnumerable<TJob> ListJobs<TJob>(IEnumerable<string> jobsPaths, Func<DirectoryInfoBase, TJob> buildJobFunc) where TJob : JobBase, new()
        {
            var jobs = new List<TJob>();

            foreach (string jobsPath in jobsPaths)
            {
                if (!_fileSystem.Directory.Exists(jobsPath))
                {
                    continue;
                }

                DirectoryInfoBase jobsDirectory = _fileSystem.DirectoryInfo.FromDirectoryName(jobsPath);
                DirectoryInfoBase[] jobDirectories = jobsDirectory.GetDirectories("*", SearchOption.TopDirectoryOnly);
                foreach (DirectoryInfoBase jobDirectory in jobDirectories)
                {
                    TJob job = buildJobFunc(jobDirectory);
                    if (job != null)
                    {
                        jobs.Add(job);
                    }
                }
            }

            return jobs;
        }

        private TriggeredJob BuildTriggeredJob(DirectoryInfoBase jobDirectory)
        {
            return BuildJob<TriggeredJob>(jobDirectory);
        }

        private AlwaysOnJob BuildAlwaysOnJob(DirectoryInfoBase jobDirectory)
        {
            return BuildJob<AlwaysOnJob>(jobDirectory);
        }

        private TJob BuildJob<TJob>(DirectoryInfoBase jobDirectory) where TJob : JobBase, new()
        {
            string jobName = jobDirectory.Name;
            FileInfoBase[] files = jobDirectory.GetFiles("*.*", SearchOption.TopDirectoryOnly);
            IScriptHost scriptHost;
            string runCommand = FindCommandToRun(files, out scriptHost);

            if (runCommand == null)
            {
                return null;
            }

            return new TJob()
            {
                Name = jobName,
                ScriptFilePath = runCommand,
                ScriptHost = scriptHost
            };
        }

        private string FindCommandToRun(FileInfoBase[] files, out IScriptHost scriptHostFound)
        {
            foreach (ScriptHostBase scriptHost in ScriptHosts)
            {
                foreach (string supportedExtension in scriptHost.SupportedExtensions)
                {
                    var supportedFiles = files.Where(f => String.Equals(f.Extension, supportedExtension, StringComparison.OrdinalIgnoreCase));
                    if (supportedFiles.Any())
                    {
                        var scriptFound = supportedFiles.FirstOrDefault(f => String.Equals(f.Name, DefaultScriptFileName, StringComparison.OrdinalIgnoreCase));
                        var supportedFile = scriptFound ?? supportedFiles.First();
                        scriptHostFound = scriptHost;
                        return supportedFile.FullName;
                    }
                }
            }

            scriptHostFound = null;

            return null;
        }

        public class TriggeredJobRunner
        {
            private const string JobEnvironmentKeyPrefix = "WEBSITE_JOB_RUNNING_";

            private readonly IEnvironment _environment;
            private readonly IFileSystem _fileSystem;

            private string _jobBinariesPath;
            private string _workingDirectory;
            private string _tempJobPath;
            private int _lastHash;
            private object _cacheLock = new object();

            public TriggeredJob Job { get; private set; }

            public TriggeredJobRunner(TriggeredJob job, IEnvironment environment, IFileSystem fileSystem)
            {
                _environment = environment;
                _fileSystem = fileSystem;

                Job = job;

                _jobBinariesPath = Path.GetDirectoryName(Job.ScriptFilePath);
                _tempJobPath = Path.Combine(_environment.TempPath, Constants.TriggeredPath, Job.Name);

                if (!_fileSystem.File.Exists(Job.ScriptFilePath))
                {
                    //Status = "Missing run_worker.cmd file";
                    //Trace.TraceError(Status);
                    //return;
                }
            }

            private int CalculateHashForJob()
            {
                var updateDatesString = new StringBuilder();
                DirectoryInfoBase jobBinariesDirectory = _fileSystem.DirectoryInfo.FromDirectoryName(_jobBinariesPath);
                FileInfoBase[] files = jobBinariesDirectory.GetFiles("*.*", SearchOption.AllDirectories);
                foreach (FileInfoBase file in files)
                {
                    updateDatesString.Append(file.LastWriteTimeUtc.Ticks);
                }

                return updateDatesString.ToString().GetHashCode();
            }

            private void CacheJobBinaries(ITracer tracer)
            {
                lock (_cacheLock)
                {
                    var currentHash = CalculateHashForJob();

                    if (_lastHash == currentHash)
                    {
                        return;
                    }

                    SafeKillAllRunningJobInstances(tracer);

                    if (_fileSystem.Directory.Exists(_tempJobPath))
                    {
                        FileSystemHelpers.DeleteDirectoryContentsSafe(_tempJobPath, true);
                    }

                    if (_fileSystem.Directory.Exists(_tempJobPath))
                    {
                        tracer.TraceWarning("Failed to delete temporary directory");
                    }

                    try
                    {
                        var tempJobInstancePath = Path.Combine(_tempJobPath, Path.GetRandomFileName());

                        FileSystemHelpers.CopyDirectoryRecursive(_fileSystem, _jobBinariesPath, tempJobInstancePath);

                        _workingDirectory = tempJobInstancePath;

                        _lastHash = currentHash;
                    }
                    catch (Exception ex)
                    {
                        //Status = "Worker is not running due to an error";
                        //TraceError("Failed to copy bin directory: " + ex);
                        tracer.TraceError("Failed to copy job files: " + ex);

                        // job disabled
                        _workingDirectory = null;
                    }
                }
            }

            public string GetJobEnvironmentKey()
            {
                return JobEnvironmentKeyPrefix + Job.Name;
            }

            public async Task RunJobInstance(ITracer tracer)
            {
                // TODO: Use actual async code
                await Task.Factory.StartNew(() =>
                {
                    CacheJobBinaries(tracer);

                    if (_workingDirectory == null)
                    {
                        return;
                    }

                    using (tracer.Step("Run script '{0}' with script host - '{1}'".FormatCurrentCulture(Job.ScriptFilePath, Job.ScriptHost.GetType())))
                    {
                        try
                        {
                            var exe = new Executable(Job.ScriptHost.HostPath, _workingDirectory, TimeSpan.MaxValue);
                            exe.EnvironmentVariables[GetJobEnvironmentKey()] = "true";
                            exe.ExecuteWithoutIdleManager(tracer, (message) => tracer.Trace(message), tracer.TraceError, TimeSpan.MaxValue,
                                                            Job.ScriptHost.ArgumentsFormat, Job.ScriptFilePath);
                        }
                        catch (Exception ex)
                        {
                            tracer.TraceError(ex);
                        }
                    }
                });
            }

            public void SafeKillAllRunningJobInstances(ITracer tracer)
            {
                try
                {
                    Process[] processes = Process.GetProcesses();
                    foreach (Process process in processes)
                    {
                        StringDictionary processEnvironment = ProcessEnvironment.TryGetEnvironmentVariables(process);
                        if (processEnvironment != null && processEnvironment.ContainsKey(GetJobEnvironmentKey()))
                        {
                            try
                            {
                                process.Kill();
                            }
                            catch (Exception ex)
                            {
                                if (!process.HasExited)
                                {
                                    tracer.TraceError("Failed to kill process - {0} for job - {1}\n{2}".FormatInvariant(process.ProcessName, Job.Name,
                                                                                                                        ex));
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    tracer.TraceError(ex);
                }
            }
        }
    }

    public abstract class ScriptHostBase : IScriptHost
    {
        private const string JobEnvironmentKey = "WEBSITE_JOB_RUNNING";

        protected ScriptHostBase(string hostPath, string argumentsFormat = "{0}")
        {
            HostPath = hostPath;
            ArgumentsFormat = argumentsFormat;
        }

        public string HostPath { get; private set; }

        public string ArgumentsFormat { get; private set; }

        public virtual bool IsSupported
        {
            get { return !string.IsNullOrEmpty(HostPath); }
        }

        public abstract IEnumerable<string> SupportedExtensions { get; }

        public void RunScript(ITracer tracer, string scriptFileName, string workingDirectory, Action<string> onWriteOutput, Action<string> onWriteError, TimeSpan timeout)
        {
            using (tracer.Step("Run script '{0}' with script host - '{1}'".FormatCurrentCulture(scriptFileName, GetType())))
            {
                try
                {
                    var exe = new Executable(HostPath, workingDirectory, TimeSpan.MaxValue);
                    exe.EnvironmentVariables[JobEnvironmentKey] = "true";
                    exe.ExecuteWithoutIdleManager(tracer, onWriteOutput, onWriteError, timeout, ArgumentsFormat, scriptFileName);
                }
                catch (Exception ex)
                {
                    tracer.TraceError(ex);
                }
            }
        }
    }

    public class WindowsScriptHost : ScriptHostBase
    {
        private static readonly string[] Supported = { ".cmd", ".bat", ".exe" };

        public WindowsScriptHost()
            : base("cmd", "/c {0}")
        {
        }

        public override IEnumerable<string> SupportedExtensions
        {
            get { return Supported; }
        }
    }

    public class NodeScriptHost : ScriptHostBase
    {
        private static readonly string[] Supported = { ".js" };

        public NodeScriptHost()
            : base(DiscoverHostPath())
        {
        }

        private static string DiscoverHostPath()
        {
            return PathUtility.ResolveNodePath();
        }

        public override IEnumerable<string> SupportedExtensions
        {
            get { return Supported; }
        }
    }

    public class BashScriptHost : ScriptHostBase
    {
        private static readonly string[] Supported = { ".sh" };

        public BashScriptHost()
            : base("bash")
        {
        }

        public override IEnumerable<string> SupportedExtensions
        {
            get { return Supported; }
        }
    }

    public class PhpScriptHost : ScriptHostBase
    {
        private static readonly string[] Supported = { ".php" };

        public PhpScriptHost()
            : base(DiscoverHostPath())
        {
        }

        private static string DiscoverHostPath()
        {
            string phpExePath = null;
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\PHP"))
            {
                if (key != null)
                {
                    phpExePath = Path.Combine((string)key.GetValue("InstallDir"), "php.exe");
                }
            }

            return phpExePath;
        }

        public override IEnumerable<string> SupportedExtensions
        {
            get { return Supported; }
        }
    }

    public class PythonScriptHost : ScriptHostBase
    {
        private static readonly string[] Supported = { ".py" };

        public PythonScriptHost()
            : base(DiscoverHostPath())
        {
        }

        private static string DiscoverHostPath()
        {
            string pythonExePath = null;
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\Python.exe"))
            {
                if (key != null)
                {
                    pythonExePath = (string)key.GetValue(null);
                }
            }

            return pythonExePath;
        }

        public override IEnumerable<string> SupportedExtensions
        {
            get { return Supported; }
        }
    }
}
