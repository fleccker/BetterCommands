using AdminToys;

using System;

namespace BetterCommands.Arguments.Toys
{
    public struct ToyDisallowedNewArgumentData
    {
        public AdminToyBase Toy { get; }

        public ToyDisallowedNewArgumentData(AdminToyBase toy)
        {
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