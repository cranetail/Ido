using AElf.Types;
using Gandalf.Contracts.InterestRateModel;
using Google.Protobuf.WellKnownTypes;

namespace Gandalf.Contracts.GToken
{
    public partial class GTokenContract
    {
        public override Int64Value GetSupplyRatePerBlock(Address input)
        {
            return new Int64Value()
            {
                Value = GetSupplyRatePerBlockInternal(input)
            };
        }
        

        public override Int64Value GetBorrowRatePerBlock(Address input)
        {
            return new Int64Value()
            {
                Value = GetBorrowRatePerBlockInternal(input)
            };
        }

        public override Int64Value GetExchangeRateStored(Address input)
        {
            return new Int64Value()
            {
                Value = ExchangeRateStoredInternal(input)
            };
        }
    }
}