using AElf.Types;

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
    }
}