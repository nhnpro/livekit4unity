using LiveKit.Proto;

namespace LiveKitUnity.Runtime.Types
{
    public enum EncryptionType
    {
        None,
        Gcm,
        Custom,
    }
    
    public static class EncryptionTypeExtensions
    {
        public static LiveKit.Proto.Encryption.Types.Type ToPbType(this EncryptionType encryptionType)
        {
            return encryptionType switch
            {
                EncryptionType.None => Encryption.Types.Type.None,
                EncryptionType.Gcm => Encryption.Types.Type.Gcm,
                EncryptionType.Custom => Encryption.Types.Type.Custom,
                _ => Encryption.Types.Type.None
            };
        }
        
        public static EncryptionType ToLKType(this LiveKit.Proto.Encryption.Types.Type encryptionType)
        {
            return encryptionType switch
            {
                Encryption.Types.Type.None => EncryptionType.None,
                Encryption.Types.Type.Gcm => EncryptionType.Gcm,
                Encryption.Types.Type.Custom => EncryptionType.Custom,
                _ => EncryptionType.None
            };
        }
        
    }
}