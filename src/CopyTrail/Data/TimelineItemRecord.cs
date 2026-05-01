using CopyTrail.Models;

namespace CopyTrail.Data;

public sealed record TimelineItemRecord(ClipboardContent Content, ClipboardEvent Event);
