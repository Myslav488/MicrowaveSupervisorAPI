namespace MicrowaveSupervisorAPI.Server.Models
{
    public class MicrowaveStatus
    {
        public bool IsDoorOpen { get; set; }
        public bool IsHeaterRunning { get; set; }
        public bool IsLightOn { get; set; }
        public int RemainingCookingTimeSeconds { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
