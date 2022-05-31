using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Whitelist
{
    public partial class WhitelistContract : WhitelistContractContainer.WhitelistContractBase
    {
        public override Hash CreateWhitelist(CreateWhitelistInput input)
        {
            return new Hash();
        }
    }


}