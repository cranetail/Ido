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

        public override Hash GetWhitelistId(Hash input)
        {
            return State.WhiteListIdMap[input];
        }

        public override ProjectInfo GetProjectInfo(Hash input)
        {
            return State.ProjectInfoMap[input];
        }

        public override InvestDetail GetInvestDetail(GetInvestDetailInput input)
        {
            return State.InvestDetailMap[input.ProjectId][input.User];
        }

        public override ProfitDetail GetProfitDetail(GetProfitDetailInput input)
        {
            return State.ProfitDetailMap[input.ProjectId][input.User];
        }

        public override ProjectListInfo GetProjectListInfo(Hash input)
        {
            return State.ProjectListInfoMap[input];
        }

        public override Address GetAdmin(Empty input)
        {
            return State.Admin.Value;
        }

        public override LiquidatedDamageDetails GetLiquidatedDamageDetails(Hash input)
        {
            return State.LiquidatedDamageDetailsMap[input];
        }
    }
}