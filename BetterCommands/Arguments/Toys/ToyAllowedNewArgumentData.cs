using AdminToys;

using System;

namespace BetterCommands.Arguments.Toys
{
    public struct ToyAllowedNewArgumentData
    {
        public bool IsNew { get; }

        public AdminToyBase Toy { get; }

        public ToyAllowedNewArgumentData(bool isNew, AdminToyBase toy)
        {
            IsNew = isNew;
            Toy = toy;
        }

        public bool IfIs<TToy>(Action<TToy> execute) where TToy : AdminToyBase
        {
            if (Toy is TToy toy)
            {
                execute?.Invoke(toy);
                return true;
            }

            return false;
        }
    }
}