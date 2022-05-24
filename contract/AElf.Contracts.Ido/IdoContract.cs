using System.Collections.Generic;
using AElf.Contracts.Whitelist;
using AElf.CSharp.Core;
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
            State.WhitelistContract.Value = input.WhitelistContract;
            State.Admin.Value = Context.Sender;
            return new Empty();
        }

        public override Empty Register(RegisterInput input)
        {
            ValidTokenSymbolOwner(input.ProjectCurrency, Context.Sender);
            ValidTokenSymbol(input.AcceptedCurrency);
            Assert(input.MaxSubscription > input.MinSubscription && input.MinSubscription > 0,"Invalid Subscription input");
            Assert(input.StartTime <= input.EndTime && input.StartTime > Context.CurrentBlockTime,"Invalid Time input");
            var id = GetHash(input, Context.Sender);
            var projectInfo = new ProjectInfo()
            {
                ProjectId = id,
                AcceptedCurrency = input.AcceptedCurrency,
                ProjectCurrency = input.ProjectCurrency,
                CrowdFundingType = input.CrowdFundingType,
                CrowdFundingIssueAmount = input.CrowdFundingIssueAmount,
                PreSalePrice = input.PreSalePrice,
                StartTime = input.StartTime,
                EndTime = input.EndTime,
                MinSubscription = input.MinSubscription,
                MaxSubscription = input.MaxSubscription,
                IsEnableWhitelist = input.IsEnableWhitelist,
                WhitelistId = input.WhitelistId,
                IsBurnRestToken = input.IsBurnRestToken,
                AdditionalInfo = input.AdditionalInfo,
                Creator = Context.Sender,
                ToRaisedAmount = input.ToRaisedAmount,
            };
            State.ProjectInfoMap[id] = projectInfo;
            var listInfo = new ProjectListInfo()
            {
                ProjectId = id,
                PublicSalePrice = input.PublicSalePrice,
                LiquidityLockProportion = input.LiquidityLockProportion,
                ListMarketInfo = input.ListMarketInfo,
                UnlockTime = input.UnlockTime,
                LatestPeriod = 0,
                TotalPeriod = input.TotalPeriod,
                FirstDistributeProportion = input.FirstDistributeProportion,
                RestDistributeProportion = input.RestDistributeProportion,
                PeriodDuration = input.PeriodDuration
            };
            State.ProjectListInfoMap[id] = listInfo;
            
            //SubscribeWhiteList
            State.WhitelistContract.SubscribeWhitelist.Send(new SubscribeWhitelistInput()
            {
                ProjectId = id,
                WhitelistId = input.WhitelistId
            });
            
            Context.Fire(new ProjectRegistered()
            {
                ProjectId = id,
                AcceptedCurrency = input.AcceptedCurrency,
                ProjectCurrency = input.ProjectCurrency,
                CrowdFundingType = input.CrowdFundingType,
                CrowdFundingIssueAmount = input.CrowdFundingIssueAmount,
                PreSalePrice = input.PreSalePrice,
                StartTime = input.StartTime,
                EndTime = input.EndTime,
                MinSubscription = input.MinSubscription,
                MaxSubscription = input.MaxSubscription,
                PublicSalePrice = input.PublicSalePrice,
                ListMarketInfo = input.ListMarketInfo,
                LiquidityLockProportion = input.LiquidityLockProportion,
                UnlockTime = input.UnlockTime,
                IsEnableWhitelist = input.IsEnableWhitelist,
                WhitelistId = input.WhitelistId,
                IsBurnRestToken = input.IsBurnRestToken,
                TotalPeriod = input.TotalPeriod,
                AdditionalInfo = input.AdditionalInfo,
                ToRaisedAmount = input.ToRaisedAmount,
                Creator = Context.Sender,
                FirstDistributeProportion = input.FirstDistributeProportion,
                RestDistributeProportion = input.RestDistributeProportion,
                PeriodDuration = input.PeriodDuration
            });
            return new Empty();
        }

        public override Empty AddWhitelists(AddWhitelistsInput input)
        {
            var projectInfo = ValidProjectExist(input.ProjectId);
            Assert(projectInfo.Enabled,"project is not enabled");
            ValidProjectOwner(input.ProjectId);

            var list = new ExtraInfoList();
            foreach (var user in input.Users)
            {
                var userInfo = new ExtraInfo()
                {
                    Address = user
                };
                list.Value.Add(userInfo);
            }
            State.WhitelistContract.AddAddressInfoListToWhitelist.Send(new AddAddressInfoListToWhitelistInput()
            {
                ExtraInfoList = list,
                WhitelistId = projectInfo.WhitelistId
            });
            return new Empty();
        }

        public override Empty RemoveWhitelists(RemoveWhitelistsInput input)
        {
            var projectInfo = ValidProjectExist(input.ProjectId);
            Assert(projectInfo.Enabled,"project is not enabled");
            ValidProjectOwner(input.ProjectId);

            var list = new ExtraInfoList();
            foreach (var user in input.Users)
            {
                var userInfo = new ExtraInfo()
                {
                    Address = user
                };
                list.Value.Add(userInfo);
            }
            State.WhitelistContract.RemoveAddressInfoListFromWhitelist.Send(new RemoveAddressInfoListFromWhitelistInput()
            {
                ExtraInfoList = list,
                WhitelistId = projectInfo.WhitelistId
            });
            return new Empty();
        }

        public override Empty EnableWhitelist(Hash input)
        {
            var projectInfo = ValidProjectExist(input);
            Assert(projectInfo.Enabled,"project is not enabled");
            ValidProjectOwner(input);

            State.ProjectInfoMap[input].IsEnableWhitelist = true;
            State.WhitelistContract.EnableWhitelist.Send(input);
            return new Empty();
        }

        public override Empty DisableWhitelist(Hash input)
        {
            var projectInfo = ValidProjectExist(input);
            Assert(projectInfo.Enabled,"project is not enabled");
            ValidProjectOwner(input);

            State.ProjectInfoMap[input].IsEnableWhitelist = false;
            State.WhitelistContract.DisableWhitelist.Send(input);
            return new Empty();
        }

        public override Empty Invest(InvestInput input)
        {
            //check status
            var projectInfo = ValidProjectExist(input.ProjectId);
            Assert(projectInfo.Enabled,"project is not enabled");
            WhitelistCheck(input.ProjectId, Context.Sender);
            Assert(projectInfo.AcceptedCurrency == input.Currency,"the currency is invalid");
            CheckInvestInput(input.ProjectId, Context.Sender, input.InvestAmount);
            var currentTimestamp = Context.CurrentBlockTime;
            Assert(currentTimestamp >= projectInfo.StartTime && currentTimestamp <= projectInfo.EndTime,"can't invest right now");
            //invest 
            TransferIn(Context.Sender,input.Currency,input.InvestAmount);
            var investDetail =  State.InvestDetailMap[projectInfo.ProjectId][Context.Sender] ?? new InvestDetail()
            {
                InvestSymbol = input.Currency,
                Amount = 0
            };
            var totalInvestAmount = investDetail.Amount.Add(input.InvestAmount);
            investDetail.Amount = totalInvestAmount;
            State.InvestDetailMap[projectInfo.ProjectId][Context.Sender] = investDetail;
            State.ProjectInfoMap[input.ProjectId].CurrentRaisedAmount = State.ProjectInfoMap[input.ProjectId]
                .CurrentRaisedAmount.Add(input.InvestAmount);
            
            var toClaimAmount = ProfitDetailUpdate(input.ProjectId, Context.Sender, totalInvestAmount);
            
            Context.Fire(new Invested()
            {
                ProjectId = input.ProjectId,
                InvestSymbol = input.Currency,
                Amount = input.InvestAmount,
                TotalAmount = totalInvestAmount,
                ProjectCurrency = projectInfo.ProjectCurrency,
                ToClaimAmount = toClaimAmount,
                User = Context.Sender
            });
            return new Empty();
        }

        public override Empty Claim(ClaimInput input)
        {
            //check status
            var projectInfo = ValidProjectExist(input.ProjectId);
            Assert(projectInfo.Enabled,"project is not enabled");

            var listInfo = State.ProjectListInfoMap[input.ProjectId];
            var profitDetailInfo = State.ProfitDetailMap[input.ProjectId][input.User];
            State.ClaimedProfitsInfoMap[input.User] = State.ClaimedProfitsInfoMap[input.User]?? new ClaimedProfitsInfo();
            var claimedProfitsInfo = State.ClaimedProfitsInfoMap[input.User];
            for (var i = profitDetailInfo.LatestPeriod; i < listInfo.LatestPeriod; i++)
            {
                var currentPeriod = i++;
                var profitPeriodAmount = profitDetailInfo.AmountsMap[currentPeriod];
                TransferOut(input.User, profitDetailInfo.Symbol, profitPeriodAmount);
                claimedProfitsInfo.Details.Add(new ClaimedProfit()
                {
                    ProjectId = input.ProjectId,
                    LatestPeriod = currentPeriod,
                    Symbol = profitDetailInfo.Symbol,
                    Amount = profitPeriodAmount
                });
                claimedProfitsInfo.TotalClaimedAmount = claimedProfitsInfo.TotalClaimedAmount.Add(profitPeriodAmount);
                State.ClaimedProfitsInfoMap[input.User] = claimedProfitsInfo;
                Context.Fire(new Claimed()
                {
                    ProjectId = input.ProjectId,
                    LatestPeriod = currentPeriod,
                    Amount = profitPeriodAmount,
                    ProjectCurrency = profitDetailInfo.Symbol,
                    TotalClaimedAmount = claimedProfitsInfo.TotalClaimedAmount,
                    TotalPeriod = listInfo.LatestPeriod,
                    User = input.User
                });
            }
            
            return new Empty();
        }
    }
}