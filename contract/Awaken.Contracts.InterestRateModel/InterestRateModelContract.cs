using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace Awaken.Contracts.InterestRateModel
{
    public partial class InterestRateModelContract : InterestRateModelContractContainer.InterestRateModelContractBase
    {
        public override Empty Initialize(InitializeInput input)
        {
            Assert(State.Owner.Value != new Address(), "Initialized");
            State.Owner.Value = Context.Sender;
            State.InterestRateModelType.Value = input.InterestRateModelType;
            if (input.InterestRateModelType)
            {
                UpdateWhitePaperInterestRateModel(input.BaseRatePerYear,input.MultiplierPerYear);
            }
            else
            {
                UpdateJumpRateModelInputInternal(input.BaseRatePerYear,input.MultiplierPerYear,input.JumpMultiplierPerYear,input.Kink);
            }
            return new Empty();
        }

        public override Empty UpdateRateModel(UpdateRateModelInput input)
        {
            Assert(State.Owner.Value == Context.Sender, "Unauthorized");
            if (State.InterestRateModelType.Value)
            {
                UpdateWhitePaperInterestRateModel(input.BaseRatePerYear,input.MultiplierPerYear);
            }
            else
            {
                UpdateJumpRateModelInputInternal(input.BaseRatePerYear,input.MultiplierPerYear,input.JumpMultiplierPerYear,input.Kink);
            }
         
            return new Empty();
        }
    }
}