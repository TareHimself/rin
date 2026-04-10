namespace Rin.Framework.Views.Enums;

[Flags]
public enum Invalidation
{
    DesiredSize = 1 << 0,
    Layout =  1 << 1 | DesiredSize
}

// [Flags]
// public enum UserPermissions
// {
//     None    = 0,      // 0000
//     Read    = 1 << 0, // 0001 (1)
//     Write   = 1 << 1, // 0010 (2)
//     Execute = 1 << 2, // 0100 (4)
//     Admin   = Read | Write | Execute // 0111 (7) - Combined constant
// }