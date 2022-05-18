using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Ido
{
    public partial class IdoContract : IdoContractContainer.IdoContractBase
    {
        public override Empty Initialize(InitializeInput input)
        {
            Assert(State.TokenContract.Value == null, "Already initialized.");
            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            State.Admin.Value = Context.Sender;
            return new Empty();
        }

        public override Empty Register(RegisterInput input)
        {
            ValidTokenSymbolOwner(input.ProjectCurrency, Context.Sender);
            ValidTokenSymbol(input.AcceptedCurrency);
            var id = GetHash(input, Context.Sender);
            var projectInfo = new ProjectInfo()
            {
                ProjectItemId = id,
                AcceptedCurrency = input.AcceptedCurrency,
                ProjectCurrency = input.ProjectCurrency,
                CrowdFundingType = input.CrowdFundingType,
                DistributionTotalAmount = input.DistributionAmount,
                PreSalePrice = input.PreSalePrice,
                StartTime = input.StartTime,
                EndTime = input.EndTime,
                MinSubscription = input.MinSubscription,
                MaxSubscription = input.MaxSubscription,
                CurrentPeriod = 0,
                TotalPeriod = input.TotalPeriod,
                IsEnableWhitelist = input.IsEnableWhitelist,
                WhitelistId = input.WhitelistId,
                IsBurnRestToken = input.IsBurnRestToken,
                AdditionalInfo = input.AdditionalInfo,
                Creator = Context.Sender
            };
            State.ProjectInfoMap[id] = projectInfo;
            var listInfo = new ProjectListInfo()
            {
                ProjectItemId = id,
                PublicSalePrice = input.PublicSalePrice,
                LiquidityLockProportion = input.LiquidityLockProportion,
                ListMarketInfo = input.ListMarketInfo,
                UnlockTime = input.UnlockTime
            };
            State.ProjectListInfoMap[id] = listInfo;
            return new Empty();
        }
        
    }
}