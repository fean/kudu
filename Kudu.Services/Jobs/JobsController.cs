using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Kudu.Contracts.Jobs;
using Kudu.Contracts.Tracing;
using Kudu.Services.Infrastructure;

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
            IEnumerable<AlwaysOnJob> alwaysOnJobs = GetJobs(_jobsManager.ListAlwaysOnJobs);

            return Request.CreateResponse(HttpStatusCode.OK, alwaysOnJobs);
        }

        [HttpGet]
        public HttpResponseMessage GetTriggeredJobs()
        {
            IEnumerable<TriggeredJob> triggeredJobs = GetJobs(_jobsManager.ListTriggeredJobs);

            return Request.CreateResponse(HttpStatusCode.OK, triggeredJobs);
        }

        [HttpGet]
        public HttpResponseMessage GetAllJobs()
        {
            IEnumerable<AlwaysOnJob> alwaysOnJobs = GetJobs(_jobsManager.ListAlwaysOnJobs);
            IEnumerable<TriggeredJob> triggeredJobs = GetJobs(_jobsManager.ListTriggeredJobs);

            var allJobs = new AllJobs()
            {
                AlwaysOnJobs = alwaysOnJobs,
                TriggeredJobs = triggeredJobs
            };

            return Request.CreateResponse(HttpStatusCode.OK, allJobs);
        }

        private IEnumerable<TJob> GetJobs<TJob>(Func<IEnumerable<TJob>> getJobsFunc) where TJob : JobBase
        {
            IEnumerable<TJob> jobs = getJobsFunc();

            foreach (var job in jobs)
            {
                UpdateJobUrl(job, Request);
            }

            return jobs;
        }

        private void UpdateJobUrl(JobBase job, HttpRequestMessage request)
        {
            job.Url = UriHelper.MakeRelative(Request.RequestUri, job.Name);
        }
    }
}