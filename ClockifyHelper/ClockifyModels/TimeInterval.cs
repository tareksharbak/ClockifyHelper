using System;
using System.Text.Json.Serialization;

namespace ClockifyHelper.ClockifyModels
{
    public class TimeInterval
    {
        [JsonPropertyName("start")]
        public DateTime Start { get; set; }

        [JsonPropertyName("end")]
        public DateTime? End { get; set; }
    }
}
