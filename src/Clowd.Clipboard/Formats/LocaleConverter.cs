using System.Globalization;

namespace Clowd.Clipboard.Formats;

/// <summary>
/// Used by CF_LOCALE, which is stored as an integer (lcid) and is represented by <see cref="CultureInfo"/>.
/// </summary>
public class LocaleConverter : Int32DataConverterBase<CultureInfo>
{
    /// <inheritdoc/>
    public override CultureInfo ReadFromInt32(int val) => new CultureInfo(val);

    /// <inheritdoc/>
    public override int WriteToInt32(CultureInfo obj) => obj.LCID;
}
