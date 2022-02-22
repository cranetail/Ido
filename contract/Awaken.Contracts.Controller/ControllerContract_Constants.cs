using Google.Protobuf.WellKnownTypes;

namespace Awaken.Contracts.Controller
{
    public partial class ControllerContract
    {
        private const int Decimals = 100000000;

        private const long Mantissa = 1000000000000000000;
        
        // closeFactorMantissa must be strictly greater than this value
        private const long MinCloseFactor = 50000000000000000; // 0.05  scaled by 1e18

        // closeFactorMantissa must not exceed this value
        private const long MaxCloseFactor = 900000000000000000; // 0.9 scaled by 1e18
        
        // No collateralFactorMantissa may exceed this value
        private const long MaxCollateralFactor = 900000000000000000; // 0.9 scaled by 1e18

        // liquidationIncentiveMantissa must be no less than this value
        private const long MinLiquidationIncentive = 1000000000000000000; // 1.0 scaled by 1e18

        // liquidationIncentiveMantissa must be no greater than this value
        private const long MaxLiquidationIncentive = 1500000000000000000; // 1.5 scaled by 1e18

        private const long PlatformTokenInitialIndex = 1000000000000000000;
    }
}