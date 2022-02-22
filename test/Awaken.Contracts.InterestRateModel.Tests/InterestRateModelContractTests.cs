using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.ContractTestBase.ContractTestKit;
using AElf.CSharp.Core;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;
using Awaken.Contracts.InterestRateModel.Tests;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;
namespace Awaken.Contracts.InterestRateModel
{
    public class InterestRateModelContractTests: InterestRateModelContractTestBase
    {
        [Fact]
        public async Task InitializeTest()
        {
            await Initialize();
        }
        
        private async Task Initialize()
        {
            const long baseRatePerYear = 0;
            const long  multiplierPerYear = 57500000000000000;
            const long jumpMultiplierPerYear = 3000000000000000000;
            const long kink = 800000000000000000;
            await AdminStub.Initialize.SendAsync(new UpdateJumpRateModelInput()
            {
                BaseRatePerYear = baseRatePerYear,
                MultiplierPerYear = multiplierPerYear,
                JumpMultiplierPerYear = jumpMultiplierPerYear,
                Kink = kink
            });
        } 
        
        [Fact]
        public async Task ViewTest()
        {
            const long baseRatePerYear = 0;
            const long  multiplierPerYear = 57500000000000000;
            const long jumpMultiplierPerYear = 3000000000000000000;
            const long kink = 800000000000000000;
            const int blocksPerYear = 63072000;
            const long mantissa = 1000000000000000000;
            await Initialize();
            var owner = await AdminStub.GetOwner.CallAsync(new Empty());
            var baseRatePerBlockReal = await AdminStub.GetBaseRatePerBlock.CallAsync(new Empty());
            var multiplierPerBlockReal = await AdminStub.GetMultiplierPerBlock.CallAsync(new Empty());
            var jumpMultiplierPerBlockReal = await AdminStub.GetJumpMultiplierPerBlock.CallAsync(new Empty());
            var kinkReal = await AdminStub.GetKink.CallAsync(new Empty());

            var baseRatePerBlockExpect = baseRatePerYear.Div(blocksPerYear);
            var multiplierPerBlockExpect = Convert.ToInt64(new BigIntValue(multiplierPerYear).Mul(mantissa)
                .Div(new BigIntValue(blocksPerYear).Mul(kink)).Value);
            var jumpMultiplierPerBlockExpect = jumpMultiplierPerYear.Div(blocksPerYear);
            owner.Value.ShouldBe(AdminAddress.Value);
            baseRatePerBlockReal.Value.ShouldBe(baseRatePerBlockExpect);
            multiplierPerBlockReal.Value.ShouldBe(multiplierPerBlockExpect);
            jumpMultiplierPerBlockReal.Value.ShouldBe(jumpMultiplierPerBlockExpect);
            kinkReal.Value.ShouldBe(kink);

            const long borrows = 800000000;
            const long cash = 1000000000;
            const long reserves = 800000000;
    
            var utilizationRate = await AdminStub.GetUtilizationRate.CallAsync(new GetUtilizationRateInput()
            {
                Borrows = borrows,
                Cash = cash,
                Reserves = reserves
            });
            var utilizationRateExpect =
                Convert.ToInt64(new BigIntValue(borrows).Mul(mantissa).Div(borrows.Add(cash).Sub(reserves)).Value); 
            utilizationRate.Value.ShouldBe(utilizationRateExpect);
            utilizationRate = await AdminStub.GetUtilizationRate.CallAsync(new GetUtilizationRateInput()
            {
                Borrows = 0,
                Cash = cash,
                Reserves = reserves
            });
            
            utilizationRate.Value.ShouldBe(0);
            var borrowRate = await AdminStub.GetBorrowRate.CallAsync(new GetBorrowRateInput()
            {
                Borrows = borrows,
                Cash = cash,
                Reserves = reserves
            });

            var supplyRate = await AdminStub.GetSupplyRate.CallAsync(new GetSupplyRateInput()
            {
                Borrows = borrows,
                Cash = cash,
                Reserves = reserves,
                ReserveFactor = 700000000000000000
            });
        }

        [Fact]
        public async Task UpdateJumpRateModelTest()
        {
            const long baseRatePerYear = 0;
            const long  multiplierPerYear = 57500000000000000;
            const long jumpMultiplierPerYear = 3000000000000000000;
            const long kink = 700000000000000000;
            await Initialize();
            await AdminStub.UpdateJumpRateModel.SendAsync(new UpdateJumpRateModelInput()
            {
                BaseRatePerYear = baseRatePerYear,
                MultiplierPerYear = multiplierPerYear,
                JumpMultiplierPerYear = jumpMultiplierPerYear,
                Kink = kink
            });
            var kinkReal = await AdminStub.GetKink.CallAsync(new Empty());
            kinkReal.Value.ShouldBe(kink);
        }
    }
}