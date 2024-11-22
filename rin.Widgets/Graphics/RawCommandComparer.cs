using rin.Widgets.Graphics.Commands;

namespace rin.Widgets.Graphics;

public class RawCommandComparer : IComparer<RawCommand>
{
    public int Compare(RawCommand x, RawCommand y)
    {
        {
            if (x.Command is CustomCommand asCustomX && y.Command is CustomCommand asCustomY)
            {
                if (asCustomX.Stage == asCustomY.Stage) return 0;
                if (asCustomX.Stage == CommandStage.Early || asCustomY.Stage == CommandStage.Late) return -1;
                if (asCustomX.Stage == CommandStage.Late || asCustomY.Stage == CommandStage.Early) return 1;
                return 0;
            }
        }

        {
            if (x.Command is CustomCommand asCustomX)
            {
                return asCustomX.Stage switch
                {
                    CommandStage.Early => -1,
                    CommandStage.Maintain => 0,
                    CommandStage.Late => 1,
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
        }

        {
            if (y.Command is CustomCommand asCustomY)
            {
                return asCustomY.Stage switch
                {
                    CommandStage.Early => 1,
                    CommandStage.Maintain => 0,
                    CommandStage.Late => -1,
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
        }
        
        return 0;
    }
}