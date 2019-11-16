using PoeShared.Scaffolding;

namespace EyeAuras.Shared.Services
{
    public struct WindowMatchParams
    {
        public bool IsEmpty => string.IsNullOrEmpty(Title);

        public string Title { get; set; }
        
        public bool IsRegex { get; set; }

        public override string ToString()
        {
            return this.DumpToTextRaw();
        }
    }
}