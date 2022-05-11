using Google.Protobuf.WellKnownTypes;

namespace Awaken.Contracts.InterestRateModel
{
    public partial class InterestRateModelContract
    {
        private const int BlocksPerYear = 62208000; //360 * 24 * 60 * 60 * 2

        private const long Mantissa = 1000000000000000000; //1e18
    }
}