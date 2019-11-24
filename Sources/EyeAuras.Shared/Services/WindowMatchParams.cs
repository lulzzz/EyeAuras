using System;
using Newtonsoft.Json;
using PoeShared.Scaffolding;

namespace EyeAuras.Shared.Services
{
    public struct WindowMatchParams
    {
        public bool IsEmpty => string.IsNullOrEmpty(Title);

        public string Title { get; set; }
        
        [JsonIgnore]
        public IntPtr Handle { get; set; }
        
        public bool IsRegex { get; set; }

        public override string ToString()
        {
            return this.DumpToTextRaw();
        }
    }
}