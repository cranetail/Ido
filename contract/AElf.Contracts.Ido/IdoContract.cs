using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Whitelist;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Sdk.CSharp;
using AElf.Types;
using Awaken.Contracts.Swap;
using Google.Protobuf;
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
                IsBurnRestToken = input.IsBurnRestToken,
                AdditionalInfo = input.AdditionalInfo,
                Creator = Context.Sender,
                ToRaisedAmount = input.ToRaisedAmount,
                Enabled = true
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
            if (input.WhitelistId != null)
            {
                State.WhiteListIdMap[id] = input.WhitelistId;
            }
            else
            {
                //create whitelist
                State.WhitelistContract.CreateWhitelist.Send(new CreateWhitelistInput()
                {
                    Creator = Context.Self,
                    ProjectId = id,
                    ManagerList = new AddressList(){Value = { Context.Self, Context.Sender}}
                });
                //write id to state
                Context.SendInline(Context.Self, nameof(SetWhitelistId), id);
            }
           
          
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
            
            var isEnableWhitelist = projectInfo.IsEnableWhitelist;
            Assert(isEnableWhitelist,"whitelist is disabled");
            
            var list = new AddressList();
            foreach (var user in input.Users)
            {
                list.Value.Add(user);
            }
            var whitelistId = State.WhiteListIdMap[input.ProjectId];
            State.WhitelistContract.AddAddressInfoListToWhitelist.Send(new AddAddressInfoListToWhitelistInput()
            {
                ExtraInfoIdList = new Whitelist.ExtraInfoIdList(){Value = { new Whitelist.ExtraInfoId(){}}},
                WhitelistId = whitelistId
            });
            return new Empty();
        }

        public override Empty RemoveWhitelists(RemoveWhitelistsInput input)
        {
            var projectInfo = ValidProjectExist(input.ProjectId);
            Assert(projectInfo.Enabled,"project is not enabled");
            ValidProjectOwner(input.ProjectId);

            var isEnableWhitelist = projectInfo.IsEnableWhitelist;
            Assert(isEnableWhitelist,"whitelist is disabled");
            
            var list = new AddressList();
            foreach (var user in input.Users)
            {
                list.Value.Add(user);
            }
            var whitelistId = State.WhiteListIdMap[input.ProjectId];
            State.WhitelistContract.RemoveAddressInfoListFromWhitelist.Send(new RemoveAddressInfoListFromWhitelistInput()
            {
                ExtraInfoIdList = new Whitelist.ExtraInfoIdList(){Value = { new Whitelist.ExtraInfoId(){}}},
                WhitelistId = whitelistId
            });
            return new Empty();
        }

        public override Empty EnableWhitelist(Hash input)
        {
            var projectInfo = ValidProjectExist(input);
            Assert(projectInfo.Enabled,"project is not enabled");
            ValidProjectOwner(input);
            var isEnableWhitelist = projectInfo.IsEnableWhitelist;
            Assert(!isEnableWhitelist,"whitelist is enabled");
            State.ProjectInfoMap[input].IsEnableWhitelist = true;
            State.WhitelistContract.EnableWhitelist.Send(input);
            return new Empty();
        }

        public override Empty DisableWhitelist(Hash input)
        {
            var projectInfo = ValidProjectExist(input);
            Assert(projectInfo.Enabled,"project is not enabled");
            ValidProjectOwner(input);
            var isEnableWhitelist = projectInfo.IsEnableWhitelist;
            Assert(isEnableWhitelist,"whitelist is disabled");
            State.ProjectInfoMap[input].IsEnableWhitelist = false;
            State.WhitelistContract.DisableWhitelist.Send(input);
            return new Empty();
        }

        public override Empty UpdateAdditionalInfo(UpdateAdditionalInfoInput input)
        {
            var projectInfo = ValidProjectExist(input.ProjectId);
            Assert(projectInfo.Enabled,"project is not enabled");
            ValidProjectOwner(input.ProjectId);
            State.ProjectInfoMap[input.ProjectId].AdditionalInfo = input.AdditionalInfo;
            Context.Fire(new AdditionalInfoUpdated()
            {
                ProjectId = input.ProjectId,
                AdditionalInfo = input.AdditionalInfo
            });
            return new Empty();
        }

        public override Empty Cancel(Hash input)
        {
            var projectInfo = ValidProjectExist(input);
            Assert(projectInfo.Enabled,"project is not enabled");
            ValidProjectOwner(input);
            Assert(Context.CurrentBlockTime <= projectInfo.EndTime,"time is expired");
            State.ProjectInfoMap[input].Enabled = false;
            Context.Fire(new ProjectCanceled()
            {
                ProjectId = input
            });
            return new Empty();
        }

        public override Empty NextPeriod(Hash input)
        {
            var projectInfo = ValidProjectExist(input);
            Assert(projectInfo.Enabled,"project is not enabled");
            ValidProjectOwner(input);
            var projectListInfo = State.ProjectListInfoMap[input];
            Assert(projectListInfo.LatestPeriod < projectListInfo.TotalPeriod,"insufficient period");
            var nextPeriodTime =
                projectInfo.EndTime.Seconds.Add(projectListInfo.PeriodDuration.Mul(projectListInfo.LatestPeriod));
            Assert(Context.CurrentBlockTime.Seconds >= nextPeriodTime,"time is not ready");
            State.ProjectListInfoMap[input].LatestPeriod = State.ProjectListInfoMap[input].LatestPeriod.Add(1);
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
            Assert(investDetail.IsUnInvest == false,"user has bad record");
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

        public override Empty UnInvest(Hash input)
        {
            var projectInfo = ValidProjectExist(input);
            var currentTimestamp = Context.CurrentBlockTime;
            Assert(currentTimestamp >= projectInfo.StartTime && currentTimestamp <= projectInfo.EndTime,"can't invest right now");
            //unInvest 
            var userinfo = State.InvestDetailMap[input][Context.Sender];
            Assert(userinfo.Amount > 0,"insufficient invest amount");
            Assert(userinfo.IsUnInvest == false,"user has already unInvest");

            State.LiquidatedDamageDetailsMap[input] =
                State.LiquidatedDamageDetailsMap[input] ?? new LiquidatedDamageDetails();
            var liquidatedDamageDetails = State.LiquidatedDamageDetailsMap[input];
            var liquidatedDamageAmount = userinfo.Amount.Mul(LiquidatedDamageProportion).Div(ProportionMax);

            var detail = new LiquidatedDamageDetail()
            {
                Amount = liquidatedDamageAmount,
                Symbol = userinfo.InvestSymbol,
                User = Context.Sender
            };

            State.InvestDetailMap[input][Context.Sender].Amount = 0;
            State.InvestDetailMap[input][Context.Sender].IsUnInvest = true;
            var unInvestAmount = userinfo.Amount.Sub(liquidatedDamageAmount);
            TransferOut(Context.Sender,userinfo.InvestSymbol, unInvestAmount);
            Context.Fire(new UnInvested()
            {
                ProjectId = input,
                User = Context.Sender,
                InvestSymbol = userinfo.InvestSymbol,
                TotalAmount = userinfo.Amount,
                UnInvestAmount = unInvestAmount
            });
            
            liquidatedDamageDetails.Details.Add(detail);
            liquidatedDamageDetails.TotalAmount = liquidatedDamageDetails.TotalAmount.Add(liquidatedDamageAmount);
            State.LiquidatedDamageDetailsMap[input] = liquidatedDamageDetails;
            Context.Fire(new LiquidatedDamageRecord()
            {
                ProjectId = input,
                User = Context.Sender,
                InvestSymbol = userinfo.InvestSymbol,
                Amount = liquidatedDamageAmount
            });
            return new Empty();
        }

        public override Empty LockLiquidity(Hash input)
        {
            var projectInfo = ValidProjectExist(input);
            Assert(projectInfo.Enabled,"project is not enabled");
            ValidProjectOwner(input);
            var projectListInfo = State.ProjectListInfoMap[input];
            Assert(Context.CurrentBlockTime >= projectInfo.EndTime,"time is not ready");
            var acceptedCurrencyAmount = projectInfo.CurrentRaisedAmount.Mul(projectListInfo.LiquidityLockProportion)
                .Div(ProportionMax);

            var projectCurrencyAmount = acceptedCurrencyAmount.Mul(projectListInfo.PublicSalePrice).Div(Mantissa);
     
            foreach (var market in projectListInfo.ListMarketInfo.Data )
            {
                var amountADesired = acceptedCurrencyAmount.Mul(market.Weight).Div(ProportionMax);
                var amountBDesired = projectCurrencyAmount.Mul(market.Weight).Div(ProportionMax);
                
                State.TokenContract.Approve.Send(new ApproveInput()
                {
                    Spender = market.Market,
                    Symbol = projectInfo.AcceptedCurrency,
                    Amount = amountADesired
                });
                State.TokenContract.Approve.Send(new ApproveInput()
                {
                    Spender = market.Market,
                    Symbol = projectInfo.ProjectCurrency,
                    Amount = amountBDesired
                });
                State.SwapContract.Value = market.Market;
                State.SwapContract.AddLiquidity.Send(new AddLiquidityInput()
                {
                    SymbolA = projectInfo.AcceptedCurrency,
                    SymbolB = projectInfo.ProjectCurrency,
                    AmountADesired = amountADesired,
                    AmountBDesired = amountBDesired,
                    AmountAMin = 0,
                    AmountBMin = 0,
                    Channel = "",
                    Deadline = Context.CurrentBlockTime.AddMinutes(3),
                    To = Context.Sender
                });    
                
            }

            
           
            return new Empty();
        }

        public override Empty Withdraw(Hash input)
        {
            var projectInfo = ValidProjectExist(input);
            Assert(projectInfo.Enabled,"project is not enabled");
            ValidProjectOwner(input);
            var projectListInfo = State.ProjectListInfoMap[input];
            Assert(Context.CurrentBlockTime >= projectInfo.EndTime,"time is not ready");
            var withdrawAmount = projectInfo.CurrentRaisedAmount.Mul(ProportionMax.Sub(projectListInfo.LiquidityLockProportion))
                .Div(ProportionMax);
            TransferOut(Context.Sender, projectInfo.AcceptedCurrency, withdrawAmount);
            return new Empty();
        }

        public override Empty ClaimLiquidatedDamage(Hash input)
        {
            //check status
            var projectInfo = ValidProjectExist(input);
            Assert(projectInfo.Enabled == false,"project should be disabled");

            var detail = State.LiquidatedDamageDetailsMap[input].Details.First(x => x.User == Context.Sender);
            Assert(detail != null,"no record in LiquidatedDamageDetails");
            Assert(detail.Claimed == false,"already claimed");
            TransferOut(detail.User,detail.Symbol,detail.Amount);
            Context.Fire(new LiquidatedDamageClaimed()
            {
                ProjectId = input,
                Amount = detail.Amount,
                InvestSymbol = detail.Symbol,
                User = detail.User
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
            Assert(profitDetailInfo.AmountsMap.Count > 0, "no invest record");
            State.ClaimedProfitsInfoMap[input.User] = State.ClaimedProfitsInfoMap[input.User]?? new ClaimedProfitsInfo();
            var claimedProfitsInfo = State.ClaimedProfitsInfoMap[input.User];
            for (var i = profitDetailInfo.LatestPeriod + 1; i <= listInfo.LatestPeriod; i++)
            {
                var currentPeriod = i ;
                var profitPeriodAmount = profitDetailInfo.AmountsMap[currentPeriod];
                if (profitPeriodAmount > 0)
                {
                    TransferOut(input.User, profitDetailInfo.Symbol, profitPeriodAmount);
                }
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
            State.ProfitDetailMap[input.ProjectId][input.User].LatestPeriod = listInfo.LatestPeriod;
            
            return new Empty();
        }

        public override Empty SetWhitelistId(Hash input)
        {
            Assert(Context.Sender == Context.Self,"only self contract can call this function");
            var whitelistIdList = State.WhitelistContract.GetWhitelistByProject.Call(input);
            var whitelistId = whitelistIdList.WhitelistId.First();
            State.WhiteListIdMap[input] = whitelistId;
            return new Empty();
        }
    }
}