namespace BetterCommands.Parsing
{
    public static class ColorUtils
    {
        public static void ColorMatchError(ref string error, bool isException)
        {
            error = error.Replace("failed", "<color=red>failed</color>")
                         .Replace("fail", "<color=red>fail</color>")
                         .Replace("error", "<color=red>error</color>")
                         .Replace("Failed", "<color=red>Failed</color>")
                         .Replace("Fail", "<color=red>Fail</color>");

            if (isException)
            {
                error = error.Replace("at", "<color=#FFF933>at</color>");
            }
        }
    }
}