using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Ido
{
    public partial class IdoContract
    {
        public override Address GetWhitelistContractAddress(Empty input)
        {
            return State.WhitelistContract.Value;
        }

        public override Address GetTokenAddress(Empty input)
        {
            return State.TokenContract.Value;
        }

        public override ExtraInfoIdList GetWhitelist(Hash input)
        {
            return base.GetWhitelist(input);
        }

        public override Hash GetWhitelistId(Hash input)
        {
            return State.WhiteListIdMap[input];
        }
    }
}