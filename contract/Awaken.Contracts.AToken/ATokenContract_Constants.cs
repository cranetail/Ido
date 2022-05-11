using System;

namespace Awaken.Contracts.AToken
{
    public  partial class ATokenContract
    {
        /// <summary>
        /// Maximum borrow rate that can ever be applied (0.000016% / block)
        /// </summary>
        public const long MaxBorrowRate = 160000000000;

        /// <summary>
        /// Maximum fraction of interest that can be set aside for reserves
        /// </summary>
        public const long MaxReserveFactor = 1000000000000000000;

        private const long Mantissa = 1000000000000000000;

        private const long ExchangeMantissa = 100000000;

        private const int Decimals = 8;
    }
}