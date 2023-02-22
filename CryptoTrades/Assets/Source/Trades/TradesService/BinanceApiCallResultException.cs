using System;
using CryptoExchange.Net.Objects;

public class BinanceApiCallResultException : Exception
{
    public CallResult CallResult { get; }
    public override string Message => CallResult.Error?.Message ?? "Api error";
    public BinanceApiCallResultException(CallResult r) => CallResult = r;
}