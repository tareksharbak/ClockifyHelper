using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ClockifyHelper.ClockifyModels
{
    public class TimeRead
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("projectId")]
        public string ProjectId { get; set; }

        [JsonPropertyName("taskId")]
        public string TaskId { get; set; }

        [JsonPropertyName("timeInterval")]
        public TimeInterval TimeInterval { get; set; }
    }
}
