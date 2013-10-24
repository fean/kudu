using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Kudu.Contracts.Jobs
{
    public abstract class Job
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "status")]
        [JsonConverter(typeof(StringEnumConverter))]
        public JobStatus JobStatus { get; set; }

        [DataMember(Name = "runCommand")]
        public string RunCommand { get; set; }

        [DataMember(Name = "url")]
        public Uri Url { get; set; }
    }

    public class AlwaysOnJob : Job
    {
    }

    public class TriggerJob
    {
        public int RunCount { get; set; }
    }
}
