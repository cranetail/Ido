using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Price
{
    public partial class PriceContract : PriceContractContainer.PriceContractBase
    {
        public override Price GetExchangeTokenPriceInfo(GetExchangeTokenPriceInfoInput input)
        {
            return new Price()
            {
                Value = State.Price[input.TokenSymbol].ToString()
            };
        }

        public override Empty SetPrice(SetPriceInput input)
        {
            State.Price[input.TokenSymbol] = input.Price;
            return new Empty();
        }
    }
}