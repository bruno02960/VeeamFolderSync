namespace VeeamFolderSync
{
    internal class Configuration
    {
        public required string SourcePath { get; set; }
        public required string ReplicaPath { get; set; }
        public required int SyncInterval { get; set; }
        public required string LogFilePath { get; set; }
    }
}
