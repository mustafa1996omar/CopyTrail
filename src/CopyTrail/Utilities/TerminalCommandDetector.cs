namespace CopyTrail.Utilities;

public static class TerminalCommandDetector
{
    private static readonly HashSet<string> KnownCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "npm", "pnpm", "yarn", "git", "dotnet", "adb", "cd", "mkdir", "docker",
        "kubectl", "npx", "code", "winget", "pip", "pip3", "python", "python3",
        "node", "cargo", "go", "make", "cmake", "apt", "apt-get", "brew", "choco",
        "pwsh", "powershell", "bash", "zsh", "sh",
        "ls", "dir", "cat", "echo", "curl", "wget", "ssh", "scp", "grep", "find",
        "rm", "rmdir", "mv", "cp", "touch", "chmod", "chown", "sudo", "su",
        "ping", "tracert", "traceroute", "netstat", "ipconfig", "ifconfig",
        "systemctl", "service", "kill", "ps", "top", "htop"
    };

    public static bool IsTerminalCommand(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        var trimmed = text.Trim();

        // More than 3 lines is not a single terminal command
        var lineCount = trimmed.Split('\n').Length;
        if (lineCount > 3) return false;

        // Shell prompt prefixes are a strong signal
        if (trimmed.StartsWith("$ ") || trimmed.StartsWith("# ") || trimmed.StartsWith("> "))
            return true;

        // Check first word against known command list
        var firstWord = trimmed.Split([' ', '\t'], 2)[0].TrimEnd('\\', '/');
        return KnownCommands.Contains(firstWord);
    }
}
