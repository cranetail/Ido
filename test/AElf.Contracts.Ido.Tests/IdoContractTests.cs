using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Ido;
using AElf.Contracts.Ido.Tests;
using System.Threading.Tasks;
using Awaken.Contracts.Swap;
using Google.Protobuf.WellKnownTypes;
using Xunit;
using System;
using System.Linq;
using System.Threading;
using AElf.Contracts.MultiToken;
using AElf.ContractTestBase.ContractTestKit;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Types;
using Shouldly;
using Awaken.Contracts.Token;
using Google.Protobuf.Collections;
using Xunit.Sdk;

namespace AElf.Contracts.Ido
{
    public class IdoContractTests : IdoContractTestBase
    {
        private Hash projectId0;
        [Fact]
        public async Task InitializeTest()
        {
           await Initialize();
           await AdminStub.Initialize.SendAsync(new InitializeInput()
           {
               WhitelistContract = WhitelistContractAddress
           });
           var whitelistAddress = await AdminStub.GetWhitelistContractAddress.CallAsync(new Empty());
           whitelistAddress.ShouldNotBe(new Address());
        }

        [Fact]
        public async Task RegisterTest()
        {
            await InitializeTest();
            var registerInput = new RegisterInput()
            {
                AcceptedCurrency = "ELF",
                ProjectCurrency = "TEST",
                CrowdFundingType = "标价销售",
                CrowdFundingIssueAmount = 10000_00000000,
                PreSalePrice = 1_00000000,
                StartTime =  Timestamp.FromDateTime(DateTime.UtcNow.Add(new TimeSpan(0, 0, 3))),
                EndTime =  Timestamp.FromDateTime(DateTime.UtcNow.Add(new TimeSpan(0, 0, 30))),
                MinSubscription = 10,
                MaxSubscription = 100,
                IsEnableWhitelist = false,
                IsBurnRestToken = true,
                AdditionalInfo = new AdditionalInfo(),
                ToRaisedAmount = 1_00000000,
                PublicSalePrice = 2_000000000,
                LiquidityLockProportion = 50,
                ListMarketInfo = new ListMarketInfo()
                {
                    Data = { new ListMarket()
                    {
                        Market = AwakenSwapContractAddress,
                        Weight = 100
                    }}
                },
                UnlockTime =Timestamp.FromDateTime(DateTime.UtcNow.Add(new TimeSpan(0, 0, 30000))),
                TotalPeriod = 1,
                FirstDistributeProportion = 100,
                RestDistributeProportion = 0,
                PeriodDuration = 0
            };
           var executionResult = await AdminStub.Register.SendAsync(registerInput);

           var projectId = ProjectRegistered.Parser
               .ParseFrom(executionResult.TransactionResult.Logs.First(l => l.Name.Contains(nameof(ProjectRegistered))).NonIndexed)
               .ProjectId;

          var whitelistId =  await AdminStub.GetWhitelistId.CallAsync(projectId);
          whitelistId.ShouldBe(HashHelper.ComputeFrom(0));
          projectId0 = projectId;

        }

        [Fact]
        public async Task WhitelistTest()
        {
            await RegisterTest();
            await AdminStub.EnableWhitelist.SendAsync(projectId0);
            var projectInfo = await AdminStub.GetProjectInfo.CallAsync(projectId0);
            projectInfo.IsEnableWhitelist.ShouldBeTrue();
            await AdminStub.AddWhitelists.SendAsync(new AddWhitelistsInput()
            {
                ProjectId = projectId0,
                Users = {UserTomAddress}
            });
            
            await AdminStub.RemoveWhitelists.SendAsync(new RemoveWhitelistsInput()
            {
                ProjectId = projectId0,
                Users = {UserTomAddress}
            });

            await AdminStub.DisableWhitelist.SendAsync(projectId0); 
            projectInfo = await AdminStub.GetProjectInfo.CallAsync(projectId0);
            projectInfo.IsEnableWhitelist.ShouldBeFalse();
        }

        [Fact]
        public async Task InvestTest()
        {
            var investAmount = 100;
            await RegisterTest();
            await AdminStub.EnableWhitelist.SendAsync(projectId0); 
            Thread.Sleep(3000);
            await TomTokenContractStub.Approve.SendAsync(new MultiToken.ApproveInput()
            {
                Spender = IdoContractAddress,
                Amount = 10000,
                Symbol = "ELF"
            });
            await TomStub.Invest.SendAsync(new InvestInput()
            {
                ProjectId = projectId0,
                Currency = "ELF",
                InvestAmount = investAmount

            });

            var investDetail =  await TomStub.GetInvestDetail.CallAsync(new GetInvestDetailInput()
            {
                ProjectId = projectId0,
                User = UserTomAddress
            });
            investDetail.Amount.ShouldBe(investAmount);
        }

        [Fact]
        public async Task ClaimTest()
        {
            await InvestTest();
            Thread.Sleep(30000);
            await AdminStub.NextPeriod.SendAsync(projectId0);
            
            await TokenContractStub.Transfer.SendAsync(new AElf.Contracts.MultiToken.TransferInput()
            {
                Amount = 100000000000,
                Symbol = "TEST",
                Memo = "ForUserClaim",
                To = IdoContractAddress
            });
            await TomStub.Claim.SendAsync(new ClaimInput()
            {
                ProjectId = projectId0,
                User = UserTomAddress
            });
            var balance = await TokenContractStub.GetBalance.SendAsync(new AElf.Contracts.MultiToken.GetBalanceInput()
            {
                Owner = UserTomAddress,
                Symbol = "TEST"
            });
            balance.Output.Balance.ShouldNotBe(0);
        }

        [Fact]
        public async Task AddLiquidityTest()
        {
            await InvestTest();
            Thread.Sleep(30000);
            await TokenContractStub.Transfer.SendAsync(new AElf.Contracts.MultiToken.TransferInput()
            {
                Amount = 100000000000,
                Symbol = "TEST",
                Memo = "ForAddLiquidity",
                To = IdoContractAddress
            });
            await AdminStub.LockLiquidity.SendAsync(projectId0);
            
        }
        private async Task CreateAndGetToken()
        {
            //TEST
            var result = await TokenContractStub.Create.SendAsync(new AElf.Contracts.MultiToken.CreateInput
            {
                Issuer = AdminAddress,
                Symbol = "TEST",
                Decimals = 8,
                IsBurnable = true,
                TokenName = "TEST symbol",
                TotalSupply = 100000000_00000000
            });

            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var issueResult = await TokenContractStub.Issue.SendAsync(new AElf.Contracts.MultiToken.IssueInput
            {
                Amount = 100000000000000,
                Symbol = "TEST",
                To = AdminAddress
            });
            issueResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var balance = await TokenContractStub.GetBalance.SendAsync(new AElf.Contracts.MultiToken.GetBalanceInput()
            {
                Owner = AdminAddress,
                Symbol = "TEST"
            });
            balance.Output.Balance.ShouldBe(100000000000000);
            //DAI
            var result2 = await TokenContractStub.Create.SendAsync(new AElf.Contracts.MultiToken.CreateInput
            {
                Issuer = AdminAddress,
                Symbol = "DAI",
                Decimals = 10,
                IsBurnable = true,
                TokenName = "DAI symbol",
                TotalSupply = 100000000_00000000
            });

            result2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var issueResult2 = await TokenContractStub.Issue.SendAsync(new AElf.Contracts.MultiToken.IssueInput
            {
                Amount = 100000000000000,
                Symbol = "DAI",
                To = AdminAddress
            });
            issueResult2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var balance2 = await TokenContractStub.GetBalance.SendAsync(new AElf.Contracts.MultiToken.GetBalanceInput()
            {
                Owner = AdminAddress,
                Symbol = "DAI"
            });
            balance2.Output.Balance.ShouldBe(100000000000000);
            await TokenContractStub.Transfer.SendAsync(new AElf.Contracts.MultiToken.TransferInput()
            {
                Amount = 100000000000,
                Symbol = "ELF",
                Memo = "Recharge",
                To = UserTomAddress
            });
            await TokenContractStub.Transfer.SendAsync(new AElf.Contracts.MultiToken.TransferInput()
            {
                Amount = 100000000000,
                Symbol = "ELF",
                Memo = "Recharge",
                To = UserLilyAddress
            });
        
           
            await TokenContractStub.Transfer.SendAsync(new AElf.Contracts.MultiToken.TransferInput()
            {
                Amount = 100000000000,
                Symbol = "DAI",
                Memo = "Recharge",
                To = UserTomAddress
            });
            //authorize  Tom and Lily and admin to transfer ELF and TEST and DAI to FinanceContract
            // await UserTomTokenContractStub.Approve.SendAsync(new AElf.Contracts.MultiToken.ApproveInput()
            // {
            //     Amount = 100000000000,
            //     Spender = AwakenSwapContractAddress,
            //     Symbol = "ELF"
            // });
            // await UserTomTokenContractStub.Approve.SendAsync(new AElf.Contracts.MultiToken.ApproveInput()
            // {
            //     Amount = 100000000000,
            //     Spender = AwakenSwapContractAddress,
            //     Symbol = "DAI"
            // });
            // await TokenContractStub.Approve.SendAsync(new AElf.Contracts.MultiToken.ApproveInput()
            // {
            //     Amount = 100000000000,
            //     Spender = AwakenSwapContractAddress,
            //     Symbol = "ELF"
            // });
            // await UserLilyTokenContractStub.Approve.SendAsync(new AElf.Contracts.MultiToken.ApproveInput()
            // {
            //     Amount = 100000000000,
            //     Spender = AwakenSwapContractAddress,
            //     Symbol = "ELF"
            // });
            // await UserTomTokenContractStub.Approve.SendAsync(new AElf.Contracts.MultiToken.ApproveInput()
            // {
            //     Amount = 100000000000,
            //     Spender = AwakenSwapContractAddress,
            //     Symbol = "TEST"
            // });
            // await TokenContractStub.Approve.SendAsync(new AElf.Contracts.MultiToken.ApproveInput()
            // {
            //     Amount = 100000000000,
            //     Spender = AwakenSwapContractAddress,
            //     Symbol = "TEST"
            // });
            // await UserLilyTokenContractStub.Approve.SendAsync(new AElf.Contracts.MultiToken.ApproveInput()
            // {
            //     Amount = 100000000000,
            //     Spender = AwakenSwapContractAddress,
            //     Symbol = "TEST"
            // });
        }
        private async Task Initialize()
        {
            await CreateAndGetToken();
            await AdminLpStub.Initialize.SendAsync(new Awaken.Contracts.Token.InitializeInput()
            {
                Owner = AwakenSwapContractAddress
            });
            await AwakenSwapContractStub.Initialize.SendAsync(new Awaken.Contracts.Swap.InitializeInput()
            {
                Admin = AdminAddress,
                AwakenTokenContractAddress = LpTokentContractAddress
            });
            await AwakenSwapContractStub.SetFeeRate.SendAsync(new Int64Value(){Value = 30});
            await UserTomSwapStub.CreatePair.SendAsync(new CreatePairInput()
            {
                SymbolPair = "ELF-TEST"
            });
        }
    }
}