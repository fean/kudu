using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using Kudu.Core.Infrastructure;

namespace Kudu.Core
{
    public class Environment : IEnvironment
    {
        private readonly IFileSystem _fileSystem;
        private readonly string _webRootPath;
        private readonly string _deploymentsPath;
        private readonly string _deploymentToolsPath;
        private readonly string _diagnosticsPath;
        private readonly string _sshKeyPath;
        private readonly string _tempPath;
        private readonly string _scriptPath;
        private readonly string _nodeModulesPath;
        private string _repositoryPath;
        private readonly string _logFilesPath;
        private readonly string _tracePath;
        private readonly string _analyticsPath;
        private readonly string _deploymentTracePath;
        private readonly string _dataPath;
        private readonly string _alwaysOnJobsDataPath;
        private readonly string _triggeredJobsDataPath;
        private readonly string[] _alwaysOnJobsPaths;
        private readonly string[] _triggeredJobsPaths;

        // This ctor is used only in unit tests
        public Environment(
                IFileSystem fileSystem,
                string rootPath,
                string siteRootPath,
                string tempPath,
                string repositoryPath,
                string webRootPath,
                string deploymentsPath,
                string diagnosticsPath,
                string sshKeyPath,
                string scriptPath,
                string nodeModulesPath,
                string dataPath)
        {
            if (fileSystem == null)
            {
                throw new ArgumentNullException("fileSystem");
            }
            if (repositoryPath == null)
            {
                throw new ArgumentNullException("repositoryPath");
            }

            _fileSystem = fileSystem;
            RootPath = rootPath;
            SiteRootPath = siteRootPath;
            _tempPath = tempPath;
            _repositoryPath = repositoryPath;
            _webRootPath = webRootPath;
            _deploymentsPath = deploymentsPath;
            _deploymentToolsPath = Path.Combine(_deploymentsPath, Constants.DeploymentToolsPath);
            _diagnosticsPath = diagnosticsPath;
            _sshKeyPath = sshKeyPath;
            _scriptPath = scriptPath;
            _nodeModulesPath = nodeModulesPath;

            _dataPath = dataPath;

            string alwaysOnJobsPath = Path.Combine(_dataPath, Constants.JobsPath, Constants.AlwaysOnPath);
            string triggeredJobsPath = Path.Combine(_dataPath, Constants.JobsPath, Constants.TriggeredPath);

            _alwaysOnJobsDataPath = alwaysOnJobsPath;
            _triggeredJobsDataPath = triggeredJobsPath;

            _alwaysOnJobsPaths = new string[]
            {
                alwaysOnJobsPath
            };

            _triggeredJobsPaths = new string[]
            {
                triggeredJobsPath
            };

            _logFilesPath = Path.Combine(rootPath, Constants.LogFilesPath);
            _tracePath = Path.Combine(rootPath, Constants.TracePath);
            _analyticsPath = Path.Combine(tempPath ?? _logFilesPath, Constants.SiteExtensionLogsDirectory);
            _deploymentTracePath = Path.Combine(rootPath, Constants.DeploymentTracePath);
        }

        public Environment(
                IFileSystem fileSystem,
                string rootPath,
                string binPath,
                string repositoryPath)
        {
            _fileSystem = fileSystem;
            RootPath = rootPath;

            SiteRootPath = Path.Combine(rootPath, Constants.SiteFolder);

            _tempPath = Path.GetTempPath();
            _repositoryPath = repositoryPath;
            _webRootPath = Path.Combine(SiteRootPath, Constants.WebRoot);
            _deploymentsPath = Path.Combine(SiteRootPath, Constants.DeploymentCachePath);
            _deploymentToolsPath = Path.Combine(_deploymentsPath, Constants.DeploymentToolsPath);
            _diagnosticsPath = Path.Combine(SiteRootPath, Constants.DiagnosticsPath);
            _sshKeyPath = Path.Combine(rootPath, Constants.SSHKeyPath);
            _scriptPath = Path.Combine(binPath, Constants.ScriptsPath);
            _nodeModulesPath = Path.Combine(binPath, Constants.NodeModulesPath);
            _logFilesPath = Path.Combine(rootPath, Constants.LogFilesPath);
            _tracePath = Path.Combine(rootPath, Constants.TracePath);
            _analyticsPath = Path.Combine(_tempPath ?? _logFilesPath, Constants.SiteExtensionLogsDirectory);
            _deploymentTracePath = Path.Combine(rootPath, Constants.DeploymentTracePath);

            string alwaysOnJobsPath = Path.Combine(Constants.JobsPath, Constants.AlwaysOnPath);
            string triggeredJobsPath = Path.Combine(Constants.JobsPath, Constants.TriggeredPath);

            _dataPath = Path.Combine(rootPath, Constants.DataPath);
            _alwaysOnJobsDataPath = Path.Combine(_dataPath, alwaysOnJobsPath);
            _triggeredJobsDataPath = Path.Combine(_dataPath, triggeredJobsPath);

            _alwaysOnJobsPaths = new string[]
            {
                Path.Combine(_webRootPath, Constants.AppDataPath, alwaysOnJobsPath),
                Path.Combine(SiteRootPath, alwaysOnJobsPath)
            };

            _triggeredJobsPaths = new string[]
            {
                Path.Combine(_webRootPath, Constants.AppDataPath, triggeredJobsPath),
                Path.Combine(SiteRootPath, triggeredJobsPath)
            };
        }

        public string RepositoryPath
        {
            get
            {
                return FileSystemHelpers.EnsureDirectory(_fileSystem, _repositoryPath);
            }
            set
            {
                // normalize the '/' to '\'
                _repositoryPath = Path.GetFullPath(value);
            }
        }

        public string WebRootPath
        {
            get
            {
                return FileSystemHelpers.EnsureDirectory(_fileSystem, _webRootPath);
            }
        }

        public string DeploymentsPath
        {
            get
            {
                return FileSystemHelpers.EnsureDirectory(_fileSystem, _deploymentsPath);
            }
        }

        public string DeploymentToolsPath
        {
            get
            {
                return FileSystemHelpers.EnsureDirectory(_fileSystem, _deploymentToolsPath);
            }
        }

        public string DiagnosticsPath
        {
            get
            {
                return FileSystemHelpers.EnsureDirectory(_fileSystem, _diagnosticsPath);
            }
        }

        public string SSHKeyPath
        {
            get
            {
                return FileSystemHelpers.EnsureDirectory(_fileSystem, _sshKeyPath);
            }
        }

        public string RootPath
        {
            get;
            private set;
        }

        public string SiteRootPath
        {
            get;
            private set;
        }

        public string TempPath
        {
            get
            {
                return _tempPath;
            }
        }

        public string ScriptPath
        {
            get
            {
                return _scriptPath;
            }
        }

        public string NodeModulesPath
        {
            get
            {
                return _nodeModulesPath;
            }
        }

        public string LogFilesPath
        {
            get
            {
                return _logFilesPath;
            }
        }

        public string TracePath
        {
            get
            {
                return FileSystemHelpers.EnsureDirectory(_fileSystem, _tracePath);
            }
        }

        public string AnalyticsPath
        {
            get
            {
                return FileSystemHelpers.EnsureDirectory(_fileSystem, _analyticsPath);
            }
        }

        public string DeploymentTracePath
        {
            get
            {
                return FileSystemHelpers.EnsureDirectory(_fileSystem, _deploymentTracePath);
            }
        }

        public string DataPath
        {
            get
            {
                return FileSystemHelpers.EnsureDirectory(_fileSystem, _dataPath);
            }
        }

        public string AlwaysOnJobsDataPath
        {
            get
            {
                return FileSystemHelpers.EnsureDirectory(_fileSystem, _alwaysOnJobsDataPath);
            }
        }

        public string TriggeredJobsDataPath
        {
            get
            {
                return FileSystemHelpers.EnsureDirectory(_fileSystem, _triggeredJobsDataPath);
            }
        }

        public IEnumerable<string> AlwaysOnJobsPaths
        {
            get { return _alwaysOnJobsPaths; }
        }

        public IEnumerable<string> TriggeredJobsPaths
        {
            get { return _triggeredJobsPaths; }
        }
    }
}
