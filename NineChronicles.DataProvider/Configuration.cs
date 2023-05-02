#nullable enable

namespace NineChronicles.DataProvider
{
    using NineChronicles.Headless.Properties;

    public class Configuration
    {
        public string? AppProtocolVersionToken { get; set; }

        public bool NoMiner { get; set; }

        public string? GenesisBlockPath { get; set; }

        public string? Host { get; set; }

        public ushort? Port { get; set; }

        public string? SwarmPrivateKeyString { get; set; }

        public string? MinerPrivateKeyString { get; set; }

        public string? StoreType { get; set; }

        public string? StorePath { get; set; }

#pragma warning disable SA1011 // Closing square brackets should be spaced correctly
        public string[]? IceServerStrings { get; set; }

        public string[]? PeerStrings { get; set; }

        public string[]? TrustedAppProtocolVersionSigners { get; set; }

        public bool RpcServer { get; set; }

        public string? RpcListenHost { get; set; }

        public int? RpcListenPort { get; set; }

        public bool GraphQLServer { get; set; }

        public string? GraphQLHost { get; set; }

        public int? GraphQLPort { get; set; }

        public string? GraphQLSecretTokenPath { get; set; }

        public bool NoCors { get; set; }

        public int Confirmations { get; set; }

        public bool StrictRendering { get; set; }

        public bool IsDev { get; set; }

        public int BlockInterval { get; set; }

        public int ReorgInterval { get; set; }

        public bool LogActionRenders { get; set; }

        public bool Render { get; set; }

        public string? AwsCognitoIdentity { get; set; }

        public string? AwsAccessKey { get; set; }

        public string? AwsSecretKey { get; set; }

        public string? AwsRegion { get; set; }

        public bool AuthorizedMiner { get; set; }

        public bool Preload { get; set; }

        public int TxLifeTime { get; set; }

        public int MessageTimeout { get; set; }

        public int TipTimeout { get; set; }

        public int DemandBuffer { get; set; }

        public int MinimumBroadcastTarget { get; set; }

        public int BucketSize { get; set; }

        public string[]? StaticPeerStrings { get; set; }

        public bool NoReduceStore { get; set; }

#pragma warning restore SA1011 // Closing square brackets should be spaced correctly
#nullable disable
        public string MySqlConnectionString { get; set; }

        public NetworkType NetworkType { get; set; } = NetworkType.Main;
    }
}
