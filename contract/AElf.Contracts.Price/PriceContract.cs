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
                Value = "100000000"
            };
        }
    }
}