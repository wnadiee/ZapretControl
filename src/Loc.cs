namespace ZapretControl
{
    internal static class Loc
    {
        public static string Lang = "ru";
        public static void Init() { try { Lang = Core.GetLang(); } catch { Lang = "ru"; } }
        public static bool En { get { return Lang == "en"; } }
        public static string S(string ru, string en) { return En ? en : ru; }
    }
}
