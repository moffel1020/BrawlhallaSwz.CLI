using AbcDisassembler;
using BrawlhallaSwz;
using BrawlhallaSwz.CLI;
using SwfLib.Tags.ActionsTags;

static void ShowHelp()
{
    Console.WriteLine(
    @"Usage: bh-swz [mode] [input] [output] [key] [seed]
        mode:
        -F --find-key [path to bhair]
        -D --decrypt [path to swz] [output directory] [key]
        -E --encrypt [input directory] [output swz path] [key] <seed>
        --help: show this message"
    );
    Environment.Exit(0);
}

static void ExitWithMessage(string message, int exitCode = 1)
{
    Console.WriteLine(message, exitCode == 0 ? Console.Out : Console.Error);
    Environment.Exit(exitCode);
}

if (args.Length <= 1) ShowHelp();

string mode = args[0];
if (mode == "--help") ShowHelp();

string inputPath = "";
string outputPath = "";
string key = "";
string seed = "";

if (args.Length >= 2) inputPath = args[1];
if (args.Length >= 3) outputPath = args[2];
if (args.Length >= 4) key = args[3];
if (args.Length >= 5) seed = args[4];

try
{
    if (mode == "-F" || mode == "--find-key")
    {
        if (args.Length != 2) ExitWithMessage("Find needs 2 parameters");

        DoABCDefineTag tag = Utils.GetDoABCDefineTag(inputPath) ?? throw new Exception("Could not find abc tag");
        using (MemoryStream abcBytes = new(tag.ABCData))
        {
            AbcFile abcFile = AbcFile.Read(abcBytes);
            uint decryptionKey = Utils.FindDecryptionKey(abcFile) ?? throw new Exception("Could not find key in abc data");
            Console.WriteLine(decryptionKey);
        }

        Environment.Exit(0);
    }

    if (mode == "-D" || mode == "--decrypt")
    {
        if (args.Length != 4) ExitWithMessage("Decrypt needs 4 parameters");
        using (FileStream inStream = new(inputPath, FileMode.Open, FileAccess.Read))
        {
            using SwzReader reader = new(inStream, uint.Parse(key));
            Directory.CreateDirectory(outputPath);
            while (reader.HasNext())
            {
                string content = reader.ReadFile();
                string name = SwzUtils.GetFileName(content);
                string finalPath = Path.Combine(outputPath, name);
                File.WriteAllText(finalPath, content);
            }
        }

        Environment.Exit(0);
    }

    if (mode == "-E" || mode == "--encrypt")
    {
        if (args.Length != 4 && args.Length != 5) ExitWithMessage("Encrypt needs 4 or 5 parameters");
        uint parsedSeed = 0;
        if (args.Length == 5) parsedSeed = uint.Parse(seed);

        using (FileStream outStream = new(outputPath, FileMode.Create, FileAccess.Write))
        {
            using SwzWriter writer = new(outStream, uint.Parse(key), parsedSeed);
            foreach (string filePath in Directory.EnumerateFiles(inputPath))
            {
                string fileContent = File.ReadAllText(filePath);
                writer.WriteFile(fileContent);
            }
        }

        Environment.Exit(0);
    }

    ExitWithMessage($"Unknown mode: {mode}");
}
catch (Exception e)
{
    Console.WriteLine(e.Message, Console.Error);
#if DEBUG
    Console.WriteLine(e.StackTrace ?? "", Console.Error);
#endif
    Environment.Exit(1);
}