using System.Text;

namespace HylianGrimoire.Models;

public interface IMessageEntryMetadataFingerprint
{
    void AppendFingerprint(StringBuilder fingerprint);
}
