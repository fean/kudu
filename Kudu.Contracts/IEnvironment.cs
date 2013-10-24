using System.Collections.Generic;

namespace Kudu.Core
{
    public interface IEnvironment
    {
        string RootPath { get; }                // e.g. /
        string SiteRootPath { get; }            // e.g. /site
        string RepositoryPath { get; set; }     // e.g. /site/repository
        string WebRootPath { get; }             // e.g. /site/wwwroot
        string DeploymentsPath { get; }         // e.g. /site/deployments
        string DeploymentToolsPath { get; }     // e.g. /site/deployments/tools
        string DiagnosticsPath { get; }         // e.g. /site/diagnostics
        string SSHKeyPath { get; }
        string TempPath { get; }
        string ScriptPath { get; }
        string NodeModulesPath { get; }
        string LogFilesPath { get; }            // e.g. /logfiles
        string TracePath { get; }               // e.g. /logfiles/git/trace
        string AnalyticsPath { get; }           // e.g. %temp%/siteExtLogs
        string DeploymentTracePath { get; }     // e.g. /logfiles/git/deployment
        string DataPath { get; }                // e.g. /data
        string AlwaysOnJobsDataPath { get; }    // e.g. /data/jobs/alwaysOn
        string TriggeredJobsDataPath { get; }    // e.g. /data/jobs/alwaysOn

        IEnumerable<string> AlwaysOnJobsPaths { get; }     // e.g. /site/wwwroot/app_data/jobs/alwaysOn  ; /site/jobs/alwaysOn
        IEnumerable<string> TriggeredJobsPaths { get; }    // e.g. /site/wwwroot/app_data/jobs/triggered ; /site/jobs/triggered
    }
}
