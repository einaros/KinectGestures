namespace Kinect
{
    using Microsoft.Research.Kinect.Nui;

    internal interface IGesture
    {
        event GestureEvent Ended;
        event GestureEvent Started;
        void ProcessBodyFrame(Body body);
    }

    internal delegate void GestureEvent(IGesture sender);
}