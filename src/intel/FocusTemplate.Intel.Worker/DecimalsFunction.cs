using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace FocusTemplate.Intel.Worker;

[Function("decimals", "uint8")]
public sealed class DecimalsFunction : FunctionMessage;