using System;
using AElf.Sdk.CSharp;
using AElf.Types;
using AElf.CSharp.Core;
using Google.Protobuf.WellKnownTypes;


namespace Gandalf.Contracts.Controller
{
    public partial class ControllerContract
    {
        public override BoolValue SetBorrowPaused(SetPausedInput input)
        {
            MarketVerify(input.GToken);
            Assert(Context.Sender == State.PauseGuardian.Value || Context.Sender == State.Admin.Value,
                "Only pause guardian and admin can pause");
            Assert(Context.Sender == State.Admin.Value || input.State, "Only admin can unpause");
            State.BorrowGuardianPaused[input.GToken] = input.State;
            return new BoolValue()
            {
                Value = input.State
            };
        }

        public override Empty SetCloseFactor(Int64Value input)
        {
            var oldCloseFactor = State.CloseFactor.Value;
            var newCloseFactor = input.Value;
            Assert(Context.Sender == State.Admin.Value, "Unauthorized");
            Assert(newCloseFactor > MinCloseFactor && newCloseFactor < MaxCloseFactor,
                "Invalid CloseFactor"); //INVALID_CLOSE_FACTOR
            State.CloseFactor.Value = input.Value;
            Context.Fire(new CloseFactorChanged()
            {
                OldCloseFactor = oldCloseFactor,
                NewCloseFactor = newCloseFactor
            });
            return new Empty();;
        }

        public override Empty SetCollateralFactor(SetCollateralFactorInput input)
        {
            Assert(Context.Sender == State.Admin.Value, "Unauthorized");
            MarketVerify(input.GToken);
            var market = State.Markets[input.GToken];
            var oldCollateralFactor = market.CollateralFactor;
            var newCollateralFactor = input.NewCollateralFactor;
            Assert(newCollateralFactor <= MaxCollateralFactor && newCollateralFactor >= 0,
                "Invalid CloseFactor");
            if (newCollateralFactor > 0 && GetUnderlyingPrice(input.GToken) == 0)
            {
                throw new AssertionException("Error Price");
            }

            market.CollateralFactor = input.NewCollateralFactor;
            Context.Fire(new CollateralFactorChanged()
            {
                GToken = input.GToken,
                OldCollateralFactor = oldCollateralFactor,
                NewCollateralFactor = newCollateralFactor
               
            });
            return new Empty();
        }

        public override Empty SetLiquidationIncentive(Int64Value input)
        {
            Assert(Context.Sender == State.Admin.Value, "Unauthorized");
            var oldLiquidationIncentive = State.LiquidationIncentive.Value;
            var newLiquidationIncentive = input.Value;
            Assert(
                newLiquidationIncentive <= MaxLiquidationIncentive &&
                newLiquidationIncentive >= MinLiquidationIncentive,
                "Invalid LiquidationIncentive"); 
            State.LiquidationIncentive.Value = input.Value;
            Context.Fire(new LiquidationIncentiveChanged()
            {
                OldLiquidationIncentive = oldLiquidationIncentive,
                NewLiquidationIncentive = newLiquidationIncentive
            });
            return new Empty();
        }

        public override Empty SetMaxAssets(Int32Value input)
        {
            Assert(Context.Sender == State.Admin.Value, "Unauthorized");
            var oldMaxAssets = State.MaxAssets.Value;
            Assert(input.Value > 0, "Invalid MaxAssets");
            State.MaxAssets.Value = input.Value;
            Context.Fire(new MaxAssetsChanged()
            {
                OldMaxAssets = oldMaxAssets,
                NewMaxAssets = input.Value
                
            });
            return new Empty();
        }

        public override BoolValue SetMintPaused(SetPausedInput input)
        {
            MarketVerify(input.GToken);
            Assert(Context.Sender == State.PauseGuardian.Value || Context.Sender == State.Admin.Value,
                "Only pause guardian and admin can pause");
            Assert(Context.Sender == State.Admin.Value || input.State, "Only admin can unpause");
            State.MintGuardianPaused[input.GToken] = input.State;
            Context.Fire(new ActionPaused()
            {
                GToken = input.GToken,
                Action = "Mint",
                PauseState = input.State
            });
            return new BoolValue()
            {
                Value = input.State
            };
        }

        public override Empty SetPauseGuardian(Address input)
        {
            Assert(Context.Sender == State.Admin.Value, "Unauthorized");
            var oldPauseGuardian = State.PauseGuardian.Value;
            var newPauseGuardian = input;
            State.PauseGuardian.Value = newPauseGuardian;
            Context.Fire(new PauseGuardianChanged()
            {
                OldPauseGuardian = oldPauseGuardian,
                NewPauseGuardian = newPauseGuardian
            });
            return new Empty();
        }

        public override BoolValue SetSeizePaused(BoolValue input)
        {
            Assert(Context.Sender == State.PauseGuardian.Value || Context.Sender == State.Admin.Value,
                "Only pause guardian and admin can pause");
            Assert(Context.Sender == State.Admin.Value || input.Value, "Only admin can unpause");
            State.SeizeGuardianPaused.Value = input.Value;
            Context.Fire(new ActionPaused()
            {
                GToken = new Address(){},
                Action = "Seize",
                PauseState = input.Value
            });
            return new BoolValue()
            {
                Value = input.Value
            };
        }

        public override BoolValue SetTransferPaused(BoolValue input)
        {
            Assert(Context.Sender == State.PauseGuardian.Value || Context.Sender == State.Admin.Value,
                "Only pause guardian and admin can pause");
            Assert(Context.Sender == State.Admin.Value || input.Value, "Only admin can unpause");
            State.TransferGuardianPaused.Value = input.Value;
            Context.Fire(new ActionPaused()
            {
                GToken = new Address(){},
                Action = "Transfer",
                PauseState = input.Value
            });
            return new BoolValue()
            {
                Value = input.Value
            };
        }

        public override Empty SetBorrowCapGuardian(Address input)
        {
            Assert(Context.Sender == State.Admin.Value, "Only admin can set borrow cap guardian");
            var oldBorrowCapGuardian = State.BorrowCapGuardian.Value;
            State.BorrowCapGuardian.Value = input;
            Context.Fire(new BorrowCapGuardianChanged()
            {
                OldBorrowCapGuardian = oldBorrowCapGuardian,
                NewBorrowCapGuardian = input
            });
            return new Empty();
        }

        public override Empty SetMarketBorrowCaps(SetMarketBorrowCapsInput input)
        {
            Assert(Context.Sender == State.BorrowCapGuardian.Value || Context.Sender == State.Admin.Value,
                "Only admin or borrow cap guardian can set borrow caps");
            var numMarkets = input.MarketBorrowCap.Count;
            for (var i = 0; i < numMarkets; i++)
            {
                State.BorrowCaps[input.MarketBorrowCap[i].GToken].Value = input.MarketBorrowCap[i].NewBorrowCap;
                Context.Fire(new BorrowCapChanged()
                {
                    GToken = input.MarketBorrowCap[i].GToken,
                    NewBorrowCap = input.MarketBorrowCap[i].NewBorrowCap
                });
            }
            return new Empty();
        }

        public override Empty SetPlatformTokenRate(Int64Value input)
        {
            Assert(Context.Sender == State.Admin.Value, "Only admin can change platformToken rate");
            var oldRate = State.PlatformTokenRate.Value;
            State.PlatformTokenRate.Value = input.Value;
            Context.Fire(new  PlatformTokenRateChanged()
            {
                OldPlatformTokenRate = oldRate,
                NewPlatformTokenRate = input.Value
            });
            return new Empty();
        }
    }
}