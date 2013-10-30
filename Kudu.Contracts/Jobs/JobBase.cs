using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Kudu.Contracts.Tracing;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Kudu.Contracts.Jobs
{
    [DataContract]
    public abstract class JobBase
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "status")]
        [JsonConverter(typeof(StringEnumConverter))]
        public JobStatus JobStatus { get; set; }

        [DataMember(Name = "runCommand")]
        public string ScriptFilePath { get; set; }

        [DataMember(Name = "url")]
        public Uri Url { get; set; }

        public IScriptHost ScriptHost { get; set; }
    }

    [DataContract]
    public class AlwaysOnJob : JobBase
    {
    }

    [DataContract]
    public class TriggeredJob : JobBase
    {
        public int RunCount { get; set; }
    }

    [DataContract]
    public class AllJobs
    {
        [DataMember(Name = "alwaysOnJobs")]
        public IEnumerable<AlwaysOnJob> AlwaysOnJobs { get; set; }

        [DataMember(Name = "triggeredJobs")]
        public IEnumerable<TriggeredJob> TriggeredJobs { get; set; }
    }

    public interface IScriptHost
    {
        string HostPath { get; }

        string ArgumentsFormat { get; }

        void RunScript(ITracer tracer, string scriptFileName, string workingDirectory, Action<string> onWriteOutput, Action<string> onWriteError, TimeSpan timeout);
    }
}
