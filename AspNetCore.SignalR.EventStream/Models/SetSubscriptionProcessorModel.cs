namespace AspNetCore.SignalR.EventStream.Models
{
    public class SetSubscriptionProcessorModel
    {
        public bool? Start {  get; set; }
        public bool? Stop { get; set; } = false;
    }
}
