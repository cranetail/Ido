using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace Awaken.Contracts.InterestRateModel
{
    public partial class InterestRateModelContract : InterestRateModelContractContainer.InterestRateModelContractBase
    {
        public override Empty Initialize(UpdateJumpRateModelInput input)
        {
            Assert(State.Owner.Value != new Address(), "Initialized");
            State.Owner.Value = Context.Sender;
            UpdateJumpRateModelInputInternal(input.BaseRatePerYear,input.MultiplierPerYear,input.JumpMultiplierPerYear,input.Kink);
            return new Empty();
        }

        public override Empty UpdateJumpRateModel(UpdateJumpRateModelInput input)
        {
            Assert(State.Owner.Value == Context.Sender, "Unauthorized");
            UpdateJumpRateModelInputInternal(input.BaseRatePerYear,input.MultiplierPerYear,input.JumpMultiplierPerYear,input.Kink);
            return new Empty();
        }
    }
}