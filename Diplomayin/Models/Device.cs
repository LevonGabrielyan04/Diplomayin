namespace Diplomayin.Models
{
    public class Device
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string OperatingSystem { get; set; }
        public string Configuration { get; set; } //JSON formatov
        public bool IsCompliant { get; set; }
        public string MaintenanceTimes { get;set; }//JSON formatov
        public string IPAddress { get; set; } // New property to store the device's address

    }
}
