using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace FocusTemplate.Intel.Worker;

[Function("symbol", "string")]
public sealed class SymbolFunction : FunctionMessage;