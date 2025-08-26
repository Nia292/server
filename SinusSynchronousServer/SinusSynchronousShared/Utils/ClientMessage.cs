using SinusSynchronous.API.Data.Enum;

namespace SinusSynchronousShared.Utils;
public record ClientMessage(MessageSeverity Severity, string Message, string UID);
