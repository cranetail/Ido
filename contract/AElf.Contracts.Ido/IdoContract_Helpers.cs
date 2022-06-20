using AElf.Contracts.MultiToken;
using AElf.Contracts.Whitelist;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.Collections;

namespace AElf.Contracts.Ido
{
    public partial class IdoContract
    {
        private void ValidTokenSymbol(string token)
        {
            var tokenInfo = State.TokenContract.GetTokenInfo.Call(new AElf.Contracts.MultiToken.GetTokenInfoInput
            {
                Symbol = token
            });
            
            Assert(!string.IsNullOrEmpty(tokenInfo.Symbol), $"Token {token} not exists.");
        }
        
        private void ValidTokenSymbolOwner(string token, Address owner)
        {
            var tokenInfo = State.TokenContract.GetTokenInfo.Call(new AElf.Contracts.MultiToken.GetTokenInfoInput
            {
                Symbol = token
            });
            
            Assert(!string.IsNullOrEmpty(tokenInfo.Symbol), $"Token {token} not exists.");
            Assert(tokenInfo.Issuer == owner,$"Token {token}'s issuer not {owner}.");
        }

        private static Hash GetHash(RegisterInput registerInput, Address registerAddress)
        {
            var hash = HashHelper.ConcatAndCompute(HashHelper.ComputeFrom(registerInput),
                HashHelper.ComputeFrom(registerAddress));
            return hash;
        }

        private ProjectInfo ValidProjectExist(Hash projectId)
        {
            var projectInfo = State.ProjectInfoMap[projectId];
            Assert(projectInfo != null,"Project is not exist");
            return projectInfo;
        }

        private void ValidProjectOwner(Hash projectId)
        {
            var projectInfo = State.ProjectInfoMap[projectId];
            Assert(projectInfo.Creator == Context.Sender,"Only project owner can call this function");
        }

        private void AdminCheck()
        {
            Assert(State.Admin.Value == Context.Sender,"Only admin can call this function");
        }

        private void WhitelistCheck(Hash projectId, Address user)
        {
            var whiteListId = State.WhiteListIdMap[projectId];
            var isInWhitelist = State.WhitelistContract.GetAddressFromWhitelist.Call(new GetAddressFromWhitelistInput()
            {
                Address = user,
                WhitelistId = whiteListId
            });
            
            Assert(isInWhitelist.Value,"User is not in the whitelist");
        }
        
        private void TransferIn(Address from, string symbol, long amount)
        {
            State.TokenContract.TransferFrom.Send(
                new TransferFromInput
                {
                    Symbol = symbol,
                    Amount = amount,
                    From = from,
                    Memo = "TransferIn",
                    To = Context.Self
                });
        }
        
        private void TransferOut(Address to, string symbol, long amount)
        {
            State.TokenContract.Transfer.Send(
                new TransferInput()
                {
                    Symbol = symbol,
                    Amount = amount,
                    Memo = "TransferOut",
                    To = to
                });
        }

        private long GetAvailableInvestAmount(Hash projectId, Address user)
        {

           var investDetail =  State.InvestDetailMap[projectId][user];
           if (investDetail == null)
           {
               return 0;
           }

           var availableInvestAmount = State.ProjectInfoMap[projectId].MaxSubscription.Sub(investDetail.Amount);
           return availableInvestAmount;

        }

        private void CheckInvestInput(Hash projectId, Address user, long investAmount)
        {
            var projectInfo = State.ProjectInfoMap[projectId];
            var latestAmount =  State.InvestDetailMap[projectId][user] == null ? 0 : State.InvestDetailMap[projectId][user].Amount;
            Assert(investAmount > 0,"Invest amount should be positive");
            var totalAmount = latestAmount.Add(investAmount);
            Assert(totalAmount >= projectInfo.MinSubscription && totalAmount <= projectInfo.MaxSubscription,"Invest amount should be in the range of subscription");
        }

        private long ProfitDetailUpdate(Hash projectId, Address user, long investAmount)
        {
            var info = State.ProjectInfoMap[projectId];
            var listInfo = State.ProjectListInfoMap[projectId];
            if (investAmount == 0)
            {
                State.ProfitDetailMap[projectId][user] = new ProfitDetail()
                {
                    LatestPeriod = 0,
                    Symbol = info.ProjectCurrency
                };
                return 0;
            }
            State.ProfitDetailMap[projectId][user] = State.ProfitDetailMap[projectId][user] ?? new ProfitDetail()
            {
                LatestPeriod = 0,
                Symbol = info.ProjectCurrency
            };
            var totalProjectTokenAmountStr = new BigIntValue(investAmount).Mul(info.PreSalePrice).Div(Mantissa);
            var totalProjectTokenAmount = Parse(totalProjectTokenAmountStr.Value);
            State.ProfitDetailMap[projectId][user].TotalProfit = totalProjectTokenAmount;
            State.ProfitDetailMap[projectId][user].LatestPeriod = 0;
            for (var i = 1; i <= listInfo.TotalPeriod; i++)
            {
                BigIntValue periodProfitStr;
                if (i == 1)
                { 
                    periodProfitStr = new BigIntValue(totalProjectTokenAmount).Mul(listInfo.FirstDistributeProportion).Div(ProportionMax);
                }
                else
                {
                    periodProfitStr =  new BigIntValue(totalProjectTokenAmount).Mul(listInfo.RestDistributeProportion).Div(ProportionMax);
                }

                var periodProfit = Parse(periodProfitStr.Value);
                State.ProfitDetailMap[projectId][user].AmountsMap[i] = periodProfit;
            }

            return totalProjectTokenAmount;
        }
        
        private static long Parse(string input)
        {
            if (!long.TryParse(input, out var output))
            {
                throw new AssertionException($"Failed to parse {input}");
            }

            return output;
        }

    }
}