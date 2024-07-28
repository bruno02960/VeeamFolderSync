using VeeamFolderSync;

if (args.Length != 4)
{
    Console.WriteLine("Mandatory arguments: <sourcePath> <configuration.ReplicaPath> <syncInterval> <logFilePath>");
    return;
}
else
{
    if (!int.TryParse(args[2], out int argVal))
    {
        Console.WriteLine("<syncInterval> should be an integer value");
        return;
    }

    if (!Directory.Exists(args[0]))
    {
        Console.WriteLine("Source path does not exist");
        return;
    }
}

string sourcePath = args[0];
string replicaPath = args[1];
string logFilePath = args[3];

try
{
    int syncIntervalInSeconds = int.Parse(args[2]);
    int syncIntervalInMilliseconds = syncIntervalInSeconds * 1000;

    Configuration configuration = new()
    {
        LogFilePath = logFilePath,
        ReplicaPath = replicaPath,
        SourcePath = sourcePath,
        SyncInterval = syncIntervalInMilliseconds
    };

    if (!Directory.Exists(configuration.ReplicaPath))
    {
        Directory.CreateDirectory(configuration.ReplicaPath);
        Log("Created replica directory", configuration.LogFilePath);
    }

    Timer timer = new(SynchronizeFolders, configuration, 0, syncIntervalInMilliseconds);

    Console.WriteLine("Press Enter to exit the program");
    Console.ReadLine();
}
catch (Exception ex)
{
    Log($"Error during program configuration {ex.Message}", logFilePath);
}

void SynchronizeFolders(object? state)
{
    // State will never be null on this case, but for best practices' sake, we should keep the callback signature
    if (state is null || state is not Configuration)
    {
        return;
    }

    Configuration configuration = (Configuration)state;

    try
    {
        string[] sourceFiles = Directory.GetFiles(configuration.SourcePath, "*", SearchOption.AllDirectories);
        string[] replicaFiles = Directory.GetFiles(configuration.ReplicaPath, "*", SearchOption.AllDirectories);

        List<string> sourceFilesRelative = sourceFiles.Select(f => f[configuration.SourcePath.Length..]).ToList();
        List<string> replicaFilesRelative = replicaFiles.Select(f => f[configuration.ReplicaPath.Length..]).ToList();

        // Copy new and updated files from source to replica
        foreach (string relativePath in sourceFilesRelative)
        {
            string sourceFile = Path.Combine(configuration.SourcePath, relativePath.TrimStart(Path.DirectorySeparatorChar));
            string replicaFile = Path.Combine(configuration.ReplicaPath, relativePath.TrimStart(Path.DirectorySeparatorChar));

            FileInfo replicaFileInfo = new(replicaFile);
            FileInfo sourceFileInfo = new(sourceFile);

            // Check if the replica does not exist or if the source file has been updated
            // Alternatively, we could have compared the MD5 hashes of these files
            if (!replicaFileInfo.Exists || sourceFileInfo.LastWriteTime > replicaFileInfo.LastWriteTime)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(replicaFile)!);
                File.Copy(sourceFile, replicaFile, true);
                Log($"Copied {relativePath}", configuration.LogFilePath);
            }
        }

        // Remove files from replica that are not in source
        foreach (string relativePath in replicaFilesRelative)
        {
            if (!sourceFilesRelative.Contains(relativePath))
            {
                string replicaFile = Path.Combine(configuration.ReplicaPath, relativePath.TrimStart(Path.DirectorySeparatorChar));
                File.Delete(replicaFile);
                Log($"Deleted {relativePath}", configuration.LogFilePath);
            }
        }

        // Remove empty directories from replica
        string[] replicaDirectories = Directory.GetDirectories(configuration.ReplicaPath, "*", SearchOption.AllDirectories);
        foreach (string dir in replicaDirectories)
        {
            if (!Directory.EnumerateFileSystemEntries(dir).Any())
            {
                Directory.Delete(dir);
                Log($"Deleted empty directory {dir[configuration.ReplicaPath.Length..]}", configuration.LogFilePath);
            }
        }
    }
    catch (Exception ex)
    {
        Log($"Error during synchronization {ex.Message}", configuration.LogFilePath);
    }
}

void Log(string message, string logFilePath)
{
    string logMessage = $"{DateTime.Now} :: {message}";
    Console.WriteLine(logMessage);
    File.AppendAllLines(logFilePath, [logMessage]);
}