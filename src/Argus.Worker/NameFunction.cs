using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace Argus.Worker;

[Function("name", "string")]
public sealed class NameFunction : FunctionMessage;