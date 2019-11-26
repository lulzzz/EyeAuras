using EyeAuras.Shared;
using shortid;
// ReSharper disable StringLiteralTypo

namespace EyeAuras.UI.Core.Services
{
    internal sealed class UniqueIdGenerator : IUniqueIdGenerator
    {
        public UniqueIdGenerator()
        {
            ShortId.SetCharacters(@"abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ");
        }

        public string Next()
        {
            return ShortId.Generate(true, false, 8);
        }
    }
}