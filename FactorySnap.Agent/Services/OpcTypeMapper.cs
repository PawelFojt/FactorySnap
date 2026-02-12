using Opc.Ua;
using FactorySnap.Shared.Enums;

namespace FactorySnap.Agent.Services;

public static class OpcTypeMapper
{
    public static DataType Map(BuiltInType opcType)
    {
        return opcType switch
        {
            BuiltInType.Boolean => DataType.Boolean,
            BuiltInType.SByte => DataType.SByte,
            BuiltInType.Byte => DataType.Byte,
            BuiltInType.Int16 => DataType.Int16,
            BuiltInType.UInt16 => DataType.UInt16,
            BuiltInType.Int32 => DataType.Int32,
            BuiltInType.UInt32 => DataType.UInt32,
            BuiltInType.Int64 => DataType.Int64,
            BuiltInType.UInt64 => DataType.UInt64,
            BuiltInType.Float => DataType.Float,
            BuiltInType.Double => DataType.Double,
            BuiltInType.String => DataType.String,
            BuiltInType.DateTime => DataType.DateTime,
            BuiltInType.ByteString => DataType.ByteArray,
            _ => DataType.String
        };
    }
}