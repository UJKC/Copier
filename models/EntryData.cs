namespace copier.Models
{
    public class EntryData
    {
        public string Title { get; set; } = "";
        public string Text { get; set; } = "";

        // New property for "Pin to Top"
        public bool IsPinned { get; set; } = false;
    }
}
