namespace Gandalf.Contracts.GToken
{
    public  partial class GTokenContract
    {
        /// <summary>
        /// Maximum borrow rate that can ever be applied (0.000016% / block)
        /// </summary>
        public const long MaxBorrowRate = 160000000000;

        /// <summary>
        /// Maximum fraction of interest that can be set aside for reserves
        /// </summary>
        public const long MaxReserveFactor = 1000000000000000000;

        /// <summary>
        /// The approximate number of blocks per year that is assumed by the interest rate model
        /// </summary>
        public const int BlocksPerYear = 63072000; //2 * 60 * 60 * 24 * 365

        // closeFactorMantissa must be strictly greater than this value
        public const long MinCloseFactor = 50000000000000000; // 0.05

        // closeFactorMantissa must not exceed this value
        public const long MaxCloseFactor = 900000000000000000; // 0.9

        // No collateralFactorMantissa may exceed this value
        public const long MaxCollateralFactor = 900000000000000000; // 0.9

        // liquidationIncentiveMantissa must be no less than this value
        public const long MinLiquidationIncentive = 1000000000000000000; // 1.0

        // liquidationIncentiveMantissa must be no greater than this value
        public const long MaxLiquidationIncentive = 1500000000000000000; // 1.5

        public const long InitialBorrowIndex = 1000000000000000000;

    
    }
}