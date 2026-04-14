using System;

namespace SaveSystem
{
    [Serializable]
    public class SaveEnvelope
    {
        public const string Magic = "SKTRSAVE1";

        public string magic = Magic;
        public string documentType;
        public int documentVersion = 1;
        public string transactionId;
        public string createdAtUtc;
        public string payloadEncoding = "json+gzip+xor+base64";
        public string payloadHash;
        public string payload;
    }
}
