using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace Argus.Worker;

[Function("symbol", "string")]
public sealed class SymbolFunction : FunctionMessage;