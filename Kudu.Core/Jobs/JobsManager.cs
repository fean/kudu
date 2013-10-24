using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
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
            var alwaysOnJobs = new List<AlwaysOnJob>();

            foreach (string alwaysOnJobsPath in _environment.AlwaysOnJobsPaths)
            {
                if (!_fileSystem.Directory.Exists(alwaysOnJobsPath))
                {
                    continue;
                }

                DirectoryInfoBase alwaysOnJobsDirectory = _fileSystem.DirectoryInfo.FromDirectoryName(alwaysOnJobsPath);
                DirectoryInfoBase[] jobDirectories = alwaysOnJobsDirectory.GetDirectories("*", SearchOption.TopDirectoryOnly);
                foreach (DirectoryInfoBase jobDirectory in jobDirectories)
                {
                    AlwaysOnJob alwaysOnJob = BuildAlwaysOnJob(jobDirectory);
                    if (alwaysOnJob != null)
                    {
                        alwaysOnJobs.Add(alwaysOnJob);
                    }
                }
            }

            return alwaysOnJobs;
        }

        private AlwaysOnJob BuildAlwaysOnJob(DirectoryInfoBase jobDirectory)
        {
            string jobName = jobDirectory.Name;
            FileInfoBase[] files = jobDirectory.GetFiles("*.*", SearchOption.TopDirectoryOnly);
            string runCommand = FindCommandToRun(files);

            return new AlwaysOnJob()
            {
                Name = jobName,
                RunCommand = runCommand
            };
        }

        private string FindCommandToRun(FileInfoBase[] files)
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
                        return supportedFile.FullName;
                    }
                }
            }

            return null;
        }
    }

    public abstract class ScriptHostBase
    {
        protected ScriptHostBase(string hostPath)
        {
            this.HostPath = hostPath;
        }

        public string HostPath { get; private set; }

        public virtual bool IsSupported
        {
            get { return !string.IsNullOrEmpty(HostPath); }
        }

        public abstract IEnumerable<string> SupportedExtensions { get; }
    }

    public class WindowsScriptHost : ScriptHostBase
    {
        private static readonly string[] Supported = { ".cmd", ".bat", ".exe" };

        public WindowsScriptHost()
            : base("cmd")
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
