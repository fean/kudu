using System.Net;
using System.Net.Http;
using System.Web.Http;
using Kudu.Contracts.Jobs;
using Kudu.Contracts.Tracing;

namespace Kudu.Services.Jobs
{
    public class JobsController : ApiController
    {
        private readonly ITracer _tracer;
        private readonly IJobsManager _jobsManager;

        public JobsController(ITracer tracer, IJobsManager jobsManager)
        {
            _tracer = tracer;
            _jobsManager = jobsManager;
        }

        [HttpGet]
        public HttpResponseMessage GetAlwaysOnJobs()
        {
            return Request.CreateResponse(HttpStatusCode.OK, _jobsManager.ListAlwaysOnJobs());
        }
    }
}