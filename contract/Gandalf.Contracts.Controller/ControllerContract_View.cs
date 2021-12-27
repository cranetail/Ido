using System;
using AElf.Sdk.CSharp;
using AElf.Types;
using AElf.CSharp.Core;


namespace Gandalf.Contracts.Controller
{
    public partial class ControllerContract
    {
        public override GetHypotheticalAccountLiquidityOutput GetHypotheticalAccountLiquidity(GetHypotheticalAccountLiquidityInput input)
        {
            var shortfall= GetHypotheticalAccountLiquidityInternal(input.Account, input.GTokenModify, input.RedeemTokens,
                input.BorrowAmount);
            
            return base.GetHypotheticalAccountLiquidity(input);
        }
    }
}