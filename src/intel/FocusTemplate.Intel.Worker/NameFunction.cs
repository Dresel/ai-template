using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace FocusTemplate.Intel.Worker;

[Function("name", "string")]
public sealed class NameFunction : FunctionMessage;