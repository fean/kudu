using System.Collections.Generic;

namespace Kudu.Contracts.Jobs
{
    public interface IJobsManager
    {
        IEnumerable<AlwaysOnJob> ListAlwaysOnJobs();
        IEnumerable<TriggeredJob> ListTriggeredJobs();
    }
}