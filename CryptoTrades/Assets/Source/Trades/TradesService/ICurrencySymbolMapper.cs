using Cysharp.Threading.Tasks;

public interface ICurrencySymbolMapper
{
    UniTask<string> GetSymbol((string baseAsset, string quoteAsset) currencyPair);
}