using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ClockifyHelper.ClockifyModels
{
    public class Time
    {
        [JsonPropertyName("start")]
        public DateTime Start { get; set; }

        [JsonPropertyName("billable")]
        public string Billable { get; set; } = "true";

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("projectId")]
        public string ProjectId { get; set; }

        [JsonPropertyName("taskId")]
        public string TaskId { get; set; }

        [JsonPropertyName("end")]
        public DateTime? End { get; set; }
    }
}
