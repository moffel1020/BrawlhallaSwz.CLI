using AbcDisassembler;
using AbcDisassembler.Instructions;
using AbcDisassembler.Multinames;
using AbcDisassembler.Swf;
using AbcDisassembler.Swf.Tags;

namespace BrawlhallaSwz.CLI;

public class Utils
{
    private static List<int> FindGetlexPositions(CPoolInfo cpool, string lexName, List<Instruction> code) => code
        .Select((o, i) => new { Item = o, Index = i })
        .Where(o => o.Item.Name == "getlex" && o.Item.Args[0].Value is INamedMultiname name && cpool.Strings[(int)name.Name] == lexName)
        .Select(o => o.Index)
        .ToList();

    private static int FindCallpropvoidPos(CPoolInfo cpool, string methodName, List<Instruction> code) => code
        .FindIndex(i => i.Name == "callpropvoid" && i.Args[0].Value is INamedMultiname named && cpool.Strings[(int)named.Name] == methodName);

    private static uint? FindLastPushuintArg(List<Instruction> ins) => (uint?)ins
        .LastOrDefault(ins => ins.Name == "pushuint")?.Args[0].Value;

    public static uint? FindDecryptionKey(AbcFile abc)
    {
        foreach (MethodBodyInfo mb in abc.MethodBodies)
        {
            List<int> getlexPos = FindGetlexPositions(abc.ConstantPool, "ANE_RawData", mb.Code);

            for (int i = 0; i < getlexPos.Count; i++)
            {
                int callpropvoidPos = getlexPos[i] == getlexPos[^1]
                    ? FindCallpropvoidPos(abc.ConstantPool, "Init", mb.Code[getlexPos[i]..])
                    : FindCallpropvoidPos(abc.ConstantPool, "Init", mb.Code[getlexPos[i]..getlexPos[i + 1]]);

                if (callpropvoidPos != -1)
                    return FindLastPushuintArg(mb.Code[0..callpropvoidPos]);
            }
        }

        return null;
    }

    public static DoAbcTag? GetDoAbcTag(string swfPath)
    {
        using FileStream stream = new(swfPath, FileMode.Open, FileAccess.Read);
        foreach (ITag tag in SwfFile.ReadTags(stream))
        {
            if (tag is DoAbcTag doAbcTag)
                return doAbcTag;
        }

        return null;
    }
}